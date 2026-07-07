// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using org.secc.LinkList.Services;
using org.secc.LinkList.Utility;

using Rock;
using Rock.Data;

namespace org.secc.LinkList.Rest.Controllers
{
    [System.Web.Http.RoutePrefix( "api/secc/linklist" )]
    public class LinkListController : ApiController
    {
        [HttpGet]
        [System.Web.Http.Route( "{idOrSlug}" )]
        public IHttpActionResult Get( string idOrSlug )
        {
            if ( idOrSlug.IsNullOrWhiteSpace() )
            {
                return Respond( HttpStatusCode.BadRequest, new { Message = "idOrSlug is required." } );
            }

            // Slug charset / length validation, on the canonical lowercase form
            // so mixed-case URLs still resolve. Numeric ids and GUIDs are also
            // accepted because ResolveItem() tries those forms first.
            var trimmed = LinkListService.NormalizeSlug( idOrSlug );
            if ( !trimmed.AsIntegerOrNull().HasValue
                && !trimmed.AsGuidOrNull().HasValue
                && !LinkListService.IsValidSlug( trimmed ) )
            {
                return Respond( HttpStatusCode.NotFound, null );
            }

            using ( var rockContext = new RockContext() )
            {
                var service = new LinkListService( rockContext );

                // Public endpoint: anonymous + IsPublic gate. Returns 404 (not
                // 403) for non-public items to avoid enumeration.
                var bag = service.GetListBag( trimmed, currentPerson: null, requirePublic: true );
                if ( bag == null )
                {
                    return Respond( HttpStatusCode.NotFound, null );
                }

                // ROCK-7164: external page view. Referer = the embedding page
                // (far more useful than the API URL). Anonymous by design.
                if ( bag.Id.HasValue )
                {
                    LinkListInteractionService.RecordView(
                        bag.Id.Value,
                        bag.Title,
                        pageUrl: Request.Headers.Referrer?.ToString() ?? Request.RequestUri?.ToString(),
                        userAgent: Request.Headers.UserAgent?.ToString(),
                        ipAddress: GetClientIp(),
                        personAliasId: null );
                }

                return Respond( HttpStatusCode.OK, bag );
            }
        }

        /// <summary>
        /// ROCK-7164: click beacon target for the web component. The body is a
        /// plain JSON STRING sent via navigator.sendBeacon, which arrives as
        /// text/plain - a CORS "simple request" (no preflight, and delivery
        /// doesn't require Access-Control-Allow-Origin since the response is
        /// never read). Web API won't model-bind text/plain, so the raw body
        /// is read and parsed by <see cref="ClickPayload"/>.
        ///
        /// ALWAYS returns 200 with an empty body regardless of validation
        /// outcome: beacons can't retry usefully, and a uniform response
        /// leaks nothing (no list enumeration signal). Invalid/spoofed
        /// payloads are silently dropped. Anti-spoof: the matrix row guid
        /// must belong to THIS list's matrix, and the recorded URL/text are
        /// read server-side - the client payload carries only the row guid.
        /// No rate limiter in v1; writes are queued/bulk-inserted so a
        /// malicious flood costs little (add a per-IP token bucket here if
        /// that changes).
        /// </summary>
        [HttpPost]
        [System.Web.Http.Route( "{idOrSlug}/click" )]
        public async Task<IHttpActionResult> PostClick( string idOrSlug )
        {
            var ok = Respond( HttpStatusCode.OK, null );

            var trimmed = LinkListService.NormalizeSlug( idOrSlug );
            if ( !trimmed.AsIntegerOrNull().HasValue
                && !trimmed.AsGuidOrNull().HasValue
                && !LinkListService.IsValidSlug( trimmed ) )
            {
                return ok;
            }

            var body = Request.Content == null ? null : await Request.Content.ReadAsStringAsync();
            if ( !ClickPayload.TryParse( body, out var matrixItemGuid ) )
            {
                return ok;
            }

            using ( var rockContext = new RockContext() )
            {
                var service = new LinkListService( rockContext );
                var item = service.ResolveItem( trimmed );
                if ( item == null )
                {
                    return ok;
                }

                // Public-only: mirrors the Get() gate and naturally excludes
                // editor previews of non-public lists.
                item.LoadAttributes( rockContext );
                if ( !service.ReadIsPublic( item ) )
                {
                    return ok;
                }

                var row = service.FindMatrixRow( item, matrixItemGuid );
                if ( row == null )
                {
                    return ok;
                }

                // URL/text come from the server-side row, never the client.
                row.LoadAttributes( rockContext );
                var url = row.GetAttributeValue( SystemGuids.LinkListGuids.MatrixAttributeKey.Url );
                var text = row.GetAttributeValue( SystemGuids.LinkListGuids.MatrixAttributeKey.LinkText );

                LinkListInteractionService.RecordClick(
                    item.Id,
                    item.Title,
                    row.Id,
                    url,
                    text,
                    userAgent: Request.Headers.UserAgent?.ToString(),
                    ipAddress: GetClientIp(),
                    personAliasId: null );
            }

            return ok;
        }

        private string GetClientIp()
        {
            try
            {
                var context = Request.Properties.TryGetValue( "MS_HttpContext", out var ctx )
                    ? ctx as System.Web.HttpContextWrapper
                    : null;
                return context?.Request?.UserHostAddress;
            }
            catch
            {
                return null;
            }
        }

        // ---------------------------------------------------------------------
        // CORS - origin reflection against the allowlist.
        // ---------------------------------------------------------------------

        // Force camelCase JSON so the public endpoint matches the Obsidian bag
        // contract the web component consumes (list.title / items / slug / ...).
        // Rock's v1 REST API otherwise negotiates PascalCase, which the component
        // reads as all-undefined -> it renders the empty default ("Link List",
        // no items). The in-Rock viewer is unaffected (it gets the bag from an
        // Obsidian block action, already camelCase).
        private static readonly JsonSerializerSettings CamelCaseSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private IHttpActionResult Respond( HttpStatusCode status, object payload )
        {
            var response = new HttpResponseMessage( status );
            if ( payload != null )
            {
                var json = JsonConvert.SerializeObject( payload, CamelCaseSettings );
                response.Content = new StringContent( json, Encoding.UTF8, "application/json" );
            }

            ApplyCorsHeaders( response );
            return ResponseMessage( response );
        }

        private void ApplyCorsHeaders( HttpResponseMessage response )
        {
            // Always vary so caches don't bleed responses across origins.
            response.Headers.Add( "Vary", "Origin" );

            var origin = Request.Headers.Contains( "Origin" )
                ? Request.Headers.GetValues( "Origin" ).FirstOrDefault()
                : null;

            if ( origin.IsNullOrWhiteSpace() )
            {
                return;
            }

            // Admin-managed Defined Type (cached) unioned with the hardcoded
            // fallback. HashSet uses an OrdinalIgnoreCase comparer.
            if ( LinkListService.GetAllowedOrigins().Contains( origin ) )
            {
                response.Headers.Add( "Access-Control-Allow-Origin", origin );
            }
        }
    }
}
