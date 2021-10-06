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

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using Rock.Web.UI;
using org.secc.Equip.Model;
using org.secc.Equip;

namespace RockWeb.Plugins.org.secc.Equip
{
    [DisplayName( "Course Page List" )]
    [Category( "SECC > Equip" )]
    [Description( "Block for displaying all of the available course pages for a chapter." )]

    public partial class CoursePageList : RockBlock
    {

        protected static class PageParameterKey
        {
            internal const string ChapterId = "ChapterId";
            internal const string CoursePageComponentId = "CoursePageComponentId";
            internal const string CoursePageId = "CoursePageId";
            internal const string BlockId = "BlockId";
        }

        #region Base Control Methods

        //
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gList.ShowActionRow = false;
            gList.GridRebind += gList_GridRebind;
            gList.GridReorder += GList_GridReorder;

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        private void GList_GridReorder( object sender, GridReorderEventArgs e )
        {
            var rockContext = new RockContext();
            CoursePageService coursePageService = new CoursePageService( rockContext );
            var chapterId = PageParameter( PageParameterKey.ChapterId ).AsInteger();
            var items = coursePageService.Queryable().Where( p => p.ChapterId == chapterId ).OrderBy( i => i.Order ).ToList();
            coursePageService.Reorder( items, e.OldIndex, e.NewIndex );
            rockContext.SaveChanges();
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
                var coursepageId = PageParameter( PageParameterKey.CoursePageId ).AsIntegerOrNull();
                var chapterId = PageParameter( PageParameterKey.ChapterId ).AsIntegerOrNull();

                //Don't show if there isn't a chapter id, the chapter id is 0 or there is a coursepageId
                if ( !chapterId.HasValue || chapterId == 0 || coursepageId.HasValue )
                {
                    this.Visible = false;
                    return;
                }
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
            BindGrid();
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
            CoursePageService coursePageService = new CoursePageService( rockContext );

            var chapterId = PageParameter( PageParameterKey.ChapterId ).AsInteger();
            var qry = coursePageService.Queryable()
                .Where( c => c.ChapterId == chapterId )
                .OrderBy( c => c.Order )
                .ToList();

            var pages = new List<CoursePageDisplay>();

            foreach ( var item in qry )
            {
                pages.Add( new CoursePageDisplay
                {
                    Id = item.Id,
                    Name = item.Name,
                    //Type = ( ( CoursePageComponent ) CoursePageContainer.Instance.Dictionary
                    //    .Where( c => c.Value.Value.EntityType.Id == item.EntityTypeId )
                    //    .Select( c => c.Value.Value )
                    //    .FirstOrDefault() )
                    //    .Name
                } );
            }

            gList.DataSource = pages;
            gList.DataBind();
        }

        #endregion

        protected void gList_RowSelected( object sender, RowEventArgs e )
        {
            var coursePageId = e.RowKeyValue.ToString();
            NavigateToCurrentPage( new Dictionary<string, string>() {
                { PageParameterKey.CoursePageId, coursePageId }
            } );
        }

        public void SetVisible( bool visible )
        {
            upnlContent.Visible = visible;
        }

        protected void btnDelete_Click( object sender, RowEventArgs e )
        {
            var id = ( int ) e.RowKeyValue;
            RockContext rockContext = new RockContext();
            CoursePageService coursePageService = new CoursePageService( rockContext );
            var coursePage = coursePageService.Get( id );
            if ( coursePage != null )
            {
                coursePageService.Delete( coursePage );
                rockContext.SaveChanges();
            }

            BindGrid();
        }

        protected void btnAddPage_Click( object sender, EventArgs e )
        {
            NavigateToCurrentPage( new Dictionary<string, string>
            {
                { PageParameterKey.ChapterId , PageParameter(PageParameterKey.ChapterId)},
                { PageParameterKey.CoursePageId, "0"}
            } );
        }
    }

    class CoursePageDisplay
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }
}