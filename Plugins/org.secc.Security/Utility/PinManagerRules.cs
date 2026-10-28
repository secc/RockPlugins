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
using System.Linq;
using Rock.Model;

namespace org.secc.Security.Utility
{
    /// <summary>
    /// The rules the PIN Manager block applies when it edits, saves or removes a PIN.
    /// </summary>
    public static class PinManagerRules
    {
        /// <summary>
        /// Returns true when the login is a PIN login of the person.
        /// </summary>
        /// <param name="login">The login, or null.</param>
        /// <param name="personId">The Id of the person on the page.</param>
        /// <param name="pinEntityTypeId">The Id of the PIN authentication entity type.</param>
        /// <returns></returns>
        public static bool IsPersonPin( UserLogin login, int personId, int pinEntityTypeId )
        {
            return login != null && login.PersonId == personId && login.EntityTypeId == pinEntityTypeId;
        }

        /// <summary>
        /// Checks a typed PIN and returns null when it can be saved, otherwise the message to show. A PIN equal to
        /// the current PIN, as typed or without surrounding spaces, isn't checked again and keeps its stored value.
        /// A new or changed PIN has its surrounding spaces removed and must be digits 0-9, not all zeros, at least
        /// <paramref name="minimumLength"/> digits long (leading zeros count), and not used by any login.
        /// </summary>
        /// <param name="text">The typed PIN.</param>
        /// <param name="currentPin">The login's current PIN, or null for a new PIN.</param>
        /// <param name="minimumLength">The minimum number of digits.</param>
        /// <param name="isTaken">Returns true when a login already has the given user name.</param>
        /// <param name="pin">The PIN to save.</param>
        /// <returns></returns>
        public static string CheckPin( string text, string currentPin, int minimumLength, Func<string, bool> isTaken, out string pin )
        {
            pin = ( text ?? string.Empty ).Trim();
            if ( currentPin != null && ( text == currentPin || pin == currentPin ) )
            {
                pin = currentPin;
                return null;
            }

            // Nothing is left after removing leading zeros when the PIN is blank or all zeros.
            if ( !pin.All( c => c >= '0' && c <= '9' ) || pin.TrimStart( '0' ).Length == 0 )
            {
                return "Error: Requested PIN could not be converted to number.";
            }

            if ( pin.Length < minimumLength )
            {
                return string.Format( "Error: Requested PIN must be {0} digits or greater.", minimumLength );
            }

            if ( isTaken( pin ) )
            {
                return "This PIN has already been taken.";
            }

            return null;
        }
    }
}
