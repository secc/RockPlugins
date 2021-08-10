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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using Microsoft.Owin.Security.OAuth;
using Rock;
using Rock.Model;
using Rock.Rest;


namespace org.secc.OAuth.Rest.Controllers
{
    /// <summary>
    /// REST API for OAuth
    /// </summary>
    public partial class OAuthController : ApiController, IHasCustomHttpRoutes
    {

        /// <summary>
        /// Add Custom route for handling the OAuth bearer token authentication.
        /// </summary>
        /// <param name="routes"></param>

        public void AddRoutes( HttpRouteCollection routes )
        {
            var config = new HttpConfiguration();
            // Web API configuration and services
            // Configure Web API to use only bearer token authentication.
            config.SuppressDefaultHostAuthentication();
            config.Filters.Add( new HostAuthenticationFilter( OAuthDefaults.AuthenticationType ) );

            // Web API routes
            config.MapHttpAttributeRoutes();
            routes.MapHttpRoute(
                name: "OAuth Services",
                routeTemplate: "api/oauth",
                defaults: new
                {
                    controller = "profile",
                    entityqualifier = RouteParameter.Optional,
                    entityqualifiervalue = RouteParameter.Optional
                } );

        }

        /// <summary>
        /// Get a person's profile (person) username
        /// </summary>
        /// <returns>A Profile object</returns>
        [System.Web.Http.Route( "api/oauth/userlogin" )]
        public HttpResponseMessage GetUserLogin()
        {
            try
            {
                var currentUser = UserLoginService.GetCurrentUser();

                if ( currentUser == null )
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.NotFound );
                }

                return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, new Dictionary<string, string> { { "Value", currentUser.UserName } } );

            }
            catch
            {
                return ControllerContext.Request.CreateResponse( HttpStatusCode.InternalServerError, "Internal Server Error" );
            }
        }

        /// <summary>
        /// Get a person's profile (person) information from Rock
        /// </summary>
        /// <returns>A Profile object</returns>
        [System.Web.Http.Route( "api/oauth/profile" )]
        public HttpResponseMessage GetProfile()
        {
            try
            {
                ClaimsIdentity id = ( ClaimsIdentity ) User.Identity;

                if ( id == null || id.Claims.Where( c => c.Type == "urn:oauth:scope" ).Where( c => c.Value.ToLower() == "profile" ).Count() == 0 )
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden, "Forbidden" );
                }
                var currentUser = UserLoginService.GetCurrentUser();

                if ( currentUser == null )
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.NotFound );
                }

                return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, new Profile( currentUser.Person ) );

            }
            catch
            {
                return ControllerContext.Request.CreateResponse( HttpStatusCode.InternalServerError, "Internal Server Error" );
            }
        }

        /// <summary>
        /// Get a person's family information from Rock
        /// </summary>
        /// <returns>A list of family members.</returns>
        [System.Web.Http.Route( "api/oauth/family" )]
        public HttpResponseMessage GetFamily()
        {
            try
            {
                ClaimsIdentity id = ( ClaimsIdentity ) User.Identity;

                if ( id == null || id.Claims.Where( c => c.Type == "urn:oauth:scope" ).Where( c => c.Value.ToLower() == "family" ).Count() == 0 )
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden, "Forbidden" );
                }
                var currentUser = UserLoginService.GetCurrentUser();

                if ( currentUser == null )
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.NotFound );
                }

                List<FamilyMemberProfile> familyMembers = new List<FamilyMemberProfile>();
                // Add the current person

                FamilyMemberProfile familyMember = new FamilyMemberProfile();
                foreach ( GroupMember member in currentUser.Person.GetFamilyMembers( true ) )
                {
                    familyMember = new FamilyMemberProfile();
                    familyMember.FamilyRole = member.GroupRole.Name;
                    familyMember.FullName = member.Person.FullName;
                    familyMember.PersonId = member.Person.Id;
                    familyMember.Profile = new OAuthController.Profile( member.Person );
                    familyMembers.Add( familyMember );
                }

                return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, familyMembers );

            }
            catch
            {
                return ControllerContext.Request.CreateResponse( HttpStatusCode.InternalServerError, "Internal Server Error" );
            }
        }


        /// <summary>
        /// A profile object for describing a person in Rock
        /// </summary>
        public class Profile
        {
            public int? PersonId { get; set; }
            public string FirstName { get; set; }
            public string NickName { get; set; }
            public string LastName { get; set; }
            public DateTime? Birthdate { get; set; }
            public string Gender { get; set; }
            public string EmailAddress { get; set; }
            public List<int> PreviousPersonIDs { get; set; }

            public Profile() { }

            public Profile( Person p )
            {

                PersonId = p.Id;
                FirstName = p.FirstName;
                NickName = p.NickName;
                LastName = p.LastName;
                Gender = p.Gender.ToString();
                Birthdate = p.BirthDate;
                EmailAddress = p.Email;
                PreviousPersonIDs = p.Aliases.AsQueryable().Where( pa => pa.Id != pa.Person.PrimaryAliasId ).Select( pa => pa.AliasPersonId.Value ).ToList();
            }
        }

        /// <summary>
        /// A FamilyMember object for describing a person's family member.
        /// </summary>
        public class FamilyMemberProfile
        {
            public int PersonId { get; set; }
            public string FamilyRole { get; set; }
            public string FullName { get; set; }
            public OAuthController.Profile Profile { get; set; }
        }

    }
}