using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security.Authentication;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;


namespace RockWeb.Plugins.org_secc.SportsAndFitness.ControlCenter
{
    [DisplayName( "Control Center Person Actions" )]
    [Category( "Sports and Fitness > Control Center" )]
    [Description( "Person Actions for Sports and Fitness Control Center" )]

    [GroupField( "Sports and Fitness Group",
        Description = "The group that contains all sports and fitness members.",
        IsRequired = true,
        Order = 0,
        Key = AttributeKeys.SFGroup )]

    [GroupField( "Group Fitness Group",
        Description = "The group that all Group Fitness participants belong to",
        IsRequired = true,
        Order = 1,
        Key = AttributeKeys.GroupFitnessGroup )]
    [TextField( "Group Fitness Sessions Attribute Key",
        Description = "The key of the attribute that contains the group fitness credits.",
        IsRequired = true,
        Order = 2,
        Key = AttributeKeys.GroupFitnessCreditKey )]
    [TextField( "Childcare Credit Attribute Key",
        Description = "The family group attribute key that contains the S&F childcare credits.",
        IsRequired = true,
        Order = 3,
        Key = AttributeKeys.ChildcareCreditKey )]
    [AttributeField( "PIN Purpose Attribute",
        Description = "Attribute that contains the Purpose of a Login PIN",
        AllowMultiple = false,
        EntityTypeGuid = "0fa592f1-728c-4885-be38-60ed6c0d834f",
        IsRequired = true,
        Order = 4,
        Key = AttributeKeys.LoginPINPurpose )]
    [NoteTypeField( "Group Fitness Note Type",
        Description = "Note Type for notes added when updating credits on group fitness members.",
        AllowMultiple = false,
        EntityTypeName = "Rock.Model.GroupMember",
        IsRequired = false,
        Order = 5,
        Key = AttributeKeys.GroupFitnessNoteType )]

    [NoteTypeField( "Childcare Credit Note Type",
        Description = "Note type for notes added wehn updating Childcare credits on the family.",
        AllowMultiple = false,
        EntityTypeName = "Rock.Model.Group",
        EntityTypeQualifierColumn = "GroupTypeId",
        EntityTypeQualifierValue = "10",
        IsRequired = false,
        Order = 6,
        Key = AttributeKeys.ChildCareNoteType )]
    [LinkedPage("Sports and Fitness History",
        Description = "The page where the user can view Sports and Fitness Check-in History",
        IsRequired = false,
        Order = 7,
        Key = AttributeKeys.SFHistory)]
    [LinkedPage("Group Fitness History",
        Description = "The page where the user can view Group Fitness Check-in History.",
        IsRequired = false,
        Order = 8,
        Key = AttributeKeys.GFHistory)]
    [LinkedPage( "Childcare History",
        Description = "The page where the user can view Childcare Check-in History.",
        IsRequired = false,
        Order = 8,
        Key = AttributeKeys.ChildcareHistory )]

    public partial class PersonActions : RockBlock
    {
        public static class AttributeKeys
        {
            public const string GroupFitnessGroup = "GroupFitnessGroup";
            public const string GroupFitnessCreditKey = "GroupFitnessSessions";
            public const string GroupFitnessNoteType = "GroupFitnessNoteType";
            public const string SFGroup = "SportsAndFitnessGroup";
            public const string ChildcareCreditKey = "ChildcareCredits";
            public const string ChildCareNoteType = "ChildcareNoteType";
            public const string LoginPINPurpose = "LoginPINPurpose";
            public const string SFHistory = "SFHistoryPage";
            public const string GFHistory = "GFHistoryPage";
            public const string ChildcareHistory = "ChildcareHistoryPage";
        }

        private string _sportsAndFitnessPINPurposeGuid = "e98517ec-1805-456b-8453-ef8480bd487f";
        private string _personIdViewStateKey = "PersonActions_PersonId";
        private Person _selectedPerson;

        private Person SelectedPerson
        {
            get
            {
                if (_selectedPerson == null)
                {
                    LoadSelectedPerson();
                }
                return _selectedPerson;
            }
        }

