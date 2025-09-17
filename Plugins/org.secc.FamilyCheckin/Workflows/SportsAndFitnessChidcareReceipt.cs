using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Linq;
using org.secc.FamilyCheckin.Utilities;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;
using Rock.Workflow.Action.CheckIn;
using CheckInLabel = Rock.CheckIn.CheckInLabel;


namespace org.secc.FamilyCheckin.Workflows
{

    [ActionCategory( "SECC > Check-In" )]
    [Description( "Generate checkout receipt for S&F Childcare" )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "S&F Childcare Receipt" )]
    [BinaryFileField(Rock.SystemGuid.BinaryFiletype.CHECKIN_LABEL, 
        Name = "Receipt Label",
        Description = "Check-out label that contains the Childcare receipt data.",
        IsRequired = false,
        Order = 0,
        Key = "ReceiptLabel")]
    [WorkflowAttribute("Checkout Receipt Data",
        Description = "Checkout Receipt Data in JSON Format",
        IsRequired = false,
        Order = 1,
        Key = "CheckoutReceiptData",
        FieldTypeClassNames = new string[] { "Rock.Field.Types.TextFieldType" })]
    public class SportsAndFitnessChidcareReceipt : CheckInActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            var checkinState = GetCheckInState( entity, out errorMessages );
            if ( checkinState == null )
            {
                return false;
            }

            var family = checkinState.CheckIn.CurrentFamily;
            if(family == null)
            {
                return true;
            }

            var receiptLabelFileGuid = GetAttributeValue( action, "ReceiptLabel" ).AsGuid();
            var receiptLabelText = GetAttributeValue( action, "CheckoutReceiptData", true );



            if(receiptLabelFileGuid.IsEmpty() || receiptLabelText.IsNullOrWhiteSpace() )
            {
                return true;
            }

            var receiptData = receiptLabelText.FromJsonOrNull<Model.SFChilcareReceipt>();

            var mergeFields = new Dictionary<string, object>();
            foreach ( var item in Rock.Lava.LavaHelper.GetCommonMergeFields(null) )
            {
                mergeFields.Add( item.Key, item.Value );
            }
            mergeFields.Add( "FamilyId", receiptData.FamilyId );
            mergeFields.Add( "CreditsBeginning", receiptData.CreditsBeginning );
            mergeFields.Add( "CreditsUsed", receiptData.CreditsUsed );
            mergeFields.Add( "CreditsEnding", receiptData.CreditsEnding );
            
            mergeFields.Add( "Participants", receiptData.Participants );

            var label = new CheckInLabel( KioskLabel.Get( receiptLabelFileGuid ), mergeFields );
            label.FileGuid = receiptLabelFileGuid;
            label.PrintFrom = checkinState.Kiosk.Device.PrintFrom;
            label.PrintTo = checkinState.Kiosk.Device.PrintToOverride;

            if(label.PrintTo == PrintTo.Kiosk)
            {
                label.PrinterDeviceId = checkinState.Kiosk.Device.PrinterDeviceId;
            }

            if(label.PrinterDeviceId.HasValue)
            {
                var printerDevice = new DeviceService( new RockContext() ).Get( label.PrinterDeviceId.Value );
                label.PrinterAddress = printerDevice.IPAddress;
            }

            var labels = new List<CheckInLabel> { label };

            var person = checkinState.CheckIn.CurrentFamily.CheckOutPeople
                .Where( p => p.Selected )
                .FirstOrDefault();

            if(person != null)
            { 
                person.Labels.AddRange( labels );
            }

            return true;
        }
    }
}
