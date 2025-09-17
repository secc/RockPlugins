using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using org.secc.FamilyCheckin.Cache;
using Rock;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using CheckInLabel = Rock.CheckIn.CheckInLabel;

namespace org.secc.FamilyCheckin.Utilities
{
    public static class CheckinLabelGen
    {
        private static string cacheKey = "org_secc:familycheckin:breakoutgroups";

        public static List<CheckInLabel> GenerateLabels( List<Person> people, Device kioskDevice, Guid? aggregateLabelGuid )
        {
            var labels = new List<CheckInLabel>();
            var globalAttributes = Rock.Web.Cache.GlobalAttributesCache.Get();
            var globalMergeValues = Rock.Lava.LavaHelper.GetCommonMergeFields( null );

            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );

            foreach ( var person in people )
            {
                var attendances = AttendanceCache.All()
                    .Where( a => a.PersonId == person.Id && a.AttendanceState != AttendanceState.CheckedOut )
                    .ToList();

                if ( !attendances.Any() )
                {
                    continue;
                }

                var groupIds = attendances.Select( a => a.GroupId ).Distinct().ToList();

                GroupService groupService = new GroupService( new RockContext() );
                var groupTypeIds = groupService.Queryable()
                    .Where( g => groupIds.Contains( g.Id ) )
                    .Select( g => g.GroupTypeId )
                    .Distinct()
                    .ToList();

                var groupTypes = groupTypeIds
                    .Select( i => GroupTypeCache.Get( i ) )
                    .OrderBy( gt => gt.Order )
                    .ToList();

                List<Guid> labelGuids = new List<Guid>();

                var mergeObjects = new Dictionary<string, object>();
                foreach ( var keyValue in globalMergeValues )
                {
                    mergeObjects.Add( keyValue.Key, keyValue.Value );
                }

                var checkinPerson = new CheckInPerson
                {
                    Person = person,
                    SecurityCode = attendances.OrderByDescending( a => a.CreatedDateTime ).FirstOrDefault().Code
                };

                mergeObjects.Add( "Person", checkinPerson );
                mergeObjects.Add( "GroupTypes", groupTypes );
                List<Rock.Model.Group> mergeGroups = new List<Rock.Model.Group>();
                List<Location> mergeLocations = new List<Location>();
                List<Schedule> mergeSchedules = new List<Schedule>();

                var sets = attendanceService
                    .Queryable().AsNoTracking().Where( a =>
                         a.PersonAlias.Person.Id == person.Id
                         && a.StartDateTime >= Rock.RockDateTime.Today
                         && a.EndDateTime == null
                         && a.Occurrence.Group != null
                         && a.Occurrence.Schedule != null
                         && a.Occurrence.Location != null
                        )
                        .Select( a =>
                             new
                             {
                                 Group = a.Occurrence.Group,
                                 Location = a.Occurrence.Location,
                                 Schedule = a.Occurrence.Schedule,
                                 AttendanceGuid = a.Guid
                             }
                        )
                        .ToList()
                        .OrderBy( a => a.Schedule.StartTimeOfDay );

                //Load breakout group
                var breakoutGroupItems = GetBreakoutGroupItems( person );

                //Add in an empty object as a placeholder for our breakout group
                mergeObjects.Add( "BreakoutGroup", "" );

                //Add in GUID for QR code
                if ( sets.Any() )
                {
                    mergeObjects.Add( "AttendanceGuid", sets.FirstOrDefault().AttendanceGuid.ToString() );
                }

                foreach ( var set in sets )
                {
                    mergeGroups.Add( set.Group );
                    mergeLocations.Add( set.Location );
                    mergeSchedules.Add( set.Schedule );

                    //Add the breakout group mergefield
                    if ( breakoutGroupItems.Any() )
                    {
                        var breakoutGroupItem = breakoutGroupItems.Where( g => g.ScheduleId == set.Schedule.Id ).FirstOrDefault();
                        if ( breakoutGroupItem != null )
                        {
                            mergeObjects["BreakoutGroup"] = breakoutGroupItem.Letter;
                        }
                    }

                }
                mergeObjects.Add( "Groups", mergeGroups );
                mergeObjects.Add( "Locations", mergeLocations );
                mergeObjects.Add( "Schedules", mergeSchedules );

                foreach ( var groupType in groupTypes )
                {
                    var groupTypeLabels = new List<CheckInLabel>();

                    GetGroupTypeLabels( groupType, groupTypeLabels, mergeObjects, labelGuids );

                    var PrinterIPs = new Dictionary<int, string>();

                    foreach ( var label in groupTypeLabels )
                    {

                        label.PrintFrom = kioskDevice.PrintFrom;
                        label.PrintTo = kioskDevice.PrintToOverride;

                        if ( label.PrintTo == PrintTo.Default )
                        {
                            label.PrintTo = groupType.AttendancePrintTo;
                        }

                        if ( label.PrintTo == PrintTo.Kiosk )
                        {
                            var device = kioskDevice;
                            if ( device != null )
                            {
                                label.PrinterDeviceId = device.PrinterDeviceId;
                            }
                        }

                        if ( label.PrinterDeviceId.HasValue )
                        {
                            if ( PrinterIPs.ContainsKey( label.PrinterDeviceId.Value ) )
                            {
                                label.PrinterAddress = PrinterIPs[label.PrinterDeviceId.Value];
                            }
                            else
                            {
                                var printerDevice = new DeviceService( rockContext ).Get( label.PrinterDeviceId.Value );
                                if ( printerDevice != null )
                                {
                                    PrinterIPs.Add( printerDevice.Id, printerDevice.IPAddress );
                                    label.PrinterAddress = printerDevice.IPAddress;
                                }
                            }
                        }
                        labels.Add( label );
                    }
                }
            }
            if ( aggregateLabelGuid.HasValue )
            {
                labels.AddRange( GenerateAggregateLabel( aggregateLabelGuid.Value, kioskDevice, people ) );
            }

