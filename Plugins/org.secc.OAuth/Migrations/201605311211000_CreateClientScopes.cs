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

    [MigrationNumber( 4, "1.4.5" )]
    class CreateClientScopes : Rock.Plugin.Migration
    {
        public override void Up()
        {
            Sql( @"INSERT INTO [dbo].[_org_secc_OAuth_Scope]
                    ([Identifier]
                    , [Description]
                    , [Active]
                    , [CreatedDateTime]
                    , [Guid])
                VALUES(
                    'Family'
                    , 'Allows access to profile data for the user''s family'
                    , 1
                    ,GETDATE()
                    ,NEWID());
                " );

            Sql( @"INSERT INTO [dbo].[_org_secc_OAuth_Scope]
                    ([Identifier]
                    , [Description]
                    , [Active]
                    , [CreatedDateTime]
                    , [Guid])
                VALUES(
                    'Profile'
                    , 'Allows access to user''s profile data (name, birthdate, email)'
                    , 1
                    ,GETDATE()
                    ,NEWID());
                " );
        }
        public override void Down()
        {
            Sql( "Truncate [dbo].[_org_secc_OAuth_Authorization];" );
            Sql( "Truncate [dbo].[_org_secc_OAuth_ClientScope];" );
            Sql( "Truncate [dbo].[_org_secc_OAuth_Scope];" );
            Sql( "Truncate [dbo].[_org_secc_OAuth_Client];" );
        }

    }
}
