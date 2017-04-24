using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Rock.Model;
using Rock.Rest;
using System.Web;
using System.Collections.Generic;
using org.secc.Security.Rest.Handlers;


namespace org.secc.Security.Rest.Controllers
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
                routeTemplate: "api/org.secc/security/{action}/{param}",
                defaults: new
                {
                    controller = "security",
                    entityqualifier = RouteParameter.Optional,
                    entityqualifiervalue = RouteParameter.Optional
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
                if ( currentUser != null )
                {
                    var person = currentUser.Person;
                    Dictionary<string, object> output = new Dictionary<string, object>() {
                                { "Active", true },
                                { "FullName", person.FullName },
                                {"NickName", person.NickName },
                                { "LastName",person.NickName}
                            };
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, output );
                }
                return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, new Dictionary<string, object>() { { "Active", false } } );

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

                    if ( currentUser != null )
                    {
                        var person = currentUser.Person;
                        Dictionary<string, object> output = new Dictionary<string, object>() {
                                { "Active", true },
                                { "FullName", person.FullName },
                                {"NickName", person.NickName },
                                { "LastName",person.NickName}
                            };
                        return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, output );
                    }
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.NotFound );
                }
                return ControllerContext.Request.CreateResponse( HttpStatusCode.NotFound );
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex, HttpContext.Current );
                return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden, "Forbidden" );
            }
        }
    }
}