using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using org.secc.SystemsMonitor.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.SystemsMonitor
{
    [DisplayName( "Monitor Tests" )]
    [Category( "SECC > Systems Monitor" )]
    [Description( "Displays the available tests." )]

    [LinkedPage( "Details Page", required: false )]
    public partial class MonitorTests : RockBlock
    {
        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gList.GridRebind += gList_GridRebind;

            gList.Actions.ShowAdd = true;
            gList.Actions.AddClick += Actions_AddClick;

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        private void Actions_AddClick( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "DetailsPage" );
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
            SystemTestService monitorTestService = new SystemTestService( rockContext );
            var qry = monitorTestService.Queryable().OrderBy( t => t.Id );

            // set the datasource as a query. This allows the grid to only fetch the records that need to be shown based on the grid page and page size
            gList.SetLinqDataSource( qry );
            gList.DataBind();
        }

        #endregion

        protected void btnRun_Click( object sender, RowEventArgs e )
        {
            var testId = e.RowKeyId;
            RockContext rockContext = new RockContext();
            SystemTestService monitorTestService = new SystemTestService( rockContext );
            var monitorTest = monitorTestService.Get( testId );
            var response = monitorTest.Run();
            maNotification.Show( "Result: " + response.Passed.ToString(), ModalAlertType.Information );
        }

        protected void gList_RowSelected( object sender, RowEventArgs e )
        {
            var param = new Dictionary<string, string> { { "MonitorTestId", e.RowKeyValue.ToString() } };
            NavigateToLinkedPage( "DetailsPage", param );
        }
    }
}