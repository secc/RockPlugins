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

namespace RockWeb.Plugins.org_secc.Administration
{

    [DisplayName( "Neighborhood Group Merge" )]
    [Category( "SECC > Administration" )]
    [Description( "Tool to merge neighborhood group rosters, attendance, and update the cycles attribute." )]

    public partial class NeighborhoodGroupMerge : RockBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
        }

        protected void btnMerge_Click( object sender, EventArgs e )
        {

            int? targetGroupId = gpTarget.SelectedValueAsInt();
            int? sourceGroupId = gpSource.SelectedValueAsInt();

            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            AttendanceService attendanceService = new AttendanceService( rockContext );

            var destLWYA = groupService.Get( targetGroupId.Value );
            
            var sourceLWYA = groupService.Get( sourceGroupId.Value );
            
            //Add source members to target group as inactive
            foreach ( var member in sourceLWYA.Members.Where( m => !destLWYA.Members.Select( dM => dM.PersonId ).Contains( m.PersonId ) ) )
            {
                destLWYA.Members.Add( new GroupMember()
                {
                    PersonId = member.PersonId,
                    GroupId = destLWYA.Id,
                    GroupRoleId = member.GroupRoleId,
                    GroupMemberStatus = GroupMemberStatus.Inactive
                } );
            }
            rockContext.SaveChanges();

            //Switch Groups Ids on attendance
            foreach ( var member in destLWYA.Members )
            {
                var attendances = attendanceService.Queryable()
                    .Where( a => a.PersonAlias.PersonId == member.PersonId && a.GroupId == sourceLWYA.Id );
                foreach ( var attendance in attendances )
                {
                    attendance.GroupId = destLWYA.Id;
                }
            }
            rockContext.SaveChanges();

            //set cycle attribute
            AddCycle( destLWYA, sourceLWYA );
        }
        

        private void CopyLocations( Group destLWYA, Group sourceLWYA )
        {
            foreach (var groupLocation in sourceLWYA.GroupLocations )
            {
                var newGroupLocation = groupLocation.Clone( false );
                newGroupLocation.Id = 0;
                newGroupLocation.Guid = Guid.NewGuid();
                newGroupLocation.GroupId = destLWYA.Id;
                destLWYA.GroupLocations.Add( newGroupLocation );
            }
        }

        private void CopyAttributes( Group destLWYA, Group sourceLWYA )
        {
            sourceLWYA.LoadAttributes();
            destLWYA.LoadAttributes();

            foreach ( var att in sourceLWYA.Attributes.Select( a => a.Key ) )
            {
                destLWYA.SetAttributeValue( att, sourceLWYA.GetAttributeValue( att ) );
            }

            destLWYA.SaveAttributeValues();
        }

        private void AddCycle( Group destLWYA, Group sourceLWYA )
        {
            destLWYA.LoadAttributes();
            sourceLWYA.LoadAttributes();
            var newCycles = destLWYA.GetAttributeValue( "LWYACycle" ).Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
            var oldCycles = sourceLWYA.GetAttributeValue( "LWYACycle" ).Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
            foreach ( var value in oldCycles )
            {
                if ( !newCycles.Contains( value ) )
                {
                    newCycles.Add( value );
                }
            }
            destLWYA.SetAttributeValue( "LWYACycle", string.Join( ",", newCycles ) );
            destLWYA.SaveAttributeValues();
        }
    }
}
