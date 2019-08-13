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
using System.ComponentModel;
using Rock;
using Rock.Model;
using Rock.Security;
using System.Web.UI;
using Rock.Web.Cache;
using Rock.Web.UI;
using System.Web;
using Rock.Data;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using org.secc.ChangeManager.Model;
using System.Reflection;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using Rock.Web.UI.Controls;
using org.secc.ChangeManager.Utilities;
using Rock.Attribute;

namespace RockWeb.Plugins.org_secc.ChangeManager
{
    [DisplayName( "Change Entry" )]
    [Category( "SECC > CRM" )]
    [Description( "Allows people to enter changes which can later be reviewed." )]

    [BooleanField( "Apply On Submit", "Should the changed be applied as soon as they are submitted?", true, key: "AutoApply" )]
    [WorkflowTypeField( "Workflow", "Workflow to run after a change request is made." )]
    public partial class ChangeEntry : Rock.Web.UI.RockBlock
    {
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
                    BindDropDown();
                    DisplayForm( person );
                }
            }
        }

        private void BindDropDown()
        {
            ddlTitle.BindToDefinedType( DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSON_TITLE.AsGuid() ), true );
            ddlGender.BindToEnum<Gender>( true );
            ddlMaritalStatus.BindToDefinedType( DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSON_MARITAL_STATUS.AsGuid() ), true );
        }

        private void DisplayForm( Person person )
        {
            ddlTitle.SetValue( person.TitleValueId );
            iuPhoto.BinaryFileId = person.PhotoId;
            tbNickName.Text = person.NickName;
            tbFirstName.Text = person.FirstName;
            tbLastName.Text = person.LastName;

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
                RequestorAliasId = CurrentPersonAliasId ?? 0
            };

            changeRequest.EvaluatePropertyChange( person, "PhotoId", iuPhoto.BinaryFileId );
            changeRequest.EvaluatePropertyChange( person, "TitleValue", DefinedValueCache.Get( ddlTitle.SelectedValueAsInt() ?? 0 ) );
            changeRequest.EvaluatePropertyChange( person, "FirstName", tbFirstName.Text );
            changeRequest.EvaluatePropertyChange( person, "NickName", tbNickName.Text );
            changeRequest.EvaluatePropertyChange( person, "LastName", tbLastName.Text );


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
                    if ( !string.IsNullOrWhiteSpace( PhoneNumber.CleanNumber( pnbPhone.Number ) ) )
                    {
                        int phoneNumberTypeId;
                        if ( int.TryParse( hfPhoneType.Value, out phoneNumberTypeId ) )
                        {
                            var phoneNumber = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == phoneNumberTypeId );
                            string oldPhoneNumber = string.Empty;
                            if ( phoneNumber == null )
                            {
                                phoneNumber = new PhoneNumber
                                {
                                    PersonId = person.Id,
                                    NumberTypeValueId = phoneNumberTypeId,
                                    CountryCode = PhoneNumber.CleanNumber( pnbPhone.CountryCode ),
                                    IsMessagingEnabled = !smsSelected && cbSms.Checked,
                                    Number = PhoneNumber.CleanNumber( pnbPhone.Number )
                                };

                                var phoneChange = new ChangeRecord
                                {
                                    RelatedEntityTypeId = EntityTypeCache.Get( typeof( PhoneNumber ) ).Id,
                                    RelatedEntityId = 0,
                                    OldValue = "",
                                    NewValue = phoneNumber.ToJson(),
                                };
                                changeRequest.ChangeRecords.Add( phoneChange );
                            }
                            else
                            {
                                changeRequest.EvaluatePropertyChange( phoneNumber, "Number", PhoneNumber.CleanNumber( pnbPhone.Number ), true );
                                changeRequest.EvaluatePropertyChange( phoneNumber, "IsMessagingEnabled", ( !smsSelected && cbSms.Checked ), true );
                                changeRequest.EvaluatePropertyChange( phoneNumber, "IsUnlisted", cbUnlisted.Checked, true );
                            }
                        }
                    }
                }
            }

            changeRequest.EvaluatePropertyChange( person, "Email", person.Email );
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
                    if ( insertPerson.Age.HasValue && insertPerson.Age.Value > 17 )
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

            if ( changeRequest.ChangeRecords.Any()
                || ( !familyChangeRequest.ChangeRecords.Any() && tbComments.Text.IsNotNullOrWhiteSpace() ) )
            {
                changeRequest.RequestorComment = tbComments.Text;
                ChangeRequestService changeRequestService = new ChangeRequestService( rockContext );
                changeRequestService.Add( changeRequest );
                rockContext.SaveChanges();
                if ( GetAttributeValue( "AutoApply" ).AsBoolean() )
                {
                    changeRequest.CompleteChanges( rockContext );
                }

                changeRequest.LaunchWorkflow( GetAttributeValue( "Workflow" ).AsGuidOrNull() );
            }

            if ( familyChangeRequest.ChangeRecords.Any() )
            {
                familyChangeRequest.RequestorComment = tbComments.Text;
                ChangeRequestService changeRequestService = new ChangeRequestService( rockContext );
                changeRequestService.Add( familyChangeRequest );
                rockContext.SaveChanges();
                if ( GetAttributeValue( "AutoApply" ).AsBoolean() )
                {
                    familyChangeRequest.CompleteChanges( rockContext );
                }
                familyChangeRequest.LaunchWorkflow( GetAttributeValue( "Workflow" ).AsGuidOrNull() );
            }



            if ( GetAttributeValue( "AutoApply" ).AsBoolean() )
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
    }
}