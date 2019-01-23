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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using OfficeOpenXml;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Reporting.NextGen
{
    [DisplayName( "Medication Dispense" )]
    [Category( "SECC > Reporting > NextGen" )]
    [Description( "Tool for noting when medications should be given out." )]
    [DefinedTypeField( "Medication Schedule Defined Type", "Defined type which contain the values for the possible times to give medication.", key: "DefinedType" )]
    [TextField( "Group Ids", "Comma separated list of group ids." )]
    [TextField( "Medication Matrix Key", "The attribute key for the medication schedule matrix." )]
    [NoteTypeField( "NoteType", "Medication Note Type", false, "Rock.Model.Person" )]
    [TextField( "Group Member Attribute Filter", "Group member filter to sort group by.", false )]
    public partial class MedicationDispense : RockBlock
    {
        List<MedicalItem> medicalItems = new List<MedicalItem>();


        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gGrid.GridRebind += gGrid_GridRebind;
        }

        protected override void OnLoad( EventArgs e )
        {
            nbAlert.Visible = false;

            if ( !Page.IsPostBack )
            {
                dpDate.SelectedDate = RockDateTime.Today;
                var definedType = DefinedTypeCache.Get( GetAttributeValue( "DefinedType" ).AsGuid() );
                if ( definedType != null )
                {
                    ddlSchedule.DataSource = definedType.DefinedValues;
                    ddlSchedule.DataBind();
                    ddlSchedule.Items.Insert( 0, new ListItem( "", "" ) );
                }

                List<Rock.Model.Attribute> filterAttribute = null;

                var filterAttributeKey = GetAttributeValue( "GroupMemberAttributeFilter" );
                if ( !string.IsNullOrWhiteSpace( filterAttributeKey ) )
                {
                    RockContext rockContext = new RockContext();
                    GroupService groupService = new GroupService( rockContext );
                    AttributeService attributeService = new AttributeService( rockContext );
                    var groupIdStrings = GetAttributeValue( "GroupIds" ).SplitDelimitedValues();
                    var groupIds = new List<int>();
                    foreach ( var id in groupIdStrings )
                    {
                        groupIds.Add( id.AsInteger() );
                    }

                    var groups = groupService.GetByIds( groupIds );

                    var groupTypeIds = groups.ToList().Select( g => g.GroupTypeId.ToString() ).Distinct();
                    var groupEntityid = EntityTypeCache.GetId<Rock.Model.GroupMember>();

                    filterAttribute = attributeService.Queryable()
                        .Where( a =>
                        ( groupIdStrings.Contains( a.EntityTypeQualifierValue ) || groupTypeIds.Contains( a.EntityTypeQualifierValue ) )
                        && a.Key == filterAttributeKey
                        && a.EntityTypeId == groupEntityid ).ToList();

                    if ( filterAttribute != null )
                    {
                        AttributeValueService attributeValueService = new AttributeValueService( rockContext );
                        ddlAttribute.Label = filterAttribute.FirstOrDefault().Name;
                        List<int> attributeIds = filterAttribute.Select( a => a.Id ).ToList();
                        var qry = new GroupMemberService( rockContext ).Queryable()
                            .Where( gm => groupIds.Contains( gm.GroupId ) )
                            .Join(
                                attributeValueService.Queryable().Where( av => attributeIds.Contains( av.AttributeId ) ),
                                m => m.Id,
                                av => av.EntityId,
                                ( m, av ) => new { Key = av.Value, Value = av.Value } )
                                .DistinctBy( a => a.Key )
                                .Where( a => !string.IsNullOrEmpty( a.Key ) )
                                .OrderBy( a => a.Value )
                                .ToList();
                        ddlAttribute.DataSource = qry;
                        ddlAttribute.DataBind();
                        ddlAttribute.Items.Insert( 0, new ListItem( "", "" ) );
                    }
                    else
                    {
                        pnlAttribute.Visible = false;
                    }
                }
                else
                {
                    pnlAttribute.Visible = false;
                }
                BindGrid();
            }

            gGrid.Actions.ShowExcelExport = false;
            gGrid.Actions.ShowMergeTemplate = false;

            if ( this.ContextEntity() == null )
            {
                LinkButton excel = new LinkButton()
                {
                    ID = "btnExcel",
                    Text = "<i class='fa fa-table'></i>",
                    CssClass = "btn btn-default btn-sm"
                };
                gGrid.Actions.Controls.Add( excel );
                excel.Click += GenerateExcel;
                ScriptManager.GetCurrent( this.Page ).RegisterPostBackControl( excel );
            }
        }


        /// <summary>
        /// Handles the GridRebind event of the gGrid control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gGrid_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        private void BindGrid()
        {
            gGrid.DataSource = GetMedicalItems();
            gGrid.DataBind();

            if ( !dpDate.SelectedDate.HasValue
                || dpDate.SelectedDate.Value != Rock.RockDateTime.Today )
            {
                gGrid.Columns[gGrid.Columns.Count - 1].Visible = false;
            }
            else
            {
                gGrid.Columns[gGrid.Columns.Count - 1].Visible = true;
            }
        }

        private List<MedicalItem> GetMedicalItems()
        {
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            var groupIdStrings = GetAttributeValue( "GroupIds" ).SplitDelimitedValues();
            var groupIds = new List<int>();
            foreach ( var id in groupIdStrings )
            {
                groupIds.Add( id.AsInteger() );
            }

            var groups = groupService.GetByIds( groupIds );
            var groupTypeIds = groups.ToList().Select( g => g.GroupTypeId.ToString() ).Distinct();

            var personEntityid = EntityTypeCache.GetId<Rock.Model.Person>().Value;
            var groupMemberEntityid = EntityTypeCache.GetId<Rock.Model.GroupMember>().Value;
            var key = GetAttributeValue( "MedicationMatrixKey" );

            var groupMembers = groups.SelectMany( g => g.Members );

            AttributeService attributeService = new AttributeService( rockContext );



            List<int> attributeIds = attributeService.Queryable()
                .Where( a =>
                    a.EntityTypeId == personEntityid
                    && a.Key == key )
                .Select( a => a.Id ).ToList();

            if ( attributeIds == null )
            {
                nbAlert.Visible = true;
                nbAlert.Text = "Medication attribute not found";
                return null;
            }

            List<int> filterAttributeIds = null;
            var filterAttributeKey = GetAttributeValue( "GroupMemberAttributeFilter" );
            if ( !string.IsNullOrWhiteSpace( filterAttributeKey ) )
            {
                filterAttributeIds = attributeService.Queryable()
                    .Where( a =>
                    ( groupIdStrings.Contains( a.EntityTypeQualifierValue ) || groupTypeIds.Contains( a.EntityTypeQualifierValue ) )
                    && a.Key == filterAttributeKey
                    && a.EntityTypeId == groupMemberEntityid )
                .Select( a => a.Id ).ToList();
            }

            var attributeMatrixItemEntityId = EntityTypeCache.GetId<AttributeMatrixItem>();

            AttributeValueService attributeValueService = new AttributeValueService( rockContext );
            AttributeMatrixService attributeMatrixService = new AttributeMatrixService( rockContext );
            AttributeMatrixItemService attributeMatrixItemService = new AttributeMatrixItemService( rockContext );
            NoteService noteService = new NoteService( rockContext );

            var qry = groupMembers
                .Join(
                    attributeValueService.Queryable().Where( av => attributeIds.Contains( av.AttributeId ) ),
                    m => m.PersonId,
                    av => av.EntityId.Value,
                    ( m, av ) => new { Person = m.Person, Member = m, AttributeValue = av.Value }
                )
                .Join(
                    attributeMatrixService.Queryable(),
                    m => m.AttributeValue,
                    am => am.Guid.ToString(),
                    ( m, am ) => new { Person = m.Person, Member = m.Member, AttributeMatrix = am }
                )
                .Join(
                    attributeMatrixItemService.Queryable(),
                    m => m.AttributeMatrix.Id,
                    ami => ami.AttributeMatrixId,
                    ( m, ami ) => new { Person = m.Person, Member = m.Member, AttributeMatrixItem = ami, TemplateId = ami.AttributeMatrix.AttributeMatrixTemplateId }
                )
                .Join(
                    attributeService.Queryable(),
                    m => new { TemplateIdString = m.TemplateId.ToString(), EntityTypeId = attributeMatrixItemEntityId },
                    a => new { TemplateIdString = a.EntityTypeQualifierValue, EntityTypeId = a.EntityTypeId },
                    ( m, a ) => new { Person = m.Person, Member = m.Member, AttributeMatrixItem = m.AttributeMatrixItem, Attribute = a }
                )
                .Join(
                    attributeValueService.Queryable(),
                    m => new { EntityId = m.AttributeMatrixItem.Id, AttributeId = m.Attribute.Id },
                    av => new { EntityId = av.EntityId ?? 0, AttributeId = av.AttributeId },
                    ( m, av ) => new { Person = m.Person, Member = m.Member, Attribute = m.Attribute, AttributeValue = av, MatrixItemId = m.AttributeMatrixItem.Id, FilterValue = "" }
                );

            if ( filterAttributeIds != null && pnlAttribute.Visible && !string.IsNullOrWhiteSpace( ddlAttribute.SelectedValue ) )
            {
                var filterValue = ddlAttribute.SelectedValue;
                qry = qry
                    .Join(
                    attributeValueService.Queryable().Where( av => filterAttributeIds.Contains( av.AttributeId ) ),
                    m => new { Id = m.Member.Id, Value = filterValue },
                    av => new { Id = av.EntityId ?? 0, Value = av.Value },
                    ( m, av ) => new { Person = m.Person, Member = m.Member, Attribute = m.Attribute, AttributeValue = m.AttributeValue, MatrixItemId = m.MatrixItemId, FilterValue = av.Value } );
            }
            var members = qry.ToList().GroupBy( a => a.Person ).ToList();

            var firstDay = ( dpDate.SelectedDate ?? Rock.RockDateTime.Today ).Date;
            var nextday = firstDay.AddDays( 1 );

            var personIds = members.Select( m => m.Key.Id );
            var attributeMatrixEntityTypeId = EntityTypeCache.GetId<AttributeMatrixItem>().Value;

            var noteType = NoteTypeCache.Get( GetAttributeValue( "NoteType" ).AsGuid() );

            if ( noteType == null )
            {
                return new List<MedicalItem>();
            }

            var noteItems = noteService.Queryable()
                .Where( n => n.NoteTypeId == noteType.Id )
                .Where( n => personIds.Contains( n.EntityId ?? 0 ) )
                .Where( h => h.CreatedDateTime >= firstDay && h.CreatedDateTime < nextday )
                .ToList();

            foreach ( var member in members )
            {
                if ( !string.IsNullOrWhiteSpace( tbName.Text )
                    && !member.Key.FullName.ToLower().Contains( tbName.Text.ToLower() )
                    && !member.Key.FullNameReversed.ToLower().Contains( tbName.Text.ToLower() ) )
                {
                    continue;
                }

                var medicines = member.GroupBy( m => m.MatrixItemId );
                foreach ( var medicine in medicines )
                {
                    var scheduleAtt = medicine.FirstOrDefault( m => m.Attribute.Key == "Schedule" );
                    var schedules = scheduleAtt.AttributeValue.Value.SplitDelimitedValues();
                    foreach ( var schedule in schedules )
                    {

                        if ( ddlSchedule.SelectedValue != "" && ddlSchedule.SelectedValue.AsGuid() != schedule.AsGuid() )
                        {
                            continue;
                        }

                        var medicalItem = new MedicalItem()
                        {
                            Person = member.Key.FullNameReversed,
                            GroupMemberId = member.Key.Id,
                            //GroupMember = member.FirstOrDefault().Person,
                            PersonId = member.Key.Id,
                            FilterAttribute = member.FirstOrDefault().FilterValue
                        };

                        if ( !string.IsNullOrWhiteSpace( schedule ) )
                        {
                            var dv = DefinedValueCache.Get( schedule.AsGuid() );
                            if ( dv != null )
                            {
                                medicalItem.Schedule = dv.Value;
                                medicalItem.ScheduleGuid = dv.Guid;
                            }
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
                        medicalItem.Key = string.Format( "{0}|{1}|{2}", medicalItem.PersonId, medicine.Key, medicalItem.ScheduleGuid );

                        var notes = noteItems
                            .Where( n => n.EntityId == medicalItem.PersonId && n.ForeignId == medicine.Key && n.ForeignGuid == medicalItem.ScheduleGuid );

                        if ( notes.Any() )
                        {
                            medicalItem.Distributed = true;
                            medicalItem.History = string.Join( "<br>", notes.Select( n => n.Text ) );
                        }
                        medicalItems.Add( medicalItem );

                    }
                }
            }

            SortProperty sortProperty = gGrid.SortProperty;
            if ( sortProperty != null )
            {
                if ( sortProperty.Property == "Person" )
                {
                    if ( sortProperty.Direction == SortDirection.Ascending )
                    {
                        medicalItems = medicalItems.OrderBy( mi => mi.Person ).ToList();
                    }
                    else
                    {
                        medicalItems = medicalItems.OrderByDescending( mi => mi.Person ).ToList();
                    }
                }
            }
            else
            {
                medicalItems = medicalItems.OrderBy( mi => mi.Person ).ToList();
                gGrid.SortProperty = new SortProperty() { Property = "Person" };
            }

            return medicalItems;
        }

        private void GenerateExcel( object sender, EventArgs e )
        {
            var medicalItems = GetMedicalItems();

            string filename = gGrid.ExportFilename;
            string workSheetName = "List";
            string title = "Medication Information";

            ExcelPackage excel = new ExcelPackage();
            excel.Workbook.Properties.Title = title;

            // add author info
            Rock.Model.UserLogin userLogin = Rock.Model.UserLoginService.GetCurrentUser();
            if ( userLogin != null )
            {
                excel.Workbook.Properties.Author = userLogin.Person.FullName;
            }
            else
            {
                excel.Workbook.Properties.Author = "Rock";
            }

            // add the page that created this
            excel.Workbook.Properties.SetCustomPropertyValue( "Source", this.Page.Request.Url.OriginalString );

            ExcelWorksheet worksheet = excel.Workbook.Worksheets.Add( workSheetName );
            worksheet.PrinterSettings.LeftMargin = .5m;
            worksheet.PrinterSettings.RightMargin = .5m;
            worksheet.PrinterSettings.TopMargin = .5m;
            worksheet.PrinterSettings.BottomMargin = .5m;

            //// write data to worksheet there are three supported data sources
            //// DataTables, DataViews and ILists

            int rowCounter = 4;
            int columnCounter = 0;

            List<string> columns = new List<string>() { "Name", "Medication", "Instructions", "Schedule" };
            var filterAttribute = "";
            var hasFilter = false;
            if ( !string.IsNullOrWhiteSpace( ddlAttribute.Label ) )
            {
                hasFilter = true;
                columns.Add( ddlAttribute.Label );
                filterAttribute = GetAttributeValue( "GroupMemberAttributeFilter" );
            }

            // print headings
            foreach ( String column in columns )
            {
                columnCounter++;
                worksheet.Cells[3, columnCounter].Value = column.SplitCase();
            }

            foreach ( var item in medicalItems )
            {
                SetExcelValue( worksheet.Cells[rowCounter, 1], item.Person );
                SetExcelValue( worksheet.Cells[rowCounter, 2], item.Medication );
                SetExcelValue( worksheet.Cells[rowCounter, 3], item.Instructions );
                SetExcelValue( worksheet.Cells[rowCounter, 4], item.Schedule );
                if ( hasFilter )
                {
                    item.GroupMember.LoadAttributes();
                    SetExcelValue( worksheet.Cells[rowCounter, 5], item.GroupMember.GetAttributeValue( filterAttribute ) );
                }
                rowCounter++;
            }

            var range = worksheet.Cells[3, 1, rowCounter, columnCounter];

            var table = worksheet.Tables.Add( range, "table1" );

            // ensure each column in the table has a unique name
            var columnNames = worksheet.Cells[3, 1, 3, columnCounter].Select( a => new { OrigColumnName = a.Text, Cell = a } ).ToList();
            columnNames.Reverse();
            foreach ( var col in columnNames )
            {
                int duplicateSuffix = 0;
                string uniqueName = col.OrigColumnName;

                // increment the suffix by 1 until there is only one column with that name
                while ( columnNames.Where( a => a.Cell.Text == uniqueName ).Count() > 1 )
                {
                    duplicateSuffix++;
                    uniqueName = col.OrigColumnName + duplicateSuffix.ToString();
                    col.Cell.Value = uniqueName;
                }
            }

            table.ShowFilter = true;
            table.TableStyle = OfficeOpenXml.Table.TableStyles.None;

            // format header range
            using ( ExcelRange r = worksheet.Cells[3, 1, 3, columnCounter] )
            {
                r.Style.Font.Bold = true;
                r.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                r.Style.Fill.BackgroundColor.SetColor( Color.FromArgb( 223, 223, 223 ) );
                r.Style.Font.Color.SetColor( Color.Black );
                r.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
            }

            // format and set title
            worksheet.Cells[1, 1].Value = title;
            using ( ExcelRange r = worksheet.Cells[1, 1, 2, columnCounter] )
            {
                r.Merge = true;
                r.Style.Font.SetFromFont( new Font( "Calibri", 22, FontStyle.Regular ) );
                r.Style.Font.Color.SetColor( Color.White );
                r.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                r.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                r.Style.Fill.BackgroundColor.SetColor( Color.FromArgb( 34, 41, 55 ) );

                // set border
                r.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                r.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                r.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                r.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            }


            // freeze panes
            worksheet.View.FreezePanes( 3, 1 );

            // autofit columns for all cells
            worksheet.Cells.AutoFitColumns( 1000 );

            byte[] byteArray;
            using ( MemoryStream ms = new MemoryStream() )
            {
                excel.SaveAs( ms );
                byteArray = ms.ToArray();
            }

            // send the spreadsheet to the browser
            this.Page.EnableViewState = false;
            this.Page.Response.Clear();
            this.Page.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            this.Page.Response.AppendHeader( "Content-Disposition", "attachment; filename=" + filename );

            this.Page.Response.Charset = string.Empty;
            this.Page.Response.BinaryWrite( byteArray );
            this.Page.Response.Flush();
            this.Page.Response.End();
        }

        private void SetExcelValue( ExcelRange range, object exportValue )
        {
            if ( exportValue != null &&
                ( exportValue is decimal || exportValue is decimal? ||
                exportValue is int || exportValue is int? ||
                exportValue is double || exportValue is double? ||
                exportValue is DateTime || exportValue is DateTime? ) )
            {
                range.Value = exportValue;
            }
            else
            {
                string value = exportValue != null ? exportValue.ToString().ConvertBrToCrLf().Replace( "&nbsp;", " " ) : string.Empty;
                range.Value = value;
                if ( value.Contains( Environment.NewLine ) )
                {
                    range.Style.WrapText = true;
                }
            }
        }

        class MedicalItem
        {
            public string Key { get; set; }
            public int PersonId { get; set; }
            public int GroupMemberId { get; set; }
            public GroupMember GroupMember { get; set; }
            public string Person { get; set; }
            public string Medication { get; set; }
            public string Instructions { get; set; }
            public string Schedule { get; set; }
            public Guid ScheduleGuid { get; set; }
            public bool Distributed { get; set; }
            public string History { get; set; }
            public string FilterAttribute { get; set; }
        }

        protected void Distribute_Click( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            RockContext rockContext = new RockContext();
            NoteService noteService = new NoteService( rockContext );
            var keys = ( ( string ) e.RowKeyValue ).SplitDelimitedValues();
            var personId = keys[0].AsInteger();
            var matrixId = keys[1].AsInteger();
            var scheduleGuid = keys[2].AsGuid();

            AttributeMatrixItemService attributeMatrixItemService = new AttributeMatrixItemService( rockContext );
            var matrix = attributeMatrixItemService.Get( matrixId );
            matrix.LoadAttributes();
            var noteType = NoteTypeCache.Get( GetAttributeValue( "NoteType" ).AsGuid() );

            Note history = new Note()
            {
                NoteTypeId = noteType.Id,
                EntityId = personId,
                ForeignId = matrixId,
                Caption = "Medication Distributed",
                Text = string.Format( "<span class=\"field-name\">{0}</span> was distributed at <span class=\"field-name\">{1}</span>", matrix.GetAttributeValue( "Medication" ), Rock.RockDateTime.Now ),
                ForeignGuid = scheduleGuid
            };
            noteService.Add( history );
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

        protected void ddlAttribute_SelectedIndexChanged( object sender, EventArgs e )
        {
            BindGrid();
        }
    }
}