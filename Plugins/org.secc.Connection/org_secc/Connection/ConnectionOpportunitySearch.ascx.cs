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
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace org.secc.Connection
{
    [DisplayName( "Connection Opportunity Search" )]
    [Category( "SECC > Connection" )]
    [Description( "Allows users to search for an opportunity to join" )]

    [CodeEditorField( "Lava Template", "Lava template to use to display the list of opportunities.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, @"{% include '~~/Assets/Lava/OpportunitySearch.lava' %}", "", 2 )]
    [BooleanField( "Enable Debug", "Display a list of merge fields available for lava.", false, "", 3 )]
    [BooleanField( "Enable Campus Context", "If the page has a campus context it's value will be used as a filter", true )]
    [BooleanField( "Set Page Title", "Determines if the block should set the page title with the connection type name.", false )]
    [BooleanField( "Display Name Filter", "Display the name filter", false )]
    [BooleanField( "Display Campus Filter", "Display the campus filter", true )]
    [BooleanField( "Display Attribute Filters", "Display the attribute filters", true )]
    [LinkedPage( "Detail Page", "The page used to view a connection opportunity." )]
    [IntegerField( "Connection Type Id", "The Id of the connection type whose opportunities are displayed.", true, 1 )]

    public partial class OpportunitySearch : Rock.Web.UI.RockBlock
    {
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

            SetFilters( false );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
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
                SetFilters( true );
                UpdateList();
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
            UpdateList();
        }

        /// <summary>
        /// Handles the Click event of the btnSearch control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSearch_Click( object sender, EventArgs e )
        {
            UpdateList();
            ScriptManager.RegisterStartupScript( upnlContent, upnlContent.GetType(), "Connections", "registerCheckboxHandler()", true );
        }


        #endregion

        #region Internal Methods

        /// <summary>
        /// Updates the list.
        /// </summary>
        private void UpdateList()
        {
            using ( var rockContext = new RockContext() )
            {
                var searchSelections = new Dictionary<string, string>();

                var connectionTypeId = GetAttributeValue( "ConnectionTypeId" ).AsInteger();
                var connectionType = new ConnectionTypeService( rockContext ).Get( connectionTypeId );
                var connectionOpportunityService = new ConnectionOpportunityService( rockContext );

                var qrySearch = connectionOpportunityService.Queryable().Where( a => a.ConnectionTypeId == connectionTypeId && a.IsActive == true ).ToList();

                if ( GetAttributeValue( "DisplayNameFilter" ).AsBoolean() )
                {
                    if ( !string.IsNullOrWhiteSpace( tbSearchName.Text ) )
                    {
                        searchSelections.Add( "tbSearchName", tbSearchName.Text );
                        var searchTerms = tbSearchName.Text.ToLower().SplitDelimitedValues( true );
                        qrySearch = qrySearch.Where( o => searchTerms.Any( t => t.Contains( o.Name.ToLower() ) || o.Name.ToLower().Contains( t ) ) ).ToList();
                    }
                }

                if ( GetAttributeValue( "DisplayCampusFilter" ).AsBoolean() )
                {
                    var searchCampuses = cblCampus.SelectedValuesAsInt;
                    if ( searchCampuses.Count > 0 )
                    {
                        searchSelections.Add( "cblCampus", searchCampuses.AsDelimited( "|" ) );
                        qrySearch = qrySearch.Where( o => o.ConnectionOpportunityCampuses.Any( c => searchCampuses.Contains( c.CampusId ) ) ).ToList();
                    }
                }

                if ( GetAttributeValue( "DisplayAttributeFilters" ).AsBoolean() )
                {
                    // Filter query by any configured attribute filters
                    if ( AvailableAttributes != null && AvailableAttributes.Any() )
                    {
                        var attributeValueService = new AttributeValueService( rockContext );
                        var parameterExpression = attributeValueService.ParameterExpression;

                        foreach ( var attribute in AvailableAttributes )
                        {
                            string filterControlId = "filter_" + attribute.Id.ToString();
                            var filterControl = phAttributeFilters.FindControl( filterControlId );
                            if ( filterControl != null )
                            {
                                var filterValues = attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter );
                                var expression = attribute.FieldType.Field.AttributeFilterExpression( attribute.QualifierValues, filterValues, parameterExpression );
                                if ( expression != null )
                                {
                                    searchSelections.Add( filterControlId, filterValues.ToJson() );
                                    var attributeValues = attributeValueService
                                        .Queryable()
                                        .Where( v => v.Attribute.Id == attribute.Id );

                                    attributeValues = attributeValues.Where( parameterExpression, expression, null );

                                    qrySearch = qrySearch.Where( o => attributeValues.Select( v => v.EntityId ).Contains( o.Id ) ).ToList();
                                }
                            }
                        }
                    }
                }

                string sessionKey = string.Format( "ConnectionSearch_{0}", this.BlockId );
                Session[sessionKey] = searchSelections;

                var opportunities = qrySearch.OrderBy( s => s.PublicName ).ToList();

                var mergeFields = new Dictionary<string, object>();
                mergeFields.Add( "CurrentPerson", CurrentPerson );
                mergeFields.Add( "Campuses", CampusCache.All() );
                mergeFields.Add( "CampusContext", RockPage.GetCurrentContext( EntityTypeCache.Get( "Rock.Model.Campus" ) ) as Campus );
                var pageReference = new PageReference( GetAttributeValue( "DetailPage" ), null );
                mergeFields.Add( "DetailPage", BuildDetailPageUrl( pageReference.BuildUrl() ) );

                // iterate through the opportunities and lava merge the summaries and descriptions
                foreach ( var opportunity in opportunities )
                {
                    opportunity.Summary = opportunity.Summary.ResolveMergeFields( mergeFields );
                    opportunity.Description = opportunity.Description.ResolveMergeFields( mergeFields );
                }

                mergeFields.Add( "Opportunities", opportunities );

                lOutput.Text = GetAttributeValue( "LavaTemplate" ).ResolveMergeFields( mergeFields );

                if ( GetAttributeValue( "SetPageTitle" ).AsBoolean() )
                {
                    string pageTitle = "Connection";
                    RockPage.PageTitle = pageTitle;
                    RockPage.BrowserTitle = String.Format( "{0} | {1}", pageTitle, RockPage.Site.Name );
                    RockPage.Header.Title = String.Format( "{0} | {1}", pageTitle, RockPage.Site.Name );
                }

                // show debug info
                if ( GetAttributeValue( "EnableDebug" ).AsBoolean() && IsUserAuthorized( Authorization.EDIT ) )
                {
                    lDebug.Visible = true;
                    lDebug.Text = mergeFields.lavaDebugInfo();
                }
            }
        }

        /// <summary>
        /// Builds the detail page URL. This is needed so that it can pass along any url paramters that are in the
        /// query string.
        /// </summary>
        /// <param name="detailPage">The detail page.</param>
        /// <returns></returns>
        private string BuildDetailPageUrl( string detailPage )
        {
            StringBuilder sbUrlParms = new StringBuilder();
            foreach ( var parm in this.RockPage.PageParameters() )
            {
                if ( parm.Key != "PageId" )
                {
                    if ( sbUrlParms.Length > 0 )
                    {
                        sbUrlParms.Append( string.Format( "&{0}={1}", parm.Key, parm.Value.ToString() ) );
                    }
                    else
                    {
                        sbUrlParms.Append( string.Format( "?{0}={1}", parm.Key, parm.Value.ToString() ) );
                    }
                }
            }

            return detailPage + sbUrlParms.ToString();
        }

        /// <summary>
        /// Sets the filters.
        /// </summary>
        private void SetFilters( bool setValues )
        {
            using ( var rockContext = new RockContext() )
            {
                string sessionKey = string.Format( "ConnectionSearch_{0}", this.BlockId );
                var searchSelections = Session[sessionKey] as Dictionary<string, string>;
                setValues = setValues && searchSelections != null;

                var connectionType = new ConnectionTypeService( rockContext ).Get( GetAttributeValue( "ConnectionTypeId" ).AsInteger() );

                if ( !GetAttributeValue( "DisplayNameFilter" ).AsBoolean() )
                {
                    tbSearchName.Visible = false;
                }

                if ( GetAttributeValue( "DisplayCampusFilter" ).AsBoolean() )
                {
                    cblCampus.Visible = true;
                    cblCampus.DataSource = CampusCache.All().Where( c => c.AttributeValues["Public"].ToString().AsBoolean() == true );
                    cblCampus.DataBind();

                }
                else
                {
                    cblCampus.Visible = false;
                }

                if ( setValues )
                {
                    if ( searchSelections.ContainsKey( "tbSearchName" ) )
                    {
                        tbSearchName.Text = searchSelections["tbSearchName"];
                    }
                    if ( searchSelections.ContainsKey( "cblCampus" ) )
                    {
                        var selectedItems = searchSelections["cblCampus"].SplitDelimitedValues().AsIntegerList();
                        cblCampus.SetValues( selectedItems );
                    }
                }
                else if ( GetAttributeValue( "EnableCampusContext" ).AsBoolean() )
                {
                    var campusEntityType = EntityTypeCache.Get( "Rock.Model.Campus" );
                    var contextCampus = RockPage.GetCurrentContext( campusEntityType ) as Campus;

                    if ( contextCampus != null )
                    {
                        cblCampus.SetValue( contextCampus.Id.ToString() );
                    }
                }

                if ( GetAttributeValue( "DisplayAttributeFilters" ).AsBoolean() )
                {
                    // Parse the attribute filters 
                    AvailableAttributes = new List<AttributeCache>();
                    if ( connectionType != null )
                    {
                        int entityTypeId = new ConnectionOpportunity().TypeId;
                        foreach ( var attributeModel in new AttributeService( new RockContext() ).Queryable()
                            .Where( a =>
                                a.EntityTypeId == entityTypeId &&
                                a.EntityTypeQualifierColumn.Equals( "ConnectionTypeId", StringComparison.OrdinalIgnoreCase ) &&
                                a.EntityTypeQualifierValue.Equals( connectionType.Id.ToString() ) &&
                                a.AllowSearch == true )
                            .OrderBy( a => a.Order )
                            .ThenBy( a => a.Name ) )
                        {
                            AvailableAttributes.Add( AttributeCache.Get( attributeModel ) );
                        }
                    }

                    // Clear the filter controls
                    phAttributeFilters.Controls.Clear();

                    if ( AvailableAttributes != null )
                    {
                        foreach ( var attribute in AvailableAttributes )
                        {
                            string controlId = "filter_" + attribute.Id.ToString();
                            var control = attribute.FieldType.Field.FilterControl( attribute.QualifierValues, controlId, false, Rock.Reporting.FilterMode.SimpleFilter );
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

                                if ( setValues && searchSelections.ContainsKey( controlId ) )
                                {
                                    var values = searchSelections[controlId].FromJsonOrNull<List<string>>();
                                    attribute.FieldType.Field.SetFilterValues( control, attribute.QualifierValues, values );
                                }
                            }
                        }
                    }
                }
                else
                {
                    phAttributeFilters.Visible = false;
                }
            }
        }

        protected override void OnPreRender( EventArgs e )
        {
            base.OnPreRender( e );
            var campuses = CampusCache.All();
            foreach ( ListItem campus in cblCampus.Items )
            {
                var campusObj = campuses.Where( c => c.Id.ToString() == campus.Value ).FirstOrDefault();
                campus.Text = "<strong><span class=\"" + campusObj.AttributeValues["Slug"] + " text\">" + campusObj.Name.ToUpper() + "</span></strong>";

            }
        }
        #endregion
    }
}