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

namespace RockWeb.Blocks.Reporting.NextGen
{
    [DisplayName( "Medication Dispense" )]
    [Category( "SECC > Reporting > NextGen" )]
    [Description( "T" )]
    [DefinedTypeField( "Medication Schedule Defined Type", "Defined type which contain the values for the possible times to give medication.", key: "DefinedType" )]
    [GroupField( "Group", "Group of people to track medication despensment." )]
    [TextField( "Medication Matrix Key", "The attribute key for the medication schedule matrix." )]
    [CategoryField( "History Category", "Category to save the history.", false, "Rock.Model.History" )]
    public partial class MedicationDispense : RockBlock
    {
        List<MedicalItem> medicalItems = new List<MedicalItem>();
        protected override void OnLoad( EventArgs e )
        {
            nbAlert.Visible = false;

            if ( !Page.IsPostBack )
            {
                dpDate.SelectedDate = RockDateTime.Today;
                var definedType = DefinedTypeCache.Read( GetAttributeValue( "DefinedType" ).AsGuid() );
                if ( definedType != null )
                {
                    ddlSchedule.DataSource = definedType.DefinedValues;
                    ddlSchedule.DataBind();
                    ddlSchedule.Items.Insert( 0, new ListItem( "", "" ) );
                }
                BindGrid();
            }
        }


        private void BindGrid()
        {
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            var group = groupService.Get( GetAttributeValue( "Group" ).AsGuid() );
            if ( group == null )
            {
                nbAlert.Visible = true;
                nbAlert.Text = "Group not found";
                return;
            }
            var groupId = group.Id.ToString();
            var groupEntityid = EntityTypeCache.GetId<Rock.Model.GroupMember>();
            var key = GetAttributeValue( "MedicationMatrixKey" );

            AttributeService attributeService = new AttributeService( rockContext );
            var attribute = attributeService.Queryable()
                .Where( a =>
                    a.EntityTypeQualifierValue == groupId
                    && a.Key == key
                    && a.EntityTypeId == groupEntityid )
                .FirstOrDefault();

            if ( attribute == null )
            {
                nbAlert.Visible = true;
                nbAlert.Text = "Medication attribute not found";
                return;
            }

            var attributeMatrixItemEntityId = EntityTypeCache.GetId<AttributeMatrixItem>();

            AttributeValueService attributeValueService = new AttributeValueService( rockContext );
            AttributeMatrixService attributeMatrixService = new AttributeMatrixService( rockContext );
            AttributeMatrixItemService attributeMatrixItemService = new AttributeMatrixItemService( rockContext );
            HistoryService historyService = new HistoryService( rockContext );

            var members = group
                .Members
                .Join(
                    attributeValueService.Queryable().Where( av => av.AttributeId == attribute.Id ),
                    m => m.Id,
                    av => av.EntityId,
                    ( m, av ) => new { Member = m, AttributeValue = av.Value.AsGuid() }
                )
                .Join(
                    attributeMatrixService.Queryable(),
                    m => m.AttributeValue,
                    am => am.Guid,
                    ( m, am ) => new { Member = m.Member, AttributeMatrix = am }
                )
                .Join(
                    attributeMatrixItemService.Queryable(),
                    m => m.AttributeMatrix.Id,
                    ami => ami.AttributeMatrixId,
                    ( m, ami ) => new { Member = m.Member, AttributeMatrixItem = ami, TemplateId = ami.AttributeMatrixTemplateId.ToString() }
                )
                .Join(
                    attributeService.Queryable().Where( a => a.EntityTypeId == attributeMatrixItemEntityId ),
                    m => m.TemplateId,
                    a => a.EntityTypeQualifierValue,
                    ( m, a ) => new { Member = m.Member, AttributeMatrixItem = m.AttributeMatrixItem, Attribute = a }
                )
                .Join(
                    attributeValueService.Queryable(),
                    m => new { EntityId = m.AttributeMatrixItem.Id, AttributeId = m.Attribute.Id },
                    av => new { EntityId = av.EntityId ?? 0, AttributeId = av.AttributeId },
                    ( m, av ) => new { Member = m.Member, Attribute = m.Attribute, AttributeValue = av, MatrixItemId = m.AttributeMatrixItem.Id, }
                )
                .GroupBy( a => a.Member )
                .ToList();

            var firstDay = ( dpDate.SelectedDate ?? Rock.RockDateTime.Today ).Date;
            var nextday = firstDay.AddDays( 1 );

            var personIds = members.Select( m => m.Key.PersonId );
            var attributeMatrixEntityTypeId = EntityTypeCache.GetId<AttributeMatrixItem>().Value;

            var historyItems = historyService.Queryable()
                .Where( h => personIds.Contains( h.EntityId ) )
                .Where( h => h.RelatedEntityTypeId == attributeMatrixEntityTypeId )
                .Where( h => h.CreatedDateTime >= firstDay && h.CreatedDateTime < nextday )
                .ToList();

            foreach ( var member in members )
            {
                if ( !string.IsNullOrWhiteSpace( tbName.Text )
                    && !member.Key.Person.FullName.ToLower().Contains( tbName.Text.ToLower() )
                    && !member.Key.Person.FullNameReversed.ToLower().Contains( tbName.Text.ToLower() ) )
                {
                    continue;
                }

                var medicines = member.GroupBy( m => m.MatrixItemId );
                foreach ( var medicine in medicines )
                {
                    var scheduleAtt = medicine.FirstOrDefault( m => m.Attribute.Key == "Schedule" );
                    if ( ddlSchedule.SelectedValue != "" && ddlSchedule.SelectedValue.AsGuid() != scheduleAtt.AttributeValue.Value.AsGuid() )
                    {
                        continue;
                    }

                    var medicalItem = new MedicalItem()
                    {
                        Person = member.Key.Person.FullNameReversed,
                        GroupMemberId = member.Key.Id,
                        PersonId = member.Key.Person.Id
                    };

                    if ( scheduleAtt != null )
                    {
                        medicalItem.Schedule = DefinedValueCache.Read( scheduleAtt.AttributeValue.Value.AsGuid() ).Value;
                    }

                    var medAtt = medicine.FirstOrDefault( m => m.Attribute.Key == "Medication" );
                    if ( medAtt != null )
                    {
                        medicalItem.Medication = medAtt.AttributeValue.Value;
                    }

                    var instructionAtt = medicine.FirstOrDefault( m => m.Attribute.Key == "Instructions" );
                    if ( instructionAtt != null )
                    {
                        medicalItem.Instructions = instructionAtt.AttributeValue.Value;
                    }
                    medicalItem.Key = string.Format( "{0}|{1}", medicalItem.PersonId, medicine.Key );

                    var history = historyItems.Where( h => h.EntityId == medicalItem.PersonId && h.RelatedEntityId == medicine.Key );
                    if ( history.Any() )
                    {
                        medicalItem.Distributed = true;
                        medicalItem.History = string.Join( "<br>", history.Select( h => h.Summary ) );
                    }
                    medicalItems.Add( medicalItem );

                }
            }
            gGrid.DataSource = medicalItems;
            gGrid.DataBind();

            if ( !dpDate.SelectedDate.HasValue
                || dpDate.SelectedDate.Value != Rock.RockDateTime.Today )
            {
                gGrid.Columns[gGrid.Columns.Count - 1].Visible=false;
            }
            else
            {
                gGrid.Columns[gGrid.Columns.Count - 1].Visible = true;
            }
        }

