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
using System.Text;


namespace RockWeb.Plugins.org_secc.ChangeManager
{
    [DisplayName( "Change Managed Profile Remove Family Member" )]
    [Category( "SECC > CRM" )]
    [Description( "Public block for users to remove family members from a family." )]

    [BooleanField( "Display Phone Number", "Should the phone number be displayed. Default is true",
        DefaultBooleanValue = true, Key = AttributeKeys.DisplayPhone, Order = 0 )]
    [BooleanField( "Display Email", " Should the person's email address be displayed. Default is true",
        DefaultBooleanValue = true, Key = AttributeKeys.DisplayEmail, Order = 1 )]


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

        private void lbRemoveMember_Click( object sender, EventArgs e )
        {
            if(hfPersonGuid.Value.IsNotNullOrWhiteSpace())
            {
                MovePerson();
                
            }
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

        private void MovePerson()
        {
            var rockContext = new RockContext();
            var groupEntity = EntityTypeCache.Get( typeof( Group ) );
            var family = CurrentPerson.GetFamily();

            PersonService personService = new PersonService( rockContext );
            var removePerson = personService.Get( hfPersonGuid.Value.AsGuid() );
            if(removePerson != null)
            {

                var familyChangeRequest = new ChangeRequest()
                {
                    EntityTypeId = groupEntity.Id,
                    EntityId = family.Id,
                    RequestorAliasId = CurrentPersonAliasId ?? 0,
                    RequestorComment = tbAdditionalInfo.Text.Trim()
                };

                GroupMemberService groupMemberService = new GroupMemberService( rockContext );
                var familyGroupTypeId = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid() ).Id;

                var members = groupMemberService.Queryable()
                    .Where( m => m.Group.GroupTypeId == familyGroupTypeId )
                    .Where( m => m.PersonId == removePerson.Id )
                    .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active );

                foreach ( var member in members )
                {
                    if(member.GroupId == family.Id)
                    {
                        var comment = $"Removed {removePerson.FullName} from {family.Name}";
                        familyChangeRequest.DeleteEntity( member, true, comment );
                    }
                }

                if(members.Count() == 1)
                {
                    var newFamily = new Group()
                    {
                        GroupTypeId = familyGroupTypeId,
                        IsSystem = false,
                        IsSecurityRole = false,
                        Name = $"{removePerson.LastName} Family",
                        Guid = Guid.NewGuid(),
                        IsActive = true,
                        CampusId = family.CampusId
                    };

                    List<GroupLocation> locations = new List<GroupLocation>();
                    
                    foreach ( var l in family.GroupLocations)
                    {
                        GroupLocation location = new GroupLocation()
                        {
                            CreatedByPersonAliasId = CurrentPersonAliasId,
                            ModifiedByPersonAliasId = CurrentPersonAliasId,
                            LocationId = l.LocationId,
                            GroupLocationTypeValueId = l.GroupLocationTypeValueId,
                            IsMailingLocation = l.IsMailingLocation,
                            IsMappedLocation = l.IsMappedLocation,
                            Guid = Guid.NewGuid()
                        };
                        locations.Add( location );
                    }
                    newFamily.GroupLocations = locations;
                    var familyContext = new RockContext();
                    GroupService groupService = new GroupService( familyContext );
                    groupService.Add( newFamily );
                    familyContext.SaveChanges();

                    GroupMember familyMember = new GroupMember
                    {
                        PersonId = removePerson.Id,
                        GroupId = newFamily.Id,
                        GroupMemberStatus = GroupMemberStatus.Active,
                        Guid = Guid.NewGuid(),
                        GroupRoleId = members.First().GroupRoleId
                    };
                    var insertComment = string.Format( "Added {0} to {1}", removePerson.FullName, newFamily.Name );
                    familyChangeRequest.AddEntity( familyMember, rockContext, false, insertComment );
                }


                List<string> errors;
                if(familyChangeRequest.ChangeRecords.Any())
                {
                    ChangeRequestService changeRequestService = new ChangeRequestService( rockContext );
                    changeRequestService.Add( familyChangeRequest );
                    rockContext.SaveChanges();
                    familyChangeRequest.CompleteChanges( rockContext, out errors );
                }
            }
            NavigateToParentPage();
        }

        private void ShowNotAuthorizedAlert()
        {
            nbNotAuthorized.Visible = true;
            pnlEdit.Visible = false;
        }
        #endregion

    }
}