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
using Rock.Web.Cache;

namespace org.secc.RegistrationCart.Services
{
    /// <summary>
    /// Registration Cart settings that apply everywhere (the catalog, the cart pages and
    /// checkout), kept as Rock Global Attributes (Admin Tools › Settings › Global Attributes).
    /// </summary>
    public static class CartSettings
    {
        public const string PreventDuplicateRegistrationsKey = "RegistrationCartPreventDuplicateRegistrations";

        /// <summary>
        /// Refuse a camper who is already registered for the option (wait list included). On
        /// unless staff turn it off.
        /// </summary>
        public static bool PreventDuplicateRegistrations =>
            GlobalAttributesCache.Get().GetValue( PreventDuplicateRegistrationsKey ).AsBooleanOrNull() ?? true;
    }
}
