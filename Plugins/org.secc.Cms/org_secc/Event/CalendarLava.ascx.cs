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
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Event
{
    /// <summary>
    /// Renders a particular calendar using Lava.
    /// </summary>
    [DisplayName( "Yearly Calendar Lava" )]
    [Category( "SECC > Event" )]
    [Description( "Renders a particular calendar using Lava." )]

    [EventCalendarField( "Event Calendar", "The event calendar to be displayed", true, "1", order: 0 )]
    [CustomDropdownListField( "Default View Option", "Determines the default view option", "Day,Week,Month,Year", true, "Week", order: 1 )]
    [LinkedPage( "Details Page", "Detail page for events", order: 2 )]
    [LavaCommandsField( "Enabled Lava Commands", "The Lava commands that should be enabled for this HTML block.", false, order: 3 )]

    [CustomRadioListField( "Campus Filter Display Mode", "", "1^Hidden, 2^Plain, 3^Panel Open, 4^Panel Closed", true, "1", order: 4 )]
    [CustomRadioListField( "Audience Filter Display Mode", "", "1^Hidden, 2^Plain, 3^Panel Open, 4^Panel Closed", true, "1", key: "CategoryFilterDisplayMode", order: 5 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.MARKETING_CAMPAIGN_AUDIENCE_TYPE, "Filter Audiences", "Determines which audiences should be displayed in the filter.", false, true, key: "FilterCategories", order: 6 )]
    [BooleanField( "Show Date Range Filter", "Determines whether the date range filters are shown", false, order: 7 )]

    [BooleanField( "Show Small Calendar", "Determines whether the calendar widget is shown", true, order: 8 )]
    [BooleanField( "Show Day View", "Determines whether the day view option is shown", false, order: 9 )]
    [BooleanField( "Show Week View", "Determines whether the week view option is shown", true, order: 10 )]
    [BooleanField( "Show Month View", "Determines whether the month view option is shown", true, order: 11 )]
    [BooleanField( "Show Year View", "Determines whether the year view option is shown", true, order: 12 )]

    [BooleanField( "Enable Campus Context", "If the page has a campus context it's value will be used as a filter", order: 13 )]
    [TextField( "Cache Duration", "Ammount of time in minutes to cache the output of this block.", true, "3600", order: 14 )]
    [CodeEditorField( "Lava Template", "Lava template to use to display the list of events.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, @"{% include '~~/Assets/Lava/Calendar.lava' %}", "", 15 )]

    [DayOfWeekField( "Start of Week Day", "Determines what day is the start of a week.", true, DayOfWeek.Sunday, order: 16 )]

    [BooleanField( "Set Page Title", "Determines if the block should set the page title with the calendar name.", false, order: 17 )]

    [TextField( "Campus Parameter Name", "The page parameter name that contains the id of the campus entity.", false, "campusId", order: 18 )]
    [TextField( "Category Parameter Name", "The page parameter name that contains the id of the category entity.", false, "categoryId", order: 19 )]
    [TextField( "Priority Attribute Key", "The calendar attribute which controls the ordering of the events", true, "EventPriority", order: 20 )]
    [IntegerField( "Limit", "Maximum number of items to display", defaultValue: 30 )]
    [CustomCheckboxListField( "Cache Tags", "Cached tags are used to link cached content so that it can be expired as a group", CACHE_TAG_LIST, false, key: "CacheTags", order: 31 )]
    public partial class CalendarLava : Rock.Web.UI.RockBlock
    {
        private const string CACHE_TAG_LIST = @"
            SELECT CAST([DefinedValue].[Value] AS VARCHAR) AS [Value], [DefinedValue].[Value] AS [Text]
            FROM[DefinedType]
            JOIN[DefinedValue] ON[DefinedType].[Id] = [DefinedValue].[DefinedTypeId]
            WHERE[DefinedType].[Guid] = 'BDF73089-9154-40C1-90E4-74518E9937DC'";

        #region Fields

        private int _calendarId = 0;
        private string _calendarName = string.Empty;
        private DayOfWeek _firstDayOfWeek = DayOfWeek.Sunday;

        protected bool CampusPanelOpen { get; set; }
        protected bool CampusPanelClosed { get; set; }
        protected bool CategoryPanelOpen { get; set; }
        protected bool CategoryPanelClosed { get; set; }

        #endregion

        #region Properties

        private String ViewMode { get; set; }
        private DateTime? SelectedDate { get; set; }
        private DateTime? FilterStartDate { get; set; }
        private DateTime? FilterEndDate { get; set; }

        #endregion

        #region Base ControlMethods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            ViewMode = ViewState["ViewMode"] as String;
            SelectedDate = ViewState["SelectedDate"] as DateTime?;
            FilterStartDate = ViewState["FilterStartDate"] as DateTime?;
            FilterEndDate = ViewState["FilterEndDate"] as DateTime?;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            _firstDayOfWeek = GetAttributeValue( "StartofWeekDay" ).ConvertToEnum<DayOfWeek>();

            var eventCalendar = new EventCalendarService( new RockContext() ).Get( GetAttributeValue( "EventCalendar" ).AsGuid() );
            if ( eventCalendar != null )
            {
                _calendarId = eventCalendar.Id;
                _calendarName = eventCalendar.Name;
            }

            CampusPanelOpen = GetAttributeValue( "CampusFilterDisplayMode" ) == "3";
            CampusPanelClosed = GetAttributeValue( "CampusFilterDisplayMode" ) == "4";
            CategoryPanelOpen = !String.IsNullOrWhiteSpace( GetAttributeValue( "FilterCategories" ) ) && GetAttributeValue( "CategoryFilterDisplayMode" ) == "3";
            CategoryPanelClosed = !String.IsNullOrWhiteSpace( GetAttributeValue( "FilterCategories" ) ) && GetAttributeValue( "CategoryFilterDisplayMode" ) == "4";

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

            nbMessage.Visible = false;

            if ( SetFilterControls() )
            {
                pnlDetails.Visible = true;
                BindData();
            }
            else
            {
                pnlDetails.Visible = false;
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
            ViewState["ViewMode"] = ViewMode;
            ViewState["SelectedDate"] = SelectedDate;
            ViewState["FilterStartDate"] = FilterStartDate;
            ViewState["FilterEndDate"] = FilterEndDate;

            return base.SaveViewState();
        }

        protected override void OnPreRender( EventArgs e )
        {
            if ( GetAttributeValue( "SetPageTitle" ).AsBoolean() && !string.IsNullOrWhiteSpace( _calendarName ) )
            {
                string pageTitle = _calendarName.EndsWith( "Calendar", StringComparison.OrdinalIgnoreCase ) ? _calendarName : string.Format( "{0} Calendar", _calendarName );
                RockPage.PageTitle = pageTitle;
                RockPage.BrowserTitle = String.Format( "{0} | {1}", pageTitle, RockPage.Site.Name );
                RockPage.Header.Title = String.Format( "{0} | {1}", pageTitle, RockPage.Site.Name );
            }

            btnDay.CssClass = "btn btn-default" + ( ViewMode == "Day" ? " active" : "" );
            btnWeek.CssClass = "btn btn-default" + ( ViewMode == "Week" ? " active" : "" );
            btnMonth.CssClass = "btn btn-default" + ( ViewMode == "Month" ? " active" : "" );
            btnYear.CssClass = "btn btn-default" + ( ViewMode == "Year" ? " active" : "" );

            base.OnPreRender( e );
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            if ( SetFilterControls() )
            {
                pnlDetails.Visible = true;
                BindData();
            }
            else
            {
                pnlDetails.Visible = false;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the SelectionChanged event of the calEventCalendar control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void calEventCalendar_SelectionChanged( object sender, EventArgs e )
        {
            SelectedDate = calEventCalendar.SelectedDate;
            ResetCalendarSelection();
            BindData();
        }

        /// <summary>
        /// Handles the DayRender event of the calEventCalendar control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DayRenderEventArgs"/> instance containing the event data.</param>
        protected void calEventCalendar_DayRender( object sender, DayRenderEventArgs e )
        {
        }

        /// <summary>
        /// Handles the VisibleMonthChanged event of the calEventCalendar control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MonthChangedEventArgs"/> instance containing the event data.</param>
        protected void calEventCalendar_VisibleMonthChanged( object sender, MonthChangedEventArgs e )
        {
            SelectedDate = e.NewDate;
            ResetCalendarSelection();
            BindData();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the cblCampus control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cblCampus_SelectedIndexChanged( object sender, EventArgs e )
        {
            BindData();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the cblCategory control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cblCategory_SelectedIndexChanged( object sender, EventArgs e )
        {
            BindData();
        }

        protected void lbDateRangeRefresh_Click( object sender, EventArgs e )
        {
            FilterStartDate = drpDateRange.LowerValue;
            FilterEndDate = drpDateRange.UpperValue;
            BindData();
        }

        /// <summary>
        /// Handles the Click event of the btnWeek control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnViewMode_Click( object sender, EventArgs e )
        {
            var btnViewMode = sender as BootstrapButton;
            if ( btnViewMode != null )
            {
                ViewMode = btnViewMode.Text;
                ResetCalendarSelection();
                BindData();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads and displays the event item occurrences
        /// </summary>
        private void BindData()
        {
            List<int> campusIds = cblCampus.Items.OfType<ListItem>().Where( l => l.Selected ).Select( a => a.Value.AsInteger() ).ToList();
            List<int> categories = cblCategory.Items.OfType<ListItem>().Where( l => l.Selected ).Select( a => a.Value.AsInteger() ).ToList();

            var cacheKey = string.Format( "{0}^{1}^{2}CalendarLava",
                                            string.Join( "-", campusIds ),
                                            string.Join( "-", categories ),
                                            BlockCache.Id.ToString() );
            var content = RockCache.Get( cacheKey, System.Globalization.CultureInfo.CurrentCulture.ToString() );
            if ( content != null && !string.IsNullOrWhiteSpace( ( string ) content ) )
            {
                lOutput.Text = ( string ) content;
                return;
            }

            var rockContext = new RockContext();
            var eventItemOccurrenceService = new EventItemOccurrenceService( rockContext );
            var attributeService = new AttributeService( rockContext );
            var attributeValueService = new AttributeValueService( rockContext );

            int calendarItemEntityTypeId = EntityTypeCache.GetId( typeof( EventCalendarItem ) ).Value;

            // Grab events
            var firstQry = eventItemOccurrenceService
                    .Queryable( "EventItem, EventItem.EventItemAudiences,Schedule" )
                    .Where( e =>
                        e.EventItem.EventCalendarItems.Any( i => i.EventCalendarId == _calendarId ) &&
                        e.EventItem.IsActive &&
                        e.EventItem.IsApproved ).ToList();

            var qry = firstQry
                    .GroupJoin(
                        attributeValueService.Queryable().Where( av => av.Attribute.EntityTypeId == ( int? ) calendarItemEntityTypeId ),
                        obj => obj.EventItem.EventCalendarItems.Where( i => i.EventCalendarId == _calendarId ).Select( i => i.Id ).FirstOrDefault(),
                        av => av.EntityId,
                        ( obj, av ) => new
                        {
                            EventItemOccurrence = obj,
                            EventCalendarItemAttributeValues = av,
                        }
                    );


            // Filter by campus
            if ( campusIds.Any() )
            {
                qry = qry
                    .Where( c =>
                        !c.EventItemOccurrence.CampusId.HasValue ||    // All
                        campusIds.Contains( c.EventItemOccurrence.CampusId.Value ) );
            }

            // Filter by Category
            if ( categories.Any() )
            {
                qry = qry
                    .Where( i => i.EventItemOccurrence.EventItem.EventItemAudiences
                        .Any( c => categories.Contains( c.DefinedValueId ) ) );
            }

            // Get the beginning and end dates
            var today = RockDateTime.Now;
            var filterStart = FilterStartDate.HasValue ? FilterStartDate.Value : today;
            var monthStart = new DateTime( filterStart.Year, filterStart.Month, 1 );
            var rangeStart = monthStart.AddMonths( -1 );
            var rangeEnd = monthStart.AddMonths( 2 );
            var beginDate = FilterStartDate.HasValue ? FilterStartDate.Value : rangeStart;
            var endDate = FilterEndDate.HasValue ? FilterEndDate.Value : rangeEnd;

            endDate = endDate.AddDays( 1 ).AddMilliseconds( -1 );

            var scheduleEntityType = EntityTypeCache.Get( Rock.SystemGuid.EntityType.SCHEDULE ).Id;
            var attributeId = attributeService.Queryable()
                .Where( a => a.EntityTypeId == scheduleEntityType && a.Key == "NextStartDate" )
                .FirstOrDefault()
                .Id;

            // Get the occurrences
            var occurrences = qry.ToList();

            var eventSchedules = occurrences.Select( o => o.EventItemOccurrence.Schedule.Id ).ToList();

            var schedules = attributeValueService.Queryable()
                .Where( av => av.AttributeId == attributeId )
                .Where( av => eventSchedules.Contains( av.EntityId ?? 0 ) )
                .ToDictionary( av => av.EntityId, av => av.Value );

            var occurrencesWithDates = occurrences
                .Select( o => new EventOccurrenceDate
                {
                    EventItemOccurrence = o.EventItemOccurrence,
                    EventCalendarItemAttributeValues = o.EventCalendarItemAttributeValues,
                    Date = schedules.ContainsKey( o.EventItemOccurrence.Schedule.Id ) ? schedules[o.EventItemOccurrence.Schedule.Id].AsDateTime() ?? new DateTime() : new DateTime()
                } )
                .Where( d => d.Date > RockDateTime.Now )
                .ToList();

            var priorityAttributeKey = GetAttributeValue( "PriorityAttributeKey" );

            var eventOccurrenceSummaries = new List<EventOccurrenceSummary>();
            foreach ( var occurrenceDates in occurrencesWithDates )
            {
                var eventItemOccurrence = occurrenceDates.EventItemOccurrence;

                if ( occurrenceDates.Date >= beginDate && occurrenceDates.Date < endDate )
                {
                    var primaryMinistry = DefinedValueCache.Get( occurrenceDates.EventCalendarItemAttributeValues.Where( av => av.AttributeKey == "PrimaryMinistry" ).Select( av => av.Value ).FirstOrDefault() );
                    var primaryMinistryImageGuid = "";
                    var primaryMinistryName = "";
                    if ( primaryMinistry != null )
                    {
                        primaryMinistryName = primaryMinistry.Value;
                        primaryMinistryImageGuid = primaryMinistry.GetAttributeValue( "CalendarImage" );
                    }

                    eventOccurrenceSummaries.Add( new EventOccurrenceSummary
                    {
                        EventItemOccurrence = eventItemOccurrence,
                        Name = eventItemOccurrence.EventItem.Name,
                        DateTime = occurrenceDates.Date,
                        Date = occurrenceDates.Date.ToShortDateString(),
                        Time = occurrenceDates.Date.ToShortTimeString(),
                        Campus = eventItemOccurrence.Campus != null ? eventItemOccurrence.Campus.Name : "All Campuses",
                        Location = eventItemOccurrence.Campus != null ? eventItemOccurrence.Campus.Name : "All Campuses",
                        LocationDescription = eventItemOccurrence.Location,
                        Description = eventItemOccurrence.EventItem.Description,
                        Summary = eventItemOccurrence.EventItem.Summary,
                        URLSlugs = occurrenceDates.EventCalendarItemAttributeValues.Where( av => av.AttributeKey == "URLSlugs" ).Select( av => av.Value ).FirstOrDefault(),
                        OccurrenceNote = eventItemOccurrence.Note.SanitizeHtml(),
                        DetailPage = String.IsNullOrWhiteSpace( eventItemOccurrence.EventItem.DetailsUrl ) ? null : eventItemOccurrence.EventItem.DetailsUrl,
                        Priority = occurrenceDates.EventCalendarItemAttributeValues.Where( av => av.AttributeKey == priorityAttributeKey ).Select( av => av.Value ).FirstOrDefault().AsIntegerOrNull() ?? int.MaxValue,
                        PrimaryMinistryImageGuid = primaryMinistryImageGuid,
                        PrimaryMinstryTitle = primaryMinistryName,
                        ImageHeaderText = occurrenceDates.EventCalendarItemAttributeValues.Where( av => av.AttributeKey == "ImageHeaderText" ).Select( av => av.Value ).FirstOrDefault(),
                        ImageHeaderTextSmall = occurrenceDates.EventCalendarItemAttributeValues.Where( av => av.AttributeKey == "ImageHeaderTextSmall" ).Select( av => av.Value ).FirstOrDefault()
                    } );

                }
            }

            var eventSummaries = eventOccurrenceSummaries
                .OrderBy( e => e.DateTime )
                .GroupBy( e => e.Name )
                .OrderBy( e => e.First().Priority )
                .Select( e => e.ToList() )
                .Take( GetAttributeValue( "Limit" ).AsInteger() )
                .ToList();

            eventOccurrenceSummaries = eventOccurrenceSummaries
                .OrderBy( e => e.DateTime )
                .ThenBy( e => e.Name )
                .ToList();

            var mergeFields = new Dictionary<string, object>();
            mergeFields.Add( "TimeFrame", ViewMode );
            mergeFields.Add( "DetailsPage", LinkedPageRoute( "DetailsPage" ) );
            mergeFields.Add( "EventItems", eventSummaries );
            mergeFields.Add( "EventItemOccurrences", eventOccurrenceSummaries );
            mergeFields.Add( "CurrentPerson", CurrentPerson );

            lOutput.Text = GetAttributeValue( "LavaTemplate" ).ResolveMergeFields( mergeFields, GetAttributeValue( "EnabledLavaCommands" ) );

            var minutes = GetAttributeValue( "CacheDuration" ).AsInteger();
            if ( minutes > 0 )
            {
                string cacheTags = GetAttributeValue( "CacheTags" ) ?? string.Empty;
                RockCache.AddOrUpdate( cacheKey,
                    System.Globalization.CultureInfo.CurrentCulture.ToString(),
                lOutput.Text, RockDateTime.Now.AddMinutes( minutes ),
                cacheTags );
            }
        }


        /// <summary>
        /// Loads the drop downs.
        /// </summary>
        private bool SetFilterControls()
        {
            // Get and verify the calendar id
            if ( _calendarId <= 0 )
            {
                ShowError( "Configuration Error", "The 'Event Calendar' setting has not been set correctly." );
                return false;
            }

            // Get and verify the view mode
            ViewMode = GetAttributeValue( "DefaultViewOption" );
            if ( !GetAttributeValue( string.Format( "Show{0}View", ViewMode ) ).AsBoolean() )
            {
                ShowError( "Configuration Error", string.Format( "The Default View Option setting has been set to '{0}', but the Show {0} View setting has not been enabled.", ViewMode ) );
                return false;
            }

            // Show/Hide calendar control
            pnlCalendar.Visible = GetAttributeValue( "ShowSmallCalendar" ).AsBoolean();

            // Get the first/last dates based on today's date and the viewmode setting
            var today = RockDateTime.Now;
            FilterStartDate = today;
            FilterEndDate = today;
            if ( ViewMode == "Week" )
            {
                FilterStartDate = today.StartOfWeek( _firstDayOfWeek );
                FilterEndDate = today.EndOfWeek( _firstDayOfWeek );
            }
            else if ( ViewMode == "Month" )
            {
                FilterStartDate = new DateTime( today.Year, today.Month, 1 );
                FilterEndDate = FilterStartDate.Value.AddMonths( 1 ).AddDays( -1 );
            }
            else if ( ViewMode == "Year" )
            {
                FilterEndDate = FilterStartDate.Value.AddYears( 1 ).AddDays( -1 );
            }

            // Setup small calendar Filter
            calEventCalendar.FirstDayOfWeek = _firstDayOfWeek.ConvertToInt().ToString().ConvertToEnum<FirstDayOfWeek>();
            calEventCalendar.SelectedDates.Clear();
            calEventCalendar.SelectedDates.SelectRange( FilterStartDate.Value, FilterEndDate.Value );

            // Setup Campus Filter
            rcwCampus.Visible = GetAttributeValue( "CampusFilterDisplayMode" ).AsInteger() > 1;
            cblCampus.DataSource = CampusCache.All();
            cblCampus.DataBind();

            //Check for Campus Parameter
            var campusId = PageParameter( GetAttributeValue( "CampusParameterName" ) ).AsIntegerOrNull();
            var campusStr = PageParameter( "Campus" );
            if ( campusId.HasValue )
            {
                //check if there's a campus with this id.
                var campus = CampusCache.Get( campusId.Value );
                if ( campus != null )
                {
                    cblCampus.SetValue( campusId.Value );
                }
            }
            else if ( !string.IsNullOrEmpty( campusStr ) )
            {
                //check if there's a campus with this name.
                campusStr = campusStr.Replace( " ", "" ).Replace( "-", "" );
                var campusCache = CampusCache.All().Where( c => c.Name.ToLower().Replace( " ", "" ).Replace( "-", "" ) == campusStr.ToLower() ).FirstOrDefault();
                if ( campusCache != null )
                {
                    cblCampus.SetValue( campusCache.Id );
                }
            }
            else
            {
                if ( GetAttributeValue( "EnableCampusContext" ).AsBoolean() )
                {
                    var contextCampus = RockPage.GetCurrentContext( EntityTypeCache.Get( "Rock.Model.Campus" ) ) as Campus;
                    if ( contextCampus != null )
                    {
                        cblCampus.SetValue( contextCampus.Id );
                    }
                }
            }

            // Setup Category Filter
            var selectedCategoryGuids = GetAttributeValue( "FilterCategories" ).SplitDelimitedValues( true ).AsGuidList();
            rcwCategory.Visible = selectedCategoryGuids.Any() && GetAttributeValue( "CategoryFilterDisplayMode" ).AsInteger() > 1;
            var definedType = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.MARKETING_CAMPAIGN_AUDIENCE_TYPE.AsGuid() );
            if ( definedType != null )
            {
                var categoryItems = definedType.DefinedValues.ToList();
                if ( selectedCategoryGuids.Count() > 0 )
                {
                    categoryItems = definedType.DefinedValues.Where( v => selectedCategoryGuids.Contains( v.Guid ) ).ToList();
                }
                cblCategory.DataSource = categoryItems;
                cblCategory.DataBind();
            }
            var categoryId = PageParameter( GetAttributeValue( "CategoryParameterName" ) ).AsIntegerOrNull();
            var ministrySlug = PageParameter( "ministry" ).ToLower();
            if ( categoryId.HasValue )
            {
                if ( definedType.DefinedValues.Where( v => ( selectedCategoryGuids.Contains( v.Guid ) || selectedCategoryGuids.Count() == 0 ) && v.Id == categoryId.Value ).FirstOrDefault() != null )
                {
                    cblCategory.SetValue( categoryId.Value );
                }

            }
            else if ( !string.IsNullOrEmpty( ministrySlug ) )
            {
                var definedValues = definedType.DefinedValues.Where( v => ( selectedCategoryGuids.Contains( v.Guid ) || selectedCategoryGuids.Count() == 0 ) ).ToList();
                DefinedTypeService definedTypeService = new DefinedTypeService( new RockContext() );
                var definedTypeModel = definedTypeService.Get( definedType.Guid );
                foreach ( var dv in definedTypeModel.DefinedValues )
                {
                    dv.LoadAttributes();
                    if ( dv.GetAttributeValue( "URLSlug" ) == ministrySlug )
                    {
                        cblCategory.SetValue( dv.Id );
                        break;
                    }
                }

            }

            // Date Range Filter
            drpDateRange.Visible = GetAttributeValue( "ShowDateRangeFilter" ).AsBoolean();
            lbDateRangeRefresh.Visible = drpDateRange.Visible;
            drpDateRange.LowerValue = FilterStartDate;
            drpDateRange.UpperValue = FilterEndDate;

            // Get the View Modes, and only show them if more than one is visible
            var viewsVisible = new List<bool> {
                GetAttributeValue( "ShowDayView" ).AsBoolean(),
                GetAttributeValue( "ShowWeekView" ).AsBoolean(),
                GetAttributeValue( "ShowMonthView" ).AsBoolean(),
                GetAttributeValue( "ShowYearView" ).AsBoolean()
            };
            var howManyVisible = viewsVisible.Where( v => v ).Count();
            btnDay.Visible = howManyVisible > 1 && viewsVisible[0];
            btnWeek.Visible = howManyVisible > 1 && viewsVisible[1];
            btnMonth.Visible = howManyVisible > 1 && viewsVisible[2];
            btnYear.Visible = howManyVisible > 1 && viewsVisible[3];

            // Set filter visibility
            bool showFilter = ( pnlCalendar.Visible || rcwCampus.Visible || rcwCategory.Visible || drpDateRange.Visible );
            pnlFilters.Visible = showFilter;
            pnlList.CssClass = showFilter ? "col-md-9" : "col-md-12";

            return true;
        }


        /// <summary>
        /// Resets the calendar selection. The control is configured for day selection, but selection will be changed to the week or month or year if that is the viewmode being used
        /// </summary>
        private void ResetCalendarSelection()
        {
            // Even though selection will be a single date due to calendar's selection mode, set the appropriate days
            if ( SelectedDate != null )
            {
                calEventCalendar.SelectedDate = SelectedDate.Value;
            }
            var selectedDate = calEventCalendar.SelectedDate;
            FilterStartDate = selectedDate;
            FilterEndDate = selectedDate;
            if ( ViewMode == "Week" )
            {
                FilterStartDate = selectedDate.StartOfWeek( _firstDayOfWeek );
                FilterEndDate = selectedDate.EndOfWeek( _firstDayOfWeek );
            }
            else if ( ViewMode == "Month" )
            {
                FilterStartDate = new DateTime( selectedDate.Year, selectedDate.Month, 1 );
                FilterEndDate = FilterStartDate.Value.AddMonths( 1 ).AddDays( -1 );
            }

            // Reset the selection
            calEventCalendar.SelectedDates.SelectRange( FilterStartDate.Value, FilterEndDate.Value );
        }

        private void SetCalendarFilterDates()
        {
            FilterStartDate = calEventCalendar.SelectedDates.Count > 0 ? calEventCalendar.SelectedDates[0] : ( DateTime? ) null;
            FilterEndDate = calEventCalendar.SelectedDates.Count > 0 ? calEventCalendar.SelectedDates[calEventCalendar.SelectedDates.Count - 1] : ( DateTime? ) null;
        }

        /// <summary>
        /// Shows the warning.
        /// </summary>
        /// <param name="heading">The heading.</param>
        /// <param name="message">The message.</param>
        private void ShowWarning( string heading, string message )
        {
            nbMessage.Heading = heading;
            nbMessage.Text = string.Format( "<p>{0}</p>", message );
            nbMessage.NotificationBoxType = NotificationBoxType.Danger;
            nbMessage.Visible = true;
        }

        /// <summary>
        /// Shows the error.
        /// </summary>
        /// <param name="heading">The heading.</param>
        /// <param name="message">The message.</param>
        private void ShowError( string heading, string message )
        {
            nbMessage.Heading = heading;
            nbMessage.Text = string.Format( "<p>{0}</p>", message );
            nbMessage.NotificationBoxType = NotificationBoxType.Danger;
            nbMessage.Visible = true;
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// A class to store event item occurrence data for liquid
        /// </summary>
        [DotLiquid.LiquidType( "EventItemOccurrence", "DateTime", "Name", "Date", "Time", "Campus", "Location",
            "LocationDescription", "Description", "Summary", "OccurrenceNote", "DetailPage",
            "Priority", "URLSlugs", "PrimaryMinistryImageGuid", "PrimaryMinstryTitle", "ImageHeaderText", "ImageHeaderTextSmall" )]
        public class EventOccurrenceSummary
        {
            public EventItemOccurrence EventItemOccurrence { get; set; }
            public DateTime DateTime { get; set; }
            public String Name { get; set; }
            public String Date { get; set; }
            public String Time { get; set; }
            public String Campus { get; set; }
            public String Location { get; set; }
            public String LocationDescription { get; set; }
            public String Summary { get; set; }
            public String Description { get; set; }
            public String OccurrenceNote { get; set; }
            public String DetailPage { get; set; }
            public int Priority { get; set; }
            public String URLSlugs { get; set; }
            public string PrimaryMinistryImageGuid { get; set; }
            public string PrimaryMinstryTitle { get; set; }
            public string ImageHeaderText { get; set; }
            public string ImageHeaderTextSmall { get; set; }
        }

        /// <summary>
        /// A class to store the event item occurrences dates
        /// </summary>
        public class EventOccurrenceDate
        {
            public EventItemOccurrence EventItemOccurrence { get; set; }
            public IEnumerable<AttributeValue> EventCalendarItemAttributeValues { get; set; }
            public DateTime Date { get; set; }
        }

        #endregion

    }
}
