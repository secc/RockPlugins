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
using System.Net;

namespace org.secc.LinkList.Utility
{
    /// <summary>
    /// ROCK-8881: resolves the client IP for the anonymous LinkList endpoints.
    /// Pure (two strings in, one string out), so it is unit-testable without an
    /// HttpContext.
    ///
    /// It makes no DNS lookups (unlike Rock's
    /// <c>WebRequestHelper.GetClientIpAddress</c>, which falls back to
    /// <c>Dns.GetHostEntry</c> for a blank or <c>::1</c> address) and mirrors
    /// the formula in Rock's <c>Net/ClientInformation.cs</c>. Only values that
    /// parse as an IP address are returned. The value is stored in
    /// <c>Interaction.IpAddress</c> (45 characters) and later read by Rock's
    /// <c>PopulateInteractionSessionData</c> job.
    ///
    /// The value can come from the client-supplied <c>X-Forwarded-For</c>
    /// header, so use it only for analytics and rate limiting, not for access
    /// decisions.
    /// </summary>
    public static class ClientIpResolver
    {
        /// <summary>
        /// The client address from <paramref name="forwardedForHeader"/> if it
        /// yields a parseable IP, otherwise <paramref name="remoteAddress"/> if
        /// that does, otherwise null. A null result suppresses nothing: the
        /// limiter treats a blank key as always-allowed.
        /// </summary>
        public static string Resolve( string forwardedForHeader, string remoteAddress )
        {
            // Rock's parser is public, pure, and already handles the four wire
            // forms it documents (bare IPv4/IPv6, IPv4 with port, bracketed
            // IPv6 with port) plus the comma-delimited CDN/web-farm proxy
            // chain. Only GetClientIpAddress - the wrapper - touches DNS.
            var forwarded = Rock.Utility.WebRequestHelper.GetXForwardedForIpAddress( forwardedForHeader );

            return AsIpAddress( forwarded ) ?? AsIpAddress( remoteAddress );
        }

        private static string AsIpAddress( string value )
        {
            if ( string.IsNullOrWhiteSpace( value ) )
            {
                return null;
            }

            var trimmed = value.Trim();
            return IPAddress.TryParse( trimmed, out _ ) ? trimmed : null;
        }
    }
}
