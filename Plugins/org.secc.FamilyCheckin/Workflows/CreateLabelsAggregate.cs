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
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using org.secc.FamilyCheckin.Cache;
using org.secc.FamilyCheckin.Model;
using org.secc.FamilyCheckin.Utilities;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;
using Rock.Workflow.Action.CheckIn;

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Creates Check-in Labels
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Creates Check-in Labels with Aggregate Family Label" )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Aggregate Checkin Label" )]
    [BinaryFileField( Rock.SystemGuid.BinaryFiletype.CHECKIN_LABEL, "Aggregated Label", "Binary file that is the parent pickup label", false )]
    [BooleanField( "Is Mobile", "If this is a mobile check-in and needs to have its attendances set as RSVP and stored in an entity set, set true.", false )]
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

            if ( checkInState != null )
            {
                var people = checkInState.CheckIn.CurrentFamily.People
                    .Where( p => p.Selected )
                    .Select( p => p.Person )
                    .ToList();

                var checkInLabels = CheckinLabelGen.GenerateLabels( people, checkInState.Kiosk.Device, GetAttributeValue( action, "AggregatedLabel" ).AsGuidOrNull() );

                //For logging purposes, storing checkInLabels as a workflow attribute
                StringBuilder combinedLabelsBuilder = new StringBuilder();
                foreach ( var label in checkInLabels )
                {
                    combinedLabelsBuilder.Append( ", " );
                    combinedLabelsBuilder.AppendLine( "Label Type:" + label.LabelType );
                    combinedLabelsBuilder.AppendLine( "Order:" + label.Order );
                    combinedLabelsBuilder.AppendLine( "PersonId:" + label.PersonId );
                    combinedLabelsBuilder.AppendLine( "PrinterDeviceId:" + label.PrinterDeviceId );
                    combinedLabelsBuilder.AppendLine( "PrinterAddress:" + label.PrinterAddress );
                    combinedLabelsBuilder.AppendLine( "PrintFrom:" + label.PrintFrom );
                    combinedLabelsBuilder.AppendLine( "PrintTo:" + label.PrintTo );
                    combinedLabelsBuilder.AppendLine( "FileGuid:" + label.FileGuid );
                    combinedLabelsBuilder.AppendLine( "LabelFile:" + label.LabelFile );
                    combinedLabelsBuilder.AppendLine( "LabelKey:" + label.LabelKey );
                    combinedLabelsBuilder.AppendLine( "MergeFields:" );
                    foreach ( var mergeField in label.MergeFields )
                    {
                        combinedLabelsBuilder.AppendLine( $"    {mergeField.Key}: {mergeField.Value}" );
                    }
                }
                string combinedLabels = combinedLabelsBuilder.ToString().TrimStart( ',', ' ' );
                action.Activity.Workflow.SetAttributeValue( "checkInLabels", combinedLabels );

                var groupType = checkInState.CheckIn.CurrentFamily.People
                    .Where( p => p.Selected )
                    .SelectMany( p => p.GroupTypes.Where( gt => gt.Selected ) )
                    .FirstOrDefault();

                if ( groupType != null )
                {
                    groupType.Labels = checkInLabels;
                }


                //For mobile check-in we need to serialize this data and save it in the database.
                //This will mean that when it's time to finish checkin in
                //All we will need to do is deserialize and pass the data to the printer
                if ( GetAttributeValue( action, "IsMobile" ).AsBoolean() )
                {


                    MobileCheckinRecordService mobileCheckinRecordService = new MobileCheckinRecordService( rockContext );
                    MobileCheckinRecord mobileCheckinRecord = mobileCheckinRecordService.Queryable()
                        .Where( r => r.Status == MobileCheckinStatus.Active )
                        .Where( r => r.CreatedDateTime > Rock.RockDateTime.Today )
                        .Where( r => r.UserName == checkInState.CheckIn.SearchValue )
                        .FirstOrDefault();

                    if ( mobileCheckinRecord == null )
                    {
                        ExceptionLogService.LogException( "Mobile Check-in failed to find mobile checkin record" );
                    }
                    mobileCheckinRecord.SerializedCheckInState = JsonConvert.SerializeObject( checkInLabels );

                    rockContext.SaveChanges();

                    //Freshen cache (we're going to need it soon)
                    MobileCheckinRecordCache.Update( mobileCheckinRecord.Id );
                }
                return true;
            }
            errorMessages.Add( $"Attempted to run {this.GetType().GetFriendlyTypeName()} in check-in, but the check-in state was null." );
            return false;
        }
    }
}