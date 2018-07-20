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
// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Web.UI;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.Cache;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using Rock.Field;
using Rock.Web.UI.Controls;
using System.Data.Entity;

namespace RockWeb.Blocks.Reporting.NextGen
{
    [DisplayName( "Medication Information" )]
    [Category( "SECC > Reporting > NextGen" )]
    [Description( "T" )]
    [TextField( "Group Ids", "Comma separated list of group ids to check" )]
    [TextField( "Medication Matrix Key", "The attribute key for the medication schedule matrix." )]
    [WorkflowTypeField( "EditWorkflow" )]
    [LinkedPage( "Workflow Page" )]
    public partial class MedicationInformation : RockBlock
    {
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }




        protected override void OnLoad( EventArgs e )
        {
            nbAlert.Visible = false;

            if ( CurrentPerson == null )
            {
                nbAlert.Visible = true;
                nbAlert.Text = "Please log in to continue.";
                return;
            }
            if ( !Page.IsPostBack )
            {
                RockContext rockContext = new RockContext();
                var familyMembers = CurrentPerson.GetFamilies().SelectMany( f => f.Members ).Select( m => m.Person ).ToList();

                AddCaretakees( familyMembers, rockContext );


                var groupIdsString = GetAttributeValue( "GroupIds" ).SplitDelimitedValues();
                var groupIds = new List<int>();
                foreach ( var id in groupIdsString )
                {
                    groupIds.Add( id.AsInteger() );
                }

                var groups = new GroupService( rockContext ).Queryable().Where( g => groupIds.Contains( g.Id ) );
                if ( !groups.Any() )
                {
                    nbAlert.Visible = true;
                    nbAlert.Text = "Please configure this block.";
                    return;
                }

                var members = groups.SelectMany( g => g.Members );

                List<GroupMember> medicalMembers = new List<GroupMember>();

                foreach ( var person in familyMembers )
                {
                    var groupMember = members.Where( m => m.PersonId == person.Id );
                    medicalMembers.AddRange( groupMember );
                }

                var gridData = new List<GridData>();

                AttributeMatrixService attributeMatrixService = new AttributeMatrixService( rockContext );
                foreach ( var member in medicalMembers )
                {
                    GridData data = new GridData
                    {
                        Id = member.Guid,
                        Name = member.Person.FullName,
                        Group = member.Group.Name,
                        Medications = "No Medication Information"
                    };
                    member.LoadAttributes();
                    var attribute = member.GetAttributeValue( GetAttributeValue( "MedicationMatrixKey" ) );
                    var attributeMatrix = attributeMatrixService.Get( attribute.AsGuid() );
                    if ( attributeMatrix != null )
                    {
                        var lava = attributeMatrix.AttributeMatrixTemplate.FormattedLava;
                        var template = attributeMatrix.AttributeMatrixTemplate;
                        template.LoadAttributes();
                        AttributeMatrixItem tempAttributeMatrixItem = new AttributeMatrixItem();
                        tempAttributeMatrixItem.AttributeMatrix = attributeMatrix;
                        tempAttributeMatrixItem.LoadAttributes();
                        Dictionary<string, object> mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, null, new Rock.Lava.CommonMergeFieldsOptions { GetLegacyGlobalMergeFields = false } );
                        mergeFields.Add( "AttributeMatrix", attributeMatrix );
                        mergeFields.Add( "ItemAttributes", tempAttributeMatrixItem.Attributes.Select( a => a.Value ).OrderBy( a => a.Order ).ThenBy( a => a.Name ) );
                        mergeFields.Add( "AttributeMatrixItems", attributeMatrix.AttributeMatrixItems.OrderBy( a => a.Order ) );
                        var medications = lava.ResolveMergeFields( mergeFields );
                        data.Medications = medications;
                    }
                    gridData.Add( data );
                }

                gGrid.DataSource = gridData;
                gGrid.DataBind();
            }
        }

        private void AddCaretakees( List<Person> familyMembers, RockContext rockContext )
        {
            var knownRelationships = Rock.SystemGuid.GroupType.GROUPTYPE_KNOWN_RELATIONSHIPS.AsGuid();
            var caretakerRoll = new GroupTypeRoleService( rockContext )
                .Queryable().AsNoTracking()
                .Where( r => r.GroupType.Guid == knownRelationships && r.Name == "Caretaker" )
                .FirstOrDefault()
                .Id;

            var caretakees = new GroupMemberService( rockContext ).Queryable()
                .Where( gm => gm.GroupRoleId == caretakerRoll && gm.PersonId == CurrentPerson.Id )
                .Select( gm => gm.Group )
                .SelectMany( g => g.Members )
                .Where( gm => gm.GroupRoleId == 5 )
                .Select( gm => gm.Person );
            familyMembers.AddRange( caretakees );
        }

        protected void SelectMember_Click( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            RockContext rockContext = new RockContext();
            WorkflowTypeService workflowTypeService = new WorkflowTypeService( rockContext );
            var groupMemberGuid = ( ( Guid ) e.RowKeyValue ).ToString();
            var workflowGuid = GetAttributeValue( "EditWorkflow" );
            var workflowId = workflowTypeService.Get( workflowGuid.AsGuid() ).Id.ToString();
            NavigateToLinkedPage( "WorkflowPage", new Dictionary<string, string> { { "WorkflowTypeId", workflowId }, { "GroupMemberGuid", groupMemberGuid } } );
        }

        class GridData
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Group { get; set; }
            public string Medications { get; set; }
        }
    }
}