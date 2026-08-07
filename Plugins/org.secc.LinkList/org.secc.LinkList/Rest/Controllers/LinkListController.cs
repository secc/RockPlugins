// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
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
                // ROCK-8881: the per-IP rate limit lives inside RecordView, so
                // every caller is covered. Note it gates the analytics write
                // only - the bag above has already been built, so this endpoint
                // sheds no database work under a flood. See
                // LinkListRateLimitPolicy.
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
        /// POST-PARSE validation ALWAYS returns 200 with an empty body: beacons
        /// can't retry usefully, and a uniform response leaks nothing (no list
        /// enumeration signal). Invalid/spoofed payloads are silently dropped.
        /// Anti-spoof: the matrix row guid must belong to THIS list's matrix,
        /// and the recorded URL/text are read server-side - the client payload
        /// carries only the row guid.
        ///
        /// ROCK-8881: two abuse guards. A bounded read caps the body at
        /// <see cref="ClickPayload.MaxBodyLength"/> and is the only path that
        /// returns a non-200 status (413). A per-IP rate limit then governs the
        /// Click write - an over-budget IP still gets 200, the Click is just not
        /// recorded (accept-but-drop; a shared-NAT visitor is never blocked).
        /// The limit is peeked before the database work so an exhausted bucket
        /// costs nothing beyond the parse, and charged inside RecordClick.
        /// </summary>
        [HttpPost]
        [System.Web.Http.Route( "{idOrSlug}/click" )]
        public async Task<IHttpActionResult> PostClick( string idOrSlug )
        {
            if ( Request.Content == null )
            {
                return Respond( HttpStatusCode.OK, null );
            }

            // ROCK-8881: bounded read. A declared Content-Length over the cap
            // fails before a byte is read; an undeclared (chunked) body throws
            // once the copy exceeds the cap. Peak memory is the cap plus one
            // 4 KB copy chunk either way.
            //
            // This replaces a Content-Length gate that could not work on
            // WebHost: System.Web.Http.WebHost wraps the body in
            // SeekableBufferedRequestStream, which hardcodes CanSeek to true
            // while leaving Length to fall through to HttpRequest.ContentLength
            // (0 for chunked). So the header always computed to a value - and
            // for the attack case, always to 0, which sailed past the cap.
            //
            // Call this exactly once, and never mix in a separate
            // ReadAsStreamAsync consume: because CanSeek lies, a second
            // serialize skips StreamContent's already-read guard and reaches
            // SeekableBufferedRequestStream.Seek, which drains the whole body
            // unbounded. LoadIntoBufferAsync short-circuits on IsBuffered, so
            // one call plus any number of ReadAsStringAsync calls is safe.
            try
            {
                await Request.Content.LoadIntoBufferAsync( ClickPayload.MaxBodyLength + 1 );
            }
            catch ( HttpRequestException )
            {
                return Respond( HttpStatusCode.RequestEntityTooLarge, new { Message = "Payload too large." } );
            }

            var ok = Respond( HttpStatusCode.OK, null );

            var trimmed = LinkListService.NormalizeSlug( idOrSlug );
            if ( !trimmed.AsIntegerOrNull().HasValue
                && !trimmed.AsGuidOrNull().HasValue
                && !LinkListService.IsValidSlug( trimmed ) )
            {
                return ok;
            }

            var body = await Request.Content.ReadAsStringAsync();
            if ( !ClickPayload.TryParse( body, out var matrixItemGuid ) )
            {
                return ok;
            }

            // ROCK-8881 load shed: an over-budget click returns the same empty
            // 200 either way, so bail before opening a RockContext rather than
            // paying for ResolveItem + two LoadAttributes + ReadIsPublic +
            // FindMatrixRow first. This peek does NOT charge the budget -
            // RecordClick does - so requests that die in the gauntlet below
            // cost nothing, and slug probes cannot drain a bucket.
            var ipAddress = GetClientIp();
            if ( LinkListRateLimitPolicy.IsClickOverBudget( ipAddress ) )
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

                // ROCK-8881: the rate limit is charged inside RecordClick, so
                // every caller is covered. The beacon still answers 200; an
                // over-budget IP just isn't recorded.
                LinkListInteractionService.RecordClick(
                    item.Id,
                    item.Title,
                    row.Id,
                    url,
                    text,
                    userAgent: Request.Headers.UserAgent?.ToString(),
                    ipAddress: ipAddress,
                    personAliasId: null );
            }

            return ok;
        }

        // ROCK-8881: X-Forwarded-For-aware client IP. Web API exposes the
        // ambient request via the MS_HttpContext property; the server variables
        // go to ClientIpResolver, which is DNS-free and rejects anything that
        // does not parse as an address.
        //
        // Deliberately NOT WebRequestHelper.GetClientIpAddress: that wrapper
        // does a synchronous DNS lookup plus an ExceptionLog write whenever the
        // address is blank or "::1", both of which an anonymous caller can
        // trigger at will on these endpoints. See ClientIpResolver.
        //
        // NOTE: a well-formed XFF value is still trusted without a
        // trusted-proxy allowlist, so this remains client-spoofable. The rate
        // limiter that consumes it is best-effort by design and bounds its own
        // keyspace precisely because this input is untrusted; see
        // LinkListRateLimitPolicy for the accepted residual-risk discussion.
        private string GetClientIp()
        {
            try
            {
                var context = Request.Properties.TryGetValue( "MS_HttpContext", out var ctx )
                    ? ctx as System.Web.HttpContextWrapper
                    : null;
                var request = context?.Request;
                if ( request == null )
                {
                    return null;
                }

                return ClientIpResolver.Resolve(
                    request.ServerVariables["HTTP_X_FORWARDED_FOR"],
                    request.ServerVariables["REMOTE_ADDR"] );
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
