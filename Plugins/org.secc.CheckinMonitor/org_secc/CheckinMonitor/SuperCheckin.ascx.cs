using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Model;
using System.Web.UI.WebControls;
using Rock.Data;

namespace RockWeb.Plugins.org_secc.CheckinMonitor
{
    [DisplayName( "Super Checkin" )]
    [Category( "SECC > Check-in" )]
    [Description( "Advanced tool for managing checkin." )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Connection Status", "Connection status for new people." )]
    public partial class SuperCheckin : CheckInBlock
    {
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            if ( !KioskCurrentlyActive )
            {
                NavigateToHomePage();
                return;
            }
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( CurrentCheckInState == null )
            {
                NavigateToHomePage();
                return;
            }

            if ( !Page.IsPostBack )
            {
                if ( CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).Any() )
                {
                    ViewState.Add( "ExistingFamily", true );
                    pnlManageFamily.Visible = true;
                    ActivateFamily();
                    SaveViewState();
                }
                else
                {
                    ViewState.Add( "ExistingFamily", false );
                    pnlNewFamily.Visible = true;
                    SaveViewState();
                }
            }
            else
            {
                bool existingFamily = ( bool ) ViewState["ExistingFamily"];
                if ( existingFamily )
                {
                    DisplayFamilyMemberMenu();
                    BuildGroupTypeModal();
                }
            }
        }

        private void ActivateFamily()
        {
            List<string> errorMessages = new List<string>();
            ProcessActivity( GetAttributeValue( "WorkflowActivity" ), out errorMessages );
            if ( errorMessages.Any() )
            {
                NavigateToPreviousPage();
            }
            DisplayFamilyMemberMenu();
            DisplayPersonInformation();

        }

