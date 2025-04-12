using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;


namespace org.secc.Jobs
{
    [DisallowConcurrentExecution]
    [DisplayName( "Easter SEKids Attendance Analytics Pull" )]
    [DateField( "Easter Sunday Date",
        Description = "The Sunday that Easter falls on.",
        IsRequired = true,
        Key = "EasterDate",
        Order = 0 )]
    [CategoryField( "SE!Kids Check-in Schedule Categories",
        Description = "The Schedule Categories that contains the SE!Kids Easter Check-in Schedules. Multi Select is Enabled.",
        AllowMultiple = true,
        EntityTypeName = "Rock.Model.Schedule",
        IsRequired = true,
        Key = "CheckinSchedules",
        Order = 1 )]
    [CategoryField( "Worship Schedule Categories",
        Description = "The Schedule Categories that contains the  Easter Worship Service Times. Multi Select is Enabled.",
        AllowMultiple = true,
        EntityTypeName = "Rock.Model.Schedule",
        IsRequired = true,
        Key = "WorshipSchedules",
        Order = 2 )]
    public class EasterChildrensAttendance : IJob
    {
        public void Execute( IJobExecutionContext context )
        {
            var jobMap = context.JobDetail.JobDataMap;

            var endDate = jobMap.GetString( "EasterDate" ).AsDateTime();
            var startDate = endDate.Value.AddDays( -6 );

            var checkinScheduleCategoryGuids = jobMap.GetString( "CheckinSchedules" ).SplitDelimitedValues()
                .Select( s => s.AsGuidOrNull() )
                .Where( s => s.HasValue )
                .ToList();

            var worshipScheduleCategoryGuids = jobMap.GetString( "WorshipSchedules" ).SplitDelimitedValues()
                .Select( s => s.AsGuidOrNull() )
                .Where( s => s.HasValue )
                .ToList();


            var rockContext = new RockContext();
            var scheduleService = new ScheduleService( rockContext );

            var schedules = scheduleService.Queryable().AsNoTracking()
                .Where( s => worshipScheduleCategoryGuids.Contains( s.Category.Guid ) )
                .Where( s => s.IsActive );

            var worshipSchedules = new List<ServiceSchedule>();
            foreach (var schedule in schedules)
            {
                var nextStartDate = schedule.GetNextStartDateTime( startDate );
                if (nextStartDate.HasValue && nextStartDate.Value.Date <= endDate.Value)
                {
                    schedule.LoadAttributes();
                    var campusGuids = schedule.GetAttributeValue( "Campus" ).SplitDelimitedValues();
                    foreach (var campusGuid in campusGuids)
                    {
                        var campusServiceSchedule = new ServiceSchedule
                        {
                            Id = schedule.Id,
                            ServiceTime = nextStartDate.Value,
                            Campus = CampusCache.Get( campusGuid.AsGuid() )
                        };

                        worshipSchedules.Add( campusServiceSchedule );
                    }
                }
            }

            var checkinSchedules = new List<ServiceSchedule>();
            var cSchedules = scheduleService.Queryable().AsNoTracking()
                .Where( s => checkinScheduleCategoryGuids.Contains( s.Category.Guid ) )
                .Where( s => s.IsActive );

            foreach (var schedule in cSchedules)
            {
                var nextStartDate = schedule.GetNextStartDateTime( startDate );

                if (nextStartDate.HasValue && nextStartDate.Value.Date <= endDate)
                {
                    schedule.LoadAttributes();
                    var campusGuids = schedule.GetAttributeValue( "Campus" ).SplitDelimitedValues();

                    foreach (var campusGuid in campusGuids)
                    {
                        var checkinServiceSchedule = new ServiceSchedule
                        {
                            Id = schedule.Id,
                            ServiceTime = nextStartDate.Value,
                            Campus = CampusCache.Get( campusGuid )
                        };

                        checkinSchedules.Add( checkinServiceSchedule );
                    }
                }
            }

            //Handle MultiNation - They use Blankenbaker checkin schedules
            var multiNationWorshipSchedules = worshipSchedules.Where( s => s.Campus.Guid == "bbccb39d-e1bc-4648-ad0c-2e85e7087607".AsGuid() )
                .ToList();

            foreach (var schedule in multiNationWorshipSchedules)
            {
                var bbCheckinSchedule = checkinSchedules.Where( s => s.Campus.Guid == "087ccb05-c911-40d8-8776-748821c04ae8".AsGuid() )
                    .Where( s => s.ServiceTime == schedule.ServiceTime )
                    .FirstOrDefault();

                if (bbCheckinSchedule != null)
                {
                    checkinSchedules.Add( new ServiceSchedule
                    {
                        Id = bbCheckinSchedule.Id,
                        ServiceTime = bbCheckinSchedule.ServiceTime,
                        Campus = CampusCache.Get( "bbccb39d-e1bc-4648-ad0c-2e85e7087607".AsGuid() )
                    } );
                }
            }
            // END Multination
            var groupTypeService = new GroupTypeService( rockContext );
            var parentGroupTypes = groupTypeService.Queryable().Where( t => t.Name.ToLower().Contains( "kids" ) )
                .ToList();

            var groupTypeIds = new List<int>();

            foreach (var gt in parentGroupTypes)
            {
                groupTypeIds.AddRange( groupTypeService.GetCheckinAreaDescendants( gt.Guid ).Select( g => g.Id ).ToList() );
            }

            var volunteerGroupIds = new AttributeValueService( rockContext ).Queryable().Where( av => av.AttributeId == 10946 )
                .Where( av => av.Value.Equals( "True", StringComparison.InvariantCultureIgnoreCase ) )
                .Select( av => av.EntityId ).ToList();


            var checkinScheduleIds = checkinSchedules.Select( s => s.Id ).ToList();
            var attendanceBaseQuery = new AttendanceService( rockContext ).Queryable()
                .Where( a => a.DidAttend == true )
                .Where( a => a.DeviceId.HasValue )
                .Where( a => a.Occurrence.SundayDate == endDate )
                .Where( a => groupTypeIds.Contains( a.Occurrence.Group.GroupTypeId ) )
                .Where( a => !volunteerGroupIds.Contains( a.Occurrence.GroupId ) )
                .Where( a => checkinScheduleIds.Contains( a.Occurrence.ScheduleId ?? -1 ) );


            var attendanceValues = new List<SEKidsAttendance>();
            var earlyChildhoodAttendance = attendanceBaseQuery
                .Where( a => !a.Occurrence.Group.GroupType.Name.ToLower().Contains( "elementary" ) )
                .GroupBy( a => new { a.CampusId, a.Occurrence.ScheduleId } )
                .Select( a => new SEKidsAttendance
                {
                    CampusId = a.Key.CampusId.Value,
                    CheckinScheduleId = a.Key.ScheduleId.Value,
                    SundayDate = a.Select( a1 => a1.Occurrence.SundayDate ).FirstOrDefault(),
                    AgeGroup = "Early Childhood",
                    Attendees = a.Count()

                } )
                .ToList();

            attendanceValues.AddRange( earlyChildhoodAttendance );

            var elementaryAttendance = attendanceBaseQuery
                .Where( a => a.Occurrence.Group.GroupType.Name.ToLower().Contains( "elementary" ) )
                .GroupBy( a => new { a.CampusId, a.Occurrence.ScheduleId } )
                .Select( a => new SEKidsAttendance
                {
                    CampusId = a.Key.CampusId.Value,
                    CheckinScheduleId = a.Key.ScheduleId.Value,
                    SundayDate = a.Select( a1 => a1.Occurrence.SundayDate ).FirstOrDefault(),
                    AgeGroup = "Elementary",
                    Attendees = a.Count()

                } )
                .ToList();

            attendanceValues.AddRange( elementaryAttendance );

            //Sync Schedules
            foreach (var item in attendanceValues)
            {
                var checkinSchedule = checkinSchedules.Where( c => c.Id == item.CheckinScheduleId ).FirstOrDefault();
                item.ScheduleId = worshipSchedules
                    .Where( s => s.ServiceTime == checkinSchedule.ServiceTime )
                    .Where( s => s.Campus.Id == checkinSchedule.Campus.Id )
                    .Select( s => s.Id )
                    .FirstOrDefault();
            }

            var sqlContext = new RockContext();
            //var truncateDataTableSQL = "TRUNCATE TABLE dbo._org_secc_Easter_SEKidsAttendance";
            var deleteCheckinSchedules = "DELETE FROM dbo._org_secc_Easter_SEKidsAttendance WHERE SundayDate = @SundayDate";
            var deleteParams = new List<SqlParameter>();
            deleteParams.Add( new SqlParameter( "@SundayDate", endDate.Value ) );


            sqlContext.Database.ExecuteSqlCommand( deleteCheckinSchedules, deleteParams.ToArray() );

            foreach (var item in attendanceValues.Where( s => s.ScheduleId > 0 ).ToList())
            {
                List<SqlParameter> sp = new List<SqlParameter>
                {
                    new SqlParameter( "@CampusId", item.CampusId ),
                    new SqlParameter( "@ScheduleId", item.ScheduleId ),
                    new SqlParameter( "@SundayDate", item.SundayDate ),
                    new SqlParameter( "@AgeGroup", item.AgeGroup ),
                    new SqlParameter( "@Attendees", item.Attendees )
                };

                var insertQry = @"
                    INSERT INTO dbo._org_secc_Easter_SEKidsAttendance
                    (
                        CampusId,
                        ScheduleId,
                        SundayDate,
                        AgeGroup,
                        AttendeeCount
                    )
                    values
                    (
                        @CampusId,
                        @ScheduleId,
                        @SundayDate,
                        @AgeGroup,
                        @Attendees
                    )";

                sqlContext.Database.ExecuteSqlCommand( insertQry, sp.ToArray() );


            }


        }


        public class ServiceSchedule
        {
            public int Id { get; set; }
            public DateTime ServiceTime { get; set; }
            public CampusCache Campus { get; set; }


        }

        public class SEKidsAttendance
        {
            public int CampusId { get; set; }
            public int CheckinScheduleId { get; set; }
            public DateTime SundayDate { get; set; }
            public int ScheduleId { get; set; }
            public string AgeGroup { get; set; }
            public int Attendees { get; set; }
        }
    }
}
