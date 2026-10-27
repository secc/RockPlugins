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
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;
using org.secc.Rest.Handlers;
using Rock.Utility;
using Xunit;

namespace org.secc.Rest.Tests
{
    /// <summary>
    /// ROCK-9331: QueryOptionsStartup.OnStartup adds QueryOptionsHandler to the Web API configuration. Requests are sent
    /// as raw URLs through an HttpServer built on that configuration, so Web API's own query parsing runs first.
    /// </summary>
    public class QueryOptionsStartupTests
    {
        private const string Url = "http://localhost/api/People";

        private const string InvalidQueryMessage = "The query specified in the URI is not valid.";

        /// <summary>
        /// Replaces the Web API configuration and the error log.
        /// </summary>
        private class TestStartup : QueryOptionsStartup
        {
            private readonly Func<HttpConfiguration> _getConfiguration;

            public readonly List<Exception> Logged = new List<Exception>();

            public TestStartup( Func<HttpConfiguration> getConfiguration )
            {
                _getConfiguration = getConfiguration;
            }

            protected override HttpConfiguration GetHttpConfiguration()
            {
                return _getConfiguration();
            }

            protected override void LogException( Exception ex )
            {
                Logged.Add( ex );
            }

            public HttpConfiguration DefaultHttpConfiguration()
            {
                return base.GetHttpConfiguration();
            }
        }

        /// <summary>
        /// A message handler that was in the configuration before OnStartup ran.
        /// </summary>
        private class ExistingHandler : DelegatingHandler
        {
            public int Calls;

            protected override Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken )
            {
                Calls++;
                return base.SendAsync( request, cancellationToken );
            }
        }

        /// <summary>
        /// Stands in for routing and the controllers; answers 200.
        /// </summary>
        private class StubDispatcher : HttpMessageHandler
        {
            public int Calls;