        private void DisplayFamilyMemberMenu()
        {
            phFamilyMembers.Controls.Clear();

            foreach ( var checkinPerson in CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).First().People )
            {
                BootstrapButton btnMember = new BootstrapButton();
                btnMember.CssClass = "btn btn-default btn-block btn-lg";
                btnMember.Text = "<b>" + checkinPerson.Person.FullName + " (" + GetSelectedCount( checkinPerson ).ToString() + ")</b><br>" + checkinPerson.Person.FormatAge();
                btnMember.ID = checkinPerson.Person.Id.ToString();
                btnMember.Click += ( s, e ) =>
                {
                    ViewState["SelectedPersonId"] = checkinPerson.Person.Id;
                    SaveViewState();
                    DisplayPersonInformation();
                };
                phFamilyMembers.Controls.Add( btnMember );
            }
        }

        private int GetSelectedCount( CheckInPerson checkinPerson )
        {
            return checkinPerson.GroupTypes
                .SelectMany( gt => gt.Groups )
                .SelectMany( g => g.Locations )
                .SelectMany( l => l.Schedules )
                .Where( s => s.Selected ).Count();
        }

        private void DisplayPersonInformation()
        {
            int selectedPersonId;
            if ( ViewState["SelectedPersonId"] != null )
            {
                selectedPersonId = ( int ) ViewState["SelectedPersonId"];
                var checkinPerson = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).First().People.Where( p => p.Person.Id == selectedPersonId ).First();
                pnlPersonInformation.Visible = true;
                pnlAddPerson.Visible = false;
                ltName.Text = checkinPerson.Person.FullName;
            }
        }

        protected void btnCheckin_Click( object sender, EventArgs e )
        {
            int selectedPersonId;
            if ( ViewState["SelectedPersonId"] != null )
            {
                selectedPersonId = ( int ) ViewState["SelectedPersonId"];
                var checkinPerson = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).First().People.Where( p => p.Person.Id == selectedPersonId ).First();
                CheckinModal();
            }
        }

        private void CheckinModal()
        {
            BuildGroupTypeModal();
            mdCheckin.Show();
        }

        private void BuildGroupTypeModal()
        {
            if ( ViewState["SelectedPersonId"] == null )
            {
                return;
            }
            var selectedPersonId = ( int ) ViewState["SelectedPersonId"];
            var checkinPerson = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).First().People.Where( p => p.Person.Id == selectedPersonId ).First();

            phCheckin.Controls.Clear();

            foreach ( var groupType in checkinPerson.GroupTypes )
            {
                //ignore group types with no non-excluded groups on non-super checkin
                if ( !cbSuperCheckin.Checked && !groupType.Groups.Where( g => !g.ExcludedByFilter ).Any() )
                {
                    continue;
                }

                PanelWidget panelWidget = new PanelWidget();
                panelWidget.Title = groupType.GroupType.Name;
                panelWidget.ID = groupType.GroupType.Guid.ToString();
                phCheckin.Controls.Add( panelWidget );
                foreach ( var group in groupType.Groups )
                {
                    if ( !cbSuperCheckin.Checked && group.ExcludedByFilter )
                    {
                        continue;
                    }
                    PanelWidget groupWidget = new PanelWidget();
                    groupWidget.Title = group.Group.Name;
                    if ( group.ExcludedByFilter )
                    {
                        groupWidget.Title = "<i class='fa fa-exclamation-triangle'></i> " + groupWidget.Title;
                    }
                    groupWidget.ID = group.Group.Guid.ToString();
                    panelWidget.Controls.Add( groupWidget );
                    foreach ( var location in group.Locations )
                    {
                        if ( !cbSuperCheckin.Checked && !location.Location.IsActive )
                        {
                            continue;
                        }

                        foreach ( var schedule in location.Schedules )
                        {
                            if ( !cbSuperCheckin.Checked && !schedule.Schedule.IsCheckInActive )
                            {
                                continue;
                            }

                            BootstrapButton btnSelect = new BootstrapButton();
                            btnSelect.Text = location.Location.Name + ": " + schedule.Schedule.Name;
                            if ( !location.Location.IsActive )
                            {
                                btnSelect.Text += " [Location Not Active]";
                            }

                            if ( !schedule.Schedule.IsCheckInActive )
                            {
                                btnSelect.Text += " [Schedule Not Active]";
                            }
                            btnSelect.ID = location.Location.Guid.ToString() + schedule.Schedule.Guid.ToString();
                            if ( schedule.Selected )
                            {
                                btnSelect.CssClass = "btn btn-success btn-block";
                                panelWidget.Expanded = true;
                                groupWidget.Expanded = true;
                            }
                            else
                            {
                                btnSelect.CssClass = "btn btn-default btn-block";
                            }
                            btnSelect.Click += ( s, e ) =>
                             {
                                 schedule.Selected = !schedule.Selected;
                                 SaveState();
                                 BuildGroupTypeModal();
                             };

                            groupWidget.Controls.Add( btnSelect );
                        }
                    }
                }
            }
        }

        protected void btnPrint_Click( object sender, EventArgs e )
        {

        }

        protected void btnBack_Click( object sender, EventArgs e )
        {
            NavigateToPreviousPage();
        }

        protected void cbSuperCheckin_CheckedChanged( object sender, EventArgs e )
        {
            BuildGroupTypeModal();
        }

        protected void mdCheckin_SaveClick( object sender, EventArgs e )
        {
            mdCheckin.Hide();
        }

        protected void btnNewMember_Click( object sender, EventArgs e )
        {
            ShowAddNewPerson();
        }

        private void ShowAddNewPerson()
        {
            pnlPersonInformation.Visible = false;
            pnlAddPerson.Visible = true;

            ddlNewPersonSuffix.BindToDefinedType( DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSON_SUFFIX.AsGuid() ), true );

            rblNewPersonGender.Items.Clear();
            rblNewPersonGender.Items.Add( new ListItem( Gender.Male.ConvertToString(), Gender.Male.ConvertToInt().ToString() ) );
            rblNewPersonGender.Items.Add( new ListItem( Gender.Female.ConvertToString(), Gender.Female.ConvertToInt().ToString() ) );
            rblNewPersonGender.Items.Add( new ListItem( Gender.Unknown.ConvertToString(), Gender.Unknown.ConvertToInt().ToString() ) );
        }

        protected void btnSaveAddPerson_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            Person person = new Person();
            person.FirstName = tbNewPersonFirstName.Text;
            person.LastName = tbNewPersonLastName.Text;
            person.SetBirthDate( dpNewPersonBirthDate.Text.AsDateTime() );
            person.ConnectionStatusValueId = DefinedValueCache.Read( GetAttributeValue( "ConnectionStatus" ).AsGuid() ).Id;

            var familyGroupType = GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY );
            var adultRoleId = familyGroupType.Roles
                .Where( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ) )
                .Select( r => r.Id )
                .FirstOrDefault();
            var childRoleId = familyGroupType.Roles
                .Where( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() ) )
                .Select( r => r.Id )
                .FirstOrDefault();
            var age = person.Age;
            int familyRoleId = age.HasValue && age < 18 ? childRoleId : adultRoleId;

            var family = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault().Group;
            if ( cbRelationship.Checked )
            {
                PersonService.AddPersonToFamily( person, true, family.Id, familyRoleId, rockContext );
            }
            else
            {
                //save the person with new family
                var newFamily = PersonService.SaveNewPerson( person, rockContext );

                //create connection
                var memberService = new GroupMemberService( rockContext );
                var adultFamilyMembers = memberService.Queryable()
                    .Where( m =>
                        m.GroupId == family.Id
                        && m.GroupRoleId == adultRoleId
                        )
                        .Select( m => m.Person )
                        .ToList();

                foreach ( var member in adultFamilyMembers )
                {
                    Person.CreateCheckinRelationship( member.Id, person.Id, rockContext );
                }
                //rockContext.SaveChanges();
            }
        }
    }
}