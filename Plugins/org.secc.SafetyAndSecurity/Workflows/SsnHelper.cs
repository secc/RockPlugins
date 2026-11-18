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
using System.Linq;
using System.Text.RegularExpressions;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Security;

namespace org.secc.SafetyAndSecurity
{
    /// <summary>
    /// SSN rules shared by the Volunteer Application SSN and Volunteer Application Validation actions.
    /// </summary>
    internal static class SsnHelper
    {
        /// <summary>
        /// Determines whether the SSN is nine digits in XXX-XX-XXXX form and not an obviously fake value.
        /// </summary>
        internal static bool IsValidSsnFormat( string ssn )
        {
            return ssn != null && Regex.Match( ssn, @"^(?!(123[ -]?45[ -]?6789)|(([0-9])\3\3[ -]?\3\3[ -]?\3\3\3\3))([0-9]{3}[- ]?[0-9]{2}[- ]?[0-9]{4})$" ).Success;
        }

        /// <summary>
        /// Works out the SSN the workflow's SSN boxes stand for. After the SSN is stored, the first two boxes
        /// are masked as ***-** and the last four stay editable, so the mask stands in for the stored first
        /// five digits only when the person really has an SSN stored.
        /// </summary>
        /// <returns>null when the boxes still match the stored SSN; otherwise the SSN to validate and store.</returns>
        internal static string ResolveSsn( RockContext rockContext, Rock.Model.Workflow workflow )
        {
            string ssn1 = Encryption.DecryptString( workflow.GetAttributeValue( "SSN1" ) );
            string ssn2 = Encryption.DecryptString( workflow.GetAttributeValue( "SSN2" ) );
            string ssn3 = Encryption.DecryptString( workflow.GetAttributeValue( "SSN3" ) );

            string storedSsn = IsMasked( ssn1, ssn2 ) ? GetStoredSsn( rockContext, workflow ) : null;

            return ResolveSsn( ssn1, ssn2, ssn3, storedSsn );
        }

        /// <summary>
        /// Works out the SSN the boxes stand for, given the person's decrypted stored SSN.
        /// </summary>
        /// <returns>null when the boxes still match the stored SSN; otherwise the SSN to validate and store.</returns>
        internal static string ResolveSsn( string ssn1, string ssn2, string ssn3, string storedSsn )
        {
            if ( IsMasked( ssn1, ssn2 ) )
            {
                var digits = new string( ( storedSsn ?? string.Empty ).Where( c => c >= '0' && c <= '9' ).ToArray() );
                if ( digits.Length == 9 )
                {
                    var lastFour = ( ssn3 ?? string.Empty ).Trim();
                    return lastFour == digits.Substring( 5 )
                        ? null
                        : digits.Substring( 0, 3 ) + "-" + digits.Substring( 3, 2 ) + "-" + lastFour;
                }
            }

            return ssn1 + "-" + ssn2 + "-" + ssn3;
        }

        /// <summary>
        /// Determines whether the first two SSN boxes hold the mask the SSN action leaves after storing the SSN.
        /// </summary>
        private static bool IsMasked( string ssn1, string ssn2 )
        {
            return ssn1 == "***" && ssn2 == "**";
        }

        /// <summary>
        /// Gets the workflow's Person's decrypted SSN attribute, or null if there is no Person.
        /// </summary>
        private static string GetStoredSsn( RockContext rockContext, Rock.Model.Workflow workflow )
        {
            PersonAlias personAlias = new PersonAliasService( rockContext ).Get( workflow.GetAttributeValue( "Person" ).AsGuid() );
            if ( personAlias == null || personAlias.Person == null )
            {
                return null;
            }

            personAlias.Person.LoadAttributes( rockContext );
            return Encryption.DecryptString( personAlias.Person.GetAttributeValue( "SSN" ) );
        }
    }
}
