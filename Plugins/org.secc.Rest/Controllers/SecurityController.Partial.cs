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
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Rock.Model;
using Rock.Rest;
using System.Web;
using System.Collections.Generic;
using org.secc.Rest.Handlers;


namespace org.secc.Rest.Controllers
{
    /// <summary>
    /// TaggedItems REST API
    /// </summary>
    public partial class SecurityController : ApiController, IHasCustomRoutes
    {
        /// <summary>
        /// Add Custom route for flushing cached attributes
        /// </summary>
        /// <param name="routes"></param>
        public void AddRoutes( System.Web.Routing.RouteCollection routes )
        {
            routes.MapHttpRoute(
                name: "security",
                routeTemplate: "api/People/{action}/{param}",
                defaults: new
                {
                    controller = "security",
                    param = RouteParameter.Optional
                } ).RouteHandler = new SessionRouteHandler();
            routes.MapHttpRoute(
                name: "securityNoParam",
                routeTemplate: "api/People/{action}",
                defaults: new
                {
                    controller = "security",
                    param = RouteParameter.Optional
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
        public HttpResponseMessage Post( string phone )
        {

            return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, phone );
        }

        [HttpGet()]
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

        [HttpGet()]
        public HttpResponseMessage CurrentUser( string param )
        {
            UserLogin currentUser;
            try
            {
                var encryptedTicket = System.Web.Security.FormsAuthentication.Decrypt( param );
                if ( encryptedTicket != null && encryptedTicket.Expired == false )
                {
                    currentUser = new UserLoginService( new Rock.Data.RockContext() ).GetByUserName( encryptedTicket.Name );
                    return PersonReport( currentUser );

                }
                return ControllerContext.Request.CreateResponse( HttpStatusCode.NotFound );
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