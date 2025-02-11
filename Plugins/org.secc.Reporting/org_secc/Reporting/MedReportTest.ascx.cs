using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;

using Rock.Web;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using org.secc.Reporting;


namespace RockWeb.Plugins.org_secc.Reporting
{
    [DisplayName("Med Test")]
    [Category("SECC > Report")]
    [Description("Test Application")]
    public partial class MedReportTest : RockBlock
    {
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            lbRunTest.Click += lbRunTest_Click;

            gMedications.GridRebind += gMedications_GridRebind;
            gMedications.Actions.ShowAdd = false;
            gMedications.Actions.ShowBulkUpdate = false;
            gMedications.Actions.ShowCommunicate = false;
            gMedications.Actions.ShowMergePerson = false;
            gMedications.Actions.ShowMergeTemplate = false;
            gMedications.ShowWorkflowOrCustomActionButtons = false;
            gMedications.ShowHeaderWhenEmpty = false;
            gMedications.EnableStickyHeaders = true;
        }


        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
        }

        private void lbRunTest_Click( object sender, EventArgs e )
        {
            BindMedGrid();

            
        }
        private void gMedications_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindMedGrid();
        }


        private void BindMedGrid()
        {
            var filter = new CampMedicationReportFilter();
            filter.RegistrationTemplateId = 1371;
            //filter.RegistrationInstanceId = 5251;

            var report = new CampMedicationReport( filter ).GenerateMedicationReportData();
            gMedications.DataSource = report;
            gMedications.DataBind();
        }
    }
}