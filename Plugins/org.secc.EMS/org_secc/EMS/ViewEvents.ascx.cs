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
// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
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
using System.Data;
using System.Linq;
using System.Web.UI;

using Rock;
using Rock.Model;
using Rock.Web.UI;
using org.secc.EMS;
using Rock.Attribute;

namespace RockWeb.Plugins.org_secc.EMS
{
    /// <summary>
    /// Block for displaying the history of changes to a particular user.
    /// </summary>
    [DisplayName( "View Events" )]
    [Category( "SECC > EMS" )]
    [Description( "Block for displaying EMS events." )]
    [TextField( "Building IDs", "A comma-separated list of EMS building ids to show on the report. Leave blank to show all buildings.", false, "", "", 0 )]
    public partial class ViewEvents : RockBlock
    {

        #region Fields

        #endregion

        #region Base Control Methods


        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += ViewEvents_BlockUpdated;

            gfSettings.ApplyFilterClick += gfSettings_ApplyFilterClick;
            gfSettings.DisplayFilterValue += gfSettings_DisplayFilterValue;
            gScroll.GridRebind += gScroll_GridRebind;
        }

        /// <summary>
        /// Handles the BlockUpdated event of the ViewEvents control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        public void ViewEvents_BlockUpdated( object sender, EventArgs e )
        {
            BindGrid();
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
		        gScroll.PageSize = 5000;
                BindFilter();
                BindGrid();
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the ApplyFilterClick event of the gfSettings control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void gfSettings_ApplyFilterClick( object sender, EventArgs e )
        {
            dpDate.SelectedDate = dpDate.SelectedDate ?? DateTime.Today;
            gfSettings.SaveUserPreference( "Events For", dpDate.SelectedDate.ToString() );
            gfSettings.SaveUserPreference( "Show All", cbShowAll.Checked.ToTrueFalse() );
            BindGrid();
        }

        /// <summary>
        /// Handles the DisplayFilterValue event of the gfSettings control.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void gfSettings_DisplayFilterValue( object sender, Rock.Web.UI.Controls.GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Show All":
                    {
                        break;
                    }
                case "Events For":
                    {
                        e.Value = dpDate.SelectedDate.ToString().Replace( " 12:00:00 AM", "" );
                        break;
                    }
                default:
                    {
                        e.Value = string.Empty;
                        break;
                    }
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the gAppt control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        void gScroll_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void BindFilter()
        {
            string showAllValue = gfSettings.GetUserPreference( "Show All" );
            if ( !string.IsNullOrWhiteSpace( showAllValue ) )
            {
                cbShowAll.Checked = showAllValue.AsBoolean();
            }

            dpDate.SelectedDate = dpDate.SelectedDate ?? DateTime.Today;
            gfSettings.SaveUserPreference( "Events For", dpDate.SelectedDate.ToString() );
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            string errorMessage = "";
            DateTime dtDate = dpDate.SelectedDate ?? DateTime.Today;
            List<webEvent> webEvents = null;
            List<int> buildings = null;
            string BuildingIDsSetting = GetAttributeValue( "BuildingIDs" );
            if ( !string.IsNullOrEmpty( BuildingIDsSetting.Trim() ) )
                buildings = BuildingIDsSetting.Trim().Split( ',' ).ToList<string>().Select( b => int.Parse( b.Trim() ) ).ToList<int>();
            try
            {
                webEvents = ( new API() ).GetWebEvents( dtDate, dtDate.AddDays( 1 ).AddSeconds( -1 ), buildings, false, !cbShowAll.Checked, out errorMessage );
            }
            catch ( System.Exception e )
            {
                if ( errorMessage == string.Empty )
                    errorMessage = e.Message;
            }

            if ( errorMessage == string.Empty && webEvents != null )
            {
                if ( webEvents.Count != 0 )
                    gScroll.DataSource = webEvents;
                else
                {
                    gScroll.DataSource = null;
                }
            }
            else
            {
                if ( errorMessage != string.Empty )
                    throw new Exception( "Error occurred retrieving events from EMS", new System.Exception( errorMessage ) );
                    gScroll.DataSource = null;
            }
            gScroll.DataBind();
        }

        #endregion
    }
}