        #region Base Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            mdGroupFitness.SaveClick += mdGroupFitness_SaveClick;
            mdChildcare.SaveClick += mdChildcare_SaveClick;
        }



        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            RouteAction();
            if (!Page.IsPostBack)
            {
                ViewState[_personIdViewStateKey] = PageParameter( "Person" );
                LoadPINBadge();
                LoadChildcareCreditBadge();
                LoadGroupFitnessSessionsBadge();
            }
        }

        #endregion Base Control Methods

        #region Events

        private void mdChildcare_SaveClick( object sender, EventArgs e )
        {
            nbChildcare.Title = String.Empty;
            nbChildcare.Text = String.Empty;
            nbChildcare.Visible = false;

            int familyGroupId = hfChildcareFamilyId.ValueAsInt();
            var creditsAdded = AddChildcareCredit( familyGroupId );

            if (creditsAdded)
            {
                LoadChildcareCreditBadge();
                mdChildcare.Hide();
                upMain.Update();
            }

        }

        private void mdGroupFitness_SaveClick( object sender, EventArgs e )
        {
            var groupMemberId = hfGroupMemberId.Value.AsInteger();

            SaveGroupFitnessParticipant( groupMemberId );

        }

        private void personAction_Click( object sender, EventArgs e )
        {
            var commandName = ((LinkButton) sender).CommandName;

            switch (commandName)
            {
                case "update-pin":
                    LoadPinModal();
                    break;
                case "add-childcare-credit":
                    LoadChildcareCreditModal();
                    break;
                case "add-groupfitness-sessions":
                    LoadGroupFitnessModal();
                    break;
                default:
                    break;
            }
        }

        #endregion Events

        #region Internal Methods

        private bool AddChildcareCredit( int familyGroupId )
        {
            using (var rockContext = new RockContext())
            {
                var groupService = new GroupService( rockContext );
                var family = groupService.Get( familyGroupId );

                if (family == null)
                {
                    nbChildcare.Title = "Family Not Found";
                    nbChildcare.Text = "The selected family was not found.";
                    nbChildcare.NotificationBoxType = NotificationBoxType.Warning;
                    nbChildcare.Visible = true;

                    return false;
                }

                var childcareFamilyAttributeKey = GetAttributeValue( AttributeKeys.ChildcareCreditKey );

                family.LoadAttributes( rockContext );
                var creditCount = family.GetAttributeValue( childcareFamilyAttributeKey ).AsInteger();
                creditCount += tbCCCreditsToAdd.Text.AsInteger();

                family.SetAttributeValue( childcareFamilyAttributeKey, creditCount );
                family.SaveAttributeValue( childcareFamilyAttributeKey, rockContext );
                rockContext.SaveChanges();


                var childcareNoteType = NoteTypeCache.Get( AttributeKeys.ChildCareNoteType.AsGuid() );
                if (tbCCNotes.Text.IsNotNullOrWhiteSpace() && childcareNoteType != null)
                {
                    var noteService = new NoteService( rockContext );
                    var note = new Note
                    {
                        NoteTypeId = childcareNoteType.Id,
                        EntityId = familyGroupId,
                        Text = tbCCNotes.Text.Trim(),
                        CreatedByPersonAliasId = CurrentPersonAliasId
                    };
                    noteService.Add( note );
                    rockContext.SaveChanges();
                }
            }

            return true;
        }

        private void ClearChildcareModal()
        {
            hfChildcareFamilyId.Value = "0";

            nbChildcare.Title = string.Empty;
            nbChildcare.Text = string.Empty;
            nbChildcare.NotificationBoxType = NotificationBoxType.Info;
            nbChildcare.Visible = false;

            tbCCBeginningCredits.Text = "0";
            tbCCCreditsToAdd.Text = "0";
            tbCCCreditsToAdd.Placeholder = "0";

            tbCCNotes.Text = string.Empty;
            tbCCNotes.Placeholder = "Optional";
        }

        private void ClearGroupFitnessModel()
        {
            hfGroupMemberId.Value = "0";
            nbGroupFitness.Title = string.Empty;
            nbGroupFitness.Text = string.Empty;
            nbGroupFitness.NotificationBoxType = NotificationBoxType.Info;
            nbGroupFitness.Visible = false;

            lGFBeginningCredits.Text = "0";
            tbGFCreditsToAdd.Text = string.Empty;
            tbGFCreditsToAdd.Placeholder = "0";
            tbGFNotes.Text = string.Empty;
            tbGFNotes.Placeholder = "Optional";
        }

        private GroupMemberSummary LoadGroupFitnessMember( RockContext rockContext )
        {
            var groupFitnessGroupGuid = GetAttributeValue( AttributeKeys.GroupFitnessGroup ).AsGuid();
            var personId = SelectedPerson.Id;
            var groupFitnessSessionKey = GetAttributeValue( AttributeKeys.GroupFitnessCreditKey );
            var groupMemberEntityType = EntityTypeCache.Get( typeof( GroupMember ) );

            var groupService = new GroupService( rockContext );

            var group = groupService.Get( groupFitnessGroupGuid );

            if (group == null)
            {
                throw new Exception( "Group Fitness Group is not found." );
            }

            var groupIdAsString = group.Id.ToString();
            var attribute = new AttributeService( rockContext ).Queryable().AsNoTracking()
                .Where( a => a.EntityTypeId == groupMemberEntityType.Id )
                .Where( a => a.EntityTypeQualifierColumn == "GroupId" )
                .Where( a => a.EntityTypeQualifierValue == groupIdAsString )
                .Where( a => a.Key == groupFitnessSessionKey )
                .FirstOrDefault();

            if (attribute == null)
            {
                throw new Exception( "Group Fitness Session Attribute not found." );
            }

            var attributeValues = new AttributeValueService( rockContext ).Queryable().AsNoTracking()
                .Where( a => a.AttributeId == attribute.Id )
                .Where( a => a.Value != null && a.Value != "" );

            var groupMember = new GroupMemberService( rockContext ).Queryable().AsNoTracking()
                .GroupJoin( attributeValues, gm => gm.Id, av => av.EntityId,
                    ( gm, av ) => new GroupMemberSummary
                    {
                        Id = gm.Id,
                        PersonId = gm.PersonId,
                        GroupId = gm.GroupId,
                        Status = gm.GroupMemberStatus,
                        IsArchived = gm.IsArchived,
                        Sessions = av.Select( av1 => av1.ValueAsNumeric ).FirstOrDefault()
                    } )
                .Where( gm => gm.GroupId == group.Id )
                .Where( gm => gm.PersonId == personId )
                .FirstOrDefault();

            return groupMember;
        }

        private void LoadPINBadge()
        {
            var pinAuthenticationEntityType = EntityTypeCache.Get( typeof( PINAuthentication ) );
            var SportsPINPurposeDV = DefinedValueCache.Get( _sportsAndFitnessPINPurposeGuid.AsGuid() );
            var personId = SelectedPerson.Id;

            var purposeAttributeGuid = GetAttributeValue( AttributeKeys.LoginPINPurpose ).AsGuid();

            using (var rockContext = new RockContext())
            {
                var attributeValue = new AttributeValueService( rockContext ).Queryable()
                    .Where( av => av.Attribute.Guid == purposeAttributeGuid );

                var loginCount = new UserLoginService( rockContext ).Queryable()
                    .Join( attributeValue, u => u.Id, av => av.EntityId,
                        ( u, av ) => new { UserLoginId = u.Id, u.UserName, u.PersonId, u.EntityTypeId, purposeIds = av.Value } )
                    .Where( l => l.EntityTypeId == pinAuthenticationEntityType.Id )
                    .Where( l => l.PersonId == personId )
                    .ToList()
                    .Select( l => new { l.UserLoginId, l.UserName, purposes = l.purposeIds.SplitDelimitedValues().AsIntegerList() } )
                    .Where( l => l.purposes.Contains( SportsPINPurposeDV.Id ) )
                    .Count();

                hlblPIN.Text = string.Format( "{0} {1}", loginCount, "Login".PluralizeIf( loginCount != 1 ) );
                if (loginCount == 0)
                {
                    hlblPIN.LabelType = LabelType.Default;
                }
                else
                {
                    hlblPIN.LabelType = LabelType.Success;
                }
                hlblPIN.Visible = true;
            }
        }

        private void LoadPinModal()
        {

        }

        private void LoadChildcareCreditBadge()
        {
            var creditsAttributeKey = GetAttributeValue( AttributeKeys.ChildcareCreditKey );

            using (var rockContext = new RockContext())
            {
                var primaryFamilyId = SelectedPerson.PrimaryFamilyId;

                var familyGroup = new GroupService( rockContext ).Get( primaryFamilyId ?? 0 );

                familyGroup.LoadAttributes( rockContext );
                var credits = familyGroup.GetAttributeValue( creditsAttributeKey ).AsInteger();

                hlblChildcare.Text = string.Format( "{0} {1} remaining", credits, "Credit".PluralizeIf( Math.Abs( credits ) != 1 ) );
                if (credits > 0)
                {
                    hlblChildcare.LabelType = LabelType.Success;
                }
                else if (credits < 0)
                {
                    hlblChildcare.LabelType = LabelType.Danger;
                }
                else
                {
                    hlblChildcare.LabelType = LabelType.Default;
                }

                hlblChildcare.Visible = true;
            }
        }

        private void LoadChildcareCreditModal()
        {
            ClearChildcareModal();
            using (var rockcontext = new RockContext())
            {
                var person = new PersonService( rockcontext ).Get( SelectedPerson.Guid );
                var family = person.GetFamily( rockcontext );
                family.LoadAttributes( rockcontext );
                var credits = family.GetAttributeValue( GetAttributeValue( AttributeKeys.ChildcareCreditKey ) ).AsInteger();

                hfChildcareFamilyId.Value = family.Id.ToString();
                tbCCBeginningCredits.Text = credits.ToString();
            }
            mdChildcare.Show();
            upModals.Update();
        }

        private void LoadGroupFitnessSessionsBadge()
        {
            using (var rockContext = new RockContext())
            {
                var groupMember = LoadGroupFitnessMember( rockContext );

                if (groupMember == null)
                {
                    hlblGroupFitness.Text = "Not Enrolled";
                    hlblGroupFitness.LabelType = LabelType.Danger;
                }
                else if (groupMember.Status == GroupMemberStatus.Inactive)
                {
                    hlblGroupFitness.Text = "Inactive Member";
                    hlblGroupFitness.LabelType = LabelType.Warning;
                }
                else if (groupMember.IsArchived)
                {
                    hlblGroupFitness.Text = "Archived Member";
                    hlblGroupFitness.LabelType = LabelType.Warning;
                }
                else
                {
                    if (!groupMember.Sessions.HasValue || groupMember.Sessions.Value == 0)
                    {
                        hlblGroupFitness.Text = "No Sessions Remaining";
                        hlblGroupFitness.LabelType = LabelType.Default;
                    }
                    else
                    {
                        hlblGroupFitness.Text = string.Format( "{0:0} {1} remaining", groupMember.Sessions.Value, "Session".PluralizeIf( groupMember.Sessions.Value != 1 ) );
                        hlblGroupFitness.LabelType = LabelType.Success;
                    }
                }

                hlblGroupFitness.Visible = true;

            }


        }

        private void LoadGroupFitnessModal()
        {
            ClearGroupFitnessModel();
            var groupMember = LoadGroupFitnessMember( new RockContext() );


            if (groupMember == null)
            {
                nbGroupFitness.Title = "Not Enrolled";
                nbGroupFitness.Text = $"{SelectedPerson.NickName} is not enrolled in Group Fitness";
                nbGroupFitness.Visible = true;
            }
            else if (groupMember.Status == GroupMemberStatus.Inactive || groupMember.IsArchived)
            {
                nbGroupFitness.Title = "Inactive Participant";
                nbGroupFitness.Text = $"{SelectedPerson.NickName} is an inactive Group Fitness Participant.";
                nbGroupFitness.Visible = true;
            }

            if (groupMember != null)
            {
                hfGroupMemberId.Value = groupMember.Id.ToString();
                lGFBeginningCredits.Text = groupMember.Sessions.HasValue
                    ? string.Format( "{0:0}", groupMember.Sessions.Value ) : "0";
            }
            mdGroupFitness.Show();
            upModals.Update();
        }

        private void LoadSelectedPerson()
        {
            var personId = ViewState[_personIdViewStateKey].ToString().AsInteger();
            using (var rockContext = new RockContext())
            {
                _selectedPerson = new PersonService( rockContext ).Get( personId );
            }
        }

        private void RouteAction()
        {
            var sm = ScriptManager.GetCurrent( Page );

            if (Request.Form["__EVENTARGUMENT"] != null)
            {
                var action = Request.Form["__EVENTARGUMENT"];
                var personQS = new Dictionary<string, string>();
                personQS.Add( "Person", PageParameter( "Person" ) );
                switch (action.ToLower())
                {
                    case "update-pin":
                        LoadPinModal();
                        break;
                    case "add-childcare-credits":
                        LoadChildcareCreditModal();
                        break;
                    case "add-groupfitness-credits":
                        LoadGroupFitnessModal();
                        break;
                    case "view-sports-history":
                        NavigateToLinkedPage( AttributeKeys.SFHistory, personQS );
                        break;
                    case "view-groupfitness-history":
                        NavigateToLinkedPage( AttributeKeys.GFHistory, personQS );
                        break;
                    case "view-childcare-history":
                        NavigateToLinkedPage( AttributeKeys.ChildcareHistory, personQS );
                        break;

                }
            }
        }

        private void SaveGroupFitnessParticipant( int groupMemberId )
        {
            using (var rockContext = new RockContext())
            {
                var groupMemberService = new GroupMemberService( rockContext );
                var member = groupMemberService.Get( groupMemberId );

                if (groupMemberId == 0)
                {
                    var groupGuid = GetAttributeValue( AttributeKeys.GroupFitnessGroup ).AsGuid();
                    var groupFitnessGroup = new GroupService( rockContext )
                        .Queryable()
                        .Include( g => g.GroupType )
                        .Where( g => g.Guid == groupGuid )
                        .SingleOrDefault();


                    member = new GroupMember
                    {
                        GroupId = groupFitnessGroup.Id,
                        PersonId = SelectedPerson.Id,
                        GroupMemberStatus = GroupMemberStatus.Active,
                        GroupRoleId = groupFitnessGroup.GroupType.DefaultGroupRoleId ?? 0,
                        IsArchived = false
                    };
                    groupMemberService.Add( member );
                }

                if (member.IsArchived || member.GroupMemberStatus == GroupMemberStatus.Inactive)
                {
                    member.IsArchived = false;
                    member.ArchivedDateTime = null;
                    member.ArchivedByPersonAliasId = null;
                    member.GroupMemberStatus = GroupMemberStatus.Active;
                    member.InactiveDateTime = null;
                }

                rockContext.SaveChanges();

                var groupFitnessSessionAttributeKey = GetAttributeValue( AttributeKeys.GroupFitnessCreditKey );
                member.LoadAttributes( rockContext );
                var sessions = member.GetAttributeValue( groupFitnessSessionAttributeKey ).AsInteger();
                sessions += tbGFCreditsToAdd.Text.AsInteger();

                member.SetAttributeValue( groupFitnessSessionAttributeKey, sessions );
                member.SaveAttributeValue( groupFitnessSessionAttributeKey, rockContext );

                rockContext.SaveChanges();

                if (tbGFNotes.Text.IsNotNullOrWhiteSpace())
                {
                    var noteService = new NoteService( rockContext );
                    var noteType = NoteTypeCache.Get( GetAttributeValue( AttributeKeys.GroupFitnessNoteType ).AsGuid() );

                    if (noteType != null)
                    {
                        var note = new Note()
                        {
                            NoteTypeId = noteType.Id,
                            EntityId = member.Id,
                            CreatedByPersonAliasId = CurrentPersonAliasId,
                            Text = tbGFNotes.Text.Trim()
                        };

                        noteService.Add( note );
                        rockContext.SaveChanges();
                    }

                }

                LoadGroupFitnessSessionsBadge();
                mdGroupFitness.Hide();

                upMain.Update();

            }
        }

        public class GroupMemberSummary
        {
            public int Id { get; set; }
            public int PersonId { get; set; }
            public int GroupId { get; set; }
            public GroupMemberStatus Status { get; set; }
            public bool IsArchived { get; set; }
            public decimal? Sessions { get; set; }
        }

        #endregion
    }
}