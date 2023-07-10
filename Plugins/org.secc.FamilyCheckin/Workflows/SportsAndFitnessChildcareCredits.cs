using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;
using Rock.Workflow.Action.CheckIn;

namespace org.secc.FamilyCheckin.Workflows
{
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Calculate the number of Sports and Fitness Childcare credits used." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Deduct Sports Fitness Chidcare Credits" )]
    [IntegerField( "Grace Period", "Grace period in minutes before charging for an additional hour.", false, 15, "", 0, "GracePeriod" )]
    [WorkflowAttribute("Checkout Receipt Data", 
        Description = "Checkout Receipt data in JSON Format", 
        IsRequired = false, 
        Order = 1,
        Key = "CheckoutReceiptData", 
        FieldTypeClassNames = new string[] { "Rock.Field.Types.TextFieldType"} )]
    public class SportsAndFitnessChildcareCredits : CheckInActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            var checkInState = GetCheckInState( entity, out errorMessages );

            if ( checkInState == null )
                return false;

            var gracePeriodMinutes = GetAttributeValue( action, "GracePeriod" ).AsInteger();
            var family = checkInState.CheckIn.CurrentFamily;
            var sfReceipt = new Model.SFChilcareReceipt();


            if ( family != null )
            {
                var attendanceService = new AttendanceService( rockContext );

                var familyContext = new RockContext();
                var groupService = new GroupService( familyContext );

                var fullFamily = groupService.Get( family.Group.Id );
                fullFamily.LoadAttributes( familyContext );
                
                sfReceipt.FamilyId = fullFamily.Id;
                sfReceipt.CreditsBeginning = fullFamily.GetAttributeValue( "SportsandFitnessChildcareCredit" ).AsInteger();
                

                //calculate the number of credits used for the family
                foreach ( var person in family.CheckOutPeople.Where( p => p.Selected ) )
                {
                    var creditsUsed = 0;
                    var attendanceRec = attendanceService
                        .Queryable()
                        .Where( a =>
                            person.AttendanceIds.Contains( a.Id ) &&
                            a.PersonAlias != null &&
                            a.PersonAlias.Person != null &&
                            a.Occurrence != null &&
                            a.Occurrence.Group != null &&
                            a.Occurrence.Location != null )
                        .FirstOrDefault();

                    //foreach ( var attendanceRec in attendanceRecs )
                    //{
                    //    var checkoutTime = attendanceRec.EndDateTime ?? RockDateTime.Now;
                    //    var timeInChildcare = checkoutTime.Subtract( attendanceRec.StartDateTime );

                    //    creditsUsed += timeInChildcare.Hours;
                    //    if ( timeInChildcare.Minutes > gracePeriodMinutes )
                    //    {
                    //        creditsUsed += 1;
                    //    }
                    //}
                    if(attendanceRec != null)
                    {
                        var checkoutTime = attendanceRec.EndDateTime ?? RockDateTime.Now;
                        var timeInChildcare = checkoutTime.Subtract( attendanceRec.StartDateTime );

                        creditsUsed += timeInChildcare.Hours;
                        if(timeInChildcare.Minutes > gracePeriodMinutes)
                        {
                            creditsUsed++;
                        }
                        sfReceipt.AddParticipant( person.Person, attendanceRec.StartDateTime, checkoutTime, creditsUsed );
                    }

                }

                //update the credits
                fullFamily.SetAttributeValue( "SportsandFitnessChildcareCredit", sfReceipt.CreditsEnding );
                fullFamily.SaveAttributeValue( "SportsandFitnessChildcareCredit", familyContext );

                var value = GetAttributeValue( action, "CheckoutReceiptData" );
                if(!value.IsNullOrWhiteSpace())
                {
                    SetWorkflowAttributeValue( action, "CheckoutReceiptData", sfReceipt.ToJson() );
                }

                
                //familyContext.SaveChanges();
            }


            return true;
        }
    }
}