            return labels;
        }

        private static List<CheckInLabel> GenerateAggregateLabel( Guid aggregateLabelGuid, Device kioskDevice, List<Person> people )
        {
            List<CheckInLabel> labels = new List<CheckInLabel>();
            List<string> labelCodes = new List<string>();
            foreach ( var person in people )
            {
                var applicableAttendances = AttendanceCache.All().Where( a => a.PersonId == person.Id );
                //only if they are checked in to somewhere that is marked as children
                if ( applicableAttendances.Any( a => a.IsChildren ) )
                {
                    var newestAttendance = applicableAttendances.OrderByDescending( a => a.CreatedDateTime ).FirstOrDefault();
                    labelCodes.Add( $"{newestAttendance.Code}-{person.FormatAge()}" );
                }
            }
            var labelCache = KioskLabel.Get( aggregateLabelGuid );

            List<string> mergeCodes = labelCache.MergeFields.Keys.ToList().Where( s => Regex.IsMatch( s, "\\b[A-Z]{3}\\b" ) ).ToList();
            while ( labelCodes.Count > 0 && mergeCodes.Any() )
            {
                var mergeDict = new Dictionary<string, string>();

                foreach ( var mergeCode in mergeCodes )
                {
                    if ( labelCodes.Count > 0 )
                    {
                        mergeDict.Add( mergeCode, labelCodes[0] );
                        labelCodes.RemoveAt( 0 );
                    }
                    else
                    {
                        mergeDict.Add( mergeCode, "" );
                    }
                }

                mergeDict.Add( "Date", Rock.RockDateTime.Today.DayOfWeek.ToString().Substring( 0, 3 ) + " " + Rock.RockDateTime.Today.ToMonthDayString() );

                if ( labelCache != null )
                {
                    var checkInLabel = new CheckInLabel( labelCache, new Dictionary<string, object>() );
                    checkInLabel.FileGuid = aggregateLabelGuid;

                    foreach ( var keyValue in mergeDict )
                    {
                        if ( checkInLabel.MergeFields.ContainsKey( keyValue.Key ) )
                        {
                            checkInLabel.MergeFields[keyValue.Key] = keyValue.Value;
                        }
                        else
                        {
                            checkInLabel.MergeFields.Add( keyValue.Key, keyValue.Value );
                        }
                    }

                    checkInLabel.PrintFrom = kioskDevice.PrintFrom;
                    checkInLabel.PrintTo = kioskDevice.PrintToOverride;

                    if ( checkInLabel.PrintTo == PrintTo.Kiosk )
                    {
                        var device = kioskDevice;
                        if ( device != null )
                        {
                            checkInLabel.PrinterDeviceId = device.PrinterDeviceId;
                        }
                    }

                    if ( checkInLabel.PrinterDeviceId.HasValue )
                    {
                        var printerDevice = new DeviceService( new RockContext() ).Get( checkInLabel.PrinterDeviceId.Value );
                        checkInLabel.PrinterAddress = printerDevice.IPAddress;
                    }

                    labels.Add( checkInLabel );
                }
            }
            return labels;
        }

