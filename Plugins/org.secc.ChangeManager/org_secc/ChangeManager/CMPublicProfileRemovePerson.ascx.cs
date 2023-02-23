using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
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
using org.secc.FamilyCheckin;
using Microsoft.Ajax.Utilities;
using System.Text;
using DocumentFormat.OpenXml.Drawing;

namespace RockWeb.Plugins.org_secc.ChangeManager
{
    [DisplayName( "Change Managed Profile Remove Family Member" )]
    [Category( "SECC > CRM" )]
    [Description( "Public block for users to remove family members from a family." )]

    [BooleanField( "Display Phone Number", "Should the phone number be displayed. Default is true",
        DefaultBooleanValue = true, Key = AttributeKeys.DisplayPhone, Order = 0 )]
    [BooleanField( "Display Email", " Should the person's email address be displayed. Default is true",
        DefaultBooleanValue = true, Key = AttributeKeys.DisplayEmail, Order = 1 )]
    [BooleanField( "Apply Change", "Should the change be applied immediately.",
        DefaultBooleanValue = true, Key = AttributeKeys.ApplyChange, Order = 2 )]


    public partial class CMPublicProfileRemovePerson : Rock.Web.UI.RockBlock
    {
        #region Keys
        protected static class AttributeKeys
        {
            internal const string DisplayPhone = "DisplayPhone";
            internal const string DisplayEmail = "DisplayEmail";
            internal const string ApplyChange = "ApplyChange";

        }

        protected static class PageParameterKeys
        {
            internal const string PersonGuid = "PersonGuid";
        }
        #endregion


        #region Base Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            lbRemoveMember.Click += lbRemoveMember_Click;
        }


        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            if ( !Page.IsPostBack )
            {
                LoadPerson();
            }
        }
        #endregion

        #region Events
        private void lbCancel_Click( object sender, EventArgs e )
        {
            throw new NotImplementedException();
        }

        private void lbRemoveMember_Click( object sender, EventArgs e )
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Methods
        private void LoadPerson()
        {
            var adultRoleGuid = Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid();
            var currentPersonRole = CurrentPerson.GetFamilyRole();

            var personGuid = PageParameter( PageParameterKeys.PersonGuid ).AsGuid();
            hfPersonGuid.Value = personGuid.ToString();

            if(personGuid.IsEmpty())
            {
                ShowNotAuthorizedAlert();
                return;
            }

            if ( adultRoleGuid != currentPersonRole.Guid )
            {
                ShowNotAuthorizedAlert();
                return;
            }

            var rockContext = new RockContext();
            var person = new PersonService( rockContext ).Get( personGuid );

            var inSameFamily = person.GetFamilyMembers( false, rockContext )
                .Where( m => m.PersonId == CurrentPerson.Id )
                .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active )
                .Any();

            if(!inSameFamily)
            {
                ShowNotAuthorizedAlert();
                return;
            }

            imgProfile.ImageUrl = person.PhotoUrl;
            lName.Text = person.FullName;

            if(person.BirthDate != null)
            {
                pnlBirthDate.Visible = true;
                lAge.Text = $"{person.Age} Years ({person.BirthDate.ToShortDateString()})";
            }
            else
            {
                pnlBirthDate.Visible = false;
            }

            lFamilyRole.Text = person.GetFamilyRole( rockContext ).Name;

            var genderTxt = string.Empty;
            switch ( person.Gender )
            {
                case Gender.Unknown:
                    break;
                case Gender.Male:
                    genderTxt = "Male";
                    break;
                case Gender.Female:
                    genderTxt = "Female";
                    break;
                default:
                    break;
            }
            lGender.Text = genderTxt;

            if(GetAttributeValue(AttributeKeys.DisplayEmail).AsBoolean())
            {
                pnlEmail.Visible = true;
                lEmail.Text = person.Email;
            }
            else
            {
                pnlEmail.Visible = false;
            }

            if(GetAttributeValue(AttributeKeys.DisplayPhone).AsBoolean())
            {
                pnlPhoneNumber.Visible = true;
                var phoneSB = new StringBuilder();
                foreach ( var p in person.PhoneNumbers )
                {
                    phoneSB.Append( $"{p.NumberFormatted} <small>({p.NumberTypeValue.Value})</small><br />" );
                }
                lPhoneNumber.Text = phoneSB.ToString().ReplaceLastOccurrence( "<br />", "" );
            }
            else
            {
                pnlPhoneNumber.Visible = false;
            }

        }

        private void ShowNotAuthorizedAlert()
        {
            nbNotAuthorized.Visible = true;
            pnlEdit.Visible = false;
        }
        #endregion

    }
}