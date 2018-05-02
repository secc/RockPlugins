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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using System.Data.Entity;

namespace RockWeb.Plugins.org_secc.Reporting
{
    /// <summary>
    /// Block for easily adding/editing metric values for any metric that has partitions of campus and service time.
    /// </summary>
    [DisplayName( "Service Metrics Entry" )]
    [Category( "SECC > Reporting" )]
    [Description( "Block for easily adding/editing metric values for any metric that has partitions of campus and service time." )]

    [CategoryField( "Schedule Categories", "The schedule categories to use for list of service times.  Note that this requires a campus attribute to be selected below.", true, "Rock.Model.Schedule", "", "", true, "", "", 0 )]
    [IntegerField( "Weeks Back", "The number of weeks back to display in the 'Week of' selection.", false, 8, "", 2 )]
    [IntegerField( "Weeks Ahead", "The number of weeks ahead to display in the 'Week of' selection.", false, 0, "", 3 )]
    [MetricCategoriesField( "Metric Categories", "Select the metric categories to display (note: only metrics in those categories with a campus and scheudle partition will displayed).", true, "", "", 4 )]
    [AttributeField( Rock.SystemGuid.EntityType.SCHEDULE, "Campus Attribute", "The campus attribute to use for filtering the schedules", false, false, order: 1 )]
    [IntegerField( "Deadline in Minutes", "The number of minutes after the start time of a service when anyone other than the champion of a metric can enter/edit information.", false, order:0, key: "DeadlineMinutes", category:"Deadlines (Pick either Minutes or Day/Time)" )]
    [DayOfWeekField( "Deadline Day of week", "The day of the week to set as a deadline", false, category: "Deadlines (Pick either Minutes or Day/Time)", order: 1, key:"DeadlineDay")]
    [TimeField("Deadline Time", "The time on the day of the week for the deadline", false, category: "Deadlines (Pick either Minutes or Day/Time)", order: 2, key:"DeadlineTime")]
    public partial class ServiceMetricsEntry : Rock.Web.UI.RockBlock
    {
        #region Fields

        private int? _selectedCampusId { get; set; }
        private DateTime? _selectedWeekend { get; set; }
        private int? _selectedServiceId { get; set; }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );
            _selectedCampusId = ViewState["SelectedCampusId"] as int?;
            _selectedWeekend = ViewState["SelectedWeekend"] as DateTime?;
            _selectedServiceId = ViewState["SelectedServiceId"] as int?;
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
                _selectedCampusId = GetBlockUserPreference( "CampusId" ).AsIntegerOrNull();
                _selectedServiceId = GetBlockUserPreference( "ScheduleId" ).AsIntegerOrNull();

