﻿// <copyright>
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
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using OpenXmlPowerTools;
using org.secc.FamilyCheckin;
using org.secc.FamilyCheckin.Cache;
using org.secc.FamilyCheckin.Model;
using org.secc.FamilyCheckin.Utilities;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.CheckinMonitor
{
    [DisplayName( "Super Checkin" )]
    [Category( "SECC > Check-in" )]
    [Description( "Advanced tool for managing checkin." )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Connection Status", "Connection status for new people." )]
    [AttributeCategoryField( "Checkin Category", "The Attribute Category to display checkin attributes from", false, "Rock.Model.Person", true, "", "", 0 )]
    [TextField( "Checkin Activity", "Name of the activity to complete checkin", true )]
    [TextField( "Reprint Activity", "Name of the activity to reprint family tag", true )]
    [TextField( "Child Reprint Activity", "Name of the activity to reprint child's tag", true )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE, "SMS Phone", "Phone number type to save as when SMS enabled" )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE, "Other Phone", "Phone number type to save as when SMS NOT enabled" )]
    [BooleanField( "Allow Reprint", "Should we allow for reprints of parent tags from this page?", false )]
    [DataViewField( "Approved People", "Data view which contains the members who may check-in.", entityTypeName: "Rock.Model.Person" )]
    [BooleanField( "Allow NonApproved Adults", "Should adults who are not in the approved person list be allowed to checkin?", false, key: "AllowNonApproved" )]
    [DataViewField( "Security Role Dataview", "Data view which people who are in a security role. It will not allow adding PINs for people in this group.", entityTypeName: "Rock.Model.Person", required: false )]
    [TextField( "Data Error URL", "Example: WorkflowEntry/12?PersonId={0}", false )]
    [BooleanField( "Enable Reprint QR Code Validation", "Should QR code scanning be required to complete a tag reprint?", false, key: "QRCodeCheckReprint" )]
    [SecurityRoleField( "Reprint Tag Security Group", "Group to allow reprinting of tags.", key: "ReprintSecurityGroup", defaultSecurityRoleGroupGuid: Rock.SystemGuid.Group.GROUP_STAFF_MEMBERS )]
    [BooleanField( "Enable Check-In QR Code Validation", "Should QR code scanning be required to complete a super check-in?", false, key: "QRCodeCheckCheckin" )]
    [SecurityRoleField( "Super Check-In Group", "Group that is allowed to perform super check in", true, key: "SuperCheckInGroup" )]

    public partial class SuperCheckin : CheckInBlock
    {
        private RockContext _rockContext;
        private List<int> _approvedPeopleIds;
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            if ( _rockContext == null )
            {
                _rockContext = new RockContext();
            }

            RockPage.AddScriptLink( "~/Scripts/CheckinClient/ZebraPrint.js" );
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

                btnPrint.Visible = GetAttributeValue( "AllowReprint" ).AsBoolean();

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
                    BuildNewFamilyControls();
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

            if ( CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).Any() &&
                CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                .FirstOrDefault()
                .People.SelectMany( p => p.GroupTypes )
                .SelectMany( gt => gt.Groups )
                .SelectMany( g => g.Locations )
                .SelectMany( l => l.Schedules )
                .Where( s => s.Selected )
                .Any()
                )
            {
                btnCompleteCheckin.Visible = true;
            }
            else
            {
                btnCompleteCheckin.Visible = false;
            }

            if ( ViewState["SelectedPersonId"] != null && pnlEditPerson.Visible )
            {
                var personId = ( int ) ViewState["SelectedPersonId"];
                var person = new PersonService( _rockContext ).Get( personId );
                EditPerson( person, false );
            }
        }

        private void BuildNewFamilyControls()
        {
            ddlAdult1Suffix.DefinedTypeId = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSON_SUFFIX.AsGuid() ).Id;
            ddlAdult1Suffix.Items.Insert( 0, new ListItem() );
            ddlAdult2Suffix.DefinedTypeId = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSON_SUFFIX.AsGuid() ).Id;
            ddlAdult2Suffix.Items.Insert( 0, new ListItem() );

            pnbAdult1Phone.Text = CurrentCheckInState.CheckIn.SearchValue.AsDouble().ToString();

            var campusList = CampusCache.All();

            if ( campusList.Any() )
            {
                cpNewFamilyCampus.DataSource = campusList;
                cpNewFamilyCampus.DataBind();
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
        }

        private List<int> GetApprovedIds()
        {
            var approvedPeopleGuid = GetAttributeValue( "ApprovedPeople" ).AsGuid();
            var approvedPeople = new DataViewService( _rockContext ).Get( approvedPeopleGuid );

            if ( approvedPeople == null )
            {
                maWarning.Show( "Approved people block setting not found.", ModalAlertType.Alert );
                return null;
            }

            if ( approvedPeople.PersistedScheduleIntervalMinutes.HasValue && approvedPeople.PersistedLastRefreshDateTime.HasValue )
            {
                //Get record from persisted.
                return _rockContext.DataViewPersistedValues
                    .Where( a => a.DataViewId == approvedPeople.Id )
                    .Select( a => a.EntityId )
                    .ToList();

            }
            else
            {
                var approvedPeopleQry = approvedPeople.GetQuery( new DataViewGetQueryArgs { DatabaseTimeoutSeconds = 30 } );
                return approvedPeopleQry.Select( e => e.Id ).ToList();
            }
        }

        private bool IsApproved( Person person )
        {
            if ( _approvedPeopleIds == null )
            {
                _approvedPeopleIds = GetApprovedIds();
            }

            return _approvedPeopleIds.Contains( person.Id );
        }

        private void DisplayFamilyMemberMenu()
        {
            nbChange.Visible = false;


            phFamilyMembers.Controls.Clear();

            if ( !CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).Any() )
            {
                NavigateToPreviousPage();
                return;
            }

            foreach ( var checkinPerson in CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).First().People.OrderBy( p => p.Person.BirthDate ) )
            {
                BootstrapButton btnMember = new BootstrapButton();
                btnMember.CssClass = "btn btn-default btn-block btn-lg";
                var name = new StringBuilder();
                //Can work with kids
                if ( IsApproved( checkinPerson.Person ) )
                {
                    name.Append( "<i class='fa fa-thumbs-o-up'></i> " );
                }
                //is not a family member
                if ( !checkinPerson.FamilyMember )
                {
                    name.Append( "<i class='fa fa-exchange'></i> " );
                }

                //legal
                checkinPerson.Person.LoadAttributes();
                if ( !string.IsNullOrWhiteSpace( checkinPerson.Person.GetAttributeValue( "LegalNotes" ) ) )
                {
                    name.Append( "<i class='fa fa-legal'></i> " );
                }
                //allergy
                if ( !string.IsNullOrWhiteSpace( checkinPerson.Person.GetAttributeValue( "Allergy" ) ) )
                {
                    name.Append( "<i class='fa fa-medkit'></i> " );
                }

                name.Append( "<b>" );
                name.Append( checkinPerson.Person.FullName );
                name.Append( "</b>" );

                name.Append( GetSelectedCountString( checkinPerson ) );

                if ( checkinPerson.Person.Age.HasValue && checkinPerson.Person.Age < 18 )
                {
                    name.Append( "<br>" );
                    name.Append( checkinPerson.Person.FormatAge() );
                }

                btnMember.Text = name.ToString();

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

        private string GetSelectedCountString( CheckInPerson checkinPerson )
        {
            int count = checkinPerson.GroupTypes
                .SelectMany( gt => gt.Groups )
                .SelectMany( g => g.Locations )
                .SelectMany( l => l.Schedules )
                .Where( s => s.Selected ).Count();
            if ( count > 0 )
            {
                return " <span class=badge>" + count.ToString() + "</span>";
            }
            return "";
        }

        private void DisplayPersonInformation()
        {
            pnlAddPerson.Visible = false;
            pnlPersonInformation.Visible = true;
            pnlEditPerson.Visible = false;

            int selectedPersonId;
            if ( ViewState["SelectedPersonId"] != null )
            {
                selectedPersonId = ( int ) ViewState["SelectedPersonId"];
                var checkinPerson = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).First().People.Where( p => p.Person.Id == selectedPersonId ).First();
                pnlPersonInformation.Visible = true;
                pnlAddPerson.Visible = false;
                ltName.Text = checkinPerson.Person.FullName;
                BuildPersonCheckinDetails();
            }
        }

        private void BuildPersonCheckinDetails()
        {
            if ( ViewState["SelectedPersonId"] != null )
            {
                var selectedPersonId = ( int ) ViewState["SelectedPersonId"];
                Person person = new PersonService( _rockContext ).Get( selectedPersonId );
                AttendanceService attendanceService = new AttendanceService( _rockContext );
                var reserved = attendanceService.Queryable().Where( a => a.StartDateTime > Rock.RockDateTime.Today
                    && a.DidAttend == false && a.EndDateTime == null && a.PersonAliasId == person.PrimaryAliasId && a.RSVPDateTime == null ).ToList();
                var current = attendanceService.Queryable().Where( a => a.StartDateTime > Rock.RockDateTime.Today
                    && a.DidAttend == true && a.EndDateTime == null && a.PersonAliasId == person.PrimaryAliasId && a.RSVPDateTime == null ).ToList();
                var history = attendanceService.Queryable().Where( a => a.StartDateTime > Rock.RockDateTime.Today
                    && a.EndDateTime != null && a.PersonAliasId == person.PrimaryAliasId && a.RSVPDateTime == null ).ToList();

                if ( current.Any() )
                {
                    pnlCheckedin.Visible = true;
                    gCheckedin.DataSource = current;
                    gCheckedin.DataBind();
                }
                else
                {
                    pnlCheckedin.Visible = false;
                }
                if ( history.Any() )
                {
                    pnlHistory.Visible = true;
                    gHistory.DataSource = history;
                    gHistory.DataBind();
                }
                else
                {
                    pnlHistory.Visible = false;
                }

                if ( reserved.Any() )
                {
                    foreach ( var item in reserved )
                    {
                        if ( item.QualifierValueId.HasValue )
                        {
                            item.Occurrence.Group.Name = "<i class='fa fa-mobile'></i> " + item.Occurrence.Group.Name;
                        }
                    }
                    pnlReserved.Visible = true;
                    gReserved.DataSource = reserved;
                    gReserved.DataBind();
                }
                else
                {
                    pnlReserved.Visible = false;
                }
            }
        }

        protected void CancelReserved_Click( object sender, RowEventArgs e )
        {
            var attendanceItemId = ( int ) e.RowKeyValue;
            var mobileRecord = MobileCheckinRecordCache.GetByAttendanceId( attendanceItemId );
            if ( mobileRecord != null && mobileRecord.Status == MobileCheckinStatus.Active )
            {
                MobileCheckinRecordCache.CancelReservation( mobileRecord, true );
            }
            else
            {
                var attendanceService = new AttendanceService( _rockContext );
                var attendanceItem = attendanceService.Get( attendanceItemId );

                attendanceItem.EndDateTime = Rock.RockDateTime.Now;
                AttendanceCache.AddOrUpdate( attendanceItem );
                _rockContext.SaveChanges();
            }
            BuildPersonCheckinDetails();
        }

        protected void Checkout_Click( object sender, RowEventArgs e )
        {
            var attendanceItemId = ( int ) e.RowKeyValue;
            var attendanceItem = new AttendanceService( _rockContext ).Get( attendanceItemId );
            attendanceItem.EndDateTime = Rock.RockDateTime.Now;
            AttendanceCache.AddOrUpdate( attendanceItem );
            _rockContext.SaveChanges();
            BuildPersonCheckinDetails();
        }


        protected void btnCheckin_Click( object sender, EventArgs e )
        {
            if ( CurrentCheckInState == null )
            {
                NavigateToHomePage();
                return;
            }

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
            if ( CurrentCheckInState == null )
            {
                NavigateToPreviousPage();
                return;
            }

            if ( ViewState["SelectedPersonId"] == null )
            {
                return;
            }

            if ( cbSuperCheckin.Checked )
            {
                cbVolunteer.Visible = true;
            }
            else
            {
                cbVolunteer.Visible = false;
            }

            var selectedPersonId = ( int ) ViewState["SelectedPersonId"];
            var checkinPerson = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).First().People.Where( p => p.Person.Id == selectedPersonId ).FirstOrDefault();
            var canWorkWithMinors = IsApproved( checkinPerson.Person );

            phCheckin.Controls.Clear();

            if ( checkinPerson == null )
            {
                return;
            }

            var volunteerGroupIds = OccurrenceCache.GetVolunteerOccurrences().Select( o => o.GroupId ).ToList();
            var childGroupIds = OccurrenceCache.GetChildrenOccurrences().Select( o => o.GroupId ).ToList();


            foreach ( var groupType in checkinPerson.GroupTypes )
            {
                //ignore group types with no non-excluded groups on non-super checkin
                if ( !cbSuperCheckin.Checked && !groupType.Groups.Where( g => !g.ExcludedByFilter ).Any() )
                {
                    continue;
                }

                //ignore if requesting volunteer and cannot work with minors
                if ( cbSuperCheckin.Checked && cbVolunteer.Checked && !canWorkWithMinors )
                {
                    continue;
                }

                //ignore if volunteer selected and does not contain volunteers
                if ( cbSuperCheckin.Checked && cbVolunteer.Checked
                    && !groupType.Groups.Where( g => volunteerGroupIds.Contains( g.Group.Id ) ).Any() )
                {
                    continue;
                }

                //ignore if volunteer not selected and does not contain children
                if ( cbSuperCheckin.Checked && !cbVolunteer.Checked
                    && !groupType.Groups.Where( g => childGroupIds.Contains( g.Group.Id ) ).Any() )
                {
                    continue;
                }

                PanelWidget panelWidget = new PanelWidget();
                if ( !groupType.Groups.Where( g => !g.ExcludedByFilter ).Any() )
                {
                    panelWidget.Title = "<i class='fa fa-exclamation-triangle'></i> " + groupType.GroupType.Name;
                }
                else
                {
                    panelWidget.Title = groupType.GroupType.Name;
                }
                panelWidget.ID = groupType.GroupType.Guid.ToString();
                phCheckin.Controls.Add( panelWidget );
                foreach ( var group in groupType.Groups )
                {
                    if ( !cbSuperCheckin.Checked && group.ExcludedByFilter )
                    {
                        continue;
                    }

                    if ( cbSuperCheckin.Checked && cbVolunteer.Checked && !volunteerGroupIds.Contains( group.Group.Id ) )
                    {
                        continue;
                    }

                    if ( cbSuperCheckin.Checked && !cbVolunteer.Checked && !childGroupIds.Contains( group.Group.Id ) )
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
                    foreach ( var location in group.Locations.OrderBy( l => l.Location.Name ) )
                    {
                        if ( !cbSuperCheckin.Checked && !location.Location.IsActive )
                        {
                            continue;
                        }

                        foreach ( var schedule in location.Schedules )
                        {
                            if ( !cbSuperCheckin.Checked && !schedule.Schedule.WasCheckInActive( RockDateTime.Now ) )
                            {
                                continue;
                            }

                            BootstrapButton btnSelect = new BootstrapButton();
                            btnSelect.Text = location.Location.Name + ": " + schedule.Schedule.Name;
                            if ( !location.Location.IsActive )
                            {
                                btnSelect.Text += " [Location Not Active]";
                            }

                            if ( !schedule.Schedule.WasCheckInActive( RockDateTime.Now ) )
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

        protected void btnEditPerson_Click( object sender, EventArgs e )
        {
            var personId = ( int ) ViewState["SelectedPersonId"];
            if ( personId != 0 )
            {
                EditPerson( new PersonService( _rockContext ).Get( personId ) );
            }
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
            BuildPersonCheckinDetails();
        }

        protected void btnNewMember_Click( object sender, EventArgs e )
        {
            ShowAddNewPerson();
        }

        private void ShowAddNewPerson()
        {
            pnlEditPerson.Visible = false;
            pnlPersonInformation.Visible = false;
            pnlAddPerson.Visible = true;

            ddlNewPersonSuffix.DefinedTypeId = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSON_SUFFIX.AsGuid() ).Id;
            ddlNewPersonSuffix.Items.Insert( 0, new ListItem() );

            tbNewPersonFirstName.Text = "";
            tbNewPersonLastName.Text = "";
            dpNewPersonBirthDate.SelectedDate = null;
            ypNewGraduation.Text = "";

            rblNewPersonGender.Items.Clear();
            rblNewPersonGender.BindToEnum<Gender>();

            var gradeLabel = "";
            var today = RockDateTime.Today;
            var transitionDate = GlobalAttributesCache.Get().GetValue( "GradeTransitionDate" ).AsDateTime() ?? new DateTime( RockDateTime.Today.Year, 6, 1 );

            if ( transitionDate > today )
            {
                gradeLabel = string.Format( "Grade (For School Year {0}-{1})", today.AddYears( -1 ).Year, today.Year );
            }
            else
            {
                gradeLabel = string.Format( "Grade (For School Year {0}-{1})", today.Year, today.AddYears( 1 ).Year );
            }
            ddlGradePicker.Label = gradeLabel;

            ScriptManager.RegisterStartupScript( ddlGradePicker, ddlGradePicker.GetType(), "grade-selection-" + BlockId.ToString(), ddlGradePicker.GetJavascriptForYearPicker( ypNewGraduation ), true );
        }

        protected void btnSaveAddPerson_Click( object sender, EventArgs e )
        {
            if ( string.IsNullOrWhiteSpace( tbNewPersonFirstName.Text )
                || string.IsNullOrWhiteSpace( tbNewPersonLastName.Text )
                || dpNewPersonBirthDate.SelectedDate == null )
            {
                maWarning.Show( "First Name, Last Name, and Birthdate are required.", ModalAlertType.Alert );
                return;
            }

            Person person = new Person();
            person.FirstName = tbNewPersonFirstName.Text;
            person.LastName = tbNewPersonLastName.Text;
            person.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
            person.RecordStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() ).Id;
            if ( !string.IsNullOrWhiteSpace( rblNewPersonGender.SelectedValue ) )
            {
                person.Gender = rblNewPersonGender.SelectedValueAsEnum<Gender>();
            }
            person.SetBirthDate( dpNewPersonBirthDate.SelectedDate );
            if ( ypNewGraduation.SelectedYear.HasValue )
            {
                person.GraduationYear = ypNewGraduation.SelectedYear.Value;
            }

            person.ConnectionStatusValueId = DefinedValueCache.Get( GetAttributeValue( "ConnectionStatus" ).AsGuid() ).Id;

            var familyGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY );
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
                PersonService.AddPersonToFamily( person, true, family.Id, familyRoleId, _rockContext );
            }
            else
            {
                //save the person with new family
                var newFamily = PersonService.SaveNewPerson( person, _rockContext );

                //create connection
                var memberService = new GroupMemberService( _rockContext );
                var adultFamilyMembers = memberService.Queryable()
                    .Where( m =>
                        m.GroupId == family.Id
                        && m.GroupRoleId == adultRoleId
                        )
                        .Select( m => m.Person )
                        .ToList();

                foreach ( var member in adultFamilyMembers )
                {
                    Person.CreateCheckinRelationship( member.Id, person.Id, _rockContext );
                }
            }
            ViewState["SelectedPersonId"] = person.Id;
            ActivateFamily();
            EditPerson( person );
        }

        private void EditPerson( Person person, bool setValue = true )
        {
            pnlAddPerson.Visible = false;
            pnlPersonInformation.Visible = false;
            pnlEditPerson.Visible = true;

            if ( person.ConnectionStatusValueId == DefinedValueCache.Get( GetAttributeValue( "ConnectionStatus" ).AsGuid() ).Id )
            {
                pnlEditNameLiteral.Visible = false;
                pnlEditNameTextBox.Visible = true;
                if ( setValue )
                {
                    tbEditFirst.Text = person.FirstName;
                    tbEditLast.Text = person.LastName;
                }
            }
            else
            {
                pnlEditNameLiteral.Visible = true;
                pnlEditNameTextBox.Visible = false;
                ltEditName.Text = person.FullName;
            }

            if ( setValue )
            {
                dpEditBirthDate.SelectedDate = person.BirthDate;


                if ( !person.HasGraduated ?? false )
                {
                    ypEditGraduation.SelectedYear = person.GraduationYear;

                    int gradeOffset = person.GradeOffset.Value;
                    var maxGradeOffset = gpEditGrade.MaxGradeOffset;

                    // keep trying until we find a Grade that has a gradeOffset that that includes the Person's gradeOffset (for example, there might be combined grades)
                    while ( !gpEditGrade.Items.OfType<ListItem>().Any( a => a.Value.AsInteger() == gradeOffset ) && gradeOffset <= maxGradeOffset )
                    {
                        gradeOffset++;
                    }

                    gpEditGrade.SetValue( gradeOffset );
                }
                else
                {
                    ypEditGraduation.SelectedYear = null;
                    gpEditGrade.SelectedIndex = 0;
                }
            }
            var gradeLabel = "";
            var today = RockDateTime.Today;
            var transitionDate = GlobalAttributesCache.Get().GetValue( "GradeTransitionDate" ).AsDateTime() ?? new DateTime( RockDateTime.Today.Year, 6, 1 );

            if ( transitionDate > today )
            {
                gradeLabel = string.Format( "Grade (For School Year {0}-{1})", today.AddYears( -1 ).Year, today.Year );
            }
            else
            {
                gradeLabel = string.Format( "Grade (For School Year {0}-{1})", today.Year, today.AddYears( 1 ).Year );
            }
            gpEditGrade.Label = gradeLabel;

            ScriptManager.RegisterStartupScript( gpEditGrade, gpEditGrade.GetType(), "grade-selection-" + BlockId.ToString(), gpEditGrade.GetJavascriptForYearPicker( ypEditGraduation ), true );

            var AttributeList = new List<int>();

            string categoryGuid = GetAttributeValue( "CheckinCategory" );
            Guid guid = categoryGuid.AsGuid();

            var category = CategoryCache.Get( guid );
            if ( category != null )
            {
                AttributeList = new AttributeService( _rockContext ).GetByCategoryId( category.Id )
                    .OrderBy( a => a.Order ).ThenBy( a => a.Name ).Select( a => a.Id ).ToList();

            }
            person.LoadAttributes();

            fsAttributes.Controls.Clear();

            foreach ( int attributeId in AttributeList )
            {
                var attribute = AttributeCache.Get( attributeId );
                string attributeValue = person.GetAttributeValue( attribute.Key );
                attribute.AddControl( fsAttributes.Controls, attributeValue, "", setValue, true );
            }
        }

        protected void btnSaveAttributes_Click( object sender, EventArgs e )
        {
            var personId = ( int ) ViewState["SelectedPersonId"];

            var person = new PersonService( _rockContext ).Get( personId );

            if ( person.ConnectionStatusValueId == DefinedValueCache.Get( GetAttributeValue( "ConnectionStatus" ).AsGuid() ).Id )
            {
                person.FirstName = tbEditFirst.Text;
                person.NickName = tbEditFirst.Text;
                person.LastName = tbEditLast.Text;

                var checkinPerson = CurrentCheckInState.CheckIn.CurrentFamily.People.Where( p => p.Person.Id == person.Id ).FirstOrDefault();
                if ( checkinPerson != null )
                {
                    checkinPerson.Person = person.Clone( false );
                }
            }

            person.SetBirthDate( dpEditBirthDate.SelectedDate );

            if ( ypEditGraduation.SelectedYear.HasValue )
            {
                person.GraduationYear = ypEditGraduation.SelectedYear.Value;
            }
            else
            {
                person.GraduationYear = null;
            }

            var AttributeList = new List<int>();

            string categoryGuid = GetAttributeValue( "CheckinCategory" );
            Guid guid = Guid.Empty;
            if ( Guid.TryParse( categoryGuid, out guid ) )
            {
                var category = CategoryCache.Get( guid );
                if ( category != null )
                {
                    AttributeList = new AttributeService( _rockContext ).GetByCategoryId( category.Id )
                        .OrderBy( a => a.Order ).ThenBy( a => a.Name ).Select( a => a.Id ).ToList();
                }
            }
            person.LoadAttributes();

            foreach ( int attributeId in AttributeList )
            {
                var attribute = AttributeCache.Get( attributeId );

                if ( person != null &&
                    attribute.IsAuthorized( Rock.Security.Authorization.EDIT, CurrentPerson ) )
                {
                    System.Web.UI.Control attributeControl = fsAttributes.FindControl( string.Format( "attribute_field_{0}", attribute.Id ) );
                    if ( attributeControl != null )
                    {
                        string originalValue = person.GetAttributeValue( attribute.Key );
                        string newValue = attribute.FieldType.Field.GetEditValue( attributeControl, attribute.QualifierValues );
                        Rock.Attribute.Helper.SaveAttributeValue( person, attribute, newValue, _rockContext );

                        // Check for changes to write to history
                        if ( ( originalValue ?? string.Empty ).Trim() != ( newValue ?? string.Empty ).Trim() )
                        {
                            string formattedOriginalValue = string.Empty;
                            if ( !string.IsNullOrWhiteSpace( originalValue ) )
                            {
                                formattedOriginalValue = attribute.FieldType.Field.FormatValue( null, originalValue, attribute.QualifierValues, false );
                            }

                            string formattedNewValue = string.Empty;
                            if ( !string.IsNullOrWhiteSpace( newValue ) )
                            {
                                formattedNewValue = attribute.FieldType.Field.FormatValue( null, newValue, attribute.QualifierValues, false );
                            }
                        }
                    }
                }
            }
            if ( person.IsValid )
            {
                _rockContext.SaveChanges();
            }
            pnlPersonInformation.Visible = true;
            pnlEditPerson.Visible = false;

            var cPerson = CurrentCheckInState.CheckIn.CurrentFamily.People.Where( p => p.Person.Id == person.Id ).FirstOrDefault();
            if ( cPerson != null )
            {
                cPerson.Person.SetBirthDate( person.BirthDate );
            }

            DisplayFamilyMemberMenu();
            nbChange.Visible = true;
        }

        protected void btnCancelAttributes_Click( object sender, EventArgs e )
        {
            pnlPersonInformation.Visible = true;
            pnlEditPerson.Visible = false;
        }

        protected void btnCompleteCheckin_Click( object sender, EventArgs e )
        {
            var requireQRCodeCheck = GetAttributeValue( "QRCodeCheckCheckin" ).AsBoolean();
            if ( requireQRCodeCheck )
            {
                mdQRPin.Show();
                tbQRCheckPurpose.Text = "QRCodeCheckCheckin";
                tbQRPin.Focus();
            }
            else
            {
                completeCheckin();
            }
        }


        protected void mdQRPin_SaveClick( object sender, EventArgs e )
        {
            var rockContext = new Rock.Data.RockContext();
            var qrCheckPurpose = tbQRCheckPurpose.Text;
            var attendanceGuid = tbQRPin.Text.AsGuid();
            var today = DateTime.Now.Date;
            var attendanceRecord = new AttendanceService( rockContext )
                                    .Queryable()
                                    .Where( a =>
                                        a.Guid == attendanceGuid &&
                                        a.StartDateTime > today )
                                    .FirstOrDefault();

            if ( attendanceGuid.IsEmpty() )
            {
                maWarning.Show( "Something went wrong. Please check QR Code and try again.", ModalAlertType.Warning );
                tbQRPin.Text = "";
                mdQRPin.Hide();
            }
            else if ( !attendanceRecord.IsNotNull() )
            {
                maWarning.Show( "This QR code is not associated with a valid attendance record. Please check QR Code and try again.", ModalAlertType.Warning );
                tbQRPin.Text = "";
                mdQRPin.Hide();
            }
            else
            {
                var qrCheckPerson = new PersonAliasService( rockContext )
                                            .Queryable()
                                            .Where( pa =>
                                                pa.Id == attendanceRecord.PersonAliasId )
                                            .FirstOrDefault();

                //Assuming the qrCheckPurpose is 'QRCodeCheckCheckin' unless it is a reprint validation...
                var qrCheckGroupGuid = GetAttributeValue( "SuperCheckInGroup" ).AsGuid();
                
                if ( qrCheckPurpose == "QRCodeCheckReprint" )
                {
                    qrCheckGroupGuid = GetAttributeValue( "ReprintSecurityGroup" ).AsGuid();
                }
                    
                var qrCheckGroup = new GroupService( rockContext )
                                            .Queryable()
                                            .Where( a =>
                                                a.Guid == qrCheckGroupGuid )
                                            .FirstOrDefault();

                var isInGroup = new GroupMemberService( rockContext )
                                    .Queryable()
                                    .Any( gm =>
                                    gm.GroupId == qrCheckGroup.Id &&
                                    gm.PersonId == qrCheckPerson.PersonId );

                if ( isInGroup && (qrCheckPurpose == "QRCodeCheckCheckin") )
                {
                    completeCheckin( qrCheckPerson.Person.FullName );
                }
                else if ( isInGroup && (qrCheckPurpose == "QRCodeCheckReprint") )
                {
                    ReprintAggregateTag();
                }
                else
                {
                    maWarning.Show( "You are not allowed to perform this action.", ModalAlertType.Warning );
                }

                tbQRPin.Text = "";
                mdQRPin.Hide();
            }

        }

        private void completeCheckin( string qrCheckPerson = null )
        {
            if ( CurrentCheckInState == null || CurrentCheckInState.CheckIn.CurrentFamily == null )
            {
                NavigateToPreviousPage();
                return;
            }

            foreach ( var person in CurrentCheckInState.CheckIn.CurrentFamily.People )
            {
                person.Selected = false;
                foreach ( var groupType in person.GroupTypes )
                {
                    groupType.Selected = false;
                    foreach ( var group in groupType.Groups )
                    {
                        group.Selected = false;
                        foreach ( var location in group.Locations )
                        {
                            location.Selected = false;
                            foreach ( var schedule in location.Schedules.Where( s => s.Selected ) )
                            {
                                location.Selected = false;
                                person.Selected = true;
                                groupType.Selected = true;
                                group.Selected = true;
                                if( tbQRCheckPurpose.Text == "QRCodeCheckCheckin" )
                                {
                                    group.Notes = qrCheckPerson.IsNotNullOrWhiteSpace() ? $"Super Check-In by {qrCheckPerson}" : "Super Check-In";
                                }
                                else if ( tbQRCheckPurpose.Text == "QRCodeCheckReprint" )
                                {
                                    group.Notes = qrCheckPerson.IsNotNullOrWhiteSpace() ? $"Parent tag reprint by {qrCheckPerson}" : "Parent Tag Re-Print";
                                }
                                location.Selected = true;
                            }
                        }
                    }
                }
            }
            SaveState();
            var activity = GetAttributeValue( "CheckinActivity" );
            List<string> errorMessages;
            ProcessActivity( activity, out errorMessages );
            ProcessLabels();
            ClearLabels();
            btnCompleteCheckin.Visible = false;
            DisplayFamilyMemberMenu();
            BuildGroupTypeModal();
            tbQRPin.Text = "";
            mdQRPin.Hide();

        }


        private void ClearLabels()
        {
            foreach ( var family in CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ) )
            {
                foreach ( var person in family.People )
                {
                    foreach ( var groupType in person.GroupTypes )
                    {
                        groupType.Labels = null;
                    }
                }
            }

        }

        private void ProcessLabels()
        {
            var printQueue = new Dictionary<string, StringBuilder>();
            foreach ( var selectedFamily in CurrentCheckInState.CheckIn.Families.Where( p => p.Selected ) )
            {
                List<CheckInLabel> labels = new List<CheckInLabel>();
                List<CheckInPerson> selectedPeople = selectedFamily.People.Where( p => p.Selected ).ToList();
                List<FamilyLabel> familyLabels = new List<FamilyLabel>();

                foreach ( CheckInPerson selectedPerson in selectedPeople )
                {
                    foreach ( var groupType in selectedPerson.GroupTypes.Where( gt => gt.Selected && gt.Labels != null ) )
                    {
                        foreach ( var label in groupType.Labels )
                        {
                            var file = new BinaryFileService( _rockContext ).Get( label.FileGuid );
                            file.LoadAttributes( _rockContext );
                            string isFamilyLabel = file.GetAttributeValue( "IsFamilyLabel" );
                            if ( isFamilyLabel != "True" )
                            {
                                labels.Add( label );
                            }
                            else
                            {
                                List<string> mergeCodes = file.GetAttributeValue( "MergeCodes" ).TrimEnd( '|' ).Split( '|' ).ToList();
                                FamilyLabel familyLabel = familyLabels.FirstOrDefault( fl => fl.FileGuid == label.FileGuid &&
                                                                                 fl.MergeFields.Count < mergeCodes.Count );
                                if ( familyLabel == null )
                                {
                                    familyLabel = new FamilyLabel();
                                    familyLabel.FileGuid = label.FileGuid;
                                    familyLabel.LabelObj = label;
                                    foreach ( var mergeCode in mergeCodes )
                                    {
                                        familyLabel.MergeKeys.Add( mergeCode.Split( '^' )[0] );
                                    }
                                    familyLabels.Add( familyLabel );
                                }
                                familyLabel.MergeFields.Add( ( selectedPerson.Person.Age.ToString() ?? "#" ) + "yr-" + selectedPerson.SecurityCode );
                            }
                        }
                    }
                }

                //Format all FamilyLabels and add to list of labels to print.
                foreach ( FamilyLabel familyLabel in familyLabels )
                {
                    //create padding to clear unused merge fields
                    List<string> padding = Enumerable.Repeat( " ", familyLabel.MergeKeys.Count ).ToList();
                    familyLabel.MergeFields.AddRange( padding );
                    for ( int i = 0; i < familyLabel.MergeKeys.Count; i++ )
                    {
                        familyLabel.LabelObj.MergeFields[familyLabel.MergeKeys[i]] = familyLabel.MergeFields[i];
                    }
                    labels.Add( familyLabel.LabelObj );
                }

                // Print client labels
                if ( labels.Any( l => l.PrintFrom == Rock.Model.PrintFrom.Client ) )
                {
                    var clientLabels = labels.Where( l => l.PrintFrom == PrintFrom.Client ).ToList();
                    var urlRoot = string.Format( "{0}://{1}", Request.Url.Scheme, Request.Url.Authority );
                    clientLabels.ForEach( l => l.LabelFile = urlRoot + l.LabelFile );
                    AddLabelScript( clientLabels.ToJson() );
                }

                // Print server labels
                if ( labels.Any( l => l.PrintFrom == Rock.Model.PrintFrom.Server ) )
                {
                    string delayCut = @"^XB";
                    string endingTag = @"^XZ";
                    var printerIp = string.Empty;
                    var labelContent = new StringBuilder();

                    // make sure labels have a valid ip
                    var lastLabel = labels.Last();
                    foreach ( var label in labels.Where( l => l.PrintFrom == PrintFrom.Server && !string.IsNullOrEmpty( l.PrinterAddress ) ) )
                    {
                        var labelCache = KioskLabel.Get( label.FileGuid );
                        if ( labelCache != null )
                        {
                            if ( printerIp != label.PrinterAddress )
                            {
                                printQueue.AddOrReplace( label.PrinterAddress, labelContent );
                                printerIp = label.PrinterAddress;
                                labelContent = new StringBuilder();
                            }

                            var printContent = labelCache.FileContent;
                            foreach ( var mergeField in label.MergeFields )
                            {
                                if ( !string.IsNullOrWhiteSpace( mergeField.Value ) )
                                {
                                    printContent = Regex.Replace( printContent, string.Format( @"(?<=\^FD){0}(?=\^FS)", mergeField.Key ), ZebraFormatString( mergeField.Value ) );
                                }
                                else
                                {
                                    printContent = Regex.Replace( printContent, string.Format( @"\^FO.*\^FS\s*(?=\^FT.*\^FD{0}\^FS)", mergeField.Key ), string.Empty );
                                    printContent = Regex.Replace( printContent, string.Format( @"\^FD{0}\^FS", mergeField.Key ), "^FD^FS" );
                                }
                            }

                            // send a Delay Cut command at the end to prevent cutting intermediary labels
                            if ( label != lastLabel )
                            {
                                printContent = Regex.Replace( printContent.Trim(), @"\" + endingTag + @"$", delayCut + endingTag );
                            }

                            labelContent.Append( printContent );
                        }
                    }

                    printQueue.AddOrReplace( printerIp, labelContent );
                }

                if ( printQueue.Any() )
                {
                    PrintLabels( printQueue );
                    printQueue.Clear();
                }
            }
            ClearCheckin();
        }

        /// <summary>
        /// Prints the labels.
        /// </summary>
        /// <param name="families">The families.</param>
        private void PrintLabels( Dictionary<string, StringBuilder> printerContent )
        {
            foreach ( var printerIp in printerContent.Keys.Where( k => !string.IsNullOrEmpty( k ) ) )
            {
                StringBuilder labelContent;
                if ( printerContent.TryGetValue( printerIp, out labelContent ) )
                {
                    var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
                    var printerIpEndPoint = new IPEndPoint( IPAddress.Parse( printerIp ), 9100 );
                    var result = socket.BeginConnect( printerIpEndPoint, null, null );
                    bool success = result.AsyncWaitHandle.WaitOne( 5000, true );

                    if ( socket.Connected )
                    {
                        var ns = new NetworkStream( socket );
                        byte[] toSend = System.Text.Encoding.ASCII.GetBytes( labelContent.ToString() );
                        ns.Write( toSend, 0, toSend.Length );
                    }
                    else
                    {
                        //phPrinterStatus.Controls.Add(new LiteralControl(string.Format("Can't connect to printer: {0}", printerIp)));
                    }

                    if ( socket != null && socket.Connected )
                    {
                        socket.Shutdown( SocketShutdown.Both );
                        socket.Close();
                    }
                }
            }

        }

        private void ClearCheckin()
        {
            foreach ( var person in CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault().People )
            {
                person.Selected = false;
                foreach ( var groupType in person.GroupTypes )
                {
                    groupType.Selected = false;
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
            }
            SaveState();
            BuildPersonCheckinDetails();
        }

        /// <summary>
        /// Adds the label script.
        /// </summary>
        /// <param name="jsonObject">The json object.</param>
        private void AddLabelScript( string jsonObject )
        {
            string script = string.Format( @"
            var labelData = {0};
		    function printLabels() {{
		        ZebraPrintPlugin.printTags(
            	    JSON.stringify(labelData),
            	    function(result) {{
			        }},
			        function(error) {{
				        // error is an array where:
				        // error[0] is the error message
				        // error[1] determines if a re-print is possible (in the case where the JSON is good, but the printer was not connected)
			            console.log('An error occurred: ' + error[0]);
                        navigator.notification.alert(
                            'An error occurred while printing the labels.' + error[0],  // message
                            alertDismissed,         // callback
                            'Error',            // title
                            'Ok'                  // buttonName
                        );
			        }}
                );
	        }}
try{{
            printLabels();
}} catch(e){{}}
            ", jsonObject, btnBack.UniqueID );
            ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "addLabelScript", script, true );
        }

        private static string ZebraFormatString( string input, bool isJson = false )
        {
            if ( isJson )
            {
                return input.Replace( "é", @"\\82" );  // fix acute e
            }
            else
            {
                return input.Replace( "é", @"\82" );  // fix acute e
            }
        }

        protected void btnNewFamily_Click( object sender, EventArgs e )
        {
            var familyGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY );
            var adultRoleId = familyGroupType.Roles
                .Where( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ) )
                .Select( r => r.Id )
                .FirstOrDefault();

            //Adult1 Basic Info
            Person adult1 = new Person();
            adult1.FirstName = tbAdult1FirstName.Text;
            adult1.LastName = tbAdult1LastName.Text;
            adult1.SuffixValueId = ddlAdult1Suffix.SelectedValueAsId();
            adult1.ConnectionStatusValueId = DefinedValueCache.Get( GetAttributeValue( "ConnectionStatus" ).AsGuid() ).Id;
            adult1.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
            adult1.RecordStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() ).Id;

            var newFamily = PersonService.SaveNewPerson( adult1, _rockContext );
            newFamily.Members.Where( m => m.Person == adult1 ).FirstOrDefault().GroupRoleId = adultRoleId;

            int homeLocationTypeId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() ).Id;
            var familyLocation = new Location();
            familyLocation.Street1 = acNewFamilyAddress.Street1;
            familyLocation.Street2 = acNewFamilyAddress.Street2;
            familyLocation.City = acNewFamilyAddress.City;
            familyLocation.State = acNewFamilyAddress.State;
            familyLocation.PostalCode = acNewFamilyAddress.PostalCode;
            newFamily.GroupLocations.Add( new GroupLocation() { Location = familyLocation, GroupLocationTypeValueId = homeLocationTypeId } );
            newFamily.CampusId = cpNewFamilyCampus.SelectedCampusId;

            _rockContext.SaveChanges();

            if ( cbAdult1SMS.Checked )
            {
                var smsPhone = DefinedValueCache.Get( GetAttributeValue( "SMSPhone" ).AsGuid() ).Id;
                adult1.UpdatePhoneNumber( smsPhone, PhoneNumber.DefaultCountryCode(), pnbAdult1Phone.Text, true, false, _rockContext );
            }
            else
            {
                var otherPhone = DefinedValueCache.Get( GetAttributeValue( "OtherPhone" ).AsGuid() ).Id;
                adult1.UpdatePhoneNumber( otherPhone, PhoneNumber.DefaultCountryCode(), pnbAdult1Phone.Text, false, false, _rockContext );
            }

            if ( !string.IsNullOrWhiteSpace( tbAdult2FirstName.Text ) && !string.IsNullOrWhiteSpace( tbAdult2LastName.Text ) )
            {
                //Adult2 Basic Info
                Person adult2 = new Person();
                adult2.FirstName = tbAdult2FirstName.Text;
                adult2.LastName = tbAdult2LastName.Text;
                adult2.SuffixValueId = ddlAdult2Suffix.SelectedValueAsId();
                adult2.ConnectionStatusValueId = DefinedValueCache.Get( GetAttributeValue( "ConnectionStatus" ).AsGuid() ).Id;
                adult2.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                adult2.RecordStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() ).Id;

                PersonService.AddPersonToFamily( adult2, true, newFamily.Id, adultRoleId, _rockContext );

                if ( cbAdult2SMS.Checked )
                {
                    var smsPhone = DefinedValueCache.Get( GetAttributeValue( "SMSPhone" ).AsGuid() ).Id;
                    adult2.UpdatePhoneNumber( smsPhone, PhoneNumber.DefaultCountryCode(), pnbAdult2Phone.Text, true, false, _rockContext );
                }
                else
                {
                    var otherPhone = DefinedValueCache.Get( GetAttributeValue( "OtherPhone" ).AsGuid() ).Id;
                    adult2.UpdatePhoneNumber( otherPhone, PhoneNumber.DefaultCountryCode(), pnbAdult2Phone.Text, false, false, _rockContext );
                }
            }
            _rockContext.SaveChanges();
            CurrentCheckInState.CheckIn.Families.Add( new CheckInFamily() { Group = newFamily, Selected = true } );
            SaveState();
            ViewState.Add( "ExistingFamily", true );
            pnlManageFamily.Visible = true;
            pnlNewFamily.Visible = false;
            SaveViewState();
            ActivateFamily();

        }

        protected void btnPrint_Click( object sender, EventArgs e )
        {
            var requireQRCodeCheck = GetAttributeValue( "QRCodeCheckReprint" ).AsBoolean();
            if ( requireQRCodeCheck )
            {
                mdQRPin.Show();
                tbQRCheckPurpose.Text = "QRCodeCheckReprint";
                tbQRPin.Focus();
            }
            else
            {
                nbLogin.Visible = false;
                tbUsername.Text = "";
                tbPassword.Text = "";
                mdLogin.Show();
            }

        }

        private void ReprintAggregateTag()
        {
            if ( !GetAttributeValue( "AllowReprint" ).AsBoolean() )
            {
                return;
            }
            List<string> errorMessages = new List<string>();
            try
            {
                ProcessActivity( GetAttributeValue( "ReprintActivity" ), out errorMessages );
            }
            catch ( Exception ex )
            {
                LogException( ex );
                maWarning.Show( "There was an exception while processing your request. The error has been logged.", ModalAlertType.Alert );
            }
            if ( !errorMessages.Any() )
            {
                LabelPrinter labelPrinter = new LabelPrinter( CurrentCheckInState, Request );
                labelPrinter.PrintNetworkLabels();
                var script = labelPrinter.GetClientScript();
                ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "addLabelScript", script, true );
            }
            ClearLabels();
        }

        protected void btnReprintPerson_Click( object sender, EventArgs e )
        {
            if ( ViewState["SelectedPersonId"] != null )
            {
                var personId = ( int ) ViewState["SelectedPersonId"];

                RockContext rockContext = new RockContext();
                AttendanceService attendanceService = new AttendanceService( rockContext );

                foreach ( var checkinPerson in CurrentCheckInState.CheckIn.CurrentFamily.People )
                {
                    if ( checkinPerson.Person.Id == personId )
                    {
                        var groupTypeIds = attendanceService.Queryable()
                            .AsNoTracking()
                            .Where( a =>
                                     a.PersonAlias.Person.Id == personId
                                     && a.StartDateTime >= Rock.RockDateTime.Today
                                     && a.EndDateTime == null
                                     && a.Occurrence.Group != null
                                     && a.Occurrence.Schedule != null
                                     && a.Occurrence.Location != null
                                     && a.QualifierValueId == null
                                    )
                                    .Select( a => a.Occurrence.Group.GroupTypeId )
                                    .ToList();
                        checkinPerson.Selected = true;
                        foreach ( var groupType in checkinPerson.GroupTypes )
                        {
                            if ( groupTypeIds.Contains( groupType.GroupType.Id ) )
                            {
                                groupType.Selected = true;
                            }
                        }
                    }
                    else
                    {
                        checkinPerson.Selected = false;
                    }
                }

            }
            List<string> errorMessages = new List<string>();
            ProcessActivity( GetAttributeValue( "ChildReprintActivity" ), out errorMessages );
            if ( !errorMessages.Any() )
            {
                LabelPrinter labelPrinter = new LabelPrinter( CurrentCheckInState, Request );
                labelPrinter.PrintNetworkLabels();
                var script = labelPrinter.GetClientScript();
                ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "addLabelScript", script, true );
            }
            ClearLabels();
        }

        protected void btnCancel_Click( object sender, EventArgs e )
        {
            NavigateToPreviousPage();
        }

        protected void btnPhone_Click( object sender, EventArgs e )
        {
            if ( ViewState["SelectedPersonId"] != null )
            {
                var person = new PersonService( _rockContext ).Get( ( int ) ViewState["SelectedPersonId"] );
                if ( person != null )
                {
                    var phoneNumbers = person.PhoneNumbers.Where( pn => !pn.IsUnlisted );
                    if ( phoneNumbers.Any() )
                    {
                        var phoneDisplay = new StringBuilder();
                        foreach ( var phoneNumber in phoneNumbers )
                        {
                            phoneDisplay.Append( phoneNumber.NumberTypeValue );
                            phoneDisplay.Append( ": " );
                            phoneDisplay.Append( phoneNumber.NumberFormatted );
                            phoneDisplay.Append( "<br />" );
                        }
                        maWarning.Show( phoneDisplay.ToString(), ModalAlertType.Information );
                    }
                    else
                    {
                        pnbAdult1Phone.Text = "";
                        mdAddPhone.Show();
                    }
                }
            }
        }

        protected void btnPIN_Click( object sender, EventArgs e )
        {
            tbPIN.Text = "";
            mdPIN.Show();
        }

        protected void mdPIN_SaveClick( object sender, EventArgs e )
        {
            var pin = tbPIN.Text.AsNumeric();
            if ( !string.IsNullOrWhiteSpace( pin ) && pin.Length > 7 )
            {
                if ( ViewState["SelectedPersonId"] != null )
                {
                    var person = new PersonService( _rockContext ).Get( ( int ) ViewState["SelectedPersonId"] );
                    if ( person != null )
                    {
                        //check to see if person is in a security role and disallow if in security role
                        var securityRoleGuid = GetAttributeValue( "SecurityRoleDataview" ).AsGuid();
                        var securityMembers = new DataViewService( _rockContext ).Get( securityRoleGuid );

                        if ( securityMembers == null )
                        {
                            maWarning.Show( "Security role dataview not found.", ModalAlertType.Warning );
                            mdPIN.Hide();
                            return;
                        }
                        var securityMembersQry = securityMembers.GetQuery( new DataViewGetQueryArgs { DatabaseTimeoutSeconds = 30 } );
                        if ( securityMembersQry.Where( p => p.Id == person.Id ).Any() )
                        {
                            maWarning.Show( "Unable to add PIN to person. This person is in a security role and cannot have a PIN added from this tool.", ModalAlertType.Warning );
                            mdPIN.Hide();
                        }

                        var userLoginService = new UserLoginService( _rockContext );
                        var userLogin = userLoginService.GetByUserName( pin );
                        if ( userLogin == null )
                        {
                            userLogin = new UserLogin();
                            userLogin.UserName = pin;
                            userLogin.IsConfirmed = true;
                            userLogin.PersonId = person.Id;
                            userLogin.EntityTypeId = EntityTypeCache.Get( "Rock.Security.Authentication.PINAuthentication" ).Id;
                            userLoginService.Add( userLogin );
                            _rockContext.SaveChanges();
                            maWarning.Show( "PIN number linked to person", ModalAlertType.Information );
                        }
                        else
                        {
                            maWarning.Show( "This PIN has already been assigned to a person, and cannot be assigned to this person.", ModalAlertType.Warning );
                        }
                    }
                }
            }
            else
            {
                maWarning.Show( "PIN number was of invalid length", ModalAlertType.Warning );
            }
            mdPIN.Hide();
        }

        protected void cbVolunteer_CheckedChanged( object sender, EventArgs e )
        {

        }

        protected void btnDataError_Click( object sender, EventArgs e )
        {
            mdDataError.Show();
            var personId = ( int ) ViewState["SelectedPersonId"];
            var url = GetAttributeValue( "DataErrorURL" );
            url = string.Format( url, personId );
            ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "runUrl", "updateIframe('" + url + "')", true );
        }

        protected void mdAddPhone_SaveClick( object sender, EventArgs e )
        {
            var personId = ( int ) ViewState["SelectedPersonId"];
            PersonService personService = new PersonService( _rockContext );
            Person person = personService.Get( personId );
            var globalAttributesCache = GlobalAttributesCache.Get();
            var numberTypeValueId = ddlPhoneNumberType.SelectedValue.AsInteger();
            if ( numberTypeValueId == 0 )
            {
                numberTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME ).Id;
            }
            if ( person != null )
            {
                person.UpdatePhoneNumber( numberTypeValueId, PhoneNumber.DefaultCountryCode(), pnbNewPhoneNumber.Text, false, false, _rockContext );
                _rockContext.SaveChanges();
            }
            mdAddPhone.Hide();
        }

        protected void btnLoginCancel_Click( object sender, EventArgs e )
        {
            tbUsername.Text = "";
            tbPassword.Text = "";
            mdLogin.Hide();
        }

        protected void btnLoginPrint_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            var userLoginService = new UserLoginService( rockContext );
            var userLogin = userLoginService.GetByUserName( tbUsername.Text );
            if ( userLogin != null && userLogin.EntityType != null )
            {
                var component = AuthenticationContainer.GetComponent( userLogin.EntityType.Name );
                if ( component != null && component.IsActive && !component.RequiresRemoteAuthentication )
                {
                    if ( component.Authenticate( userLogin, tbPassword.Text ) )
                    {
                        var person = userLogin.Person;
                        if ( AuthorizedToReprint( person ) )
                        {
                            tbUsername.Text = "";
                            tbPassword.Text = "";
                            ReprintAggregateTag();
                            mdLogin.Hide();
                            return;
                        }
                        else
                        {
                            nbLogin.Visible = true;
                            nbLogin.Text = "Unauthorized to reprint";
                        }
                    }
                    else
                    {
                        nbLogin.Visible = true;
                        nbLogin.Text = "Incorrect Username Or Password";
                    }
                }
            }
        }

        private bool AuthorizedToReprint( Person person )
        {
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            var group = groupService.Get( GetAttributeValue( "ReprintSecurityGroup" ).AsGuid() );
            if ( group != null )
            {
                return groupMemberService
                    .GetByGroupIdAndPersonId( group.Id, person.Id )
                    .Where( gm => gm.GroupMemberStatus == GroupMemberStatus.Active )
                    .Any();
            }
            return false;
        }

        protected void btnMobile_Click( object sender, EventArgs e )
        {
            BindMCRRepeater();
        }

        private void BindMCRRepeater()
        {
            var kioskType = KioskTypeCache.All().Where( k => k.CheckinTemplateId == LocalDeviceConfig.CurrentCheckinTypeId ).FirstOrDefault();
            var campus = kioskType.Campus;

            RockContext rockContext = new RockContext();
            MobileCheckinRecordService mobileCheckinRecordService = new MobileCheckinRecordService( rockContext );
            GroupService groupService = new GroupService( rockContext );

            var familyId = CurrentCheckInState.CheckIn.CurrentFamily.Group.Id;

            var groupQry = groupService.Queryable();

            var records = mobileCheckinRecordService.Queryable().AsNoTracking()
                .Where( r => r.CreatedDateTime >= Rock.RockDateTime.Today && r.FamilyGroupId == familyId )
                .Join( groupQry,
                r => r.FamilyGroupId,
                g => g.Id,
                ( r, g ) => new
                {
                    Record = r,
                    FamilyName = g.Name,
                    Attendances = r.Attendances
                } )
                .ToList()
                .Select( r => new MCRPoco
                {
                    Record = r.Record,
                    Caption = string.Format( "{0} ({1})", r.FamilyName, r.Record.Campus.Name ),
                    Active = r.Record.Status == MobileCheckinStatus.Active,
                    SubCaption = string.Join( "<br>", r.Attendances.Select( a => string.Format( "{0}: {1} in {2} at {3}", a.PersonAlias.Person.NickName, a.Occurrence.Group.Name, a.Occurrence.Location.Name, a.Occurrence.Schedule.Name ) ) )
                } )
                .OrderByDescending( r => r.Record.CreatedDateTime )
                .ToList();

            rMCR.DataSource = records;
            rMCR.DataBind();

            if ( !records.Any() )
            {
                ltMobileCheckin.Text = "<h3>There are no check-in records for this family</h3>";
            }
            else
            {
                ltMobileCheckin.Text = "";
            }


            mdMobileCheckin.Show();
        }


        protected void rMCR_ItemCommand( object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e )
        {
            if ( e.CommandName == "Checkin" )
            {
                var accessKey = e.CommandArgument.ToString();
                MobileCheckin( accessKey );
            }
            else if ( e.CommandName == "Cancel" )
            {
                var accessKey = e.CommandArgument.ToString();
                var record = MobileCheckinRecordCache.GetByAccessKey( accessKey );
                MobileCheckinRecordCache.CancelReservation( record, true );
                BindMCRRepeater();
            }

            if ( ViewState["SelectedPersonId"] != null && pnlEditPerson.Visible )
            {
                var personId = ( int ) ViewState["SelectedPersonId"];
                var person = new PersonService( _rockContext ).Get( personId );
                EditPerson( person );
            }
        }

        private void MobileCheckin( string accessKey )
        {
            var mobileDidAttendId = DefinedValueCache.Get( Constants.DEFINED_VALUE_MOBILE_DID_ATTEND ).Id;
            var mobileNotAttendId = DefinedValueCache.Get( Constants.DEFINED_VALUE_MOBILE_NOT_ATTEND ).Id;

            RockContext rockContext = new RockContext();
            MobileCheckinRecordService mobileCheckinRecordService = new MobileCheckinRecordService( rockContext );

            var mobileCheckinRecord = mobileCheckinRecordService.Queryable().Where( r => r.AccessKey == accessKey ).FirstOrDefault();

            if ( mobileCheckinRecord == null )
            {
                maWarning.Show( "Mobile check-in record not found", ModalAlertType.Alert );
                BindMCRRepeater();
                return;
            }
            else if ( mobileCheckinRecord.Status == MobileCheckinStatus.Canceled )
            {
                maWarning.Show( "Mobile check-in record is expired.", ModalAlertType.Alert );
                BindMCRRepeater();
                return;
            }
            else if ( mobileCheckinRecord.Status == MobileCheckinStatus.Complete )
            {
                maWarning.Show( "Mobile check-in record has already been completed.", ModalAlertType.Alert );
                BindMCRRepeater();
                return;
            }

            try
            {
                if ( mobileCheckinRecord == null )
                {
                    return;
                }

                List<CheckInLabel> labels = JsonConvert.DeserializeObject<List<CheckInLabel>>( mobileCheckinRecord.SerializedCheckInState );

                LabelPrinter labelPrinter = new LabelPrinter()
                {
                    Request = Request,
                    Labels = labels
                };

                labelPrinter.PrintNetworkLabels();
                var script = labelPrinter.GetClientScript();
                ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "addLabelScript", script, true );

                foreach ( var attendance in mobileCheckinRecord.Attendances )
                {
                    if ( attendance.QualifierValueId == mobileDidAttendId )
                    {
                        attendance.DidAttend = true;
                        attendance.QualifierValueId = null;
                        attendance.StartDateTime = Rock.RockDateTime.Now;
                    }
                    else if ( attendance.QualifierValueId == mobileNotAttendId )
                    {
                        attendance.DidAttend = false;
                        attendance.QualifierValueId = null;
                    }
                    attendance.Note = "Completed mobile check-in at: " + CurrentCheckInState.Kiosk.Device.Name;
                }

                mobileCheckinRecord.Status = MobileCheckinStatus.Complete;

                rockContext.SaveChanges();

                //wait until we successfully save to update cache
                foreach ( var attendance in mobileCheckinRecord.Attendances )
                {
                    AttendanceCache.AddOrUpdate( attendance );

                }
                MobileCheckinRecordCache.Update( mobileCheckinRecord.Id );
                BindMCRRepeater();
            }
            catch ( Exception e )
            {
                LogException( e );
                maWarning.Show( "An unexpected issue occurred.", ModalAlertType.Alert );
                BindMCRRepeater();
            }
        }


        private class MCRPoco
        {
            public MobileCheckinRecord Record { get; set; }
            public string Caption { get; set; }
            public string SubCaption { get; set; }
            public bool Active { get; set; }
        }
    }
    class FamilyLabel
    {
        public Guid FileGuid { get; set; }

        public CheckInLabel LabelObj { get; set; }

        private List<string> _mergeFields = new List<string>();
        public List<string> MergeFields
        {
            get
            {
                return _mergeFields;
            }
            set
            {
                _mergeFields = value;
            }
        }
        private List<string> _mergeKeys = new List<string>();

        public List<string> MergeKeys
        {
            get
            {
                return _mergeKeys;
            }
            set
            {
                _mergeKeys = value;
            }
        }
    }



}