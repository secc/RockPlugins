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
using Rock.Security;
using System.Web.UI;
using System.Net.Sockets;
using System.Text;
using System.Net;
using org.secc.FamilyCheckin.Utilities;
using System.Data.Entity;

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

    public partial class SuperCheckin : CheckInBlock
    {

        private RockContext _rockContext;

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
            ddlAdult1Suffix.BindToDefinedType( DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSON_SUFFIX.AsGuid() ), true );
            ddlAdult2Suffix.BindToDefinedType( DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSON_SUFFIX.AsGuid() ), true );

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

        private void DisplayFamilyMemberMenu()
        {
            nbChange.Visible = false;
            var approvedPeopleGuid = GetAttributeValue( "ApprovedPeople" ).AsGuid();
            var approvedPeople = new DataViewService( _rockContext ).Get( approvedPeopleGuid );

            if ( approvedPeople == null )
            {
                maWarning.Show( "Approved people block setting not found.", ModalAlertType.Alert );
                return;
            }
            var errorMessages = new List<string>();
            var approvedPeopleQry = approvedPeople.GetQuery( null, 30, out errorMessages );

            phFamilyMembers.Controls.Clear();

            if ( !CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).Any() )
            {
                NavigateToPreviousPage();
                return;
            }

            foreach ( var checkinPerson in CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).First().People )
            {
                BootstrapButton btnMember = new BootstrapButton();
                btnMember.CssClass = "btn btn-default btn-block btn-lg";
                var name = new StringBuilder();
                //Can work with kids
                if ( approvedPeopleQry.Where( dv => dv.Id == checkinPerson.Person.Id ).Any() )
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

                if ( !GetAttributeValue( "AllowNonApproved" ).AsBoolean() )
                {
                    var approvedPeopleGuid = GetAttributeValue( "ApprovedPeople" ).AsGuid();
                    var approvedPeople = new DataViewService( _rockContext ).Get( approvedPeopleGuid );

                    if ( approvedPeople == null )
                    {
                        maWarning.Show( "Approved people block setting not found.", ModalAlertType.Alert );
                        return;
                    }
                    var errorMessages = new List<string>();
                    var approvedPeopleQry = approvedPeople.GetQuery( null, 30, out errorMessages );

                    if ( checkinPerson.Person.Age > 18 && !approvedPeopleQry.Where( dv => dv.Id == checkinPerson.Person.Id ).Any() )
                    {
                        btnCheckin.Visible = false;
                    }
                    else
                    {
                        btnCheckin.Visible = true;
                    }
                }
                else
                {
                    btnCheckin.Visible = true;
                }
            }
        }

        private void BuildPersonCheckinDetails()
        {
            if ( ViewState["SelectedPersonId"] != null )
            {
                var selectedPersonId = ( int ) ViewState["SelectedPersonId"];
                Person person = new PersonService( _rockContext ).Get( selectedPersonId );
                AttendanceService attendanceService = new AttendanceService( _rockContext );
                var reserved = attendanceService.Queryable().Where( a => a.CreatedDateTime > Rock.RockDateTime.Today
                    && a.DidAttend == false && a.PersonAliasId == person.PrimaryAliasId ).ToList();
                var current = attendanceService.Queryable().Where( a => a.CreatedDateTime > Rock.RockDateTime.Today
                    && a.DidAttend == true && a.EndDateTime == null && a.PersonAliasId == person.PrimaryAliasId ).ToList();
                var history = attendanceService.Queryable().Where( a => a.CreatedDateTime > Rock.RockDateTime.Today
                    && a.DidAttend == true && a.EndDateTime != null && a.PersonAliasId == person.PrimaryAliasId ).ToList();
                if ( reserved.Any() )
                {
                    pnlReserved.Visible = true;
                    gReserved.DataSource = reserved;
                    gReserved.DataBind();
                }
                else
                {
                    pnlReserved.Visible = false;
                }
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
            }
        }

        protected void CheckinReserved_Click( object sender, RowEventArgs e )
        {
            var attendanceItemId = ( int ) e.RowKeyValue;
            var attendanceItem = new AttendanceService( _rockContext ).Get( attendanceItemId );
            attendanceItem.DidAttend = true;
            attendanceItem.StartDateTime = Rock.RockDateTime.Now;
            _rockContext.SaveChanges();
            BuildPersonCheckinDetails();
        }

        protected void CancelReserved_Click( object sender, RowEventArgs e )
        {
            var attendanceItemId = ( int ) e.RowKeyValue;
            var attendanceService = new AttendanceService( _rockContext );
            var attendanceItem = attendanceService.Get( attendanceItemId );
            attendanceService.Delete( attendanceItem );
            _rockContext.SaveChanges();
            BuildPersonCheckinDetails();
        }

        protected void Checkout_Click( object sender, RowEventArgs e )
        {
            var attendanceItemId = ( int ) e.RowKeyValue;
            var attendanceItem = new AttendanceService( _rockContext ).Get( attendanceItemId );
            attendanceItem.EndDateTime = Rock.RockDateTime.Now;
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
            var selectedPersonId = ( int ) ViewState["SelectedPersonId"];
            var checkinPerson = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).First().People.Where( p => p.Person.Id == selectedPersonId ).FirstOrDefault();

            phCheckin.Controls.Clear();

            if ( checkinPerson == null )
            {
                return;
            }

            foreach ( var groupType in checkinPerson.GroupTypes )
            {
                //ignore group types with no non-excluded groups on non-super checkin
                if ( !cbSuperCheckin.Checked && !groupType.Groups.Where( g => !g.ExcludedByFilter ).Any() )
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

            ddlNewPersonSuffix.BindToDefinedType( DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSON_SUFFIX.AsGuid() ), true );

            tbNewPersonFirstName.Text = "";
            tbNewPersonLastName.Text = "";
            dpNewPersonBirthDate.Text = "";
            ypNewGraduation.Text = "";

            rblNewPersonGender.Items.Clear();
            rblNewPersonGender.BindToEnum<Gender>();

            ScriptManager.RegisterStartupScript( ddlGradePicker, ddlGradePicker.GetType(), "grade-selection-" + BlockId.ToString(), ddlGradePicker.GetJavascriptForYearPicker( ypNewGraduation ), true );
        }

        protected void btnSaveAddPerson_Click( object sender, EventArgs e )
        {
            if ( string.IsNullOrWhiteSpace( tbNewPersonFirstName.Text )
                || string.IsNullOrWhiteSpace( tbNewPersonLastName.Text )
                || string.IsNullOrWhiteSpace( dpNewPersonBirthDate.Text ) )
            {
                maWarning.Show( "First Name, Last Name, and Birthdate are required.", ModalAlertType.Alert );
                return;
            }

            Person person = new Person();
            person.FirstName = tbNewPersonFirstName.Text;
            person.LastName = tbNewPersonLastName.Text;
            person.RecordTypeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
            person.RecordStatusValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() ).Id;
            if ( !string.IsNullOrWhiteSpace( rblNewPersonGender.SelectedValue ) )
            {
                person.Gender = rblNewPersonGender.SelectedValueAsEnum<Gender>();
            }
            person.SetBirthDate( dpNewPersonBirthDate.Text.AsDateTime() );
            if ( ypNewGraduation.SelectedYear.HasValue )
            {
                person.GraduationYear = ypNewGraduation.SelectedYear.Value;
            }

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

            if ( person.ConnectionStatusValueId == DefinedValueCache.Read( GetAttributeValue( "ConnectionStatus" ).AsGuid() ).Id )
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
            ScriptManager.RegisterStartupScript( gpEditGrade, gpEditGrade.GetType(), "grade-selection-" + BlockId.ToString(), gpEditGrade.GetJavascriptForYearPicker( ypEditGraduation ), true );

            var AttributeList = new List<int>();

            string categoryGuid = GetAttributeValue( "CheckinCategory" );
            Guid guid = Guid.Empty;
            if ( Guid.TryParse( categoryGuid, out guid ) )
            {
                var category = CategoryCache.Read( guid );
                if ( category != null )
                {
                    AttributeList = new AttributeService( _rockContext ).GetByCategoryId( category.Id )
                        .OrderBy( a => a.Order ).ThenBy( a => a.Name ).Select( a => a.Id ).ToList();
                }
            }
            person.LoadAttributes();

            fsAttributes.Controls.Clear();

            foreach ( int attributeId in AttributeList )
            {
                var attribute = AttributeCache.Read( attributeId );
                string attributeValue = person.GetAttributeValue( attribute.Key );
                attribute.AddControl( fsAttributes.Controls, attributeValue, "", setValue, true );
            }
        }

        protected void btnSaveAttributes_Click( object sender, EventArgs e )
        {
            var changes = new List<string>();
            var personId = ( int ) ViewState["SelectedPersonId"];

            var person = new PersonService( _rockContext ).Get( personId );

            if ( person.ConnectionStatusValueId == DefinedValueCache.Read( GetAttributeValue( "ConnectionStatus" ).AsGuid() ).Id )
            {
                History.EvaluateChange( changes, "First Name", person.FirstName, tbEditFirst.Text );
                person.FirstName = tbEditFirst.Text;
                person.NickName = tbEditFirst.Text;
                History.EvaluateChange( changes, "Last Name", person.LastName, tbEditLast.Text );
                person.LastName = tbEditLast.Text;

                var checkinPerson = CurrentCheckInState.CheckIn.CurrentFamily.People.Where( p => p.Person.Id == person.Id ).FirstOrDefault();
                if ( checkinPerson != null )
                {
                    checkinPerson.Person = person.Clone( false );
                }
            }

            History.EvaluateChange( changes, "Birth Date", person.BirthDate, dpEditBirthDate.Text.AsDateTime() );
            person.SetBirthDate( dpEditBirthDate.Text.AsDateTime() );

            if ( ypEditGraduation.SelectedYear.HasValue )
            {
                History.EvaluateChange( changes, "Graduation Year", person.GraduationYear, ypEditGraduation.SelectedYear.Value );
                person.GraduationYear = ypEditGraduation.SelectedYear.Value;
            }
            else
            {
                History.EvaluateChange( changes, "Graduation Year", person.GraduationYear, null );
                person.GraduationYear = null;
            }

            int personEntityTypeId = EntityTypeCache.Read( typeof( Person ) ).Id;

            var AttributeList = new List<int>();

            string categoryGuid = GetAttributeValue( "CheckinCategory" );
            Guid guid = Guid.Empty;
            if ( Guid.TryParse( categoryGuid, out guid ) )
            {
                var category = CategoryCache.Read( guid );
                if ( category != null )
                {
                    AttributeList = new AttributeService( _rockContext ).GetByCategoryId( category.Id )
                        .OrderBy( a => a.Order ).ThenBy( a => a.Name ).Select( a => a.Id ).ToList();
                }
            }
            person.LoadAttributes();

            foreach ( int attributeId in AttributeList )
            {
                var attribute = AttributeCache.Read( attributeId );

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

                            History.EvaluateChange( changes, attribute.Name, formattedOriginalValue, formattedNewValue );
                        }
                    }
                }
            }
            if ( person.IsValid && changes.Any() )
            {
                HistoryService.SaveChanges( _rockContext, typeof( Person ), Rock.SystemGuid.Category.HISTORY_PERSON_DEMOGRAPHIC_CHANGES.AsGuid(),
                    person.Id, changes );
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
            foreach ( var person in CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault().People )
            {
                if ( person.GroupTypes.SelectMany( gt => gt.Groups ).SelectMany( g => g.Locations ).SelectMany( l => l.Schedules ).Where( s => s.Selected ).Any() )
                {
                    person.Selected = true;
                    foreach ( var groupType in person.GroupTypes )
                    {
                        groupType.Selected = true;
                        if ( groupType.Groups.SelectMany( g => g.Locations ).SelectMany( l => l.Schedules ).Where( s => s.Selected ).Any() )
                        {
                            foreach ( var group in groupType.Groups )
                            {
                                if ( group.Locations.SelectMany( l => l.Schedules ).Where( s => s.Selected ).Any() )
                                {
                                    group.Selected = true;
                                    foreach ( var location in group.Locations )
                                    {
                                        if ( location.Schedules.Where( s => s.Selected ).Any() )
                                        {
                                            location.Selected = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    person.Selected = false;
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
        }

        private void ClearLabels()
        {
            foreach (var family in CurrentCheckInState.CheckIn.Families.Where(f => f.Selected ) )
            {
                foreach (var person in family.People )
                {
                    foreach (var groupType in person.GroupTypes )
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
                    foreach ( var groupType in selectedPerson.GroupTypes.Where( gt => gt.Selected ) )
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
                        var labelCache = KioskLabel.Read( label.FileGuid );
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
            var familyGroupType = GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY );
            var adultRoleId = familyGroupType.Roles
                .Where( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ) )
                .Select( r => r.Id )
                .FirstOrDefault();

            //Adult1 Basic Info
            Person adult1 = new Person();
            adult1.FirstName = tbAdult1FirstName.Text;
            adult1.LastName = tbAdult1LastName.Text;
            adult1.SuffixValueId = ddlAdult1Suffix.SelectedValueAsId();
            adult1.ConnectionStatusValueId = DefinedValueCache.Read( GetAttributeValue( "ConnectionStatus" ).AsGuid() ).Id;
            adult1.RecordTypeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
            adult1.RecordStatusValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() ).Id;

            var newFamily = PersonService.SaveNewPerson( adult1, _rockContext );
            newFamily.Members.Where( m => m.Person == adult1 ).FirstOrDefault().GroupRoleId = adultRoleId;

            int homeLocationTypeId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() ).Id;
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
                var smsPhone = DefinedValueCache.Read( GetAttributeValue( "SMSPhone" ).AsGuid() ).Id;
                adult1.UpdatePhoneNumber( smsPhone, PhoneNumber.DefaultCountryCode(), pnbAdult1Phone.Text, true, false, _rockContext );
            }
            else
            {
                var otherPhone = DefinedValueCache.Read( GetAttributeValue( "OtherPhone" ).AsGuid() ).Id;
                adult1.UpdatePhoneNumber( otherPhone, PhoneNumber.DefaultCountryCode(), pnbAdult1Phone.Text, false, false, _rockContext );
            }

            if ( !string.IsNullOrWhiteSpace( tbAdult2FirstName.Text ) && !string.IsNullOrWhiteSpace( tbAdult2LastName.Text ) )
            {
                //Adult2 Basic Info
                Person adult2 = new Person();
                adult2.FirstName = tbAdult2FirstName.Text;
                adult2.LastName = tbAdult2LastName.Text;
                adult2.SuffixValueId = ddlAdult2Suffix.SelectedValueAsId();
                adult2.ConnectionStatusValueId = DefinedValueCache.Read( GetAttributeValue( "ConnectionStatus" ).AsGuid() ).Id;
                adult2.RecordTypeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                adult2.RecordStatusValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() ).Id;

                PersonService.AddPersonToFamily( adult2, true, newFamily.Id, adultRoleId, _rockContext );

                if ( cbAdult2SMS.Checked )
                {
                    var smsPhone = DefinedValueCache.Read( GetAttributeValue( "SMSPhone" ).AsGuid() ).Id;
                    adult2.UpdatePhoneNumber( smsPhone, PhoneNumber.DefaultCountryCode(), pnbAdult2Phone.Text, true, false, _rockContext );
                }
                else
                {
                    var otherPhone = DefinedValueCache.Read( GetAttributeValue( "OtherPhone" ).AsGuid() ).Id;
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
            if ( !GetAttributeValue( "AllowReprint" ).AsBoolean() )
            {
                return;
            }
            List<string> errorMessages = new List<string>();
            ProcessActivity( GetAttributeValue( "ReprintActivity" ), out errorMessages );
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

                foreach ( var checkinPerson in CurrentCheckInState.CheckIn.CurrentFamily.People )
                {
                    if ( checkinPerson.Person.Id == personId )
                    {
                        checkinPerson.Selected = true;
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
                        maWarning.Show( "No phone number found.", ModalAlertType.Alert );
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
                        var errorMessages = new List<string>();
                        var securityMembersQry = securityMembers.GetQuery( null, 30, out errorMessages );
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
                            userLogin.EntityTypeId = EntityTypeCache.Read( "Rock.Security.Authentication.PINAuthentication" ).Id;
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
    }
    public class FamilyLabel
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