                if ( CheckSelection() )
                {
                    LoadDropDowns();
                    BindMetrics();
                }
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
            ViewState["SelectedCampusId"] = _selectedCampusId;
            ViewState["SelectedWeekend"] = _selectedWeekend;
            ViewState["SelectedServiceId"] = _selectedServiceId;
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
            BindMetrics();
        }


        protected void rptrSelection_ItemCommand( object source, RepeaterCommandEventArgs e )
        {
            switch( e.CommandName )
            {
                case "Campus":
                    _selectedCampusId = e.CommandArgument.ToString().AsIntegerOrNull();
                    break;
                case "Weekend":
                    _selectedWeekend = e.CommandArgument.ToString().AsDateTime();
                    break;
                case "Service":
                    _selectedServiceId = e.CommandArgument.ToString().AsIntegerOrNull();
                    break;
            }

            if ( CheckSelection() )
            {
                LoadDropDowns();
                BindMetrics();
            }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptrMetric control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptrMetric_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            if ( e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem )
            {
                var nbMetricValue = e.Item.FindControl( "nbMetricValue" ) as NumberBox;
                if ( nbMetricValue != null )
                {
                    nbMetricValue.ValidationGroup = BlockValidationGroup;
                }
                if ( e.Item.DataItem is ServiceMetric )
                {
                    nbMetricValue.ReadOnly = ( ( ServiceMetric ) e.Item.DataItem ).ReadOnly;
                }
            }
        }


        /// <summary>
        /// Handles the ItemDataBound event of the rptrMetricCategory control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptrMetricCategory_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var nbMetricsSaved = e.Item.FindControl( "nbMetricsSaved" ) as NotificationBox;
            var rptrMetric = e.Item.FindControl( "rptrMetric" ) as Repeater;
            var tbNote = e.Item.FindControl( "tbNote" ) as RockTextBox;
            var lMetricCategoryTitle = e.Item.FindControl( "lMetricCategoryTitle" ) as Label;

            nbMetricsSaved.Visible = false;

            var metricCategory = ( Category ) e.Item.DataItem;


            int campusEntityTypeId = EntityTypeCache.Read( typeof( Rock.Model.Campus ) ).Id;
            int scheduleEntityTypeId = EntityTypeCache.Read( typeof( Rock.Model.Schedule ) ).Id;

            int? campusId = bddlCampus.SelectedValueAsInt();
            int? scheduleId = bddlService.SelectedValueAsInt();
            DateTime? weekend = bddlWeekend.SelectedValue.AsDateTime();

            var serviceMetricValues = new List<ServiceMetric>();
            var notes = new List<string>();

            // Check to see if we are past the deadline
            Boolean readOnlyMetric = false;
            var services = GetServices(CampusCache.Read(campusId.Value));
            var selectedService = services.Where(s => s.Id == scheduleId.Value).FirstOrDefault();
            if (selectedService != null && selectedService.GetScheduledStartTimes(weekend.Value.AddDays(-6), weekend.Value.AddSeconds(86399)).Any())
            {
                int? minutes = GetAttributeValue("DeadlineMinutes").AsIntegerOrNull();
                DateTime service = selectedService.GetScheduledStartTimes(weekend.Value.AddDays(-6), weekend.Value.AddSeconds(86399)).FirstOrDefault();
                if (minutes.HasValue)
                {
                    var theTmp = DateTime.Now.Subtract(service).TotalMinutes;
                    if (DateTime.Now.Subtract(service).TotalMinutes >= minutes.Value)
                    {
                        readOnlyMetric = true;
                    }
                }
                var weekday = GetAttributeValue("DeadlineDay").AsIntegerOrNull();
                var time = GetAttributeValue("DeadlineTime");
                if (weekday.HasValue && !string.IsNullOrWhiteSpace(time))
                {
                    int daysUntilDay = (weekday.Value - (int)service.DayOfWeek + 7) % 7;
                    DateTime expireDate = service.AddDays(daysUntilDay).Date.Add(Convert.ToDateTime(time).TimeOfDay);
                    if (expireDate < DateTime.Now)
                    {
                        readOnlyMetric = true;
                    }
                }
            }

            if ( metricCategory != null )
            {
                using ( var rockContext = new RockContext() )
                {
                    lMetricCategoryTitle.Text = metricCategory.Name;

                    var metricValueService = new MetricValueService( rockContext );
                    foreach ( var metric in new MetricService( rockContext )
                        .Queryable()
                        .Where( m =>
                            m.MetricCategories.Where(mc => mc.CategoryId == metricCategory.Id).Any() &&
                            m.MetricPartitions.Count == 2 &&
                            m.MetricPartitions.Any( p => p.EntityTypeId.HasValue && p.EntityTypeId.Value == campusEntityTypeId ) &&
                            m.MetricPartitions.Any( p => p.EntityTypeId.HasValue && p.EntityTypeId.Value == scheduleEntityTypeId ) )
                        .OrderBy( m => m.Title )
                        .Select( m => new
                        {
                            m.Id,
                            m.Title,
                            CampusPartitionId = m.MetricPartitions.Where( p => p.EntityTypeId.HasValue && p.EntityTypeId.Value == campusEntityTypeId ).Select( p => p.Id ).FirstOrDefault(),
                            SchedulePartitionId = m.MetricPartitions.Where( p => p.EntityTypeId.HasValue && p.EntityTypeId.Value == scheduleEntityTypeId ).Select( p => p.Id ).FirstOrDefault(),
                            m.MetricChampionPersonAliasId
                        } ) )
                    {
                        var serviceMetric = new ServiceMetric( metric.Id, metric.Title, CurrentPerson.Aliases.Where( a => a.Id == metric.MetricChampionPersonAliasId ).Any() ? false : readOnlyMetric );

                        if ( campusId.HasValue && weekend.HasValue && scheduleId.HasValue )
                        {
                            var metricValue = metricValueService
                                .Queryable().AsNoTracking()
                                .Where( v =>
                                    v.MetricId == metric.Id &&
                                    v.MetricValueDateTime.HasValue && v.MetricValueDateTime.Value == weekend.Value &&
                                    v.MetricValuePartitions.Count == 2 &&
                                    v.MetricValuePartitions.Any( p => p.MetricPartitionId == metric.CampusPartitionId && p.EntityId.HasValue && p.EntityId.Value == campusId.Value ) &&
                                    v.MetricValuePartitions.Any( p => p.MetricPartitionId == metric.SchedulePartitionId && p.EntityId.HasValue && p.EntityId.Value == scheduleId.Value ) )
                                .FirstOrDefault();

                            if ( metricValue != null )
                            {
                                serviceMetric.Value = metricValue.YValue;

                                if ( !string.IsNullOrWhiteSpace( metricValue.Note ) &&
                                    !notes.Contains( metricValue.Note ) )
                                {
                                    notes.Add( metricValue.Note );
                                }

                            }
                        }

                        serviceMetricValues.Add( serviceMetric );
                    }

                    rptrMetric.DataSource = serviceMetricValues;
                    rptrMetric.DataBind();

                    tbNote.Text = notes.AsDelimited( Environment.NewLine + Environment.NewLine );
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            int campusEntityTypeId = EntityTypeCache.Read( typeof( Rock.Model.Campus ) ).Id;
            int scheduleEntityTypeId = EntityTypeCache.Read( typeof( Rock.Model.Schedule ) ).Id;

            int? campusId = bddlCampus.SelectedValueAsInt();
            int? scheduleId = bddlService.SelectedValueAsInt();
            DateTime? weekend = bddlWeekend.SelectedValue.AsDateTime();

            if ( campusId.HasValue && scheduleId.HasValue && weekend.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var metricService = new MetricService( rockContext );
                    var metricValueService = new MetricValueService( rockContext );

                    foreach (RepeaterItem categoryItem in rptrMetricCategory.Items)
                    {
                        var btnSave = categoryItem.FindControl("btnSave") as LinkButton;

                        if (btnSave.CommandArgument == ((LinkButton)sender).CommandArgument)
                        {
                            var tbNote = categoryItem.FindControl("tbNote") as RockTextBox;
                            var rptrMetric = categoryItem.FindControl("rptrMetric") as Repeater;

                            foreach (RepeaterItem item in rptrMetric.Items)
                            {
                                var hfMetricIId = item.FindControl("hfMetricId") as HiddenField;
                                var nbMetricValue = item.FindControl("nbMetricValue") as NumberBox;

                                if (hfMetricIId != null && nbMetricValue != null && nbMetricValue.ReadOnly == false)
                                {
                                    int metricId = hfMetricIId.ValueAsInt();
                                    var metric = new MetricService(rockContext).Get(metricId);

                                    if (metric != null)
                                    {
                                        int campusPartitionId = metric.MetricPartitions.Where(p => p.EntityTypeId.HasValue && p.EntityTypeId.Value == campusEntityTypeId).Select(p => p.Id).FirstOrDefault();
                                        int schedulePartitionId = metric.MetricPartitions.Where(p => p.EntityTypeId.HasValue && p.EntityTypeId.Value == scheduleEntityTypeId).Select(p => p.Id).FirstOrDefault();

                                        var metricValue = metricValueService
                                            .Queryable()
                                            .Where(v =>
                                               v.MetricId == metric.Id &&
                                               v.MetricValueDateTime.HasValue && v.MetricValueDateTime.Value == weekend.Value &&
                                               v.MetricValuePartitions.Count == 2 &&
                                               v.MetricValuePartitions.Any(p => p.MetricPartitionId == campusPartitionId && p.EntityId.HasValue && p.EntityId.Value == campusId.Value) &&
                                               v.MetricValuePartitions.Any(p => p.MetricPartitionId == schedulePartitionId && p.EntityId.HasValue && p.EntityId.Value == scheduleId.Value))
                                            .FirstOrDefault();

                                        if (metricValue == null)
                                        {
                                            metricValue = new MetricValue();
                                            metricValue.MetricValueType = MetricValueType.Measure;
                                            metricValue.MetricId = metric.Id;
                                            metricValue.MetricValueDateTime = weekend.Value;
                                            metricValueService.Add(metricValue);

                                            var campusValuePartition = new MetricValuePartition();
                                            campusValuePartition.MetricPartitionId = campusPartitionId;
                                            campusValuePartition.EntityId = campusId.Value;
                                            metricValue.MetricValuePartitions.Add(campusValuePartition);

                                            var scheduleValuePartition = new MetricValuePartition();
                                            scheduleValuePartition.MetricPartitionId = schedulePartitionId;
                                            scheduleValuePartition.EntityId = scheduleId.Value;
                                            metricValue.MetricValuePartitions.Add(scheduleValuePartition);
                                        }

                                        metricValue.YValue = nbMetricValue.Text.AsDecimalOrNull();
                                        metricValue.Note = tbNote.Text;
                                    }
                                }
                            }
                        }
                    }
                    rockContext.SaveChanges();
                }

                /*nbMetricsSaved.Text = string.Format( "Your metrics for the {0} service on {1} at the {2} Campus have been saved.",
                    bddlService.SelectedItem.Text, bddlWeekend.SelectedItem.Text, bddlCampus.SelectedItem.Text );
                nbMetricsSaved.Visible = true;
                */

                BindMetrics();

            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the filter controls.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void bddl_SelectionChanged( object sender, EventArgs e )
        {
            BindMetrics();
        }

        #endregion

        #region Methods

        private bool CheckSelection()
        {
            // If campus and schedule have been selected before, assume current weekend
            if ( _selectedCampusId.HasValue && _selectedServiceId.HasValue && !_selectedWeekend.HasValue )
            {
                _selectedWeekend = RockDateTime.Today.SundayDate();
            }

            var options = new List<ServiceMetricSelectItem>();

            if ( !_selectedCampusId.HasValue )
            {
                lSelection.Text = "Select Location:";
                foreach ( var campus in GetCampuses() )
                {
                    options.Add( new ServiceMetricSelectItem( "Campus", campus.Id.ToString(), campus.Name ) );
                }
            }

            if ( !options.Any() && !_selectedWeekend.HasValue )
            {

                var weeksAhead = GetAttributeValue( "WeeksAhead" ).AsInteger();
                lSelection.Text = "Select Week of:";
                foreach ( var weekend in GetWeekendDates( 1, weeksAhead ) )
                {
                    options.Add( new ServiceMetricSelectItem( "Weekend", weekend.ToString( "o" ), "Sunday " + weekend.ToShortDateString() ) );
                }
            }

            if ( !options.Any() && !_selectedServiceId.HasValue )
            {
                lSelection.Text = "Select Service Time:";
                foreach ( var service in GetServices(CampusCache.Read( _selectedCampusId.Value ) ) )
                {
                    options.Add( new ServiceMetricSelectItem( "Service", service.Id.ToString(), service.Name ) );
                }
            }

            if ( options.Any() )
            {
                rptrSelection.DataSource = options;
                rptrSelection.DataBind();

                pnlSelection.Visible = true;
                pnlMetrics.Visible = false;

                return false;
            }
            else
            {
                pnlSelection.Visible = false;
                pnlMetrics.Visible = true;

                return true;
            }
        }

        private void BuildCampusSelection()
        {
            foreach ( var campus in CampusCache.All()
                .Where( c => c.IsActive.HasValue && c.IsActive.Value )
                .OrderBy( c => c.Name ) )
            {
                bddlCampus.Items.Add( new ListItem( campus.Name, campus.Id.ToString() ) );
            }
        }

        /// <summary>
        /// Loads the drop downs.
        /// </summary>
        private void LoadDropDowns()
        {
            bddlCampus.Items.Clear();
            bddlWeekend.Items.Clear();
            bddlService.Items.Clear();

            // Load Campuses
            foreach ( var campus in GetCampuses() )
            {
                bddlCampus.Items.Add( new ListItem( campus.Name, campus.Id.ToString() ) );
            }
            bddlCampus.SetValue( _selectedCampusId.Value );
            bddlCampus.SelectedIndexChanged += BddlCampus_SelectedIndexChanged;

            // Load Weeks
            var weeksBack = GetAttributeValue( "WeeksBack" ).AsInteger();
            var weeksAhead = GetAttributeValue( "WeeksAhead" ).AsInteger();
            foreach ( var date in GetWeekendDates( weeksBack, weeksAhead ) )
            {
                bddlWeekend.Items.Add( new ListItem( "Sunday " + date.ToShortDateString(), date.ToString( "o" ) ) );
            }
            bddlWeekend.SetValue( _selectedWeekend.Value.ToString( "o" ) );

            // Load service times
            foreach( var service in GetServices( CampusCache.Read( _selectedCampusId.Value ) ) )
            {
                bddlService.Items.Add( new ListItem( service.Name, service.Id.ToString() ) );
            }
            if ( _selectedServiceId.HasValue )
            {
                bddlService.SetValue( _selectedServiceId.Value );
            }
        }

        private void BddlCampus_SelectedIndexChanged( object sender, EventArgs e )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the campuses.
        /// </summary>
        /// <returns></returns>
        private List<CampusCache> GetCampuses()
        {
            var campuses = new List<CampusCache>();

            foreach ( var campus in CampusCache.All()
                .Where( c => c.IsActive.HasValue && c.IsActive.Value )
                .OrderBy( c => c.Name ) )
            {
                campuses.Add( campus );
            }

            return campuses;
        }

        /// <summary>
        /// Gets the weekend dates.
        /// </summary>
        /// <returns></returns>
        private List<DateTime> GetWeekendDates( int weeksBack, int weeksAhead )
        {
            var dates = new List<DateTime>();

            // Load Weeks
            var sundayDate = RockDateTime.Today.SundayDate();
            var daysBack = weeksBack * 7;
            var daysAhead = weeksAhead * 7;
            var startDate = sundayDate.AddDays( 0 - daysBack );
            var date = sundayDate.AddDays( daysAhead );
            while ( date >= startDate )
            {
                dates.Add( date );
                date = date.AddDays( -7 );
            }

            return dates;
        }

        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <returns></returns>
        private List<Schedule> GetServices( CampusCache campus = null )
        {
            var services = new List<Schedule>();
            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "ScheduleCategories" ) ) )
            {
                List<Guid> categoryGuids = GetAttributeValue( "ScheduleCategories" ).Split( ',' ).Select( g => g.AsGuid() ).ToList();
                string campusAttributeGuid = GetAttributeValue( "CampusAttribute" );
                
                DateTime? weekend = _selectedWeekend;
                using ( var rockContext = new RockContext() )
                {
                    var attributeValueQry = new AttributeValueService( rockContext ).Queryable();
                    foreach ( Guid categoryGuid in categoryGuids )
                    {

                        var scheduleCategory = CategoryCache.Read( categoryGuid );
                        if ( scheduleCategory != null && campus != null )
                        {
                            var schedules = new ScheduleService( rockContext )
                                .Queryable().AsNoTracking()
                                .Where( s => s.CategoryId == scheduleCategory.Id )
                                .Join(
                                    attributeValueQry.Where( av => av.Attribute.Guid.ToString() == campusAttributeGuid
                                                                    && av.Value.Contains( campus.Guid.ToString() ) ),
                                    p => p.Id,
                                    av => av.EntityId,
                                    ( p, av ) => new { Schedule = p, Value = av.Value } );
                            // Check to see if the event was applicable the week for which we are entering data
                            foreach ( var schedule in schedules )
                            {
                                var occurrences = ScheduleICalHelper.GetOccurrences( schedule.Schedule.GetCalenderEvent(), weekend.Value.Date.AddDays( -6 ), weekend.Value.Date.AddDays( 1 ) );
                                if ( occurrences.Count > 0 )
                                {
                                    services.Add( schedule.Schedule );
                                }
                            }

                        }
                        else if ( scheduleCategory != null )
                        {
                            foreach ( var schedule in new ScheduleService( rockContext )
                                .Queryable().AsNoTracking()
                                .Where( s =>
                                    s.CategoryId.HasValue &&
                                    s.CategoryId.Value == scheduleCategory.Id )
                                .OrderBy( s => s.Name ) )
                            {
                                services.Add( schedule );
                            }
                        }
                    }
                }
            }
            return services.OrderBy( s => s.NextStartDateTime.HasValue ? s.NextStartDateTime.Value.Ticks : s.EffectiveEndDate.HasValue ? s.EffectiveEndDate.Value.Ticks : 0 ).ToList();
        }

        /// <summary>
        /// Binds the metrics.
        /// </summary>
        private void BindMetrics()
        {
            var serviceMetricValues = new List<ServiceMetric>();

            int campusEntityTypeId = EntityTypeCache.Read( typeof( Rock.Model.Campus ) ).Id;
            int scheduleEntityTypeId = EntityTypeCache.Read( typeof( Rock.Model.Schedule ) ).Id;

            int? campusId = bddlCampus.SelectedValueAsInt();
            int? scheduleId = bddlService.SelectedValueAsInt();
            DateTime? weekend = bddlWeekend.SelectedValue.AsDateTime();

            // If we changed the campus, make sure we reload the services
            if ( campusId != _selectedCampusId)
            {
                _selectedCampusId = campusId;

                bddlService.Items.Clear();
                bddlService.ClearSelection();
                scheduleId = null;

                // Load service times
                foreach ( var service in GetServices( CampusCache.Read( campusId.Value ) ) )
                {
                    bddlService.Items.Add( new ListItem( service.Name, service.Id.ToString() ) );
                    if ( _selectedServiceId == service.Id )
                    {
                        bddlService.SetValue( _selectedServiceId.Value );
                        scheduleId = _selectedServiceId.Value;
                    }
                }
                // Clear the repeater
                rptrMetricCategory.DataSource = null;
                rptrMetricCategory.DataBind();
                SaveViewState();
            }

            // If we changed the weekend, make sure we reload the services
            if ( weekend != _selectedWeekend )
            {
                _selectedWeekend = weekend;

                bddlService.Items.Clear();
                bddlService.ClearSelection();
                scheduleId = null;

                // Load service times
                foreach ( var service in GetServices( CampusCache.Read( campusId.Value ) ) )
                {
                    bddlService.Items.Add( new ListItem( service.Name, service.Id.ToString() ) );
                    if ( _selectedServiceId == service.Id ) {
                        bddlService.SetValue( _selectedServiceId.Value );
                        scheduleId = _selectedServiceId.Value;
                    }
                }
                // Clear the repeater
                rptrMetricCategory.DataSource = null;
                rptrMetricCategory.DataBind();
                SaveViewState();
            }


            if ( campusId.HasValue && scheduleId.HasValue && weekend.HasValue )
            {
                
                SetBlockUserPreference( "CampusId", campusId.HasValue ? campusId.Value.ToString() : "" );
                SetBlockUserPreference( "ScheduleId", scheduleId.HasValue ? scheduleId.Value.ToString() : "" );

                var metricCategories = MetricCategoriesFieldAttribute.GetValueAsGuidPairs( GetAttributeValue( "MetricCategories" ) );
                CategoryService categoryService = new CategoryService(new RockContext());
                var categories = categoryService.GetByGuids(metricCategories.Where(mc => mc.CategoryGuid.HasValue).Select(mc => mc.CategoryGuid.Value).ToList());

                rptrMetricCategory.DataSource = categories.ToList();
                rptrMetricCategory.DataBind();


            }


        }

        #endregion

    }

    public class ServiceMetricSelectItem
    {
        public string CommandName { get; set; }
        public string CommandArg { get; set; }
        public string OptionText { get; set; }
        public ServiceMetricSelectItem( string commandName, string commandArg, string optionText )
        {
            CommandName = commandName;
            CommandArg = commandArg;
            OptionText = optionText;   
        }
    }

    public class ServiceMetric
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal? Value { get; set; }
        public Boolean ReadOnly { get; set; }

        public ServiceMetric( int id, string name, Boolean readOnly = false )
        {
            Id = id;
            Name = name;
            ReadOnly = readOnly;
        }
    }
}