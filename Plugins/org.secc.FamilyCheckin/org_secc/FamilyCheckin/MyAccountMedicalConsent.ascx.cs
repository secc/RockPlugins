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
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

[DisplayName( "My Account Medical Consent" )]
[Category( "SECC > Family Check In" )]
[Description( "A block to prompt parents for medical consent for the minors in their family" )]

#region Block Attributes
[BooleanField( "Show Message", "Should a message be displayed when no minors are found who need consent?", true, "", 0 )]
[TextField( "Medical Consent Attribute Key", "The key of the person attribute that will be updated with the medical consent message", true, "MedicalConsent", "", 1 )]
#endregion Block Attributes

public partial class Plugins_org_secc_FamilyCheckin_MyAccountMedicalConsent : Rock.Web.UI.RockBlock
{
    #region Attribute Keys

    private static class AttributeKey
    {
        public const string ShowMessage = "ShowMessage";
        public const string MedicalConsentAttributeKey = "MedicalConsentAttributeKey";
    }

    #endregion Attribute Keys

    #region Base Control Methods

    protected override void OnLoad( EventArgs e )
    {
        base.OnLoad( e );

        if ( !Page.IsPostBack )
        {
            LoadConsentForm();
        }
    }

    private void LoadConsentForm()
    {
        var medicalConsentAttributeKey = GetAttributeValue( AttributeKey.MedicalConsentAttributeKey );
        var rockContext = new RockContext();
        var familyGroup = new GroupService( rockContext ).Get( ( int ) CurrentPerson.PrimaryFamilyId );
        var minors = familyGroup.Members.Where( m => m.Person.Age < 18 ).ToList();

        if ( minors.Count == 0 )
        {
            ShowNoMinorsMessage();
        }

        if ( AllMinorsHaveConsent( minors, medicalConsentAttributeKey, rockContext ) )
        {
            if ( GetAttributeValue( AttributeKey.ShowMessage ).AsBoolean() )
            {
                ShowAllMinorsHaveConsentMessage();
            }
        }
        else
        {
            ShowConsentForm( minors, familyGroup, rockContext );
        }
    }

    private void ShowNoMinorsMessage()
    {
        if ( GetAttributeValue( AttributeKey.ShowMessage ).AsBoolean() )
        {
            nbNoMinors.Visible = true;
        }
    }

    private bool AllMinorsHaveConsent( IEnumerable<GroupMember> minors, string medicalConsentAttributeKey, RockContext rockContext )
    {
        foreach ( var minor in minors )
        {
            var person = minor.Person;
            person.LoadAttributes( rockContext );

            if ( person.GetAttributeValue( medicalConsentAttributeKey ).IsNullOrWhiteSpace() )
            {
                return false;
            }
        }
        return true;
    }

    private void ShowAllMinorsHaveConsentMessage()
    {
        if ( GetAttributeValue( AttributeKey.ShowMessage ).AsBoolean() )
        {
            nbAllMinorsHaveConsent.Visible = true;
        }
    }

    private void ShowConsentForm( IEnumerable<GroupMember> minors, Group familyGroup, RockContext rockContext )
    {
        h4FamilyName.InnerText = $"{CurrentPerson.PrimaryFamily.Name} Household";
        sCurrentPersonFullName.InnerText = CurrentPerson.FullName;
        sCurrentPersonCellNumber.InnerText = CurrentPerson.GetPhoneNumber( DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).Guid )?.NumberFormatted;
        sCurrentPersonAddress.InnerText = familyGroup.GroupLocations.FirstOrDefault( gl => gl.GroupLocationTypeValue.Value == "Home" )?.Location?.GetFullStreetAddress();
        aUpdateCurrentPerson.HRef = $"{GlobalAttributesCache.Value( "PublicApplicationRoot" )}MyAccount/Edit/{CurrentPerson.Guid}";

        ShowOtherAdults( familyGroup );
        ShowMinors( minors );

        pnlConsent.Visible = true;
    }

    private void ShowOtherAdults( Group familyGroup )
    {
        var adultRoleId = GroupTypeCache.GetFamilyGroupType().Roles
                            .FirstOrDefault( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() )?.Id;

        if ( adultRoleId.HasValue )
        {
            var otherAdults = familyGroup.Members
                .Where( m => m.GroupRoleId == adultRoleId.Value && m.PersonId != CurrentPerson.Id )
                .Select( m => m.Person )
                .ToList();

            if ( otherAdults.Any() )
            {
                HtmlGenericControl otherAdult;
                foreach ( var adult in otherAdults )
                {
                    var phoneNumber = adult.GetPhoneNumber( DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).Guid )?.NumberFormatted;
                    otherAdult = new HtmlGenericControl( "div" )
                    {
                        InnerHtml = $"<p>{adult.FullName}<br />{phoneNumber}</p><a href=\"{GlobalAttributesCache.Value( "PublicApplicationRoot" )}MyAccount/Edit/{adult.Guid}\" class=\"btn btn-primary btn-xs\">Update Info</a>"
                    };
                    otherAdult.AddCssClass( "col-sm-6 other-adult" );
                    pnlOtherAdults.Controls.Add( otherAdult );
                }
                pnlOtherAdults.Visible = true;
            }
        }
    }

    private void ShowMinors( IEnumerable<GroupMember> minors )
    {
        foreach ( var child in minors )
        {
            pnlChildren.Controls.Add( new HtmlGenericControl( "p" ) { InnerHtml = $"{child.Person.FullName} ({child.Person.Age})" } );
        }
    }

    #endregion

    #region Events

    protected void btnConsent_Click( object sender, EventArgs e )
    {
        var medicalConsent = $"{CurrentPerson.FullName} {RockDateTime.Today.ToShortDateString()}";
        var rockContext = new RockContext();
        var familyGroup = new GroupService( rockContext ).Get( ( int ) CurrentPerson.PrimaryFamilyId );
        var minors = familyGroup.Members.Where( m => m.Person.Age < 18 ).ToList();

        foreach ( var minor in minors )
        {
            var person = minor.Person;
            person.LoadAttributes( rockContext );
            person.SetAttributeValue( GetAttributeValue( AttributeKey.MedicalConsentAttributeKey ), medicalConsent );
            person.SaveAttributeValues( rockContext );
        }

        pnlConsent.Visible = false;
        pnlSuccess.Visible = true;
    }

    #endregion
}