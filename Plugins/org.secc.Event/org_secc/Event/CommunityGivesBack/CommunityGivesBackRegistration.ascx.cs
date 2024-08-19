using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Nest;


namespace RockWeb.Plugins.org_secc.CommunityGivesBack
{

    [DisplayName("Community Gives Back Registration")]
    [Category("SECC > Community Gives Back")]
    [Description("Registration for the Community Gives Back program")]

    [DefinedTypeField("Community Gives Back Schools",
        Description = "Defined Type that contains the list of Community Gives Back Schools.",
        IsRequired = true,
        Key = AttributeKeys.SchoolList,
        Order = 0)]
    [CodeEditorField("Acknowledgement Text",
        Description = "Lava Template that includes the data sharing agreement.",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        Order = 1,
        Key = AttributeKeys.AcknowledgementText)]
    [CodeEditorField("Confirmation Text",
        Description = "Lava Template that includes the Confirmation Text when a user completes the form.",
        IsRequired = false,
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        Order = 2,
        Key = AttributeKeys.ConfirmationText)]
    [LavaCommandsField("Enabled Lava Commands",
        Description = "The lava commands that are enabled on this block.",
        IsRequired = false,
        Order = 3,
        Key = AttributeKeys.LavaCommands)]
    [WorkflowTypeField("Registration Workflow Type",
        Description = "Community Gives Back Workflow Type",
        IsRequired = true,
        AllowMultiple = false,
        Order = 4,
        Key = AttributeKeys.RegistrationWorkflow)]
    [BooleanField("Auto Populate Registration Data",
        Description = "Auto Populate Registration Data when avaliable. Default is True.",
        DefaultBooleanValue = true,
        IsRequired = false,
        Order = 5,
        Key = AttributeKeys.AutoPopulate)]
    public partial class CommunityGivesBackRegistration : RockBlock
    {
        public class AttributeKeys
        {
            public const string SchoolList = "SchoolList";
            public const string AcknowledgementText = "AcknowledgementLava";
            public const string RegistrationWorkflow = "RegistrationWorkflow";
            public const string ConfirmationText = "ConfirmationText";
            public const string LavaCommands = "EnabledCommands";
            public const string AutoPopulate = "AutoPopulate";

        }

        #region Base Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            if (!Page.IsPostBack)
            {
                LoadStep( ActiveStep.ACKNOWLEDGEMENT );
            }
        }
        #endregion

        #region Events

        protected void btnAcknowledgeNext_Click( object sender, EventArgs e )
        {
            if(!cbAgreeToTerms.Checked)
            {
                pnlValidateTermsAgree.Visible = true;
                return;
            }

            pnlValidateTermsAgree.Visible = false;
            LoadStep( ActiveStep.CONTACT_INFO );
        }


        protected void btnContactBack_Click( object sender, EventArgs e )
        {
            LoadStep( ActiveStep.ACKNOWLEDGEMENT );
        }

        protected void btnContactNext_Click( object sender, EventArgs e )
        {
            LoadStep( ActiveStep.SCHOOL_SELECTION );
        }

        protected void btnSchoolBack_Click( object sender, EventArgs e )
        {
            LoadStep( ActiveStep.CONTACT_INFO );
        }

        protected void btnSchoolNext_Click( object sender, EventArgs e )
        {

        }

        protected void cbAgreeToTerms_CheckedChanged( object sender, EventArgs e )
        {
            btnAcknowledgeNext.Enabled = cbAgreeToTerms.Checked;
        }


        #endregion

        private void LoadAcknowledgement()
        {
            var rockContext = new RockContext();
            var acknowledgementTemplate = GetAttributeValue( AttributeKeys.AcknowledgementText );

            if(acknowledgementTemplate.IsNullOrWhiteSpace())
            {
                LoadStep( ActiveStep.CONTACT_INFO );
            }

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, CurrentPerson );
            lAcknowledgement.Text = acknowledgementTemplate.ResolveMergeFields( mergeFields, GetAttributeValue( AttributeKeys.LavaCommands ) );
            pnlValidateTermsAgree.Visible = true;
        }

        private void LoadContactInfo()
        {
            var autoPopulateContactInfo = GetAttributeValue( AttributeKeys.AutoPopulate ).AsBoolean();

            if(autoPopulateContactInfo && CurrentPerson != null)
            {
                if(tbFirstName.Text.IsNullOrWhiteSpace() &&
                    tbLastName.Text.IsNullOrWhiteSpace() &&
                    tbEmail.Text.IsNullOrWhiteSpace() &&
                    tbMobilePhone.Text.IsNullOrWhiteSpace())
                {
                    tbFirstName.Text = CurrentPerson.NickName;
                    tbLastName.Text = CurrentPerson.LastName;
                    tbEmail.Text = CurrentPerson.Email;
                    var mobilePhone = CurrentPerson.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
                    tbMobilePhone.Text = mobilePhone != null ? mobilePhone.NumberFormatted : String.Empty;
                }
            }

            pnlContactInformation.Visible = true;

        }

        private void LoadSchoolSelection()
        {
            //populate school selection
        }

        private void LoadStep(ActiveStep visiblePanel)
        {
            pnlAcknowledgement.Visible = false;
            pnlContactInformation.Visible = false;

            switch (visiblePanel)
            {
                case ActiveStep.ACKNOWLEDGEMENT:
                    LoadAcknowledgement();
                    break;
                case ActiveStep.CONTACT_INFO:
                    LoadContactInfo();
                    break;
                case ActiveStep.SCHOOL_SELECTION:
                    LoadSchoolSelection();
                    break;
                case ActiveStep.CONFIRMATION:
                    break;
                case ActiveStep.COMPLETE:
                    break;
                default:
                    break;
            }
        }


        protected enum ActiveStep
        {
            ACKNOWLEDGEMENT,
            CONTACT_INFO,
            SCHOOL_SELECTION,
            CONFIRMATION,
            COMPLETE
        }

        public class School
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int TotalSponsorships { get; set; }
            public int ClaimedSponsorships { get; set; }
            public int AvailableSponsorships
            {
                get
                {
                    return TotalSponsorships - ClaimedSponsorships;
                }
            }
        }


    }
}