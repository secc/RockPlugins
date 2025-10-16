﻿// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
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
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using org.secc.GroupManager.Model;
using org.secc.PersonMatch;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.GroupManager
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Publish Group Registration" )]
    [Category( "SECC > Groups" )]
    [Description( "Allows a person to register for a published group." )]

    [CustomRadioListField( "Group Member Status", "The group member status to use when adding person to group (default: 'Pending'.)", "2^Pending,1^Active,0^Inactive", true, "2", "", 2 )]
    [DefinedValueField( "2E6540EA-63F0-40FE-BE50-F2A84735E600", "Connection Status", "The connection status to use for new individuals (default: 'Web Prospect'.)", true, false, "368DD475-242C-49C4-A42C-7278BE690CC2", "", 3 )]
    [DefinedValueField( "8522BADD-2871-45A5-81DD-C76DA07E2E7E", "Record Status", "The record status to use for new individuals (default: 'Pending'.)", true, false, "283999EC-7346-42E3-B807-BCE9B2BABB49", "", 4 )]
    [WorkflowTypeField( "Workflow", "An optional workflow to start when registration is created. The GroupMember will set as the workflow 'Entity' when processing is started.", false, false, "", "", 5 )]
    [CodeEditorField( "Lava Template", "The lava template to use to format the group details.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, @"
", "", 7 )]
    [LinkedPage( "Result Page", "An optional page to redirect user to after they have been registered for the group.", false, "", "", 8 )]
    [CodeEditorField( "Result Lava Template", "The lava template to use to format result message after user has been registered. Will only display if user is not redirected to a Result Page ( previous setting ).", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, @"
", "", 9 )]
    [CustomRadioListField( "Auto Fill Form", "If set to FALSE then the form will not load the context of the logged in user (default: 'True'.)", "true^True,false^False", true, "true", "", 10 )]
    [TextField( "Register Button Alt Text", "Alternate text to use for the Register button (default is 'Register').", false, "", "", 11 )]
    [BooleanField( "Prevent Overcapacity Registrations", "When set to true, user cannot register for groups that are at capacity or whose default GroupTypeRole are at capacity. If only one spot is available, no spouses can be registered.", true, "", 12 )]
    [BooleanField( "Require Email", "Should email be required for registration?", true, key: REQUIRE_EMAIL_KEY )]
    [BooleanField( "Require Mobile Phone", "Should mobile phone numbers be required for registration?", false, key: REQUIRE_MOBILE_KEY )]
    [BooleanField( "Require DOB", "Should DOB be required for registration?", false, key: REQUIRE_DOB )]

    public partial class PublishGroupRegistration : RockBlock
    {
        #region Fields
        private const string REQUIRE_EMAIL_KEY = "IsRequireEmail";
        private const string REQUIRE_MOBILE_KEY = "IsRequiredMobile";
        private const string REQUIRE_DOB = "IsRequiredDOB";

        RockContext _rockContext = null;
        bool _showSpouse = false;
        PublishGroup _publishGroup = null;
        GroupTypeRole _defaultGroupRole = null;
        DefinedValueCache _dvcConnectionStatus = null;
        DefinedValueCache _dvcRecordStatus = null;
        DefinedValueCache _married = null;
        DefinedValueCache _homeAddressType = null;
        GroupTypeCache _familyType = null;
        GroupTypeRoleCache _adultRole = null;
        bool _autoFill = true;
        bool _isValidSettings = true;

        #endregion

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !CheckSettings() )
            {
                _isValidSettings = false;
                pnlView.Visible = false;
            }
            else
            {
                nbNotice.Visible = false;
                pnlView.Visible = true;

                if ( !Page.IsPostBack )
                {
                    ShowDetails();
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the Block control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            ShowDetails();
        }

        /// <summary>
        /// Handles the Click event of the btnRegister control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnRegister_Click( object sender, EventArgs e )
        {
            // Check _isValidSettings in case the form was showing and they clicked the visible register button.
            if ( Page.IsValid && _isValidSettings )
            {
                var rockContext = new RockContext();
                var personService = new PersonService( rockContext );

                Person person = null;
                Person spouse = null;
                Group family = null;
                GroupLocation homeLocation = null;
                bool isMatch = false;

                // Only use current person if the name entered matches the current person's name and autofill mode is true
                if ( _autoFill )
                {
                    if ( CurrentPerson != null && CurrentPerson.NickName.IsNotNullOrWhiteSpace() && CurrentPerson.LastName.IsNotNullOrWhiteSpace() &&
                        tbFirstName.Text.Trim().Equals( CurrentPerson.NickName.Trim(), StringComparison.OrdinalIgnoreCase ) &&
                        tbLastName.Text.Trim().Equals( CurrentPerson.LastName.Trim(), StringComparison.OrdinalIgnoreCase ) )
                    {
                        person = personService.Get( CurrentPerson.Id );
                        isMatch = true;
                    }
                }

                // Try to find person by name/email 
                if ( person == null )
                {
                    var personQuery = new PersonService.PersonMatchQuery( tbFirstName.Text.Trim(), tbLastName.Text.Trim(), tbEmail.Text.Trim(), pnCell.Text.Trim() );
                    person = personService.FindPerson( personQuery, true );
                    if ( person != null )
                    {
                        isMatch = true;
                    }
                }

                // Check to see if this is a new person
                if ( person == null )
                {
                    var people = personService.GetByMatch(
                                            tbFirstName.Text.Trim(),
                                            tbLastName.Text.Trim(),
                                            dppDOB.SelectedDate,
                                            tbEmail.Text.Trim(),
                                            pnCell.Text.Trim(),
                                            acAddress.Street1,
                                            acAddress.PostalCode );
                    if ( people.Count() == 1 &&
                         // Make sure their email matches.  If it doesn't, we need to go ahead and create a new person to be matched later.
                         ( string.IsNullOrWhiteSpace( tbEmail.Text.Trim() ) ||
                         ( people.First().Email != null &&
                         tbEmail.Text.ToLower().Trim() == people.First().Email.ToLower().Trim() ) ) &&

                         // Make sure their DOB matches.  If it doesn't, we need to go ahead and create a new person to be matched later.
                         ( !dppDOB.SelectedDate.HasValue ||
                         ( people.First().BirthDate != null &&
                         dppDOB.SelectedDate.Value == people.First().BirthDate ) )
                       )
                    {
                        person = people.First();
                    }
                    else
                    {

                        // If so, create the person and family record for the new person
                        person = new Person();
                        person.FirstName = tbFirstName.Text.Trim();
                        person.LastName = tbLastName.Text.Trim();
                        person.Email = tbEmail.Text.Trim();
                        if ( dppDOB.SelectedDate.HasValue )
                        {
                            person.BirthDay = dppDOB.SelectedDate.Value.Day;
                            person.BirthMonth = dppDOB.SelectedDate.Value.Month;
                            person.BirthYear = dppDOB.SelectedDate.Value.Year;
                        }
                        person.IsEmailActive = true;
                        person.EmailPreference = EmailPreference.EmailAllowed;
                        person.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                        person.ConnectionStatusValueId = _dvcConnectionStatus.Id;
                        person.RecordStatusValueId = _dvcRecordStatus.Id;
                        person.Gender = Gender.Unknown;

                        family = PersonService.SaveNewPerson( person, rockContext, _publishGroup.Group.CampusId, false );
                    }
                }
                else
                {
                    // updating current existing person
                    person.Email = tbEmail.Text;

                    // Get the current person's families
                    var families = person.GetFamilies( rockContext );

                    // If address can being entered, look for first family with a home location

                    foreach ( var aFamily in families )
                    {
                        homeLocation = aFamily.GroupLocations
                            .Where( l =>
                                l.GroupLocationTypeValueId == _homeAddressType.Id )
                            .FirstOrDefault();
                        if ( homeLocation != null )
                        {
                            family = aFamily;
                            break;
                        }
                    }


                    // If a family wasn't found with a home location, use the person's first family
                    if ( family == null )
                    {
                        family = families.FirstOrDefault();
                    }
                }


                if ( !isMatch || !string.IsNullOrWhiteSpace( pnHome.Number ) )
                {
                    SetPhoneNumber( rockContext, person, pnHome, null, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() );
                }
                if ( !isMatch || !string.IsNullOrWhiteSpace( pnCell.Number ) )
                {
                    SetPhoneNumber( rockContext, person, pnCell, cbSms, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
                }

                if ( !isMatch || !string.IsNullOrWhiteSpace( acAddress.Street1 ) )
                {
                    string oldLocation = homeLocation != null ? homeLocation.Location.ToString() : string.Empty;
                    string newLocation = string.Empty;

                    var location = new LocationService( rockContext ).Get( acAddress.Street1, acAddress.Street2, acAddress.City, acAddress.State, acAddress.PostalCode, acAddress.Country );
                    if ( location != null )
                    {
                        if ( homeLocation == null )
                        {
                            homeLocation = new GroupLocation();
                            homeLocation.GroupLocationTypeValueId = _homeAddressType.Id;
                            homeLocation.IsMappedLocation = true;

                            if ( homeLocation == null ){
                                Console.WriteLine( "Home location is null!" );
                            }
                            
                            if ( family == null )
                            {
                                Console.WriteLine( "Family is null!" );
                            }

                            if ( family.GroupLocations == null )
                            {
                                Console.WriteLine( "Family.GroupLocations is null!" );
                            }

                            family.GroupLocations.Add( homeLocation );
                        }
                        else
                        {
                            oldLocation = homeLocation.Location.ToString();
                        }

                        homeLocation.Location = location;
                        newLocation = location.ToString();
                    }
                    else
                    {
                        if ( homeLocation != null )
                        {
                            homeLocation.Location = null;
                            family.GroupLocations.Remove( homeLocation );
                            new GroupLocationService( rockContext ).Delete( homeLocation );
                        }
                    }
                }

                // Check for the spouse
                if ( cbRegisterSpouse.Checked && _showSpouse && tbSpouseFirstName.Text.IsNotNullOrWhiteSpace() && tbSpouseLastName.Text.IsNotNullOrWhiteSpace() )
                {
                    spouse = person.GetSpouse( rockContext );
                    bool isSpouseMatch = true;

                    if ( spouse == null ||
                        !tbSpouseFirstName.Text.Trim().Equals( spouse.FirstName.Trim(), StringComparison.OrdinalIgnoreCase ) ||
                        !tbSpouseLastName.Text.Trim().Equals( spouse.LastName.Trim(), StringComparison.OrdinalIgnoreCase ) ||
                         // Make sure their email matches.  If it doesn't, we need to go ahead and create a new person to be matched later.
                         ( string.IsNullOrWhiteSpace( tbSpouseEmail.Text.Trim() ) ||
                         ( spouse.Email != null &&
                         tbSpouseEmail.Text.ToLower().Trim() != spouse.Email.ToLower().Trim() ) ) &&

                         // Make sure their DOB matches.  If it doesn't, we need to go ahead and create a new person to be matched later.
                         ( !dppSpouseDOB.SelectedDate.HasValue ||
                         ( spouse.BirthDate != null &&
                         dppSpouseDOB.SelectedDate.Value != spouse.BirthDate ) )
                        )
                    {
                        spouse = new Person();
                        isSpouseMatch = false;

                        spouse.FirstName = tbSpouseFirstName.Text.FixCase();
                        spouse.LastName = tbSpouseLastName.Text.FixCase();

                        if ( dppSpouseDOB.SelectedDate.HasValue )
                        {
                            spouse.BirthDay = dppSpouseDOB.SelectedDate.Value.Day;
                            spouse.BirthMonth = dppSpouseDOB.SelectedDate.Value.Month;
                            spouse.BirthYear = dppSpouseDOB.SelectedDate.Value.Year;
                        }

                        spouse.ConnectionStatusValueId = _dvcConnectionStatus.Id;
                        spouse.RecordStatusValueId = _dvcRecordStatus.Id;
                        spouse.Gender = Gender.Unknown;

                        spouse.IsEmailActive = true;
                        spouse.EmailPreference = EmailPreference.EmailAllowed;

                        var groupMember = new GroupMember();
                        groupMember.GroupRoleId = _adultRole.Id;
                        groupMember.Person = spouse;

                        family.Members.Add( groupMember );

                        spouse.MaritalStatusValueId = _married.Id;
                        person.MaritalStatusValueId = _married.Id;
                    }

                    spouse.Email = tbSpouseEmail.Text;

                    if ( !isSpouseMatch || !string.IsNullOrWhiteSpace( pnHome.Number ) )
                    {
                        SetPhoneNumber( rockContext, spouse, pnHome, null, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() );
                    }

                    if ( !isSpouseMatch || !string.IsNullOrWhiteSpace( pnSpouseCell.Number ) )
                    {
                        SetPhoneNumber( rockContext, spouse, pnSpouseCell, cbSpouseSms, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
                    }
                }


                // Save the person/spouse and change history 
                rockContext.SaveChanges();

                // Check to see if a workflow should be launched for each person
                WorkflowTypeCache workflowType = null;
                Guid? workflowTypeGuid = GetAttributeValue( "Workflow" ).AsGuidOrNull();
                if ( workflowTypeGuid.HasValue )
                {
                    workflowType = WorkflowTypeCache.Get( workflowTypeGuid.Value );
                }

                // Save the registrations ( and launch workflows )
                var newGroupMembers = new List<GroupMember>();
                AddPersonToGroup( rockContext, person, workflowType, newGroupMembers );
                AddPersonToGroup( rockContext, spouse, workflowType, newGroupMembers );

                // Show the results
                pnlView.Visible = false;
                pnlResult.Visible = true;

                // Show lava content
                var mergeFields = new Dictionary<string, object>();
                mergeFields.Add( "PublishGroup", _publishGroup );
                mergeFields.Add( "Group", _publishGroup.Group );
                mergeFields.Add( "GroupMembers", newGroupMembers );

                string template = GetAttributeValue( "ResultLavaTemplate" );
                lResult.Text = template.ResolveMergeFields( mergeFields );

                SendConfirmation( person );
                SendConfirmation( spouse );

                // Will only redirect if a value is specifed
                NavigateToLinkedPage( "ResultPage" );
            }
        }

        private void SendConfirmation( Person person )
        {
            if ( person == null )
            {
                return;
            }

            var mergeObjects = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            mergeObjects["Person"] = person;
            mergeObjects["PublishGroup"] = _publishGroup;
            mergeObjects["Group"] = _publishGroup.Group;

            var message = new RockEmailMessage();
            message.FromEmail = _publishGroup.ConfirmationEmail;
            message.FromName = _publishGroup.ConfirmationFromName;
            message.Subject = _publishGroup.ConfirmationSubject;
            message.Message = _publishGroup.ConfirmationBody;
            message.AddRecipient( new RockEmailMessageRecipient( person, mergeObjects ) );
            message.Send();
        }

        protected void cbRegisterSpouse_CheckedChanged( object sender, EventArgs e )
        {
            tbSpouseFirstName.Visible = cbRegisterSpouse.Checked;
            tbSpouseLastName.Visible = cbRegisterSpouse.Checked;
            tbSpouseEmail.Visible = cbRegisterSpouse.Checked;
            pnSpouseCell.Visible = cbRegisterSpouse.Checked;
            cbSpouseSms.Visible = cbRegisterSpouse.Checked;
            dppSpouseDOB.Visible = cbRegisterSpouse.Checked;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Shows the details.
        /// </summary>
        private void ShowDetails()
        {
            _rockContext = _rockContext ?? new RockContext();

            if ( _publishGroup != null )
            {
                if (( _publishGroup.RegistrationRequirement != RegistrationRequirement.RegistrationAvailable &&
                     _publishGroup.RegistrationRequirement != RegistrationRequirement.RegistrationRequired )
                     || _publishGroup.StartDateTime > Rock.RockDateTime.Now
                     || _publishGroup.EndDateTime < Rock.RockDateTime.Now )
                {
                    pnlForm.Visible = false;
                }

                // Show lava content
                var mergeFields = new Dictionary<string, object>();
                mergeFields.Add( "PublishGroup", _publishGroup );
                mergeFields.Add( "Group", _publishGroup.Group );

                string template = GetAttributeValue( "LavaTemplate" );
                lLavaOverview.Text = template.ResolveMergeFields( mergeFields );

                pnlCol2.Visible = _showSpouse;

                tbEmail.Required = GetAttributeValue( REQUIRE_EMAIL_KEY ).AsBoolean();
                pnCell.Required = GetAttributeValue( REQUIRE_MOBILE_KEY ).AsBoolean();
                dppDOB.Required = GetAttributeValue( REQUIRE_DOB ).AsBoolean();
                dppSpouseDOB.Required = GetAttributeValue( REQUIRE_DOB ).AsBoolean();

                string phoneLabel = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE ).Value;
                phoneLabel = phoneLabel.Trim().EndsWith( "Phone" ) ? phoneLabel : phoneLabel + " Phone";
                pnCell.Label = phoneLabel;
                pnSpouseCell.Label = "Spouse " + phoneLabel;

                if ( CurrentPersonId.HasValue && _autoFill )
                {
                    var personService = new PersonService( _rockContext );
                    Person person = personService
                        .Queryable( "PhoneNumbers.NumberTypeValue" ).AsNoTracking()
                        .FirstOrDefault( p => p.Id == CurrentPersonId.Value );

                    tbFirstName.Text = CurrentPerson.FirstName;
                    tbLastName.Text = CurrentPerson.LastName;
                    dppDOB.SelectedDate = CurrentPerson.BirthDate;
                    tbEmail.Text = CurrentPerson.Email;


                    Guid homePhoneType = Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid();
                    var homePhone = person.PhoneNumbers
                        .FirstOrDefault( n => n.NumberTypeValue.Guid.Equals( homePhoneType ) );
                    if ( homePhone != null )
                    {
                        pnHome.Text = homePhone.Number;
                    }

                    Guid cellPhoneType = Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid();
                    var cellPhone = person.PhoneNumbers
                        .FirstOrDefault( n => n.NumberTypeValue.Guid.Equals( cellPhoneType ) );
                    if ( cellPhone != null )
                    {
                        pnCell.Text = cellPhone.Number;
                        cbSms.Checked = cellPhone.IsMessagingEnabled;
                    }

                    Location homeAddress = null;

                    var families = person.GetFamilies();

                    // If address can being entered, look for first family with a home location

                    foreach ( var aFamily in families )
                    {
                            homeAddress = aFamily.GroupLocations
                            .Where( l =>
                                l.GroupLocationTypeValueId == _homeAddressType.Id )
                            .FirstOrDefault()?.Location;
                    }

                    if ( homeAddress != null )
                    {
                        acAddress.SetValues( homeAddress );
                    }

                    if ( _showSpouse )
                    {
                        var spouse = person.GetSpouse( _rockContext );
                        if ( spouse != null )
                        {
                            tbSpouseFirstName.Text = spouse.FirstName;
                            tbSpouseLastName.Text = spouse.LastName;
                            tbSpouseEmail.Text = spouse.Email;
                            dppSpouseDOB.SelectedDate = spouse.BirthDate;

                            var spouseCellPhone = spouse.PhoneNumbers
                                .FirstOrDefault( n => n.NumberTypeValue.Guid.Equals( cellPhoneType ) );
                            if ( spouseCellPhone != null )
                            {
                                pnSpouseCell.Text = spouseCellPhone.Number;
                                cbSpouseSms.Checked = spouseCellPhone.IsMessagingEnabled;
                            }
                        }
                    }

                }

                if ( GetAttributeValue( "PreventOvercapacityRegistrations" ).AsBoolean() )
                {
                    int openGroupSpots = 2;
                    int openRoleSpots = 2;

                    // If the group has a GroupCapacity, check how far we are from hitting that.
                    if ( _publishGroup.Group.GroupCapacity.HasValue )
                    {
                        openGroupSpots = _publishGroup.Group.GroupCapacity.Value - _publishGroup.Group.ActiveMembers().Count();
                    }

                    // When someone registers for a group on the front-end website, they automatically get added with the group's default
                    // GroupTypeRole. If that role exists and has a MaxCount, check how far we are from hitting that.
                    if ( _defaultGroupRole != null && _defaultGroupRole.MaxCount.HasValue )
                    {
                        openRoleSpots = _defaultGroupRole.MaxCount.Value - _publishGroup.Group.Members
                            .Where( m => m.GroupRoleId == _defaultGroupRole.Id && m.GroupMemberStatus == GroupMemberStatus.Active )
                            .Count();
                    }

                    // Between the group's GroupCapacity and DefaultGroupRole.MaxCount, grab the one we're closest to hitting, and how close we are to
                    // hitting it.
                    int openSpots = Math.Min( openGroupSpots, openRoleSpots );

                    // If there's only one spot open, disable the spouse fields and display a warning message.
                    if ( openSpots == 1 )
                    {
                        tbSpouseFirstName.Enabled = false;
                        tbSpouseLastName.Enabled = false;
                        pnSpouseCell.Enabled = false;
                        cbSpouseSms.Enabled = false;
                        tbSpouseEmail.Enabled = false;
                        nbWarning.Text = "This group is near its capacity. Only one individual can register.";
                        nbWarning.Visible = true;
                    }

                    // If no spots are open, display a message that says so.
                    if ( openSpots <= 0 )
                    {
                        nbNotice.Text = "This group is at or exceeds capacity.";
                        nbNotice.Visible = true;
                        pnlView.Visible = false;
                    }
                }
            }
        }

        /// <summary>
        /// Adds the person to group.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="person">The person.</param>
        /// <param name="workflowType">Type of the workflow.</param>
        /// <param name="groupMembers">The group members.</param>
        private void AddPersonToGroup( RockContext rockContext, Person person, WorkflowTypeCache workflowType, List<GroupMember> groupMembers )
        {
            if ( person != null )
            {
                GroupMember groupMember = null;
                if ( !_publishGroup.Group.Members
                    .Any( m =>
                        m.PersonId == person.Id &&
                        m.GroupRoleId == _defaultGroupRole.Id ) )
                {
                    var groupMemberService = new GroupMemberService( rockContext );
                    groupMember = new GroupMember();
                    groupMember.PersonId = person.Id;
                    groupMember.GroupRoleId = _defaultGroupRole.Id;
                    groupMember.GroupMemberStatus = ( GroupMemberStatus ) GetAttributeValue( "GroupMemberStatus" ).AsInteger();
                    groupMember.GroupId = _publishGroup.Group.Id;
                    groupMemberService.Add( groupMember );
                    rockContext.SaveChanges();
                }
                else
                {
                    GroupMemberStatus status = ( GroupMemberStatus ) GetAttributeValue( "GroupMemberStatus" ).AsInteger();
                    groupMember = _publishGroup.Group.Members.Where( m =>
                       m.PersonId == person.Id &&
                       m.GroupRoleId == _defaultGroupRole.Id ).FirstOrDefault();
                    if ( groupMember.GroupMemberStatus != status )
                    {
                        var groupMemberService = new GroupMemberService( rockContext );

                        // reload this group member in the current context
                        groupMember = groupMemberService.Get( groupMember.Id );
                        groupMember.GroupMemberStatus = status;
                        rockContext.SaveChanges();
                    }

                }

                if ( groupMember != null && workflowType != null && ( workflowType.IsActive ?? true ) )
                {
                    try
                    {
                        List<string> workflowErrors;
                        var workflow = Workflow.Activate( workflowType, person.FullName );
                        new WorkflowService( rockContext ).Process( workflow, groupMember, out workflowErrors );
                    }
                    catch ( Exception ex )
                    {
                        ExceptionLogService.LogException( ex, this.Context );
                    }
                }
            }
        }

        /// <summary>
        /// Checks the settings.  If false is returned, it's expected that the caller will make
        /// the nbNotice visible to inform the user of the "settings" error.
        /// </summary>
        /// <returns>true if settings are valid; false otherwise</returns>
        private bool CheckSettings()
        {
            _rockContext = _rockContext ?? new RockContext();


            _autoFill = GetAttributeValue( "AutoFillForm" ).AsBoolean();

            string registerButtonText = GetAttributeValue( "RegisterButtonAltText" );
            if ( string.IsNullOrWhiteSpace( registerButtonText ) )
            {
                registerButtonText = "Register";
            }
            btnRegister.Text = registerButtonText;

            var publishGroupService = new PublishGroupService( _rockContext );

            if ( _publishGroup == null )
            {
                var publishGroupGuid = PageParameter( "PublishGroup" ).AsGuidOrNull();
                if ( publishGroupGuid.HasValue )
                {
                    _publishGroup = publishGroupService.Get( publishGroupGuid.Value );
                }
                else
                {
                    var slug = PageParameter( "PublishGroup" ).ToLower();
                    _publishGroup = publishGroupService.Queryable().Where( pg => pg.Slug == slug ).FirstOrDefault();
                }
            }

            if ( _publishGroup == null || _publishGroup.Group == null || _publishGroup.PublishGroupStatus != PublishGroupStatus.Approved )
            {
                nbNotice.Heading = "Unknown Group";
                nbNotice.Text = "<p>This page requires a valid group identifying parameter and there was not one provided.</p>";
                nbNotice.Visible = true;
                return false;
            }

            if ( _publishGroup.RegistrationLink.IsNotNullOrWhiteSpace() )
            {
                Response.Redirect( _publishGroup.RegistrationLink, false );
                Context.ApplicationInstance.CompleteRequest();
                return false;
            }

            _showSpouse = _publishGroup.AllowSpouseRegistration;
            _defaultGroupRole = _publishGroup.Group.GroupType.DefaultGroupRole;


            _dvcConnectionStatus = DefinedValueCache.Get( GetAttributeValue( "ConnectionStatus" ).AsGuid() );
            if ( _dvcConnectionStatus == null )
            {
                nbNotice.Heading = "Invalid Connection Status";
                nbNotice.Text = "<p>The selected Connection Status setting does not exist.</p>";
                return false;
            }

            _dvcRecordStatus = DefinedValueCache.Get( GetAttributeValue( "RecordStatus" ).AsGuid() );
            if ( _dvcRecordStatus == null )
            {
                nbNotice.Heading = "Invalid Record Status";
                nbNotice.Text = "<p>The selected Record Status setting does not exist.</p>";
                return false;
            }

            _married = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_MARITAL_STATUS_MARRIED.AsGuid() );
            _homeAddressType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() );
            _familyType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid() );
            _adultRole = _familyType.Roles.FirstOrDefault( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ) );

            if ( _married == null || _homeAddressType == null || _familyType == null || _adultRole == null )
            {
                nbNotice.Heading = "Missing System Value";
                nbNotice.Text = "<p>There is a missing or invalid system value. Check the settings for Marital Status of 'Married', Location Type of 'Home', Group Type of 'Family', and Family Group Role of 'Adult'.</p>";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sets the phone number.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="person">The person.</param>
        /// <param name="pnbNumber">The PNB number.</param>
        /// <param name="cbSms">The cb SMS.</param>
        /// <param name="phoneTypeGuid">The phone type unique identifier.</param>
        private void SetPhoneNumber( RockContext rockContext, Person person, PhoneNumberBox pnbNumber, RockCheckBox cbSms, Guid phoneTypeGuid )
        {
            var phoneType = DefinedValueCache.Get( phoneTypeGuid );
            if ( phoneType != null )
            {
                var phoneNumber = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == phoneType.Id );
                string oldPhoneNumber = string.Empty;
                if ( phoneNumber == null )
                {
                    phoneNumber = new PhoneNumber { NumberTypeValueId = phoneType.Id };
                }
                else
                {
                    oldPhoneNumber = phoneNumber.NumberFormattedWithCountryCode;
                }

                phoneNumber.CountryCode = PhoneNumber.CleanNumber( pnbNumber.CountryCode );
                phoneNumber.Number = PhoneNumber.CleanNumber( pnbNumber.Number );

                if ( string.IsNullOrWhiteSpace( phoneNumber.Number ) )
                {
                    if ( phoneNumber.Id > 0 )
                    {
                        new PhoneNumberService( rockContext ).Delete( phoneNumber );
                        person.PhoneNumbers.Remove( phoneNumber );
                    }
                }
                else
                {
                    if ( phoneNumber.Id <= 0 )
                    {
                        person.PhoneNumbers.Add( phoneNumber );
                    }
                    if ( cbSms != null && cbSms.Checked )
                    {
                        phoneNumber.IsMessagingEnabled = true;
                        person.PhoneNumbers
                            .Where( n => n.NumberTypeValueId != phoneType.Id )
                            .ToList()
                            .ForEach( n => n.IsMessagingEnabled = false );
                    }
                }
            }
        }

        #endregion


    }
}