        private static List<BreakoutGroupItem> GetBreakoutGroupItems( Person person )
        {
            List<BreakoutGroupItem> allBreakoutGroupItems = RockCache.Get( cacheKey ) as List<BreakoutGroupItem>;
            if ( allBreakoutGroupItems == null || !allBreakoutGroupItems.Any() )
            {
                allBreakoutGroupItems = new List<BreakoutGroupItem>();
                RockContext rockContext = new RockContext();

                var attributeService = new AttributeService( rockContext );
                var attributeValueService = new AttributeValueService( rockContext );

                var groupEntityTypeId = EntityTypeCache.GetId( typeof( Rock.Model.Group ) );
                var breakoutGroupGroupTypeId = GroupTypeCache.GetId( Constants.GROUP_TYPE_BREAKOUT_GROUPS.AsGuid() );

                if ( groupEntityTypeId == null || breakoutGroupGroupTypeId == null )
                {
                    ExceptionLogService.LogException( new Exception( "Could not load breakout groups due to missing breakoutgroup type" ) );
                    return new List<BreakoutGroupItem>();
                }

                var letterAttribute = attributeService.GetByEntityTypeQualifier( groupEntityTypeId, "GroupTypeId", breakoutGroupGroupTypeId.Value.ToString(), false )
                    .Where( a => a.Key == Constants.GROUP_ATTRIBUTE_KEY_LETTER )
                    .FirstOrDefault();

                if ( letterAttribute == null )
                {
                    ExceptionLogService.LogException( new Exception( "Could not load breakout group letter attribute." ) );
                    return new List<BreakoutGroupItem>();
                }

                var attributeQueryable = attributeValueService.Queryable().AsNoTracking()
                    .Where( av => letterAttribute.Id == av.AttributeId );

                //Get all the data in one go 
                var breakoutData = new GroupService( rockContext )
                    .Queryable()
                    .AsNoTracking()
                    .Where( g => g.GroupTypeId == breakoutGroupGroupTypeId && g.IsActive && !g.IsArchived )
                    .Join( attributeQueryable,
                        g => g.Id,
                        av => av.EntityId,
                        ( g, av ) => new
                        {
                            ScheduleId = g.ScheduleId ?? 0,
                            PersonIds = g.Members.Where( gm => gm.IsArchived == false && gm.GroupMemberStatus == GroupMemberStatus.Active ).Select( gm => gm.PersonId ),
                            Letter = av.Value
                        } )
                    .ToList();

                foreach ( var item in breakoutData )
                {
                    foreach ( var personId in item.PersonIds )
                    {
                        allBreakoutGroupItems.Add( new BreakoutGroupItem
                        {
                            Letter = item.Letter,
                            PersonId = personId,
                            ScheduleId = item.ScheduleId
                        } );
                    }
                }

                RockCache.AddOrUpdate( cacheKey, null, allBreakoutGroupItems, RockDateTime.Now.AddMinutes( 10 ), Constants.CACHE_TAG );
            }

            return allBreakoutGroupItems.Where( i => i.PersonId == person.Id ).ToList();
        }

        private static void GetGroupTypeLabels( GroupTypeCache groupType, List<CheckInLabel> labels, Dictionary<string, object> mergeObjects, List<Guid> labelGuids )
        {
            foreach ( var attribute in groupType.Attributes.OrderBy( a => a.Value.Order ) )
            {
                if ( attribute.Value.FieldType.Guid == Rock.SystemGuid.FieldType.LABEL.AsGuid() &&
                    attribute.Value.QualifierValues.ContainsKey( "binaryFileType" ) &&
                    attribute.Value.QualifierValues["binaryFileType"].Value.Equals( Rock.SystemGuid.BinaryFiletype.CHECKIN_LABEL, StringComparison.OrdinalIgnoreCase ) )
                {
                    Guid? binaryFileGuid = groupType.GetAttributeValue( attribute.Key ).AsGuidOrNull();
                    if ( binaryFileGuid != null )
                    {
                        if ( labelGuids.Contains( binaryFileGuid ?? new Guid() ) )
                        {
                            //don't add an already exisiting label
                            continue;
                        }
                        var labelCache = KioskLabel.Get( binaryFileGuid.Value );
                        if ( labelCache != null )
                        {
                            var checkInLabel = new CheckInLabel( labelCache, mergeObjects );
                            checkInLabel.FileGuid = binaryFileGuid.Value;
                            labels.Add( checkInLabel );
                            labelGuids.Add( binaryFileGuid ?? new Guid() );
                        }
                    }
                }
            }
        }

    }
}
