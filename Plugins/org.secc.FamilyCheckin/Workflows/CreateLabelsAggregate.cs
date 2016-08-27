using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.Linq;

using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;
using Rock.Workflow.Action.CheckIn;
using Rock;

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Creates Check-in Labels
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Creates Check-in Labels with Aggregate Family Label" )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Aggregate Checkin Label" )]
    [BinaryFileField( Rock.SystemGuid.BinaryFiletype.CHECKIN_LABEL, "Aggregated Label", "Label to aggregate", true )]
    //[DefinedValueField("E4D289A9-70FA-4381-913E-2A757AD11147","Label Merge Field","Merge field to replace text with")]
    [TextField( "Merge Text", "Text to merge label merge field into separated by commas.", true, "AAA,BBB,CCC,DDD" )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP, "Volunteer Group Attribute" )]
    public class CreateLabelsAggregate : CheckInActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The workflow action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            var checkInState = GetCheckInState( entity, out errorMessages );

            CheckInGroupType lastCheckinGroupType = null;

            List<string> labelCodes = new List<string>();
            List<int> childGroupIds;

            var volAttributeGuid = GetAttributeValue( action, "VolunteerGroupAttribute" );
            string volAttributeKey = "";
            if ( !string.IsNullOrWhiteSpace( volAttributeGuid ) )
            {
                volAttributeKey = AttributeCache.Read( volAttributeGuid.AsGuid() ).Key;
                childGroupIds = checkInState.Kiosk.KioskGroupTypes
                    .SelectMany( g => g.KioskGroups )
                    .Where( g => !g.Group.GetAttributeValue( volAttributeKey ).AsBoolean() )
                    .Select( g => g.Group.Id ).ToList();
            }
            else
            {
                childGroupIds = new List<int>();
            }

            if ( checkInState != null )
            {
                var globalAttributes = Rock.Web.Cache.GlobalAttributesCache.Read( rockContext );
                var globalMergeValues = Rock.Lava.LavaHelper.GetCommonMergeFields( null );

                var groupMemberService = new GroupMemberService( rockContext );

                foreach ( var family in checkInState.CheckIn.Families.Where( f => f.Selected ) )
                {
                    foreach ( var person in family.People.Where( p => p.Selected ) )
                    {
                        if ( person.GroupTypes.Where(gt => gt.Selected).SelectMany( gt => gt.Groups ).Where( g => g.Selected && childGroupIds.Contains( g.Group.Id ) ).Any() )
                        {
                            labelCodes.Add( person.SecurityCode + "-" + LabelAge( person.Person ) );
                        }

                        var firstCheckinGroupType = person.GroupTypes.Where( g => g.Selected ).FirstOrDefault();
                        if ( firstCheckinGroupType != null )
                        {
                            List<Guid> labelGuids = new List<Guid>();

                            var mergeObjects = new Dictionary<string, object>();
                            foreach ( var keyValue in globalMergeValues )
                            {
                                mergeObjects.Add( keyValue.Key, keyValue.Value );
                            }
                            mergeObjects.Add( "Person", person );
                            mergeObjects.Add( "GroupTypes", person.GroupTypes.Where( g => g.Selected ).ToList() );
                            List<Group> mergeGroups = new List<Group>();
                            List<Location> mergeLocations = new List<Location>();
                            List<Schedule> mergeSchedules = new List<Schedule>();

                            var sets = new AttendanceService( rockContext )
                                .Queryable().AsNoTracking().Where( a =>
                                     a.PersonAlias.Person.Id == person.Person.Id
                                     && a.StartDateTime >= Rock.RockDateTime.Today
                                     && a.EndDateTime == null
                                    )
                                    .Select( a =>
                                         new
                                         {
                                             Group = a.Group,
                                             Location = a.Location,
                                             Schedule = a.Schedule
                                         }
                                    )
                                    .ToList()
                                    .OrderBy( a => a.Schedule.StartTimeOfDay );

                            foreach ( var set in sets )
                            {
                                mergeGroups.Add( set.Group );
                                mergeLocations.Add( set.Location );
                                mergeSchedules.Add( set.Schedule );
                            }
                            mergeObjects.Add( "Groups", mergeGroups );
                            mergeObjects.Add( "Locations", mergeLocations );
                            mergeObjects.Add( "Schedules", mergeSchedules );

                            foreach ( var groupType in person.GroupTypes.Where( g => g.Selected ) )
                            {
                                lastCheckinGroupType = groupType;

                                groupType.Labels = new List<CheckInLabel>();

                                GetGroupTypeLabels( groupType.GroupType, firstCheckinGroupType.Labels, mergeObjects, labelGuids );

                                var PrinterIPs = new Dictionary<int, string>();

                                foreach ( var label in groupType.Labels )
                                {

                                    label.PrintFrom = checkInState.Kiosk.Device.PrintFrom;
                                    label.PrintTo = checkInState.Kiosk.Device.PrintToOverride;

                                    if ( label.PrintTo == PrintTo.Default )
                                    {
                                        label.PrintTo = groupType.GroupType.AttendancePrintTo;
                                    }

                                    if ( label.PrintTo == PrintTo.Kiosk )
                                    {
                                        var device = checkInState.Kiosk.Device;
                                        if ( device != null )
                                        {
                                            label.PrinterDeviceId = device.PrinterDeviceId;
                                        }
                                    }
                                    else if ( label.PrintTo == PrintTo.Location )
                                    {
                                        // Should only be one
                                        var group = groupType.Groups.Where( g => g.Selected ).FirstOrDefault();
                                        if ( group != null )
                                        {
                                            var location = group.Locations.Where( l => l.Selected ).FirstOrDefault();
                                            if ( location != null )
                                            {
                                                var device = location.Location.PrinterDevice;
                                                if ( device != null )
                                                {
                                                    label.PrinterDeviceId = device.PrinterDeviceId;
                                                }
                                            }
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
                                }
                            }
                        }


                    }

                    //Add in custom labels for parents
                    //This is the aggregate part
                    List<CheckInLabel> customLabels = new List<CheckInLabel>();

                    List<string> mergeCodes = ( ( string ) GetAttributeValue( action, "MergeText" ) ).Split( ',' ).ToList();
                    while ( labelCodes.Count > 0 )
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

                        var labelCache = KioskLabel.Read( new Guid( GetAttributeValue( action, "AggregatedLabel" ) ) );
                        if ( labelCache != null )
                        {
                            var checkInLabel = new CheckInLabel( labelCache, new Dictionary<string, object>() );
                            checkInLabel.FileGuid = new Guid( GetAttributeValue( action, "AggregatedLabel" ) );

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

                            checkInLabel.PrintFrom = checkInState.Kiosk.Device.PrintFrom;
                            checkInLabel.PrintTo = checkInState.Kiosk.Device.PrintToOverride;

                            if ( checkInLabel.PrintTo == PrintTo.Default )
                            {
                                checkInLabel.PrintTo = lastCheckinGroupType.GroupType.AttendancePrintTo;
                            }

                            if ( checkInLabel.PrintTo == PrintTo.Kiosk )
                            {
                                var device = checkInState.Kiosk.Device;
                                if ( device != null )
                                {
                                    checkInLabel.PrinterDeviceId = device.PrinterDeviceId;
                                }
                            }
                            else if ( checkInLabel.PrintTo == PrintTo.Location )
                            {
                                // Should only be one
                                var group = lastCheckinGroupType.Groups.Where( g => g.Selected ).FirstOrDefault();
                                if ( group != null )
                                {
                                    var location = group.Locations.Where( l => l.Selected ).FirstOrDefault();
                                    if ( location != null )
                                    {
                                        var device = location.Location.PrinterDevice;
                                        if ( device != null )
                                        {
                                            checkInLabel.PrinterDeviceId = device.PrinterDeviceId;
                                        }
                                    }
                                }
                            }
                            if ( checkInLabel.PrinterDeviceId.HasValue )
                            {
                                var printerDevice = new DeviceService( rockContext ).Get( checkInLabel.PrinterDeviceId.Value );
                                checkInLabel.PrinterAddress = printerDevice.IPAddress;
                            }
                            if ( lastCheckinGroupType != null )
                            {
                                lastCheckinGroupType.Labels.Add( checkInLabel );
                            }
                        }

                    }
                }

                return true;

            }

            return false;
        }

        private string LabelAge( Person person )
        {
            var age = person.Age;
            if ( age != 0 )
            {
                return age.ToString() + "yr";
            }
            var nulableBirthday = person.BirthDate;
            if ( nulableBirthday.HasValue )
            {
                DateTime birthday = nulableBirthday ?? DateTime.Now;
                var today = DateTime.Today;
                return Math.Floor( ( today.Subtract( birthday ).Days / ( 365.25 / 12 ) ) ).ToString() + "mo";
            }
            return "N/A";
        }

        private void GetGroupTypeLabels( GroupTypeCache groupType, List<CheckInLabel> labels, Dictionary<string, object> mergeObjects, List<Guid> labelGuids )
        {
            //groupType.LoadAttributes();
            foreach ( var attribute in groupType.Attributes.OrderBy( a => a.Value.Order ) )
            {
                if ( attribute.Value.FieldType.Guid == Rock.SystemGuid.FieldType.BINARY_FILE.AsGuid() &&
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
                        var labelCache = KioskLabel.Read( binaryFileGuid.Value );
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