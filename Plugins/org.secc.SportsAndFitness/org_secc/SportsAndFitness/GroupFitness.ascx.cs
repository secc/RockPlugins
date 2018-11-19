// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
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

using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Web.UI.Controls;
using Rock.Web.Cache;
using System.Web.UI.WebControls;
using System.Web.UI;
using Rock.Model;
using Rock.Data;
using org.secc.FamilyCheckin.Utilities;
using System.Data.Entity;

namespace RockWeb.Plugins.org_secc.SportsAndFitness
{
    [DisplayName( "Group Fitness Checkin" )]
    [Category( "SECC > Check-in" )]
    [Description( "Block to check into group fitness." )]
    [TextField( "Process Activity", "Action to search", true, "Process" )]
    [TextField( "Checkin Activity", "Activity for completing checkin.", true, "Checkin" )]
    [TextField( "Sessions Attribute Key", "Attribute key which contains the session count", true, "Sessions" )]
    [GroupField( "Group Fitness Parent Group", "Group which contains all group fitness groups. Needed for reading the number of sessions left.", true )]
    [IntegerField( "Minimum Digits", "Minimum number of digits required.", true, 7 )]

    public partial class GroupFitness : CheckInBlock
    {

        private RockContext _rockContext;
        private string _groupSessionsKey;

        private List<GroupTypeCache> parentGroupTypesList;
        private GroupTypeCache currentParentGroupType;

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RockPage.AddScriptLink( "~/Scripts/CheckinClient/cordova-2.4.0.js", false );
            RockPage.AddScriptLink( "~/Scripts/CheckinClient/ZebraPrint.js" );

            _rockContext = new RockContext();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( CurrentCheckInState == null )
            {
                NavigateToPreviousPage();
                return;
            }

            _groupSessionsKey = GetAttributeValue( "SessionsAttributeKey" );

            btnCheckin.Visible = false;

            if ( !Page.IsPostBack )
            {
                this.Page.Form.DefaultButton = lbSearch.UniqueID;

                CurrentCheckInState.CheckIn = new CheckInStatus();
                SaveState();
            }
            else
            {
                if ( CurrentCheckInState.CheckIn.Families.Any() )
                {
                    if ( CurrentCheckInState.CheckIn.CurrentPerson != null )
                    {
                        ShowPersonCheckin();
                    }
                    else
                    {
                        ShowPeopleList();
                    }
                }
            }
        }

