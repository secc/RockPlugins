using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Newtonsoft.Json;
using Rock.Web.UI;
using System.Data.Entity;

namespace RockWeb.Plugins.org_secc.GroupManager
{

    [DisplayName( "Breakout Group Migration" )]
    [Category( "SECC > Groups" )]
    [Description( "Tool to migrate kids to groups." )]

    public partial class BreakoutGroupMigration : RockBlock
    {

        List<int> graduationYears = new List<int>() { 2029, 2028, 2027, 2026, 2025, 2024 };
        Dictionary<int, string> graduationKey = new Dictionary<int, string>() {
            { 2029, "Kindergarten"},
            { 2028, "1st Grade" },
            { 2027,"2nd Grade" },
            { 2026, "3rd Grade" },
            { 2025, "4th Grade"},
            { 2024,  "5th Grade" } };

        int parentGroupId = 356254;
        int groupTypeId = 150;

        Dictionary<int, int> scheduleKey = new Dictionary<int, int>() {
            {1,42 },
            {2,44 },
            {3,46 },
        };

        int groupTypeRoleId = 369;


        RockContext rockContext;

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
        }

        protected void btnStart_Click( object sender, EventArgs e )
        {
            rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            PersonService personService = new PersonService( rockContext );
            GroupMemberService groupMemberService = new GroupMemberService(rockContext);

            foreach ( var year in graduationYears )
            {
                var children = personService.Queryable().Where( p => p.GraduationYear == year );
                foreach ( var child in children.ToList() )
                {
                    child.LoadAttributes();
                    var breakoutGroupAttribute = child.GetAttributeValue( "Arena-16-2938" );
                    if ( string.IsNullOrEmpty( breakoutGroupAttribute ) )
                    {
                        continue;
                    }
                    var groupParts = breakoutGroupAttribute.Split( '-' );
                    if ( groupParts.Count() != 2 || !scheduleKey.Keys.Contains( groupParts[0].AsInteger() ) )
                    {
                        continue;
                    }

                    var groupLetter = groupParts[1];
                    var groupScheduleId = scheduleKey[groupParts[0].AsInteger()];
                    var gradeName = graduationKey[year];

                    var group = groupService.Queryable()
                        .Where( g =>
                            g.GroupTypeId == groupTypeId
                            && g.Name.EndsWith( groupLetter )
                            && g.Name.StartsWith( gradeName )
                            && g.ScheduleId == groupScheduleId
                            ).FirstOrDefault();
                    if ( group == null )
                    {
                        group = CreateNewGroup( gradeName, groupScheduleId, groupLetter );
                    }

                    if ( !group.Members.Select( gm => gm.PersonId ).Contains( child.Id ) )
                    {
                        var groupMember = new GroupMember()
                        {
                            GroupId = group.Id,
                            PersonId = child.Id,
                            GroupRoleId = groupTypeRoleId
                        };
                        groupMemberService.Add( groupMember );
                        rockContext.SaveChanges();
                    }

                }
            }
        }

        private Group CreateNewGroup( string gradeName, int groupScheduleId, string groupLetter )
        {
            var schedule = new ScheduleService( rockContext ).Get( groupScheduleId );


            var groupName = string.Format( "{0} | {1} | Group {2}", gradeName, schedule.Name, groupLetter );
            GroupService groupService = new GroupService( rockContext );
            var group = new Group()
            {
                Name = groupName,
                GroupTypeId = groupTypeId,
                ScheduleId = groupScheduleId,
                ParentGroupId = parentGroupId                
            };

            groupService.Add( group );
            rockContext.SaveChanges();
            group.LoadAttributes();
            group.SetAttributeValue( "Letter", groupLetter );
            group.SaveAttributeValues();
            return group;
        }
    }
}
