
using System;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using org.secc.Jira.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Jira
{

    [DisplayName( "Jira Lava" )]
    [Category( "SECC > Jira" )]
    [Description( "Bock for editing a Jira topic and it's JQL." )]

    [CodeEditorField( "Lava Template",
        Description = "Lava template to display the content on the block.",
        EditorMode = CodeEditorMode.Lava,
        Order = 0,
        Key = AttributeKey.LavaTemplate )]

    [LavaCommandsField( "Enabled Lava Commands",
        IsRequired =false,
        Order = 1,
        Key = AttributeKey.EnabledCommands )]

    public partial class JiraLava : Rock.Web.UI.RockBlock
    {


        private static class AttributeKey
        {
            public const string LavaTemplate = "LavaTemplate";
            public const string EnabledCommands = "EnabledCommands";
        }


        #region Base Control Methods

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.AddConfigurationUpdateTrigger( upnlContent );
        }


        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                RockContext rockContext = new RockContext();
                JiraTopicService jiraTopicService = new JiraTopicService( rockContext );
                var topics = jiraTopicService
                    .Queryable( "JiraTickets" )
                    .AsNoTracking()
                    .OrderBy( t => t.Order )
                    .ToList();

                var mergeFields = LavaHelper.GetCommonMergeFields( this.RockPage, CurrentPerson );
                mergeFields.Add( "JiraTopics", topics );
                var output = GetAttributeValue( AttributeKey.LavaTemplate )
                    .ResolveMergeFields(
                        mergeFields,
                        CurrentPerson,
                        GetAttributeValue( AttributeKey.EnabledCommands )
                    );

                ltLava.Text = output;
            }
        }
        #endregion
    }
}