            protected override Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken )
            {
                Calls++;
                return Task.FromResult( new HttpResponseMessage( HttpStatusCode.OK ) { Content = new StringContent( "stub" ) } );
            }
        }

        private class Result
        {
            public HttpStatusCode Status;
            public string Body;
            public int ExistingHandlerCalls;
            public int DispatcherCalls;
        }

        private static Result Send( string query )
        {
            var configuration = new HttpConfiguration();
            var existing = new ExistingHandler();
            configuration.MessageHandlers.Add( existing );

            new TestStartup( () => configuration ).OnStartup();

            var dispatcher = new StubDispatcher();
            using ( var server = new HttpServer( configuration, dispatcher ) )
            using ( var client = new HttpClient( server, false ) )
            using ( var response = client.GetAsync( Url + query ).GetAwaiter().GetResult() )
            {
                return new Result
                {
                    Status = response.StatusCode,
                    Body = response.Content == null ? null : response.Content.ReadAsStringAsync().GetAwaiter().GetResult(),
                    ExistingHandlerCalls = existing.Calls,
                    DispatcherCalls = dispatcher.Calls
                };
            }
        }

        [Theory]
        [InlineData( "?$expand=Users" )]
        [InlineData( "?$top=50&$expand=Users" )]
        [InlineData( "?%24expand=Users" )]
        [InlineData( "?$expand[]=Users" )]
        [InlineData( "?%24expand%5B%5D=Users" )]
        [InlineData( "?$filter[]=Users/any()" )]
        [InlineData( "?$expand=%55sers" )]
        [InlineData( "?$expand=+Users" )]
        [InlineData( "?$expand=Users+" )]
        [InlineData( "?$expand=%09Users" )]
        [InlineData( "?$expand=Aliases%2CUsers" )]
        [InlineData( "?$expand=Rock.Model.Person/Users" )]
        [InlineData( "?$expand=Person/Users" )]
        [InlineData( "?$expand=Users&$expand=Aliases" )]
        [InlineData( "?$expand=Aliases&$expand=Users" )]
        [InlineData( "?$filter='&$filter=Users/any()" )]
        [InlineData( "?$select=Users/Id" )]
        [InlineData( "?$orderby=Users/Count" )]
        [InlineData( "?$filter=Users/any(u:u/UserName%20eq%20'a')" )]
        public void QueryReferringToUsers_IsAnswered400BeforeOtherHandlers( string query )
        {
            var result = Send( query );

            Assert.Equal( HttpStatusCode.BadRequest, result.Status );
            Assert.Contains( InvalidQueryMessage, result.Body );
            Assert.Equal( 0, result.ExistingHandlerCalls );
            Assert.Equal( 0, result.DispatcherCalls );
        }

        [Theory]
        [InlineData( "" )]
        [InlineData( "?" )]
        [InlineData( "?$filter=LastName%20eq%20'Users'" )]
        [InlineData( "?$filter=Name%20eq%20'O''Users'" )]
        [InlineData( "?$expand=PhoneNumbers" )]
        [InlineData( "?$expand=Members/Person&$select=Id,UserName" )]
        [InlineData( "?name=Users" )]
        [InlineData( "?Users=1" )]
        [InlineData( "?expand=Users" )]
        [InlineData( "?$top=50&$skip=0" )]
        public void OtherQuery_ReachesThePipeline( string query )
        {
            var result = Send( query );

            Assert.Equal( HttpStatusCode.OK, result.Status );
            Assert.Equal( "stub", result.Body );
            Assert.Equal( 1, result.ExistingHandlerCalls );
            Assert.Equal( 1, result.DispatcherCalls );
        }

        [Theory]
        [InlineData( "?a[b=1" )]
        [InlineData( "?$expand=Users&a[b=1" )]
        public void QueryWebApiCannotParse_ReachesThePipelineUnchanged( string query )
        {
            // Web API's query parse, which OData also uses for its query options, fails on this query string.
            var parseError = Record.Exception( () => new HttpRequestMessage( HttpMethod.Get, Url + query ).GetQueryNameValuePairs() );
            Assert.IsAssignableFrom<ArgumentException>( parseError );

            var result = Send( query );

            Assert.Equal( HttpStatusCode.OK, result.Status );
            Assert.Equal( 1, result.ExistingHandlerCalls );
            Assert.Equal( 1, result.DispatcherCalls );
        }

        /// <summary>
        /// Test copy of Rock's ValidControllerNameConstraint: a controller value must start with a letter.
        /// </summary>
        private class ValidControllerNameConstraint : IHttpRouteConstraint
        {
            public bool Match( HttpRequestMessage request, IHttpRoute route, string parameterName, IDictionary<string, object> values, HttpRouteDirection routeDirection )
            {
                object controller;
                return !values.TryGetValue( "controller", out controller ) || !( controller is string name ) || name.Length == 0 || char.IsLetter( name[0] );
            }
        }

        /// <summary>
        /// The Rock.Rest WebApiConfig routes that can match a GroupMembers request, in registration order. The first one
        /// stands in for the attribute routes such as api/GroupMembers/KnownRelationship, whose route data has no
        /// controller value.
        /// </summary>
        private static HttpRouteCollection RockRoutes()
        {
            var routes = new HttpRouteCollection();
            var getOrOptions = new HttpMethodConstraint( HttpMethod.Get, HttpMethod.Options );
            routes.MapHttpRoute( "KnownRelationship", "api/GroupMembers/KnownRelationship" );
            routes.MapHttpRoute( "DefaultApiGetById", "api/{controller}/{id}", new { action = "GetById" },
                new { httpMethod = getOrOptions, controllerName = new ValidControllerNameConstraint() } );
            routes.MapHttpRoute( "DefaultApiGetFunction", "api/{controller}({key})", new { action = "GET" },
                new { httpMethod = getOrOptions, controllerName = new ValidControllerNameConstraint() } );
            routes.MapHttpRoute( "DefaultApiGetList", "api/{controller}", new { action = "GET" },
                new { httpMethod = getOrOptions, controllerName = new ValidControllerNameConstraint() } );
            routes.MapHttpRoute( "DefaultApiPost", "api/{controller}/{id}", new { action = "POST", id = RouteParameter.Optional },
                new { httpMethod = new HttpMethodConstraint( HttpMethod.Post, HttpMethod.Options ) } );
            routes.MapHttpRoute( "DefaultApiDelete", "api/{controller}/{id}", new { action = "DELETE", id = RouteParameter.Optional },
                new { httpMethod = new HttpMethodConstraint( HttpMethod.Delete, HttpMethod.Options ), controllerName = new ValidControllerNameConstraint() } );
            return routes;
        }

        /// <summary>
        /// Sends a request the way the web host does: the matched route's data (from RockRoutes, unless given) is set on
        /// the request before it goes through the HttpServer whose configuration OnStartup set up.
        /// </summary>
        private static Result SendRouted( string method, string pathAndQuery, IHttpRouteData routeData = null )
        {
            var configuration = new HttpConfiguration();
            var existing = new ExistingHandler();
            configuration.MessageHandlers.Add( existing );

            new TestStartup( () => configuration ).OnStartup();

            var dispatcher = new StubDispatcher();
            using ( var server = new HttpServer( configuration, dispatcher ) )
            using ( var client = new HttpClient( server, false ) )
            using ( var request = new HttpRequestMessage( new HttpMethod( method ), "http://localhost/" + pathAndQuery ) )
            {
                routeData = routeData ?? RockRoutes().GetRouteData( request );
                if ( routeData != null )
                {
                    request.SetRouteData( routeData );
                }

                using ( var response = client.SendAsync( request ).GetAwaiter().GetResult() )
                {
                    return new Result
                    {
                        Status = response.StatusCode,
                        Body = response.Content == null ? null : response.Content.ReadAsStringAsync().GetAwaiter().GetResult(),
                        ExistingHandlerCalls = existing.Calls,
                        DispatcherCalls = dispatcher.Calls
                    };
                }
            }
        }

        private static void AssertAnswered400( Result result )
        {
            Assert.Equal( HttpStatusCode.BadRequest, result.Status );
            Assert.Equal( "{\"Message\":\"" + InvalidQueryMessage + "\"}", result.Body );
            Assert.Equal( 0, result.ExistingHandlerCalls );
            Assert.Equal( 0, result.DispatcherCalls );
        }

        [Theory]
        [InlineData( "GET", "api/GroupMembers?IncludeDeceased=true" )]
        [InlineData( "GET", "API/groupmembers/?includedeceased=TRUE" )]
        [InlineData( "GET", "api/Group%4Dembers?IncludeDeceased=1" )]
        [InlineData( "GET", "api/GroupMembers?$top=50&IncludeDeceased=false" )]
        [InlineData( "GET", "api/GroupMembers?IncludeDeceased=true&IncludeDeceased=false" )]
        [InlineData( "GET", "api/GroupMembers?Include%44eceased=1" )]
        [InlineData( "GET", "api/GroupMembers?IncludeDeceased=1&a[b=1" )]
        [InlineData( "GET", "api/GroupMembers(5)?IncludeDeceased=true" )]
        [InlineData( "GET", "api/GroupMembers/5?IncludeDeceased=true" )]
        [InlineData( "POST", "api/GroupMembers?IncludeDeceased=true" )]
        [InlineData( "DELETE", "api/GroupMembers/5?IncludeDeceased=true" )]
        public void GroupMembersRouteWithIncludeDeceased_IsAnswered400BeforeOtherHandlers( string method, string pathAndQuery )
        {
            AssertAnswered400( SendRouted( method, pathAndQuery ) );
        }

        [Fact]
        public void GroupMembersODataRouteWithIncludeDeceased_IsAnswered400BeforeOtherHandlers()
        {
            // The route data the OData route constraint produces for api/()GroupMembers.
            var routeData = new HttpRouteData( new HttpRoute( "api/{*odataPath}" ),
                new HttpRouteValueDictionary { { "odataPath", "()GroupMembers" }, { "controller", "GroupMembers" } } );

            AssertAnswered400( SendRouted( "GET", "api/()GroupMembers?IncludeDeceased=true", routeData ) );
        }

        [Theory]
        [InlineData( "api/Groups?$expand=Users" )]
        [InlineData( "api/PersonAlias?$expand=Person/Users" )]
        [InlineData( "api/GroupMembers?$expand=Person/Users" )]
        [InlineData( "api/org.secc/People/CurrentUser?$expand=Users" )]
        public void QueryReferringToUsers_OnOtherPaths_IsAnswered400BeforeOtherHandlers( string pathAndQuery )
        {
            AssertAnswered400( SendRouted( "GET", pathAndQuery ) );
        }

        [Theory]
        [InlineData( "POST", "api/People?$expand=Users" )]
        [InlineData( "PUT", "api/People/5?$expand=Users" )]
        [InlineData( "PATCH", "api/People/5?$select=Users" )]
        [InlineData( "DELETE", "api/People/5?$filter=Users/any()" )]
        public void QueryReferringToUsers_WithOtherMethods_IsAnswered400BeforeOtherHandlers( string method, string pathAndQuery )
        {
            AssertAnswered400( SendRouted( method, pathAndQuery ) );
        }

        [Theory]
        [InlineData( "GET", "api/People?includeDeceased=true" )]
        [InlineData( "GET", "api/People/Search?name=x&includeDeceased=true" )]
        [InlineData( "GET", "api/GroupMembers" )]
        [InlineData( "GET", "api/GroupMembers?$top=50" )]
        [InlineData( "GET", "api/GroupMembers?IncludeDeceased" )]
        [InlineData( "GET", "api/GroupMembers?a=1?IncludeDeceased=1" )]
        [InlineData( "POST", "api/GroupMembers" )]
        [InlineData( "GET", "api/GroupMembers/KnownRelationship?personId=1&relationshipRoleId=5" )]
        [InlineData( "GET", "api/GroupMembers/KnownRelationship?personId=1&relationshipRoleId=5&IncludeDeceased=true" )]
        [InlineData( "GET", "api/Groups?IncludeDeceased=true" )]
        [InlineData( "GET", "api/GroupMemberHistoricals?IncludeDeceased=true" )]
        [InlineData( "GET", "api/GroupAppGroupMembers?IncludeDeceased=true" )]
        public void OtherRoutedRequest_ReachesThePipeline( string method, string pathAndQuery )
        {
            var result = SendRouted( method, pathAndQuery );

            Assert.Equal( HttpStatusCode.OK, result.Status );
            Assert.Equal( "stub", result.Body );
            Assert.Equal( 1, result.ExistingHandlerCalls );
            Assert.Equal( 1, result.DispatcherCalls );
        }

        [Fact]
        public void StartupOrder_IsTheLowest()
        {
            Assert.Equal( int.MinValue, new QueryOptionsStartup().StartupOrder );
        }

        [Fact]
        public void RockStartupDiscovery_FindsAndCreatesTheStartup()
        {
            // Rock's RunStartups finds IRockStartup types this way and creates them with Activator.CreateInstance.
            var types = Rock.Reflection.SearchAssembly( typeof( QueryOptionsStartup ).Assembly, typeof( IRockStartup ) );
            Assert.Contains( typeof( QueryOptionsStartup ), types.Values );
            Assert.IsType<QueryOptionsStartup>( Activator.CreateInstance( typeof( QueryOptionsStartup ) ) as IRockStartup );
        }

        [Fact]
        public void OnStartup_InsertsTheHandlerFirst()
        {
            var configuration = new HttpConfiguration();
            var existing = new ExistingHandler();
            configuration.MessageHandlers.Add( existing );
            var startup = new TestStartup( () => configuration );

            startup.OnStartup();

            Assert.Equal( 2, configuration.MessageHandlers.Count );
            Assert.IsType<QueryOptionsHandler>( configuration.MessageHandlers[0] );
            Assert.Same( existing, configuration.MessageHandlers[1] );
            Assert.Empty( startup.Logged );
        }

        [Fact]
        public void OnStartup_LogsAndDoesNotThrow_WhenTheConfigurationIsUnavailable()
        {
            var error = new InvalidOperationException( "configuration unavailable" );
            var startup = new TestStartup( () => throw error );

            var thrown = Record.Exception( () => startup.OnStartup() );

            Assert.Null( thrown );
            Assert.Same( error, Assert.Single( startup.Logged ) );
        }

        [Fact]
        public void DefaultConfiguration_IsTheGlobalWebApiConfiguration()
        {
            // GlobalConfiguration lives in System.Web.Http.WebHost, which this project does not reference directly.
            var globalConfiguration = Type.GetType( "System.Web.Http.GlobalConfiguration, System.Web.Http.WebHost", true )
                .GetProperty( "Configuration" )
                .GetValue( null );

            Assert.Same( globalConfiguration, new TestStartup( () => null ).DefaultHttpConfiguration() );
        }
    }
}
