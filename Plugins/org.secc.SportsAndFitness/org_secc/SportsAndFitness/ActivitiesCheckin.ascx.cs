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

namespace RockWeb.Plugins.org_secc.SportsAndFitness
{
    [DisplayName( "Activities Checkin" )]
    [Category( "SECC > Check-in" )]
    [Description( "Block to check into activites center." )]
    [TextField( "Checkin Activity", "Activity for completing checkin.", false )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP_MEMBER, "Expiration Date Attribute", "Select the attribute used to filter by expiration.", true, false, order: 3 )]

    public partial class ActivitiesCheckin : CheckInBlock
    {

        private RockContext _rockContext;
        private int _noteTypeId;
        private string _expirationDateKey;
        private int _memberConnectionStatusId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_MEMBER.AsGuid() ).Id;

        private List<GroupTypeCache> parentGroupTypesList;
        private GroupTypeCache currentParentGroupType;
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

            var expirationDateAttributeGuid = GetAttributeValue( "ExpirationDateAttribute" ).AsGuid();
            if ( expirationDateAttributeGuid != Guid.Empty )
            {
                _expirationDateKey = AttributeCache.Read( expirationDateAttributeGuid, _rockContext ).Key;
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
            foreach ( var person in family.People.Where( p => p.GroupTypes.Where( gt => gt.Groups.Any() ).Any() ) )
            {
                var card = new Panel();
                card.CssClass = "well col-md-3 col-sm-4 col-xs-12";
                phMembers.Controls.Add( card );

                PlaceHolder phNotes = new PlaceHolder();
                card.Controls.Add( phNotes );

                Panel pnlCheckbox = new Panel();
                pnlCheckbox.CssClass = "col-sm-6 col-xs-12";
                card.Controls.Add( pnlCheckbox );

                var btnCheckbox = new BootstrapButton();
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
                Image imgPhoto = new Image();
                imgPhoto.CssClass = "thumbnail";
                if ( person.Person.ConnectionStatusValueId != _memberConnectionStatusId )
                {
                    imgPhoto.Style.Add( "border", "solid blue 3px" );
                }
                imgPhoto.ImageUrl = person.Person.PhotoUrl;
                imgPhoto.Style.Add( "width", "100%" );
                pnlImage.Controls.Add( imgPhoto );

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
                            BootstrapButton btnLocation = new BootstrapButton();
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
            }
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

        enum GroupMembershipStatus
        {
            Expired,
            NearExpired,
            NotExpired,
            Member
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
    }
}