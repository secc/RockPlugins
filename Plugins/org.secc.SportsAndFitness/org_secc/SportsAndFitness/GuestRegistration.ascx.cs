using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI.WebControls;
using DocumentFormat.OpenXml.Wordprocessing;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.SportsAndFitness
{

    [DisplayName( "Sports and Fitness Guest Registration" )]
    [Category( "SECC > Sports and Fitness" )]
    [Description( "Register/Sign in a guest into Sports and Fitness Center" )]

    [WorkflowTypeField( "Guest Registration Workflow",
        Description = "Workflow that manages the sports and fitness guest registration process.",
        AllowMultiple = false,
        IsRequired = true,
        Order = 0,
        Key = AttributeKeys.GuestWorkflowKey )]
    [CodeEditorField( "Welcome Message",
        Description = "Introduction message for the Welcome Panel. Supports Lava.",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        IsRequired = false,
        DefaultValue = "",
        Order = 1,
        Key = AttributeKeys.WelcomeIntroKey )]
    [CodeEditorField( "Existing Guest Message",
        Description = "Welcome message to display to existing guests when entering phone number",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        IsRequired = false,
        DefaultValue = "",
        Order = 2,
        Key = AttributeKeys.ExistingGuestMessageKey )]
    [LavaCommandsField( "Lava Commands",
        Description = "Lava commands that are supported by this block",
        IsRequired = false,
        DefaultValue = "",
        Order = 3,
        Key = AttributeKeys.LavaCommandKey )]
    [CodeEditorField( "New Guest Message",
        Description = "Message/Instructions for New Guest screen",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        IsRequired = true,
        DefaultValue = "",
        Order = 4,
        Key = AttributeKeys.NewGuestMessageKey )]
    [CodeEditorField( "Finish Message",
        Description = "Complete/Finish Message for the finish screen",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        IsRequired = false,
        DefaultValue = "",
        Order = 5,
        Key = AttributeKeys.FinishMessageKey )]
    [DefinedValueField( "Connection Status",
        Description = "Connection Status that is used for Sports and Fitness Guest",
        AllowMultiple = false,
        DefinedTypeGuid = "2e6540ea-63f0-40fe-be50-f2a84735e600",
        IsRequired = true,
        Order = 3,
        Key = AttributeKeys.ConnectionStatusKey )]
    [CampusField( "Default Campus", "Default Campus for new Sports and Fitness Guests", true, "", "", 4, AttributeKeys.DefaultCampusKey )]




    public partial class GuestRegistration : RockBlock
    {
        public static class AttributeKeys
        {
            public const string ConnectionStatusKey = "ConnectionStatus";
            public const string ExistingGuestMessageKey = "ExistingGuestMessage";
            public const string NewGuestMessageKey = "NeweGuestMessage";
            public const string FinishMessageKey = "FinishMessage";
            public const string GuestWorkflowKey = "GuestWorkflow";
            public const string InvitationQSKey = "Invitation";
            public const string LavaCommandKey = "LavaCommands";
            public const string WelcomeIntroKey = "WelcomeIntro";
            public const string DefaultCampusKey = "DefaultCampus";
        }

        private Guid? _invitationGuid;

        private Guid? InvitationGuid
        {
            get
            {
                if (!_invitationGuid.HasValue)
                {
                    _invitationGuid = ViewState["SFGuest_Invitation"] as Guid?;
                }
                return _invitationGuid;
            }
            set
            {
                _invitationGuid = value;
                ViewState["SFGuest_Invitation"] = value;
            }
        }

        #region Base Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            lbPreviousGuest.Click += lbPreviousGuest_Click;
            lbNewGuest.Click += lbNewGuest_Click;
            lbSearchReturningGuest.Click += lbSearchReturningGuest_Click;
            lbCancelReturningGuest.Click += lbCancelReturningGuest_Click;
            lbSaveNewGuest.Click += lbSaveNewGuest_Click;
            lbCancelNewGuest.Click += lbCancelNewGuest_Click;
            lbFinish.Click += lbFinish_Click;
        }



        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if (!IsPostBack)
            {
                LoadInvitation();
            }
        }
        #endregion

        #region Events
        private void lbNewGuest_Click( object sender, EventArgs e )
        {
            LoadNewGuestForm();
        }

        private void lbCancelReturningGuest_Click( object sender, EventArgs e )
        {
            nbReturningGuest.Visible = false;
            tbReturningGuestNumber.Text = string.Empty;
            InvitationGuid = null;
            LoadInvitation();

        }

        private void lbSearchReturningGuest_Click( object sender, EventArgs e )
        {
            nbReturningGuest.Visible = false;
            var phonenumber = tbReturningGuestNumber.Text.Trim();

            if (phonenumber.IsNullOrWhiteSpace())
            {
                nbReturningGuest.Text = "Phone Number is required";
                nbReturningGuest.NotificationBoxType = NotificationBoxType.Validation;
                nbReturningGuest.Visible = true;
                return;
            }

            if (phonenumber.Length < 7)
            {
                nbReturningGuest.Text = "Phone Number must be at least 7 digits.";
                nbReturningGuest.NotificationBoxType = NotificationBoxType.Validation;
                nbReturningGuest.Visible = true;
                return;
            }

            LoadReturningGuest( phonenumber );
        }

        private void lbPreviousGuest_Click( object sender, EventArgs e )
        {
            LoadPreviousGuestPanel();
        }

        private void lbCancelNewGuest_Click( object sender, EventArgs e )
        {
            ClearNewGuestFields();
            InvitationGuid = null;
            LoadInvitation();
        }

        private void lbFinish_Click( object sender, EventArgs e )
        {
            ClearNewGuestFields();
            InvitationGuid = null;
            LoadInvitation();
        }

        private void lbSaveNewGuest_Click( object sender, EventArgs e )
        {
            AddNewGuest();
        }
        #endregion Events

        #region Private Methods

        private void AddNewGuest()
        {
            Person guest = null;
            var personRecordType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON );
            var personInactiveStatus = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE );
            var pendingRecordStatus = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING );
            var connectionStatus = DefinedValueCache.Get( GetAttributeValue( AttributeKeys.ConnectionStatusKey ) );
            var mobilePhoneType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE );
            var defaultCampus = CampusCache.Get( GetAttributeValue( AttributeKeys.DefaultCampusKey ) );

            var lastName = tbLastName.Text.Trim();
            var firstName = tbFirstName.Text.Trim();
            var email = tbEmail.Text.Trim();
            var phoneNumber = PhoneNumber.CleanNumber( tbMobile.Text.Trim() );

            Gender? gender = null;

            switch (ddlGender.SelectedValue.AsInteger())
            {
                case 1:
                    gender = Gender.Male;
                    break;
                case 2:
                    gender = Gender.Female;
                    break;
                default:
                    gender = Gender.Unknown;
                    break;
            }

            using (var context = new RockContext())
            {

                var personService = new PersonService( context );
                var personQry = personService.Queryable().AsNoTracking()
                    .Where( p => p.RecordTypeValueId == personRecordType.Id )
                    .Where( p => p.RecordStatusValueId != personInactiveStatus.Id )
                    .Where( p => p.LastName == lastName )
                    .Where( p => p.FirstName == firstName || p.NickName == firstName )
                    .Where( p => p.Email == email )
                    .Where( p => p.PhoneNumbers.Where( n => n.Number == phoneNumber ).Count() > 0 )
                    .Where( p => p.Gender == Gender.Unknown || p.Gender == gender )
                    .Where( p => p.BirthDate == dpBirthDate.SelectedDate );

                if (personQry.Count() == 1)
                {
                    guest = personQry.SingleOrDefault();
                }
            }

            if (guest == null)
            {
                using (var personContext = new RockContext())
                {
                    var personService = new PersonService( personContext );
                    var person = new Person()
                    {
                        FirstName = firstName,
                        NickName = firstName,
                        LastName = lastName,
                        Gender = gender.Value,
                        Email = email,
                        BirthMonth = dpBirthDate.SelectedDate.Value.Month,
                        BirthDay = dpBirthDate.SelectedDate.Value.Day,
                        BirthYear = dpBirthDate.SelectedDate.Value.Year,
                        RecordTypeValueId = personRecordType.Id,
                        RecordStatusValueId = pendingRecordStatus.Id,
                        ConnectionStatusValueId = connectionStatus.Id
                    };


                    var numberFormatted = PhoneNumber.FormattedNumber( "1", phoneNumber );
                    person.PhoneNumbers.Add( new PhoneNumber
                    {
                        NumberTypeValueId = mobilePhoneType.Id,
                        Number = phoneNumber,
                        NumberFormatted = numberFormatted,
                        IsMessagingEnabled = false,
                        IsUnlisted = false
                    } );

                    personService.Add( person );
                    personContext.SaveChanges();

                    var groupService = new GroupService( personContext );
                    var familyGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY );

                    GroupTypeRoleCache groupTypeRole = person.Age >= 18 ?
                        familyGroupType.Roles.Where( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ).FirstOrDefault() :
                        familyGroupType.Roles.Where( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() ).FirstOrDefault();

                    var familyGroup = new Group()
                    {
                        GroupTypeId = familyGroupType.Id,
                        Name = $"{lastName} Family",
                        CampusId = defaultCampus.Id,
                        IsActive = true
                    };
                    familyGroup.Members.Add( new GroupMember
                    {
                        PersonId = person.Id,
                        GroupRoleId = groupTypeRole.Id,
                        GroupMemberStatus = GroupMemberStatus.Active
                    } );

                    groupService.Add( familyGroup );
                    personContext.SaveChanges();
                    guest = person;
                }
            }
            LoadGuest( guest );

        }

        private void ClearNewGuestFields()
        {
            tbFirstName.Text = string.Empty;
            tbLastName.Text = string.Empty;
            tbMobile.Text = string.Empty;
            tbEmail.Text = string.Empty;
            dpBirthDate.SelectedDate = null;
            dpBirthDate.AllowFutureDateSelection = false;
            ddlGender.SelectedValue = string.Empty;

        }

        private void HidePanels()
        {
            pnlWelcome.Visible = false;
            pnlReturningGuest.Visible = false;
            pnlNewGuest.Visible = false;

        }

        private void LoadInvitation()
        {
            InvitationGuid = PageParameter( AttributeKeys.InvitationQSKey ).AsGuidOrNull();

            if (!InvitationGuid.HasValue)
            {
                LoadWelcomePanel();
            }

        }

        private void LoadReturningGuest( string phoneNumber )
        {
            using (var context = new RockContext())
            {
                phoneNumber = PhoneNumber.CleanNumber( phoneNumber );


                var inactiveValue = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE );
                var personRecord = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON );

                var phoneService = new PhoneNumberService( context );
                var phonePerson = phoneService.Queryable().AsNoTracking()
                    .Where( n => n.Number == phoneNumber )
                    .Where( n => n.Person.RecordStatusValueId != inactiveValue.Id )
                    .Where( n => n.Person.RecordTypeValueId == personRecord.Id )
                    .Where( n => n.Person.IsDeceased == false )
                    .Select( n => n.Person )
                    .ToList();

                if (phonePerson.Count != 1)
                {
                    LoadNewGuestForm();
                }
                else
                {
                    LoadGuest( phonePerson.Single() );
                }

            }
        }

        private void LoadFinishPanel( Person p )
        {
            var mergeFields = new Dictionary<string, object>();
            mergeFields.Add( "Guest", p );
            HidePanels();
            pnlFinish.Visible = true;
            lFinishMessage.Text = ProcessLava( GetAttributeValue( AttributeKeys.FinishMessageKey ) );

        }

        private void LoadGuest( Person p )
        {
            LoadFinishPanel( p );
        }

        private void LoadWelcomePanel()
        {
            HidePanels();
            pnlWelcome.Visible = true;
            var lava = GetAttributeValue( AttributeKeys.WelcomeIntroKey );
            lWelcomeLava.Text = ProcessLava( lava );

        }

        private void LoadNewGuestForm( bool guestNotFound = false )
        {
            HidePanels();
            pnlNewGuest.Visible = true;
            ClearNewGuestFields();

        }

        private void LoadPreviousGuestPanel()
        {
            HidePanels();
            pnlReturningGuest.Visible = true;
            lReturningGuestMessage.Text = ProcessLava( GetAttributeValue( AttributeKeys.ExistingGuestMessageKey ) );


        }

        private string ProcessLava( string lavaTemplate, Dictionary<string, object> importedFields = null )
        {
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, CurrentPerson );

            if (importedFields != null)
            {
                foreach (var item in importedFields)
                {
                    mergeFields.Add( item.Key, item.Value );
                }
            }

            return lavaTemplate.ResolveMergeFields( mergeFields, GetAttributeValue( AttributeKeys.LavaCommandKey ) );
        }

        #endregion
    }
}