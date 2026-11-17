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
using Rock.Plugin;

namespace org.secc.OAuth.Migrations
{
    /// <summary>
    /// Deactivates the sample client named Localhost Test whose callback URL is https://localhost:44300/, and every
    /// active client with the ApiKey and ApiSecret that earlier versions of CreateClientScopes gave the sample client.
    /// </summary>
    [MigrationNumber( 6, "1.13.0" )]
    public class DeactivateLocalhostTestClient : Migration
    {
        /// <summary>
        /// The SHA-256 hash, as a T-SQL binary literal, of the ApiKey and ApiSecret that earlier versions of
        /// CreateClientScopes gave the sample client: the 16 bytes of the ApiKey followed by the 16 bytes of the
        /// ApiSecret, as CAST( ... AS binary(16) ) gives them for uniqueidentifier values (the order of Guid.ToByteArray()).
        /// </summary>
        protected virtual string SampleApiKeyAndSecretSha256 => "0x0D2A574350918E0BB22ECB14C3512F12C78B0D88C623D6F1CBF1C04FC41AD2BD";

        public override void Up()
        {
            Sql( $@"
                UPDATE [dbo].[_org_secc_OAuth_Client]
                SET [Active] = 0, [ModifiedDateTime] = GETDATE()
                WHERE [Active] = 1
                    AND (
                        ( [ClientName] = 'Localhost Test' AND [CallbackUrl] = 'https://localhost:44300/' )
                        OR HASHBYTES( 'SHA2_256', CAST( [ApiKey] AS binary(16) ) + CAST( [ApiSecret] AS binary(16) ) ) = {SampleApiKeyAndSecretSha256} );" );
        }

        public override void Down()
        {
            // Nothing to undo. An administrator can reactivate a client on the OAuth Configuration page.
        }
    }
}
