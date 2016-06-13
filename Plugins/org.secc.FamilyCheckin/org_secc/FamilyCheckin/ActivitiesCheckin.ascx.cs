using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Web.UI.Controls;
using Rock.Web.Cache;
using System.Web.UI.WebControls;
using System.Web.UI;
using Rock.Model;
using System.Reflection;
using System.Web.UI.HtmlControls;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Rock.Data;
using org.secc.FamilyCheckin.Utilities;

namespace RockWeb.Plugins.org_secc.FamilyCheckin
{
    [DisplayName( "Activities Checkin" )]
    [Category( "SECC > Check-in" )]
    [Description( "Block to check into activites center." )]
    [TextField( "Preselect Activity", "Activity for preselecting classes.", false )]

    public partial class ActivitiesCheckin : CheckInBlock
    {
        private RockContext _rockContext;
        private int _noteTypeId;

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

            if ( !Page.IsPostBack )
            {
                List<string> errors = new List<string>();
                string workflowActivity = GetAttributeValue( "WorkflowActivity" );
                try
                {
                    bool test = ProcessActivity( workflowActivity, out errors );
                }
                catch
                {
                    NavigateToPreviousPage();
                    Response.End();
                    return;
                }
            }

            _noteTypeId = (int?)GetCacheItem( "NoteTypeId" ) ?? 0;

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
            var family = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
            if ( family == null )
            {
                DisplayNoEligibleMembers();
                return;
            }
            foreach ( var person in family.People.Where( p => p.GroupTypes.Where( gt => gt.Groups.Any() ).Any() ) )
            {
                PlaceHolder phNotes = new PlaceHolder();
                phMembers.Controls.Add( phNotes );

                var card = new Panel();
                card.CssClass = "well";
                phMembers.Controls.Add( card );

                var btnCheckbox = new BootstrapButton();
                if ( person.Selected )
                {
                    btnCheckbox.CssClass = "btn btn-success btn-lg";
                    btnCheckbox.Text = "<i class='fa fa-check-square-o'></i>";
                }
                else
                {
                    btnCheckbox.CssClass = "btn btn-default btn-lg";
                    btnCheckbox.Text = "<i class='fa fa-square-o'></i>";
                }
                card.Controls.Add( btnCheckbox );

                Literal ltName = new Literal();
                ltName.Text = "<b>" + person.Person.FullName + "</b>";
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
                        ltGroup.Text = g.Group.Name;
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
                            btnLocation.ID = "c" + person.Person.Id.ToString() + l.Location.Id.ToString();
                            card.Controls.Add( btnLocation );
                            Literal ltLocation = new Literal();
                            ltLocation.Text = l.Location.Name;
                            card.Controls.Add( ltLocation );
                        }
                    }
                }
            }
        }

        private List<Note> GetGroupMemberNotes( CheckInPerson person, Rock.Model.Group group )
        {
            List<Note> notes = new List<Note>();
            var groupMember = new GroupMemberService( _rockContext ).Queryable().Where( gm => gm.PersonId == person.Person.Id && gm.GroupId == group.Id ).FirstOrDefault();
            if ( groupMember == null )
            {
                return notes;
            }
            if ( !string.IsNullOrWhiteSpace( groupMember.Note ) )
            {
                Note note = new Note();
                note.Text = groupMember.Note;
                note.IsAlert = false;
                notes.Add( note );
            }

            notes.AddRange( new NoteService( _rockContext ).Get( _noteTypeId, groupMember.Id ).ToList() );

            return notes;
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
            //Check-in and navigate home
        }

        protected void btnBack_Click( object sender, EventArgs e )
        {
            NavigateToPreviousPage();
        }
    }
}