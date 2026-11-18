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
using System.Web;
using Rock.Utility;

namespace org.secc.Authentication
{
    /// <summary>
    /// The caller address that per-caller limits are counted against.
    /// </summary>
    public static class CallerAddress
    {
        /// <summary>
        /// Returns the address in the last X-Forwarded-For entry, with its port and IPv6 brackets removed; if none
        /// is left (missing, blank, only a port or brackets), the connecting address (REMOTE_ADDR); or "unknown"
        /// when there is neither or no request.
        /// </summary>
        /// <param name="request">The request, or null.</param>
        /// <returns></returns>
        public static string ForRequest( HttpRequestBase request )
        {
            if ( request == null )
            {
                return "unknown";
            }

            var entries = ( request.Headers["X-Forwarded-For"] ?? string.Empty ).Split( ',' );
            var address = WebRequestHelper.GetXForwardedForIpAddress( entries[entries.Length - 1].Trim() );
            if ( string.IsNullOrWhiteSpace( address ) )
            {
                address = ( request.ServerVariables["REMOTE_ADDR"] ?? string.Empty ).Trim();
            }

            return string.IsNullOrWhiteSpace( address ) ? "unknown" : address;
        }
    }
}
