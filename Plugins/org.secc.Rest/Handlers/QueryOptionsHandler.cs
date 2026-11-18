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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace org.secc.Rest.Handlers
{
    /// <summary>
    /// Web API message handler that answers 400 Bad Request when the OData $expand, $select, $filter or $orderby
    /// query option of a request refers to a property named Users. It also answers 400 when a request on one of the
    /// GroupMembers controller's convention or OData routes (such as api/GroupMembers, api/GroupMembers/{id} and
    /// api/GroupMembers({key})) has an IncludeDeceased query parameter; the controller's attribute-routed actions
    /// (KnownRelationship, GetGroupPlacementGroupMembers) are not affected. Every other request continues unchanged.
    /// </summary>
    public class QueryOptionsHandler : DelegatingHandler
    {
        /// <summary>
        /// The message returned with the 400 response.
        /// </summary>
        internal const string InvalidQueryMessage = "The query specified in the URI is not valid.";

        private const string UsersProperty = "Users";

        private const string FilterOption = "$filter";

        private static readonly string[] _checkedOptions = { "$expand", "$select", FilterOption, "$orderby" };

        private const string GroupMembersController = "GroupMembers";

        private const string IncludeDeceasedParameter = "IncludeDeceased";

        /// <summary>
        /// Returns the 400 response for a request whose query options refer to Users, or a GroupMembers request with an
        /// IncludeDeceased parameter; otherwise passes the request on.
        /// </summary>
        protected override Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken )
        {
            if ( IsGroupMembersIncludeDeceased( GetRouteControllerName( request ), request ) )
            {
                return Task.FromResult( request.CreateErrorResponse( HttpStatusCode.BadRequest, InvalidQueryMessage ) );
            }

            IEnumerable<KeyValuePair<string, string>> queryPairs;
            try
            {
                // The same parse OData uses for its query options, so both read the same keys and values.
                queryPairs = request.GetQueryNameValuePairs();
            }
            catch ( Exception )
            {
                // A query string Web API can't parse is left to the rest of the pipeline, which parses it the same way.
                return base.SendAsync( request, cancellationToken );
            }

            if ( ReferencesUsers( queryPairs ) )
            {
                return Task.FromResult( request.CreateErrorResponse( HttpStatusCode.BadRequest, InvalidQueryMessage ) );
            }

            return base.SendAsync( request, cancellationToken );
        }

        /// <summary>
        /// The controller value of the route matched for the request (set by routing before the message handlers run),
        /// or null when there is none.
        /// </summary>
        private static string GetRouteControllerName( HttpRequestMessage request )
        {
            var routeData = request.GetRouteData();
            object controller = null;
            return routeData?.Values != null && routeData.Values.TryGetValue( "controller", out controller ) ? controller as string : null;
        }

        /// <summary>
        /// Returns true when the controller name is GroupMembers (ignoring case) and the query string has an
        /// IncludeDeceased key with any value after '=' (including an empty one), read the way the GroupMembers
        /// controller reads it.
        /// </summary>
        internal static bool IsGroupMembersIncludeDeceased( string controllerName, HttpRequestMessage request )
        {
            if ( !string.Equals( controllerName, GroupMembersController, StringComparison.OrdinalIgnoreCase ) )
            {
                return false;
            }

            try
            {
                return HttpUtility.ParseQueryString( request.RequestUri.Query ).Get( IncludeDeceasedParameter ) != null;
            }
            catch ( Exception )
            {
                // Reached only when the request URI's query can't be read, such as for a relative URI.
                return false;
            }
        }

        /// <summary>
        /// Returns true when any $expand, $select, $filter or $orderby pair refers to Users. Each pair is checked on its own.
        /// </summary>
        internal static bool ReferencesUsers( IEnumerable<KeyValuePair<string, string>> queryPairs )
        {
            foreach ( var pair in queryPairs )
            {
                if ( pair.Key == null || pair.Value == null
                    || !_checkedOptions.Contains( pair.Key, StringComparer.OrdinalIgnoreCase ) )
                {
                    continue;
                }

                var skipStringLiterals = string.Equals( pair.Key, FilterOption, StringComparison.OrdinalIgnoreCase );
                if ( ContainsUsersWord( pair.Value, skipStringLiterals ) )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true when a run of ASCII letters in the value equals Users, ignoring case. With skipStringLiterals,
        /// text between single quotes is not checked (a doubled quote inside a literal closes and reopens it).
        /// </summary>
        internal static bool ContainsUsersWord( string value, bool skipStringLiterals )
        {
            var inLiteral = false;
            var wordStart = -1;

            for ( var i = 0; i <= value.Length; i++ )
            {
                var c = i < value.Length ? value[i] : '\0';
                if ( !inLiteral && ( ( c >= 'A' && c <= 'Z' ) || ( c >= 'a' && c <= 'z' ) ) )
                {
                    if ( wordStart < 0 )
                    {
                        wordStart = i;
                    }

                    continue;
                }

                if ( wordStart >= 0 )
                {
                    if ( i - wordStart == UsersProperty.Length
                        && string.Compare( value, wordStart, UsersProperty, 0, UsersProperty.Length, StringComparison.OrdinalIgnoreCase ) == 0 )
                    {
                        return true;
                    }

                    wordStart = -1;
                }

                if ( skipStringLiterals && c == '\'' )
                {
                    inLiteral = !inLiteral;
                }
            }

            return false;
        }
    }
}