        /// <summary>
        /// Search button handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void lbSearch_Click( object sender, EventArgs e )
        {
            if ( CurrentCheckInState == null )
            {
                NavigateToPreviousPage();
                return;
            }

            if ( string.IsNullOrWhiteSpace( tbPhone.Text ) )
            {
                return;
            }

            if ( tbPhone.Text.Trim().Length < ( GetAttributeValue( "MinimumDigits" ).AsIntegerOrNull() ?? 7 ) )
            {
                maError.Show( "Please enter at least " + ( GetAttributeValue( "MinimumDigits" ) + "digits." ), ModalAlertType.Information );
                return;
            }
            CurrentCheckInState.CheckIn = new CheckInStatus();
            CurrentCheckInState.CheckIn.SearchValue = tbPhone.Text;
            CurrentCheckInState.CheckIn.SearchType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER );
            List<string> errors = new List<string>();
            try
            {
                bool test = ProcessActivity( GetAttributeValue( "WorkflowActivity" ), out errors );
            }
            catch
            {
                NavigateToPreviousPage();
                Response.End();
                return;
            }
            if ( CurrentCheckInState.CheckIn.Families.Any() )
            {
                if ( CurrentCheckInState.CheckIn.Families.Count() == 1 )
                {
                    var family = CurrentCheckInState.CheckIn.Families.FirstOrDefault();
                    if ( family == null )
                    {
                        ShowPersonNotFound();
                        return;
                    }
                    family.Selected = true;
                    var person = family.People.FirstOrDefault();
                    if ( person == null )
                    {
                        person.Selected = true;
                        ShowPersonNotFound();
                        return;
                    }
                    person.Selected = true;
                    ShowPersonCheckin();
                }
                else
                {
                    ShowPeopleList();
                }
            }
            else
            {
                ShowPersonNotFound();
            }

        }

        /// <summary>
        /// Generates a dynamic list of people to be selected as the check-in person
        /// </summary>
        private void ShowPeopleList()
        {
            pnlMessage.Visible = false;
            phPeople.Controls.Clear();
            foreach ( var family in CurrentCheckInState.CheckIn.Families )
            {
                foreach ( var person in family.People )
                {
                    BootstrapButton btnPerson = new BootstrapButton();
                    btnPerson.Text = person.Person.FullName;
                    btnPerson.CssClass = "btn btn-default btn-lg btn-block";
                    btnPerson.ID = "btnPerson" + person.Person.Guid.ToString();
                    btnPerson.Click += ( s, e ) =>
                    {
                        family.Selected = true;
                        person.Selected = true;
                        SaveState();
                        ShowPersonCheckin();
                    };
                    phPeople.Controls.Add( btnPerson );
                }
            }
            BootstrapButton btnCancel = new BootstrapButton();
            btnCancel.ID = "btnPhoneCancel";
            btnCancel.CssClass = "btn btn-danger btn-lg btn-block";
            btnCancel.Text = "Cancel";
            btnCancel.Click += ( s, e ) =>
            {
                if ( CurrentCheckInState == null )
                {
                    NavigateToPreviousPage();
                    return;
                }
                tbPhone.Text = "";
                CurrentCheckInState.CheckIn = new CheckInStatus();
                pnlMessage.Visible = true;
                phPeople.Controls.Clear();
            };
            phPeople.Controls.Add( btnCancel );
        }

        /// <summary>
        /// Runs the process workflow and generated the dynamic controls
        /// </summary>
        private void ShowPersonCheckin()
        {
            phClasses.Controls.Clear();

            if ( CurrentCheckInState == null )
            {
                NavigateToPreviousPage();
                return;
            }

            pnlNotFound.Visible = false;
            pnlSearch.Visible = false;
            pnlCheckin.Visible = true;
            var person = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).SelectMany( f => f.People ).Where( p => p.Selected ).FirstOrDefault();
            if ( person == null )
            {
                maError.Show( "There was an error processing your request.", ModalAlertType.Warning );
                return;
            }
            ltNickName.Text = person.Person.NickName;
            var groups = GetMembershipGroups();
            if ( !groups.Any() )
            {
                maError.Show( "No group fitness groups found.", ModalAlertType.Warning );
                return;
            }

            List<string> errors = new List<string>();
            try
            {
                bool test = ProcessActivity( GetAttributeValue( "ProcessActivity" ), out errors );
            }
            catch
            {
                NavigateToPreviousPage();
                Response.End();
                return;
            }

            GroupMemberService groupMemberService = new GroupMemberService( _rockContext );
            int groupCount = 0;
            int checkinCount = 0;
            foreach ( var group in groups )
            {
                var groupMember = new GroupMemberService( _rockContext ).GetByGroupIdAndPersonId( group.Id, person.Person.Id ).FirstOrDefault();
                if ( groupMember != null )
                {
                    Panel pnlWell = new Panel();
                    pnlWell.CssClass = "well";
                    phClasses.Controls.Add( pnlWell );

                    groupMember.LoadAttributes();
                    int sessions = groupMember.GetAttributeValue( _groupSessionsKey ).AsInteger();
                    if ( sessions > 0 )
                    {
                        var checkinGroups = CurrentCheckInState.CheckIn.CurrentPerson
                            .GroupTypes.SelectMany( gt => gt.Groups )
                            .Where( g => g.Group.GetAttributeValue( "Group" ).AsGuid() == group.Guid )
                            .ToList();
                        if ( checkinGroups.Any() )
                        {
                            Literal sessionCount = new Literal();
                            sessionCount.Text = string.Format( "<h2>You have {0} {1} sessions remaining</h2>", sessions, group.Name );
                            pnlWell.Controls.Add( sessionCount );

                            foreach ( var chGroup in checkinGroups )
                            {
                                foreach ( var location in chGroup.Locations )
                                {
                                    foreach ( var schedule in location.Schedules )
                                    {
                                        Panel pnlClass = new Panel();
                                        pnlClass.Style.Add( "margin", "5px" );
                                        pnlWell.Controls.Add( pnlClass );

                                        BootstrapButton btnSelect = new BootstrapButton();
                                        if ( schedule.Selected )
                                        {
                                            btnSelect.Text = "<i class='fa fa-check-square-o'></i>";
                                            btnSelect.CssClass = "btn btn-success btn-lg";
                                        }
                                        else
                                        {
                                            btnSelect.Text = "<i class='fa fa-square-o'></i>";
                                            btnSelect.CssClass = "btn btn-default btn-lg";
                                        }
                                        btnSelect.ID = "s" + chGroup.Group.Id.ToString() + location.Location.Id.ToString() + schedule.Schedule.Id.ToString();
                                        btnSelect.Click += ( s, e ) => { ToggleClass( chGroup, location, schedule, schedule.Selected ); };
                                        pnlWell.Controls.Add( btnSelect );

                                        Literal ltClassName = new Literal();
                                        ltClassName.Text = string.Format( "<span class='classText'> {0}: {1} in {2}</span>",
                                            chGroup.Group.Name,
                                            schedule.Schedule.Name,
                                            location.Location.Name );
                                        pnlWell.Controls.Add( ltClassName );
                                        checkinCount++;
                                    }
                                }
                            }
                        }
                        else
                        {
                            Literal ltNoActiveSessions = new Literal();
                            ltNoActiveSessions.Text = string.Format( "<h2>You have {0} {1} sessions remaining, but there are no active check-ins.</h2>", sessions, group.Name );
                            pnlWell.Controls.Add( ltNoActiveSessions );
                        }

                    }
                    else
                    {
                        Literal ltNoSessions = new Literal();
                        ltNoSessions.Text = string.Format( "<h2>You have no {0} sessions remaining</h2>", group.Name );
                        pnlWell.Controls.Add( ltNoSessions );
                    }
                    groupCount++;
                }
            }

            //If the person is not a member of any fitness groups don't show a not found 
            if ( groupCount == 0 )
            {
                ShowPersonNotFound();
                StartTimeout();
                return;
            }

            //If the person does not have any groups to check into send them back after a time
            if ( checkinCount == 0 )
            {
                StartTimeout();
            }
            StartTimeout();
        }

        /// <summary>
        /// Toggles on or off one check-in GroupLocationSchedule (it will deselct others)
        /// </summary>
        /// <param name="chGroup">Checkin Group</param>
        /// <param name="chLocation">Checkin Location</param>
        /// <param name="chSchedule">Checkin Schedule</param>
        /// <param name="alreadySelected">Is the Group Location Schedule already selected</param>
        private void ToggleClass( CheckInGroup chGroup, CheckInLocation chLocation, CheckInSchedule chSchedule, bool alreadySelected )
        {
            foreach ( var groupType in CurrentCheckInState.CheckIn.CurrentPerson.GroupTypes )
            {
                groupType.Selected = true;
                foreach ( var group in groupType.Groups )
                {
                    group.Selected = false;
                    foreach ( var location in group.Locations )
                    {
                        location.Selected = false;
                        foreach ( var schedule in location.Schedules )
                        {
                            schedule.Selected = false;
                        }
                    }
                }
            }
            if ( !alreadySelected )
            {
                chGroup.Selected = true;
                chLocation.Selected = true;
                chSchedule.Selected = true;
                btnCheckin.Visible = true;
            }
            SaveState();
            ShowPersonCheckin();
        }

        /// <summary>
        /// Hides all other pannels and shows person not found pannel
        /// </summary>
        private void ShowPersonNotFound()
        {
            StartTimeout();
            pnlCheckin.Visible = false;
            pnlSearch.Visible = false;
            pnlNotFound.Visible = true;
        }

        /// <summary>
        /// Handler for refresh timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Timer1_Tick( object sender, EventArgs e )
        {

        }

        /// <summary>
        /// Gets a list of all fitness groups
        /// </summary>
        /// <returns>List of fitness groups</returns>
        private List<Group> GetMembershipGroups()
        {
            string parentGroupGuid = null;
            parentGroupGuid = GetAttributeValue( "GroupFitnessParentGroup" );
            if ( string.IsNullOrWhiteSpace( parentGroupGuid ) )
            {
                return new List<Group>();
            }
            GroupService groupService = new GroupService( _rockContext );
            var parentGroup = groupService.Get( parentGroupGuid.AsGuid() );
            if ( parentGroup != null )
            {
                return groupService.Queryable().AsNoTracking().Where( g => g.ParentGroupId == parentGroup.Id ).ToList();
            }
            return new List<Group>();
        }

        /// <summary>
        /// Handler for cancel button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            if ( CurrentCheckInState == null )
            {
                NavigateToPreviousPage();
                return;
            }
            StopTimeout();
            tbPhone.Text = "";
            CurrentCheckInState.CheckIn = new CheckInStatus();
            SaveState();
            pnlDone.Visible = false;
            pnlCheckin.Visible = false;
            pnlNotFound.Visible = false;
            pnlSearch.Visible = true;
            pnlMessage.Visible = true;
            phPeople.Controls.Clear();
        }

        /// <summary>
        /// Handler for checkin button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnCheckin_Click( object sender, EventArgs e )
        {
            StopTimeout();
            var checkinAction = GetAttributeValue( "CheckinActivity" );
            List<string> Errors;
            ProcessActivity( checkinAction, out Errors );
            ProcessLabels();
            pnlCheckin.Visible = false;
            pnlDone.Visible = true;
        }

        /// <summary>
        /// Generates labels and attaches appropriate scripts to print labels client side
        /// </summary>
        private void ProcessLabels()
        {
            LabelPrinter labelPrinter = new LabelPrinter( CurrentCheckInState, Request );
            labelPrinter.PrintNetworkLabels();
            var script = labelPrinter.GetClientScript();
            script += "setTimeout( function(){ __doPostBack( '" + btnDone.UniqueID + "', 'OnClick' ); },6000)";
            ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "addLabelScript", script, true );
        }

        /// <summary>
        /// Registers script to update page
        /// </summary>
        private void StartTimeout()
        {
            var script = "startTimeout()";
            ScriptManager.RegisterClientScriptBlock( upContent, upContent.GetType(), "StartTimeout", script, true );
        }

        /// <summary>
        /// Registers script that stops script that updates page
        /// </summary>
        private void StopTimeout()
        {
            var script = "stopTimeout()";
            ScriptManager.RegisterClientScriptBlock( upContent, upContent.GetType(), "StopTimeout", script, true );
        }
    }
}