// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using Rock.Plugin;

namespace org.secc.Migrations
{
    /// <summary>
    /// ROCK-8762: backing storage for the short account-confirmation code that
    /// org.secc.Rest AccountController emails to mobile users.
    ///
    /// Only the hash of the outstanding code and the time it was issued are stored. The code
    /// itself is never persisted, and both values are cleared once the code is used or expires.
    /// Adding these attributes is metadata only -- no existing rows are read or rewritten.
    ///
    /// Attribute keys and the hash attribute's Guid are duplicated as constants in
    /// AccountController.Partial.cs; keep the two in sync.
    /// </summary>
    [MigrationNumber( 34, "1.12.9" )]
    public partial class UserLogin_ConfirmationCodeAttributes : Migration
    {
        public override void Up()
        {
            // Entity: Rock.Model.UserLogin Attribute: Confirmation Code Hash
            RockMigrationHelper.AddOrUpdateEntityAttribute( "Rock.Model.UserLogin",
                Rock.SystemGuid.FieldType.TEXT,
                "",
                "",
                "Confirmation Code Hash",
                "Confirmation Code Hash",
                "SHA-256 hash of the account confirmation code most recently issued for this login. Written and cleared by the org.secc.Rest account API; not intended to be edited by hand.",
                0,
                "",
                "7B3F9A42-5C81-4E6D-9F2A-1D8C4B60E3A7",
                "SeccConfirmationCodeHash" );

            // Entity: Rock.Model.UserLogin Attribute: Confirmation Code Issued
            RockMigrationHelper.AddOrUpdateEntityAttribute( "Rock.Model.UserLogin",
                Rock.SystemGuid.FieldType.TEXT,
                "",
                "",
                "Confirmation Code Issued",
                "Confirmation Code Issued",
                "Tick count for the date and time the outstanding account confirmation code was issued. Used to expire the code.",
                1,
                "",
                "C4E81D57-2A93-4F16-B8D5-6E70F9A24C1B",
                "SeccConfirmationCodeIssued" );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( "7B3F9A42-5C81-4E6D-9F2A-1D8C4B60E3A7" ); // Rock.Model.UserLogin: Confirmation Code Hash
            RockMigrationHelper.DeleteAttribute( "C4E81D57-2A93-4F16-B8D5-6E70F9A24C1B" ); // Rock.Model.UserLogin: Confirmation Code Issued
        }
    }
}
