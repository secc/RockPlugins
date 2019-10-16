// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using org.secc.Finance.Utility;
using Rock.Web.Cache;

namespace RockWeb.Plugins.org_secc.Finance
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Contribution Statement Generator" )]
    [Category( "SECC > Finance" )]
    [Description( "Block for kicking off the process for generating contribution statements." )]
    [WorkflowTypeField( "Statement Generator Workflow", "The workflow to launch to generate statements.", true )]
    [DataViewField( "Default Review DataView", "The default DataView to use for the review process.", false )]
    public partial class ContributionStatementGenerator : Rock.Web.UI.RockBlock
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

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        protected override void CreateChildControls()
        {
            BootstrapButton button = new BootstrapButton();
            button.Text = "<i class=\"fa fa-arrow-alt-circle-right\"></i>";
            button.ToolTip = "Continue";
            button.CssClass = "btn btn-primary btn-sm pull-right";
            button.Click += btnContinue_Click;
            gdGivingUnits.Actions.Controls.Add( button );
            gdGivingUnits.Actions.ShowExcelExport = false;
            gdGivingUnits.Actions.ShowMergeTemplate = false;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if (!Page.IsPostBack)
            {
                tbGivingId.Text = GetBlockUserPreference( "GivingIdFilter" );
                tbGivingGroup.Text = GetBlockUserPreference( "GivingGroupFilter" );
                drpDates.LowerValue = GetBlockUserPreference( "LastGiftDateRangeFilterLower" ).AsDateTime();
                drpDates.UpperValue = GetBlockUserPreference( "LastGiftDateRangeFilterUpper" ).AsDateTime();
                if ( GetAttributeValue( "DefaultReviewDataView" ).IsNotNullOrWhiteSpace() )
                {
                    DataViewService dataViewService = new DataViewService( new RockContext() );
                    dvipReviewDataView.SetValue( dataViewService.Get( GetAttributeValue( "DefaultReviewDataView" ).AsGuid() ) );
                }
                DisplayResults();
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
            DisplayResults();
        }

        protected void gfFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            SetBlockUserPreference( "GivingIdFilter", tbGivingId.Text );
            SetBlockUserPreference( "GivingGroupFilter", tbGivingGroup.Text );
            SetBlockUserPreference( "LastGiftDateRangeFilterLower", drpDates.LowerValue.HasValue ? drpDates.LowerValue.Value.ToString() : "" );
            SetBlockUserPreference( "LastGiftDateRangeFilterUpper", drpDates.UpperValue.HasValue ? drpDates.UpperValue.Value.ToString() : "" );
            DisplayResults();
        }

        protected void gfFilter_ClearFilterClick( object sender, EventArgs e )
        {
            DeleteBlockUserPreference( "GivingIdFilter" );
            DeleteBlockUserPreference( "GivingGroupFilter" );
            DeleteBlockUserPreference( "LastGiftDateRangeFilterLower" );
            DeleteBlockUserPreference( "LastGiftDateRangeFilterUpper" );
            tbGivingId.Text = null;
            tbGivingGroup.Text = null;
            drpDates.LowerValue = drpDates.UpperValue = null;
            DisplayResults();
        }

        protected void gdGivingUnits_GridRebind( object sender, GridRebindEventArgs e )
        {
            DisplayResults();
        }

        protected void btnContinue_Click( object sender, EventArgs e )
        {
            lAlert.Text = "Total " + GetSelectedGivingUnitsCount() + " Giving Units Included";

            drpStatementDate.LowerValue = GetBlockUserPreference( "LastGiftDateRangeFilterLower" ).AsDateTime();
            drpStatementDate.UpperValue = GetBlockUserPreference( "LastGiftDateRangeFilterUpper" ).AsDateTime();

            pnlGivingUnitList.Visible = false;
            pnlSettings.Visible = true;
        }

        protected void btnGenerate_Click( object sender, EventArgs e )
        {
            var reviewDataView = GetAttributeValue( "DefaultReviewDataView" ).AsGuidOrNull();
            var statementGeneratorWorkflow = GetAttributeValue( "StatementGeneratorWorkflow" ).AsGuid();

            foreach ( GivingGroup givingGroup in GetSelectedGivingUnits() )
            {
                // Setup the attribute values
                var workflowAttributeValues = new Dictionary<string, string>();
                if ( reviewDataView.HasValue )
                {
                    workflowAttributeValues.Add( "ReviewDataView", reviewDataView.ToString() );
                }
                workflowAttributeValues.Add( "Version", tbVersion.Text );
                workflowAttributeValues.Add( "StatementDateRange", drpStatementDate.LowerValue.Value.ToString( "s" ) + "," + drpStatementDate.UpperValue.Value.ToString( "s" ) );
                workflowAttributeValues.Add( "GivingId", givingGroup.GivingId );

                var transaction = new Rock.Transactions.LaunchWorkflowTransaction( statementGeneratorWorkflow, "Contribution Statement for " + givingGroup.GivingGroupName );
                transaction.WorkflowAttributeValues = workflowAttributeValues;
                Rock.Transactions.RockQueue.TransactionQueue.Enqueue( transaction );
            }
            pnlProgressText.Controls.Add( new Label() { Text = GetSelectedGivingUnits().Count() + " contribution statement generation requests submitted." } );
            pnlProgressBar.Visible = true;
            pnlSettings.Visible = false;

        }

        protected void btnBack_Click( object sender, EventArgs e )
        {
            pnlGivingUnitList.Visible = true;
            pnlSettings.Visible = false;
        }
        #endregion

        #region Methods

        private void DisplayResults()
        {
            gdGivingUnits.DataSource = GetGivingGroups().ToList();
            gdGivingUnits.DataBind();
        }

        private IQueryable<GivingGroup> GetGivingGroups()
        {
            RockContext rockContext = new RockContext();
            rockContext.SqlLogging( true );

            int contributionTypeId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.TRANSACTION_TYPE_CONTRIBUTION ).Id;

            DateTime lower = DateTime.MinValue;
            DateTime upper = DateTime.MaxValue;

            // Filter by Last Gift DateRange
            if ( drpDates.LowerValue.HasValue || drpDates.UpperValue.HasValue )
            {
                lower = ( drpDates.LowerValue.HasValue ? drpDates.LowerValue.Value : DateTime.MinValue );
                upper = ( drpDates.UpperValue.HasValue ? drpDates.UpperValue.Value.AddDays( 1 ) : DateTime.MaxValue );
            }

            var givingGroups = rockContext.Database.SqlQuery<GivingGroup>( @"SELECT 
                    max([GivingGroups].[TransactionDateTime] ) AS [LastGift], 
                    [GivingGroups].[GivingId] AS [GivingId], 
                    [GivingGroups].[GivingGroupName] AS [GivingGroupName]
                        FROM( SELECT
                         [FT].[TransactionDateTime] AS [TransactionDateTime],
                         [FT].[TransactionTypeValueId] AS [TransactionTypeValueId],
                         [P].[GivingId] AS [GivingId],
                         CASE WHEN ([P].[GivingId] LIKE N'G%') THEN [G].[Name] 
							ELSE CASE WHEN( [P].[NickName] IS NULL ) THEN N'' 
							ELSE [P].[NickName] END + N' ' + CASE WHEN( [P].[LastName] IS NULL ) THEN N'' ELSE [P].[LastName] END END AS [GivingGroupName]
                        FROM [dbo].[FinancialTransaction] AS [FT]
                            LEFT OUTER JOIN[dbo].[PersonAlias] AS [PA] ON [FT].[AuthorizedPersonAliasId] = [PA].[Id]
                            LEFT OUTER JOIN[dbo].[Person] AS [P] ON [PA].[PersonId] = [P].[Id]
                            LEFT OUTER JOIN[dbo].[Group] AS [G] ON [P].[GivingGroupId] = [G].[Id]
                        WHERE [FT].[TransactionTypeValueId] = @p0
						AND (@p1 is NULL OR [FT].[TransactionDateTime] >= @p1)
						AND (@p2 is NULL OR [FT].[TransactionDateTime] < @p2)
                    )  AS [GivingGroups]
                    GROUP by GivingId, GivingGroupName", contributionTypeId, lower, upper).AsQueryable();

            // Filter by Giving ID
            if (tbGivingId.Text.IsNotNullOrWhiteSpace())
            {
                givingGroups = givingGroups.Where( gg => gg.GivingId.Contains( tbGivingId.Text ) );
            }

            // Filter by Giving Group Name
            if (tbGivingGroup.Text.IsNotNullOrWhiteSpace())
            {
                givingGroups = givingGroups.Where( gg => gg.GivingGroupName.Contains( tbGivingGroup.Text ) );
            }


            givingGroups = givingGroups.OrderByDescending( g => g.LastGift );

            return givingGroups;
        }

        private IQueryable<GivingGroup> GetSelectedGivingUnits()
        {
            if (gdGivingUnits.SelectedKeys.Count > 0)
            {
                var selectedKeys = gdGivingUnits.SelectedKeys.Select( k => k.ToString() ).ToList();
                return GetGivingGroups().ToList().Where( gg => selectedKeys.Contains( gg.GivingId ) ).AsQueryable();
            }
            return GetGivingGroups();
        }
        private int GetSelectedGivingUnitsCount()
        {
            if (gdGivingUnits.SelectedKeys.Count > 0)
            {
                return gdGivingUnits.SelectedKeys.Count;
            }
            return GetGivingGroups().Count();
        }
        #endregion


    }

    public class GivingGroup
    {
        public string GivingId { get; set; }

        public string GivingGroupName { get; set; }

        public DateTime? LastGift { get; set; }
    }
}