using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using System.Data.Entity;


namespace org.secc.Jobs
{
    [DisplayName("Remove Can/Allow Event Checkin Relationships")]
    public class RemoveEventCanCheckinRelationships : IJob
    {

        const string EventCanCheckinRelationshipGuid = "1758C197-8C6F-4727-A52B-37FA19603C35";
        const string EventAllowCheckinByRelationshipGuid = "0CAF69B9-9C8D-4222-BAF8-31C54BA0C123";

        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            var groupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_KNOWN_RELATIONSHIPS );
            var canCheckinRole = groupType.Roles.Where( r => r.Guid == EventCanCheckinRelationshipGuid.AsGuid() )
                .SingleOrDefault();

            if(canCheckinRole != null)
            {
                RemoveExpiredEventRelationshipByRole( canCheckinRole.Id );
            }

            var allowCheckinByRole = groupType.Roles.Where( r => r.Guid == EventAllowCheckinByRelationshipGuid.AsGuid() )
                .SingleOrDefault();

            if(allowCheckinByRole != null)
            {
                RemoveExpiredEventRelationshipByRole( allowCheckinByRole.Id );
            }
        }

        private void RemoveExpiredEventRelationshipByRole(int roleId)
        {
            using (var gmContext = new RockContext())
            {
                var groupMemberEntityType = EntityTypeCache.Get( typeof( GroupMember ) );
                var roleIdStr = roleId.ToString();
                var attributeValueQry = new AttributeValueService( gmContext )
                    .Queryable().AsNoTracking()
                    .Where( a => a.Attribute.EntityTypeId == groupMemberEntityType.Id )
                    .Where( a => a.Attribute.Key == "ExpirationDateTime" )
                    .Where( a => a.Attribute.EntityTypeQualifierColumn == "GroupRoleId" )
                    .Where( a => a.Attribute.EntityTypeQualifierValue == roleIdStr );

                var groupMemberService = new GroupMemberService( gmContext );
                var currentTime = RockDateTime.Now;
                var groupMembers = groupMemberService.Queryable()
                    .Where( gm => gm.GroupRoleId == roleId )
                    .Join( attributeValueQry, gm => gm.Id, av => av.EntityId,
                        ( gm, av ) => new { Member = gm, ExpireDate = av.ValueAsDateTime } )
                    .Where( gm => gm.ExpireDate <= currentTime )
                    .Select( gm => gm.Member )
                    .ToList();

                groupMemberService.DeleteRange( groupMembers );
                gmContext.SaveChanges( true );
            }
        }
    }
}
