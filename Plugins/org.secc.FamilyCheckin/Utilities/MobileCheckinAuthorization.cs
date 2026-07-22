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
using Rock;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.FamilyCheckin.Utilities
{
    /// <summary>
    /// Single definition of the mobile check-in ?UserName= impersonation gate (ROCK-8765),
    /// shared by the MobileCheckinStart block and the FamilyCheckin REST controller so the
    /// page and API policies cannot drift.
    /// </summary>
    public static class MobileCheckinAuthorization
    {
        /// <summary>
        /// Attribute key of the MobileCheckinStart block's Debug Mode boolean.
        /// </summary>
        public const string DEBUG_MODE_ATTRIBUTE_KEY = "DebugMode";

        /// <summary>
        /// Whether the caller may act on a ?UserName= impersonated session for the given
        /// MobileCheckinStart block. An authenticated caller is always required; beyond
        /// that, impersonation is allowed while the block's DebugMode attribute is on (a
        /// debug/load-test switch that must stay off in normal operation), or for a person
        /// with ADMINISTRATE on the block. Anonymous callers are never allowed, so a
        /// forgotten DebugMode flag can't be driven without a login (load tests must
        /// authenticate, e.g. via a .ROCK auth cookie).
        /// </summary>
        public static bool IsImpersonationAllowed( BlockCache block, Person person )
        {
            if ( block == null || person == null )
            {
                return false;
            }

            return block.GetAttributeValue( DEBUG_MODE_ATTRIBUTE_KEY ).AsBoolean()
                || block.IsAuthorized( Rock.Security.Authorization.ADMINISTRATE, person );
        }
    }
}
