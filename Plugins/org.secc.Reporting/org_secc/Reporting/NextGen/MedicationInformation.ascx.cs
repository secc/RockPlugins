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
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Blocks.Reporting.NextGen
{
    [DisplayName( "Medication Information" )]
    [Category( "SECC > Reporting > NextGen" )]
    [Description( "T" )]
    [GroupTypesField( "Group Types", "Any member of these group types will show up." )]
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


                var groupTypeStrings = GetAttributeValue( "GroupTypes" ).SplitDelimitedValues();
                var groupTypeIds = new List<int>();
                foreach ( var groupType in groupTypeStrings )
                {
                    var groupTypeCache = GroupTypeCache.Get( groupType.AsGuid() );
                    if ( groupTypeCache != null )
                    {
                        groupTypeIds.Add( groupTypeCache.Id );
                    }
                }

                var groups = new GroupService( rockContext ).Queryable()
                    .Where( g => g.IsActive && !g.IsArchived && groupTypeIds.Contains( g.GroupTypeId ) );
                if ( !groups.Any() )
                {
                    nbAlert.Visible = true;
                    nbAlert.Text = "Please configure this block.";
                    return;
                }

                var members = groups.SelectMany( g => g.Members );


                var gridData = new List<GridData>();
                AttributeMatrixService attributeMatrixService = new AttributeMatrixService( rockContext );
                foreach ( var person in familyMembers )
                {
                    //Get all the camp group members for this person
                    List<GroupMember> medicalMembers = members.Where( m => m.PersonId == person.Id ).ToList();

                    if ( !medicalMembers.Any() )
                    {
                        continue;
                    }

                    GridData data = new GridData
                    {
                        Id = person.PrimaryAlias.Guid,
                        Name = person.FullName,
                        Group = string.Join( "<br>", medicalMembers.Select( m => m.Group.Name ) ),
                        Medications = "No Medication Information"
                    };
                    person.LoadAttributes();
                    var attribute = person.GetAttributeValue( GetAttributeValue( "MedicationMatrixKey" ) );
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
            var personAliasGuid = ( ( Guid ) e.RowKeyValue ).ToString();
            NavigateToWorkflow( personAliasGuid );
         
        }

        protected void gGrid_RowSelected( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            var personAliasGuid = ( ( Guid ) e.RowKeyValue ).ToString();
            NavigateToWorkflow( personAliasGuid );
        }

        private void NavigateToWorkflow( string personAliasGuid )
        {
            RockContext rockContext = new RockContext();
            WorkflowTypeService workflowTypeService = new WorkflowTypeService( rockContext );
            var workflowGuid = GetAttributeValue( "EditWorkflow" );
            var workflowId = workflowTypeService.Get( workflowGuid.AsGuid() ).Id.ToString();
            NavigateToLinkedPage( "WorkflowPage", new Dictionary<string, string> { { "WorkflowTypeId", workflowId }, { "PersonGuid", personAliasGuid } } );
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