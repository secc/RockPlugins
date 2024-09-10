using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;

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

        List<ActionCommands> _enabledActions = null;

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

        protected List<ActionCommands> EnabledActions
        {
            get
            {
                if(_enabledActions == null)
                {
                    _enabledActions = LoadEnabledActions();
                }
                return _enabledActions;
            }
            set
            {
                _enabledActions = value;
                ViewState[$"{this.BlockId}_EnabledActions"] = _enabledActions;
            }
        }

        

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            lbSavePIN.Click += lbSavePIN_Click;
            mdlAddCredits.SaveClick += mdlAddCredits_SaveClick;
            rptActions.ItemDataBound += rptActions_ItemDataBound;
        }



        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            nbPersonActions.Visible = false;


            if (!IsPostBack)
            {
                _creditType = null;
                LoadActionCommands();
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

        protected void rptActions_ItemCommand( object source, RepeaterCommandEventArgs e )
        {
            var lbAction = (LinkButton) e.Item.FindControl( "lbAction" );

            if(lbAction == null)
            {
                return;
            }

            var action = lbAction.CommandName.ToLower();
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

        private void rptActions_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            Literal lCount = (Literal) e.Item.FindControl( "lCount" );
            ActionCommands actionCmd = (ActionCommands) e.Item.DataItem;

            switch (actionCmd.CommandName.ToLower())
            {
                case "updatepin":
                    SetPINLabelText( actionCmd.Count, lCount );
                    lCount.Visible = true;
                    break;
                case "childcarecredit":
                    SetChildcareCreditText( actionCmd.Count, lCount );
                    lCount.Visible = true;
                    break;
                case "groupfitnesscredit":
                    SetGroupFitnessLabelText( actionCmd.Count, lCount );
                    lCount.Visible = true;
                    break;
                default:
                    lCount.Visible = false;
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
                //LoadPINHighlightLabel();
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

        private void LoadActionCommands()
        {
            rptActions.DataSource = EnabledActions.OrderBy( a => a.Order );
            rptActions.DataBind();
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

        private List<ActionCommands> LoadEnabledActions()
        {
            List<ActionCommands> actions = null;

            actions = (List<ActionCommands>) ViewState[$"{this.BlockId}_EnabledActions"];

            if(actions != null && actions.Any())
            {
                return actions;
            }


            actions = new List<ActionCommands>
            {
                new ActionCommands
                {
                    Name = "Manage PIN",
                    CommandName = "UpdatePIN",
                    IconCSS = "fas fa-hashtag",
                    Count = GetPINCount(),
                    Order = 0
                },
                new ActionCommands
                {
                    Name = "Childcare Credits",
                    CommandName = "ChildcareCredit",
                    IconCSS = "fas fa-coins",
                    Count = GetChildcareCreditCount(),
                    Order = 1
                },
                new ActionCommands
                {
                    Name = "Group Fitness Sessions",
                    CommandName = "GroupFitnessCredit",
                    IconCSS = "fas fa-coins",
                    Count = GetGroupFitnessSessionCount(),
                    Order = 2
                },
                new ActionCommands
                {
                    Name = "Sports &amp; Fitness History",
                    CommandName = "SportsAndFitnessHistory",
                    IconCSS = "fas fa-basketball-ball",
                    Count = null,
                    Order = 3
                },
                new ActionCommands
                {
                    Name = "Group Fitness History",
                    CommandName = "GroupFitnessHistory",
                    IconCSS = "fas fa-dumbell",
                    Count = null,
                    Order = 4
                },
                new ActionCommands
                {
                    Name = "View Childcare History",
                    CommandName = "ChildcareHistory",
                    IconCSS = "far fa-shapes",
                    Count = null,
                    Order = 5
                }
            };

            return actions;
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

        private int? GetChildcareCreditCount()
        {

            if (SelectedPerson == null || !SelectedPerson.PrimaryFamilyId.HasValue)
            {
                return null;
            }

            using (var rockContext = new RockContext())
            {
                var familyGroup = new GroupService( rockContext ).Get( SelectedPerson.PrimaryFamilyId.Value );

                familyGroup.LoadAttributes( rockContext );
                return familyGroup.GetAttributeValue( "SportsandFitnessChildcareCredit" ).AsInteger();

            }
        }

        private int? GetGroupFitnessSessionCount()
        {
 
            if (SelectedPerson == null)
            {
                return null;
                ;
            }

            using (var rockContext = new RockContext())
            {
                var gfGroup = new GroupService( rockContext ).Get( GetAttributeValue( AttributeKeys.GroupFitnessGroup ).AsGuid() );
                var groupMember = new GroupMemberService( rockContext ).Queryable()
                    .Where( g => g.GroupId == gfGroup.Id )
                    .Where( g => g.PersonId == SelectedPerson.Id )
                    .Where( g => g.GroupMemberStatus == GroupMemberStatus.Active )
                    .Where( g => !g.IsArchived )
                    .FirstOrDefault();

                if (groupMember == null)
                {
                    return null;
                }
                else
                {
                    groupMember.LoadAttributes( rockContext );
                    return groupMember.GetAttributeValue( "Sessions" ).AsInteger();
                }
            
            }
        }

        private int?  GetPINCount()
        {

            if(SelectedPerson == null)
            {
                return null;
            }


            var userLoginEntityType = EntityTypeCache.Get( typeof( UserLogin ) );
            var pinAuthenticationEntityType = EntityTypeCache.Get( typeof( PINAuthentication ) );
            var sfPINPurposeDV = DefinedValueCache.Get( new Guid( "e98517ec-1805-456b-8453-ef8480bd487f" ) );


            using (var rockContext = new RockContext())
            {
                var userLoginService = new UserLoginService( rockContext );
                var attributeValueService = new AttributeValueService( rockContext );

                var pinPurposeQry = attributeValueService.Queryable().AsNoTracking()
                    .Where( v => v.Attribute.EntityTypeId == userLoginEntityType.Id )
                    .Where( v => v.Attribute.Key == "PINPurpose" );

                return userLoginService.Queryable().AsNoTracking()
                    .Where( l => l.EntityTypeId == pinAuthenticationEntityType.Id )
                    .Where( l => l.PersonId == SelectedPerson.Id )
                    .Join( pinPurposeQry, l => l.Id, p => p.EntityId,
                        ( l, p ) => new { UserLogin = l, PurposeValue = p.Value } )
                    .Select( l => new { l.UserLogin.Id, l.PurposeValue } )
                    .ToList()
                    .SelectMany( l => l.PurposeValue.SplitDelimitedValues().AsIntegerList(),
                        ( l, p ) => new { l.Id, PurposeValue = p } )
                    .Where( p => p.PurposeValue == sfPINPurposeDV.Id )
                    .Count();


            }
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

                var action = EnabledActions.Where( a => a.CommandName.Equals( "childcarecredit", StringComparison.InvariantCultureIgnoreCase ) )
                    .SingleOrDefault();

                action.Count = GetChildcareCreditCount();
                LoadActionCommands();
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

                var action = EnabledActions
                    .Where( a => a.CommandName.Equals( "groupfitnesscredit", StringComparison.InvariantCultureIgnoreCase ) )
                    .SingleOrDefault();

                action.Count = GetGroupFitnessSessionCount();
                LoadActionCommands();
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
            var pinAction = EnabledActions.Where( a => a.CommandName.Equals("updatepin", StringComparison.InvariantCultureIgnoreCase) ).SingleOrDefault();
            pinAction.Count = GetPINCount();
            LoadActionCommands();
            return true;
        }


        private void SetChildcareCreditText(int? count, Literal l)
        {
            var labelCSS = "";
            var labelText = "";

            if(!count.HasValue)
            {
                count = 0;
            }

            labelText = $"{count} {(Math.Abs(count.Value) == 1 ? "Credit" : "Credits")} Remaining";

            if(count <= 0)
            {
                labelCSS = "label label-default";
            }
            else
            {
                labelCSS = "label label-success";
            }

            l.Text = $"<div class=\"{labelCSS}\">{labelText}</div>";

        }

        private void SetGroupFitnessLabelText(int? count, Literal l)
        {
            var labelCss = "";
            var labelText = "";

            if(!count.HasValue)
            {
                labelCss = "label label-warning";
                labelText = "Not Enrolled";
            }
            else if(count <= 0)
            {
                labelCss = "label label-default";
                labelText = $"{count} Credits Remaining";
            }
            else if(count > 0)
            {
                labelCss = "label label-success";
                labelText = $"{count} {(count == 1 ? "Credit" : "Credits")} Remaining";
            }

            l.Text = $"<div class=\"{labelCss}\">{labelText}</div>";

        }


        private void SetPINLabelText(int? count, Literal l)
        {

            var labelcss = "";
            var labelText = "";
            if(!count.HasValue)
            {
                count = 0;
            }

            if(count == 0)
            {
                labelcss = "label label-default";
                labelText = "No PIN Numbers";

            }
            else if(Math.Abs(count.Value) == 1)
            {
                labelText = $"{count} PIN Number";
                labelcss = $"label {( count == -1 ? "label-danger" : "label-success")}";
            }
            else
            {
                labelText = $"{count} PIN Numbers";
                labelcss = $"label label-success";
            }

            l.Text = $"<div class=\"{labelcss}\">{labelText}</div>";

        }

        public class PINSummary
        {
            public int UserLoginId { get; set; }
            public string PIN { get; set; }
            public int? PersonId { get; set; }
        }

        public class ActionCommands
        {
            public string Name { get; set; }
            public string CommandName { get; set; }
            public string IconCSS { get; set; }
            public int? Count { get; set; }
            public int Order { get; set; }
        }

        public enum CreditType
        {
            Childcare,
            GroupFitness
        }



       

    }
}