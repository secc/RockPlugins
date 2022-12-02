using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.Security;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Communication
{

    [DisplayName( "Communication List Subscriptions" )]
    [Category( "SECC > Communication" )]
    [Description( "Block that allows a person to manage the communication list that they are subscribed to." )]

    #region Block Attributes
    [TextField(
        "Block Title",
        Description = "Sets the text for the block header. Default is blank.",
        DefaultValue = "",
        IsRequired = false,
        Key = AttributeKey.BlockTitle,
        Order = 0 )]

    [CodeEditorField( "Introduction Message",
        Description = "Introduction Message Lava text.",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        IsRequired = false,
        DefaultValue = "",
        Key = AttributeKey.IntroductionText,
        Order = 1 )]

    [LavaCommandsField( "Enabled Lava Commands",
        Description = "Enabled Lava Commands",
        IsRequired = false,
        Key = AttributeKey.EnabledLavaCommands,
        Order = 2 )]

    [TextField( "Communication Group Type Attribute Key",
        Description = "Group attribute which tells the user how they will be communicated with.",
        Key = AttributeKey.GroupTypeAttributeKey,
        DefaultValue = "Type" )]

    [TextField( "Communication Group Keyword Attribute Key",
        Description = "Group attribute which tell the user how tehy will be communicated with.",
        Key = AttributeKey.GroupAttributeKey,
        DefaultValue = "Keyword" )]

    #endregion
    public partial class CommunicationListSubscriptions : RockBlock
    {
        #region Attribute Keys
        private static class AttributeKey
        {
            public const string BlockTitle = "BlockTitle";
            public const string IntroductionText = "IntroductionText";
            public const string EnabledLavaCommands = "EnabledLavaCommands";
            public const string GroupTypeAttributeKey = "AttributeKey";
            public const string GroupAttributeKey = "KeywordKey";

        }
        #endregion

        public Person Person { get; set; }

        #region Base Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upCommunicationLists );

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



        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !IsPostBack )
            {
                LoadDynamicHeader();
                LoadCommunicationLists();
            }

        }

        #endregion

        #region Events

        private void Block_BlockUpdated( object sender, EventArgs e )
        {
            LoadDynamicHeader();
            LoadCommunicationLists();
        }

        #endregion

        #region Methods



        private List<int> GetSubscribedListIds()
        {

            var context = new RockContext();

            var communicationListGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_COMMUNICATIONLIST.AsGuid() );

            var communicationGTIDAsString = communicationListGroupType.Id.ToString();
            
            return new GroupMemberService( context ).Queryable().AsNoTracking()
                .Where(m => m.PersonId == Person.Id)
                .Where( m => m.Group.IsActive )
                .Where( m => m.Group.GroupTypeId == communicationListGroupType.Id )
                .Where( m => m.Group.IsArchived == false )
                .Where(m => m.Group.IsPublic)
                .Where( m => m.GroupMemberStatus != GroupMemberStatus.Inactive && m.IsArchived == false )
                .Select( m => m.GroupId )
                .ToList();

        }

        private  List<CommunicationListSummary> GetCommunicationLists()
        {
            var context = new RockContext();
            var communicationListGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_COMMUNICATIONLIST );
            var groupEntityType = EntityTypeCache.Get( typeof( Rock.Model.Group ) );


            var groupTypeIdAsString = communicationListGroupType.Id.ToString();

            var attributeValueService = new AttributeValueService( context );
            var baseAttributeValueQry = attributeValueService.Queryable().AsNoTracking()
                .Where( v => v.Attribute.EntityTypeId == groupEntityType.Id )
                .Where( v => v.Attribute.EntityTypeQualifierColumn == "GroupTypeId" && v.Attribute.EntityTypeQualifierValue == groupTypeIdAsString );


            var typeValueQry = attributeValueService.Queryable().AsNoTracking()
                .Where( v => v.Attribute.Key == "Type" );

            var publicNameValueQry = attributeValueService.Queryable().AsNoTracking()
                .Where( v => v.Attribute.Key == "PublicName" );

            var results = new GroupService( context ).Queryable().AsNoTracking()
                .Where( g => g.GroupTypeId == communicationListGroupType.Id )
                .Where( g => g.IsPublic )
                .Where( g => !g.IsArchived )
                .Join(typeValueQry, g => g.Id, t => t.EntityId,
                    (g, t) => new { Group = g,  ListType = t.Value })
                .Join(publicNameValueQry, g => g.Group.Id, n => n.EntityId,
                    (g, n)  => new {Group = g.Group, ListType = g.ListType, PublicName = n.Value})
                .Select( g => new CommunicationListSummary
                {
                    CommunicationList = g.Group,
                    ListType = g.ListType,
                    PublicName = g.PublicName
                } )
                .ToList();

            return results;

        }

        /// <summary>
        /// Loads Block Title and introduction text.  
        /// </summary>
        private void LoadDynamicHeader()
        {
            lBlockTitle.Text = GetAttributeValue( AttributeKey.BlockTitle );

            var introductionText = GetAttributeValue( AttributeKey.IntroductionText );

            if ( introductionText.IsNullOrWhiteSpace() )
            {
                pnlIntroduction.Visible = false;
                return;
            }

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            lIntroduction.Text = introductionText.ResolveMergeFields( mergeFields, GetAttributeValue( AttributeKey.EnabledLavaCommands ) );

        }

        /// <summary>
        /// Load list of avaiable communication lists
        /// </summary>
        private void LoadCommunicationLists()
        {
            var subscribedListIds = GetSubscribedListIds();
            var communicationLists = GetCommunicationLists();

  
            foreach ( var list in communicationLists )
            {
                list.IsSubscribed = subscribedListIds.Contains( list.CommunicationList.Id );
                list.IsAvailable = list.CommunicationList.IsAuthorized( Rock.Security.Authorization.VIEW, CurrentPerson );

            }

            rptLists.DataSource = communicationLists
                .Where( l => l.IsAvailable )
                .OrderByDescending( l => l.IsSubscribed )
                .ThenBy( l => l.PublicName )
                .ToList();
            rptLists.DataBind();
            
        }



        #endregion
        private class CommunicationListSummary
        {
            public Group CommunicationList { get; set; }
            public bool IsSubscribed { get; set; }
            public string ListType { get; set; }
            public bool IsAvailable { get; set; } = true;
            public string PublicName { get; set; }
        }
    }
}