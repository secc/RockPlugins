using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using CSScriptLibrary;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security.Authentication;
using Rock.Web.Cache;
using Rock.Web.UI;



namespace RockWeb.Plugins.org_secc.SportsAndFitness.ControlCenter
{
    [DisplayName( "Control Center Person Actions" )]
    [Category( "Sports and Fitness > Control Center" )]
    [Description( "Person Actions for Sports and Fitness Control Center" )]

    [LinkedPage( "Sports & Fitness History",
        Description = "Link to the Sports and Fitness History Page",
        IsRequired = true,
        Category = "Linked Pages",
        Key = AttributeKeys.SFHistoryPage )]
    [LinkedPage( "Group Fitness History",
        Description = "Link to the Group Fitness History Page",
        IsRequired = true,
        Category = "Linked Pages",
        Key = AttributeKeys.GroupFitnessHistoryPage )]
    [LinkedPage( "Childcare History",
        Description = "Link to the Childcare History Page.",
        IsRequired = true,
        Category = "Linked Pages",
        Key = AttributeKeys.ChildcareHistoryPage )]

    [GroupField("Group Fitness Group",
        Description = "Group that includes people who are signed up for Group Fitness.",
        IsRequired = true,
        Key = AttributeKeys.GroupFitnessGroup)]

    public partial class PersonActions : RockBlock
    {
        public class AttributeKeys
        {
            public const string SFHistoryPage = "SFHistoryPage";
            public const string GroupFitnessGroup = "GroupFitnessGroup";
            public const string GroupFitnessHistoryPage = "GFHistoryPage";
            public const string ChildcareHistoryPage = "ChildcareHistoryPage";

        }

        Person _selectedPerson = null;

        CreditType? _creditType = null;

        protected Person SelectedPerson
        {
            get
            {
                if (_selectedPerson == null)
                {
                    _selectedPerson = GetSelectedPerson();
                }
                return _selectedPerson;
            }
        }

        protected CreditType? SelectedCreditType
        {
            get
            {
                if(_creditType == null)
                {
                    _creditType = (CreditType?) ViewState[$"{this.BlockId}_CreditType"];
                }

                return _creditType;
            }
            set
            {
                _creditType = value;
                ViewState[$"{this.BlockId}_CreditType"] = _creditType;
            }
        }

        

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            lbUpdatePin.Click += PersonAction_Click;
            lbChildcareCredits.Click += PersonAction_Click;
            lbGroupFitnessCredits.Click += PersonAction_Click;
            lbSportsAndFitnessHistory.Click += PersonAction_Click;
            lbGroupFitnessHistory.Click += PersonAction_Click;
            lbChildcareHistory.Click += PersonAction_Click;
            lbSavePIN.Click += lbSavePIN_Click;
            mdlAddCredits.SaveClick += mdlAddCredits_SaveClick;
        }



        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            nbPersonActions.Visible = false;

