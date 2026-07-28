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

namespace org.secc.OAuth.Utilities
{
    /// <summary>
    /// Helper for validating user-supplied redirect ("return") URLs to prevent
    /// open-redirect attacks (see ROCK-8763). A URL is only treated as safe when
    /// it navigates back into this application.
    /// </summary>
    public static class RedirectUrlHelper
    {
        /// <summary>
        /// Determines whether the supplied URL is a local URL that is safe to use
        /// as a redirect target.
        /// </summary>
        /// <remarks>
        /// Accepts site-relative paths ("/foo") and application-relative paths
        /// ("~/foo"), plus absolute URLs whose host matches the current request.
        /// Rejects protocol-relative URLs ("//evil.com"), backslash variants
        /// ("/\evil.com"), and absolute URLs pointing at a different host - all of
        /// which a browser would treat as off-site navigation.
        /// </remarks>
        /// <param name="url">The candidate redirect URL. Should already be URL-decoded.</param>
        /// <param name="requestUrl">The current request URL, used to compare hosts for absolute URLs. May be null.</param>
        /// <returns><c>true</c> when the URL is local and safe to redirect to; otherwise <c>false</c>.</returns>
        public static bool IsLocalUrl( string url, Uri requestUrl )
        {
            if ( string.IsNullOrEmpty( url ) )
            {
                return false;
            }

            // Site-relative: "/" or "/path", but NOT "//host" or "/\host"
            // (browsers treat a leading "//" or "/\" as protocol-relative -> off-site).
            if ( url[0] == '/' )
            {
                return url.Length == 1 || ( url[1] != '/' && url[1] != '\\' );
            }

            // Application-relative: "~/" or "~/path", but NOT "~//" or "~/\".
            if ( url.Length > 1 && url[0] == '~' && url[1] == '/' )
            {
                return url.Length == 2 || ( url[2] != '/' && url[2] != '\\' );
            }

            // Absolute URL: only safe when it targets the current request's host.
            Uri absoluteUri;
            if ( requestUrl != null && Uri.TryCreate( url, UriKind.Absolute, out absoluteUri ) )
            {
                return string.Equals( requestUrl.Host, absoluteUri.Host, StringComparison.OrdinalIgnoreCase );
            }

            return false;
        }
    }
}
