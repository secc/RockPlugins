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
using System;
using System.Linq;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.OAuth.Utilities
{
    /// <summary>
    /// Helper for validating user-supplied redirect ("return") URLs to prevent
    /// open-redirect attacks (see ROCK-8763).
    /// </summary>
    public static class RedirectUrlHelper
    {
        /// <summary>
        /// Returns <c>true</c> when the URL is safe to use as a redirect target.
        /// </summary>
        /// <remarks>
        /// Accepted:
        ///   - Site-relative paths ("/foo")
        ///   - Application-relative paths ("~/foo")
        ///   - Absolute http/https URLs whose host is on the allow-list
        ///     (Rock SiteDomain rows, the OAuthAllowedRedirectDomains global attribute,
        ///     or the current request host).
        ///
        /// Rejected:
        ///   - Strings containing control characters (tabs, newlines, etc.)
        ///   - Protocol-relative ("//evil.com"), backslash variants ("/\evil.com")
        ///   - Absolute URLs with non-http/https scheme (e.g. javascript:, data:)
        ///   - Absolute URLs whose host is not on the allow-list
        ///
        /// NOTE: The caller must URL-decode the value before passing it here so that
        /// encoded control characters (%09, %0A) are expanded before the check runs.
        ///
        /// Admin deploy note: absolute URLs that resolve to Webflow or other non-Rock
        /// hosts must be added to the OAuthAllowedRedirectDomains global attribute
        /// (comma- or newline-separated host names, without scheme).
        /// </remarks>
        /// <param name="url">Candidate redirect URL (already URL-decoded).</param>
        /// <param name="requestUrl">Current request URL, used as an implicit allow-list entry. May be null.</param>
        public static bool IsSafeRedirectUrl( string url, Uri requestUrl )
        {
            if ( string.IsNullOrEmpty( url ) )
            {
                return false;
            }

            // Reject control characters (catches tab %09, newline %0A, DEL, etc.).
            // Must run before any positional check so encoded variants can't sneak through.
            for ( int i = 0; i < url.Length; i++ )
            {
                if ( url[i] < ' ' || url[i] == '' )
                {
                    return false;
                }
            }

            // Reject leading/trailing whitespace — browsers strip it from Location headers,
            // which can turn "/ /evil.com" into "//evil.com" after the check passes.
            if ( url.Length != url.Trim().Length )
            {
                return false;
            }

            // Site-relative: "/" or "/path", but NOT "//host" or "/\host"
            // (browsers treat "//" and "/\" as protocol-relative — off-site navigation).
            if ( url[0] == '/' )
            {
                return url.Length == 1 || ( url[1] != '/' && url[1] != '\\' );
            }

            // Application-relative: "~/" or "~/path", but NOT "~//" or "~/\".
            if ( url.Length > 1 && url[0] == '~' && url[1] == '/' )
            {
                return url.Length == 2 || ( url[2] != '/' && url[2] != '\\' );
            }

            // Absolute URL: must be http or https — blocks javascript:, data:, etc.
            Uri absoluteUri;
            if ( !Uri.TryCreate( url, UriKind.Absolute, out absoluteUri ) )
            {
                return false;
            }

            if ( absoluteUri.Scheme != Uri.UriSchemeHttp && absoluteUri.Scheme != Uri.UriSchemeHttps )
            {
                return false;
            }

            var candidateHost = NormalizeHost( absoluteUri.Host );
            if ( string.IsNullOrEmpty( candidateHost ) )
            {
                return false;
            }

            // 1. Current request host (keeps same-site behavior even without a SiteDomain row).
            if ( requestUrl != null &&
                 string.Equals( NormalizeHost( requestUrl.Host ), candidateHost, StringComparison.OrdinalIgnoreCase ) )
            {
                return true;
            }

            // 2. Rock-known domains: SiteDomain rows — exact, case-insensitive, normalized match.
            //    Do NOT use GetByDomainContained() — it is a substring match and allows
            //    "rock.secc.org.evil.com" to pass as "rock.secc.org".
            using ( var rockContext = new RockContext() )
            {
                var knownDomains = new SiteDomainService( rockContext )
                    .Queryable()
                    .Select( d => d.Domain )
                    .ToList();

                foreach ( var domain in knownDomains )
                {
                    if ( string.Equals( NormalizeHost( domain ), candidateHost, StringComparison.OrdinalIgnoreCase ) )
                    {
                        return true;
                    }
                }
            }

            // 3. Admin-configured extra hosts (e.g. Webflow sites Rock has no SiteDomain row for).
            //    Global attribute key: OAuthAllowedRedirectDomains
            //    Format: comma- or newline-separated host names (no scheme), e.g. "www.southeastchristian.org"
            var attributeValue = GlobalAttributesCache.Value( "OAuthAllowedRedirectDomains" );
            if ( !string.IsNullOrWhiteSpace( attributeValue ) )
            {
                foreach ( var entry in attributeValue.Split( new char[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries ) )
                {
                    if ( string.Equals( NormalizeHost( entry.Trim() ), candidateHost, StringComparison.OrdinalIgnoreCase ) )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Strips scheme, port, path, and query from a host-or-URL string, returning a
        /// lowercase bare hostname suitable for exact comparison.
        /// </summary>
        private static string NormalizeHost( string input )
        {
            if ( string.IsNullOrWhiteSpace( input ) )
            {
                return string.Empty;
            }

            // If the value includes a scheme (e.g. "https://www.southeastchristian.org"),
            // parse it as a URI to isolate the Host property.
            if ( input.IndexOf( "://", StringComparison.Ordinal ) >= 0 )
            {
                Uri uri;
                if ( Uri.TryCreate( input, UriKind.Absolute, out uri ) )
                {
                    input = uri.Host;
                }
                else
                {
                    return string.Empty;
                }
            }

            // Strip any residual port, path, or query (e.g. "rock.secc.org:443" or "rock.secc.org/path").
            int cut = input.IndexOfAny( new char[] { '/', ':', '?' } );
            if ( cut >= 0 )
            {
                input = input.Substring( 0, cut );
            }

            return input.Trim().TrimEnd( '.' ).ToLowerInvariant();
        }
    }
}
