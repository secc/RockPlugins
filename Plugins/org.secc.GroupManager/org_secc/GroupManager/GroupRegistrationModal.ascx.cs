// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
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
using Rock;
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.GroupManager
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Group Registration Modal" )]
    [Category( "Groups" )]
    [Description( "Allows a person to register for a group." )]

    [CustomRadioListField( "Group Member Status", "The group member status to use when adding person to group (default: 'Pending'.)", "2^Pending,1^Active,0^Inactive", true, "2", "", 1 )]
    [DefinedValueField( "2E6540EA-63F0-40FE-BE50-F2A84735E600", "Connection Status", "The connection status to use for new individuals (default: 'Web Prospect'.)", true, false, "368DD475-242C-49C4-A42C-7278BE690CC2", "", 2 )]
    [DefinedValueField( "8522BADD-2871-45A5-81DD-C76DA07E2E7E", "Record Status", "The record status to use for new individuals (default: 'Pending'.)", true, false, "283999EC-7346-42E3-B807-BCE9B2BABB49", "", 3 )]

    [BooleanField( "Large Button", "Show large button with text?" )]
    public partial class GroupRegistrationModal : RockBlock
    {
        #region Fields

        RockContext _rockContext = null;
        Group _group = null;
        GroupTypeRole _defaultGroupRole = null;
        DefinedValueCache _dvcConnectionStatus = null;
        DefinedValueCache _dvcRecordStatus = null;
        DefinedValueCache _homeAddressType = null;
        GroupTypeCache _familyType = null;
        GroupTypeRoleCache _adultRole = null;

        #endregion


        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            CheckSettings();
        }

        #endregion

        #region Events

        protected void btnLaunchModal_Click( object sender, EventArgs e )
        {
            mdDialog.Show();
        }

        /// <summary>
        /// Handles the Click event of the btnRegister control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnRegister_Click( object sender, EventArgs e )
        {
            if ( Page.IsValid )
            {
                if ( _rockContext == null )
                {
                    _rockContext = new RockContext();
                }
                var personService = new PersonService( _rockContext );

                Person person = null;

                var changes = new List<string>();
                var spouseChanges = new List<string>();
                var familyChanges = new List<string>();

                // Try to find person by name/email 
                if ( person == null )
                {
                    var matches = GetByMatch( tbFirstName.Text.Trim(), tbLastName.Text.Trim(), dpBirthday.SelectedDate, pnCell.Text.Trim(), tbEmail.Text.Trim() );
                    if ( matches.Count() == 1 )
                    {
                        person = matches.First();
                    }
                }
                // Check to see if this is a new person
                if ( person == null )
                {
                    // If so, create the person and family record for the new person
                    person = new Person();
                    person.FirstName = tbFirstName.Text.Trim();
                    person.LastName = tbLastName.Text.Trim();
                    person.SetBirthDate( dpBirthday.SelectedDate );
                    person.UpdatePhoneNumber( DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).Id,
                        PhoneNumber.DefaultCountryCode(), pnCell.Text, true, false, _rockContext );
                    person.Email = tbEmail.Text.Trim();
                    person.IsEmailActive = true;
                    person.EmailPreference = EmailPreference.EmailAllowed;
                    person.RecordTypeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                    person.ConnectionStatusValueId = _dvcConnectionStatus.Id;
                    person.RecordStatusValueId = _dvcRecordStatus.Id;
                    person.Gender = Gender.Unknown;

                    PersonService.SaveNewPerson( person, _rockContext, _group.CampusId, false );
                }

                // Save the registration
                AddPersonToGroup( _rockContext, person );
                pnlForm.Visible = false;
                pnlResults.Visible = true;
                ltResults.Text = person.FullName + " has been added to your group.";
                ClearForm();
            }
        }

        

        protected void btnAddAnother_Click( object sender, EventArgs e )
        {
            pnlForm.Visible = true;
            pnlResults.Visible = false;
        }
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            ClearForm();
            mdDialog.Hide();
        }

        protected void btnClose_Click( object sender, EventArgs e )
        {
            mdDialog.Hide();
        }

        #endregion

        #region Internal Methods

        private void ClearForm()
        {
            tbFirstName.Text = "";
            tbLastName.Text = "";
            dpBirthday.Text = "";
            pnCell.Text = "";
            tbEmail.Text = "";
        }

        private List<Person> GetByMatch( string firstName, string lastName, DateTime? birthday, string cellPhone, string email )
        {
            firstName = firstName ?? string.Empty;
            lastName = lastName ?? string.Empty;
            cellPhone = PhoneNumber.CleanNumber( cellPhone ) ?? string.Empty;
            email = email ?? string.Empty;

            //Search for person who matches first and last name and one of email, phone number, or birthday
            return new PersonService( _rockContext ).Queryable()
                         .Where( p => p.LastName == lastName
                                 && ( p.FirstName == firstName || p.NickName == firstName )
                                 && ( p.Email == email || p.PhoneNumbers.Where( pn => pn.Number == cellPhone ).Any() || ( birthday != null && p.BirthDate == birthday ) ) )
                                 .ToList();
        }

        /// <summary>
        /// Adds the person to group.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="person">The person.</param>
        /// <param name="workflowType">Type of the workflow.</param>
        /// <param name="groupMembers">The group members.</param>
        private void AddPersonToGroup( RockContext rockContext, Person person )
        {
            if ( person != null )
            {
                if ( !_group.Members
                    .Any( m =>
                        m.PersonId == person.Id &&
                        m.GroupRoleId == _defaultGroupRole.Id ) )
                {
                    var groupMemberService = new GroupMemberService( rockContext );
                    var groupMember = new GroupMember();
                    groupMember.PersonId = person.Id;
                    groupMember.GroupRoleId = _defaultGroupRole.Id;
                    groupMember.GroupMemberStatus = ( GroupMemberStatus ) GetAttributeValue( "GroupMemberStatus" ).AsInteger();
                    groupMember.GroupId = _group.Id;
                    groupMemberService.Add( groupMember );
                    rockContext.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Checks the settings.
        /// </summary>
        /// <returns></returns>
        private bool CheckSettings()
        {
            _rockContext = _rockContext ?? new RockContext();

            int groupId = PageParameter( "GroupId" ).AsInteger();
            _group = new GroupService( _rockContext )
                .Queryable( "GroupType.DefaultGroupRole" ).AsNoTracking()
                .FirstOrDefault( g => g.Id == groupId );
            if ( _group == null )
            {
                return false;
            }
            else
            {
                _defaultGroupRole = _group.GroupType.DefaultGroupRole;
            }

            _dvcConnectionStatus = DefinedValueCache.Read( GetAttributeValue( "ConnectionStatus" ).AsGuid() );
            if ( _dvcConnectionStatus == null )
            {
                return false;
            }

            _dvcRecordStatus = DefinedValueCache.Read( GetAttributeValue( "RecordStatus" ).AsGuid() );
            if ( _dvcRecordStatus == null )
            {
                return false;
            }


            _homeAddressType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() );
            _familyType = GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid() );
            _adultRole = _familyType.Roles.FirstOrDefault( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ) );

            if ( !GetAttributeValue( "LargeButton" ).AsBoolean() )
            {
                btnLaunchModal.Text = "<i class='fa fa-user-plus'></i>";
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
        /// <param name="changes">The changes.</param>
        private void SetPhoneNumber( RockContext rockContext, Person person, PhoneNumberBox pnbNumber, RockCheckBox cbSms, Guid phoneTypeGuid, List<string> changes )
        {
            var phoneType = DefinedValueCache.Read( phoneTypeGuid );
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

                History.EvaluateChange( changes,
                    string.Format( "{0} Phone", phoneType.Value ),
                    oldPhoneNumber, phoneNumber.NumberFormattedWithCountryCode );
            }
        }

        #endregion
    }
}