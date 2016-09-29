using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.GroupManager
{
    /// <summary>
    /// User control for managing users sports and fitness participation
    /// </summary>
    [DisplayName( "Personal Group Information Panel" )]
    [Category( "SECC > Groups" )]
    [Description( "Allows for the editing of a person's sports and fitness status." )]

    [TextField( "Title", "The title to show on the panel", false, "Group Information", "CustomSetting" )]
    [TextField( "CSS Icon", "Font icon to show on the panel", false, "fa fa-group", "CustomSetting" )]
    [TextField( "Groups", "The groups to display information about", false, "", "CustomSetting" )]

    partial class PersonalGroupInformationPanel : RockBlockCustomSettings
    {
        private Person Person;

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            Person = this.ContextEntity<Person>();

            if ( Person == null )
            {
                // check the query string and attempt to load the person from it
                if ( PageParameter( "PersonId" ) != null )
                {
                    int personId = PageParameter( "PersonId" ).AsInteger();

                    Person = new PersonService( new RockContext() ).Get( personId );
                    Person.LoadAttributes();
                }

                if ( Person == null )
                {
                    Person = new Person();
                }
            }



            if ( Person != null && Person.Id != 0 )
            {
                ltTitle.Text = string.Format( "<i class='{0}'></i> {1}", GetAttributeValue( "CSSIcon" ), GetAttributeValue( "Title" ) );
                upnlAttributeValues.Visible = true;

                if ( !Page.IsPostBack )
                {
                    ShowView();
                }
                else
                {
                    if ( hfEditMode.Value == "1" )
                    {
                        ShowEdit();
                        if ( Request["__EVENTTARGET"] == "DeleteGroup" )
                        {
                            DeleteGroupMember( Request["__EVENTARGUMENT"].AsInteger() );
                            ShowEdit();
                        }
                    }
                    else
                    {
                        ShowView();
                    }
                }
            }
            else
            {
                upnlAttributeValues.Visible = false;
            }
        }

        private void DeleteGroupMember( int groupMemberId )
        {
            RockContext rockContext = new RockContext();
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            var groupMember = groupMemberService.Get( groupMemberId );
            if ( groupMember != null
                && groupMember.PersonId == Person.Id
                && groupMember.Group.IsAuthorized( Authorization.EDIT, CurrentPerson ) )
            {
                groupMemberService.Delete( groupMember );
                rockContext.SaveChanges();
            }
            ShowEdit();
        }

        private void ShowView()
        {
            phGroups.Controls.Clear();
            var groups = GetGroups();
            RockContext rockContext = new RockContext();
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            foreach ( var group in groups )
            {
                if (! group.IsAuthorized(Authorization.VIEW, CurrentPerson ) )
                {
                    continue;
                }

                var groupRoleIds = group.GroupType.Roles.Select( r => r.Id ).ToList();
                var groupMembers = groupMemberService.GetByGroupIdAndPersonId( group.Id, Person.Id );
                foreach ( var groupMember in groupMembers )
                {
                    var incorrectRoleIdAlert = "";
                    if (!groupRoleIds.Contains( groupMember.GroupRoleId ) )
                    {
                        incorrectRoleIdAlert = string.Format( " <i class='fa fa-exclamation-triangle' title='{0}'></i>",
                            group.GroupType.DefaultGroupRoleId );
                    }

                    Literal ltGroup = new Literal();
                    ltGroup.Text = string.Format( "<h4>{0} [{1}{2}]</h4>", group.Name, groupMember.GroupRole.Name, incorrectRoleIdAlert );
                    phGroups.Controls.Add( ltGroup );
                    string formattedValue = string.Empty;

                    groupMember.LoadAttributes();
                    var attributeList = groupMember.Attributes.Select( d => d.Value )
                         .OrderBy( a => a.Order ).ThenBy( a => a.Name ).ToList();
                    foreach ( var attribute in attributeList )
                    {
                        var attributeValue = groupMember.GetAttributeValue( attribute.Key );

                        if ( attribute.FieldType.Class == typeof( Rock.Field.Types.ImageFieldType ).FullName )
                        {
                            formattedValue = attribute.FieldType.Field.FormatValueAsHtml( phGroups, attributeValue, attribute.QualifierValues, true );
                        }
                        else
                        {
                            formattedValue = attribute.FieldType.Field.FormatValueAsHtml( phGroups, attributeValue, attribute.QualifierValues, false );
                        }

                        if ( !string.IsNullOrWhiteSpace( formattedValue ) )
                        {
                            phGroups.Controls.Add( new RockLiteral { Label = attribute.Name, Text = formattedValue } );
                        }

                        if ( !string.IsNullOrWhiteSpace( groupMember.Note ) )
                        {
                            RockLiteral ltNote = new RockLiteral();
                            ltNote.Label = "Note";
                            ltNote.Text = groupMember.Note;
                            phGroups.Controls.Add( ltNote );
                        }
                    }
                }
            }
        }

        private void ShowEdit( int savedGroupMemberId = 0 )
        {
            phGroups.Controls.Clear();
            var groups = GetGroups();
            RockContext rockContext = new RockContext();
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            foreach ( var group in groups )
            {
                Panel pnlGroup = new Panel();
                phGroups.Controls.Add( pnlGroup );

                Literal ltGroup = new Literal();
                ltGroup.Text = string.Format( "<h4>{0}</h4>", group.Name );
                pnlGroup.Controls.Add( ltGroup );

                var groupMembers = groupMemberService.GetByGroupIdAndPersonId( group.Id, Person.Id );
                var unusedRoles = group.GroupType.Roles.Where( r => !groupMembers.Select( gm => gm.GroupRoleId ).Contains( r.Id ) );

                if ( unusedRoles.Any() )
                {
                    Panel pnlButtonGroup = new Panel();
                    pnlButtonGroup.CssClass = "input-group";
                    pnlGroup.Controls.Add( pnlButtonGroup );

                    RockDropDownList ddlRole = new RockDropDownList();
                    ddlRole.DataSource = unusedRoles;
                    ddlRole.DataTextField = "Name";
                    ddlRole.DataValueField = "Id";
                    ddlRole.CssClass = "form-control";
                    ddlRole.ID = "ddl" + group.Id.ToString();
                    ddlRole.DataBind();
                    pnlButtonGroup.Controls.Add( ddlRole );

                    Panel pnlInputGroupButton = new Panel();
                    pnlInputGroupButton.CssClass = "input-group-btn";
                    pnlButtonGroup.Controls.Add( pnlInputGroupButton );

                    BootstrapButton btnAdd = new BootstrapButton();
                    btnAdd.Text = "Add To Group";
                    btnAdd.CssClass = "btn btn-primary";
                    btnAdd.ID = "Add" + group.Id.ToString();
                    btnAdd.Click += ( s, e ) => { AddGroupMember( group, ddlRole ); };
                    pnlInputGroupButton.Controls.Add( btnAdd );
                }

                foreach ( var groupMember in groupMembers )
                {
                    Panel pnlWell = new Panel();
                    pnlWell.CssClass = "well";
                    phGroups.Controls.Add( pnlWell );

                    Literal ltRole = new Literal();
                    ltRole.Text = string.Format( "<b><em>Group Role: {0}</em></b> ", groupMember.GroupRole.Name );
                    pnlWell.Controls.Add( ltRole );

                    BootstrapButton btnRemove = new BootstrapButton();
                    btnRemove.Text = "Remove";
                    btnRemove.CssClass = "btn btn-danger btn-xs pull-right";
                    btnRemove.ID = groupMember.Id.ToString();
                    btnRemove.Click += ( s, e ) => { ConfirmDelete( groupMember.Id ); };
                    pnlWell.Controls.Add( btnRemove );

                    HtmlGenericContainer hr = new HtmlGenericContainer( "hr" );
                    pnlWell.Controls.Add( hr );

                    groupMember.LoadAttributes();
                    var attributeList = groupMember.Attributes.Select( d => d.Value )
                         .OrderBy( a => a.Order ).ThenBy( a => a.Name ).ToList();
                    foreach ( var attribute in attributeList )
                    {
                        string attributeValue = groupMember.GetAttributeValue( attribute.Key );
                        attribute.AddControl( pnlWell.Controls, attributeValue, "", true, true );
                    }

                    RockTextBox tbNotes = new RockTextBox();
                    tbNotes.TextMode = TextBoxMode.MultiLine;
                    tbNotes.ID = "Note" + groupMember.Id.ToString();
                    tbNotes.Label = "Notes";
                    tbNotes.Text = groupMember.Note;
                    pnlWell.Controls.Add( tbNotes );

                    if (savedGroupMemberId == groupMember.Id )
                    {
                        NotificationBox nbSavedNote = new NotificationBox();
                        nbSavedNote.Text = "Group member information saved";
                        nbSavedNote.NotificationBoxType = NotificationBoxType.Success;
                        pnlWell.Controls.Add( nbSavedNote );
                    }

                    BootstrapButton btnSave = new BootstrapButton();
                    btnSave.ID = "save" + groupMember.Id.ToString();
                    btnSave.Text = "Save";
                    btnSave.CssClass = "btn btn-primary btn-xs";
                    btnSave.Click += ( s, e ) => { SaveGroupMember( rockContext, groupMember, pnlWell, attributeList, tbNotes ); };
                    pnlWell.Controls.Add( btnSave );
                }
            }

            HtmlGenericContainer hrDone = new HtmlGenericContainer( "hr" );
            phGroups.Controls.Add( hrDone );

            BootstrapButton btnDone = new BootstrapButton();
            btnDone.Text = "Done";
            btnDone.ID = "btnDone";
            btnDone.Click += ( s, e ) =>
            {
                hfEditMode.Value = "0";
                ShowView();
            };
            phGroups.Controls.Add( btnDone );
        }


        private void SaveGroupMember(RockContext rockContext, GroupMember groupMember, Panel pnlWell, List<AttributeCache> AttributeList, TextBox notes )
        {
            var changes = new List<string>();
            foreach ( var attribute in AttributeList )
            {

                if ( Person != null
                    && groupMember.Group.IsAuthorized( Authorization.EDIT, CurrentPerson)
                    && attribute.IsAuthorized( Authorization.EDIT, CurrentPerson ) )
                {
                    groupMember.Note = notes.Text;
                    rockContext.SaveChanges();

                    Control attributeControl = pnlWell.FindControl( string.Format( "attribute_field_{0}", attribute.Id ) );
                    if ( attributeControl != null )
                    {
                        string newValue = attribute.FieldType.Field.GetEditValue( attributeControl, attribute.QualifierValues );
                        groupMember.SetAttributeValue( attribute.Key, newValue );

                        groupMember.SaveAttributeValues();
                    }
                }
            }
            ShowEdit(groupMember.Id);
        }

        private void AddGroupMember( Group group, RockDropDownList ddlRole )
        {
            int roleId = ddlRole.SelectedValue.AsInteger();
            if ( Person != null
                && roleId != 0
                && group.IsAuthorized( Authorization.EDIT, CurrentPerson ) )
            {
                RockContext rockContext = new RockContext();
                GroupMemberService groupMemberService = new GroupMemberService( rockContext );
                if ( groupMemberService.GetByGroupIdAndPersonIdAndGroupRoleId( group.Id, Person.Id, roleId ) == null )
                {
                    GroupMember groupMember = new GroupMember()
                    {
                        GroupId = group.Id,
                        PersonId = Person.Id,
                        GroupRoleId = roleId
                    };
                    groupMemberService.Add( groupMember );
                    rockContext.SaveChanges();
                }
            }
            ShowEdit();
        }

        private void ConfirmDelete( int groupMemberId )
        {
            var script = string.Format( @"
                groupMemberDeleteId={0};
                Rock.dialogs.confirm('Are you sure you want to remove this person from this group? Any group member information will be lost.',deleteGroupMember)
                ", groupMemberId );
            ScriptManager.RegisterStartupScript( upnlAttributeValues, upnlAttributeValues.GetType(), "ConfirmDelete", script, true );
        }

        protected override void ShowSettings()
        {
            tbTitle.Text = GetAttributeValue( "Title" );
            tbIcon.Text = GetAttributeValue( "CSSIcon" );

            gpGroups.SetValues( GetGroups() );

            mdSettings.Show();
        }

        protected void mdSettings_SaveClick( object sender, EventArgs e )
        {
            SetAttributeValue( "Title", tbTitle.Text );
            SetAttributeValue( "CSSIcon", tbIcon.Text );
            SetAttributeValue( "Groups", string.Join( "|", gpGroups.SelectedValues ) );
            SaveAttributeValues();
            mdSettings.Hide();
        }

        private List<Group> GetGroups()
        {
            var groupsString = GetAttributeValue( "Groups" );
            if ( !string.IsNullOrWhiteSpace( groupsString ) )
            {
                var groupIdString = groupsString
                    .Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries )
                    .Select( s => s.AsInteger() ).ToList();
                RockContext rockContext = new RockContext();
                GroupService groupService = new GroupService( rockContext );
                return groupService.GetByIds( groupIdString ).ToList();
            }
            return new List<Group>();
        }

        protected void lbEdit_Click( object sender, EventArgs e )
        {
            hfEditMode.Value = "1";
            ShowEdit();
        }
    }
}