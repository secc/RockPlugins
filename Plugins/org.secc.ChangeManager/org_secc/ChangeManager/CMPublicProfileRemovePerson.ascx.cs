using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.Data.Entity;

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
            lFirstName.Text = person.NickName;

        }

        private void MovePerson()
        {
            var rockContext = new RockContext();
            rockContext.WrapTransaction( () =>
            {
                var groupService = new GroupService( rockContext );
                var groupMemberService = new GroupMemberService( rockContext );
                var personService = new PersonService( rockContext );
                var groupLocationService = new GroupLocationService( rockContext );

                var changeRequestContext = new RockContext();
                var changeRequestService = new ChangeRequestService( changeRequestContext );


                var familyGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid() );
                var adultRole = familyGroupType.Roles
                    .Where( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() )
                    .SingleOrDefault();
                var childRole = familyGroupType.Roles
                    .Where( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() )
                    .SingleOrDefault();

                var personToMove = personService.Get( hfPersonGuid.Value.AsGuid() );

                var sharedFamily = groupService.Queryable()
                    .Include( f => f.Members )
                    .Include(f => f.GroupLocations)
                    .Where( g => g.GroupTypeId == familyGroupType.Id )
                    .Where( g => g.Members.Where( m => m.PersonId == CurrentPerson.Id && m.GroupMemberStatus == GroupMemberStatus.Active ).Any() )
                    .Where( g => g.Members.Where( m => m.PersonId == personToMove.Id && m.GroupMemberStatus == GroupMemberStatus.Active ).Any() )
                    .FirstOrDefault();

                if(sharedFamily == null)
                {
                    // No Shared Family Found;
                    NavigateToParentPage();
                    return;
                }

                var changeRequest = new ChangeRequest()
                {
                    EntityTypeId = EntityTypeCache.Get( typeof( Group ) ).Id,
                    EntityId = sharedFamily.Id,
                    RequestorAliasId = CurrentPersonAliasId.Value,
                    RequestorComment = tbAdditionalInfo.Text.Trim(),
                    ChangeRecords = new List<ChangeRecord>()
                };

                var personToMoveFamilies = personToMove.GetFamilies();

                var groupMemberToRemove = sharedFamily.Members.Where( m => m.PersonId == personToMove.Id )
                    .FirstOrDefault();

                var changeRecordOld = new ChangeRecord()
                {
                    WasApplied = true,
                    RelatedEntityTypeId = EntityTypeCache.Get( typeof( GroupMember ) ).Id,
                    RelatedEntityId = groupMemberToRemove.Id,
                    OldValue = groupMemberToRemove.ToJson(),
                    Comment = $"Removed {personToMove.FullName} from {sharedFamily.Name}.",
                    Guid = Guid.NewGuid(),
                    Action = ChangeRecordAction.Delete
                };
                changeRequest.ChangeRecords.Add( changeRecordOld );               
                

                var familyRoleId = groupMemberToRemove.GroupRoleId;

                var groupMember = groupMemberService.Get( groupMemberToRemove.Guid );
                groupMemberService.Delete( groupMember );
                rockContext.SaveChanges();

                if(personToMoveFamilies.Count() > 1)
                {
                    //Person in multiple familes
                    NavigateToParentPage();
                    return;
                }

                var newGroup = new Group();
                newGroup.Name = personToMove.LastName + " " + familyGroupType.Name;
                newGroup.GroupTypeId = familyGroupType.Id;
                newGroup.CampusId = sharedFamily.CampusId;
                groupService.Add( newGroup );
                rockContext.SaveChanges();

                if(personToMove.GivingGroup != null && personToMove.GivingGroupId == sharedFamily.Id )
                {
                    personToMove.GivingGroupId = newGroup.Id;
                }

                var newGroupMember = new GroupMember();
                newGroupMember.GroupId = newGroup.Id;
                newGroupMember.PersonId = personToMove.Id;
                newGroupMember.GroupMemberStatus = GroupMemberStatus.Active;
                newGroupMember.GroupRoleId = familyRoleId;
                newGroupMember.Guid = Guid.NewGuid();
                groupMemberService.Add( newGroupMember );
                rockContext.SaveChanges();

                var newChangeRecord = new ChangeRecord()
                {
                    WasApplied = true,
                    RelatedEntityTypeId = EntityTypeCache.Get( typeof( GroupMember ) ).Id,
                    RelatedEntityId = newGroupMember.Id,
                    NewValue = newGroupMember.ToJson(),
                    Comment = $"Added {personToMove.FullName} to {newGroup.Name}.",
                    Guid = Guid.NewGuid(),
                    Action = ChangeRecordAction.Create
                };
                changeRequest.ChangeRecords.Add( newChangeRecord );

                foreach ( var gl in sharedFamily.GroupLocations )
                {
                    var newLocation = new GroupLocation();
                    newLocation.GroupId = newGroup.Id;
                    newLocation.LocationId = gl.LocationId;
                    newLocation.GroupLocationTypeValueId = gl.GroupLocationTypeValueId;
                    newLocation.IsMailingLocation = gl.IsMailingLocation;
                    newLocation.IsMappedLocation = gl.IsMappedLocation;
                    groupLocationService.Add( newLocation );
                }

                rockContext.SaveChanges();
                changeRequestService.Add( changeRequest );
                changeRequestContext.SaveChanges();
            } );

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