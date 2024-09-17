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
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

/// <summary>
/// Template block for developers to use to start a new block.
/// </summary>
[DisplayName( "My Account Medical Consent" )]
[Category( "SECC > Family Check In" )]
[Description( "A block to prompt parents for medical consent for the minors in their family" )]

#region Block Attributes
[BooleanField( "Show No Minors Message", "Should a message be displayed when no minors are found who need consent?", true, "", 0 )]
[TextField( "Medical Consent Attribute Key", "The key of the person attribute that will be updated with the medical consent message", true, "MedicalConsent", "", 1 )]
#endregion Block Attributes

public partial class Plugins_org_secc_FamilyCheckin_MyAccountMedicalConsent : Rock.Web.UI.RockBlock

{
    #region Attribute Keys

    private static class AttributeKey
    {
        public const string ShowNoMinorsMessage = "ShowNoMinorsMessage";
        public const string MedicalConsentAttributeKey = "MedicalConsentAttributeKey";
    }

    #endregion Attribute Keys

    #region Base Control Methods

    /// <summary>
    /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
    /// </summary>
    /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
    protected override void OnLoad( EventArgs e )
    {
        base.OnLoad( e );

        if ( !Page.IsPostBack )
        {
            var medicalConsentAttributeKey = GetAttributeValue( AttributeKey.MedicalConsentAttributeKey );

            var rockContext = new RockContext();

            var familyGroup = new GroupService( rockContext ).Get( ( int ) CurrentPerson.PrimaryFamilyId );
            var minors = familyGroup.Members.Where( m => m.Person.Age < 18 ).ToList();
            if ( minors.Count == 0 )
            {
                if ( GetAttributeValue( AttributeKey.ShowNoMinorsMessage ).AsBoolean() )
                {
                    nbNoMinors.Visible = true;
                }
                return;
            }

            // check if all minors already have a value for the medical consent attribute
            foreach ( var minor in minors )
            {
                var person = minor.Person;
                person.LoadAttributes( rockContext );

                if ( person.GetAttributeValue( medicalConsentAttributeKey ).IsNullOrWhiteSpace() )
                {
                    // if any minor does not have a value for the medical consent attribute, show the consent form
                    h4FamilyName.InnerText = $"{CurrentPerson.PrimaryFamily.Name} Household";
                    sCurrentPersonFullName.InnerText = CurrentPerson.FullName;
                    sCurrentPersonCellNumber.InnerText = CurrentPerson.GetPhoneNumber( DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).Guid )?.NumberFormatted;
                    sCurrentPersonAddress.InnerText = familyGroup.GroupLocations.FirstOrDefault( gl => gl.GroupLocationTypeValue.Value == "Home" )?.Location?.GetFullStreetAddress();
                    aUpdateCurrentPerson.HRef = $"{GlobalAttributesCache.Value( "PublicApplicationRoot" )}MyAccount/Edit/{CurrentPerson.Guid}";

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
                            String phoneNumber;
                            foreach ( var adult in otherAdults )
                            {
                                phoneNumber = adult.GetPhoneNumber( DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).Guid )?.NumberFormatted;
                                pnlOtherAdults.Controls.Add( new HtmlGenericControl( "p") {
                                    InnerHtml = adult.FullName + "<br />" + phoneNumber + "<br />" +
                                    $"</p><a href=\"{GlobalAttributesCache.Value( "PublicApplicationRoot" )}MyAccount/Edit/{adult.Guid}\" class=\"btn btn-primary btn-xs\">Update Info</a>"
                                } );
                            }
                            pnlOtherAdults.Visible = true;
                        }
                    }

                    foreach ( var child in minors )
                    {
                        pnlChildren.Controls.Add( new HtmlGenericControl("p") { InnerHtml = child.Person.FullName + "(" + child.Person.Age + ")" } );
                    }

                    pnlConsent.Visible = true;
                    return;
                }
            }

            // if all minors already have a value for the medical consent attribute, show the success message
            if ( GetAttributeValue( AttributeKey.ShowNoMinorsMessage ).AsBoolean() )
            {
                nbAllMinorsHaveConsent.Visible = true;
            }

            return;
        }
    }

    #endregion

    #region Events

    // Handlers called by the controls on your block.

    protected void btnConsent_Click( object sender, EventArgs e )
    {
        var medicalConsent = $"{CurrentPerson.FullName} {RockDateTime.Today.ToShortDateString()}";

        var rockContext = new RockContext();
        var familyGroup = new GroupService( rockContext ).Get( ( int ) CurrentPerson.PrimaryFamilyId );
        var minors = familyGroup.Members.Where( m => m.Person.Age < 18 ).ToList();

        // for each child
        foreach ( var minor in minors )
        {
            // Assuming you want to update the medical consent attribute for each minor
            var person = minor.Person;
            person.LoadAttributes( rockContext );
            person.SetAttributeValue( GetAttributeValue( AttributeKey.MedicalConsentAttributeKey ), medicalConsent );
            person.SaveAttributeValues( rockContext );
        }

        pnlConsent.Visible = false;
        pnlSuccess.Visible = true;
    }

    #endregion

    #region Methods

    // helper functional methods (like BindGrid(), etc.)


    #endregion
}