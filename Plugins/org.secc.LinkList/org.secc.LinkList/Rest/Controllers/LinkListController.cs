using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using org.secc.LinkList.Services;

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

            // Slug charset / length validation. Numeric ids and GUIDs are also
            // accepted because ResolveItem() tries those forms first.
            var trimmed = idOrSlug.Trim();
            if ( trimmed.Length > 200 )
            {
                return Respond( HttpStatusCode.NotFound, null );
            }
            if ( !trimmed.AsIntegerOrNull().HasValue
                && !trimmed.AsGuidOrNull().HasValue
                && !System.Text.RegularExpressions.Regex.IsMatch( trimmed, "^[a-zA-Z0-9-]+$" ) )
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

                return Respond( HttpStatusCode.OK, bag );
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
