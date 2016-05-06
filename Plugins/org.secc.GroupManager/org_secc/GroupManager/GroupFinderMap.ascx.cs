using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Caching;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using DotLiquid;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Field.Types;
using Rock.Model;
using Rock.Security;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.GroupManager
{
    /// <summary>
    /// Block for people to find a group that matches their search parameters.
    /// </summary>
    [DisplayName( "Group Finder Map" )]
    [Category( "Groups" )]
    [Description( "Block for people to find a group that matches their search parameters." )]

    // Linked Pages
    [LinkedPage( "Group Detail Page", "The page to navigate to for group details.", false, "", "CustomSetting" )]
    [LinkedPage( "Register Page", "The page to navigate to when registering for a group.", false, "", "CustomSetting" )]

    // Filter Settings
    [GroupTypeField( "Group Type", "", true, "", "CustomSetting" )]
    [GroupField( "Group Parent", "", true, "", "CustomSetting" )]
    [GroupTypeField( "Geofenced Group Type", "", false, "", "CustomSetting" )]
    [TextField( "ScheduleFilters", "", false, "", "CustomSetting" )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP, "Attribute Filters", "", false, true, "", "CustomSetting" )]
    [IntegerField( "Max Results", "Maximum number of results to display. 0 is no filter", false, 0, "CustomSetting" )]
    [BooleanField( "Pre Fill", "Pre fill loged in users email", true, "CustomSetting" )]
    [BooleanField( "Hide Full", "Hide groups that have reached their capacity?", true, "CustomSetting" )]
    [BooleanField( "Show Reset", "Display Reset Button", true, "CustomSetting" )]

    // Map Settings
    [BooleanField( "Large Map", "Show a full width map", false, "CustomSetting" )]
    [BooleanField( "Show Map", "", false, "CustomSetting" )]
    [BooleanField( "Show Families", "Show families on map", false, "CustomSetting" )]
    [TextField( "Search Icon", "URL of marker for searched location", false, "", "CustomSetting" )]
    [TextField( "Group Icon", "URL of marker for searched location", false, "", "CustomSetting" )]
    [TextField( "Family Icon", "URL of marker for searched location", false, "", "CustomSetting" )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.MAP_STYLES, "Map Style", "", true, false, Rock.SystemGuid.DefinedValue.MAP_STYLE_GOOGLE, "CustomSetting" )]
    [IntegerField( "Map Height", "", false, 600, "CustomSetting" )]
    [BooleanField( "Show Fence", "", false, "CustomSetting" )]
    [ValueListField( "Ranges", "", false, "1|5|10", "1", null, null, "CustomSetting" )]
    [ValueListField( "Polygon Colors", "", false, "#f37833|#446f7a|#afd074|#649dac|#f8eba2|#92d0df|#eaf7fc", "#ffffff", null, null, "CustomSetting" )]
    [CodeEditorField( "Map Info", "", CodeEditorMode.Lava, CodeEditorTheme.Rock, 200, false, @"
<h4 class='margin-t-none'>{{ Group.Name }}</h4> 

<div class='margin-b-sm'>
{% for attribute in Group.AttributeValues %}
    <strong>{{ attribute.AttributeName }}:</strong> {{ attribute.ValueFormatted }} <br />
{% endfor %}
</div>

<div class='margin-v-sm'>
{% if Location.FormattedHtmlAddress && Location.FormattedHtmlAddress != '' %}
	{{ Location.FormattedHtmlAddress }}
{% endif %}
</div>

{% if LinkedPages.GroupDetailPage != '' %}
    <a class='btn btn-xs btn-action margin-r-sm' href='{{ LinkedPages.GroupDetailPage }}?GroupId={{ Group.Id }}'>View {{ Group.GroupType.GroupTerm }}</a>
{% endif %}

{% if LinkedPages.RegisterPage != '' %}
    {% if LinkedPages.RegisterPage contains '?' %}
        <a class='btn btn-xs btn-action' href='{{ LinkedPages.RegisterPage }}&GroupId={{ Group.Id }}'>Register</a>
    {% else %}
        <a class='btn btn-xs btn-action' href='{{ LinkedPages.RegisterPage }}?GroupId={{ Group.Id }}'>Register</a>
    {% endif %}
{% endif %}
", "CustomSetting" )]

    [CodeEditorField( "Family Info", "", CodeEditorMode.Lava, CodeEditorTheme.Rock, 200, false, @"", "CustomSetting" )]
    [BooleanField( "Map Info Debug", "", false, "CustomSetting" )]

    // Lava Output Settings
    [CodeEditorField( "Message", "", CodeEditorMode.Lava, CodeEditorTheme.Rock, 200, false, @"
", "CustomSetting" )]
    [BooleanField( "Show Lava Output", "", false, "CustomSetting" )]
    [CodeEditorField( "Lava Output", "", CodeEditorMode.Lava, CodeEditorTheme.Rock, 200, false, @"
", "CustomSetting" )]
    [CodeEditorField( "Groupless Message", "", CodeEditorMode.Lava, CodeEditorTheme.Rock, 200, false, @"
", "CustomSetting" )]
    [BooleanField( "Lava Output Debug", "", false, "CustomSetting" )]

    // Grid Settings
    [BooleanField( "Show Grid", "", false, "CustomSetting" )]
    [BooleanField( "Show Schedule", "", false, "CustomSetting" )]
    [BooleanField( "Show Proximity", "", true, "CustomSetting" )]
    [BooleanField( "Show Count", "", false, "CustomSetting" )]
    [BooleanField( "Show Age", "", false, "CustomSetting" )]
    [BooleanField( "Show Description", "", true, "CustomSetting" )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP, "Attribute Columns", "", false, true, "", "CustomSetting" )]
    [BooleanField( "Sort By Distance", "", true, "CustomSetting" )]
    [TextField( "Page Sizes", "To show a dropdown of page sizes, enter a comma delimited list of page sizes. For example: 10,20 will present a drop down with 10,20,All as options with the default as 10", false, "", "CustomSetting" )]

    //FamilyGrid Settings
    [BooleanField( "ShowFamilyGrid", "", false, "CustomSetting" )]

    public partial class GroupFinderMap : RockBlockCustomSettings
    {

        #region Private Variables
        private Guid _targetPersonGuid = Guid.Empty;
        Dictionary<string, string> _urlParms = new Dictionary<string, string>();
        #endregion

        #region Properties

        /// <summary>
        /// Gets the settings tool tip.
        /// </summary>
        /// <value>
        /// The settings tool tip.
        /// </value>
        public override string SettingsToolTip
        {
            get
            {
                return "Edit Settings";
            }
        }

        /// <summary>
        /// Gets or sets the attribute filters.
        /// </summary>
        /// <value>
        /// The attribute filters.
        /// </value>
        public List<AttributeCache> AttributeFilters { get; set; }

        /// <summary>
        /// Gets or sets the _ attribute columns.
        /// </summary>
        /// <value>
        /// The _ attribute columns.
        /// </value>
        public List<AttributeCache> AttributeColumns { get; set; }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            AttributeFilters = ViewState["AttributeFilters"] as List<AttributeCache>;
            AttributeColumns = ViewState["AttributeColumns"] as List<AttributeCache>;

            BuildDynamicControls();

        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gGroups.DataKeyNames = new string[] { "Id" };
            gGroups.Actions.ShowAdd = false;
            gGroups.GridRebind += gGroups_GridRebind;
            gGroups.ShowActionRow = false;
            gGroups.AllowPaging = false;

            this.BlockUpdated += Block_Updated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            this.LoadGoogleMapsApi();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            nbNotice.Visible = false;

            if ( Request["PersonGuid"] != null )
            {
                Guid.TryParse( Request["PersonGuid"].ToString(), out _targetPersonGuid );
                _urlParms.Add( "PersonGuid", _targetPersonGuid.ToString() );
            }

            if ( !Page.IsPostBack )
            {
                BindAttributes();
                BuildDynamicControls();
                DisplayMessage();

                if ( _targetPersonGuid != Guid.Empty )
                {
                    ShowViewForPerson( _targetPersonGuid );
                }
                else
                {
                    ShowView();
                }
            }
            else
            {
                if ( hfDidSearch.Value.AsBoolean() )
                {
                    ShowResults();
                }
            }
        }

        private void DisplayMessage()
        {
            string template = GetAttributeValue( "Message" );

            var mergeFields = new Dictionary<string, object>();

            lMessage.Text = template.ResolveMergeFields( mergeFields );
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState["AttributeFilters"] = AttributeFilters;
            ViewState["AttributeColumns"] = AttributeColumns;

            return base.SaveViewState();
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the ContentDynamic control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        void Block_Updated( object sender, EventArgs e )
        {
            ShowView();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the gtpGroupType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gtpGroupType_SelectedIndexChanged( object sender, EventArgs e )
        {
            SetGroupTypeOptions();
        }

        /// <summary>
        /// Handles the Click event of the lbSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSave_Click( object sender, EventArgs e )
        {
            if ( !Page.IsValid )
            {
                return;
            }

            SetAttributeValue( "GroupType", GetGroupTypeGuid( gtpGroupType.SelectedGroupTypeId ) );
            SetAttributeValue( "GroupParent", gpGroupParent.SelectedValue );
            SetAttributeValue( "GeofencedGroupType", GetGroupTypeGuid( gtpGeofenceGroupType.SelectedGroupTypeId ) );
            if ( cblSchedule.Visible )
            {
                SetAttributeValue( "ScheduleFilters", cblSchedule.Items.Cast<ListItem>().Where( i => i.Selected ).Select( i => i.Value ).ToList().AsDelimited( "," ) );
            }
            else
            {
                SetAttributeValue( "ScheduleFilters", string.Empty );
            }

            SetAttributeValue( "AttributeFilters", cblAttributes.Items.Cast<ListItem>().Where( i => i.Selected ).Select( i => i.Value ).ToList().AsDelimited( "," ) );
            SetAttributeValue( "MaxResults", nudMaxResults.Value.ToString() );
            SetAttributeValue( "PreFill", cbPreFill.Checked.ToString() );
            SetAttributeValue( "HideFull", cbHideFull.Checked.ToString() );
            SetAttributeValue( "ShowReset", cbShowReset.Checked.ToString() );
            SetAttributeValue( "ShowMap", cbShowMap.Checked.ToString() );
            SetAttributeValue( "ShowFamilies", cbShowFamilies.Checked.ToString() );
            SetAttributeValue( "MapStyle", ddlMapStyle.SelectedValue );
            SetAttributeValue( "LargeMap", cbLargeMap.Checked.ToString() );
            SetAttributeValue( "MapHeight", nbMapHeight.Text );
            SetAttributeValue( "SearchIcon", tbSearchIcon.Text );
            SetAttributeValue( "GroupIcon", tbGroupIcon.Text );
            SetAttributeValue( "FamilyIcon", tbFamilyIcon.Text );
            SetAttributeValue( "ShowFence", cbShowFence.Checked.ToString() );
            SetAttributeValue( "PolygonColors", vlPolygonColors.Value );
            SetAttributeValue( "Ranges", vlRanges.Value );
            SetAttributeValue( "MapInfo", ceMapInfo.Text );
            SetAttributeValue( "FamilyInfo", ceFamilyInfo.Text );
            SetAttributeValue( "MapInfoDebug", cbMapInfoDebug.Checked.ToString() );

            SetAttributeValue( "Message", ceMessage.Text );
            SetAttributeValue( "ShowLavaOutput", cbShowLavaOutput.Checked.ToString() );
            SetAttributeValue( "LavaOutput", ceLavaOutput.Text );
            SetAttributeValue( "GrouplessMessage", ceGrouplessMessage.Text );
            SetAttributeValue( "LavaOutputDebug", cbLavaOutputDebug.Checked.ToString() );

            SetAttributeValue( "ShowGrid", cbShowGrid.Checked.ToString() );
            SetAttributeValue( "ShowSchedule", cbShowSchedule.Checked.ToString() );
            SetAttributeValue( "ShowDescription", cbShowDescription.Checked.ToString() );
            SetAttributeValue( "ShowProximity", cbProximity.Checked.ToString() );
            SetAttributeValue( "SortByDistance", cbSortByDistance.Checked.ToString() );
            SetAttributeValue( "PageSizes", tbPageSizes.Text );
            SetAttributeValue( "ShowCount", cbShowCount.Checked.ToString() );
            SetAttributeValue( "ShowAge", cbShowAge.Checked.ToString() );
            SetAttributeValue( "AttributeColumns", cblGridAttributes.Items.Cast<ListItem>().Where( i => i.Selected ).Select( i => i.Value ).ToList().AsDelimited( "," ) );
            SetAttributeValue( "ShowFamilyGrid", cbFamilyGrid.Checked.ToString() );

            var ppFieldType = new PageReferenceFieldType();
            SetAttributeValue( "GroupDetailPage", ppFieldType.GetEditValue( ppGroupDetailPage, null ) );
            SetAttributeValue( "RegisterPage", ppFieldType.GetEditValue( ppRegisterPage, null ) );

            SaveAttributeValues();

            mdEdit.Hide();
            pnlEditModal.Visible = false;
            upnlContent.Update();

            BindAttributes();
            BuildDynamicControls();
            ShowView();
        }

        /// <summary>
        /// Handles the Click event of the btnSearch control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSearch_Click( object sender, EventArgs e )
        {
            ShowResults();
        }

        /// <summary>
        /// Handles the Click event of the btnClear control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnClear_Click( object sender, EventArgs e )
        {
            acAddress.SetValues( null );
            BuildDynamicControls();

            pnlMap.Visible = false;
            pnlLavaOutput.Visible = false;
            pnlGrid.Visible = false;

            hfDidSearch.Value = "False";
        }

        /// <summary>
        /// Handles the RowSelected event of the gGroups control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gGroups_RowSelected( object sender, RowEventArgs e )
        {
            if ( !NavigateToLinkedPage( "GroupDetailPage", "GroupId", e.RowKeyId ) )
            {
                ShowResults();
            }
        }

        /// <summary>
        /// Handles the Click event of the registerColumn control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        void registerColumn_Click( object sender, RowEventArgs e )
        {
            _urlParms.Add( "GroupId", e.RowKeyId.ToString() );
            if ( !NavigateToLinkedPage( "RegisterPage", _urlParms ) )
            {
                ShowResults();
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the gGroups control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        void gGroups_GridRebind( object sender, EventArgs e )
        {
            ShowResults();
        }


        #endregion

        #region Internal Methods

        /// <summary>
        /// Shows the settings.
        /// </summary>
        protected override void ShowSettings()
        {
            pnlEditModal.Visible = true;
            upnlContent.Update();
            mdEdit.Show();

            var rockContext = new RockContext();
            var groupTypes = new GroupTypeService( rockContext )
                .Queryable().AsNoTracking().ToList();

            BindGroupType( gtpGroupType, groupTypes, "GroupType" );
            BindGroupType( gtpGeofenceGroupType, groupTypes, "GeofencedGroupType" );

            gpGroupParent.SetValue( GetAttributeValue( "GroupParent" ).AsInteger() );

            string scheduleFilters = GetAttributeValue( "ScheduleFilters" );
            if ( !string.IsNullOrEmpty( scheduleFilters ) )
            {
                foreach ( string val in scheduleFilters.SplitDelimitedValues() )
                {
                    var li = cblSchedule.Items.FindByValue( val );
                    if ( li != null )
                    {
                        li.Selected = true;
                    }
                }
            }

            SetGroupTypeOptions();
            foreach ( string attr in GetAttributeValue( "AttributeFilters" ).SplitDelimitedValues() )
            {
                var li = cblAttributes.Items.FindByValue( attr );
                if ( li != null )
                {
                    li.Selected = true;
                }
            }
            nudMaxResults.Value = GetAttributeValue( "MaxResults" ).AsInteger();
            cbPreFill.Checked = GetAttributeValue( "PreFill" ).AsBoolean();
            cbHideFull.Checked = GetAttributeValue( "HideFull" ).AsBoolean();
            cbShowReset.Checked = GetAttributeValue( "ShowReset" ).AsBoolean();
            cbShowMap.Checked = GetAttributeValue( "ShowMap" ).AsBoolean();
            cbShowFamilies.Checked = GetAttributeValue( "ShowFamilies" ).AsBoolean();
            ddlMapStyle.BindToDefinedType( DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.MAP_STYLES.AsGuid() ) );
            cbLargeMap.Checked = GetAttributeValue( "LargeMap" ).AsBoolean();
            ddlMapStyle.SetValue( GetAttributeValue( "MapStyle" ) );
            nbMapHeight.Text = GetAttributeValue( "MapHeight" );
            tbSearchIcon.Text = GetAttributeValue( "SearchIcon" );
            tbGroupIcon.Text = GetAttributeValue( "GroupIcon" );
            tbFamilyIcon.Text = GetAttributeValue( "FamilyIcon" );
            cbShowFence.Checked = GetAttributeValue( "ShowFence" ).AsBoolean();
            vlPolygonColors.Value = GetAttributeValue( "PolygonColors" );
            vlRanges.Value = GetAttributeValue( "Ranges" );
            ceMapInfo.Text = GetAttributeValue( "MapInfo" );
            ceFamilyInfo.Text = GetAttributeValue( "FamilyInfo" );
            cbMapInfoDebug.Checked = GetAttributeValue( "MapInfoDebug" ).AsBoolean();

            ceMessage.Text = GetAttributeValue( "Message" );
            cbShowLavaOutput.Checked = GetAttributeValue( "ShowLavaOutput" ).AsBoolean();
            ceLavaOutput.Text = GetAttributeValue( "LavaOutput" );
            ceGrouplessMessage.Text = GetAttributeValue( "GrouplessMessage" );
            cbLavaOutputDebug.Checked = GetAttributeValue( "LavaOutputDebug" ).AsBoolean();

            cbShowGrid.Checked = GetAttributeValue( "ShowGrid" ).AsBoolean();
            cbShowSchedule.Checked = GetAttributeValue( "ShowSchedule" ).AsBoolean();
            cbShowDescription.Checked = GetAttributeValue( "ShowDescription" ).AsBoolean();
            cbProximity.Checked = GetAttributeValue( "ShowProximity" ).AsBoolean();
            cbSortByDistance.Checked = GetAttributeValue( "SortByDistance" ).AsBoolean();
            tbPageSizes.Text = GetAttributeValue( "PageSizes" );
            cbShowCount.Checked = GetAttributeValue( "ShowCount" ).AsBoolean();
            cbShowAge.Checked = GetAttributeValue( "ShowAge" ).AsBoolean();
            foreach ( string attr in GetAttributeValue( "AttributeColumns" ).SplitDelimitedValues() )
            {
                var li = cblGridAttributes.Items.FindByValue( attr );
                if ( li != null )
                {
                    li.Selected = true;
                }
            }

            cbFamilyGrid.Checked = GetAttributeValue( "ShowFamilyGrid" ).AsBoolean();

            var ppFieldType = new PageReferenceFieldType();
            ppFieldType.SetEditValue( ppGroupDetailPage, null, GetAttributeValue( "GroupDetailPage" ) );
            ppFieldType.SetEditValue( ppRegisterPage, null, GetAttributeValue( "RegisterPage" ) );

            upnlContent.Update();
        }

        /// <summary>
        /// Binds the group attribute list.
        /// </summary>
        private void SetGroupTypeOptions()
        {
            cblSchedule.Visible = false;

            // Rebuild the checkbox list settings for both the filter and display in grid attribute lists
            cblAttributes.Items.Clear();
            cblGridAttributes.Items.Clear();

            if ( gtpGroupType.SelectedGroupTypeId.HasValue )
            {
                var groupType = GroupTypeCache.Read( gtpGroupType.SelectedGroupTypeId.Value );
                if ( groupType != null )
                {
                    cblSchedule.Visible = ( groupType.AllowedScheduleTypes & ScheduleType.Weekly ) == ScheduleType.Weekly;

                    var group = new Group();
                    group.GroupTypeId = groupType.Id;
                    group.LoadAttributes();
                    foreach ( var attribute in group.Attributes )
                    {
                        cblAttributes.Items.Add( new ListItem( attribute.Value.Name, attribute.Value.Guid.ToString() ) );
                        cblGridAttributes.Items.Add( new ListItem( attribute.Value.Name, attribute.Value.Guid.ToString() ) );
                    }
                }
            }

            cblAttributes.Visible = cblAttributes.Items.Count > 0;
            cblGridAttributes.Visible = cblAttributes.Items.Count > 0;
        }

        private void ShowViewForPerson( Guid targetPersonGuid )
        {
            // check for a specific person in the query string
            Person targetPerson = null;
            Location targetPersonLocation = null;

            targetPerson = new PersonService( new RockContext() ).Queryable().Where( p => p.Guid == targetPersonGuid ).FirstOrDefault();
            targetPersonLocation = targetPerson.GetHomeLocation();

            if ( targetPerson != null )
            {
                lTitle.Text = String.Format( "<h4 class='margin-t-none'>Groups for {0}</h4>", targetPerson.FullName );
                acAddress.SetValues( targetPersonLocation );
                acAddress.Visible = false;
                pnlMessage.Visible = false;
                btnSearch.Visible = false;
                btnClear.Visible = false;

                if ( targetPersonLocation.GeoPoint != null )
                {
                    lTitle.Text += String.Format( "<p>Search based on: {0}</p>", targetPersonLocation.ToString() );

                    ShowResults();
                }
                else
                {
                    lTitle.Text += String.Format( "<p>The position of the address on file ({0}) could not be determined.</p>", targetPersonLocation.ToString() );
                }
            }

        }


        /// <summary>
        /// Shows the view.
        /// </summary>
        private void ShowView()
        {
            // If the groups should be limited by geofence, or the distance should be displayed,
            // then we need to capture the person's address
            acAddress.Visible = true;
            pnlMessage.Visible = true;
            pnlSearch.Visible = true;
            pnlMap.Visible = false;
            pnlResults.Visible = false;

            if ( CurrentPerson != null && GetAttributeValue( "PreFill" ).AsBoolean() )
            {
                acAddress.SetValues( CurrentPerson.GetHomeLocation() );
            }

            btnSearch.Visible = true;

            ddlRange.Items.Clear();
            var ranges = GetAttributeValue( "Ranges" ).Split( '|' ).Where<string>( s => !string.IsNullOrEmpty( s ) ).ToList();
            foreach ( var range in ranges )
            {
                ddlRange.Items.Add( new ListItem( range + ( range == "1" ? " Mile" : " Miles" ), range ) );
            }

            btnClear.Visible = btnSearch.Visible;

            // If we've already displayed results, then re-display them
            if ( pnlResults.Visible )
            {
                ShowResults();
            }

            if ( GetAttributeValue( "LargeMap" ).AsBoolean() )
            {
                pnlMap.CssClass = "margin-v-sm col-md-12";
                pnlGrid.CssClass = "margin-v-sm col-md-12";
                pnlFamilyGrid.CssClass = "margin-v-sm col-md-12";
            }

            btnReset.Visible = GetAttributeValue( "ShowReset" ).AsBoolean();
        }

        /// <summary>
        /// Adds the attribute filters.
        /// </summary>
        private void BindAttributes()
        {
            // Parse the attribute filters 
            AttributeFilters = new List<AttributeCache>();
            foreach ( string attr in GetAttributeValue( "AttributeFilters" ).SplitDelimitedValues() )
            {
                Guid? attributeGuid = attr.AsGuidOrNull();
                if ( attributeGuid.HasValue )
                {
                    var attribute = AttributeCache.Read( attributeGuid.Value );
                    if ( attribute != null )
                    {
                        AttributeFilters.Add( attribute );
                    }
                }
            }

            // Parse the attribute filters 
            AttributeColumns = new List<AttributeCache>();
            foreach ( string attr in GetAttributeValue( "AttributeColumns" ).SplitDelimitedValues() )
            {
                Guid? attributeGuid = attr.AsGuidOrNull();
                if ( attributeGuid.HasValue )
                {
                    var attribute = AttributeCache.Read( attributeGuid.Value );
                    if ( attribute != null )
                    {
                        AttributeColumns.Add( attribute );
                    }
                }
            }
        }

        /// <summary>
        /// Builds the dynamic controls.
        /// </summary>
        private void BuildDynamicControls()
        {
            // Clear attribute filter controls and recreate
            phFilterControls.Controls.Clear();
            string ScheduleFilters = GetAttributeValue( "ScheduleFilters" );
            if ( !string.IsNullOrEmpty( ScheduleFilters ) )
            {
                if ( ScheduleFilters.Contains( "Day" ) )
                {
                    var control = FieldTypeCache.Read( Rock.SystemGuid.FieldType.DAY_OF_WEEK ).Field.FilterControl( null, "filter_dow", false, Rock.Reporting.FilterMode.SimpleFilter );
                    AddFilterControl( control, "Day of Week", "The day of week that group meets on." );
                }

                if ( ScheduleFilters.Contains( "Time" ) )
                {
                    var control = FieldTypeCache.Read( Rock.SystemGuid.FieldType.TIME ).Field.FilterControl( null, "filter_time", false, Rock.Reporting.FilterMode.SimpleFilter );
                    AddFilterControl( control, "Time of Day", "The time of day that group meets." );
                }
            }

            if ( AttributeFilters != null )
            {
                foreach ( var attribute in AttributeFilters )
                {
                    var control = attribute.FieldType.Field.FilterControl( attribute.QualifierValues, "filter_" + attribute.Id.ToString(), false, Rock.Reporting.FilterMode.SimpleFilter );
                    AddFilterControl( control, attribute.Name, attribute.Description );
                }
            }

            // Build attribute columns
            foreach ( var column in gGroups.Columns.OfType<AttributeField>().ToList() )
            {
                gGroups.Columns.Remove( column );
            }
            if ( AttributeColumns != null )
            {
                foreach ( var attribute in AttributeColumns )
                {
                    string dataFieldExpression = attribute.Key;
                    bool columnExists = gGroups.Columns.OfType<AttributeField>().FirstOrDefault( a => a.DataField.Equals( dataFieldExpression ) ) != null;
                    if ( !columnExists )
                    {
                        AttributeField boundField = new AttributeField();
                        boundField.DataField = dataFieldExpression;
                        boundField.HeaderText = attribute.Name;
                        boundField.SortExpression = string.Empty;

                        var attributeCache = AttributeCache.Read( attribute.Id );
                        if ( attributeCache != null )
                        {
                            boundField.ItemStyle.HorizontalAlign = attributeCache.FieldType.Field.AlignValue;
                        }

                        gGroups.Columns.Add( boundField );
                    }
                }
            }

            // Add Register Column
            foreach ( var column in gGroups.Columns.OfType<EditField>().ToList() )
            {
                gGroups.Columns.Remove( column );
            }

            var registerPage = new PageReference( GetAttributeValue( "RegisterPage" ) );

            if ( _targetPersonGuid != Guid.Empty )
            {
                registerPage.Parameters = _urlParms;
            }

            if ( registerPage.PageId > 0 )
            {
                var registerColumn = new EditField();
                registerColumn.HeaderText = "Register";
                registerColumn.Click += registerColumn_Click;
                gGroups.Columns.Add( registerColumn );
            }

            var pageSizes = GetAttributeValue( "PageSizes" ).Split( ',' ).AsIntegerList();

            ddlPageSize.Items.Clear();
            ddlPageSize.Items.AddRange( pageSizes.Select( a => new ListItem( a.ToString(), a.ToString() ) ).ToArray() );
            ddlPageSize.Items.Add( new ListItem( "All", "0" ) );

            if ( pageSizes.Any() )
            {
                // set default PageSize to whatever is first in the PageSize setting
                ddlPageSize.Visible = true;
                ddlPageSize.SelectedValue = pageSizes[0].ToString();
            }
            else
            {
                ddlPageSize.Visible = false;
            }

            // if the SortByDistance is enabled, prevent them from sorting by ColumnClick
            if ( GetAttributeValue( "SortByDistance" ).AsBoolean() )
            {
                gGroups.AllowSorting = false;
            }

            //Add Connection Status checkboxes
            if ( GetAttributeValue( "ShowFamilies" ).AsBoolean() || GetAttributeValue( "ShowFamilyGrid" ).AsBoolean() )
            {
                wpConnectionStatus.Visible = true;
                var connectionStatuses = DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS.AsGuid() );
                cblConnectionStatus.DataSource = connectionStatuses.DefinedValues.OrderBy( dv => dv.Value );
                cblConnectionStatus.DataBind();
                //Load user preferences
                var connectionPreference = GetUserPreference( BlockId.ToString() + "ConnectionStatus" );
                if ( !string.IsNullOrWhiteSpace( connectionPreference ) )
                {

                    var preferences = connectionPreference.Split( '|' );
                    for ( int i = 0; i < cblConnectionStatus.Items.Count; i++ )

                    {
                        if ( preferences.Contains( cblConnectionStatus.Items[i].Value ) )
                            cblConnectionStatus.Items[i].Selected = true;
                    }
                }

            }
            else
            {
                wpConnectionStatus.Visible = false;
            }
        }

        private void AddFilterControl( Control control, string name, string description )
        {
            if ( control is IRockControl )
            {
                var rockControl = ( IRockControl ) control;
                rockControl.Label = name;
                rockControl.Help = description;
                phFilterControls.Controls.Add( control );
            }
            else
            {
                var wrapper = new RockControlWrapper();
                wrapper.ID = control.ID + "_wrapper";
                wrapper.Label = name;
                wrapper.Controls.Add( control );
                phFilterControls.Controls.Add( wrapper );
            }
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void ShowResults()
        {
            // Get the group types that we're interested in
            Guid? groupTypeGuid = GetAttributeValue( "GroupType" ).AsGuidOrNull();
            if ( !groupTypeGuid.HasValue )
            {
                ShowError( "A valid Group Type is required." );
                return;
            }

            gGroups.Columns[1].Visible = GetAttributeValue( "ShowDescription" ).AsBoolean();
            gGroups.Columns[2].Visible = GetAttributeValue( "ShowSchedule" ).AsBoolean();
            gGroups.Columns[3].Visible = GetAttributeValue( "ShowCount" ).AsBoolean();
            gGroups.Columns[4].Visible = GetAttributeValue( "ShowAge" ).AsBoolean();

            bool showProximity = GetAttributeValue( "ShowProximity" ).AsBoolean();
            gGroups.Columns[5].Visible = showProximity;  // Distance

            //Get info on home locations for later
            var homeAddressDv = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() );
            double metersRange = Location.MetersPerMile * ddlRange.SelectedValueAsInt() ?? 0.0D;

            var rockContext = new RockContext();

            //Current User's Search Location
            Location searchLocation = new LocationService( rockContext )
                .Get( acAddress.Street1, acAddress.Street2, acAddress.City,
                acAddress.State, acAddress.PostalCode, acAddress.Country );

            // Get query of groups of the selected group type
            var groupService = new GroupService( rockContext );
            IQueryable<Group> groupQry = groupService.Queryable( "GroupLocations.Location" );

            //Sort by group parent if option set
            if ( GetAttributeValue( "GroupParent" ).AsInteger() != 0 )
            {
                var availableGroupIds = ( List<int> ) GetCacheItem( "AvailableGroupIds" );
                if ( availableGroupIds == null )
                {
                    availableGroupIds = GetChildGroups( GetAttributeValue( "GroupParent" ).AsInteger(), groupService ).Select( g => g.Id ).ToList();
                    AddCacheItem( "AvailableGroupIds", availableGroupIds );
                }

                groupQry = groupQry.Where( g => g.IsActive && g.GroupType.Guid.Equals( groupTypeGuid.Value ) && g.IsPublic && availableGroupIds.Contains( g.Id ) );
            }
            else
            {
                //else just get the available groups of type
                groupQry = groupQry.Where( g => g.IsActive && g.GroupType.Guid.Equals( groupTypeGuid.Value ) && g.IsPublic );
            }

            //Limit groups by distance from geopoint
            if ( ddlRange.SelectedValue.AsInteger() != 0 )
            {
                groupQry = groupQry.Where( g => g.GroupLocations.FirstOrDefault() != null
                    && g.GroupLocations.FirstOrDefault().Location.GeoPoint.Distance( searchLocation.GeoPoint ) <= metersRange );
            }

            var groupParameterExpression = groupService.ParameterExpression;
            var schedulePropertyExpression = Expression.Property( groupParameterExpression, "Schedule" );

            var dowFilterControl = phFilterControls.FindControl( "filter_dow" );
            if ( dowFilterControl != null )
            {
                var field = FieldTypeCache.Read( Rock.SystemGuid.FieldType.DAY_OF_WEEK ).Field;

                var filterValues = field.GetFilterValues( dowFilterControl, null, Rock.Reporting.FilterMode.SimpleFilter );
                var expression = field.PropertyFilterExpression( null, filterValues, schedulePropertyExpression, "WeeklyDayOfWeek", typeof( DayOfWeek? ) );
                groupQry = groupQry.Where( groupParameterExpression, expression, null );
            }

            var timeFilterControl = phFilterControls.FindControl( "filter_time" );
            if ( timeFilterControl != null )
            {
                var field = FieldTypeCache.Read( Rock.SystemGuid.FieldType.TIME ).Field;

                var filterValues = field.GetFilterValues( timeFilterControl, null, Rock.Reporting.FilterMode.SimpleFilter );
                var expression = field.PropertyFilterExpression( null, filterValues, schedulePropertyExpression, "WeeklyTimeOfDay", typeof( TimeSpan? ) );
                groupQry = groupQry.Where( groupParameterExpression, expression, null );
            }

            // Filter query by any configured attribute filters
            if ( AttributeFilters != null && AttributeFilters.Any() )
            {
                var attributeValueService = new AttributeValueService( rockContext );
                var parameterExpression = attributeValueService.ParameterExpression;

                foreach ( var attribute in AttributeFilters )
                {
                    var filterControl = phFilterControls.FindControl( "filter_" + attribute.Id.ToString() );
                    if ( filterControl != null )
                    {
                        var filterValues = attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter );
                        var expression = attribute.FieldType.Field.AttributeFilterExpression( attribute.QualifierValues, filterValues, parameterExpression );
                        if ( expression != null )
                        {
                            var attributeValues = attributeValueService
                                .Queryable()
                                .Where( v => v.Attribute.Id == attribute.Id );

                            attributeValues = attributeValues.Where( parameterExpression, expression, null );

                            groupQry = groupQry.Where( w => attributeValues.Select( v => v.EntityId ).Contains( w.Id ) );
                        }
                    }
                }
            }

            List<GroupLocation> fences = null;
            List<Group> groups = null;

            // Run query to get list of matching groups
            SortProperty sortProperty = gGroups.SortProperty;
            if ( sortProperty != null )
            {
                groups = groupQry.Sort( sortProperty ).ToList();
            }
            else
            {
                groups = groupQry.OrderBy( g => g.Name ).ToList();
            }

            //Hide full groups if it is set
            if ( GetAttributeValue( "HideFull" ).AsBoolean() )
            {
                groups.ForEach( g => g.LoadAttributes() );
                groups = groups
                    .Where( g => g.GetAttributeValue( "Maximum Members" ).AsInteger() > g.Members.Where( m => m.GroupMemberStatus > 0 ).Count() )
                    .ToList();
            }


            //Filter out to show only groups that have GeoPoints
            groups = groups.Where( g => g.GroupLocations.Where( gl => gl.Location.GeoPoint != null ).Count() > 0 ).ToList();

            int? fenceGroupTypeId = GetGroupTypeId( GetAttributeValue( "GeofencedGroupType" ).AsGuidOrNull() );
            bool showMap = GetAttributeValue( "ShowMap" ).AsBoolean();
            bool showFences = showMap && GetAttributeValue( "ShowFence" ).AsBoolean();

            var distances = new Dictionary<int, double>();
            List<Group> families = new List<Group>();
            // If we care where these groups are located...
            if ( fenceGroupTypeId.HasValue || showMap || showProximity )
            {
                pnlSearch.Visible = false;
                // Get the location for the address entered



                // If showing a map, and person's location was found, save a mapitem for this location
                FinderMapItem personMapItem = null;
                if ( showMap && searchLocation != null && searchLocation.GeoPoint != null )
                {
                    var infoWindow = string.Format( @"
<div style='width:250px'>
    <div class='clearfix'>
		<strong>Your Location</strong>
        <br/>{0}
    </div>
</div>
", searchLocation.FormattedHtmlAddress );

                    personMapItem = new FinderMapItem( searchLocation );
                    personMapItem.Name = "Your Location";
                    personMapItem.InfoWindow = HttpUtility.HtmlEncode( infoWindow.Replace( Environment.NewLine, string.Empty ).Replace( "\n", string.Empty ).Replace( "\t", string.Empty ) );
                    var searchIcon = GetAttributeValue( "SearchIcon" );
                    //add custom icon to our search location
                    if ( !string.IsNullOrWhiteSpace( searchIcon ) )
                    {
                        personMapItem.Icon = searchIcon;
                    }
                }

                // Get the locations, and optionally calculate the distance for each of the groups
                var groupLocations = new List<GroupLocation>();
                foreach ( var group in groups )
                {
                    foreach ( var groupLocation in group.GroupLocations
                        .Where( gl => gl.Location.GeoPoint != null ) )
                    {
                        groupLocations.Add( groupLocation );

                        if ( searchLocation != null && searchLocation.GeoPoint != null )
                        {
                            double meters = groupLocation.Location.GeoPoint.Distance( searchLocation.GeoPoint ) ?? 0.0D;
                            double miles = meters * Location.MilesPerMeter;

                            // If this group already has a distance calculated, see if this location is closer and if so, use it instead
                            if ( distances.ContainsKey( group.Id ) )
                            {
                                if ( distances[group.Id] < miles )
                                {
                                    distances[group.Id] = miles;
                                }
                            }
                            else
                            {
                                distances.Add( group.Id, miles );
                            }
                        }
                    }
                }
                //Only show those in range.
                groupLocations = groupLocations
                    .Where( gl => distances.ContainsKey( gl.GroupId ) && distances[gl.GroupId] < Int32.Parse( ddlRange.SelectedValue ) && gl.Location.GeoPoint != null )
                    .ToList();

                // If groups should be limited by a geofence
                var fenceMapItems = new List<MapItem>();
                if ( fenceGroupTypeId.HasValue )
                {
                    fences = new List<GroupLocation>();
                    if ( searchLocation != null && searchLocation.GeoPoint != null )
                    {
                        fences = new GroupLocationService( rockContext )
                            .Queryable( "Group,Location" )
                            .Where( gl =>
                                gl.Group.GroupTypeId == fenceGroupTypeId &&
                                gl.Location.GeoFence != null &&
                                searchLocation.GeoPoint.Intersects( gl.Location.GeoFence ) )
                            .ToList();
                    }

                    // Limit the group locations to only those locations inside one of the fences
                    groupLocations = groupLocations
                        .Where( gl =>
                            fences.Any( f => gl.Location.GeoPoint.Intersects( f.Location.GeoFence ) ) )
                        .ToList();



                    // If the map and fences should be displayed, create a map item for each fence
                    if ( showMap && showFences )
                    {
                        foreach ( var fence in fences )
                        {
                            var mapItem = new FinderMapItem( fence.Location );
                            mapItem.EntityTypeId = EntityTypeCache.Read( "Rock.Model.Group" ).Id;
                            mapItem.EntityId = fence.GroupId;
                            mapItem.Name = fence.Group.Name;
                            fenceMapItems.Add( mapItem );
                        }
                    }
                }

                // if not sorting by ColumnClick and SortByDistance, then sort the groups by distance
                if ( gGroups.SortProperty == null && GetAttributeValue( "SortByDistance" ).AsBoolean() )
                {
                    // only show groups with a known location, and sort those by distance
                    groups = groups.Where( a => distances.Select( b => b.Key ).Contains( a.Id ) ).ToList();
                    groups = groups.OrderBy( a => distances[a.Id] ).ThenBy( a => a.Name ).ToList();
                }

                //if there is a max results will limit to that number, 0 means all
                var maxResults = GetAttributeValue( "MaxResults" ).AsInteger();
                if ( maxResults > 0 )
                {
                    groups = groups.Take( maxResults ).ToList();
                }

                // if limiting by PageSize, limit to the top X groups
                int? pageSize = ddlPageSize.SelectedValue.AsIntegerOrNull();
                if ( pageSize.HasValue && pageSize > 0 )
                {
                    groups = groups.Take( pageSize.Value ).ToList();
                }

                // Limit the groups to the those that still contain a valid location
                groups = groups
                    .Where( g =>
                        groupLocations.Any( gl => gl.GroupId == g.Id ) )
                    .ToList();

                //If selected add nearby families to map

                if ( ( GetAttributeValue( "ShowFamilies" ).AsBoolean() || GetAttributeValue( "ShowFamilyGrid" ).AsBoolean() ) && searchLocation.GeoPoint != null )
                {

                    int familyGroupTypeId = new GroupTypeService(rockContext).Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid()).Id;

                   families = groupService.Queryable( "GroupLocations.Location Members.Person.PhoneNumbers" )
                        .Where( g =>
                        g.IsActive
                        && g.GroupTypeId == familyGroupTypeId
                        && ( g.GroupLocations.Where(
                            gl =>
                            gl.GroupLocationTypeValueId == homeAddressDv.Id && gl.IsMappedLocation
                            && gl.Location.GeoPoint.Distance( searchLocation.GeoPoint ) <= metersRange
                            )
                        ).Any()
                    ).ToList();


                    //Limit by connection statuses if needed
                    var connectionStatuses = cblConnectionStatus.Items.Cast<ListItem>()
                    .Where( li => li.Selected ).Select( li => li.Value ).Select( s => s.AsInteger() )
                    .ToList();

                    if ( connectionStatuses.Count() > 0 )
                    {
                        families = families.Where(
                            f => f.Members.Where(
                                gm => connectionStatuses.Contains(
                                    gm.Person.ConnectionStatusValueId ?? 0
                                )
                            ).Any()
                        ).ToList();


                    }
                    SetUserPreference( BlockId.ToString() + "ConnectionStatus", string.Join( "|", connectionStatuses ) );

                }

                // If a map is to be shown
                if ( showMap && ( groups.Any() || families.Any() ) )
                {
                    Template groupInfoTemplate = Template.Parse( GetAttributeValue( "MapInfo" ) );
                    Template familyInfoTemplate = Template.Parse( GetAttributeValue( "FamilyInfo" ) );

                    bool showDebug = UserCanEdit && GetAttributeValue( "MapInfoDebug" ).AsBoolean();
                    lMapInfoDebug.Visible = showDebug;

                    var debugStatus = ShowDebugStatus.NoShow;
                    if ( showDebug )
                    {
                        debugStatus = ShowDebugStatus.Show;
                    }

                    // Add mapitems for all the remaining valid group locations
                    var groupIcon = GetAttributeValue( "groupIcon" );
                    var groupMapItems = new List<MapItem>();
                    foreach ( var gl in groupLocations )
                    {
                        var group = groups.Where( g => g.Id == gl.GroupId ).FirstOrDefault();
                        if ( group != null )
                        {
                            // Resolve info window lava template
                            var linkedPageParams = new Dictionary<string, string> { { "GroupId", group.Id.ToString() } };
                            var mergeFields = new Dictionary<string, object>();
                            mergeFields.Add( "Group", gl.Group );
                            mergeFields.Add( "Location", gl.Location );

                            Dictionary<string, object> linkedPages = new Dictionary<string, object>();
                            linkedPages.Add( "GroupDetailPage", LinkedPageUrl( "GroupDetailPage", null ) );

                            if ( _targetPersonGuid != Guid.Empty )
                            {
                                linkedPages.Add( "RegisterPage", LinkedPageUrl( "RegisterPage", _urlParms ) );
                            }
                            else
                            {
                                linkedPages.Add( "RegisterPage", LinkedPageUrl( "RegisterPage", null ) );
                            }

                            mergeFields.Add( "LinkedPages", linkedPages );

                            // add collection of allowed security actions
                            Dictionary<string, object> securityActions = new Dictionary<string, object>();
                            securityActions.Add( "View", group.IsAuthorized( Authorization.VIEW, CurrentPerson ) );
                            securityActions.Add( "Edit", group.IsAuthorized( Authorization.EDIT, CurrentPerson ) );
                            securityActions.Add( "Administrate", group.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson ) );
                            mergeFields.Add( "AllowedActions", securityActions );

                            string infoWindow = groupInfoTemplate.Render( Hash.FromDictionary( mergeFields ) );

                            if ( debugStatus == ShowDebugStatus.Show )
                            {
                                lMapInfoDebug.Text = mergeFields.lavaDebugInfo( null, "<span class='label label-info'>Lava used for the group map window.</span>", "" );
                                debugStatus = ShowDebugStatus.GroupIncluded;
                            }

                            // Add a map item for group
                            var mapItem = new FinderMapItem( gl.Location );
                            mapItem.EntityTypeId = EntityTypeCache.Read( "Rock.Model.Group" ).Id;
                            mapItem.EntityId = group.Id;
                            mapItem.Name = group.Name;
                            mapItem.InfoWindow = HttpUtility.HtmlEncode( infoWindow.Replace( Environment.NewLine, string.Empty ).Replace( "\n", string.Empty ).Replace( "\t", string.Empty ) );
                            if ( !string.IsNullOrWhiteSpace( groupIcon ) )
                            {
                                mapItem.Icon = groupIcon;
                            }
                            groupMapItems.Add( mapItem );
                        }
                    }

                    var familyMapItems = new List<MapItem>();

                    if ( GetAttributeValue( "ShowFamilies" ).AsBoolean() )
                    {
                        var familyIcon = GetAttributeValue( "FamilyIcon" );
                        foreach ( var family in families )
                        {

                            var mapItem = new FinderMapItem( family.GroupLocations.Where( gl => gl.GroupLocationTypeValueId == homeAddressDv.Id ).First().Location );
                            mapItem.EntityTypeId = EntityTypeCache.Read( "Rock.Model.Group" ).Id;
                            mapItem.EntityId = family.Id;
                            mapItem.Name = family.Name;
                            if ( !string.IsNullOrWhiteSpace( familyIcon ) )
                            {
                                mapItem.Icon = familyIcon;
                            }

                            var mergeFields = new Dictionary<string, object>();
                            mergeFields.Add( "Family", family );
                            mapItem.InfoWindow = familyInfoTemplate.Render( Hash.FromDictionary( mergeFields ) );
                            familyMapItems.Add( mapItem );

                            if ( debugStatus == ShowDebugStatus.GroupIncluded )
                            {
                                lMapInfoDebug.Text += mergeFields.lavaDebugInfo( null, "<span class='label label-info'>Lava used for the family map window.</span>", "" );
                                debugStatus = ShowDebugStatus.FamilyIncluded;
                            }
                        }
                    }

                    var campusMapItems = new List<MapItem>();

                    var campusList = CampusCache.All();
                    foreach ( var campus in campusList )
                    {
                        var mapItem = new FinderMapItem( campus );
                        mapItem.EntityTypeId = EntityTypeCache.Read( "Rock.Model.Campus" ).Id;
                        mapItem.EntityId = campus.Id;
                        mapItem.Name = campus.Name;
                        mapItem.InfoWindow = HttpUtility.HtmlEncode( "<b>" + campus.Name + "</b><br>" +
                            campus.Location.Street1 + "<br>" + campus.Location.City +
                            ", " + campus.Location.State + " " + campus.Location.PostalCode );
                        campus.LoadAttributes();
                        var campusIcon = campus.GetAttributeValue( "MapIcon" );
                        if ( !string.IsNullOrWhiteSpace( campusIcon ) )
                        {
                            mapItem.Icon = campusIcon;
                        }
                        campusMapItems.Add( mapItem );
                    }

                    // Show the map
                    Map( personMapItem, fenceMapItems, groupMapItems, familyMapItems, campusMapItems );
                    pnlMap.Visible = true;
                }
                else
                {
                    pnlMap.Visible = false;
                }
            }
            else
            {
                pnlMap.Visible = false;
            }

            // Should a lava output be displayed
            if ( GetAttributeValue( "ShowLavaOutput" ).AsBoolean() )
            {
                string template = GetAttributeValue( "LavaOutput" );

                var mergeFields = new Dictionary<string, object>();
                if ( fences != null )
                {
                    mergeFields.Add( "Fences", fences.Select( f => f.Group ).ToList() );
                }
                else
                {
                    mergeFields.Add( "Fences", new Dictionary<string, object>() );
                }

                mergeFields.Add( "Groups", groups );

                Dictionary<string, object> linkedPages = new Dictionary<string, object>();
                linkedPages.Add( "GroupDetailPage", LinkedPageUrl( "GroupDetailPage", null ) );

                if ( _targetPersonGuid != Guid.Empty )
                {
                    linkedPages.Add( "RegisterPage", LinkedPageUrl( "RegisterPage", _urlParms ) );
                }
                else
                {
                    linkedPages.Add( "RegisterPage", LinkedPageUrl( "RegisterPage", null ) );
                }

                mergeFields.Add( "LinkedPages", linkedPages );

                lLavaOverview.Text = template.ResolveMergeFields( mergeFields );

                bool showDebug = UserCanEdit && GetAttributeValue( "LavaOutputDebug" ).AsBoolean();
                lLavaOutputDebug.Visible = showDebug;
                if ( showDebug )
                {
                    lLavaOutputDebug.Text = mergeFields.lavaDebugInfo( null, "<span class='label label-info'>Lava used for the summary info.</span>" );
                }

                pnlLavaOutput.Visible = true;
            }
            else
            {
                pnlLavaOutput.Visible = false;
            }

            // Should a grid be displayed
            if ( GetAttributeValue( "ShowGrid" ).AsBoolean() )
            {
                pnlGrid.Visible = true;

                // Save the groups into the grid's object list since it is not being bound to actual group objects
                gGroups.ObjectList = new Dictionary<string, object>();
                groups.ForEach( g => gGroups.ObjectList.Add( g.Id.ToString(), g ) );

                // Bind the grid
                gGroups.DataSource = groups.Select( g => new
                {
                    Id = g.Id,
                    Name = g.Name,
                    GroupTypeName = g.GroupType.Name,
                    GroupOrder = g.Order,
                    GroupTypeOrder = g.GroupType.Order,
                    Description = g.Description,
                    IsSystem = g.IsSystem,
                    IsActive = g.IsActive,
                    GroupRole = string.Empty,
                    DateAdded = DateTime.MinValue,
                    Schedule = g.Schedule,
                    MemberCount = g.Members.Count(),
                    AverageAge = Math.Round( g.Members.Select( m => m.Person ).Average( p => p.Age ) ?? 0.0D ),
                    Distance = distances.Where( d => d.Key == g.Id )
                        .Select( d => d.Value ).FirstOrDefault()
                } )
                .Where( a => a.Distance < ddlRange.SelectedValue.AsInteger() && distances.ContainsKey( a.Id ) )
                .ToList();
                gGroups.DataBind();
            }
            else
            {
                pnlGrid.Visible = false;
            }

            if ( GetAttributeValue( "ShowFamilyGrid" ).AsBoolean() )
            {
                pnlFamilyGrid.Visible = true;
                var source = families.Select( f => new
                {
                    Name = f.Name,
                    Members = f.Members.Where( m => m.GroupMemberStatus == GroupMemberStatus.Active )
                        .OrderByDescending( m => m.Person.AgePrecise )
                        .Select( m => string.Format( "{0}: {1}", m.Person.NickName, m.Person.Age ) )
                        .Aggregate( ( current, next ) => current + ", " + next ),
                    Address = f.GroupLocations.Where( gl => gl.GroupLocationTypeValueId == homeAddressDv.Id ).First().Location.FormattedAddress,
                    CellPhone = f.Members
                        .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active )
                        .Select( m => m.Person )
                        .Where( p => p.PhoneNumbers.Where( pn => pn.IsMessagingEnabled ).Any() )
                        .Any()
                        ?
                        f.Members
                        .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active )
                        .Select( m => m.Person ).Where( p => p.PhoneNumbers.Where( pn => pn.IsMessagingEnabled ).Any() )
                        .Select( p => string.Format( "{0}: {1}", p.NickName, p.PhoneNumbers.Where( pn => pn.IsMessagingEnabled ).FirstOrDefault().NumberFormatted ) )
                        .Aggregate( ( current, next ) => current + ", " + next )
                        : "",
                    Email = f.Members
                        .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active )
                        .Where( m => !string.IsNullOrWhiteSpace( m.Person.Email ) && m.Person.EmailPreference == EmailPreference.EmailAllowed ).Any()
                        ?
                        f.Members
                        .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active )
                        .Where( m => !string.IsNullOrWhiteSpace( m.Person.Email ) && m.Person.EmailPreference == EmailPreference.EmailAllowed )
                        .Select( m => string.Format( "{0}: {1}", m.Person.NickName, m.Person.Email ) )
                        .Aggregate( ( current, next ) => current + ", " + next )
                        : "",
                    Id = f.Members.Where( m => m.GroupMemberStatus == GroupMemberStatus.Active ).OrderByDescending( m => m.Person.AgePrecise ).Select( m => m.PersonId ).First()

                } ).ToList();
                if ( source.Any() )
                {
                    gFamilies.DataSource = source;
                    gFamilies.DataBind();
                }
            }
            else
            {
                pnlFamilyGrid.Visible = false;
            }

            if ( groups.Any() || families.Any() )
            {
                // Show the results
                pnlResults.Visible = true;
            }
            else
            {
                //Could not find any groups re-show address field
                pnlSearch.Visible = true;
                Template template = Template.Parse( GetAttributeValue( "GrouplessMessage" ) );


                // Resolve lava template
                var mergeFields = new Dictionary<string, object>();

                Dictionary<string, object> linkedPages = new Dictionary<string, object>();
                linkedPages.Add( "GroupDetailPage", LinkedPageUrl( "GroupDetailPage", null ) );

                if ( _targetPersonGuid != Guid.Empty )
                {
                    linkedPages.Add( "RegisterPage", LinkedPageUrl( "RegisterPage", _urlParms ) );
                }
                else
                {
                    linkedPages.Add( "RegisterPage", LinkedPageUrl( "RegisterPage", null ) );
                }


                mergeFields.Add( "LinkedPages", linkedPages );

                lMessage.Text = template.Render( Hash.FromDictionary( mergeFields ) );

            }
            hfDidSearch.Value = "True";
        }

        private List<Group> GetChildGroups( int groupId, GroupService groupService )
        {
            List<Group> childGroups = new List<Group>();
            var group = groupService.Get( groupId );
            childGroups.AddRange( group.Groups );
            List<Group> grandChildGroups = new List<Group>();
            foreach ( var childGroup in childGroups )
            {
                grandChildGroups.AddRange( GetChildGroups( childGroup.Id, groupService ) );
            }
            childGroups.AddRange( grandChildGroups );
            return childGroups;
        }

        private void BindGroupType( GroupTypePicker control, List<GroupType> groupTypes, string attributeName )
        {
            control.GroupTypes = groupTypes;

            Guid? groupTypeGuid = GetAttributeValue( attributeName ).AsGuidOrNull();
            if ( groupTypeGuid.HasValue )
            {
                var groupType = groupTypes.FirstOrDefault( g => g.Guid.Equals( groupTypeGuid.Value ) );
                if ( groupType != null )
                {
                    control.SelectedGroupTypeId = groupType.Id;
                }
            }
        }

        private int? GetGroupTypeId( Guid? groupTypeGuid )
        {
            if ( groupTypeGuid.HasValue )
            {
                var groupType = GroupTypeCache.Read( groupTypeGuid.Value );
                if ( groupType != null )
                {
                    return groupType.Id;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the group type unique identifier.
        /// </summary>
        /// <param name="groupTypeId">The group type identifier.</param>
        /// <returns></returns>
        private string GetGroupTypeGuid( int? groupTypeId )
        {
            if ( groupTypeId.HasValue )
            {
                var groupType = GroupTypeCache.Read( groupTypeId.Value );
                if ( groupType != null )
                {
                    return groupType.Guid.ToString();
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Maps the specified location.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="fences">The fences.</param>
        /// <param name="groups">The groups.</param>
        private void Map( MapItem location, List<MapItem> fences, List<MapItem> groups, List<MapItem> families, List<MapItem> campuses )
        {
            pnlMap.Visible = true;
            pnlMessage.Visible = false;

            string mapStylingFormat = @"
                        <style>
                            #map_wrapper {{
                                height: {0}px;
                            }}

                            #map_canvas {{
                                width: 100%;
                                height: 100%;
                                border-radius: 8px;
                            }}
                        </style>";
            lMapStyling.Text = string.Format( mapStylingFormat, GetAttributeValue( "MapHeight" ) );

            // add styling to map
            string styleCode = "null";
            var markerColors = new List<string>();

            DefinedValueCache dvcMapStyle = DefinedValueCache.Read( GetAttributeValue( "MapStyle" ).AsInteger() );
            if ( dvcMapStyle != null )
            {
                styleCode = dvcMapStyle.GetAttributeValue( "DynamicMapStyle" );
                markerColors = dvcMapStyle.GetAttributeValue( "Colors" )
                    .Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries )
                    .ToList();
                markerColors.ForEach( c => c = c.Replace( "#", string.Empty ) );
            }
            if ( !markerColors.Any() )
            {
                markerColors.Add( "FE7569" );
            }


            string locationColor = markerColors[0].Replace( "#", string.Empty );

            var polygonColorList = GetAttributeValue( "PolygonColors" ).Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
            string polygonColors = "\"" + polygonColorList.AsDelimited( "\", \"" ) + "\"";
            string groupColor = ( markerColors.Count > 1 ? markerColors[1] : markerColors[0] ).Replace( "#", string.Empty );
            string familyColor = ( markerColors.Count > 2 ? markerColors[2] : markerColors[0] ).Replace( "#", string.Empty );
            string campusColor = ( markerColors.Count > 2 ? markerColors[3] : markerColors[0] ).Replace( "#", string.Empty );

            string latitude = "39.8282";
            string longitude = "-98.5795";
            string zoom = "4";
            var orgLocation = GlobalAttributesCache.Read().OrganizationLocation;
            if ( orgLocation != null && orgLocation.GeoPoint != null )
            {
                latitude = orgLocation.GeoPoint.Latitude.ToString();
                longitude = orgLocation.GeoPoint.Longitude.ToString();
                zoom = "12";
            }

            // write script to page
            string mapScriptFormat = @"

        var locationData = {0};
        var fenceData = {1};
        var groupData = {2}; 
        var familyData = {11};
        var campusData = {10};

        var allMarkers = [];

        var map;
        var bounds = new google.maps.LatLngBounds();
        var infoWindow = new google.maps.InfoWindow();

        var mapStyle = {3};

        var pinShadow = new google.maps.MarkerImage('//chart.googleapis.com/chart?chst=d_map_pin_shadow',
            new google.maps.Size(40, 37),
            new google.maps.Point(0, 0),
            new google.maps.Point(12, 35));

        var polygonColorIndex = 0;
        var polygonColors = [{5}];

        var min = .999999;
        var max = 1.000001;

        initializeMap();

        function initializeMap() {{

            // Set default map options
            var mapOptions = {{
                 mapTypeId: 'roadmap'
                ,styles: mapStyle
                ,center: new google.maps.LatLng({7}, {8})
                ,zoom: {9}
            }};

            // Display a map on the page
            map = new google.maps.Map(document.getElementById('map_canvas'), mapOptions);
            map.setTilt(45);

            if ( locationData != null )
            {{
                var items = addMapItem(0, locationData, '{4}', true);
                for (var j = 0; j < items.length; j++) {{
                    items[j].setMap(map);
                }}
            }}

            if ( fenceData != null ) {{
                for (var i = 0; i < fenceData.length; i++) {{
                    var items = addMapItem(i, fenceData[i] );
                    for (var j = 0; j < items.length; j++) {{
                        items[j].setMap(map);
                    }}
                }}
            }}

            if ( familyData != null ) {{
                for (var i = 0; i < familyData.length; i++) {{
                    var items = addMapItem(i, familyData[i], '{12}', true);
                    for (var j = 0; j < items.length; j++) {{
                        items[j].setMap(map);
                    }}
                }}
            }}

            if ( groupData != null ) {{
                for (var i = 0; i < groupData.length; i++) {{
                    var items = addMapItem(i, groupData[i], '{6}', true);
                    for (var j = 0; j < items.length; j++) {{
                        items[j].setMap(map);
                    }}
                }}
            }}

            if ( campusData != null ) {{
                for (var i = 0; i < campusData.length; i++) {{
                    var items = addMapItem(i, campusData[i], '{13}', false);
                    for (var j = 0; j < items.length; j++) {{
                        items[j].setMap(map);
                    }}
                }}
            }}

            // adjust any markers that may overlap
            adjustOverlappedMarkers();

            if (!bounds.isEmpty()) {{
                map.fitBounds(bounds);
            }}

        }}

        function addMapItem( i, mapItem, color, addBounds ) {{

            var items = [];

            if (mapItem.Point) {{ 

                var position = new google.maps.LatLng(mapItem.Point.Latitude, mapItem.Point.Longitude);
                //if you want to center on this map item
                if (addBounds) {{
                    bounds.extend(position);
                }}

                if (!color) {{
                    color = 'FE7569'
                }}
                
                if (mapItem.Icon){{
                    var pinImage = new google.maps.MarkerImage(mapItem.Icon,
                        new google.maps.Size(34, 34),
                        new google.maps.Point(0,0),
                        new google.maps.Point(10, 34));
                }}
                else {{
                    var pinImage = new google.maps.MarkerImage('//chart.googleapis.com/chart?chst=d_map_pin_letter&chld=%E2%80%A2|' + color,
                        new google.maps.Size(21, 34),
                        new google.maps.Point(0,0),
                        new google.maps.Point(10, 34));
                }}

                marker = new google.maps.Marker({{
                    position: position,
                    map: map,
                    title: htmlDecode(mapItem.Name),
                    icon: pinImage,
                    shadow: pinShadow
                }});

                marker['EntityId']=mapItem.EntityId;
    
                items.push(marker);
                allMarkers.push(marker);

                if ( mapItem.InfoWindow != null ) {{
                    marker['InfoWindowContent'] = $('<div/>').html(mapItem.InfoWindow).text();
                    google.maps.event.addListener(marker, 'click', (function (marker, i) {{
                        return function () {{
                            infoWindow.setContent( $('<div/>').html(mapItem.InfoWindow).text() );
                            infoWindow.open(map, marker);
                        }}
                    }})(marker, i));
                }}

                if ( mapItem.EntityId && mapItem.EntityId > 0 ) {{ 
                    google.maps.event.addListener(marker, 'mouseover', (function (marker, i) {{
                        return function () {{
                            $(""tr[datakey='"" + mapItem.EntityId + ""']"").addClass('row-highlight');
                        }}
                    }})(marker, i));

                    google.maps.event.addListener(marker, 'mouseout', (function (marker, i) {{
                        return function () {{
                            $(""tr[datakey='"" + mapItem.EntityId + ""']"").removeClass('row-highlight');
                        }}
                    }})(marker, i));

                }}
            }}

            if (typeof mapItem.PolygonPoints !== 'undefined' && mapItem.PolygonPoints.length > 0) {{

                var polygon;
                var polygonPoints = [];

                $.each(mapItem.PolygonPoints, function(j, point) {{
                    var position = new google.maps.LatLng(point.Latitude, point.Longitude);
                    bounds.extend(position);
                    polygonPoints.push(position);
                }});

                var polygonColor = getNextPolygonColor();

                polygon = new google.maps.Polygon({{
                    paths: polygonPoints,
                    map: map,
                    strokeColor: polygonColor,
                    fillColor: polygonColor
                }});

                items.push(polygon);

                // Get Center
                var polyBounds = new google.maps.LatLngBounds();
                for ( j = 0; j < polygonPoints.length; j++) {{
                    polyBounds.extend(polygonPoints[j]);
                }}

                if ( mapItem.InfoWindow != null ) {{ 
                    google.maps.event.addListener(polygon, 'click', (function (polygon, i) {{
                        return function () {{
                            infoWindow.setContent( mapItem.InfoWindow );
                            infoWindow.setPosition(polyBounds.getCenter());
                            infoWindow.open(map);
                        }}
                    }})(polygon, i));
                }}
            }}

            return items;

        }}
        
        function setAllMap(markers, map) {{
            for (var i = 0; i < markers.length; i++) {{
                markers[i].setMap(map);
            }}
        }}

        function htmlDecode(input) {{
            var e = document.createElement('div');
            e.innerHTML = input;
            return e.childNodes.length === 0 ? """" : e.childNodes[0].nodeValue;
        }}

        function getNextPolygonColor() {{
            var color = 'FE7569';
            if ( polygonColors.length > polygonColorIndex ) {{
                color = polygonColors[polygonColorIndex];
                polygonColorIndex++;
            }} else {{
                color = polygonColors[0];
                polygonColorIndex = 1;
            }}
            return color;
        }}

        function adjustOverlappedMarkers() {{
            
            if (allMarkers.length > 1) {{
                for(i=0; i < allMarkers.length-1; i++) {{
                    var marker1 = allMarkers[i];
                    var pos1 = marker1.getPosition();
                    for(j=i+1; j < allMarkers.length; j++) {{
                        var marker2 = allMarkers[j];
                        var pos2 = marker2.getPosition();
                        if (pos1.equals(pos2)) {{
                            var newLat = pos1.lat() * (Math.random() * (max - min) + min);
                            var newLng = pos1.lng() * (Math.random() * (max - min) + min);
                            marker1.setPosition( new google.maps.LatLng(newLat,newLng) );
                        }}
                    }}
                }}
            }}

        }}
        
        function centerOnMarkerByGroupId(groupId){{
            allMarkers.forEach(function(item){{
                if (item['EntityId']==groupId) {{
                    var latLng = item.getPosition();
                    map.setCenter(latLng);
                    infoWindow.setContent( item['InfoWindowContent'] );
                    infoWindow.open(map, item);
                }}
            }});
        }}
";

            var locationJson = location != null ?
                string.Format( "JSON.parse('{0}')", location.ToJson().Replace( Environment.NewLine, "" ).EscapeQuotes().Replace( "\x0A", "" ) ) : "null";

            var fencesJson = fences != null && fences.Any() ?
                string.Format( "JSON.parse('{0}')", fences.ToJson().Replace( Environment.NewLine, "" ).EscapeQuotes().Replace( "\x0A", "" ) ) : "null";

            var groupsJson = groups != null && groups.Any() ?
                string.Format( "JSON.parse('{0}')", groups.ToJson().Replace( Environment.NewLine, "" ).EscapeQuotes().Replace( "\x0A", "" ) ) : "null";

            var familiesJson = families != null && families.Any() ?
                string.Format( "JSON.parse('{0}')", families.ToJson().Replace( Environment.NewLine, "" ).EscapeQuotes().Replace( "\x0A", "" ) ) : "null";

            var campusJson = campuses != null && campuses.Any() ?
                string.Format( "JSON.parse('{0}')", campuses.ToJson().Replace( Environment.NewLine, "" ).EscapeQuotes().Replace( "\x0A", "" ) ) : "null";

            string mapScript = string.Format( mapScriptFormat,
                locationJson,       // 0
                fencesJson,         // 1
                groupsJson,         // 2
                styleCode,          // 3
                locationColor,      // 4
                polygonColors,      // 5
                groupColor,         // 6
                latitude,           // 7
                longitude,          // 8
                zoom,               // 9
                campusJson,         //10
                familiesJson,       //11
                familyColor,        //12
                campusColor         //13
                );

            ScriptManager.RegisterStartupScript( pnlMap, pnlMap.GetType(), "group-finder-map-script", mapScript, true );

        }

        private void ShowError( string message )
        {
            nbNotice.Heading = "Error";
            nbNotice.NotificationBoxType = NotificationBoxType.Danger;
            ShowMessage( message );
        }

        private void ShowWarning( string message )
        {
            nbNotice.Heading = "Warning";
            nbNotice.NotificationBoxType = NotificationBoxType.Warning;
            ShowMessage( message );
        }

        private void ShowMessage( string message )
        {
            nbNotice.Text = string.Format( "<p>{0}</p>", message );
            nbNotice.Visible = true;
        }

        #endregion

        enum ShowDebugStatus
        {
            NoShow,
            Show,
            GroupIncluded,
            FamilyIncluded

        };

        /// <summary>
        /// A map item class specific to group finder
        /// </summary>
        class FinderMapItem : MapItem
        {
            private CampusCache.CampusLocation location;

            /// <summary>
            /// Gets or sets the information window.
            /// </summary>
            /// <value>
            /// The information window.
            /// </value>
            public string InfoWindow { get; set; }

            /// <summary>
            /// Gets or sets the information window.
            /// </summary>
            /// <value>
            /// The information window.
            /// </value>
            public string Icon { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="FinderMapItem"/> class.
            /// </summary>
            /// <param name="location">The location.</param>
            public FinderMapItem( Location location )
                : base( location )
            {

            }

            public FinderMapItem( CampusCache campus ) : base()
            {
                this.location = campus.Location;
                PolygonPoints = new List<MapCoordinate>();

                if ( location != null )
                {
                    LocationId = campus.LocationId ?? 0;
                    if ( location != null )
                    {
                        Point = new MapCoordinate( location.Latitude, location.Longitude );
                    }
                }
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlPageSize control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlPageSize_SelectedIndexChanged( object sender, EventArgs e )
        {
            ShowResults();
        }

        protected void btnReset_Click( object sender, EventArgs e )
        {
            hfDidSearch.Value = "False";
            ShowView();
        }

        protected void PersonSelected_Click( object sender, RowEventArgs e )
        {
            Response.Redirect( string.Format( "/Person/{0}", e.RowKeyValue ) );
        }
    }
}
