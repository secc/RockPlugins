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

using System;
using System.Linq;
using Rock;
using Rock.Plugin;


namespace org.secc.Jobs.Migrations
{
    [MigrationNumber( 1, "1.12.0" )]
    public partial class EventRelationshipRoles : Migration
    {
        public override void Up()
        {
            var knownRelationshipGroupTypeGuid = Rock.SystemGuid.GroupType.GROUPTYPE_KNOWN_RELATIONSHIPS;


            RockMigrationHelper.AddGroupTypeRole( knownRelationshipGroupTypeGuid, "Event Can Checkin", "Can checkin a person for an event", 100, maxCount: null, minCount: null, guid: "1758C197-8C6F-4727-A52B-37FA19603C35", isSystem: false, isLeader: false, isDefaultGroupTypeRole: false );
            RockMigrationHelper.AddGroupTypeRole( knownRelationshipGroupTypeGuid, "Event Allow Checkin By", "Allow checkin by person for an event.", 101, maxCount: null, minCount: null, guid: "0CAF69B9-9C8D-4222-BAF8-31C54BA0C123", isSystem: false, isLeader: false, isDefaultGroupTypeRole: false );

            Rock.Web.Cache.GroupTypeCache.UpdateCachedEntity( Rock.Web.Cache.GroupTypeCache.GetId( knownRelationshipGroupTypeGuid.AsGuid() ).Value, System.Data.Entity.EntityState.Modified );

            var eventCanCheckinGuid = new Guid( "1758C197-8C6F-4727-A52B-37FA19603C35" );
            var eventAllowCheckinByGuid = new Guid( "0CAF69B9-9C8D-4222-BAF8-31C54BA0C123" );
            var knownRelationshipGroupType = Rock.Web.Cache.GroupTypeCache.Get( knownRelationshipGroupTypeGuid );

            var canCheckinRole = knownRelationshipGroupType.Roles.FirstOrDefault( r => r.Guid == eventCanCheckinGuid );
            var allowCheckinByRole = knownRelationshipGroupType.Roles.FirstOrDefault( r => r.Guid == eventAllowCheckinByGuid );

            if ( canCheckinRole != null && allowCheckinByRole != null)
            {
                RockMigrationHelper.AddAttributeValue( "610A5BE8-8FDE-46AA-8F9D-1AF7F1F23441", canCheckinRole.Id, "True", "B63A44AA-073E-4004-AEF9-75FCF8EC5D06" );
                RockMigrationHelper.AddAttributeValue( "C91148D9-D663-493A-86E8-5000BD281852", canCheckinRole.Id, allowCheckinByRole.Guid.ToString(), "48DFC66D-61E0-42E2-8621-1653412328E9" );
                RockMigrationHelper.AddAttributeValue( "C91148D9-D663-493A-86E8-5000BD281852", allowCheckinByRole.Id, canCheckinRole.Guid.ToString(), "B29DC979-91E3-4A3B-9331-27736BFE08E2" );

            }


        }
        public override void Down()
        {
            RockMigrationHelper.DeleteGroupTypeRole( "0CAF69B9-9C8D-4222-BAF8-31C54BA0C123" );
            RockMigrationHelper.DeleteGroupTypeRole( "1758C197-8C6F-4727-A52B-37FA19603C35" );
        }


    }
}
