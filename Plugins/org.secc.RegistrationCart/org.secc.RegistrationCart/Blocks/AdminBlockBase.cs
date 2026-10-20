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
using System.Globalization;
using Rock;
using Rock.Blocks;
using Rock.Security;

namespace org.secc.RegistrationCart.Blocks
{
    /// <summary>Shared plumbing for the Registration Cart administration blocks.</summary>
    public abstract class AdminBlockBase : RockBlockType
    {
        protected const string OfferingKeyParameter = "OfferingKey";

        protected string ObsidianBlockUrl( string fileName ) => "~/Plugins/org_secc/RegistrationCart/blocks/" + fileName + ".obs";

        protected bool AllowIntegerIds => !( PageCache?.Layout?.Site?.DisablePredictableIds ?? false );

        /// <summary>Edit rights come from the block's security settings (inherits from the page).</summary>
        protected bool CanEdit => BlockCache?.IsAuthorized( Authorization.EDIT, GetCurrentPerson() ) ?? false;

        protected BlockActionResult RequireEdit() =>
            CanEdit ? null : ActionForbidden( "You are not authorized to change Registration Cart settings." );

        protected static string DateString( DateTime? date ) => date?.ToString( "yyyy-MM-dd" ) ?? "";

        protected static DateTime? ParseDate( string value )
        {
            if ( value.IsNullOrWhiteSpace() )
            {
                return null;
            }

            // Obsidian date pickers send ISO strings; tolerate full date-times too.
            if ( DateTime.TryParse( value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var d ) )
            {
                return d.Date;
            }

            return value.AsDateTime()?.Date;
        }

        protected static int? ParseInt( string value ) => value.IsNullOrWhiteSpace() ? ( int? ) null : value.AsIntegerOrNull();
    }
}
