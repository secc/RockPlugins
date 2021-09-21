using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Data.Entity;
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
using org.secc.Jira.Model;
using System.Threading.Tasks;

namespace RockWeb.Plugins.org_secc.Jira
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Jira Topic List" )]
    [Category( "SECC > Jira" )]
    [Description( "Template block for developers to use to start a new list block." )]

    [LinkedPage( "Detail Page",
        Description = "Page for editing and updating the Jira Topic",
        Order = 0,
        Key = AttributeKeys.DetailPage )]

    public partial class JiraTopicList : RockBlock
    {
        internal static class AttributeKeys
        {
            internal const string DetailPage = "DetailPage";
        }


        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gList.Actions.ShowAdd = true;
            gList.Actions.AddClick += Actions_AddClick;


            gList.GridRebind += gList_GridRebind;

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        private void Actions_AddClick( object sender, EventArgs e )
        {
            NavigateToLinkedPage( AttributeKeys.DetailPage );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                BindGrid();
            }
        }

        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {

        }

        /// <summary>
        /// Handles the GridRebind event of the gPledges control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gList_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            RockContext rockContext = new RockContext();
            JiraTopicService jiraTopicService = new JiraTopicService( rockContext );

            var qry = jiraTopicService.Queryable().OrderBy( j => j.Order );

            gList.SetLinqDataSource( qry );
            gList.DataBind();
        }

        #endregion

        protected void gList_RowSelected( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( AttributeKeys.DetailPage, new Dictionary<string, string> { { "JiraTopicId", e.RowKeyValue.ToString() } } );
        }

        protected void btnRefresh_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            JiraTopicService jiraTopicService = new JiraTopicService( rockContext );
            var topics = jiraTopicService.Queryable().ToList();
            foreach ( var topic in topics )
            {
                Task.Run( () => topic.UpdateTickets() );
            }
        }

        protected void Delete_Click( object sender, RowEventArgs e )
        {
            RockContext rockContext = new RockContext();
            JiraTopicService jiraTopicService = new JiraTopicService( rockContext );
            var topic = jiraTopicService.Get( e.RowKeyId );
            jiraTopicService.Delete( topic );
            rockContext.SaveChanges();
            BindGrid();
        }
    }
}