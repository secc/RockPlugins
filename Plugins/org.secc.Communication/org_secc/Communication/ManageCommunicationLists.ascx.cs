using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

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
    [DefinedValueField( "Confirmation SMS From", AllowMultiple = false, DefinedTypeGuid = "611bde1f-7405-4d16-8626-ccfedb0e62be", Description = "SMS Phone number for confirmation message", AllowAddingNewValues = false, Key = "FromSMSNumber" )]

    public partial class ManageCommunicationLists : RockBlock
    {
        private PhoneNumber mobilePhone = null;

        public Person Person { get; set; }
        public List<int> CommunicationGroupIds { get; set; }
        public List<int> CommunicationMembershipIds { get; set; }
        private List<Group> CommunicationGroups { get; set; }
        private List<GroupMember> CommunicationMembership { get; set; }
        private List<string> ExpandedPanels { get; set; }

        private PhoneNumber MobilePhone
        {
            get
            {
                if ( mobilePhone == null )
                {
                    mobilePhone = Person.GetPhoneNumber( "407e7e45-7b2e-4fcd-9605-ecb1339f2453".AsGuid() );
                }
                return mobilePhone;
            }
        }

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


            nbNotice.Visible = false;
            nbNotice.Heading = string.Empty;
            nbNotice.Text = string.Empty;
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
                var myAccountLink = ResolveRockUrl( $"~/MyAccount/Edit/{Person.Guid}" );
                if ( MobilePhone == null && type.Equals( "text message", StringComparison.InvariantCultureIgnoreCase ) )
                {

                    NotificationBox phoneAlert = new NotificationBox
                    {
                        ID = $"nbNoMobile{panelWidget.ID}",
                        NotificationBoxType = NotificationBoxType.Validation,
                        Heading = "Mobile Phone Required",
                        Text = $"You are subscribed to this list, but we do not have a mobile phone number on record. Please update your <a href='{myAccountLink}'>profile</a>."
                    };
                    pnlToggle.Controls.Add( phoneAlert );
                }
                else if ( ( Person.Email.IsNullOrWhiteSpace() || Person.EmailPreference != EmailPreference.EmailAllowed ) && type.Equals( "Email", StringComparison.InvariantCultureIgnoreCase ) )
                {
                    NotificationBox emailAlert = new NotificationBox
                    {
                        ID = $"nbEmailError{panelWidget.ID}",
                        NotificationBoxType = NotificationBoxType.Validation,
                        Heading = "Email Required",
                        Text = $"You are subscribed to this list, but you email peferences are not set to allow emails. "
                            + $"Please verify your email address and email preferences from your <a href='{myAccountLink}'>profile</a>."
                    };
                    pnlToggle.Controls.Add( emailAlert );

                }

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
                if ( type.Equals( "Email", StringComparison.InvariantCultureIgnoreCase ) )
                {
                    pnlToggle.Controls.Add( CreateEmailBox( panelWidget.ID ) );
                }
                else if ( type.Equals( "Text Message", StringComparison.InvariantCultureIgnoreCase ) )
                {
                    pnlToggle.Controls.Add( CreatePhoneBox( panelWidget.ID ) );
                }
                else
                {
                    switch ( Person.CommunicationPreference )
                    {
                        case CommunicationType.Email:
                            pnlToggle.Controls.Add( CreateEmailBox( panelWidget.ID ) );
                            break;
                        case CommunicationType.SMS:
                            pnlToggle.Controls.Add( CreatePhoneBox( panelWidget.ID ) );
                            break;

                        default:
                            break;
                    }
                }

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
                on.Click += ( s, e ) => { Subscribe( group.Id, panelWidget.ID ); };
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

        private void ShowEmailNotice()
        {
            ShowNotice( "Email Address Reqired",
                "Email address is required when registering for this communication list.",
                NotificationBoxType.Validation );
        }

        private void ShowMobilePhoneNotice()
        {
            ShowNotice( "Mobile Phone is required.",
                "Mobile phone number is required when registering for this communication list.",
                NotificationBoxType.Validation );

        }

        private void ShowNotice( string heading, string message, NotificationBoxType boxType = NotificationBoxType.Info )
        {
            nbNotice.Heading = heading;
            nbNotice.Text = message;
            nbNotice.NotificationBoxType = boxType;
            nbNotice.Visible = true;
        }

        private void Subscribe( int groupId, string panelId = null )
        {
            var communicationGroup = CommunicationGroups.Where( g => g.Id == groupId )
                .FirstOrDefault();


            var communicationType = communicationGroup.GetAttributeValue( "Type" );
            PanelWidget pnlWidget = phGroups.FindControl( panelId ) as PanelWidget;
            GroupMemberStatus status = GroupMemberStatus.Active;

            if ( pnlWidget != null && communicationType.Equals( "Text Message", StringComparison.InvariantCultureIgnoreCase ) )
            {
                var phoneNumberBox = pnlWidget.FindControl( $"tbPhone{panelId}" ) as PhoneNumberBox;
                var phoneNumber = string.Empty;
                if ( phoneNumberBox != null )
                {
                    phoneNumber = PhoneNumber.CleanNumber( phoneNumberBox.Text );
                }

                if ( phoneNumber.IsNullOrWhiteSpace() )
                {
                    ShowMobilePhoneNotice();
                    return;
                }

                if ( mobilePhone == null || mobilePhone.Number != phoneNumber )
                {
                    UpdateMobilePhone( phoneNumber );
                }
                status = GroupMemberStatus.Active;
                //SendConfirmationMessage( groupId );
            }
            else if ( pnlWidget != null && communicationType.Equals( "Email", StringComparison.InvariantCultureIgnoreCase ) )
            {
                var emailBox = pnlWidget.FindControl( $"tbEmail{panelId}" ) as EmailBox;
                var email = string.Empty;
                if ( emailBox != null )
                {
                    email = emailBox.Text;
                }

                if ( email.IsNullOrWhiteSpace() )
                {
                    ShowEmailNotice();
                    return;
                }

                if ( !Person.Email.Equals( email, StringComparison.InvariantCultureIgnoreCase ) )
                {
                    UpdateEmail( email );
                }
            }
            else
            {
                if ( Person.CommunicationPreference == CommunicationType.SMS )
                {
                    PhoneNumberBox phoneBox = pnlWidget.FindControl( $"tbPhone{panelId}" ) as PhoneNumberBox;
                    var phoneNumber = String.Empty;

                    if ( phoneBox != null )
                    {
                        phoneNumber = PhoneNumber.CleanNumber( phoneBox.Text );
                    }

                    if ( phoneNumber.IsNullOrWhiteSpace() )
                    {
                        ShowMobilePhoneNotice();
                        return;
                    }

                    if ( MobilePhone == null || !mobilePhone.Equals( phoneNumber ) )
                    {
                        UpdateMobilePhone( phoneNumber );
                    }

                }
                else
                {
                    EmailBox emailBox = pnlWidget.FindControl( $"tbEmail{panelId}" ) as EmailBox;
                    var email = String.Empty;

                    if ( emailBox != null )
                    {
                        email = emailBox.Text.Trim();
                    }

                    if ( email.IsNullOrWhiteSpace() )
                    {
                        ShowEmailNotice();
                        return;
                    }

                    if ( !email.Equals( Person.Email ) )
                    {
                        UpdateEmail( email );
                    }
                }
            }



            RockContext rockContext = new RockContext();
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            var groupMembers = groupMemberService.GetByGroupIdAndPersonId( groupId, Person.Id ).ToList();
            if ( groupMembers.Any() )
            {
                foreach ( var member in groupMembers )
                {
                    member.GroupMemberStatus = status;
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
                    GroupMemberStatus = status
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

        private void UpdateEmail( string emailAddress )
        {
            using ( var context = new RockContext() )
            {
                var personService = new PersonService( context );
                var person = personService.Get( Person.Guid );
                person.Email = emailAddress;
                person.EmailPreference = EmailPreference.EmailAllowed;
                context.SaveChanges();

            }
        }

        private void UpdateMobilePhone( string phoneNumber )
        {
            using ( var context = new RockContext() )
            {
                var mobilePhoneDefinedValue = DefinedValueCache.Get( "407e7e45-7b2e-4fcd-9605-ecb1339f2453" );
                var personService = new PersonService( context );
                var person = personService.Get( Person.Guid );

                person.UpdatePhoneNumber( mobilePhoneDefinedValue.Id, "1", phoneNumber, true, null, context );
                context.SaveChanges();

                Person = personService.Get( Person.Guid );
                mobilePhone = null;
            }
        }

        private EmailBox CreateEmailBox( string panelWidgetId )
        {
            return new EmailBox
            {
                ID = $"tbEmail{panelWidgetId}",
                Label = "Verify Email Address",
                Help = "Your meail address will be updated to the address entered here and your "
                    + "preferences will be set to Email Allowed.",
                Text = Person != null ? Person.Email : String.Empty
            };
        }

        private PhoneNumberBox CreatePhoneBox( string panelWidgetId )
        {
            return new PhoneNumberBox
            {
                ID = $"tbPhone{panelWidgetId}",
                Label = "Verify Mobile Number",
                Help = "Your mobile phone number will be updated to the number entered here "
                    + " with text messaging enabled.",
                Text = MobilePhone != null ? MobilePhone.NumberFormatted : String.Empty
            };
        }

        private void SendConfirmationMessage( int groupId )
        {
            var group = CommunicationGroups.FirstOrDefault( g => g.Id == groupId );
            var groupResponseMessage = group.GetAttributeValue( "SMSSubscribeResponse" );

            var smsBody = $"{groupResponseMessage} Reply Y to confirm receiving msgs. Reply HELP for help, STOP to quit. Msg&data rates may apply.";
            var fromNumberGuid = GetAttributeValue( "FromSMSNumber" ).AsGuidOrNull();

            if ( !fromNumberGuid.HasValue )
            {
                return;
            }

            var phoneDefinedValue = DefinedValueCache.Get( fromNumberGuid.Value );
            var smsMessage = new RockSMSMessage
            {
                CreateCommunicationRecord = false,
                FromNumber = phoneDefinedValue,
                Message = smsBody,

            };
            smsMessage.SetRecipients( new List<int> { Person.Id } );


            smsMessage.Send();


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
            nbNotice.Visible = false;
            nbSuccess.Visible = true;
            nbSuccess.Text = "You have been successfully subscribed. We look forward to communicating with you soon!";

            BindGroups( true );
        }
    }
}