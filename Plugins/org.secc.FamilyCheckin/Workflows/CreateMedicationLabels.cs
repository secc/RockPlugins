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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;
using Rock.Workflow.Action.CheckIn;

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Creates Check-in Labels
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Creates Labels for a student's medication needs." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Create Medication Labels" )]
    [TextField( "Group Attribute Key", "Attribute key for the check-in group which refers to the registration group." )]
    [TextField( "Matrix Attribute Key", "Attribute key for the medication matrix." )]
    [BinaryFileField( Rock.SystemGuid.BinaryFiletype.CHECKIN_LABEL, "Medication Label", "Label to print", false )]
    [TextField( "Medication Text", "Merge fields for medications", false, "Medication 1,Medication 2,Medication 3" )]
    [TextField( "Instructions Text", "Merge fields for instructions", false, "Instructions 1,Instructions 2,Instructions 3" )]
    [TextField( "Matrix Attribute Medication Key" )]
    [TextField( "Matrix Attribute Instructions Key" )]
    [TextField( "Matrix Attribute Schedule Key" )]

    public class CreateMedicationLabels : CheckInActionComponent
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
            var matrixAttributeMedicationKey = GetAttributeValue( action, "MatrixAttributeMedicationKey" );
            var matrixAttributeInstructionsKey = GetAttributeValue( action, "MatrixAttributeInstructionsKey" );
            var matrixAttributeScheduleKey = GetAttributeValue( action, "MatrixAttributeScheduleKey" );

            AttributeMatrixService attributeMatrixService = new AttributeMatrixService( rockContext );
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );

            if ( checkInState != null )
            {
                var family = checkInState.CheckIn.CurrentFamily;
                if ( family != null )
                {
                    var commonMergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null );

                    var people = family.GetPeople( true );
                    foreach ( var person in people.Where( p => p.Selected ) )
                    {
                        foreach ( var groupType in person.GroupTypes )
                        {
                            groupType.Labels = new List<CheckInLabel>();


                            foreach ( var group in groupType.Groups )
                            {
                                List<string> medicationText = ( ( string ) GetAttributeValue( action, "MedicationText" ) ).Split( ',' ).ToList();
                                List<string> instructionsText = ( ( string ) GetAttributeValue( action, "InstructionsText" ) ).Split( ',' ).ToList();

                                List<MedInfo> medInfos = new List<MedInfo>();
                                if ( medicationText.Count == instructionsText.Count )
                                {
                                    for ( int i = 0; i < medicationText.Count; i++ )
                                    {
                                        medInfos.Add( new MedInfo { Medication = medicationText[i], Instructions = instructionsText[i] } );
                                    }
                                }

                                group.Group.LoadAttributes();
                                var groupGuid = group.Group.GetAttributeValue( GetAttributeValue( action, "GroupAttributeKey" ) );
                                var registrationGroup = new GroupService( rockContext ).Get( groupGuid.AsGuid() );
                                if ( registrationGroup == null )
                                {
                                    continue;
                                }

                                var groupMember = groupMemberService.GetByGroupIdAndPersonId( registrationGroup.Id, person.Person.Id ).FirstOrDefault();

                                var medicationKey = GetAttributeValue( action, "MatrixAttributeKey" );
                                var medicationMatrix = person.Person.GetAttributeValue( medicationKey );


                                var attributeMatrix = attributeMatrixService.Get( medicationMatrix.AsGuid() );

                                var labelCache = KioskLabel.Get( new Guid( GetAttributeValue( action, "MedicationLabel" ) ) );

                                //Set up merge fields so we can use the lava from the merge fields
                                var mergeObjects = new Dictionary<string, object>();
                                foreach ( var keyValue in commonMergeFields )
                                {
                                    mergeObjects.Add( keyValue.Key, keyValue.Value );
                                }

                                mergeObjects.Add( "RegistrationGroup", registrationGroup );
                                mergeObjects.Add( "RegistrationGroupMember", groupMember );
                                mergeObjects.Add( "Group", group );
                                mergeObjects.Add( "Person", person );
                                mergeObjects.Add( "People", people );
                                mergeObjects.Add( "GroupType", groupType );

                                if ( attributeMatrix == null || attributeMatrix.AttributeMatrixItems.Count == 0 )
                                {
                                    // Add a No Medication Information label for anyone without data
                                    var checkInLabel = new CheckInLabel( labelCache, mergeObjects );

                                    var index = 0;
                                    foreach (string mergeFieldText in medicationText)
                                    {
                                        checkInLabel.MergeFields.Add( mergeFieldText, index == 0 ? "No Medication Information Found" : "" );
                                        checkInLabel.MergeFields.Add( instructionsText[index], index == 0 ? "Scan QR to Register Medication" : "" );                                                                              
                                        index++;
                                    }
                                    checkInLabel.MergeFields.Add( "NoMedsQR", @"^FO244,244^GFA,1517,1936,16,:Z64:eJxdlc9rG0cUx99obdYoxmuDhEtRdopPxTkkvengaGWQyVUF6xbqf8G3+iC6Uy1UxoHoPwhBuYg19A/IwazQoT22kNySsEE9FFPoChpYzGan3zer9Y+MQOyXz755897MfHcymUxoQoomL9fwqHytMysTkdCR1FoTdGplVmzp+Ja+4fpfnd77J/jd0n9LPNMxUV9Oo4bIGi6RQydETS+IWlbuGn3M2vBCp6Sa3iy6MFw56rjX7VtvRL9Grrt6iHg/bSJ/3oJ2csT7CTRmh5baphT8XhbkF+rFkqecX+jpB6NPvJTzW3qel5zzg+eIR36dcn5LfzRccbw/jUh8ilyZF/m9ICbUfzueUH8R75v4G476Sbyp72e16/rJyrz5rfpJZHJ6Xf8drvqdTp9WnjY6T13Z6RyZfjf39lqtlnvd/+ZeA6PQ+gt+cHDQ7oOvrKysHhx0iEfTy7zYiqkc0DIS0W3txaLknXbfi2S0+ldnpd0me8gNivHTuRPHZNuswaXOZZKQMzL6hg/fdmtedL9dP8rlIiFbK/RG50S8PHBNrLNCI/5L/plI9Hzwuv6k2+Sn2EDycmoJHeMtP0HDSWbUgMZcmjeAuUsF/+PoXZ/LWBfvI8ziB0GAF8xw4g3ybTtIv+v1DqFl4pAeMT89PVPMHeUPh7Nsq3ifuT+wLr/ZgTh7HVZ2x+STnZLgybBmJyGtrJQq1cFkxDomX62+I+H8ljnvPzD3A2UFr79+NXlO7gZzm6xg8ZVN6+RKjh+Bpy9+USUfKmu2kE+669RwOX4Ynl/uOPN0EI7H413EY8MF/YgDOI0TG/HQlbUW/azjOMZ6h2iu2Fxyhx6GD87z0Junz/LdB1ch2Ykd6IX95Njh84/+p1ag04E6HeH8c/8Te6YXXF/BK2H46vxqJ/z27KLqIB7lewFzNJz7gwm84Gpt7VS53D/mfskd8N3h1XnI/T9zqxu7VV5/kD7i/cH9xetYP/p/GYbPXXdjg3j9s+xR/fAQ3GU90ONwgverbrX6awX991MrKuYn3H+tWHuzmDXun6/8zIrgAKUePLysqN3xS6pW6AL5tR3gTjZwfn4izq+x/0Qt/HLifBr7X/ClLvlSE22nVN/vteu61+0iP3kpeTNjjzHvP+HwyqmxR9wPv+Dzkpv7JvrbUf0Hkv8t7x/uf4wj7OSF7sMfIpmTzEueXvNer9cVbbE/9fOadLrm/luRNZvrvDg/7N+RmE6NTgq/Vta85Ow/m7Xa9tsp5v8+Mf4D//Dgf8T3l/0L/iE1c6z/pOTzJd/abD4Wjz39p96SWFvhf9qMnAr/9ac3uvDfOxz+eV/IqRAJx7N9ZHvkoTUxzp+Jh7tLWGyM82f89w5PmvX+puEL8OL6z2BAMmb/43iCQaOBvAqTHxwGJj8aLth/0SGScBxp6od9YWb+vowU1+9j7yL+vuTL75cqufHvkzrJz23mR6um/zAwfJ9giOa8wNuE5gpKfYfz/sHgDmtU7x2uECkM/qfl3//wIwrw:B84A" );

                                    addLabel( checkInLabel, checkInState, groupType, group, rockContext );
                                }
                                else
                                {
                                    var items = attributeMatrix.AttributeMatrixItems.ToList();
                                    var index = 0;

                                    while ( index < items.Count )
                                    {
                                        var checkInLabel = new CheckInLabel( labelCache, mergeObjects );

                                        foreach ( var med in medInfos )
                                        {

                                            if ( items.Count > index )
                                            {
                                                items[index].LoadAttributes();

                                                string scheduleText = "";
                                                string separator = "";
                                                var schedule = items[index].GetAttributeValue( matrixAttributeScheduleKey ).SplitDelimitedValues();
                                                foreach ( var scheduleGuid in schedule )
                                                {
                                                    scheduleText += separator + DefinedValueCache.Get( scheduleGuid );
                                                    separator = ", ";
                                                }

                                                checkInLabel.MergeFields.Add( med.Medication,
                                                    items[index].GetAttributeValue( matrixAttributeMedicationKey )
                                                    + " - "
                                                    + scheduleText
                                                );

                                                checkInLabel.MergeFields.Add( med.Instructions, items[index].GetAttributeValue( matrixAttributeInstructionsKey ) );
                                            }
                                            else
                                            {
                                                checkInLabel.MergeFields.Add( med.Medication, "" );
                                                checkInLabel.MergeFields.Add( med.Instructions, "" );
                                            }

                                            index++;
                                        }
                                        checkInLabel.MergeFields.Add( "NoMedsQR", "" );

                                        addLabel( checkInLabel, checkInState, groupType, group, rockContext );

                                        //Save that we just checked in the student's medications
                                        person.Person.SetAttributeValue( Utilities.Constants.PERSON_ATTRIBUTE_KEY_LASTMEDICATIONCHECKIN, Rock.RockDateTime.Today );
                                        person.Person.SaveAttributeValue( Utilities.Constants.PERSON_ATTRIBUTE_KEY_LASTMEDICATIONCHECKIN );
                                    }
                                }
                            }
                        }
                    }
                }
                return true;
            }

            errorMessages.Add( $"Attempted to run {this.GetType().GetFriendlyTypeName()} in check-in, but the check-in state was null." );
            return false;
        }

        private void addLabel( CheckInLabel checkInLabel, CheckInState checkInState, CheckInGroupType groupType, CheckInGroup group, RockContext rockContext )
        {

            var PrinterIPs = new Dictionary<int, string>();

            if ( checkInLabel.PrintTo == PrintTo.Default )
            {
                checkInLabel.PrintTo = groupType.GroupType.AttendancePrintTo;
            }
            else if ( checkInLabel.PrintTo == PrintTo.Location && group.Locations.Any() )
            {
                var deviceId = group.Locations.FirstOrDefault().Location.PrinterDeviceId;
                if ( deviceId != null )
                {
                    checkInLabel.PrinterDeviceId = deviceId;
                }
            }
            else
            {
                var device = checkInState.Kiosk.Device;
                if ( device != null )
                {
                    checkInLabel.PrinterDeviceId = device.PrinterDeviceId;
                }
            }


            if ( checkInLabel.PrinterDeviceId.HasValue )
            {
                if ( PrinterIPs.ContainsKey( checkInLabel.PrinterDeviceId.Value ) )
                {
                    checkInLabel.PrinterAddress = PrinterIPs[checkInLabel.PrinterDeviceId.Value];
                }
                else
                {
                    var printerDevice = new DeviceService( rockContext ).Get( checkInLabel.PrinterDeviceId.Value );
                    if ( printerDevice != null )
                    {
                        PrinterIPs.Add( printerDevice.Id, printerDevice.IPAddress );
                        checkInLabel.PrinterAddress = printerDevice.IPAddress;
                    }
                }
            }

            groupType.Labels.Insert( 0, checkInLabel );
        }

        private class MedInfo
        {
            public string Medication { get; set; }
            public string Instructions { get; set; }
        }
    }
}