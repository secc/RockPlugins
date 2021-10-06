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
using System.Web.UI.HtmlControls;
using System.Web;

namespace RockWeb.Plugins.org.secc.Equip
{
    [DisplayName( "Course Outline" )]
    [Category( "SECC > Equip" )]
    [Description( "Displays all of the chapters and pages in a course." )]

    public partial class CourseOutline : RockBlock
    {


        /// <summary>
        /// Keys to use for Block Attributes
        /// </summary>
        protected static class PageParameterKeys
        {
            public const string CourseId = "CourseId";
            public const string ChapterId = "ChapterId";
            public const string CoursePageId = "CoursePageId";
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

            BindOutline();

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

        #endregion

        #region Methods

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindOutline()
        {
            string path = HttpContext.Current.Request.Url.AbsolutePath;

            var course = GetCourse();
            if ( course == null || !course.IsAuthorized( Rock.Security.Authorization.EDIT, CurrentPerson ) )
            {
                return;
            }

            var btnCourse = new HtmlGenericControl( "a" );
            btnCourse.InnerHtml = "<b>" + course.Name + "</b>";
            btnCourse.Attributes.Add( "href", GetUrl( path, new Dictionary<string, object>() { { PageParameterKeys.CourseId, course.Id } } ) );
            phCourseDetails.Controls.Add( btnCourse );

            if ( course.ExternalCourseUrl.IsNotNullOrWhiteSpace() )
            {
                var btnLink = new HtmlGenericControl( "a" );
                btnLink.InnerHtml = course.ExternalCourseUrl;
                btnLink.Attributes.Add( "href", course.ExternalCourseUrl );
                btnLink.Attributes.Add( "target", "blank" );
                btnLink.AddCssClass( "center-block" );
                phCourseDetails.Controls.Add( btnLink );
                return;
            }

            var chapterUl = new HtmlGenericControl( "ul" );
            phCourseDetails.Controls.Add( chapterUl );

            foreach ( var chapter in course.Chapters.OrderBy( c => c.Order ).ToList() )
            {
                var chapterLi = new HtmlGenericControl( "li" );
                chapterUl.Controls.Add( chapterLi );

                var btnChapter = new HtmlGenericControl( "a" );
                btnChapter.InnerHtml = chapter.Name;
                btnChapter.Attributes.Add( "href", GetUrl( path, new Dictionary<string, object>() { { PageParameterKeys.ChapterId, chapter.Id } } ) );
                chapterLi.Controls.Add( btnChapter );

                var chapterId = PageParameter( PageParameterKeys.ChapterId ).AsInteger();
                if ( chapterId == chapter.Id )
                {
                    btnChapter.InnerHtml = "<i>" + chapter.Name + "</i>";
                }

                var coursePageUl = new HtmlGenericControl( "ul" );
                chapterLi.Controls.Add( coursePageUl );

                if ( chapter.CoursePages == null )
                {
                    chapter.CoursePages = new List<CoursePage>();
                }

                foreach ( var coursePage in chapter.CoursePages.OrderBy( p => p.Order ).ToList() )
                {
                    var coursePageLi = new HtmlGenericControl( "li" );
                    coursePageUl.Controls.Add( coursePageLi );

                    var btnCoursePage = new HtmlGenericControl( "a" );
                    btnCoursePage.InnerHtml = coursePage.Name;
                    btnCoursePage.Attributes.Add( "href", GetUrl( path, new Dictionary<string, object>() { { PageParameterKeys.CoursePageId, coursePage.Id } } ) );
                    coursePageLi.Controls.Add( btnCoursePage );

                    var btnCoursePageId = PageParameter( PageParameterKeys.CoursePageId ).AsInteger();
                    if ( btnCoursePageId == coursePage.Id )
                    {
                        btnCoursePage.InnerHtml = "<i>" + coursePage.Name + "</i>";
                    }
                }
            }
        }

        private string GetUrl( string path, Dictionary<string, object> queryStrings )
        {
            path = path + "?";
            foreach ( var param in queryStrings )
            {
                path += param.Key + "=" + param.Value.ToString() + "&";
            }

            return path.TrimEnd( '&' );
        }

        private void EditCourse( int courseId )
        {
            NavigateToCurrentPage( new Dictionary<string, string> {
                { PageParameterKeys.CourseId, courseId.ToString() }
            } );
        }

        private void EditCoursePage( int coursePageId )
        {
            NavigateToCurrentPage( new Dictionary<string, string> {
                { PageParameterKeys.CoursePageId, coursePageId.ToString() },
            } );
        }

        private void EditChapter( int chapterId )
        {
            NavigateToCurrentPage( new Dictionary<string, string> {
                { PageParameterKeys.ChapterId, chapterId.ToString() }
            } );
        }

        private Course GetCourse()
        {
            RockContext rockContext = new RockContext();

            var courseId = PageParameter( PageParameterKeys.CourseId ).AsIntegerOrNull();
            if ( courseId != null )
            {
                CourseService courseService = new CourseService( rockContext );
                return courseService.Get( courseId.Value );
            }

            var chapterId = PageParameter( PageParameterKeys.ChapterId ).AsIntegerOrNull();
            if ( chapterId != null )
            {
                ChapterService chapterService = new ChapterService( rockContext );
                var chapter = chapterService.Get( chapterId.Value );
                return chapter.Course;
            }

            var coursePageId = PageParameter( PageParameterKeys.CoursePageId ).AsIntegerOrNull();
            if ( coursePageId != null )
            {
                CoursePageService coursePageService = new CoursePageService( rockContext );
                return coursePageService.Get( coursePageId.Value ).Chapter.Course;
            }

            return null;
        }

        #endregion
    }
}