        class MedicalItem
        {
            public string Key { get; set; }
            public int PersonId { get; set; }
            public int GroupMemberId { get; set; }
            public string Person { get; set; }
            public string Medication { get; set; }
            public string Instructions { get; set; }
            public string Schedule { get; set; }
            public bool Distributed { get; set; }
            public string History { get; set; }
        }

        protected void Distribute_Click( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            RockContext rockContext = new RockContext();
            HistoryService historyService = new HistoryService( rockContext );
            var keys = ( ( string ) e.RowKeyValue ).SplitDelimitedValues();
            var personId = keys[0].AsInteger();
            var matrixId = keys[1].AsInteger();

            AttributeMatrixItemService attributeMatrixItemService = new AttributeMatrixItemService( rockContext );
            var matrix = attributeMatrixItemService.Get( matrixId );
            matrix.LoadAttributes();
            var category = new CategoryService( rockContext ).Get( GetAttributeValue( "HistoryCategory" ).AsGuid() );

            History history = new History()
            {
                CategoryId = category.Id,
                EntityTypeId = EntityTypeCache.GetId<Person>().Value,
                EntityId = personId,
                RelatedEntityTypeId = EntityTypeCache.GetId<AttributeMatrixItem>().Value,
                RelatedEntityId = matrixId,
                Verb = "Distributed",
                Caption = "Medication Distributed",
                Summary = string.Format( "<span class=\"field-name\">{0}</span> was distributed at <span class=\"field-name\">{1}</span>", matrix.GetAttributeValue( "Medication" ), Rock.RockDateTime.Now )
            };
            historyService.Add( history );
            rockContext.SaveChanges();
            BindGrid();
        }

        protected void ddlSchedule_SelectedIndexChanged( object sender, EventArgs e )
        {
            BindGrid();
        }

        protected void dpDate_TextChanged( object sender, EventArgs e )
        {
            BindGrid();
        }

        protected void tbName_TextChanged( object sender, EventArgs e )
        {
            BindGrid();
        }
    }
}