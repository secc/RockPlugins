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
using System.IO;
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

namespace RockWeb.Plugins.org.secc.Equip
{
    [DisplayName( "Chapter List" )]
    [Category( "SECC > Equip" )]
    [Description( "Block for displaying all of the available chapters to manage." )]

    #region Attributes

    #endregion
    public partial class ChapterList : RockBlock
    {
        protected static class PageParameterKey
        {
            internal const string CategoryId = "CategoryId";
            internal const string ChapterId = "ChapterId";
            internal const string CourseId = "CourseId";
            internal const string CoursePageId = "CoursePageId";
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
            gList.GridRebind += gList_GridRebind;
            gList.GridReorder += gList_GridReorder;

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        private void gList_GridReorder( object sender, GridReorderEventArgs e )
        {
            var rockContext = new RockContext();
            ChapterService chapterService = new ChapterService( rockContext );
            var courseId = PageParameter( PageParameterKey.CourseId ).AsInteger();
            var items = chapterService.Queryable().Where( p => p.CourseId == courseId ).OrderBy( i => i.Order ).ToList();
            chapterService.Reorder( items, e.OldIndex, e.NewIndex );
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
                var chaperId = PageParameter( PageParameterKey.ChapterId ).AsIntegerOrNull();
                var coursepageId = PageParameter( PageParameterKey.CoursePageId ).AsIntegerOrNull();

                if ( chaperId.HasValue || coursepageId.HasValue )
                {
                    this.Visible = false;
                }
                else
                {
                    BindGrid();
                }
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
            var courseId = PageParameter( PageParameterKey.CourseId ).AsInteger();
            RockContext rockContext = new RockContext();
            CourseService courseService = new CourseService( rockContext );
            var course = courseService.Get( courseId );
            if ( course == null || course.ExternalCourseUrl.IsNotNullOrWhiteSpace() )
            {
                this.Visible = false;
                return;
            }

            ChapterService chapterService = new ChapterService( rockContext );


            var qry = chapterService.Queryable()
                .Where( cc => cc.CourseId == courseId )
                .OrderBy( cc => cc.Order );

            gList.SetLinqDataSource( qry );
            gList.DataBind();
        }

        #endregion

        protected void gList_RowSelected( object sender, RowEventArgs e )
        {
            NavigateToCurrentPage( new Dictionary<string, string> { { PageParameterKey.ChapterId, e.RowKeyId.ToString() } } );
        }

        public void SetVisible( bool visible )
        {
            upnlContent.Visible = visible;
        }

        protected void btnDelete_Click( object sender, RowEventArgs e )
        {
            var id = ( int ) e.RowKeyValue;
            RockContext rockContext = new RockContext();
            ChapterService chapterService = new ChapterService( rockContext );
            var coursePage = chapterService.Get( id );
            if ( coursePage != null )
            {
                chapterService.Delete( coursePage );
                rockContext.SaveChanges();
            }

            BindGrid();
        }

        protected void btnAddChapter_Click( object sender, EventArgs e )
        {
            NavigateToCurrentPage(
                   new Dictionary<string, string> {
                        { PageParameterKey.ChapterId, "0" },
                        { PageParameterKey.CourseId, PageParameter( PageParameterKey.CourseId ) }
                   } );
        }
    }
}