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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalanche;
using Avalanche.Attribute;
using Avalanche.Models;
using Newtonsoft.Json;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.GroupManager
{
    [DisplayName( "Camp Group" )]
    [Category( "SECC > Groups" )]
    [Description( "Block for camp leaders to see their group." )]

    [CodeEditorField( "Lava", "Lava to use in displaying the members.", Rock.Web.UI.Controls.CodeEditorMode.Lava )]
    [LavaCommandsField( "Enabled Lava Commands", "The Lava commands that should be enabled for this block.", false )]
    [KeyValueListField( "Group Ids", "Map the volunteer group ids to the student group ids", true, "", "Volunteer Group Id", "Student Group Id" )]
    [TextField( "Volunteer Group Key", "Key for the group attribute for the volunteer's group" )]
    [TextField( "Student Group Key", "Key for the group attribute for the students's group" )]
    public partial class CampGroupMembers : RockBlock
    {

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summarysni>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                try
                {
                    RenderPage();
                }
                catch ( Exception ex )
                {
                    ltOutput.Text = "There was an issue.";
                    LogException( ex );
                }
            }
        }

        public void RenderPage()
        {
            var pairs = GetAttributeValue( "GroupIds" ).ToKeyValuePairList();
            RockContext rockContext = new RockContext();
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            AttributeValueService attributeValueService = new AttributeValueService( rockContext );
            List<GroupMember> members = new List<GroupMember>();

            foreach ( var pair in pairs )
            {
                var volunteerId = pair.Key.AsInteger();
                var volunteers = groupMemberService.GetByGroupIdAndPersonId( volunteerId, CurrentPerson.Id );
                foreach ( var volunteer in volunteers )
                {
                    //Add the volunteer and any other volunteers
                    members.Add( volunteer );
                    volunteer.LoadAttributes();
                    var campGroupName = volunteer.GetAttributeValue( GetAttributeValue( "VolunteerGroupKey" ) );
                    var otherVolunteers = groupMemberService.GetByGroupId( volunteer.GroupId );
                    var volunterGroupAttributeValues = attributeValueService.GetByAttributeId( volunteer.Attributes[GetAttributeValue( "VolunteerGroupKey" )].Id ).ToList();

                    foreach ( var otherVolunteer in otherVolunteers )
                    {
                        if ( volunterGroupAttributeValues.Where( av => av.EntityId == otherVolunteer.Id && av.Value == campGroupName ).Any() )
                        {
                            members.Add( otherVolunteer );
                        }
                    }

                    //Now add matching students
                    var students = groupMemberService.GetByGroupId( ( ( string ) pair.Value ).AsInteger() );
                    if ( students.Any() )
                    {
                        var firstStudent = students.FirstOrDefault();
                        firstStudent.LoadAttributes();
                        var studentGroupAttributeValues = attributeValueService.GetByAttributeId( firstStudent.Attributes[GetAttributeValue( "StudentGroupKey" )].Id ).ToList();
                        foreach ( var student in students )
                        {
                            if ( studentGroupAttributeValues.Where( av => av.EntityId == student.Id && av.Value == campGroupName ).Any() )
                            {
                                members.Add( student );
                            }
                        }
                    }
                }
            }
            members = members.DistinctBy( gm => gm.PersonId ).ToList();
            var enabledCommands = GetAttributeValue( "EnabledLavaCommands" );
            var mergeObjects = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, CurrentPerson );
            mergeObjects.Add( "GroupMembers", members );
            ltOutput.Text = GetAttributeValue( "Lava" ).ResolveMergeFields( mergeObjects, enabledCommands );
        }
    }
}