using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using Rock.Web.UI;
using System.Data.Entity;
using System.Web.Security;

namespace RockWeb.Plugins.org_secc.Communication
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Manage Communication Lists" )]
    [Category( "SECC > Communication" )]
    [Description( "Block for users to manage their communication list subscriptions" )]
    [TextField( "Communication Group Type Attribute Key", "Group attribute which tells the user how they will be communicated with.", key: "AttributeKey", defaultValue: "Type" )]
    [TextField( "Communication Group Keyword Attribute Key", "Group attribute which tell the user how tehy will be communicated with.", key: "KeywordKey", defaultValue: "Keyword" )]
    public partial class ManageCommunicationLists : RockBlock
    {
        public Person Person { get; set; }
        public List<int> CommunicationGroupIds { get; set; }
        public List<int> CommunicationMembershipIds { get; set; }
        private List<Group> CommunicationGroups { get; set; }
        private List<GroupMember> CommunicationMembership { get; set; }
        private List<string> ExpandedPanels { get; set; }

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            Person = CurrentPerson;
            if ( Person == null )
            {
                var param = PageParameter( "p" );
                if ( param.IsNotNullOrWhiteSpace() && param.Length >= 12 )
                {
                    RockContext rockContext = new RockContext();
                    PersonService personService = new PersonService( rockContext );
                    var people = personService.Queryable().Where( p => p.Guid.ToString().EndsWith( param ) ).ToList();
                    if ( people.Count == 1 )
                    {
                        Person = people.FirstOrDefault();
                    }

                }
            }

            if ( Person == null )
            {
                var site = RockPage.Layout.Site;
                if ( site.LoginPageId.HasValue )
                {
                    site.RedirectToLoginPage( true );
                }
                else
                {
                    FormsAuthentication.RedirectToLoginPage();
                }
                Response.End();
            }

        }


        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            CommunicationMembershipIds = ( List<int> ) ViewState["CommunicationMembershipIds"];
            CommunicationGroupIds = ( List<int> ) ViewState["CommunicationGroupIds"];

            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );

            var tempCommunicationGroups = groupService.Queryable().Where( g => CommunicationGroupIds.Contains( g.Id ) ).ToList();
            CommunicationGroups = new List<Group>();
            foreach ( var id in CommunicationGroupIds )
            {
                CommunicationGroups.AddRange( tempCommunicationGroups.Where( g => g.Id == id ) );
            }

            CommunicationMembership = groupMemberService.Queryable().Where( m => CommunicationMembershipIds.Contains( m.Id ) ).ToList();

            if ( ViewState["KeywordGroupId"] != null && ViewState["KeywordGroupId"] is int )
            {
                var groupMember = new GroupMember { GroupId = ( int ) ViewState["KeywordGroupId"] };
                groupMember.LoadAttributes();
                groupMember.Attributes = groupMember.Attributes.Where( a => a.Value.IsGridColumn ).ToDictionary( a => a.Key, a => a.Value );
                Rock.Attribute.Helper.AddEditControls( groupMember, phGroupAttributes, false );
            }

            BindGroups( false );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( Person == null )
            {
                return;
            }

            if ( !Page.IsPostBack )
            {
                LoadKeywordGroup();
                LoadGroups();
                BindGroups( true );
            }
        }

        private void LoadKeywordGroup()
        {
            var keyword = PageParameter( "keyword" );
            if ( keyword.IsNullOrWhiteSpace() )
            {
                pnlKeyword.Visible = false;
                return;
            }

            var attributeKey = GetAttributeValue( "KeywordKey" );

            int communicationListGroupTypeId = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_COMMUNICATIONLIST.AsGuid() ).Id;
            var groupEntityTypeId = EntityTypeCache.Get( typeof( Group ) ).Id;
            RockContext rockContext = new RockContext();

            AttributeService attributeService = new AttributeService( rockContext );
            var attribute = attributeService.Queryable().AsNoTracking()
                .Where( a => a.EntityTypeId == groupEntityTypeId
                && a.EntityTypeQualifierValue == communicationListGroupTypeId.ToString()
                && a.Key == attributeKey )
                .FirstOrDefault();

            if ( attribute == null )
            {
                pnlKeyword.Visible = false;
                return;
            }

            AttributeValueService attributeValueService = new AttributeValueService( rockContext );
            var attributeValue = attributeValueService.Queryable().AsNoTracking()
                .Where( av => av.AttributeId == attribute.Id &&
                 ( av.Value == keyword
                 || av.Value.Contains( "|" + keyword )
                 || av.Value.Contains( keyword + "|" )
                 || av.Value.Contains( "|" + keyword + "|" ) )
                ).FirstOrDefault();

            if ( attributeValue == null )
            {
                pnlKeyword.Visible = false;
                return;
            }

            GroupService groupService = new GroupService( rockContext );
            var group = groupService.Get( attributeValue.EntityId ?? 0 );

            if ( group == null || !group.IsActive || group.IsArchived )
            {
                pnlKeyword.Visible = false;
                return;
            }
            group.LoadAttributes();

            var name = group.GetAttributeValue( "PublicName" );
            if ( name.IsNullOrWhiteSpace() )
            {
                name = group.Name;
            }
            ltGroupName.Text = name;

            var type = group.GetAttributeValue( GetAttributeValue( "AttributeKey" ) );
            if ( type.IsNullOrWhiteSpace() )
            {
                type = "Text Message,Email";
            }

            type = string.Join( ", ", type.SplitDelimitedValues( false ) );

            ltType.Text = type;
            ltDescription.Text = group.Description;

            GroupMemberService groupMemberService = new GroupMemberService( rockContext );

            GroupMember activeMember = null;

            var groupMembers = groupMemberService.GetByGroupIdAndPersonId( group.Id, Person.Id ).ToList();

            foreach ( var member in groupMembers )
            {
                if ( member.GroupMemberStatus == GroupMemberStatus.Active )
                {
                    activeMember = member;
                }
            }

            bool hasActiveMember = false;

            if ( activeMember != null )
            {
                hasActiveMember = true;
            }
            else
            {
                activeMember = new GroupMember
                {
                    GroupId = group.Id
                };
            }

            activeMember.LoadAttributes();
            activeMember.Attributes = activeMember.Attributes.Where( a => a.Value.IsGridColumn ).ToDictionary( a => a.Key, a => a.Value );


            if ( hasActiveMember )
            {
                if ( activeMember.Attributes.Any() )
                {
                    nbAlreadySubscribed.Visible = true;
                    nbAlreadySubscribed.Text = "You are already subscribed to this list, but you can update your subscription settings here.";
                    btnSubscribe.Text = "Update";
                }
                else
                {
                    nbAlreadySubscribed.Visible = true;
                    nbAlreadySubscribed.Text = "You are already subscribed to this list, but you can manage the rest of your subscriptions on this page. ";
                    btnSubscribe.Visible = false;
                }
            }

            if ( activeMember.Attributes.Any() )
            {
                ltAttributesHeader.Text = "<h3>Subscription Settings</h3>";
                Rock.Attribute.Helper.AddEditControls( activeMember, phGroupAttributes, true );
            }

            ViewState["KeywordGroupId"] = group.Id;
        }

        protected override object SaveViewState()
        {
            var obj = base.SaveViewState();
            ViewState["CommunicationMembershipIds"] = CommunicationMembership.Select( m => m.Id ).ToList();
            ViewState["CommunicationGroupIds"] = CommunicationGroups.Select( g => g.Id ).ToList();
            return obj;
        }

        private void LoadGroups()
        {
            int communicationListGroupTypeId = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_COMMUNICATIONLIST.AsGuid() ).Id;
            RockContext rockContext = new RockContext();

            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            GroupService groupService = new GroupService( rockContext );


            CommunicationMembership = groupMemberService.Queryable().AsNoTracking()
                .Where( gm => gm.Group.GroupTypeId == communicationListGroupTypeId )
                .Where( gm => gm.PersonId == Person.Id && !gm.IsArchived && gm.GroupMemberStatus == GroupMemberStatus.Active )
                .ToList();
            var membershipGroupIds = CommunicationMembership.Select( m => m.GroupId ).ToList();
            var tempGroups = groupService.Queryable().AsNoTracking()
                 .Where( g => g.IsPublic &&
                     ( g.GroupTypeId == communicationListGroupTypeId
                     || membershipGroupIds.Contains( g.Id ) && g.GroupTypeId == communicationListGroupTypeId ) )
                 .OrderBy( g => g.Name )
                 .ToList();

            CommunicationGroups = new List<Group>();

            //First add subscribed Ggroups
            foreach ( var group in tempGroups )
            {
                if ( membershipGroupIds.Contains( group.Id ) )
                {
                    CommunicationGroups.Add( group );
                }
            }

            //Then add the rest
            foreach ( var group in tempGroups )
            {
                if ( !membershipGroupIds.Contains( group.Id ) )
                {
                    CommunicationGroups.Add( group );
                }
            }
            SaveViewState();
        }



        private void BindGroups( bool insertValues )
        {
            GetExpandedPanels();
            phGroups.Controls.Clear();

            var membershipGroupIds = CommunicationMembership.Select( m => m.GroupId ).ToList();


            foreach ( var group in CommunicationGroups )
            {
                var member = CommunicationMembership.Where( gm => gm.GroupId == group.Id ).FirstOrDefault();

                if ( member == null )
                {
                    member = new GroupMember { GroupId = group.Id };
                }

                var pw = AddCommunicationPanel( group, member, insertValues );
                if ( membershipGroupIds.Contains( group.Id ) )
                {
                    pw.TitleIconCssClass = "fa fa-check";
                    pw.CssClass = "subscribed";
                }
                else
                {
                    pw.CssClass = "unsubscribed";
                }

                if ( ExpandedPanels.Contains( pw.ID ) )
                {
                    pw.Expanded = true;
                }
            }
        }

        private PanelWidget AddCommunicationPanel( Group group, GroupMember member, bool insertValues )
        {
            PanelWidget panelWidget = new PanelWidget
            {
                ID = "pnl+" + group.Id.ToString() + "panel",
                Title = group.Name
            };

            phGroups.Controls.Add( panelWidget );

            group.LoadAttributes();

            if ( group.GetAttributeValue( "PublicName" ).IsNotNullOrWhiteSpace() )
            {
                panelWidget.Title = group.GetAttributeValue( "PublicName" );
            }

            var type = group.GetAttributeValue( GetAttributeValue( "AttributeKey" ) );
            if ( type.IsNullOrWhiteSpace() )
            {
                type = "Text Message,Email";
            }

            type = string.Join( ", ", type.SplitDelimitedValues( false ) );

            var text = string.Format( "<small>{0}</small><p>{1}</p>",
                type,
                group.Description
                );


            Literal literal = new Literal
            {
                Text = text
            };

            panelWidget.Controls.Add( literal );

            var showAttributes = false;

            Panel pnlToggle = new Panel
            {
                ID = "pnlToggle_" + panelWidget.ID,
                CssClass = "btn-group btn-toggle"
            };
            panelWidget.Controls.Add( pnlToggle );

            if ( member.PersonId == Person.Id )
            {
                showAttributes = true;
                LinkButton off = new LinkButton
                {
                    ID = "btnOff" + panelWidget.ID,
                    CssClass = "btn btn-default btn-xs",
                    Text = "Unsubscribe",
                    CausesValidation = false
                };
                pnlToggle.Controls.Add( off );
                off.Click += ( s, e ) => { Unsubscribe( group.Id ); };

                HtmlGenericContainer on = new HtmlGenericContainer
                {
                    TagName = "div",
                    CssClass = "btn btn-success btn-xs",
                    InnerText = "Subscribed"
                };
                pnlToggle.Controls.Add( on );
            }
            else
            {
                HtmlGenericContainer off = new HtmlGenericContainer
                {
                    TagName = "div",
                    CssClass = "btn btn-danger btn-xs",
                    InnerText = "Unsubscribed"
                };
                pnlToggle.Controls.Add( off );

                LinkButton on = new LinkButton
                {
                    ID = "btnOn" + panelWidget.ID,
                    CssClass = "btn btn-default btn-xs",
                    Text = "Subscribe",
                    CausesValidation = false
                };
                pnlToggle.Controls.Add( on );
                on.Click += ( s, e ) => { Subscribe( group.Id ); };
            }

            member.LoadAttributes();

            member.Attributes = member.Attributes.Where( a => a.Value.IsGridColumn ).ToDictionary( a => a.Key, a => a.Value );

            if ( member.Attributes.Any() )
            {
                Literal attributeTitle = new Literal
                {
                    ID = "ltAttTitle_" + panelWidget.ID,
                    Text = "<h3>Subscription Settings</h3>",
                    Visible = showAttributes
                };

                panelWidget.Controls.Add( attributeTitle );

                var attributePlaceholder = new PlaceHolder
                {
                    ID = "phAtt_" + panelWidget.ID,
                    Visible = showAttributes
                };
                panelWidget.Controls.Add( attributePlaceholder );

                Rock.Attribute.Helper.AddEditControls( member, attributePlaceholder, insertValues );

                BootstrapButton btnSave = new BootstrapButton
                {
                    ID = "btnSave_" + panelWidget.ID,
                    Text = "Update Settings",
                    CssClass = "btn btn-primary btn-xs",
                    Visible = showAttributes
                };
                panelWidget.Controls.Add( btnSave );
                btnSave.Click += ( s, e ) =>
                {
                    UpdateSettings( member, attributePlaceholder );
                    btnSave.Text = "Saved";
                };

            }
            return panelWidget;
        }

        private void UpdateSettings( GroupMember member, PlaceHolder attributePlaceholder )
        {
            Rock.Attribute.Helper.GetEditValues( attributePlaceholder, member );
            member.SaveAttributeValues();
        }

        private void Subscribe( int groupId )
        {
            RockContext rockContext = new RockContext();
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            var groupMembers = groupMemberService.GetByGroupIdAndPersonId( groupId, Person.Id ).ToList();
            if ( groupMembers.Any() )
            {
                foreach ( var member in groupMembers )
                {
                    member.GroupMemberStatus = GroupMemberStatus.Active;
                    CommunicationMembership.Add( member );
                }
            }
            else
            {
                GroupService groupService = new GroupService( rockContext );
                var group = groupService.Get( groupId );
                var defaultGroupRoleId = group.GroupType.DefaultGroupRoleId;
                var newMember = new GroupMember
                {
                    PersonId = Person.Id,
                    GroupId = groupId,
                    GroupRoleId = defaultGroupRoleId.Value,
                    GroupMemberStatus = GroupMemberStatus.Active
                };
                groupMemberService.Add( newMember );

                CommunicationMembership.Add( newMember );
            }

            rockContext.SaveChanges();
            SaveViewState();
            BindGroups( true );
        }


        private void Unsubscribe( int groupId )
        {
            RockContext rockContext = new RockContext();
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            var groupMembers = groupMemberService.GetByGroupIdAndPersonId( groupId, Person.Id ).ToList();

            foreach ( var member in groupMembers )
            {
                member.GroupMemberStatus = GroupMemberStatus.Inactive;
                CommunicationMembership.RemoveAll( m => m.GroupId == groupId );
            }

            rockContext.SaveChanges();
            SaveViewState();
            BindGroups( true );
        }



        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            BindGroups( true );
        }


        private void GetExpandedPanels()
        {
            ExpandedPanels = new List<string>();
            if ( phGroups != null )
            {

                var panels = phGroups.Controls.OfType<PanelWidget>().ToList();
                foreach ( var panel in panels )
                {
                    if ( panel.Expanded )
                    {
                        HiddenField hiddenField = panel.Controls[4] as HiddenField;
                        ExpandedPanels.Add( panel.ID );

                    }
                }
            }
        }

        #endregion

        protected void btnSubscribe_Click( object sender, EventArgs e )
        {
            var groupId = ( int ) ViewState["KeywordGroupId"];

            RockContext rockContext = new RockContext();
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            GroupService groupService = new GroupService( rockContext );

            var group = groupService.Get( groupId );
            var defaultGroupRoleId = group.GroupType.DefaultGroupRoleId;


            var groupMembers = groupMemberService.GetByGroupIdAndPersonId( groupId, Person.Id ).ToList();

            if ( !groupMembers.Any() )
            {
                var groupMember = new GroupMember
                {
                    GroupId = groupId,
                    PersonId = Person.Id,
                    GroupRoleId = defaultGroupRoleId.Value
                };
                groupMemberService.Add( groupMember );
                groupMembers.Add( groupMember );
                rockContext.SaveChanges();
            }

            foreach ( var member in groupMembers )
            {
                member.GroupMemberStatus = GroupMemberStatus.Active;
                rockContext.SaveChanges();

                member.LoadAttributes();
                Rock.Attribute.Helper.GetEditValues( phGroupAttributes, member );
                member.SaveAttributeValues();

                CommunicationMembership.Add( member );
            }

            SaveViewState();
            pnlKeyword.Visible = false;
            nbSuccess.Visible = true;
            nbSuccess.Text = "You have been successfully subscribed. We look forward to communicating with you soon!";

            BindGroups( true );
        }
    }
}