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
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.UI;
using org.secc.Equip.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web;

namespace RockWeb.Plugins.org.secc.Equip
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Chapter Detail" )]
    [Category( "SECC > Equip" )]
    [Description( "Block for managing a course chapter." )]

    public partial class ChapterDetail : Rock.Web.UI.RockBlock
    {

        #region PageParameterKeys

        /// <summary>
        /// Keys to use for Page Parameters
        /// </summary>
        protected static class PageParameterKey
        {
            internal const string CourseId = "CourseId";
            internal const string ChapterId = "ChapterId";
            internal const string CoursePageId = "CoursePageId";
        }

        #endregion PageParameterKeys

        #region Fields
        #endregion

        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            btnDelete.Attributes["onclick"] = string.Format( "javascript: return Rock.dialogs.confirmDelete(event, '{0}');", Chapter.FriendlyTypeName );

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
                var chapter = GetChapter();
                if ( chapter == null )
                {
                    this.Visible = false;
                    return;
                }

                pdAuditDetails.SetEntity( chapter, "/" );
                if ( chapter.Id == 0 )
                {
                    CourseService courseService = new CourseService( new RockContext() );
                    var course = courseService.Get( PageParameter( PageParameterKey.CourseId ).AsInteger() );
                    if ( course != null )
                    {
                        ltTitle.Text = course.Name + ": New Chapter";
                    }

                    pdAuditDetails.Visible = false;
                    ShowEdit();
                }
                else
                {
                    ltTitle.Text = chapter.Name;
                    ShowDetails();
                }
            }
        }

        private Chapter GetChapter()
        {
            return GetChapter( new ChapterService( new RockContext() ) );
        }

        private Chapter GetChapter( ChapterService chapterService )
        {
            var coursepageId = PageParameter( PageParameterKey.CoursePageId ).AsIntegerOrNull();

            if ( coursepageId.HasValue )
            {

                return null;
            }

            var chapterId = PageParameter( PageParameterKey.ChapterId ).AsIntegerOrNull();

            if ( !chapterId.HasValue )
            {
                return null;
            }

            var chapter = chapterService.Get( chapterId.Value );
            if ( chapter == null )
            {
                chapter = new Chapter()
                {
                    CourseId = PageParameter( PageParameterKey.CourseId ).AsInteger()
                };
            }

            return chapter;
        }

        #endregion

        #region Events
        protected void btnSave_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            ChapterService chapterService = new ChapterService( rockContext );

            var chapter = GetChapter( chapterService );

            chapter.Name = tbName.Text;
            chapter.Description = tbDescription.Text;


            if ( chapter.Id == 0 )
            {
                chapterService.Add( chapter );

                var courseId = PageParameter( PageParameterKey.CourseId ).AsIntegerOrNull();
                var chapters = chapterService
                    .Queryable()
                    .Where( cc => cc.CourseId == courseId )
                    .OrderBy( cc => cc.Order ).ToList();

                chapter.Order = 0;
                if ( chapters.Any() )
                {
                    chapter.Order = chapters.Last().Order + 1;
                }
            }

            rockContext.SaveChanges();
            NavigateToCurrentPage( new Dictionary<string, string> { { PageParameterKey.ChapterId, chapter.Id.ToString() } } );
        }


        protected void btnDelete_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            ChapterService chapterService = new ChapterService( rockContext );
            var chapter = GetChapter( chapterService );
            chapterService.Delete( chapter );
            rockContext.SaveChanges();
            NavigateToParentPage();
        }

        protected void btnEdit_Click( object sender, EventArgs e )
        {
            ShowEdit();
        }

        protected void btnCancel_Click( object sender, EventArgs e )
        {
            var chapter = GetChapter();
            if ( chapter.Id == 0 )
            {
                NavigateToCurrentPage( new Dictionary<string, string> { { PageParameterKey.CourseId, PageParameter( PageParameterKey.CourseId ) } } );
            }
            else
            {
                ShowDetails();
            }
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            ShowDetails();
        }

        #endregion

        #region Methods
        private void ShowDetails()
        {
            var chapter = GetChapter();
            HideSecondaryBlocks( false );
            pnlView.Visible = true;
            pnlEdit.Visible = false;
            ltName.Text = chapter.Name;
            ltDescription.Text = chapter.Description;
        }

        private void ShowEdit()
        {
            var chapter = GetChapter();
            HideSecondaryBlocks( true );
            pnlEdit.Visible = true;
            pnlView.Visible = false;
            tbName.Text = chapter.Name;
            tbDescription.Text = chapter.Description;
        }
        #endregion

    }
}