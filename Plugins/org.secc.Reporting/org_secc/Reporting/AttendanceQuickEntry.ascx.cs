using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using org.secc.FamilyCheckin;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;


namespace RockWeb.Plugins.org_secc.Reporting

{
    [DisplayName( "Worship Attendance Entry" )]
    [Category( "SECC > Reporting" )]
    [Description( "Block for campuses to quickly enter their worship service attenance." )]

    [MetricField( "Worship Attendance Metric",
        Description = "The weekly metric for worship attendance.",
        IsRequired = true,
        Order = 0,
        Key = AttributeKey.WorshipAttendance )]
    [TextField( "Title",
        Description = "Panel Title",
        IsRequired = false,
        DefaultValue = "Worship Attendance Entry",
        Order = 1,
        Key = AttributeKey.PanelTitle )]
    [CategoryField( "Worship Attendance Parent Category",
        Description = "Parent Schedule Category for Weekly Worship Service Attendance.",
        EntityTypeName = "Rock.Model.Schedule",
        IsRequired = true,
        Order = 2,
        Key = AttributeKey.ScheduleCategory )]
    [AttributeField( "Schedule Campus Attribute",
        Description = "Attribute indicating which campus(es) that a schedule belongs to.",
        EntityTypeGuid = "0b2c38a7-d79c-4f85-9757-f1b045d32c8a",
        IsRequired = true,
        Key = AttributeKey.ScheduleCampus,
        Order = 3 )]

    public partial class AttendanceQuickEntry : RockBlock
    {
        public static class AttributeKey
        {
            public const string PanelTitle = "PanelTitle";
            public const string ScheduleCategory = "ScheduleCategory";
            public const string WorshipAttendance = "WorshipAttendance";
            public const string ScheduleCampus = "ScheduleCampus";
        }

        CampusCache _selectedCampus = null;
        string _serviceCachceKey;

        Rock.Model.Metric _worshipAttendanceMetric = null;
        List<WorshipScheduleSummary> _worshipSchedules = null;


        #region Properties
        private CampusCache SelectedCampus
        {
            get
            {
                if (_selectedCampus == null)
                {
                    SetCampus( false );
                }

                return _selectedCampus;
            }
            set
            {
                _selectedCampus = value;
            }
        }

        private Rock.Model.Metric WorshipAttendanceMetric
        {
            get
            {
                if (_worshipAttendanceMetric == null)
                {
                    LoadMetric();
                }
                return _worshipAttendanceMetric;
            }
        }

        private List<WorshipScheduleSummary> WorshipSchedules
        {
            get
            {
                if (_worshipSchedules == null)
                {
                    LoadSchedules();
                }
                return _worshipSchedules;
            }
        }

        #endregion

