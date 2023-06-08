using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using org.secc.FamilyCheckin.Utilities;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;
using Rock.Workflow.Action.CheckIn;


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
            var receiptDataText = GetAttributeValue( action, "CheckoutReceiptData", true );

            if(receiptLabelFileGuid.IsEmpty() || receiptDataText.IsNullOrWhiteSpace())
            {
                return true;
            }

            var receiptData = receiptDataText.FromJsonOrNull<Model.SFChilcareReceipt>();

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

            var groupType = checkinState.CheckIn.CurrentFamily.People
                .Where( p => p.Selected )
                .SelectMany( p => p.GroupTypes.Where( gt => gt.Selected ) )
                .FirstOrDefault();

            if(groupType != null)
            {
                groupType.Labels = labels;
            }

            return true;
        }
    }
}
