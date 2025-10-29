using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;




namespace RockWeb.Plugins.org_secc.CommunityGivesBack
{

    [DisplayName( "Community Gives Back Registration" )]
    [Category( "SECC > Community Gives Back" )]
    [Description( "Registration for the Community Gives Back program" )]

    [DefinedTypeField( "Community Gives Back Schools",
        Description = "Defined Type that contains the list of Community Gives Back Schools.",
        IsRequired = true,
        Key = AttributeKeys.SchoolList,
        Order = 0 )]
    [CodeEditorField( "Acknowledgement Text",
        Description = "Lava Template that includes the data sharing agreement.",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        Order = 1,
        Key = AttributeKeys.AcknowledgementText )]
    [CodeEditorField( "Confirmation Text",
        Description = "Lava Template that includes the Confirmation Text when a user completes the form.",
        IsRequired = false,
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        Order = 2,
        Key = AttributeKeys.ConfirmationText )]
    [CodeEditorField("Registration Complete Text",
        Description = "Lava Template that includes the success/complete message when a user successfully signs up.",
        IsRequired = true,
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        Order = 3,
        Key = AttributeKeys.RegistrationCompleteText)]
    [LavaCommandsField( "Enabled Lava Commands",
        Description = "The lava commands that are enabled on this block.",
        IsRequired = false,
        Order = 4,
        Key = AttributeKeys.LavaCommands )]
    [WorkflowTypeField( "Registration Workflow Type",
        Description = "Community Gives Back Workflow Type",
        IsRequired = true,
        AllowMultiple = false,
        Order = 4,
        Key = AttributeKeys.RegistrationWorkflow )]
    [BooleanField( "Auto Populate Registration Data",
        Description = "Auto Populate Registration Data when avaliable. Default is True.",
        DefaultBooleanValue = true,
        IsRequired = false,
        Order = 5,
        Key = AttributeKeys.AutoPopulate )]
    [CustomDropdownListField("Campaign",
        Description = "The default campaign that this school list is associated wtih.",
        ListSource = DefaultCampaignSql,
        IsRequired = true,
        Key = AttributeKeys.CGBCampaign,
        Order = 6)]

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
            public const string CGBCampaign = "CGBCampaign";
            public const string RegistrationCompleteText = "CompleteText";

        }

        public const string DefaultCampaignSql = @"
            SELECT DISTINCT av.[Value] as Value, av.[Value] as Text
            FROM AttributeValue av 
            INNER JOIN Attribute a on av.AttributeId = a.Id
            WHERE a.[Guid] = 'BBE01EA2-357C-4289-AFF9-585CB5B3B88C'";

        protected List<SupportedSchool> SchoolList
        {
            get
            {
                return ViewState[this.BlockId + "_SchoolList"] as List<SupportedSchool>;
            }
            set
            {
                ViewState[this.BlockId + "_SchoolList"] = value;
            }
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
                SchoolList = null;
                LoadStep( ActiveStep.ACKNOWLEDGEMENT );
            }
            LoadSchoolList();
        }
        #endregion

        #region Events

        protected void btnAcknowledgeNext_Click( object sender, EventArgs e )
        {
            if (!cbAgreeToTerms.Checked)
            {
                pnlValidateTermsAgree.Visible = true;
                return;
            }

            pnlValidateTermsAgree.Visible = false;
            LoadStep( ActiveStep.CONTACT_INFO );
        }

        protected void btnConfirmationBack_Click( object sender, EventArgs e )
        {
            LoadStep( ActiveStep.SCHOOL_SELECTION );
        }

        protected void btnConfirmationFinish_Click( object sender, EventArgs e )
        {
            ProcessRegistration();
            LoadStep( ActiveStep.COMPLETE );
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
            LoadStep( ActiveStep.CONFIRMATION );
        }

        protected void cbAgreeToTerms_CheckedChanged( object sender, EventArgs e )
        {
            btnAcknowledgeNext.Enabled = cbAgreeToTerms.Checked;
        }


        protected void ddlSchools_SelectedIndexChanged( object sender, EventArgs e )
        {
            var selectedValue = ddlSchools.SelectedValue.AsInteger();

            var school = SchoolList.FirstOrDefault( s => s.Id == selectedValue );

            if (school == null)
            {
                nudSponsorships.Enabled = false;
            }
            else
            {
                nudSponsorships.Enabled = true;
                nudSponsorships.Maximum = school.AvailableSponsorships;
            }
        }

        #endregion

        private void LoadSchoolList()
        {

            if (SchoolList != null)
            {
                return;
            }

            var rockContext = new RockContext();
            var definedTypeGuid = GetAttributeValue( AttributeKeys.SchoolList ).AsGuid();
            var workflowTypeCache = WorkflowTypeCache.Get( GetAttributeValue( AttributeKeys.RegistrationWorkflow ).AsGuid() );
            var workflowTypeString = workflowTypeCache.Id.ToString();

            var definedValueEntityType = EntityTypeCache.Get( typeof( DefinedValue ) );
            var workflowEntityType = EntityTypeCache.Get( typeof( Workflow ) );

            var attributeValueService = new AttributeValueService( rockContext );

            var workflowAttributeValues = attributeValueService.Queryable().AsNoTracking()
                .Where( v => v.Attribute.EntityTypeId == workflowEntityType.Id )
                .Where( v => v.Attribute.EntityTypeQualifierColumn == "WorkflowTypeId" )
                .Where( v => v.Attribute.EntityTypeQualifierValue == workflowTypeString );

            var signups = new WorkflowService( rockContext ).Queryable().AsNoTracking()
                .Where( w => w.WorkflowTypeId == workflowTypeCache.Id )
                .Join( workflowAttributeValues, w => w.Id, v => v.EntityId,
                    ( w, v ) => new { WorkflowId = w.Id, AttributeKey = v.Attribute.Key, Value = v.Value } )
                .GroupBy( w => w.WorkflowId )
                .Select( w => new
                {
                    WorkflowId = w.Key,
                    SchoolGuid = w.FirstOrDefault( v => v.AttributeKey == "School" ).Value,
                    Sponsorships = w.FirstOrDefault( v => v.AttributeKey == "StudentsToSponsor" ).Value
                } )
                .ToList()
                .GroupBy( s => s.SchoolGuid )
                .Select( s => new { SchoolGuid = s.Key.AsGuid(), SponsoredStudents = s.Sum( s1 => s1.Sponsorships.AsInteger() ) } )
                .ToList();

            var definedType = DefinedTypeCache.Get( definedTypeGuid );
            var definedTypeIdStr = definedType.Id.ToString();

            var baseAVQry = attributeValueService.Queryable().AsNoTracking()
                .Where(v => v.Attribute.EntityTypeId == definedValueEntityType.Id)
                .Where(v => v.Attribute.EntityTypeQualifierColumn == "DefinedTypeId")
                .Where(v => v.Attribute.EntityTypeQualifierValue == definedTypeIdStr);

            var sponsorshipAvailableValues = baseAVQry
                .Where( v => v.Attribute.Key == "SponsorshipsAvailable" )
                .Select( v => new { DefinedValueId = v.EntityId, TotalSponsorships = v.ValueAsNumeric } )
                .ToList();

            var selectedCampaign = GetAttributeValue(AttributeKeys.CGBCampaign);


            var campaignDVIDs = baseAVQry
                .Where(v => v.Attribute.Key == "Year")
                .Where(v => v.Value == selectedCampaign)
                .Select(v => v.EntityId.Value)
                .ToList();




            SchoolList = new DefinedValueService( rockContext ).Queryable()
                .Where( v => v.DefinedTypeId == definedType.Id )
                .Where(v => campaignDVIDs.Contains(v.Id))
                .Where( v => v.IsActive )
                .ToList()
                .Select( d => new SupportedSchool
                {
                    Id = d.Id,
                    Guid = d.Guid,
                    Name = d.Value,
                    TotalSponsorships = ((int?) sponsorshipAvailableValues.Where( a => a.DefinedValueId == d.Id ).Select( a => a.TotalSponsorships ).FirstOrDefault()) ?? 0,
                    ClaimedSponsorships = signups.Where( s => s.SchoolGuid == d.Guid ).Select( s => s.SponsoredStudents ).FirstOrDefault()
                } )
                .Where( v => v.AvailableSponsorships > 0 )
                .ToList();
        }

        private void LoadAcknowledgement()
        {
            var rockContext = new RockContext();
            var acknowledgementTemplate = GetAttributeValue( AttributeKeys.AcknowledgementText );

            if (acknowledgementTemplate.IsNullOrWhiteSpace())
            {
                LoadStep( ActiveStep.CONTACT_INFO );
            }

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, CurrentPerson );
            lAcknowledgement.Text = acknowledgementTemplate.ResolveMergeFields( mergeFields, GetAttributeValue( AttributeKeys.LavaCommands ) );
            pnlValidateTermsAgree.Visible = false;
            pnlAcknowledgement.Visible = true;
        }

        private void LoadConfirmation()
        {
            var school = SchoolList.Where( s => s.Id == ddlSchools.SelectedValue.AsInteger() ).FirstOrDefault();

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, CurrentPerson );
            mergeFields.Add( "RegistrantFirstName", tbFirstName.Text.Trim() );
            mergeFields.Add( "RegistrantLastName", tbLastName.Text.Trim() );
            mergeFields.Add( "RegistrantEmail", tbEmail.Text.Trim() );
            mergeFields.Add( "MobileNumber", tbMobilePhone.Text.Trim() );
            mergeFields.Add( "SchoolName", school.Name );
            mergeFields.Add( "ChildrenSponsored", nudSponsorships.Value );
            mergeFields.Add( "SponsorSiblingGroup", rblSiblingGroups.SelectedItem.Text );
            lConfirmationText.Text = GetAttributeValue( AttributeKeys.ConfirmationText ).ResolveMergeFields( mergeFields, GetAttributeValue( AttributeKeys.LavaCommands ) );

            pnlConfirmation.Visible = true;
        }

        private void LoadCompletePanel()
        {
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields(this.RockPage, CurrentPerson);
            lRegistrationCompleteText.Text = GetAttributeValue(AttributeKeys.RegistrationCompleteText).ResolveMergeFields(mergeFields, GetAttributeValue(AttributeKeys.LavaCommands));

            pnlComplete.Visible = true;
        }

        private void LoadContactInfo()
        {
            var autoPopulateContactInfo = GetAttributeValue( AttributeKeys.AutoPopulate ).AsBoolean();

            if (autoPopulateContactInfo && CurrentPerson != null)
            {
                if (tbFirstName.Text.IsNullOrWhiteSpace() &&
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
            if (ddlSchools.Items.Count == 0 || ddlSchools.SelectedValue == "0")
            {
                ddlSchools.DataValueField = "Id";
                ddlSchools.DataTextField = "DataTextField";
                ddlSchools.DataSource = SchoolList.OrderBy( s => s.Name ).ToList();
                ddlSchools.DataBind();

                ddlSchools.Items.Insert( 0, new ListItem( "", "0" ) );
                nudSponsorships.Enabled = false;
            }
            else
            {
                nudSponsorships.Enabled = true;
            }

            pnlSelectSchool.Visible = true;

        }

        private void LoadStep( ActiveStep visiblePanel )
        {
            pnlAcknowledgement.Visible = false;
            pnlContactInformation.Visible = false;
            pnlSelectSchool.Visible = false;
            pnlConfirmation.Visible = false;
            pnlComplete.Visible = false;

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
                    LoadConfirmation();
                    break;
                case ActiveStep.COMPLETE:
                    LoadCompletePanel();
                    break;
                default:
                    break;
            }
        }

        private void ProcessRegistration()
        {
            var workflowTypeGuid = GetAttributeValue( AttributeKeys.RegistrationWorkflow ).AsGuid();
            var workflowTypeCache = WorkflowTypeCache.Get( workflowTypeGuid );
            var school = SchoolList.FirstOrDefault( s => s.Id == ddlSchools.SelectedValueAsInt() );


            var workflowTitle = $"{tbFirstName.Text.Trim()} {tbLastName.Text.Trim()} - Sponsorship Registration";
            var workflow = Workflow.Activate( workflowTypeCache, workflowTitle );
            if(CurrentPersonAliasId.HasValue)
            {
                workflow.InitiatorPersonAliasId = CurrentPersonAliasId;
            }

            workflow.SetAttributeValue( "FirstName", tbFirstName.Text.Trim() );
            workflow.SetAttributeValue( "LastName", tbLastName.Text.Trim() );
            workflow.SetAttributeValue( "Email", tbEmail.Text.Trim() );
            workflow.SetAttributeValue( "MobilePhone", tbMobilePhone.Text.Trim() );
            workflow.SetAttributeValue( "School", school.Guid.ToString() );
            workflow.SetAttributeValue( "StudentstoSponsor", nudSponsorships.Value.ToString() );
            workflow.SetAttributeValue( "SponsorSiblingGroup", rblSiblingGroups.SelectedValue.AsBoolean().ToString() );
            workflow.SetAttributeValue( "InfoShareDate", RockDateTime.Now.ToRfc822DateTime() );

            List<string> errors = new List<string>();
            new WorkflowService( new RockContext() ).Process( workflow, out errors );
        }


        protected enum ActiveStep
        {
            ACKNOWLEDGEMENT,
            CONTACT_INFO,
            SCHOOL_SELECTION,
            CONFIRMATION,
            COMPLETE
        }

        [Serializable]
        public class SupportedSchool 
        {
            public int Id { get; set; }
            public Guid Guid { get; set; }
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
            public string DataTextField
            {
                get
                {
                    return $"{Name} - {AvailableSponsorships} Available";
                }
            }
        }






    }
}