        #region "Base Control Methods"

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            _serviceCachceKey = $"WorshipServiceTimes_Page{CurrentPageReference.PageId}";
            lbSubmit.Click += lbSubmit_Click;
            lbReset.Click += lbReset_Click;
            ddlSchedule.SelectedIndexChanged += ddlSchedule_SelectedIndexChanged;
        }



        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            SetNotification( String.Empty, String.Empty );
            if (!IsPostBack)
            {
                SetCampus( true );
                LoadForm();
            }
        }

        #endregion

        #region Events
        private void ddlSchedule_SelectedIndexChanged( object sender, EventArgs e )
        {
            var scheduleID = ddlSchedule.SelectedValueAsInt();

            if(scheduleID.HasValue)
            {
                var schedule = WorshipSchedules.SingleOrDefault( s => s.ScheduleId == scheduleID && s.CampusId == SelectedCampus.Id );

                tbAttendance.Text = schedule.Attendance.HasValue ? schedule.Attendance.ToString() : String.Empty;
            }
            else
            {
                tbAttendance.Text = String.Empty;
            }
        }

        private void lbReset_Click( object sender, EventArgs e )
        {
            LoadForm();
        }

        private void lbSubmit_Click( object sender, EventArgs e )
        {
            SaveAttendance();

        }
        #endregion

        #region Internal Methods

        private MetricValue GetMetricValue( WorshipScheduleSummary summary, RockContext context )
        {
            var campusEntityTypeId = EntityTypeCache.Get( typeof( Campus ) ).Id;
            var scheduleEntityTypeId = EntityTypeCache.Get( typeof( Schedule ) ).Id;

            var sundayDate = summary.StartDateTime.SundayDate();

            var metricValueService = new MetricValueService( context );
            var metricValuePartitionService = new MetricValuePartitionService( context );

            var campusPartition = metricValuePartitionService.Queryable()
                .Where( p => p.MetricPartition.MetricId == WorshipAttendanceMetric.Id )
                .Where( p => p.MetricPartition.EntityTypeId == campusEntityTypeId )
                .Where( p => p.EntityId == summary.CampusId );

            var schedulePartition = metricValuePartitionService.Queryable()
                .Where( p => p.MetricPartition.MetricId == WorshipAttendanceMetric.Id )
                .Where( p => p.MetricPartition.EntityTypeId == scheduleEntityTypeId )
                .Where( p => p.EntityId == summary.ScheduleId );



            var metricValue = metricValueService.Queryable()
                .Where( m => m.MetricId == WorshipAttendanceMetric.Id )
                .Where( m => m.MetricValueDateTime == sundayDate )
                .Join( campusPartition, m => m.Id, p => p.MetricValueId,
                    ( m, p ) => new { metric = m, campusId = p.EntityId } )
                .Join( schedulePartition, m => m.metric.Id, p => p.MetricValueId,
                    ( m, p ) => new { metric = m.metric, campusId = m.campusId, scheduleId = p.Id } )
                .Select( m => m.metric )
                .FirstOrDefault();

            return metricValue;
                
        }

        private void LoadForm()
        {
            if(SelectedCampus == null)
            {
                SetNotification( "<i class=\"fas fa-exclamation-triangle\"></i> Campus Not Selected",
                    "Campus must be provided to enter worship attendance",
                    NotificationBoxType.Validation );
                pnlEntry.Visible = false;
                return;
            }

            lPanelTitle.Text = GetAttributeValue( AttributeKey.PanelTitle );

            var campusSchedules = WorshipSchedules.Where( s => s.CampusId == SelectedCampus.Id ).ToList();

            if(!campusSchedules.Any())
            {
                SetNotification( "<i class=\"fas fa-exclamation-triangle\"></i> No Services Found",
                    $"No Services found for today at {SelectedCampus.Name} Campus.",
                    NotificationBoxType.Info );
                pnlEntry.Visible = false;
                return;
            }

            lCampus.Text = SelectedCampus.Name;
            lDate.Text = RockDateTime.Now.ToShortDateString();

            ddlSchedule.Items.Clear();
            ddlSchedule.Items.Add( new ListItem( string.Empty, string.Empty ) );
            foreach (var item in campusSchedules.OrderBy(s => s.StartDateTime))
            {
                var listItem = new ListItem( $"{item.StartDateTime: h:mm tt}", item.ScheduleId.ToString() );
                ddlSchedule.Items.Add( listItem );
            }
            tbAttendance.Text = string.Empty;

            pnlEntry.Visible = true;

        }

        private void LoadMetric()
        {
            using (var rockContext = new RockContext())
            {
                var metricGuid = GetAttributeValue( AttributeKey.WorshipAttendance ).AsGuid();

                _worshipAttendanceMetric = new MetricService( rockContext ).Get( metricGuid );
            }
        }

        private void LoadSchedules()
        {          

            var cachedValue = RockCache.Get( _serviceCachceKey, true ) as string;

            if(cachedValue !=  null)
            {
                _worshipSchedules = JsonConvert.DeserializeObject<List<WorshipScheduleSummary>>(cachedValue);
                return;
            }
            

            var rockContext = new RockContext();
            var baseCategoryGuid = GetAttributeValue( AttributeKey.ScheduleCategory ).AsGuid();

            var categoryService = new CategoryService( rockContext );
            var scheduleService = new ScheduleService( rockContext );
            var categoryIds = categoryService.GetAllDescendents( baseCategoryGuid )
                .Select( c => c.Id ).ToList();

            var campusAttributeGuid = GetAttributeValue( AttributeKey.ScheduleCampus ).AsGuid();
            var campusAttributeValues = new AttributeValueService( rockContext ).Queryable().AsNoTracking()
                .Where( v => v.Attribute.Guid == campusAttributeGuid );


            var rockSchedules = scheduleService.Queryable().AsNoTracking()
                .Join( campusAttributeValues, s => s.Id, v => v.EntityId,
                    ( s, v ) => new { Schedule = s, CampusValue = v.Value } )
                .Where( s => categoryIds.Contains( s.Schedule.CategoryId ?? -1 ) )
                .Where( s => s.Schedule.IsActive )
                .Where( s => s.CampusValue != null && s.CampusValue != "" )
                .ToList();

            var worshipSchedule = new List<WorshipScheduleSummary>();

            foreach (var schedule in rockSchedules)
            {
                var todaysOccurrences = schedule.Schedule
                    .GetScheduledStartTimes( RockDateTime.Today, RockDateTime.Today.AddDays( 1 ).AddSeconds( -1 ) );
                if (todaysOccurrences.Count == 0)
                {
                    continue;
                }
                foreach (var campusGuid in schedule.CampusValue.SplitDelimitedValues())
                {
                    var service = new WorshipScheduleSummary()
                    {
                        ScheduleId = schedule.Schedule.Id,
                        CampusId = CampusCache.Get( campusGuid ).Id,
                        WorshipSchedule = schedule.Schedule,
                        StartDateTime = todaysOccurrences.First(),
                    };

                    var metricValue = GetMetricValue( service, rockContext );
                    if(metricValue != null)
                    {
                        service.Attendance = (int?) metricValue.YValue;
                        service.MetricValueId = metricValue.Id;
                    }

                    worshipSchedule.Add( service );
                }
            }

            var expirationMinutes = 60;
            //expire cache before midnight
            if(RockDateTime.Now.AddMinutes(expirationMinutes) > RockDateTime.Today.AddDays(1))
            {
                expirationMinutes = RockDateTime.Today.AddDays( 1 ).Subtract( Rock.RockDateTime.Now ).Minutes;
            }

            _worshipSchedules = worshipSchedule;

            RockCache.AddOrUpdate( _serviceCachceKey, null, JsonConvert.SerializeObject(worshipSchedule), RockDateTime.Now.AddMinutes(expirationMinutes));


        }

        private void SaveAttendance()
        {
            var worshipService = WorshipSchedules
                .Where( s => s.CampusId == SelectedCampus.Id )
                .Where( s => s.ScheduleId == ddlSchedule.SelectedValueAsInt() )
                .SingleOrDefault();

            if (worshipService == null)
            {
                throw new Exception( "Worship Schedule not found." );
            }

            worshipService.Attendance = tbAttendance.Text.AsIntegerOrNull();
            using (var rockContext = new RockContext())
            {
                var campusEntityTypeId = EntityTypeCache.Get( typeof( Campus ) ).Id;
                var scheduleEntityTypeId = EntityTypeCache.Get( typeof( Schedule ) ).Id;

                var partitionService = new MetricPartitionService( rockContext );
                var campusPartition = partitionService.Queryable()
                    .Where( c => c.MetricId == WorshipAttendanceMetric.Id )
                    .Where( c => c.EntityTypeId == campusEntityTypeId )
                    .SingleOrDefault();

                var schedulePartition = partitionService.Queryable()
                    .Where( c => c.MetricId == WorshipAttendanceMetric.Id )
                    .Where( c => c.EntityTypeId == scheduleEntityTypeId )
                    .SingleOrDefault();

                var metricValueService = new MetricValueService( rockContext );
                MetricValue metricValue = null;

                if(worshipService.MetricValueId.HasValue)
                {
                    metricValue = metricValueService.Get( worshipService.MetricValueId.Value );
                }
                else
                {
                    metricValue = new MetricValue
                    {
                        MetricId = WorshipAttendanceMetric.Id,
                        MetricValueDateTime = worshipService.StartDateTime.SundayDate()
                    };
                    metricValue.MetricValuePartitions.Add( new MetricValuePartition
                    {
                        MetricPartitionId = campusPartition.Id,
                        EntityId = worshipService.CampusId
                    } );
                    metricValue.MetricValuePartitions.Add( new MetricValuePartition
                    {
                        MetricPartitionId = schedulePartition.Id,
                        EntityId = worshipService.ScheduleId
                    } );
                    metricValueService.Add( metricValue );
                }
                metricValue.YValue = worshipService.Attendance;

                rockContext.SaveChanges();

                if(!worshipService.MetricValueId.HasValue)
                {
                    worshipService.MetricValueId = metricValue.Id;
                }
            }

            RockCache.AddOrUpdate( _serviceCachceKey, JsonConvert.SerializeObject(WorshipSchedules) );
            SetNotification( "<i class=\"fas fa-check-square\"></i> Attendance Saved", "<br />Worship Attendance Successfully Saved", NotificationBoxType.Success);



        }

        private void SetCampus( bool reload )
        {
            var campusKey = $"{CurrentPageReference.PageId}_CampusId";
            int? campusId = null;

            if (reload)
            {
                ViewState.Remove( campusKey );
            }


            campusId = (int?) ViewState[campusKey];

            if (!campusId.HasValue)
            {
                campusId = PageParameter( "Campus" ).AsInteger();
                ViewState[campusKey] = campusId;
            }

            if (!campusId.HasValue)
            {
                SelectedCampus = null;
            }
            else
            {
                SelectedCampus = CampusCache.Get( campusId.Value );
            }
        }

        private void SetNotification(string title, string message, NotificationBoxType boxType = NotificationBoxType.Default)
        {
            nbMessage.Title = title;
            nbMessage.Text = message;
            nbMessage.NotificationBoxType = boxType;

            if(title.IsNullOrWhiteSpace() && message.IsNullOrWhiteSpace())
            {
                nbMessage.Visible = false;
            }
            else
            {
                nbMessage.Visible = true;
            }
        }


        #endregion
    }

    class WorshipScheduleSummary
    {
        public int ScheduleId { get; set; }
        public int CampusId { get; set; }
        public Schedule WorshipSchedule { get; set; }
        public DateTime StartDateTime { get; set; }
        public int? MetricValueId { get; set; }
        public int? Attendance { get; set; }

    }
}