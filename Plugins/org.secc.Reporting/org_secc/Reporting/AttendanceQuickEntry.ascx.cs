using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
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

        Metric _worshipAttendanceMetric = null;
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

        private Metric WorshipAttendanceMetric
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
            if (!Page.IsPostBack)
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

        private int? GetAttendance( DateTime serviceDate, int campusId, int scheduleId )
        {
            using (var rockContext = new RockContext())
            {
                var campusEntityType = EntityTypeCache.Get( typeof( Rock.Model.Campus ) );
                var scheduleEntityType = EntityTypeCache.Get( typeof( Rock.Model.Schedule ) );
                var sundayDate = serviceDate.SundayDate();

                var valueService = new MetricValueService( rockContext );
                var metricValue = valueService.Queryable().AsNoTracking()
                    .Where( v => v.MetricId == WorshipAttendanceMetric.Id )
                    .Where( v => v.MetricValueDateTime == sundayDate )
                    .Where( v => v.MetricValuePartitions.Where( p => p.MetricPartition.EntityTypeId == campusEntityType.Id ).Select( p => p.EntityId ).Contains( campusId ) )
                    .Where( v => v.MetricValuePartitions.Where( p => p.MetricPartition.EntityTypeId == scheduleEntityType.Id ).Select( p => p.EntityId ).Contains( scheduleId ) )
                    .Select( v => v.YValue )
                    .FirstOrDefault();

                return (int?) metricValue;
            }

        }

        private void LoadForm()
        {
            if(SelectedCampus == null)
            {
                SetNotification( "Campus Not Selected",
                    "Campus must be provided to enter worship attendance",
                    NotificationBoxType.Validation );
                pnlEntry.Visible = false;
                return;
            }

            var campusSchedules = WorshipSchedules.Where( s => s.CampusId == SelectedCampus.Id ).ToList();

            if(!campusSchedules.Any())
            {
                SetNotification( "No Services Found",
                    $"No Services found for today at {SelectedCampus.Name} Campus.",
                    NotificationBoxType.Info );
                pnlEntry.Visible = false;
                return;
            }

            lCampus.Text = SelectedCampus.Name;
            lDate.Text = RockDateTime.Now.ToShortDateString();

            ddlSchedule.Items.Clear();
            ddlSchedule.Items.Add( new ListItem( string.Empty, string.Empty ) );
            foreach (var item in campusSchedules)
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
            

            var cachedValue = RockCache.Get( _serviceCachceKey, true ) as List<WorshipScheduleSummary>;

            if(cachedValue !=  null)
            {
                _worshipSchedules = cachedValue;
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

                    service.Attendance = GetAttendance( service.StartDateTime, service.CampusId, service.ScheduleId );

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

            RockCache.AddOrUpdate( _serviceCachceKey, "", worshipSchedule, new TimeSpan(0, expirationMinutes, 0) );


        }

        private void SaveAttendance()
        {
            var worshipService = WorshipSchedules
                .Where( s => s.CampusId == SelectedCampus.Id )
                .Where( s => s.ScheduleId == ddlSchedule.SelectedValueAsInt() )
                .SingleOrDefault();

            worshipService.Attendance = tbAttendance.Text.AsInteger();

            var campusEntityTypeId = EntityTypeCache.Get( typeof( Campus ) ).Id;
            int? campusPartitionId = null;

            var scheduleEntityTypeId = EntityTypeCache.Get( typeof( Schedule ) ).Id;
            int? schedulePartitionId = null;

            using (var partitionContext = new RockContext())
            {
                var metricPartitionService = new MetricPartitionService( partitionContext );
                campusPartitionId = metricPartitionService.Queryable().AsNoTracking()
                    .Where( p => p.MetricId == WorshipAttendanceMetric.Id )
                    .Where( p => p.EntityTypeId == campusEntityTypeId )
                    .Select( p => p.Id )
                    .SingleOrDefault();

                schedulePartitionId = metricPartitionService.Queryable().AsNoTracking()
                    .Where( p => p.MetricId == WorshipAttendanceMetric.Id )
                    .Where( p => p.EntityTypeId == scheduleEntityTypeId )
                    .Select( p => p.Id )
                    .SingleOrDefault();
            }

            using (var metricValueContext = new RockContext())
            {
                var sundaydate = RockDateTime.Today.SundayDate();

                //var metricValueService = new MetricValueService(metricValueContext).Queryable()
                //    .Where(v => v.MetricValueDateTime == sundaydate)
                //    .Where(v => v.MetricValuePartitions
                //                .Where(p => p.MetricPartitionId == campusPartitionId.Value)
                //                .Where(p => p.EntityId == ))
            }

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
        public int? Attendance { get; set; }

    }
}