            if(!IsPostBack)
            {
                _creditType = null;
            }
        }

        private void lbSavePIN_Click( object sender, EventArgs e )
        {
            var personId = SelectedPerson.Id;
            var pin = tbPIN.Text.Trim();

            if(SavePIN(personId, pin))
            {
                LoadPINList();
            }

        }


        protected void mdlPINs_SaveClick( object sender, EventArgs e )
        {
            mdlPINs.Hide();
        }

        private void mdlAddCredits_SaveClick( object sender, EventArgs e )
        {
            switch (SelectedCreditType)
            {
                case CreditType.Childcare:
                    SaveChildcareCredits();
                    break;
                case CreditType.GroupFitness:
                    SaveGroupFitnessCredits();
                    break;
            }
            mdlAddCredits.Hide();
        }

        private void PersonAction_Click( object sender, EventArgs e )
        {
            var action = ((LinkButton) sender).CommandName.ToLower();

            switch (action)
            {
                case "updatepin":
                    LoadPINList();
                    mdlPINs.Show();
                    break;
                case "childcarecredit":
                    LoadCreditModel( CreditType.Childcare );
                    break;
                case "groupfitnesscredit":
                    LoadCreditModel( CreditType.GroupFitness );
                    break;
                case "sportsandfitnesshistory":
                    LoadPage( AttributeKeys.SFHistoryPage, true );
                    break;
                case "groupfitnesshistory":
                    LoadPage( AttributeKeys.GroupFitnessHistoryPage, true );
                    break;
                case "childcarehistory":
                    LoadPage( AttributeKeys.ChildcareHistoryPage, true );
                    break;
                default:
                    break;
            }
        }
        protected void rptPIN_ItemCommand( object source, RepeaterCommandEventArgs e )
        {
            if(e.CommandName == "DeletePIN")
            {
                DeletePIN( e.CommandArgument.ToString().AsInteger() );
            }
        }

        protected void rptPIN_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {

            var item = e.Item.DataItem as PINSummary;
            LinkButton lbRemove = e.Item.FindControl( "lbDeletePIN" ) as LinkButton;
            if(lbRemove != null)
            {
                lbRemove.CommandArgument = item.UserLoginId.ToString();
            }


        }

        private void DeletePIN(int id)
        {
            var sportsPINDV = DefinedValueCache.Get( new Guid( "e98517ec-1805-456b-8453-ef8480bd487f" ) );

            if (id <= 0)
            {
                return;
            }
            using (var rockContext = new RockContext())
            {
                var loginService = new UserLoginService( rockContext );
                var login = loginService.Get( id );
                if(login == null)
                {
                    return;
                }

                login.LoadAttributes( rockContext );
                var pinTypesIds = login.GetAttributeValue( "PINPurpose" ).SplitDelimitedValues()
                    .Select( v => v.AsInteger() )
                    .ToList();

                if(pinTypesIds.Count() > 1 && pinTypesIds.Contains(sportsPINDV.Id))
                {
                    pinTypesIds.Remove( sportsPINDV.Id );
                    login.SetAttributeValue( "PINPurpose", string.Join( "|", pinTypesIds ) );
                    login.SaveAttributeValue( "PINPurpose" );
                }
                else if(pinTypesIds.Contains(sportsPINDV.Id))
                {
                    loginService.Delete( login );
                }

                rockContext.SaveChanges();
                LoadPINList();
            }
        }


        private Person GetSelectedPerson()
        {
            using (var rockContext = new RockContext())
            {
                var person = new PersonService( rockContext ).Get( PageParameter( "Person" ).AsInteger() );

                if (person != null)
                {
                    person.LoadAttributes( rockContext );
                }
                return person;
            }
        }

        private void LoadChildcareModel()
        {
            if(SelectedPerson == null)
            {
                return;
            }
            mdlAddCredits.Title = "Add Childcare Credits";

            var familyGroupId = SelectedPerson.PrimaryFamilyId;
            if(!familyGroupId.HasValue)
            {
                nbPersonActions.Title = "<strong>Family Not Found</strong>";
                nbPersonActions.Text = $"<p>Please contact Rock Support {SelectedPerson.FullName}'s family not found.";
                nbPersonActions.Visible = true;
                return;
            }

            using (var rockContext = new RockContext())
            {
                var groupService = new GroupService( rockContext );
                var family = groupService.Get( familyGroupId.Value );
                family.LoadAttributes();
                var credits = family.GetAttributeValue( "SportsandFitnessChildcareCredit" ).AsInteger();

                tbExistingCredits.Text = credits.ToString();
            }

            mdlAddCredits.Show();

        }

        private void LoadCreditModel(CreditType ct)
        {
            SelectedCreditType = ct;
            tbExistingCredits.Text = string.Empty;
            tbCreditsToAdd.Text = string.Empty;

            switch (SelectedCreditType)
            {
                case CreditType.Childcare:
                    LoadChildcareModel();
                    break;
                case CreditType.GroupFitness:
                    LoadGroupFitnessModel();
                    break;
                default:
                    break;
            }
           
        }

        private void LoadGroupFitnessModel()
        {
            if(SelectedPerson == null)
            {
                return;
            }
            mdlAddCredits.Title = "Add Group Fitness Credits";
            var groupGuid = GetAttributeValue( AttributeKeys.GroupFitnessGroup ).AsGuid();

            using (var rockContext = new RockContext())
            {
                var groupMemberService = new GroupMemberService( rockContext );
                var groupMember = groupMemberService.Queryable()
                    .Where( m => m.PersonId == SelectedPerson.Id )
                    .Where( m => m.Group.Guid == groupGuid )
                    .FirstOrDefault();

                if(groupMember == null)
                {
                    tbExistingCredits.Text = "0";
                }
                else
                {
                    groupMember.LoadAttributes( rockContext );
                    var existingCredits = groupMember.GetAttributeValue( "Sessions" ).AsInteger();
                    tbExistingCredits.Text = existingCredits.ToString();
                }
            }

            mdlAddCredits.Show();

        }


        private void LoadPage( string linkedPageAttributeKey, bool includePerson )
        {
            var qsValues = new Dictionary<string, string>();

            if (includePerson)
            {
                qsValues.Add( "Person", SelectedPerson.Id.ToString() );
            }

            this.NavigateToLinkedPage( linkedPageAttributeKey, qsValues );
        }

        private void LoadPINList()
        { 
            if(SelectedPerson == null)
            {
                return;
            }

            nbPIN.Text = string.Empty;
            nbPIN.Title = string.Empty;
            nbPIN.Visible = false;
            tbPIN.Text = string.Empty;

            var pinAuthenticationET = EntityTypeCache.Get( typeof( PINAuthentication ) );
            var userloginET = EntityTypeCache.Get( typeof( UserLogin ) );
            var sportsPINDV = DefinedValueCache.Get( new Guid( "e98517ec-1805-456b-8453-ef8480bd487f" ) );

            using (var rockContext = new RockContext())
            {
                var purposeQry = new AttributeValueService( rockContext ).Queryable().AsNoTracking()
                    .Where( a => a.Attribute.EntityTypeId == userloginET.Id )
                    .Where( a => a.Attribute.Key == "PINPurpose" );


                var logins = new UserLoginService( rockContext ).Queryable().AsNoTracking()
                    .Where( l => l.EntityTypeId == pinAuthenticationET.Id )
                    .Where( l => l.PersonId == SelectedPerson.Id )
                    .Join( purposeQry, l => l.Id, p => p.EntityId,
                        ( l, p ) => new { UserLogin = l, PurposeValue = p.Value } )
                    .Select( l => new { l.UserLogin.Id, l.UserLogin.UserName, l.UserLogin.PersonId, l.PurposeValue } )
                    .ToList()
                    .SelectMany( l => l.PurposeValue.SplitDelimitedValues(),
                        ( l, v ) => new { l.Id, l.UserName, l.PersonId, PurposeValue = v.AsInteger() } )
                    .Where( l => l.PurposeValue == sportsPINDV.Id )
                    .Select(l => new PINSummary
                    {
                        PersonId = l.PersonId,
                        PIN = l.UserName,
                        UserLoginId = l.Id
                    } )
                    .ToList();

                rptPIN.DataSource = logins;
                rptPIN.DataBind();
            }


        }

        private void SaveChildcareCredits()
        {
            if(SelectedPerson == null)
            {
                return;
            }

            var familyid = SelectedPerson.PrimaryFamilyId;

            using (var rockContext = new RockContext())
            {
                var family = new GroupService( rockContext ).Get( familyid.Value );
                family.LoadAttributes( rockContext );
                var credits = family.GetAttributeValue( "SportsandFitnessChildcareCredit" ).AsInteger();
                credits += tbCreditsToAdd.Text.AsInteger();

                family.SetAttributeValue( "SportsandFitnessChildcareCredit", credits );
                family.SaveAttributeValue( "SportsandFitnessChildcareCredit" );
                rockContext.SaveChanges();
                tbCreditsToAdd.Text = "0";
            }
        }

        private void SaveGroupFitnessCredits()
        {
            if(SelectedPerson == null)
            {
                return;
            }

            var groupFitnessGroupGuid = GetAttributeValue( AttributeKeys.GroupFitnessGroup ).AsGuid();

            using (var rockContext = new RockContext())
            {
                var groupService = new GroupService( rockContext );

                var groupMemberService = new GroupMemberService( rockContext );
                var groupFitnessGroup = groupService.Get( groupFitnessGroupGuid );


                var groupMember = groupMemberService.Queryable()
                    .Where( m => m.PersonId == SelectedPerson.Id )
                    .Where( m => m.GroupId == groupFitnessGroup.Id )
                    .FirstOrDefault();

                var defaultRoleId = GroupTypeCache.Get( groupFitnessGroup.GroupTypeId ).DefaultGroupRoleId;

                if(groupMember == null)
                {
                    groupMember = new GroupMember
                    {
                        GroupId = groupFitnessGroup.Id,
                        PersonId = SelectedPerson.Id,
                        GroupMemberStatus = GroupMemberStatus.Active,
                        Guid = Guid.NewGuid(),
                        IsArchived = false,
                        CreatedByPersonAliasId = CurrentPersonAliasId,
                        ModifiedByPersonAliasId = CurrentPersonAliasId,
                        GroupRoleId = defaultRoleId.Value
                    };

                    groupMemberService.Add( groupMember );
                    rockContext.SaveChanges();
                }

                if(groupMember.GroupMemberStatus != GroupMemberStatus.Active)
                {
                    groupMember.GroupMemberStatus = GroupMemberStatus.Active;
                    groupMember.InactiveDateTime = null;
                    groupMember.ModifiedByPersonAliasId = CurrentPersonAliasId;
                }

                if(groupMember.IsArchived)
                {
                    groupMember.IsArchived = false;
                    groupMember.ModifiedByPersonAliasId = CurrentPersonAliasId;
                }

                groupMember.LoadAttributes( rockContext );
                var sessions = groupMember.GetAttributeValue( "Sessions" ).AsInteger();
                sessions += tbCreditsToAdd.Text.AsInteger();
                groupMember.SetAttributeValue( "Sessions", sessions );
                groupMember.SaveAttributeValue( "Sessions" );

                rockContext.SaveChanges();
                tbCreditsToAdd.Text = "0";
            }
        }

        private bool SavePIN(int personId, string pin)
        {
            List<string> errors = new List<string>();
            if(personId <= 0)
            {
                errors.Add( "Person Id is not valid." );

            }
            var pinRegex = @"^\d+$";
            if (!System.Text.RegularExpressions.Regex.IsMatch( pin, pinRegex ))
            {
                errors.Add( "PIN must be numeric." );
            }

            if(pin.Length <4)
            {
                errors.Add( "PIN must be at least 4 characters long." );
            }

            var pinEntityType = EntityTypeCache.Get( typeof( PINAuthentication ) );
            var sportsPINDefinedValue = DefinedValueCache.Get( new Guid( "e98517ec-1805-456b-8453-ef8480bd487f" ) );

            using (var rockContext = new RockContext())
            {
                var loginService = new UserLoginService( rockContext );
                var loginExists = loginService.Queryable().AsNoTracking()
                    .Where( l => l.UserName == pin )
                    .Where( l => l.EntityTypeId == pinEntityType.Id )
                    .Any();

                if (loginExists)
                {
                    errors.Add( "PIN Already Exists. Please try a new PIN." );
                }

                if(!errors.Any())
                {
                    var userLogin = new UserLogin
                    {
                        PersonId = personId,
                        UserName = pin,
                        EntityTypeId = pinEntityType.Id,
                        IsConfirmed = true,
                        IsPasswordChangeRequired = false,
                        IsLockedOut = false,
                        CreatedByPersonAliasId = CurrentPersonAliasId,
                        ModifiedByPersonAliasId = CurrentPersonAliasId,
                        Guid = Guid.NewGuid()
                    };
                    loginService.Add( userLogin );
                    rockContext.SaveChanges();

                    userLogin.LoadAttributes( rockContext );
                    userLogin.SetAttributeValue( "PINPurpose", sportsPINDefinedValue.Id );
                    userLogin.SaveAttributeValue( "PINPurpose" );

                    rockContext.SaveChanges();
                }
            }

            if(errors.Any())
            {
                nbPIN.Title = "<strong>Unable to Save PIN</strong>";
                var sb = new StringBuilder();
                sb.Append( "<ul>" );
                foreach (var error in errors)
                {
                    sb.Append( $"<li>{error}</li>" );
                }
                sb.Append( "</ul>" );
                nbPIN.Text = sb.ToString();
                nbPIN.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Validation;
                nbPIN.Visible = true;
                return false;
            }

            return true;
        }

        public class PINSummary
        {
            public int UserLoginId { get; set; }
            public string PIN { get; set; }
            public int? PersonId { get; set; }
        }

        public enum CreditType
        {
            Childcare,
            GroupFitness
        }


    }
}