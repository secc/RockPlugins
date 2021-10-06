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
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using org.secc.Widgities.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Utility.EntityCoding;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Widgities
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Widgity Type List" )]
    [Category( "SECC > Widgities" )]
    [Description( "Block for displaying widgity types" )]

    [LinkedPage(
        "Details Page",
        Description = "Page to edit widgity type.",
        Key = AttributeKey.DetailsPage
        )]
    public partial class WidgityTypeList : RockBlock
    {

        protected static class AttributeKey
        {
            internal const string DetailsPage = "DetailsPage";
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
            gList.GridRebind += gList_GridRebind;
            gList.Actions.ShowAdd = true;
            gList.Actions.AddClick += Actions_AddClick;

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        private void Actions_AddClick( object sender, EventArgs e )
        {
            NavigateToLinkedPage( AttributeKey.DetailsPage );
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
                var rockContext = new RockContext();
                var entityTypes = new EntityTypeService( rockContext ).GetEntities()
                    .OrderBy( t => t.FriendlyName )
                    .ToList();

                ddlEntityType.EntityTypes = entityTypes;

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
            WidgityTypeService widgityTypeService = new WidgityTypeService( rockContext );

            // sample query to display a few people
            // Use AsNoTracking() since these records won't be modified, and therefore don't need to be tracked by the EF change tracker
            var qry = widgityTypeService.Queryable().OrderBy( wt => wt.Name );

            // set the datasource as a query. This allows the grid to only fetch the records that need to be shown based on the grid page and page size
            gList.SetLinqDataSource( qry );
            gList.DataBind();
        }

        #endregion

        protected void gList_RowSelected( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( AttributeKey.DetailsPage, new Dictionary<string, string> { { "WidgityTypeId", e.RowKeyId.ToString() } } );
        }

        protected void btnImport_Click( object sender, EventArgs e )
        {
            mdImport.Show();
        }

        protected void mdImport_SaveClick( object sender, EventArgs e )
        {
            if ( !fImport.BinaryFileId.HasValue )
            {
                return;
            }

            using ( var rockContext = new RockContext() )
            {
                var binaryFileService = new BinaryFileService( rockContext );
                var binaryFile = binaryFileService.Get( fImport.BinaryFileId ?? 0 );
                var categoryService = new CategoryService( rockContext );

                var container = Newtonsoft.Json.JsonConvert.DeserializeObject<ExportedEntitiesContainer>( binaryFile.ContentsToString() );
                List<string> messages;

                var decoder = new EntityDecoder( new RockContext() );
                //decoder.UserValues.Add( "EntityTypes",  ddlEntityType );

                var success = decoder.Import( container, false, out messages );

                nbImport.Text = string.Empty;
                foreach ( var msg in messages )
                {
                    nbImport.Text += string.Format( "{0}<br>", msg.EncodeHtml() );
                }

                nbImport.Visible = true;

                if ( success )
                {
                    fImport.BinaryFileId = null;
                }

                mdImport.Hide();
                BindGrid();
            }
        }
    }
}