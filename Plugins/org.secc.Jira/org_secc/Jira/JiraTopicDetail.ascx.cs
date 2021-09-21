
using System;
using System.ComponentModel;
using System.Web.UI;
using org.secc.Jira.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;

namespace RockWeb.Plugins.org_secc.Jira
{

    [DisplayName( "Jira Topic Detail" )]
    [Category( "SECC > Jira" )]
    [Description( "Bock for editing a Jira topic and it's JQL." )]

    public partial class JiraTopicDetail : Rock.Web.UI.RockBlock
    {

        #region PageParameterKeys

        private static class PageParameterKey
        {
            public const string JiraTopicId = "JiraTopicId";
        }

        #endregion PageParameterKeys


        #region Base Control Methods

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // This event gets fired after block settings are updated. It's nice to repaint the screen if these settings would alter it.
            this.AddConfigurationUpdateTrigger( upnlContent );
        }


        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                var topic = GetJiraTopic();
                tbName.Text = topic.Name;
                tbJQL.Text = topic.JQL;
            }
        }

        #endregion

        #region Events


        #endregion

        #region Methods

        private JiraTopic GetJiraTopic()
        {
            return GetJiraTopic( new JiraTopicService( new RockContext() ) );
        }

        private JiraTopic GetJiraTopic( JiraTopicService jiraTopicService )
        {
            var id = PageParameter( PageParameterKey.JiraTopicId ).AsInteger();
            var topic = jiraTopicService.Get( id );

            if ( topic == null )
            {
                topic = new JiraTopic
                {
                    Id = 0
                };
            }

            return topic;
        }

        #endregion

        protected void btnSave_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            JiraTopicService jiraTopicService = new JiraTopicService( rockContext );
            var topic = GetJiraTopic( jiraTopicService );
            if ( topic.Id == 0 )
            {
                jiraTopicService.Add( topic );
            }
            topic.Name = tbName.Text;
            topic.JQL = tbJQL.Text;

            rockContext.SaveChanges();

            NavigateToParentPage();
        }

        protected void btnCancel_Click( object sender, EventArgs e )
        {
            NavigateToParentPage();
        }
    }
}