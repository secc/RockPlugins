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
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using org.secc.Rest.Handlers;
using Rock.Model;
using Rock.Rest;

namespace org.secc.Rest.Controllers
{
    /// <summary>
    /// TaggedItems REST API
    /// </summary>
    [Rock.SystemGuid.RestControllerGuid( "23B0A764-52A6-4F32-A4C1-F0B5445D3504" )]
    public partial class SecurityController : ApiController, IHasCustomHttpRoutes
    {
        /// <summary>
        /// Add Custom route for flushing cached attributes
        /// </summary>
        /// <param name="routes"></param>
        public void AddRoutes( HttpRouteCollection routes )
        {
            RouteTable.Routes.MapHttpRoute(
                name: "securityNoParam",
                routeTemplate: "api/org.secc/People/{action}",
                defaults: new
                {
                    // Must match the controller class name's casing exactly — Rock 16's
                    // controller-mapping lookup during REST controller registration is
                    // case-sensitive (see FamilyCheckinController.AddRoutes).
                    controller = "Security"
                } ).RouteHandler = new SessionRouteHandler();
        }

        /// <summary>
        /// Posts the specified entity type identifier.
        /// </summary>
        /// <param name="entityTypeId">The entity type identifier.</param>
        /// <param name="ownerId">The owner identifier.</param>
        /// <param name="entityGuid">The entity unique identifier.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        [Rock.SystemGuid.RestActionGuid( "D3D6E170-CD7E-43BC-B277-6D0CDA0C20D3" )]
        public HttpResponseMessage Post( string phone )
        {

            return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, phone );
        }

        [HttpGet()]
        [Rock.SystemGuid.RestActionGuid( "7080335B-8BD0-4A04-8C16-F4CA54A87C5B" )]
        public HttpResponseMessage CurrentUser()
        {
            try
            {
                var currentUser = UserLoginService.GetCurrentUser();
                return PersonReport( currentUser );


            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex, HttpContext.Current );
                return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden, "Forbidden" );
            }
        }

        private HttpResponseMessage PersonReport( UserLogin currentUser )
        {
            if ( currentUser != null )
            {
                var person = currentUser.Person;
                if ( person != null )
                {
                    var campus = person.GetCampus();
                    Dictionary<string, object> output = new Dictionary<string, object>() {
                                { "Active", true },
                                { "FullName", person.FullName },
                                { "NickName", person.NickName },
                                { "LastName", person.LastName },
                                { "CampusId", campus!=null ? campus.Id : 1 },
                                { "Campus",  campus!=null ? campus.Name : "Blankenbaker" },
                                { "Gender", person.Gender.ToString() }
                            };
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, output );
                }
            }
            return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, new Dictionary<string, object>() { { "Active", false } } );
        }
    }
}