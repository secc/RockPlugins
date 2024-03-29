﻿// <copyright>
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.WorkFlowUpdate
{
    [DisplayName( "Workflow Bulk Select" )]
    [Category( "SECC > WorkFlow" )]
    [Description( "Tool for bulk selecting workflows." )]
    [LinkedPage( "Update Page", "Page for updating all the workflows in bulk." )]


    public partial class WorkflowBulkSelect : RockBlock, ICustomGridColumns
    {
        #region Fields

        private WorkflowType _workflowType = null;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the available attributes.
        /// </summary>
        /// <value>
        /// The available attributes.
        /// </value>
        public List<AttributeCache> AvailableAttributes { get; set; }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            AvailableAttributes = ViewState["AvailableAttributes"] as List<AttributeCache>;

            AddDynamicControls();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            int workflowTypeId = 0;
            workflowTypeId = PageParameter( "WorkflowTypeId" ).AsInteger();
            _workflowType = new WorkflowTypeService( new RockContext() ).Get( workflowTypeId );

            if ( _workflowType != null )
            {
                pnlWorkflowTypeSelect.Visible = false;
                gfWorkflows.ApplyFilterClick += gfWorkflows_ApplyFilterClick;
                gfWorkflows.DisplayFilterValue += gfWorkflows_DisplayFilterValue;

                // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
                this.BlockUpdated += Block_BlockUpdated;
                this.AddConfigurationUpdateTrigger( upnlSettings );

                gWorkflows.DataKeyNames = new string[] { "Id" };
                gWorkflows.GridRebind += gWorkflows_GridRebind;

                gWorkflows.Actions.ShowExcelExport = false;
                gWorkflows.Actions.ShowMergeTemplate = false;

                LinkButton btnBulk = new LinkButton
                {
                    Text = "<i class='fa fa-truck fa-fw'></i>",
                    CssClass = "btn-bulk-update btn btn-default btn-sm"
                };
                btnBulk.Click += Actions_BulkUpdateClick;
                gWorkflows.Actions.Controls.Add( btnBulk );

                if ( !string.IsNullOrWhiteSpace( _workflowType.WorkTerm ) )
                {
                    gWorkflows.RowItemText = _workflowType.WorkTerm;
                    lGridTitle.Text = _workflowType.WorkTerm.Pluralize();
                }

                RockPage.PageTitle = _workflowType.Name;

                if ( !string.IsNullOrWhiteSpace( _workflowType.IconCssClass ) )
                {
                    lHeadingIcon.Text = string.Format( "<i class='{0}'></i>", _workflowType.IconCssClass );
                }
            }
            else
            {
                pnlWorkflowList.Visible = false;
            }
        }

        private void Actions_BulkUpdateClick( object sender, EventArgs e )
        {
            var items = gWorkflows.SelectedKeys.Select( s => ( int ) s ).ToList();
            if ( !items.Any() )
            {
                var dataKeyArray = gWorkflows.DataKeys;
                foreach ( DataKey dataKey in dataKeyArray )
                {
                    items.Add( ( int ) dataKey.Value );
                }
            }

            if ( !items.Any() )
            {
                return;
            }

            RockContext rockContext = new RockContext();
            EntitySetService entitySetService = new EntitySetService( rockContext );
            EntitySet entitySet = new EntitySet
            {
                EntityTypeId = EntityTypeCache.GetId( typeof( Rock.Model.Workflow ) ),
                ExpireDateTime = RockDateTime.Now.AddDays( 1 )
            };
            entitySetService.Add( entitySet );


            foreach ( var item in items )
            {
                var entitySetItem = new EntitySetItem()
                {
                    EntityId = item,
                };
                entitySet.Items.Add( entitySetItem );
            }
            rockContext.SaveChanges();
            NavigateToLinkedPage( "UpdatePage", new Dictionary<string, string> { { "EntitySetId", entitySet.Id.ToString() } } );
        }


        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                SetFilter();
                BindGrid();
            }
        }


        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState["AvailableAttributes"] = AvailableAttributes;

            return base.SaveViewState();
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
        }

        protected void gfWorkflows_DisplayFilterValue( object sender, Rock.Web.UI.Controls.GridFilter.DisplayFilterValueArgs e )
        {

            if ( AvailableAttributes != null )
            {
                var attribute = AvailableAttributes.FirstOrDefault( a => MakeKeyUniqueToType( a.Key ) == e.Key );
                if ( attribute != null )
                {
                    try
                    {
                        var values = JsonConvert.DeserializeObject<List<string>>( e.Value );
                        e.Value = attribute.FieldType.Field.FormatFilterValues( attribute.QualifierValues, values );
                        return;
                    }
                    catch { }
                }
            }

            if ( e.Key == MakeKeyUniqueToType( "Activated" ) )
            {
                e.Value = DateRangePicker.FormatDelimitedValues( e.Value );
            }
            else if ( e.Key == MakeKeyUniqueToType( "Completed" ) )
            {
                e.Value = DateRangePicker.FormatDelimitedValues( e.Value );
            }
            else if ( e.Key == MakeKeyUniqueToType( "Initiator" ) )
            {
                int? personId = e.Value.AsIntegerOrNull();
                if ( personId.HasValue )
                {
                    var person = new PersonService( new RockContext() ).Get( personId.Value );
                    if ( person != null )
                    {
                        e.Value = person.FullName;
                    }
                }
            }
            else if ( e.Key == MakeKeyUniqueToType( "Name" ) )
            {
                return;
            }
            else if ( e.Key == MakeKeyUniqueToType( "Status" ) )
            {
                return;
            }
            else if ( e.Key == MakeKeyUniqueToType( "State" ) )
            {
                return;
            }
            else
            {
                e.Value = string.Empty;
            }
        }

        protected void gfWorkflows_ApplyFilterClick( object sender, EventArgs e )
        {
            int? personId = ppInitiator.SelectedValue;

            if ( AvailableAttributes != null )
            {
                foreach ( var attribute in AvailableAttributes )
                {
                    var filterControl = phAttributeFilters.FindControl( "filter_" + attribute.Id.ToString() );
                    if ( filterControl != null )
                    {
                        try
                        {
                            var values = attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter );
                        }
                        catch { }
                    }
                }
            }

            BindGrid();
        }

        /// <summary>
        /// Handles the Add event of the gWorkflows control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void gWorkflows_Add( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "EntryPage", "WorkflowTypeId", _workflowType.Id );
        }

        /// <summary>
        /// Handles the Edit event of the gWorkflows control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gWorkflows_Manage( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "workflowId", e.RowKeyId );
        }


        /// <summary>
        /// Handles the GridRebind event of the gWorkflows control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void gWorkflows_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }


        #endregion

        #region Methods

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <returns></returns>
        private string GetState()
        {
            // Get the check box list values by evaluating the posted form values for each input item in the rendered checkbox list.  
            // This is required because of a bug in ASP.NET that results in the Selected property for CheckBoxList items to not be
            // set correctly on a postback.
            var selectedItems = new List<string>();
            for ( int i = 0; i < cblState.Items.Count; i++ )
            {
                string value = Request.Form[cblState.UniqueID + "$" + i.ToString()];
                if ( value != null )
                {
                    cblState.Items[i].Selected = true;
                    selectedItems.Add( value );
                }
                else
                {
                    cblState.Items[i].Selected = false;
                }
            }

            // no items were selected (not good)
            if ( !selectedItems.Any() )
            {
                return "None";
            }

            // Only one item was selected, return it's value
            if ( selectedItems.Count() == 1 )
            {
                return selectedItems[0];
            }

            // All items were selected, which is not technically a 'filter'
            return string.Empty;
        }

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void SetFilter()
        {
            BindAttributes();
            AddDynamicControls();
        }

        private void BindAttributes()
        {
            // Parse the attribute filters 
            AvailableAttributes = new List<AttributeCache>();
            if ( _workflowType != null )
            {
                int entityTypeId = new Rock.Model.Workflow().TypeId;
                string workflowQualifier = _workflowType.Id.ToString();
                foreach ( var attributeModel in new AttributeService( new RockContext() ).Queryable()
                    .Where( a =>
                        a.EntityTypeId == entityTypeId &&
                        a.IsGridColumn &&
                        a.EntityTypeQualifierColumn.Equals( "WorkflowTypeId", StringComparison.OrdinalIgnoreCase ) &&
                        a.EntityTypeQualifierValue.Equals( workflowQualifier ) )
                    .OrderByDescending( a => a.EntityTypeQualifierColumn )
                    .ThenBy( a => a.Order )
                    .ThenBy( a => a.Name ) )
                {
                    AvailableAttributes.Add( AttributeCache.Get( attributeModel ) );
                }
            }
        }

        /// <summary>
        /// Adds the attribute columns.
        /// </summary>
        private void AddDynamicControls()
        {
            // Clear the filter controls
            phAttributeFilters.Controls.Clear();

            // Remove attribute columns
            foreach ( var column in gWorkflows.Columns.OfType<AttributeField>().ToList() )
            {
                gWorkflows.Columns.Remove( column );
            }

            if ( AvailableAttributes != null )
            {
                foreach ( var attribute in AvailableAttributes )
                {
                    var control = attribute.FieldType.Field.FilterControl( attribute.QualifierValues, "filter_" + attribute.Id.ToString(), false, Rock.Reporting.FilterMode.SimpleFilter );
                    if ( control != null )
                    {
                        if ( control is IRockControl )
                        {
                            var rockControl = ( IRockControl ) control;
                            rockControl.Label = attribute.Name;
                            rockControl.Help = attribute.Description;
                            phAttributeFilters.Controls.Add( control );
                        }
                        else
                        {
                            var wrapper = new RockControlWrapper();
                            wrapper.ID = control.ID + "_wrapper";
                            wrapper.Label = attribute.Name;
                            wrapper.Controls.Add( control );
                            phAttributeFilters.Controls.Add( wrapper );
                        }
                    }


                    string dataFieldExpression = attribute.Key;
                    bool columnExists = gWorkflows.Columns.OfType<AttributeField>().FirstOrDefault( a => a.DataField.Equals( dataFieldExpression ) ) != null;
                    if ( !columnExists )
                    {
                        AttributeField boundField = new AttributeField();
                        boundField.DataField = dataFieldExpression;
                        boundField.AttributeId = attribute.Id;
                        boundField.HeaderText = attribute.Name;
                        boundField.Condensed = false;

                        var attributeCache = Rock.Web.Cache.AttributeCache.Get( attribute.Id );
                        if ( attributeCache != null )
                        {
                            boundField.ItemStyle.HorizontalAlign = attributeCache.FieldType.Field.AlignValue;
                        }

                        gWorkflows.Columns.Add( boundField );
                    }
                }
            }

            var dateField = new DateTimeField();
            gWorkflows.Columns.Add( dateField );
            dateField.DataField = "CreatedDateTime";
            dateField.SortExpression = "CreatedDateTime";
            dateField.HeaderText = "Created";
            dateField.FormatAsElapsedTime = true;

            var statusField = new BoundField();
            gWorkflows.Columns.Add( statusField );
            statusField.DataField = "Status";
            statusField.SortExpression = "Status";
            statusField.HeaderText = "Status";
            statusField.DataFormatString = "<span class='label label-info'>{0}</span>";
            statusField.HtmlEncode = false;

            var stateField = new CallbackField();
            gWorkflows.Columns.Add( stateField );
            stateField.DataField = "IsCompleted";
            stateField.SortExpression = "CompletedDateTime";
            stateField.HeaderText = "State";
            stateField.HtmlEncode = false;
            stateField.OnFormatDataValue += ( sender, e ) =>
            {
                if ( ( bool ) e.DataValue )
                {
                    e.FormattedValue = "<span class='label label-default'>Completed</span>";
                }
                else
                {
                    e.FormattedValue = "<span class='label label-success'>Active</span>";
                }
            };
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            if ( _workflowType != null )
            {
                pnlWorkflowList.Visible = true;

                var idCol = gWorkflows.ColumnsOfType<BoundField>().Where( c => c.DataField == "WorkflowId" ).FirstOrDefault();
                if ( idCol != null )
                {
                    idCol.Visible = !string.IsNullOrWhiteSpace( _workflowType.WorkflowIdPrefix );
                }

                var rockContext = new RockContext();
                var workflowService = new WorkflowService( rockContext );

                var qry = workflowService
                    .Queryable( "Activities.ActivityType,InitiatorPersonAlias.Person" ).AsNoTracking()
                    .Where( w => w.WorkflowTypeId.Equals( _workflowType.Id ) );

                // Activated Date Range Filter
                if ( drpActivated.LowerValue.HasValue )
                {
                    qry = qry.Where( w => w.ActivatedDateTime >= drpActivated.LowerValue.Value );
                }
                if ( drpActivated.UpperValue.HasValue )
                {
                    DateTime upperDate = drpActivated.UpperValue.Value.Date.AddDays( 1 );
                    qry = qry.Where( w => w.ActivatedDateTime.Value < upperDate );
                }

                // State Filter
                List<string> states = cblState.SelectedValues;
                if ( states.Count == 1 )    // Don't filter if none or both options are selected
                {
                    if ( states[0] == "Active" )
                    {
                        qry = qry.Where( w => !w.CompletedDateTime.HasValue );
                    }
                    else
                    {
                        qry = qry.Where( w => w.CompletedDateTime.HasValue );
                    }
                }

                // Completed Date Range Filter
                if ( drpCompleted.LowerValue.HasValue )
                {
                    qry = qry.Where( w => w.CompletedDateTime.HasValue && w.CompletedDateTime.Value >= drpCompleted.LowerValue.Value );
                }
                if ( drpCompleted.UpperValue.HasValue )
                {
                    DateTime upperDate = drpCompleted.UpperValue.Value.Date.AddDays( 1 );
                    qry = qry.Where( w => w.CompletedDateTime.HasValue && w.CompletedDateTime.Value < upperDate );
                }

                string name = tbName.Text;
                if ( !string.IsNullOrWhiteSpace( name ) )
                {
                    qry = qry.Where( w => w.Name.StartsWith( name ) );
                }

                int? personId = ppInitiator.SelectedValue;
                if ( personId.HasValue )
                {
                    qry = qry.Where( w => w.InitiatorPersonAlias.PersonId == personId.Value );
                }

                string status = tbStatus.Text;
                if ( !string.IsNullOrWhiteSpace( status ) )
                {
                    qry = qry.Where( w => w.Status.StartsWith( status ) );
                }

                // Filter query by any configured attribute filters
                if ( AvailableAttributes != null && AvailableAttributes.Any() )
                {
                    var attributeValueService = new AttributeValueService( rockContext );
                    var parameterExpression = attributeValueService.ParameterExpression;

                    foreach ( var attribute in AvailableAttributes )
                    {
                        var filterControl = phAttributeFilters.FindControl( "filter_" + attribute.Id.ToString() );
                        qry = attribute.FieldType.Field.ApplyAttributeQueryFilter( qry, filterControl, attribute, workflowService, Rock.Reporting.FilterMode.SimpleFilter );
                    }
                }

                IQueryable<Rock.Model.Workflow> workflows = null;

                var sortProperty = gWorkflows.SortProperty;
                if ( sortProperty != null )
                {
                    if ( sortProperty.Property == "Initiator" )
                    {
                        if ( sortProperty.Direction == SortDirection.Ascending )
                        {
                            workflows = qry
                                .OrderBy( w => w.InitiatorPersonAlias.Person.LastName )
                                .ThenBy( w => w.InitiatorPersonAlias.Person.NickName );
                        }
                        else
                        {
                            workflows = qry
                                .OrderByDescending( w => w.InitiatorPersonAlias.Person.LastName )
                                .ThenByDescending( w => w.InitiatorPersonAlias.Person.NickName );
                        }
                    }
                    else
                    {
                        workflows = qry.Sort( sortProperty );
                    }
                }
                else
                {
                    workflows = qry.OrderByDescending( s => s.CreatedDateTime );
                }

                // Since we're not binding to actual workflow list, but are using AttributeField columns,
                // we need to save the workflows into the grid's object list
                var workflowObjectQry = workflows;
                if ( gWorkflows.AllowPaging )
                {
                    workflowObjectQry = workflowObjectQry.Skip( gWorkflows.PageIndex * gWorkflows.PageSize ).Take( gWorkflows.PageSize );
                }

                gWorkflows.ObjectList = workflowObjectQry.ToList().ToDictionary( k => k.Id.ToString(), v => v as object );

                gWorkflows.EntityTypeId = EntityTypeCache.Get<Rock.Model.Workflow>().Id;
                var qryGrid = workflows.Select( w => new
                {
                    w.Id,
                    w.WorkflowId,
                    w.Name,
                    Initiator = w.InitiatorPersonAlias != null ? w.InitiatorPersonAlias.Person : null,
                    Activities = w.Activities.Where( a => a.ActivatedDateTime.HasValue && !a.CompletedDateTime.HasValue ).OrderBy( a => a.ActivityType.Order ).Select( a => a.ActivityType.Name ),
                    w.CreatedDateTime,
                    Status = w.Status,
                    IsCompleted = w.CompletedDateTime.HasValue
                } );

                gWorkflows.SetLinqDataSource( qryGrid );
                gWorkflows.DataBind();
            }
            else
            {
                pnlWorkflowList.Visible = false;
            }

        }

        private string MakeKeyUniqueToType( string key )
        {
            if ( _workflowType != null )
            {
                return string.Format( "{0}-{1}", _workflowType.Id, key );
            }
            return key;
        }

        #endregion

        protected void pWorkflowType_SelectItem( object sender, EventArgs e )
        {
            var workflowTypeId = pWorkflowType.SelectedValueAsInt();
            NavigateToCurrentPage( new Dictionary<string, string> { { "WorkflowTypeId", workflowTypeId.ToString() } } );
        }
    }
}