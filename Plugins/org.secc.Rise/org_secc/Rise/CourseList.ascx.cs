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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using org.secc.Rise;
using org.secc.Rise.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Rise
{
    [DisplayName( "Course List" )]
    [Category( "SECC > Rise" )]
    [Description( "Displays list of synced courses." )]

    [LinkedPage(
        "Detail Page",
        Description = "Course detail page.",
        Key = AttributeKeys.DETAIL_PAGE )]

    [LinkedPage(
        "Report Page",
        Description = "Page for course reports.",
        Key = AttributeKeys.REPORT_PAGE )]
    public partial class CourseList : RockBlock, ICustomGridColumns
    {
        private class AttributeKeys
        {
            public const string DETAIL_PAGE = "DetailPage";
            public const string REPORT_PAGE = "ReportPage";
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

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
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
            CourseService courseService = new CourseService( rockContext );

            var qry = courseService.Queryable( "Categories" ).OrderBy( c => c.Name );

            var sortProperty = gList.SortProperty;
            if ( gList.AllowSorting && sortProperty != null )
            {
                qry = qry.Sort( sortProperty );
            }
            else
            {
                qry = qry.OrderBy( c => c.Name );
            }

            gList.SetLinqDataSource( qry );
            gList.DataBind();
        }

        #endregion

        protected void gList_RowSelected( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            NavigateToLinkedPage( AttributeKeys.DETAIL_PAGE, new Dictionary<string, string> { { "CourseId", e.RowKeyValue.ToString() } } );
        }

        protected void btnSync_Click( object sender, EventArgs e )
        {
            System.Threading.Tasks.Task.Run( () =>
            {
                RiseClient riseClient = new RiseClient();
                var i = riseClient.SyncCourses();
            } );

            maSync.Show( string.Format( "Syncing courses has begun in the background." ), Rock.Web.UI.Controls.ModalAlertType.Information );
        }

        protected void gList_RowDataBound( object sender, System.Web.UI.WebControls.GridViewRowEventArgs e )
        {
            var field = ( Literal ) e.Row.FindControl( "lCategories" );

            if ( field != null )
            {
                field.Text = string.Join( ", ", ( ( Course ) e.Row.DataItem ).Categories.Select( c => c.Name ).OrderBy( c => c ).ToList() );
            }

        }

        protected void lbReport_Click( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( AttributeKeys.REPORT_PAGE, new Dictionary<string, string> { { "CourseId", e.RowKeyId.ToString() } } );
        }
    }
}