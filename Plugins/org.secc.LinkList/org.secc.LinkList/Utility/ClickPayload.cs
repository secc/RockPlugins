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

using Newtonsoft.Json.Linq;

namespace org.secc.LinkList.Utility
{
    /// <summary>
    /// Parses the click-beacon body posted by the web component. sendBeacon
    /// sends a plain STRING (arrives as text/plain, so Web API model binding
    /// never runs - the endpoint reads the raw body and hands it here).
    /// Expected shape: <c>{"matrixItemGuid":"..."}</c>.
    ///
    /// Pure and defensive (public unauthenticated input): bounded size,
    /// strict guid parse, never throws. BCL + Newtonsoft only, so it is
    /// unit-testable in isolation.
    /// </summary>
    public static class ClickPayload
    {
        /// <summary>Reject bodies larger than this before parsing.</summary>
        public const int MaxBodyLength = 2048;

        /// <summary>
        /// True when <paramref name="body"/> is a JSON object whose
        /// <c>matrixItemGuid</c> property parses as a Guid. False (with
        /// <see cref="Guid.Empty"/>) on null, oversized, malformed, or
        /// non-object input. Never throws.
        /// </summary>
        public static bool TryParse( string body, out Guid matrixItemGuid )
        {
            matrixItemGuid = Guid.Empty;

            if ( string.IsNullOrWhiteSpace( body ) || body.Length > MaxBodyLength )
            {
                return false;
            }

            try
            {
                var token = JToken.Parse( body );
                if ( token.Type != JTokenType.Object )
                {
                    return false;
                }

                var raw = ( ( JObject ) token ).Value<string>( "matrixItemGuid" );
                return Guid.TryParse( raw, out matrixItemGuid );
            }
            catch
            {
                matrixItemGuid = Guid.Empty;
                return false;
            }
        }
    }
}
