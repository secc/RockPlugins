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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using org.secc.ChangeManager.Model;
using org.secc.ChangeManager.Utilities;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.ChangeManager
{
    [DisplayName( "Change Entry" )]
    [Category( "SECC > CRM" )]
    [Description( "Allows people to enter changes which can later be reviewed." )]

    [BooleanField( "Apply On Submit", "Should the changed be applied as soon as they are submitted? Ignored if there is an Approved Updaters Data View.", true, key: "AutoApply", order: 0 )]
    [DataViewField( "Approved Updaters Data View",
        "Data View of people who's changes are automatically applied.",
        false, key: "ApprovedDataView", order: 1 )]
    [DataViewField( "Blacklist Data View",
        "Data View of people who should never have their data automatically updated such as staff members, VIPs or other people you wish to have reviewed before updating.",
        false, key: "BlacklistDataView", order: 2 )]
    [WorkflowTypeField( "Workflow", "Workflow to run after a change request is made.", order: 3 )]
    [BooleanField( "Simple Mode",
        Description = "Shows only a box for requests.",
        DefaultBooleanValue = false,
        Order = 4,
        Key = AttributeKey.SimpleMode
        )]

    public partial class ChangeEntry : Rock.Web.UI.RockBlock
    {
        private static class AttributeKey
        {
            public const string AutoApply = "AutoApply";
            public const string ApprovedDataView = "ApprovedDataView";
            public const string BlacklistDataView = "BlacklistDataView";
            public const string Workflow = "Workflow";
            public const string SimpleMode = "SimpleMode";
        }


        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                Person person = GetPerson();
                if ( person == null )
                {
                    pnlMain.Visible = false;
                    pnlNoPerson.Visible = true;
                }
                else
                {
                    if ( GetAttributeValue( AttributeKey.SimpleMode ).AsBoolean() )
                    {
                        DisplaySimple( person );
                    }
                    else
                    {
                        BindDropDown();
                        DisplayForm( person );
                    }

                }
            }
        }

        private void DisplaySimple( Person person )
        {
            pnlSimple.Visible = true;
            ltPersonName.Text = person.FullName;
        }

        private void BindDropDown()
        {
            ddlTitle.DefinedTypeId = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSON_TITLE.AsGuid() ).Id;
            ddlTitle.Items.Insert( 0, new ListItem() );

            ddlSuffix.DefinedTypeId = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSON_SUFFIX.AsGuid() ).Id;
            ddlSuffix.Items.Insert( 0, new ListItem() );

            ddlGender.BindToEnum<Gender>( true );
            ddlMaritalStatus.DefinedTypeId = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSON_MARITAL_STATUS.AsGuid() ).Id;
            ddlMaritalStatus.Items.Insert( 0, new ListItem() );

        }

        private void DisplayForm( Person person )
        {
            pnlMain.Visible = true;

            ddlTitle.SetValue( person.TitleValueId );
            iuPhoto.BinaryFileId = person.PhotoId;
            tbNickName.Text = person.NickName;
            tbFirstName.Text = person.FirstName;
            tbMiddleName.Text = person.MiddleName;
            tbLastName.Text = person.LastName;
            ddlSuffix.SetValue( person.SuffixValueId );

            var families = person.GetFamilies();

            //If there is more than one family don't show family role
            if ( families.Count() > 1 )
            {
                ddlFamilyRole.Visible = false;
            }
            else
            {
                ddlFamilyRole.SelectedValue = person.GetFamilyRole().Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ? "A" : "C";

                var familyMemberRole = person.GetFamilyMembers( true )
                    .Where( m => person.Id == m.PersonId )
                    .Select( m => m.GroupRole )
                    .FirstOrDefault();

                if ( familyMemberRole != null )
                {
                    ddlFamilyRole.SelectedValue = familyMemberRole.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ? "A" : "C";
                }
            }


            //PhoneNumber
            var mobilePhoneType = DefinedValueCache.Get( new Guid( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE ) );

            var phoneNumbers = new List<PhoneNumber>();
            var phoneNumberTypes = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE ) );
            if ( phoneNumberTypes.DefinedValues.Any() )
            {
                foreach ( var phoneNumberType in phoneNumberTypes.DefinedValues )
                {
                    var phoneNumber = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == phoneNumberType.Id );
                    if ( phoneNumber == null )
                    {
                        var numberType = new DefinedValue();
                        numberType.Id = phoneNumberType.Id;
                        numberType.Value = phoneNumberType.Value;

                        phoneNumber = new PhoneNumber { NumberTypeValueId = numberType.Id, NumberTypeValue = numberType };
                        phoneNumber.IsMessagingEnabled = mobilePhoneType != null && phoneNumberType.Id == mobilePhoneType.Id;
                    }
                    else
                    {
                        // Update number format, just in case it wasn't saved correctly
                        phoneNumber.NumberFormatted = PhoneNumber.FormattedNumber( phoneNumber.CountryCode, phoneNumber.Number );
                    }

                    phoneNumbers.Add( phoneNumber );
                }
            }
            rContactInfo.DataSource = phoneNumbers;
            rContactInfo.DataBind();


            //email
            tbEmail.Text = person.Email;
            cbIsEmailActive.Checked = person.IsEmailActive;

            rblEmailPreference.SetValue( person.EmailPreference.ConvertToString( false ) );
            rblCommunicationPreference.SetValue( person.CommunicationPreference == CommunicationType.SMS ? "2" : "1" );

            //demographics
            bpBirthday.SelectedDate = person.BirthDate;
            ddlGender.SetValue( person.Gender.ConvertToInt() );
            ddlMaritalStatus.SetValue( person.MaritalStatusValueId );
            dpAnniversaryDate.SelectedDate = person.AnniversaryDate;

            if ( !person.HasGraduated ?? false )
            {
                int gradeOffset = person.GradeOffset.Value;
                var maxGradeOffset = ddlGradePicker.MaxGradeOffset;

                // keep trying until we find a Grade that has a gradeOffset that that includes the Person's gradeOffset (for example, there might be combined grades)
                while ( !ddlGradePicker.Items.OfType<ListItem>().Any( a => a.Value.AsInteger() == gradeOffset ) && gradeOffset <= maxGradeOffset )
                {
                    gradeOffset++;
                }

                ddlGradePicker.SetValue( gradeOffset );
                ypGraduation.SelectedYear = person.GraduationYear;
            }
            else
            {
                ddlGradePicker.SelectedIndex = 0;
                ypGraduation.SelectedYear = person.GraduationYear;
            }
            ScriptManager.RegisterStartupScript( ddlGradePicker, ddlGradePicker.GetType(), "grade-selection-" + BlockId.ToString(), ddlGradePicker.GetJavascriptForYearPicker( ypGraduation ), true );

            //Family Info
            var location = person.GetHomeLocation();
            acAddress.SetValues( location );
            ddlCampus.SetValue( person.GetCampus() );

        }

        private Person GetPerson()
        {
            var personId = hfPersonId.ValueAsInt();
            if ( personId == 0 )
            {
                personId = PageParameter( "PersonId" ).AsInteger();
                hfPersonId.SetValue( personId );
            }
            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );
            return personService.Get( personId );
        }

        protected void btnSave_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            var person = GetPerson();
            var personAliasEntityType = EntityTypeCache.Get( typeof( PersonAlias ) );
            var changeRequest = new ChangeRequest
            {
                EntityTypeId = personAliasEntityType.Id,
                EntityId = person.PrimaryAliasId ?? 0,
                RequestorAliasId = CurrentPersonAliasId ?? 0,
                RequestorComment = tbComments.Text
            };

            changeRequest.EvaluatePropertyChange( person, "PhotoId", iuPhoto.BinaryFileId );
            changeRequest.EvaluatePropertyChange( person, "TitleValue", DefinedValueCache.Get( ddlTitle.SelectedValueAsInt() ?? 0 ) );
            changeRequest.EvaluatePropertyChange( person, "FirstName", tbFirstName.Text );
            changeRequest.EvaluatePropertyChange( person, "NickName", tbNickName.Text );
            changeRequest.EvaluatePropertyChange( person, "MiddleName", tbMiddleName.Text );
            changeRequest.EvaluatePropertyChange( person, "LastName", tbLastName.Text );
            changeRequest.EvaluatePropertyChange( person, "SuffixValue", DefinedValueCache.Get( ddlSuffix.SelectedValueAsInt() ?? 0 ) );

            var families = person.GetFamilies();

            if ( families.Count() == 1 )
            {
                var groupMember = person.PrimaryFamily.Members.Where( gm => gm.PersonId == person.Id ).FirstOrDefault();
                if ( groupMember != null )
                {
                    GroupTypeRole groupTypeRole = null;
                    GroupTypeRoleService groupTypeRoleService = new GroupTypeRoleService( rockContext );
                    if ( ddlFamilyRole.SelectedValue == "A" )
                    {
                        groupTypeRole = groupTypeRoleService.Get( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() );
                    }
                    else if ( ddlFamilyRole.SelectedValue == "C" )
                    {
                        groupTypeRole = groupTypeRoleService.Get( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() );
                    }
                    changeRequest.EvaluatePropertyChange( groupMember, "GroupRole", groupTypeRole, true );
                }
            }

            //Evaluate PhoneNumbers
            var phoneNumberTypeIds = new List<int>();
            bool smsSelected = false;
            foreach ( RepeaterItem item in rContactInfo.Items )
            {
                HiddenField hfPhoneType = item.FindControl( "hfPhoneType" ) as HiddenField;
                PhoneNumberBox pnbPhone = item.FindControl( "pnbPhone" ) as PhoneNumberBox;
                CheckBox cbUnlisted = item.FindControl( "cbUnlisted" ) as CheckBox;
                CheckBox cbSms = item.FindControl( "cbSms" ) as CheckBox;

                if ( hfPhoneType != null &&
                    pnbPhone != null &&
                    cbSms != null &&
                    cbUnlisted != null )
                {

                    int phoneNumberTypeId;
                    if ( int.TryParse( hfPhoneType.Value, out phoneNumberTypeId ) )
                    {
                        var phoneNumber = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == phoneNumberTypeId );
                        string oldPhoneNumber = string.Empty;
                        if ( phoneNumber == null && pnbPhone.Number.IsNotNullOrWhiteSpace() ) //Add number
                        {
                            phoneNumber = new PhoneNumber
                            {
                                PersonId = person.Id,
                                NumberTypeValueId = phoneNumberTypeId,
                                CountryCode = PhoneNumber.CleanNumber( pnbPhone.CountryCode ),
                                IsMessagingEnabled = !smsSelected && cbSms.Checked,
                                Number = PhoneNumber.CleanNumber( pnbPhone.Number )
                            };
                            var phoneComment = string.Format( "{0}: {1}.", DefinedValueCache.Get( phoneNumberTypeId ).Value, pnbPhone.Number );
                            changeRequest.AddEntity( phoneNumber, rockContext, true, phoneComment );
                        }
                        else if ( phoneNumber != null && pnbPhone.Text.IsNullOrWhiteSpace() ) // delete number
                        {
                            var phoneComment = string.Format( "{0}: {1}.", phoneNumber.NumberTypeValue.Value, phoneNumber.NumberFormatted );
                            changeRequest.DeleteEntity( phoneNumber, true, phoneComment );
                        }
                        else if ( phoneNumber != null && pnbPhone.Text.IsNotNullOrWhiteSpace() ) // update number
                        {
                            changeRequest.EvaluatePropertyChange( phoneNumber, "Number", PhoneNumber.CleanNumber( pnbPhone.Number ), true );
                            changeRequest.EvaluatePropertyChange( phoneNumber, "IsMessagingEnabled", ( !smsSelected && cbSms.Checked ), true );
                            changeRequest.EvaluatePropertyChange( phoneNumber, "IsUnlisted", cbUnlisted.Checked, true );
                        }

                        if ( hfPhoneType.Value.AsInteger() == DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ) )
                        {
                            var validationInfo = ValidateMobilePhoneNumber( PhoneNumber.CleanNumber( pnbPhone.Number ) );
                            if ( validationInfo.IsNotNullOrWhiteSpace() )
                            {
                                changeRequest.RequestorComment += "<h4>Dynamically Generated Warnings:</h4>" + validationInfo;
                            }
                        }
                    }

                }
            }

            changeRequest.EvaluatePropertyChange( person, "Email", tbEmail.Text );
            changeRequest.EvaluatePropertyChange( person, "IsEmailActive", cbIsEmailActive.Checked );
            changeRequest.EvaluatePropertyChange( person, "EmailPreference", rblEmailPreference.SelectedValueAsEnum<EmailPreference>() );
            changeRequest.EvaluatePropertyChange( person, "CommunicationPreference", rblCommunicationPreference.SelectedValueAsEnum<CommunicationType>() );


            var birthday = bpBirthday.SelectedDate;
            if ( birthday.HasValue )
            {
                changeRequest.EvaluatePropertyChange( person, "BirthMonth", birthday.Value.Month );
                changeRequest.EvaluatePropertyChange( person, "BirthDay", birthday.Value.Day );
                if ( birthday.Value.Year != DateTime.MinValue.Year )
                {
                    changeRequest.EvaluatePropertyChange( person, "BirthYear", birthday.Value.Year );
                }
                else
                {
                    int? year = null;
                    changeRequest.EvaluatePropertyChange( person, "BirthYear", year );
                }
            }

            changeRequest.EvaluatePropertyChange( person, "Gender", ddlGender.SelectedValueAsEnum<Gender>() );
            changeRequest.EvaluatePropertyChange( person, "MaritalStatusValue", DefinedValueCache.Get( ddlMaritalStatus.SelectedValueAsInt() ?? 0 ) );
            changeRequest.EvaluatePropertyChange( person, "AnniversaryDate", dpAnniversaryDate.SelectedDate );
            changeRequest.EvaluatePropertyChange( person, "GraduationYear", ypGraduation.SelectedYear );

            var groupEntity = EntityTypeCache.Get( typeof( Group ) );
            var groupLocationEntity = EntityTypeCache.Get( typeof( GroupLocation ) );
            var family = person.GetFamily();

            var familyChangeRequest = new ChangeRequest()
            {
                EntityTypeId = groupEntity.Id,
                EntityId = family.Id,
                RequestorAliasId = CurrentPersonAliasId ?? 0
            };

            familyChangeRequest.EvaluatePropertyChange( family, "Campus", CampusCache.Get( ddlCampus.SelectedValueAsInt() ?? 0 ) );

            var currentLocation = person.GetHomeLocation();
            Location location = new Location
            {
                Street1 = acAddress.Street1,
                Street2 = acAddress.Street2,
                City = acAddress.City,
                State = acAddress.State,
                PostalCode = acAddress.PostalCode,
            };
            var globalAttributesCache = GlobalAttributesCache.Get();
            location.Country = globalAttributesCache.OrganizationCountry;
            location.Country = string.IsNullOrWhiteSpace( location.Country ) ? "US" : location.Country;

            if ( ( currentLocation == null && location.Street1.IsNotNullOrWhiteSpace() ) ||
                ( currentLocation != null && currentLocation.Street1 != location.Street1 ) )
            {
                LocationService locationService = new LocationService( rockContext );
                locationService.Add( location );
                rockContext.SaveChanges();

                var previousLocationType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_PREVIOUS.AsGuid() );
                var homeLocationType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() );

                GroupLocation groupLocation = new GroupLocation
                {
                    CreatedByPersonAliasId = CurrentPersonAliasId,
                    ModifiedByPersonAliasId = CurrentPersonAliasId,
                    GroupId = family.Id,
                    LocationId = location.Id,
                    GroupLocationTypeValueId = homeLocationType.Id,
                    IsMailingLocation = true,
                    IsMappedLocation = true
                };

                var newGroupLocation = familyChangeRequest.AddEntity( groupLocation, rockContext, true, location.ToString() );

                var homelocations = family.GroupLocations.Where( gl => gl.GroupLocationTypeValueId == homeLocationType.Id );
                foreach ( var homelocation in homelocations )
                {
                    familyChangeRequest.EvaluatePropertyChange(
                        homelocation,
                        "GroupLocationTypeValue",
                        previousLocationType,
                        true,
                        homelocation.Location.ToString() );

                    familyChangeRequest.EvaluatePropertyChange(
                        homelocation,
                        "IsMailingLocation",
                        false,
                        true,
                        homelocation.Location.ToString() );
                }
            }

            //Adding a new family member
            if ( pAddPerson.SelectedValue.HasValue )
            {
                PersonService personService = new PersonService( rockContext );
                var insertPerson = personService.Get( pAddPerson.SelectedValue.Value );
                if ( insertPerson != null )
                {
                    GroupMemberService groupMemberService = new GroupMemberService( rockContext );
                    var familyGroupTypeId = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid() ).Id;

                    //Remove all other group members
                    if ( cbRemovePerson.Checked )
                    {
                        var members = groupMemberService.Queryable()
                            .Where( m => m.PersonId == pAddPerson.SelectedValue.Value && m.Group.GroupTypeId == familyGroupTypeId );
                        foreach ( var member in members )
                        {
                            var comment = string.Format( "Removed {0} from {1}", insertPerson.FullName, member.Group.Name );
                            familyChangeRequest.DeleteEntity( member, true, comment );
                        }
                    }

                    var personFamilies = person.GetFamilies().ToList();

                    GroupTypeRoleService groupTypeRoleService = new GroupTypeRoleService( rockContext );

                    var roleId = groupTypeRoleService.Get( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ).Id;
                    if ( insertPerson.Age.HasValue && insertPerson.Age.Value < 18 )
                    {
                        roleId = groupTypeRoleService.Get( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() ).Id;
                    }

                    foreach ( var personFamily in personFamilies )
                    {
                        //Make a new group member
                        GroupMember familyMember = new GroupMember
                        {
                            PersonId = pAddPerson.SelectedValue.Value,
                            GroupId = personFamily.Id,
                            GroupMemberStatus = GroupMemberStatus.Active,
                            Guid = Guid.NewGuid(),
                            GroupRoleId = roleId
                        };
                        var insertComment = string.Format( "Added {0} to {1}", insertPerson.FullName, personFamily.Name );
                        familyChangeRequest.AddEntity( familyMember, rockContext, true, insertComment );
                    }
                }

            }

            bool autoApply = CanAutoApply( person );
            List<string> errors;

            if ( changeRequest.ChangeRecords.Any()
            || ( !familyChangeRequest.ChangeRecords.Any() && tbComments.Text.IsNotNullOrWhiteSpace() ) )
            {

                ChangeRequestService changeRequestService = new ChangeRequestService( rockContext );
                changeRequestService.Add( changeRequest );
                rockContext.SaveChanges();
                if ( autoApply )
                {
                    changeRequest.CompleteChanges( rockContext, out errors );
                }

                changeRequest.LaunchWorkflow( GetAttributeValue( "Workflow" ).AsGuidOrNull() );
            }

            if ( familyChangeRequest.ChangeRecords.Any() )
            {
                familyChangeRequest.RequestorComment = tbComments.Text;
                ChangeRequestService changeRequestService = new ChangeRequestService( rockContext );
                changeRequestService.Add( familyChangeRequest );
                rockContext.SaveChanges();
                if ( autoApply )
                {
                    familyChangeRequest.CompleteChanges( rockContext, out errors );
                }
                familyChangeRequest.LaunchWorkflow( GetAttributeValue( "Workflow" ).AsGuidOrNull() );
            }

            if ( autoApply )
            {
                NavigateToPerson();
            }
            else
            {
                pnlMain.Visible = false;
                pnlNoPerson.Visible = false;
                pnlDone.Visible = true;
            }
        }

        private bool CanAutoApply( Person person )
        {
            var userCanApply = false;
            var approvedDataview = GetAttributeValue( "ApprovedDataView" ).AsGuidOrNull();

            RockContext rockContext = new RockContext();
            if ( approvedDataview.HasValue )
            {
                DataViewService dataViewService = new DataViewService( rockContext );
                var dv = dataViewService.Get( approvedDataview.Value );
                if ( dv != null )
                {
                    List<string> errorMessages;
                    var qry = ( IQueryable<Person> ) dv.GetQuery( null, 30, out errorMessages );
                    if ( qry.Where( p => p.Id == CurrentPersonId ).Any() )
                    {
                        userCanApply = true;
                    }
                }
            }
            else
            {
                userCanApply = GetAttributeValue( "AutoApply" ).AsBoolean();
            }

            if ( !userCanApply )
            {
                return false;
            }

            var blackListDV = GetAttributeValue( "BlacklistDataView" ).AsGuidOrNull();
            if ( blackListDV.HasValue )
            {
                DataViewService dataViewService = new DataViewService( rockContext );
                var dv = dataViewService.Get( blackListDV.Value );
                if ( dv != null )
                {
                    List<string> errorMessages;
                    var qry = ( IQueryable<Person> ) dv.GetQuery( null, 30, out errorMessages );
                    if ( qry.Where( p => p.Id == person.Id ).Any() )
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        protected void pPerson_SelectPerson( object sender, EventArgs e )
        {
            var personId = pPerson.SelectedValue;
            if ( personId.HasValue )
            {
                NavigateToCurrentPage( new Dictionary<string, string> { { "PersonId", personId.Value.ToString() } } );
            }
        }

        protected void btnDone_Click( object sender, EventArgs e )
        {
            NavigateToPerson();
        }

        private void NavigateToPerson()
        {
            Response.Redirect( "/Person/" + GetPerson().Id.ToString() );
        }

        protected void pnbPhone_TextChanged( object sender, EventArgs e )
        {

            var tbPhone = ( PhoneNumberBox ) sender;
            var hfPhoneType = ( HiddenField ) tbPhone.Parent.FindControl( "hfPhoneType" );
            var mobileDV = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
            if ( hfPhoneType.Value.AsInteger() == mobileDV.Id )
            {

                var number = PhoneNumber.CleanNumber( tbPhone.Text );
                string validationInformation = ValidateMobilePhoneNumber( number );

                if ( validationInformation.IsNotNullOrWhiteSpace() )
                {
                    maNotice.Show( validationInformation, ModalAlertType.Warning );
                }
            }
        }

        private string ValidateMobilePhoneNumber( string number )
        {
            var person = GetPerson();
            RockContext rockContext = new RockContext();
            PhoneNumberService phoneNumberService = new PhoneNumberService( rockContext );

            var otherOwners = phoneNumberService.Queryable()
                .Where( pn => pn.Number == number )
                .Select( pn => pn.Person )
                .Where( p => p.Id != person.Id )
                .ToList();

            if ( otherOwners.Any() )
            {
                var notice = string.Format(
                    "The phone number {0} is on the following records." +
                    "<ul>{1}</ul>" +
                    "Mobile phone numbers should exist on one record only. Please ensure that {0} belongs to {2} and remove this number from all other users.",
                    PhoneNumber.FormattedNumber( PhoneNumber.DefaultCountryCode(), number ),
                    string.Join( "", otherOwners.Select( p => "<li><a href='/Person/" + p.Id.ToString() + "' target='_blank'>" + p.FullName + "</a></li>" ) ),
                    person.FullName
                    );
                return notice;
            }
            return string.Empty;
        }

        protected void lbSimpleSave_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            var person = GetPerson();
            var personAliasEntityType = EntityTypeCache.Get( typeof( PersonAlias ) );
            var changeRequest = new ChangeRequest
            {
                EntityTypeId = personAliasEntityType.Id,
                EntityId = person.PrimaryAliasId ?? 0,
                RequestorAliasId = CurrentPersonAliasId ?? 0,
                RequestorComment = tbSimpleRequest.Text
            };

            ChangeRequestService changeRequestService = new ChangeRequestService( rockContext );
            changeRequestService.Add( changeRequest );
            rockContext.SaveChanges();

            pnlSimple.Visible = false;
            pnlDone.Visible = true;
            btnDone.Visible = false;
        }
    }
}