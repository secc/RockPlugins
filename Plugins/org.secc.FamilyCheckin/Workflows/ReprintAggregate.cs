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
    [ExportMetadata( "ComponentName", "Reprint Aggregate" )]
    [BinaryFileField( Rock.SystemGuid.BinaryFiletype.CHECKIN_LABEL, "Aggregated Label", "Label to aggregate", true )]
    //[DefinedValueField("E4D289A9-70FA-4381-913E-2A757AD11147","Label Merge Field","Merge field to replace text with")]
    [TextField( "Merge Text", "Text to merge label merge field into separated by commas.", true, "AAA,BBB,CCC,DDD" )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP, "Volunteer Group Attribute" )]
    public class ReprintAggregate : CheckInActionComponent
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
            if ( checkInState != null )
            {
                List<string> labelCodes = new List<string>();

                var volAttributeGuid = GetAttributeValue( action, "VolunteerGroupAttribute" ).AsGuid();
                var volAttribute = AttributeCache.Read( volAttributeGuid );

                List<int> ChildGroupIds;

                if ( volAttribute != null )
                {
                    AttributeValueService attributeValueService = new AttributeValueService( rockContext );
                    ChildGroupIds = attributeValueService.Queryable().Where( av => av.AttributeId == volAttribute.Id && av.Value == "False" ).Select( av => av.EntityId.Value ).ToList();
                }
                else
                {
                    ChildGroupIds = new List<int>();
                }


                var globalAttributes = Rock.Web.Cache.GlobalAttributesCache.Read( rockContext );
                var globalMergeValues = Rock.Lava.LavaHelper.GetCommonMergeFields( null );

                var groupMemberService = new GroupMemberService( rockContext );

                AttendanceService attendanceService = new AttendanceService( rockContext );

                foreach ( var family in checkInState.CheckIn.Families.Where( f => f.Selected ) )
                {
                    foreach ( var person in family.People )
                    {
                        var attendances = attendanceService.Queryable( "AttendanceCode" )
                            .Where( a => a.CreatedDateTime >= Rock.RockDateTime.Today
                                         && person.Person.Id == a.PersonAlias.PersonId
                                         && ChildGroupIds.Contains( a.GroupId ?? 0 )
                                         && a.AttendanceCode != null )
                            .DistinctBy( a => a.AttendanceCodeId ).ToList();
                        foreach ( var attendance in attendances )
                        {
                            if ( attendance != null && attendance.AttendanceCode != null )
                            {
                                labelCodes.Add( attendance.AttendanceCode.Code + "-" + LabelAge( person.Person ) );
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

                            checkInLabel.MergeFields.Add( "Date", Rock.RockDateTime.Today.DayOfWeek.ToString().Substring( 0, 3 ) + " " + Rock.RockDateTime.Today.ToMonthDayString() );

                            checkInLabel.PrintFrom = checkInState.Kiosk.Device.PrintFrom;
                            checkInLabel.PrintTo = checkInState.Kiosk.Device.PrintToOverride;

                            if ( checkInLabel.PrintTo == PrintTo.Kiosk )
                            {
                                var device = checkInState.Kiosk.Device;
                                if ( device != null )
                                {
                                    checkInLabel.PrinterDeviceId = device.PrinterDeviceId;
                                }
                            }
                            
                            if ( checkInLabel.PrinterDeviceId.HasValue )
                            {
                                var printerDevice = new DeviceService( rockContext ).Get( checkInLabel.PrinterDeviceId.Value );
                                checkInLabel.PrinterAddress = printerDevice.IPAddress;
                            }
                            var firstPerson = family.People.Where( p => p.GroupTypes.Any() ).FirstOrDefault();
                            if ( firstPerson != null )
                            {
                                //we have to set as selected or it wil not print
                                firstPerson.Selected = true;
                                var firstGroupType = firstPerson.GroupTypes.FirstOrDefault();
                                firstGroupType.Selected = true;
                                if ( firstGroupType.Labels == null )
                                {
                                    firstGroupType.Labels = new List<CheckInLabel>() { checkInLabel };
                                }
                                else
                                {
                                    firstGroupType.Labels.Add( checkInLabel );
                                }
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
    }
}