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
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.SportsAndFitness
{
    [DisplayName( "Activities Checkin" )]
    [Category( "SECC > Check-in" )]
    [Description( "Block to check into activites center." )]
    [TextField( "Checkin Activity", "Activity for completing checkin.", false )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP_MEMBER, "Expiration Date Attribute", "Select the attribute used to filter by expiration.", true, false, order: 3 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Connection Status", "Connection status for new people." )]
    [GroupRoleField( Rock.SystemGuid.GroupType.GROUPTYPE_KNOWN_RELATIONSHIPS, "Can CheckIn Relationship" )]
    [CustomDropdownListField( "Required Signature Document", "Document that guests must have on file.", "SELECT Id AS [Value], [Name] AS [Text] FROM SignatureDocumentTemplate", true )]
    public partial class ActivitiesCheckin : CheckInBlock
    {

        #region Field

        private RockContext _rockContext;
        private int _noteTypeId;
        private string _expirationDateKey;
        private int _memberConnectionStatusId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_MEMBER.AsGuid() ).Id;
        private List<GroupTypeCache> parentGroupTypesList;
        private GroupTypeCache currentParentGroupType;

        #endregion

        #region Events
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            if ( !KioskCurrentlyActive )
            {
                NavigateToHomePage();
            }

            _rockContext = new RockContext();
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( Request["__EVENTTARGET"] == "PhotoUpload" )
            {
                UpdatePhoto( Request["__EVENTARGUMENT"] );
            }

            var expirationDateAttributeGuid = GetAttributeValue( "ExpirationDateAttribute" ).AsGuid();
            if ( expirationDateAttributeGuid != Guid.Empty )
            {
                _expirationDateKey = AttributeCache.Get( expirationDateAttributeGuid, _rockContext ).Key;
            }



            if ( !Page.IsPostBack )
            {
                List<string> errors = new List<string>();
                string workflowActivity = GetAttributeValue( "WorkflowActivity" );
                try
                {
                    bool test = ProcessActivity( workflowActivity, out errors );
                }
                catch ( Exception ex )
                {
                    LogException( ex );
                    NavigateToPreviousPage();
                    return;
                }

                if ( CurrentCheckInState == null )
                {
                    NavigateToPreviousPage();
                    return;
                }

                //people can be preselected coming out of the workflow, we want to unselect them
                foreach ( var person in CurrentCheckInState.CheckIn.Families.Where( s => s.Selected ).SelectMany( f => f.People ) )
                {
                    person.Selected = false;
                }
                SaveState();
                Session["modalActive"] = false;
                Session["selectedGuest"] = null;

                rblGender.BindToEnum<Gender>();
            }

            _noteTypeId = ( int? ) GetCacheItem( "NoteTypeId" ) ?? 0;

            if ( _noteTypeId == 0 )
            {
                var groupMemberEntityId = new EntityTypeService( _rockContext ).Get( Rock.SystemGuid.EntityType.GROUP_MEMBER.AsGuid() ).Id;
                var groupMemberNoteType = new NoteTypeService( _rockContext ).Queryable()
                    .Where( nt => nt.EntityTypeId == groupMemberEntityId ).FirstOrDefault();
                if ( groupMemberNoteType != null )
                {
                    _noteTypeId = groupMemberNoteType.Id;
                    AddCacheItem( _noteTypeId );
                }
            }

            BuildMemberCards();
            if ( Session["selectedGuest"] != null )
            {
                ShowGuestOfModal();
            }
            else if ( Session["modalActive"] != null && ( bool ) Session["modalActive"] )
            {
                ShowAddPersonModal();
            }
        }

        protected void btnCancel_Click( object sender, EventArgs e )
        {
            NavigateToPreviousPage();
        }

        protected void btnCheckin_Click( object sender, EventArgs e )
        {
            var locationlessPeople = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).SelectMany( f => f.People )
                .Where( p => p.Selected && !p.GroupTypes.Where( gt => gt.Selected ).Any() ).ToList();
            if ( locationlessPeople.Any() )
            {
                if ( locationlessPeople.Count == 1 )
                {
                    maError.Show( locationlessPeople.First().Person.FullName + " is selected for check-in, but does not have any locations selected.", ModalAlertType.Alert );
                }
                else
                {
                    maError.Show( "The following people are selected for check-in in but do not have any locations selected: " + string.Join( ", ", locationlessPeople.Select( p => p.Person.FullName ) ), ModalAlertType.Alert );
                }
                return;
            }
            List<string> errors = new List<string>();
            string workflowActivity = GetAttributeValue( "CheckinActivity" );
            try
            {
                bool test = ProcessActivity( workflowActivity, out errors );
                NavigateToNextPage();
            }
            catch
            {
                maError.Show( "There was an issue processing check-in. If the problem persists, please contact your administrator.", ModalAlertType.Warning );
            }
        }

        protected void btnBack_Click( object sender, EventArgs e )
        {
            NavigateToPreviousPage();
        }

        protected void btnAdd_Click( object sender, EventArgs e )
        {
            ShowAddPersonModal();
        }

        protected void btnPhone_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            PhoneNumberService phoneNumberService = new PhoneNumberService( rockContext );
            if ( !string.IsNullOrWhiteSpace( tbSearch.Text ) )
            {
                var people = phoneNumberService.GetBySearchterm( tbSearch.Text )
                    .Select( pn => pn.Person )
                    .DistinctBy( p => p.Id )
                    .ToList() //Leave EF
                    .Select( p => new GridPerson
                    {
                        Person = p,
                        Id = p.Id,
                        Address = p.GetHomeLocation().ToString()
                    } )
                    .ToList();
                gPeopleToAdd.DataSource = people;
                gPeopleToAdd.DataBind();
            }
            gPeopleToAdd.Visible = true;
            btnOpenCreatePerson.Visible = true;
        }

        protected void btnName_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );
            if ( !string.IsNullOrWhiteSpace( tbSearch.Text ) )
            {
                var people = personService.GetByFullName( tbSearch.Text, false )
                    .ToList() //Leave EF
                    .Select( p => new GridPerson
                    {
                        Person = p,
                        Id = p.Id,
                        Address = p.GetHomeLocation() != null ? p.GetHomeLocation().ToString() : ""
                    } )
                    .ToList();
                gPeopleToAdd.DataSource = people;
                gPeopleToAdd.DataBind();
            }
            gPeopleToAdd.Visible = true;
            btnOpenCreatePerson.Visible = true;
        }

        protected void lbAddGuest_Click( object sender, RowEventArgs e )
        {
            AddCheckInRelationship( ( int ) e.RowKeyValue );
        }
        protected void btnCancelSearch_Click( object sender, EventArgs e )
        {
            gPeopleToAdd.Visible = false;
            btnOpenCreatePerson.Visible = false;
            mdSearchPerson.Hide();
        }

        protected void btnOpenCreatePerson_Click( object sender, EventArgs e )
        {
            mdSearchPerson.Hide();
            mdCreatePerson.Show();
        }

        protected void lbCancel_Click( object sender, EventArgs e )
        {
            mdCreatePerson.Hide();
        }

        protected void btnNewFamily_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            Person person = new Person();
            person.FirstName = tbFirstName.Text;
            person.LastName = tbLastName.Text;
            person.SuffixValueId = ddlSuffix.SelectedValueAsId();
            person.Email = ebEmail.Text;
            person.SetBirthDate( bpBirthday.SelectedDate );

            person.ConnectionStatusValueId = DefinedValueCache.Get( GetAttributeValue( "ConnectionStatus" ).AsGuid() ).Id;
            person.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
            person.RecordStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() ).Id;

            var newFamily = PersonService.SaveNewPerson( person, rockContext, cpNewFamilyCampus.SelectedCampusId );

            int homeLocationTypeId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() ).Id;
            var familyLocation = new Location();
            familyLocation.Street1 = acNewFamilyAddress.Street1;
            familyLocation.Street2 = acNewFamilyAddress.Street2;
            familyLocation.City = acNewFamilyAddress.City;
            familyLocation.State = acNewFamilyAddress.State;
            familyLocation.PostalCode = acNewFamilyAddress.PostalCode;
            newFamily.GroupLocations.Add( new GroupLocation() { Location = familyLocation, GroupLocationTypeValueId = homeLocationTypeId } );

            if ( cbSMS.Checked )
            {
                var smsPhone = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).Id;
                person.UpdatePhoneNumber( smsPhone, PhoneNumber.DefaultCountryCode(), pnbPhone.Text, true, false, _rockContext );
            }
            else
            {
                var otherPhone = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() ).Id;
                person.UpdatePhoneNumber( otherPhone, PhoneNumber.DefaultCountryCode(), pnbPhone.Text, false, false, _rockContext );
            }

            rockContext.SaveChanges();

            AddCheckInRelationship( person.Id );
        }

        protected void btnCameraCancel_Click( object sender, EventArgs e )
        {
            mdCamera.Hide();
        }
        #endregion

        #region Methods
        private void UpdatePhoto( string requestJSON )
        {
            var requestObj = JsonConvert.DeserializeObject<Dictionary<string, object>>( requestJSON );
            if ( requestObj.ContainsKey( "PersonId" )
                && requestObj["PersonId"] is long
                && requestObj.ContainsKey( "Image" )
                && requestObj["Image"] is string
                && !string.IsNullOrWhiteSpace( ( string ) requestObj["Image"] ) )
            {
                var data = new BinaryFileData()
                {
                    Content = Convert.FromBase64String( ( ( string ) requestObj["Image"] ).Replace( "data:image/jpeg;base64,", "" ) )
                };

                var rockContext = new RockContext();
                var personService = new PersonService( rockContext );
                var person = personService.Get( ( int ) ( ( long ) requestObj["PersonId"] ) );

                if ( person == null )
                {
                    return;
                }


                var file = new BinaryFile()
                {
                    MimeType = "image/jpg",
                    DatabaseData = data,
                    FileName = person.FullName,
                    BinaryFileType = new BinaryFileTypeService( rockContext ).Get( Rock.SystemGuid.BinaryFiletype.PERSON_IMAGE.AsGuid() ),
                };
                var binaryFileService = new BinaryFileService( rockContext );
                binaryFileService.Add( file );
                rockContext.SaveChanges();

                person.PhotoId = file.Id;
                rockContext.SaveChanges();
                AddOrUpdatePersonInPhotoRequestGroup( person, rockContext );
                var ciPerson = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                    .FirstOrDefault()
                    .People.Where( p => p.Person.Id == person.Id )
                    .FirstOrDefault();
                if ( ciPerson != null )
                {
                    ciPerson.Person.PhotoId = file.Id;
                }
                mdCamera.Hide();
            }
        }

        private void AddOrUpdatePersonInPhotoRequestGroup( Person person, RockContext rockContext )
        {
            GroupService service = new GroupService( rockContext );
            var _photoRequestGroup = service.GetByGuid( Rock.SystemGuid.Group.GROUP_PHOTO_REQUEST.AsGuid() );

            var groupMember = _photoRequestGroup.Members.Where( m => m.PersonId == person.Id ).FirstOrDefault();
            if ( groupMember == null )
            {
                groupMember = new GroupMember();
                groupMember.GroupId = _photoRequestGroup.Id;
                groupMember.PersonId = person.Id;
                groupMember.GroupRoleId = _photoRequestGroup.GroupType.DefaultGroupRoleId ?? -1;
                _photoRequestGroup.Members.Add( groupMember );
            }

            groupMember.GroupMemberStatus = GroupMemberStatus.Pending;
        }

        private void BuildMemberCards()
        {
            phMembers.Controls.Clear();
            if ( CurrentCheckInState == null )
            {
                NavigateToPreviousPage();
                return;
            }
            var family = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
            if ( family == null || !family.People.Where( p => p.GroupTypes.Where( gt => gt.Groups.Any() ).Any() ).Any() )
            {
                DisplayNoEligibleMembers();
                return;
            }
            var count = 0;
            foreach ( var person in family.People.Where( p => p.FamilyMember ).Where( p => p.GroupTypes.Where( gt => gt.Groups.Any() ).Any() ) )
            {
                var card = new Panel();
                card.CssClass = "well col-md-3 col-sm-4 col-xs-12";
                phMembers.Controls.Add( card );

                PlaceHolder phNotes = new PlaceHolder();
                card.Controls.Add( phNotes );

                Panel pnlCheckbox = new Panel();
                pnlCheckbox.CssClass = "col-sm-6 col-xs-12";
                card.Controls.Add( pnlCheckbox );

                var btnCheckbox = new BootstrapButton
                {
                    CausesValidation = false
                };
                if ( person.Selected )
                {
                    btnCheckbox.CssClass = "btn btn-success btn-lg btn-block";
                    btnCheckbox.Text = "<i class='fa fa-check-square-o fa-5x'></i><br>" + person.Person.NickName;
                    if ( person.Person.Age < 18 )
                    {
                        btnCheckbox.Text += "<br>" + person.Person.FormatAge();
                    }
                    btnCheckbox.DataLoadingText = "<i class='fa fa-check-square-o fa-5x'></i><br>Loading...";
                }
                else
                {
                    btnCheckbox.CssClass = "btn btn-default btn-lg  btn-block";
                    btnCheckbox.Text = "<i class='fa fa-square-o fa-5x'></i><br>" + person.Person.NickName;
                    if ( person.Person.Age < 18 )
                    {
                        btnCheckbox.Text += "<br>" + person.Person.FormatAge();
                    }
                    btnCheckbox.DataLoadingText = "<i class='fa fa-square-o fa-5x'></i><br>Loading...";
                }
                btnCheckbox.Click += ( s, e ) => { SelectPerson( person ); };
                btnCheckbox.ID = "btn" + person.Person.Id.ToString();
                pnlCheckbox.Controls.Add( btnCheckbox );

                Panel pnlImage = new Panel();
                pnlImage.CssClass = "col-sm-6 col-xs-12";
                card.Controls.Add( pnlImage );
                LinkButton lbImage = new LinkButton();
                lbImage.Click += ( s, e ) => { OpenCameraModal( person ); };

                pnlImage.Controls.Add( lbImage );
                Image imgPhoto = new Image();
                imgPhoto.CssClass = "thumbnail";
                if ( person.ExcludedByFilter )
                {
                    imgPhoto.Style.Add( "border", "solid red 3px" );
                }
                else if ( person.Person.ConnectionStatusValueId != _memberConnectionStatusId && person.Person.GetAttributeValue( "Employer" ) != "Southeast Christian Church" )
                {
                    imgPhoto.Style.Add( "border", "solid blue 3px" );
                }
                imgPhoto.ImageUrl = person.Person.PhotoUrl;
                imgPhoto.Style.Add( "width", "100%" );
                lbImage.Controls.Add( imgPhoto );

                Literal ltName = new Literal();
                ltName.Text = "<h1 class='col-xs-12'>" + person.Person.FullNameFormal + "</h1>";
                card.Controls.Add( ltName );

                foreach ( var gt in person.GroupTypes )
                {
                    foreach ( var g in gt.Groups )
                    {
                        List<Note> notes = GetGroupMemberNotes( person, g.Group );
                        foreach ( var note in notes )
                        {
                            NotificationBox nbNote = new NotificationBox();
                            if ( note.IsAlert == true )
                            {
                                nbNote.NotificationBoxType = NotificationBoxType.Danger;
                            }
                            else
                            {
                                nbNote.NotificationBoxType = NotificationBoxType.Info;
                            }
                            nbNote.Text = nbNote.Text = note.Text;
                            phNotes.Controls.Add( nbNote );
                        }

                        Literal ltGroup = new Literal();
                        ltGroup.Text = "<b>" + g.Group.Name + "</b><br>";
                        card.Controls.Add( ltGroup );
                        foreach ( var l in g.Locations )
                        {
                            BootstrapButton btnLocation = new BootstrapButton
                            {
                                CausesValidation = false
                            };
                            if ( l.Selected )
                            {
                                btnLocation.CssClass = "btn btn-success";
                                btnLocation.Text = "<i class='fa fa-check-square-o'></i>";
                            }
                            else
                            {
                                btnLocation.CssClass = "btn btn-default";
                                btnLocation.Text = "<i class='fa fa-square-o'></i>";
                            }
                            btnLocation.ID = "c" + person.Person.Id.ToString() + g.Group.Guid + l.Location.Id.ToString();
                            btnLocation.Click += ( s, e ) => { SelectLocation( person, gt, g, l ); };
                            card.Controls.Add( btnLocation );
                            Literal ltLocation = new Literal();
                            ltLocation.Text = " " + l.Location.Name + "<br>";
                            card.Controls.Add( ltLocation );
                        }
                    }
                }
                //clearfix
                count++;
                if ( count == 4 )
                {
                    Panel panelClearfix = new Panel
                    {
                        CssClass = "clearfix"
                    };
                    phMembers.Controls.Add( panelClearfix );
                    count = 0;
                }
            }
        }

        private void OpenCameraModal( CheckInPerson person )
        {
            mdCamera.Show();
            mdCamera.Title = person.Person.FullName;
            var script = string.Format( @"
var video = document.getElementById('video');
// Get access to the camera!
if(navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {{
    navigator.mediaDevices.getUserMedia({{ video: true }}).then(function(stream) {{
        video.srcObject = stream;
        video.play();
    }});
}}

var canvas = document.getElementById('canvas');
var context = canvas.getContext('2d');
var video = document.getElementById('video');
var saveButton = document.getElementById('saveImage')

// Trigger photo take
document.getElementById(""snap"").addEventListener(""click"", function() {{
    context.drawImage( video, 0, 0, 640, 480 );
photoDiv.className = 'text-center';
videoDiv.className = 'hidden'
        }});

// photo redo
document.getElementById(""retakeImage"").addEventListener(""click"", function() {{
    context.drawImage( video, 0, 0, 640, 480 );
photoDiv.className = 'hidden';
videoDiv.className = 'text-center'
        }});

saveButton.addEventListener(""click"", function() {{
var req = new Object();
req['PersonId'] = {0};
req['Image']=canvas.toDataURL('image/jpeg');
__doPostBack('PhotoUpload', JSON.stringify(req))
}});", person.Person.Id );
            ScriptManager.RegisterStartupScript( this, this.GetType(), "Camera", script, true );
        }

        private void ShowGuestOfModal()
        {
            phAddPerson.Controls.Clear();
            var guest = Session["selectedGuest"] as CheckInPerson;

            mdAddPerson.Title = guest.Person.FullName + " is a guest of:";

            var family = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
            var people = family
                .People
                .Where( p => p.FamilyMember && p.ExcludedByFilter == false )
                .Where( p => p.GroupTypes.Where( gt => gt.Groups.Any() ).Any() );

            foreach ( var person in people )
            {

                Panel hgcPadding = new Panel();
                hgcPadding.CssClass = "col-md-8 col-md-offset-2 col-xs-12";
                hgcPadding.Style.Add( "padding", "5px" );
                phAddPerson.Controls.Add( hgcPadding );

                BootstrapButton btnPerson = new BootstrapButton();
                btnPerson.ID = "btnAddGuestOf" + person.Person.Id.ToString();
                btnPerson.Text = person.Person.FullName;

                btnPerson.CssClass = "btn btn-success btn-block btn-lg";
                btnPerson.Click += ( s, e ) =>
                {
                    var guestReference = family.People.Where( p => p.Person.Id == guest.Person.Id ).FirstOrDefault();
                    guestReference.FamilyMember = true;
                    //hijacking the excluded by filter to show they are not card holders
                    guestReference.ExcludedByFilter = true;
                    //we are hijacking the security code to pass throug the guest of information
                    RockContext rockContext = new RockContext();
                    PersonAliasService personAliasService = new PersonAliasService( rockContext );
                    var personAliasId = personAliasService.GetPrimaryAliasId( person.Person.Id ) ?? 0;
                    guestReference.SecurityCode = personAliasId.ToString();
                    Session["selectedGuest"] = null;
                    SaveState();
                    mdAddPerson.Hide();
                    CheckForRelease( guestReference );
                    BuildMemberCards();
                };
                btnPerson.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i> Adding: " + person.Person.FullName + " as guest sponsor...";
                hgcPadding.Controls.Add( btnPerson );
            }
            Panel hgcCancelPadding = new Panel();
            hgcCancelPadding.CssClass = "col-md-8 col-md-offset-2 col-xs-12";
            hgcCancelPadding.Style.Add( "padding", "5px" );
            phAddPerson.Controls.Add( hgcCancelPadding );

            BootstrapButton btnDone = new BootstrapButton
            {
                ID = "btnDone",
                Text = "Cancel",
                CssClass = "btn btn-danger btn-lg col-md-8 col-xs-12 btn-block",
            };
            btnDone.Click += ( s, e ) =>
            {
                Session["selectedGuest"] = null;
                mdAddPerson.Hide();
            };
            btnCancel.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i> Closing...";
            hgcCancelPadding.Controls.Add( btnDone );

        }

        private void SelectLocation( CheckInPerson person, CheckInGroupType gt, CheckInGroup g, CheckInLocation l )
        {
            l.Selected = !l.Selected;
            if ( l.Selected )
            {
                person.Selected = true;
                gt.Selected = true;
                g.Selected = true;
                foreach ( var s in l.Schedules )
                {
                    s.Selected = true;
                }
            }
            else
            {
                foreach ( var s in l.Schedules )
                {
                    s.Selected = false;
                }
                if ( !g.Locations.Where( _l => _l.Selected ).Any() )
                {
                    g.Selected = false;
                }
                if ( !gt.Groups.Where( _g => _g.Selected ).Any() )
                {
                    gt.Selected = false;
                }
                if ( !person.GroupTypes.Where( _gt => _gt.Selected ).Any() )
                {
                    person.Selected = false;
                }
            }
            SaveState();
            BuildMemberCards();
        }

        private void SelectPerson( CheckInPerson person )
        {
            person.Selected = !person.Selected;
            SaveState();
            BuildMemberCards();
        }

        private List<Note> GetGroupMemberNotes( CheckInPerson person, Rock.Model.Group group )
        {
            List<Note> notes = new List<Note>();
            Rock.Model.Group membershipGroup = GetMembershipGroup( person, group );
            if ( membershipGroup == null )
            {
                return notes;
            }
            var groupMember = new GroupMemberService( _rockContext ).Queryable().Where( gm => gm.PersonId == person.Person.Id && gm.GroupId == membershipGroup.Id ).FirstOrDefault();
            if ( groupMember == null )
            {
                return notes;
            }
            if ( !string.IsNullOrWhiteSpace( groupMember.Note ) )
            {
                Note note = new Note();
                note.Text = groupMember.Note;
                note.IsAlert = true;
                notes.Add( note );
            }

            //This line is commented out because sports and fitnes has complained that they
            //are seeing notes they did not add

            //notes.AddRange( new NoteService( _rockContext ).Get( _noteTypeId, groupMember.Id ).ToList() );

            switch ( GroupMembershipExpired( groupMember ) )
            {
                case GroupMembershipStatus.Expired:
                    Note eNote = new Note();
                    eNote.Text = "Card expired or no longer church member.";
                    eNote.IsAlert = true;
                    notes.Add( eNote );
                    break;
                case GroupMembershipStatus.NearExpired:
                    Note neNote = new Note();
                    neNote.Text = "Card will expire soon.";
                    neNote.IsAlert = false;
                    notes.Add( neNote );
                    break;
            }

            return notes;
        }

        private GroupMembershipStatus GroupMembershipExpired( GroupMember groupMember )
        {
            if ( groupMember.Person.ConnectionStatusValue.Guid == Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_MEMBER.AsGuid() )
            {
                return GroupMembershipStatus.Member;
            }

            var person = groupMember.Person;
            person.LoadAttributes();
            if ( person.GetAttributeValue( "Employer" ) == "Southeast Christian Church" )
            {
                return GroupMembershipStatus.Member;
            }

            groupMember.LoadAttributes();
            var expirationDate = groupMember.GetAttributeValue( _expirationDateKey ).AsDateTime() ?? Rock.RockDateTime.Today.AddDays( -1 );

            if ( expirationDate < Rock.RockDateTime.Today )
            {
                return GroupMembershipStatus.Expired;
            }
            else if ( expirationDate < Rock.RockDateTime.Today.AddDays( 30 ) )
            {
                return GroupMembershipStatus.NearExpired;
            }
            else
            {
                return GroupMembershipStatus.NotExpired;
            }
        }

        private Rock.Model.Group GetMembershipGroup( CheckInPerson person, Rock.Model.Group group )
        {
            group.LoadAttributes();
            var membershipGroupGuid = group.GetAttributeValue( "Group" );

            if ( membershipGroupGuid == null )
            {
                return null;
            }
            var membershipGroup = new GroupService( _rockContext ).Get( membershipGroupGuid.AsGuid() );
            return membershipGroup;
        }

        private void DisplayNoEligibleMembers()
        {
            pnlNoEligibleMembers.Visible = true;
            pnlMain.Visible = false;
        }

        private void ShowAddPersonModal()
        {
            Session["modalActive"] = true;
            phAddPerson.Controls.Clear();
            var people = CurrentCheckInState.CheckIn.Families
                .SelectMany( f => f.People )
                .Where( p => !p.FamilyMember )
                .OrderByDescending( p => p.Person.Age );

            mdAddPerson.Title = "Select Person To Add:";

            foreach ( var person in people )
            {
                Panel hgcPadding = new Panel();
                hgcPadding.CssClass = "col-md-8 col-md-offset-2 col-xs-12";
                hgcPadding.Style.Add( "padding", "5px" );
                phAddPerson.Controls.Add( hgcPadding );

                BootstrapButton btnPerson = new BootstrapButton();
                btnPerson.ID = "btnAddPerson" + person.Person.Id.ToString();
                btnPerson.Text = person.Person.FullName;

                btnPerson.CssClass = "btn btn-success btn-block btn-lg";
                btnPerson.Click += ( s, e ) =>
                {
                    Session["modalActive"] = false;
                    Session["selectedGuest"] = person;
                    SaveState();
                    ShowGuestOfModal();
                };
                btnPerson.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i> Adding: " + person.Person.FullName + " to check-in...";
                hgcPadding.Controls.Add( btnPerson );
            }

            Panel hgcNewPadding = new Panel();
            hgcNewPadding.CssClass = "col-md-8 col-md-offset-2 col-xs-12";
            hgcNewPadding.Style.Add( "padding", "5px" );
            phAddPerson.Controls.Add( hgcNewPadding );

            BootstrapButton btnNew = new BootstrapButton
            {
                ID = "btnNew",
                Text = "Add New Guest",
                CssClass = "btn btn-warning btn-lg col-md-8 col-xs-12 btn-block",
                CausesValidation = false
            };
            btnNew.Click += ( s, e ) =>
            {
                Session["modalActive"] = false;
                mdSearchPerson.Show();
                mdAddPerson.Hide();
            };
            btnCancel.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i> Closing...";
            hgcNewPadding.Controls.Add( btnNew );



            Panel hgcCancelPadding = new Panel();
            hgcCancelPadding.CssClass = "col-md-8 col-md-offset-2 col-xs-12";
            hgcCancelPadding.Style.Add( "padding", "5px" );
            phAddPerson.Controls.Add( hgcCancelPadding );

            BootstrapButton btnDone = new BootstrapButton
            {
                ID = "btnDone",
                Text = "Cancel",
                CssClass = "btn btn-danger btn-lg col-md-8 col-xs-12 btn-block",
                CausesValidation = false
            };
            btnDone.Click += ( s, e ) =>
            {
                Session["modalActive"] = false;
                mdAddPerson.Hide();
            };
            btnCancel.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i> Closing...";
            hgcCancelPadding.Controls.Add( btnDone );

            mdAddPerson.Show();
        }
        private void CheckForRelease( CheckInPerson guestReference )
        {
            var signatureDocumentTemplateId = GetAttributeValue( "RequiredSignatureDocument" ).AsInteger();
            RockContext rockContext = new RockContext();
            SignatureDocumentService signatureDocumentService = new SignatureDocumentService( rockContext );
            PersonAliasService personAliasService = new PersonAliasService( rockContext );
            var personAliases = personAliasService.Queryable().Where( pa => pa.PersonId == guestReference.Person.Id ).Select( pa => pa.Id );
            var documents = signatureDocumentService.Queryable()
                .Where( d => d.SignedByPersonAlias != null
                && d.SignatureDocumentTemplateId == signatureDocumentTemplateId
                && personAliases.Contains( d.AppliesToPersonAliasId ?? 0 )
                && d.Status == SignatureDocumentStatus.Signed ).Any();
            if ( !documents )
            {
                maError.Show( guestReference.Person.FullName + " does not have a signed release on file. Do not allow them to check-in without signing a release.", ModalAlertType.Alert );
            }
        }

        private void AddCheckInRelationship( int guestId )
        {
            GetAttributeValue( "CanCheckInRelationship" );
            var knownRelationshipGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_KNOWN_RELATIONSHIPS );
            var canCheckInRole = knownRelationshipGroupType.Roles.FirstOrDefault( r => r.Guid.Equals( new Guid( GetAttributeValue( "CanCheckInRelationship" ) ) ) );
            if ( canCheckInRole != null )
            {
                var rockContext = new RockContext();
                var groupMemberService = new GroupMemberService( rockContext );


                var family = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
                var people = family
                    .People
                    .Where( p => p.FamilyMember && p.ExcludedByFilter == false )
                    .Where( p => p.GroupTypes.Where( gt => gt.Groups.Any() ).Any() );

                foreach ( var person in people )
                {
                    groupMemberService.CreateKnownRelationship( person.Person.Id, guestId, canCheckInRole.Id );
                }
            }

            //we have to reload the page to add the person
            Page.Response.Redirect( Page.Request.Url.ToString(), true );
        }
        #endregion

        #region Helpers
        enum GroupMembershipStatus
        {
            Expired,
            NearExpired,
            NotExpired,
            Member
        }

        private class GridPerson
        {
            public int Id { get; set; }
            public string Address { get; set; }
            public Person Person { get; set; }
        }
        #endregion



    }
}