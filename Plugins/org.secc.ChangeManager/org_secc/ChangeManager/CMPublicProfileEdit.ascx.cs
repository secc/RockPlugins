// <copyright>
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
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using org.secc.ChangeManager.Model;
using org.secc.ChangeManager.Utilities;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.ChangeManager
{
    /// <summary>
    /// The main Person Profile block the main information about a person 
    /// </summary>
    [DisplayName( "Change Managed Public Profile Edit" )]
    [Category( "SECC > CRM" )]
    [Description( "Public block for users to manage their accounts using the Change Manager library." )]

    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Default Connection Status", "The connection status that should be set by default", false, false, "", "", order: 0 )]
    [BooleanField( "Disable Name Edit", "Whether the First and Last Names can be edited.", false, order: 1 )]
    [GroupLocationTypeField( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY, "Address Type", "The type of address to be displayed / edited.", false, Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME, "", order: 4 )]
    [BooleanField( "Show Phone Numbers", "Allows hiding the phone numbers.", false, order: 5 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE, "Phone Types", "The types of phone numbers to display / edit.", false, true, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME, order: 6 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE, "Required Adult Phone Types", "The phone numbers that are required when editing an adult record.", false, true, order: 7 )]
    [BooleanField( "Require Adult Email Address", "Require an email address on adult records", true, order: 8 )]
    [BooleanField( "Show Communication Preference", "Show the communication preference and allow it to be edited", true, order: 9 )]

    [BooleanField(
        "Display Terms of Service",
        Description = "Should a checkbox agreeing to the terms service be required?",
        Key = AttributeKeys.DisplayTerms,
        Order = 10 )]

    [CodeEditorField(
        "Terms of Service Text",
        Description = "The text to be displayed next to the agree to the terms of service link.",
        Key = AttributeKeys.TermsOfServiceText,
        DefaultValue = "I agree to the terms of service.",
        IsRequired = false,
        Order = 11 )]

    [BooleanField( "Show Campus Selector", "Allows selection of primary campus.", false, order: 12 )]
    [AttributeField(
        "Person Attributes (adults)",
        Key = AttributeKeys.PersonAttributesAdult,
        EntityTypeGuid = Rock.SystemGuid.EntityType.PERSON,
        Description = "The person attributes that should be displayed / edited for adults.",
        IsRequired = false,
        AllowMultiple = true,
        Order = 13)]

    [AttributeField(
        "Person Attributes (children)",
        Key = AttributeKeys.PersonAttributesChild,
        EntityTypeGuid = Rock.SystemGuid.EntityType.PERSON,
        Description = "The person attributes that should be displayed / edited for children.",
        IsRequired = false,
        AllowMultiple = true,
        Order = 14)]


    public partial class CMPublicProfileEdit : RockBlock
    {
        #region Keys
        protected static class AttributeKeys
        {
            internal const string DisplayTerms = "DisplayTerms";
            internal const string TermsOfServiceText = "TermsOfServiceText";
            internal const string PersonAttributesAdult = "PersonAttributesAdult";
            internal const string PersonAttributesChild = "PersonAttributesChild";
        }

        protected static class PageParameterKeys
        {
            internal const string PersonGuid = "PersonGuid";
        }


        #endregion

        #region Fields

        private List<Guid> _RequiredPhoneNumberGuids = new List<Guid>();
        private bool _IsEditRecordAdult = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the Role Type. Used to help in loading Attribute panel
        /// </summary>
        protected int? RoleType
        {
            get { return ViewState["RoleType"] as int? ?? null; }
            set { ViewState["RoleType"] = value; }
        }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            ScriptManager.RegisterStartupScript( ddlGradePicker, ddlGradePicker.GetType(), "grade-selection-" + BlockId.ToString(), ddlGradePicker.GetJavascriptForYearPicker( ypGraduation ), true );
            ddlTitle.BindToDefinedType( DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_TITLE ) ), true );
            ddlSuffix.BindToDefinedType( DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_SUFFIX ) ), true );
            RockPage.AddCSSLink( ResolveRockUrl( "~/Styles/fluidbox.css" ) );
            RockPage.AddScriptLink( ResolveRockUrl( "~/Scripts/imagesloaded.min.js" ) );
            RockPage.AddScriptLink( ResolveRockUrl( "~/Scripts/jquery.fluidbox.min.js" ) );

            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "RequiredAdultPhoneTypes" ) ) )
            {
                _RequiredPhoneNumberGuids = GetAttributeValue( "RequiredAdultPhoneTypes" ).Split( ',' ).Select( Guid.Parse ).ToList();
            }
            rContactInfo.ItemDataBound += rContactInfo_ItemDataBound;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            if ( !Page.IsPostBack )
            {
                var person = GetPerson();

                if ( CurrentPerson != null
                    && CurrentPerson.AgeClassification != AgeClassification.Child
                    && person != null )
                {
                    if ( person.Id == 0
                        || CurrentPerson.GetFamilyMembers( true ).Select( m => m.Person ).Where( p => p.Id == person.Id ).Any() )
                    {
                        ShowEditPersonDetails( person );
                        return;
                    }
                }

                pnlEdit.Visible = false;
                nbNotAuthorized.Visible = true;
            }
        }

        protected override void CreateChildControls()
        {
            base.EnsureChildControls();

            var childGuid = Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid();
            var person = GetPerson();

            if ( person != null && person.Id != 0 )
            {
                List<Guid> attributeGuids = null;
                if ( person.GetFamilyRole().Guid == childGuid )
                {
                    attributeGuids = GetAttributeValue( AttributeKeys.PersonAttributesChild ).SplitDelimitedValues().AsGuidList();
                }
                else
                {
                    attributeGuids = GetAttributeValue( AttributeKeys.PersonAttributesAdult ).SplitDelimitedValues().AsGuidList();
                }
                if ( attributeGuids != null && attributeGuids.Count > 0 )
                {
                    var attributeKeys = AttributeCache.All().Where( a => attributeGuids.Contains( a.Guid ) ).Select( a => a.Key ).ToList();
                    person.LoadAttributes();
                    Helper.AddEditControls( "", attributeKeys, person, phAttributes, this.BlockValidationGroup, true, null, numberOfColumns: 2 );
                }
            }
        }

        #endregion

        #region Events

        #region View Events
        #endregion

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            if ( GetAttributeValue( AttributeKeys.DisplayTerms ).AsBoolean() && !cbTOS.Checked )
            {
                nbTOS.Visible = true;
                return;
            }

            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );

            var person = GetPerson( personService );

            var personAliasEntityType = EntityTypeCache.Get( typeof( PersonAlias ) );

            if ( person.Id != 0 )
            {

                var changeRequest = new ChangeRequest
                {
                    EntityTypeId = personAliasEntityType.Id,
                    EntityId = person.PrimaryAliasId ?? 0,
                    RequestorAliasId = CurrentPersonAliasId ?? 0
                };


                if ( person.PhotoId != imgPhoto.BinaryFileId )
                {
                    changeRequest.EvaluatePropertyChange( person, "PhotoId", imgPhoto.BinaryFileId );
                    if ( person.Photo != null )
                    {
                        changeRequest.EvaluatePropertyChange( person.Photo, "IsTemporary", true, true );
                    }
                }

                changeRequest.EvaluatePropertyChange( person, "TitleValue", DefinedValueCache.Get( ddlTitle.SelectedValueAsInt() ?? 0 ) );
                changeRequest.EvaluatePropertyChange( person, "FirstName", tbFirstName.Text );
                changeRequest.EvaluatePropertyChange( person, "NickName", tbNickName.Text );
                changeRequest.EvaluatePropertyChange( person, "LastName", tbLastName.Text );
                changeRequest.EvaluatePropertyChange( person, "SuffixValue", DefinedValueCache.Get( ddlSuffix.SelectedValueAsInt() ?? 0 ) );

                var birthMonth = person.BirthMonth;
                var birthDay = person.BirthDay;
                var birthYear = person.BirthYear;

                var birthday = bpBirthDay.SelectedDate;
                if ( birthday.HasValue )
                {
                    // If setting a future birth date, subtract a century until birth date is not greater than today.
                    var today = RockDateTime.Today;
                    while ( birthday.Value.CompareTo( today ) > 0 )
                    {
                        birthday = birthday.Value.AddYears( -100 );
                    }

                    changeRequest.EvaluatePropertyChange( person, "BirthMonth", birthday.Value.Month );
                    changeRequest.EvaluatePropertyChange( person, "BirthDay", birthday.Value.Day );

                    if ( birthday.Value.Year != DateTime.MinValue.Year )
                    {
                        changeRequest.EvaluatePropertyChange( person, "BirthYear", birthday.Value.Year );
                    }
                    else
                    {
                        changeRequest.EvaluatePropertyChange( person, "BirthYear", ( int? ) null );
                    }
                }

                if ( ddlGradePicker.Visible )
                {
                    changeRequest.EvaluatePropertyChange( person, "GraduationYear", ypGraduation.SelectedYear );
                }
                changeRequest.EvaluatePropertyChange( person, "Gender", rblGender.SelectedValue.ConvertToEnum<Gender>() );

                var primaryFamilyMembers = person.GetFamilyMembers( true ).Where( m => m.PersonId == person.Id ).ToList();
                foreach ( var member in primaryFamilyMembers )
                {
                    changeRequest.EvaluatePropertyChange( member, "GroupRoleId", rblRole.SelectedValue.AsInteger(), true );
                }

                var primaryFamily = person.GetFamily( rockContext );
                var familyChangeRequest = new ChangeRequest
                {
                    EntityTypeId = EntityTypeCache.Get( typeof( Group ) ).Id,
                    EntityId = primaryFamily.Id,
                    RequestorAliasId = CurrentPersonAliasId ?? 0
                };

                // update campus
                bool showCampus = GetAttributeValue( "ShowCampusSelector" ).AsBoolean();
                if ( showCampus )
                {
                    // Only update campus for adults
                    GroupTypeRoleService groupTypeRoleService = new GroupTypeRoleService( rockContext );
                    var adultGuid = Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid();
                    var adultRole = groupTypeRoleService.Get( adultGuid );
                    if ( rblRole.SelectedValue.AsInteger() == adultRole.Id )
                    { 
                        familyChangeRequest.EvaluatePropertyChange( primaryFamily, "CampusId", cpCampus.SelectedCampusId );
                    }
                }

                //Evaluate PhoneNumbers
                bool showPhoneNumbers = GetAttributeValue( "ShowPhoneNumbers" ).AsBoolean();
                if ( showPhoneNumbers )
                {
                    var phoneNumberTypeIds = new List<int>();
                    var phoneNumbersScreen = new List<PhoneNumber>();
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
                                var phoneNumberList = person.PhoneNumbers.Where( n => n.NumberTypeValueId == phoneNumberTypeId ).ToList();
                                var phoneNumber = phoneNumberList.FirstOrDefault( pn => pn.Number == PhoneNumber.CleanNumber( pnbPhone.Number ) );
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
                                    phoneNumbersScreen.Add( phoneNumber );

                                }
                                else if ( phoneNumber != null && pnbPhone.Text.IsNotNullOrWhiteSpace() ) // update number
                                {
                                    changeRequest.EvaluatePropertyChange( phoneNumber, "Number", PhoneNumber.CleanNumber( pnbPhone.Number ), true );
                                    changeRequest.EvaluatePropertyChange( phoneNumber, "IsMessagingEnabled", ( !smsSelected && cbSms.Checked ), true );
                                    changeRequest.EvaluatePropertyChange( phoneNumber, "IsUnlisted", cbUnlisted.Checked, true );
                                    phoneNumbersScreen.Add( phoneNumber );
                                }

                            }
                        }
                    }
                    //Remove old phone numbers or changed
                    var phoneNumbersToRemove = person.PhoneNumbers
                                       .Where( n => !phoneNumbersScreen.Any( n2 => n2.Number == n.Number && n2.NumberTypeValueId == n.NumberTypeValueId ) ).ToList();

                    foreach ( var number in phoneNumbersToRemove )
                    {
                        var phoneComment = string.Format( "{0}: {1}.", number.NumberTypeValue.Value, number.NumberFormatted );
                        changeRequest.DeleteEntity( number, true, phoneComment );
                    }

                }

                changeRequest.EvaluatePropertyChange( person, "Email", tbEmail.Text.Trim() );
                changeRequest.EvaluatePropertyChange( person, "EmailPreference", rblEmailPreference.SelectedValueAsEnum<EmailPreference>() );
                changeRequest.EvaluatePropertyChange( person, "CommunicationPreference", rblCommunicationPreference.SelectedValueAsEnum<CommunicationType>() );

                // if they used the ImageEditor, and cropped it, the non-cropped file is still in BinaryFile. So clean it up
                if ( imgPhoto.CropBinaryFileId.HasValue )
                {
                    if ( imgPhoto.CropBinaryFileId != person.PhotoId )
                    {
                        BinaryFileService binaryFileService = new BinaryFileService( rockContext );
                        var binaryFile = binaryFileService.Get( imgPhoto.CropBinaryFileId.Value );
                        if ( binaryFile != null && binaryFile.IsTemporary )
                        {
                            string errorMessage;
                            if ( binaryFileService.CanDelete( binaryFile, out errorMessage ) )
                            {
                                binaryFileService.Delete( binaryFile );
                                rockContext.SaveChanges();
                            }
                        }
                    }
                }


                // save family information
                if ( pnlAddress.Visible )
                {
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
                            GroupId = primaryFamily.Id,
                            LocationId = location.Id,
                            GroupLocationTypeValueId = homeLocationType.Id,
                            IsMailingLocation = true,
                            IsMappedLocation = true
                        };

                        var newGroupLocation = familyChangeRequest.AddEntity( groupLocation, rockContext, true, location.ToString() );

                        var homelocations = primaryFamily.GroupLocations.Where( gl => gl.GroupLocationTypeValueId == homeLocationType.Id );
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
                }

                // Handle both Child and Adult attributes together here
                var attributeGuids = GetAttributeValue( AttributeKeys.PersonAttributesAdult ).SplitDelimitedValues().AsGuidList();
                attributeGuids.AddRange( GetAttributeValue( AttributeKeys.PersonAttributesChild ).SplitDelimitedValues().AsGuidList() );
                if ( attributeGuids.Count > 0 )
                {
                    person.LoadAttributes();
                    Helper.GetEditValues( phAttributes, person );
                    changeRequest.EvaluateAttributes( person );
                }

                if ( changeRequest.ChangeRecords.Any()
                || ( !familyChangeRequest.ChangeRecords.Any() && tbComments.Text.IsNotNullOrWhiteSpace() ) )
                {
                    changeRequest.RequestorComment = tbComments.Text;
                    ChangeRequestService changeRequestService = new ChangeRequestService( rockContext );
                    changeRequestService.Add( changeRequest );
                    rockContext.SaveChanges();

                    changeRequest.CompleteChanges( rockContext );
                }

                if ( familyChangeRequest.ChangeRecords.Any() )
                {
                    familyChangeRequest.RequestorComment = tbComments.Text;
                    ChangeRequestService changeRequestService = new ChangeRequestService( rockContext );
                    changeRequestService.Add( familyChangeRequest );
                    rockContext.SaveChanges();
                    familyChangeRequest.CompleteChanges( rockContext );
                }
            }
            else
            {
                var primaryFamily = CurrentPerson.GetFamily( rockContext );

                person.PhotoId = imgPhoto.BinaryFileId;

                if ( imgPhoto.BinaryFileId.HasValue )
                {
                    BinaryFileService binaryFileService = new BinaryFileService( rockContext );
                    var binaryFile = binaryFileService.Get( imgPhoto.BinaryFileId.Value );
                    binaryFile.IsTemporary = false;
                }

                person.FirstName = tbFirstName.Text;
                person.NickName = tbNickName.Text;
                person.LastName = tbLastName.Text;
                person.TitleValueId = ddlTitle.SelectedValue.AsIntegerOrNull();
                person.SuffixValueId = ddlSuffix.SelectedValue.AsIntegerOrNull();
                var birthday = bpBirthDay.SelectedDate;
                if ( birthday.HasValue )
                {
                    // If setting a future birth date, subtract a century until birth date is not greater than today.
                    var today = RockDateTime.Today;
                    while ( birthday.Value.CompareTo( today ) > 0 )
                    {
                        birthday = birthday.Value.AddYears( -100 );
                    }

                    person.BirthMonth = birthday.Value.Month;
                    person.BirthDay = birthday.Value.Day;

                    if ( birthday.Value.Year != DateTime.MinValue.Year )
                    {
                        person.BirthYear = birthday.Value.Year;
                    }
                    else
                    {
                        person.BirthYear = null;
                    }
                }

                person.Gender = rblGender.SelectedValue.ConvertToEnum<Gender>();
                person.Email = tbEmail.Text;
                person.EmailPreference = rblEmailPreference.SelectedValue.ConvertToEnum<EmailPreference>();

                if ( ddlGradePicker.Visible )
                {
                    person.GraduationYear = ypGraduation.SelectedYear;
                }


                GroupMember groupMember = new GroupMember
                {
                    PersonId = person.Id,
                    GroupId = primaryFamily.Id,
                    GroupRoleId = rblRole.SelectedValue.AsInteger()
                };

                PersonService.AddPersonToFamily( person, true, primaryFamily.Id, rblRole.SelectedValue.AsInteger(), rockContext );

                PhoneNumberService phoneNumberService = new PhoneNumberService( rockContext );

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
                            var phoneNumber = new PhoneNumber
                            {
                                PersonId = person.Id,
                                NumberTypeValueId = phoneNumberTypeId,
                                CountryCode = PhoneNumber.CleanNumber( pnbPhone.CountryCode ),
                                IsMessagingEnabled = cbSms.Checked,
                                Number = PhoneNumber.CleanNumber( pnbPhone.Number )
                            };
                            phoneNumberService.Add( phoneNumber );
                        }
                    }
                }
                rockContext.SaveChanges();

                var changeRequest = new ChangeRequest
                {
                    EntityTypeId = personAliasEntityType.Id,
                    EntityId = person.PrimaryAliasId ?? 0,
                    RequestorAliasId = CurrentPersonAliasId ?? 0,
                    RequestorComment = "Added as new person from My Account."
                };

                if ( tbComments.Text.IsNotNullOrWhiteSpace() )
                {
                    changeRequest.RequestorComment += "<br><br>Comment: " + tbComments.Text;
                }

                ChangeRequestService changeRequestService = new ChangeRequestService( rockContext );
                changeRequestService.Add( changeRequest );
                rockContext.SaveChanges();
                changeRequest.CompleteChanges( rockContext );
            }

            NavigateToParentPage();
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rContactInfo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        void rContactInfo_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var pnbPhone = e.Item.FindControl( "pnbPhone" ) as PhoneNumberBox;
            if ( pnbPhone != null )
            {
                pnbPhone.ValidationGroup = BlockValidationGroup;
                var phoneNumber = e.Item.DataItem as PhoneNumber;
                if ( _IsEditRecordAdult && ( phoneNumber != null ) )
                {
                    pnbPhone.Required = _RequiredPhoneNumberGuids.Contains( phoneNumber.NumberTypeValue.Guid );
                    if ( pnbPhone.Required )
                    {
                        pnbPhone.RequiredErrorMessage = string.Format( "{0} phone is required", phoneNumber.NumberTypeValue.Value );
                        HtmlGenericControl phoneNumberContainer = ( HtmlGenericControl ) e.Item.FindControl( "divPhoneNumberContainer" );
                        phoneNumberContainer.AddCssClass( "required" );
                    }
                }

            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Shows the edit person details.
        /// </summary>
        /// <param name="personGuid">The person's global unique identifier.</param>
        private void ShowEditPersonDetails( Person person )
        {
            var childGuid = Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid();
            RockContext rockContext = new RockContext();

            RoleType = null;

            rblRole.DataSource = GroupTypeCache.GetFamilyGroupType().Roles.OrderBy( r => r.Order ).ToList();
            rblRole.DataBind();
            rblRole.Visible = true;
            rblRole.Required = true;

            if ( GetAttributeValue( "DisableNameEdit" ).AsBoolean() )
            {
                tbFirstName.Enabled = false;
                tbLastName.Enabled = false;
            }

            imgPhoto.BinaryFileId = person.PhotoId;
            imgPhoto.NoPictureUrl = Person.GetPersonNoPictureUrl( person, 200, 200 );
            ddlTitle.SelectedValue = person.TitleValueId.HasValue ? person.TitleValueId.Value.ToString() : string.Empty;
            tbFirstName.Text = person.FirstName;
            tbNickName.Text = person.NickName;
            tbLastName.Text = person.LastName;
            ddlSuffix.SelectedValue = person.SuffixValueId.HasValue ? person.SuffixValueId.Value.ToString() : string.Empty;
            bpBirthDay.SelectedDate = person.BirthDate;
            rblGender.SelectedValue = person.Gender.ConvertToString();

            var familyRole = person.GetFamilyRole();
            rblRole.SelectedValue = familyRole != null ? familyRole.Id.ToString() : "0";


            if ( person.Id != 0 && person.GetFamilyRole().Guid == childGuid )
            {
                _IsEditRecordAdult = false;
                tbEmail.Required = false;
                // don't display campus selector to children.
                phCampus.Visible = false;

                if ( person.GraduationYear.HasValue )
                {
                    ypGraduation.SelectedYear = person.GraduationYear.Value;
                }
                else
                {
                    ypGraduation.SelectedYear = null;
                }

                ddlGradePicker.Visible = true;
                if ( !person.HasGraduated ?? false )
                {
                    int gradeOffset = person.GradeOffset.Value;
                    var maxGradeOffset = ddlGradePicker.MaxGradeOffset;

                    // keep trying until we find a Grade that has a gradeOffset that includes the Person's gradeOffset (for example, there might be combined grades)
                    while ( !ddlGradePicker.Items.OfType<ListItem>().Any( a => a.Value.AsInteger() == gradeOffset ) && gradeOffset <= maxGradeOffset )
                    {
                        gradeOffset++;
                    }

                    ddlGradePicker.SetValue( gradeOffset );
                }
                else
                {
                    ddlGradePicker.SelectedIndex = 0;
                }
            }
            else
            {
                _IsEditRecordAdult = true;
                bool requireEmail = GetAttributeValue( "RequireAdultEmailAddress" ).AsBoolean();
                tbEmail.Required = requireEmail;
                ddlGradePicker.Visible = false;

                // show/hide campus selector
                bool showCampus = GetAttributeValue( "ShowCampusSelector" ).AsBoolean();
                phCampus.Visible = showCampus;
                if ( showCampus )
                {
                    cpCampus.Campuses = CampusCache.All( false );
                    cpCampus.SetValue( person.GetCampus().Id );
                }
            }


            tbEmail.Text = person.Email;
            rblEmailPreference.SelectedValue = person.EmailPreference.ConvertToString( false );

            rblCommunicationPreference.Visible = this.GetAttributeValue( "ShowCommunicationPreference" ).AsBoolean();
            rblCommunicationPreference.SetValue( person.CommunicationPreference == CommunicationType.SMS ? "2" : "1" );


            // Family Attributes
            if ( person.Id == CurrentPerson.Id )
            {
                Guid? locationTypeGuid = GetAttributeValue( "AddressType" ).AsGuidOrNull();
                if ( locationTypeGuid.HasValue )
                {
                    pnlAddress.Visible = true;
                    var addressTypeDv = DefinedValueCache.Get( locationTypeGuid.Value );

                    lAddressTitle.Text = addressTypeDv.Value + " Address";

                    var familyGroupTypeGuid = Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuidOrNull();

                    if ( familyGroupTypeGuid.HasValue )
                    {
                        var familyGroupType = GroupTypeCache.Get( familyGroupTypeGuid.Value );

                        var familyAddress = new GroupLocationService( rockContext ).Queryable()
                                            .Where( l => l.Group.GroupTypeId == familyGroupType.Id
                                                    && l.GroupLocationTypeValueId == addressTypeDv.Id
                                                    && l.Group.Members.Any( m => m.PersonId == person.Id ) )
                                            .FirstOrDefault();
                        if ( familyAddress != null )
                        {
                            acAddress.SetValues( familyAddress.Location );
                        }
                    }
                }
            }
            else
            {
                pnlAddress.Visible = false;
            }

            BindPhoneNumbers( person );

            pnlEdit.Visible = true;

            if ( GetAttributeValue( AttributeKeys.DisplayTerms ).AsBoolean() )
            {
                cbTOS.Visible = true;
                cbTOS.Required = true;
                cbTOS.Text = GetAttributeValue( AttributeKeys.TermsOfServiceText );
            }
        }
        private void BindPhoneNumbers( Person person = null )
        {
            if ( person == null )
                person = new Person();

            bool showPhoneNumbers = GetAttributeValue( "ShowPhoneNumbers" ).AsBoolean();
            pnlPhoneNumbers.Visible = showPhoneNumbers;
            if ( showPhoneNumbers )
            {

                var phoneNumbers = new List<PhoneNumber>();
                var phoneNumberTypes = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE ) );
                var mobilePhoneType = DefinedValueCache.Get( new Guid( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE ) );
                var selectedPhoneTypeGuids = GetAttributeValue( "PhoneTypes" ).Split( ',' ).AsGuidList();

                if ( phoneNumberTypes.DefinedValues.Where( pnt => selectedPhoneTypeGuids.Contains( pnt.Guid ) ).Any() )
                {
                    foreach ( var phoneNumberType in phoneNumberTypes.DefinedValues.Where( pnt => selectedPhoneTypeGuids.Contains( pnt.Guid ) ) )
                    {
                        var phoneNumberList = person.PhoneNumbers.Where( n => n.NumberTypeValueId == phoneNumberType.Id ).ToList();
                        if ( phoneNumberList.Count() == 0 )
                        {
                            phoneNumberList.Add( null );
                        }

                        foreach ( var phoneNumberTemp in phoneNumberList )
                        {
                            var phoneNumber = phoneNumberTemp;
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
                }
            }

        }

        private Person GetPerson()
        {
            return GetPerson( new PersonService( new RockContext() ) );
        }

        private Person GetPerson( PersonService personService )
        {
            var personGuid = PageParameter( PageParameterKeys.PersonGuid );
            var person = personService.Get( personGuid.AsGuid() );
            if ( person == null )
            {
                return new Person
                {
                    Guid = Guid.NewGuid()
                };
            }

            //Only return if the person is in the same family
            var familyMembers = person.GetFamilyMembers( true, ( RockContext ) personService.Context );
            if ( familyMembers.Select( gm => gm.PersonId ).Contains( CurrentPersonId ?? 0 ) )
            {
                return person;
            }
            return null;
        }

        #endregion

        protected void rblRole_SelectedIndexChanged( object sender, EventArgs e )
        {

            GroupTypeRoleService groupTypeRoleService = new GroupTypeRoleService( new RockContext() );
            List<Guid> attributeGuidList = new List<Guid>();
            var adultGuid = Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid();
            var groupTypeGuid = Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid();
            var selectedId = rblRole.SelectedValueAsId();
            if ( selectedId.HasValue )
            {
                if ( groupTypeRoleService.Queryable().Where( gr =>
                               gr.GroupType.Guid == groupTypeGuid &&
                               gr.Guid == adultGuid &&
                               gr.Id == selectedId ).Any() )
                {
                    ddlGradePicker.Visible = false;
                    tbEmail.Required = true;
                }
                else
                {
                    ddlGradePicker.Visible = true;
                    tbEmail.Required = false;
                }
            }
        }

        protected void btnCancel_Click( object sender, EventArgs e )
        {
            NavigateToParentPage();
        }
    }
}