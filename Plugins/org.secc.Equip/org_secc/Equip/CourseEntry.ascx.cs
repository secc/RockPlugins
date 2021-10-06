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
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using OpenXmlPowerTools;
using org.secc.Equip;
using org.secc.Equip.Helpers;
using org.secc.Equip.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org.secc.Equip
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Course Entry" )]
    [Category( "SECC > Equip" )]
    [Description( "This block handles the user learning experience." )]

    [LinkedPage( "Completion Page",
        Key = AttributeKey.CompletionPage,
        Description = "Page to send the user to upon completion of the course. If left blank will send to parent page.",
        IsRequired = false
        )]
    public partial class CourseEntry : Rock.Web.UI.RockBlock
    {

        #region Attribute Keys

        /// <summary>
        /// Keys to use for Block Attributes
        /// </summary>
        protected static class AttributeKey
        {
            internal const string CompletionPage = "CompletionPage";
        }

        #endregion Attribute Keys

        #region PageParameterKeys

        /// <summary>
        /// Keys to use for Page Parameters
        /// </summary>
        protected static class PageParameterKey
        {
            internal const string CourseId = "courseid";
            internal const string Slug = "slug";
            internal const string Complete = "complete";
        }

        #endregion PageParameterKeys

        private CoursePageRecord coursePageRecord;
        private List<Control> controls;
        private string externalRedirectDebug;

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

        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );
            if ( ViewState["coursePageRecordId"] is int )
            {
                var coursePageRecordId = ( int ) ViewState["coursePageRecordId"];
                RockContext rockContext = new RockContext();
                CoursePageRecordService coursePageRecordService = new CoursePageRecordService( rockContext );
                coursePageRecord = coursePageRecordService.Get( coursePageRecordId );
                if ( coursePageRecord != null )
                {
                    DisplayCoursePage();
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            btnNext.Visible = true;
            if ( !Page.IsPostBack )
            {
                RockContext rockContext = new RockContext();
                coursePageRecord = GetCurrentCoursePageRecord( rockContext );

                if ( coursePageRecord == null )
                {
                    //This has a value when the user is an editor and the course is external
                    if ( externalRedirectDebug.IsNotNullOrWhiteSpace() )
                    {
                        DisplayExternalRedirectDebug();
                    }
                    else
                    {
                        DisplayCompletion();
                    }

                    return;
                }
                RockPage.PageTitle = coursePageRecord.CoursePage.Chapter.Course.Name;
                RockPage.BrowserTitle = RockPage.PageTitle;
                RockPage.Title = RockPage.PageTitle;
                Page.Title = RockPage.PageTitle;
                RockPage.BreadCrumbs.Last().Name = RockPage.PageTitle;

                DisplayCoursePage();
            }
        }

        private void DisplayExternalRedirectDebug()
        {
            pnlElements.Visible = false;
            pnlSuccess.Visible = false;
            pnlFail.Visible = false;
            pnlSuccess.Visible = false;
            pnlTOC.Visible = false;

            pnlRedirectDebug.Visible = true;
            nbRedirectDebug.Visible = true;

            var queryToAppend = "complete=true";
            var returnUrl = new UriBuilder(Request.Url);
            if (returnUrl.Query !=null && returnUrl.Query.Length > 1 )
            {
                returnUrl.Query = returnUrl.Query.Substring( 1 ) + "&" + queryToAppend;
            }
            else
            {
                returnUrl.Query = queryToAppend;
            }

            nbRedirectDebug.Text = string.Format(
@"If you were not an editor of this course you would have been redirected to <a href='{0}'>{0}</a>
<br><br>
To complete this course have your external course provider redirect the user back to {1}.
<br><br>
<a href='{1}'>Click Here</a> to complete this course.
",
                externalRedirectDebug,
                returnUrl.ToString() );
        }

        private void DisplayCoursePage()
        {
            pnlElements.Visible = true;
            pnlSuccess.Visible = false;
            pnlFail.Visible = false;

            phContent.Controls.Clear();
            var pageComponent = GetPageComponent();

            controls = pageComponent.DisplayCoursePage( coursePageRecord.CoursePage, phContent );

            int i = 0;
            foreach ( var control in controls )
            {
                // make sure each qualifier control has a unique/predictable ID to help avoid viewstate issues
                var controlTypeName = control.GetType().Name;
                var oldControlId = control.ID ?? string.Empty;
                control.ID = string.Format( "qualifier_{0}_{1}", control.ID, i++ );

                // if this is a RockControl with a required field validator, make sure RequiredFieldValidator.ControlToValidate gets updated with the new control id
                if ( control is IRockControl )
                {
                    var rockControl = ( IRockControl ) control;
                    if ( rockControl.RequiredFieldValidator != null )
                    {
                        if ( rockControl.RequiredFieldValidator.ControlToValidate == oldControlId )
                        {
                            rockControl.RequiredFieldValidator.ControlToValidate = control.ID;
                        }
                    }
                }
            }

            UpdateTableOfContents( coursePageRecord.CoursePage );

        }

        private void UpdateTableOfContents( CoursePage coursePage )
        {
            phTOC.Controls.Clear();
            phMobileTOC.Controls.Clear();

            var course = coursePage.Chapter.Course;
            var passed = false;
            var first = true;

            var mostAdvancedCoursePageId = coursePage.Id;

            if ( ViewState["coursePageRecordId"] is int )
            {
                RockContext rockContext = new RockContext();
                CoursePageRecordService coursePageRecordService = new CoursePageRecordService( rockContext );
                mostAdvancedCoursePageId = coursePageRecordService.Get( ( int ) ViewState["coursePageRecordId"] ).CoursePageId;
            }



            foreach ( var chapter in course.Chapters.OrderBy( c => c.Order ).ToList() )
            {
                var ltChapter = new Literal
                {
                    ID = "ltChapter" + chapter.Id.ToString(),
                };
                if ( coursePage.ChapterId == chapter.Id )
                {
                    ltChapter.Text = string.Format( "<b>{0}</b></br>", chapter.Name );
                }
                else
                {
                    ltChapter.Text = string.Format( "{0}</br>", chapter.Name );
                }
                phTOC.Controls.Add( ltChapter );

                //Mobile
                if ( !first )
                {
                    HtmlGenericControl liDivider = new HtmlGenericControl( "li" );
                    liDivider.AddCssClass( "divider" );
                    phMobileTOC.Controls.Add( liDivider );
                }
                first = false;

                HtmlGenericControl liChapter = new HtmlGenericControl( "li" );
                liChapter.AddCssClass( "dropdown-header" );
                liChapter.InnerText = chapter.Name;
                phMobileTOC.Controls.Add( liChapter );


                //Add the course pages
                foreach ( var page in chapter.CoursePages.OrderBy( c => c.Order ).ToList() )
                {
                    phTOC.Controls.Add( new Literal { Text = "&emsp;" } );

                    if ( page.Id == coursePage.Id )
                    {
                        Literal ltPage = new Literal
                        {
                            ID = "ltPage" + page.Id.ToString(),
                            Text = string.Format( "<a href='#'><i class='fa fa-caret-right'></i> {0}</a><br>", page.Name )
                        };

                        phTOC.Controls.Add( ltPage );

                        //Mobile
                        phMobileTitle.Controls.Clear();
                        Literal mobileTitle = new Literal
                        {
                            Text = page.Name
                        };
                        phMobileTitle.Controls.Add( mobileTitle );

                        HtmlGenericControl li = new HtmlGenericControl( "li" )
                        {
                            InnerHtml = string.Format( "<a href='#'><i class='fa fa-caret-right'></i> {0}</a>", page.Name )
                        };
                        phMobileTOC.Controls.Add( li );

                    }
                    else
                    {
                        LinkButton lbPage = new LinkButton
                        {
                            ID = "lbPage" + page.Id.ToString(),
                            Text = string.Format( "{0}<br>", page.Name )
                        };

                        HtmlGenericControl li = new HtmlGenericControl( "li" );
                        phMobileTOC.Controls.Add( li );

                        LinkButton lbMobile = new LinkButton
                        {
                            ID = "lbMobile" + page.Id.ToString(),
                            Text = page.Name
                        };

                        if ( passed )
                        {
                            lbPage.Enabled = false;
                            lbMobile.Enabled = false;
                        }
                        else
                        {

                            lbPage.Click += ( s, e ) => { MoveToPage( page.Id ); };
                            lbMobile.Click += ( s, e ) => { MoveToPage( page.Id ); };
                        }

                        phTOC.Controls.Add( lbPage );
                        li.Controls.Add( lbMobile );
                    }


                    if ( page.Id == mostAdvancedCoursePageId )
                    {
                        passed = true;
                    }

                }

            }


        }

        private void MoveToPage( int coursePageId )
        {
            var currentPersonAliasIds = CurrentPerson.Aliases.Select( a => a.Id ).ToList();

            RockContext rockContext = new RockContext();
            CoursePageRecordService coursePageRecordService = new CoursePageRecordService( rockContext );
            coursePageRecord = coursePageRecordService.Queryable()
                .Where( r => r.CoursePageId == coursePageId && currentPersonAliasIds.Contains( r.ChapterRecord.CourseRecord.PersonAliasId ) )
                .OrderByDescending( r => r.CreatedDateTime )
                .FirstOrDefault();

            btnNext.Visible = false;


            DisplayCoursePage();
        }

        private CoursePageComponent GetPageComponent()
        {
            return ( CoursePageComponent ) CoursePageContainer.Instance.Dictionary
               .Where( c => c.Value.Value.EntityType.Id == coursePageRecord.CoursePage.EntityTypeId )
               .Select( c => c.Value.Value )
               .FirstOrDefault();
        }

        private CoursePageRecord GetCurrentCoursePageRecord( RockContext rockContext )
        {
            CourseRecordService courseRecordService = new CourseRecordService( rockContext );
            ChapterRecordService chapterRecordService = new ChapterRecordService( rockContext );
            CoursePageRecordService coursePageRecordService = new CoursePageRecordService( rockContext );

            CourseRecord courseRecord = GetCourseRecord( courseRecordService );

            if ( externalRedirectDebug.IsNotNullOrWhiteSpace() )
            {
                return null;
            }

            if ( courseRecord == null  )
            {
                throw new Exception( "This course does not exist or you are not authorized" );
            }

            ChapterRecord chapterRecord = GetNextChapterRecord( courseRecord, chapterRecordService );

            while ( chapterRecord != null )
            {
                CoursePageRecord coursePageRecord = GetNextCoursePageRecord( coursePageRecordService, chapterRecord );
                if ( coursePageRecord != null )
                {
                    ViewState["coursePageRecordId"] = coursePageRecord.Id;
                    return coursePageRecord;
                }
                else
                {
                    chapterRecord.CompletionDateTime = RockDateTime.Now;
                    chapterRecord.Passed = chapterRecord.CoursePageRecords.Where( p => p.Passed ).Count() >= chapterRecord.Chapter.CoursePages.Count();
                    rockContext.SaveChanges();

                    if ( chapterRecord.Passed == false )
                    {
                        ShowFailedChapter();
                        return null;
                    }
                    chapterRecord = GetNextChapterRecord( courseRecord, chapterRecordService );
                }
            }

            //if we got to this place the course is complete
            courseRecord.CompletionDateTime = RockDateTime.Now;
            courseRecord.Passed = courseRecord.ChapterRecords.Where( c => c.Passed ).Count() >= courseRecord.Course.Chapters.Count();
            rockContext.SaveChanges();

            CourseRequirementHelper.UpdateCourseStatuses( courseRecord.CourseId, courseRecord.PersonAliasId, courseRecord.Passed );

            DisplayCompletion();
            return null;
        }

        private void ShowFailedChapter()
        {
            ViewState["coursePageRecordId"] = null;
            RockContext rockContext = new RockContext();
            coursePageRecord = GetCurrentCoursePageRecord( rockContext );
            pnlElements.Visible = false;
            pnlSuccess.Visible = false;
            pnlFail.Visible = true;
        }


        private CoursePageRecord GetNextCoursePageRecord( CoursePageRecordService coursePageRecordService, ChapterRecord chapterRecord )
        {
            var pages = chapterRecord.Chapter.CoursePages.OrderBy( p => p.Order );
            foreach ( var page in pages )
            {
                var pageRecords = chapterRecord.CoursePageRecords
                    .Where( r => r.CoursePageId == page.Id );

                //If no page record exists create one
                if ( !pageRecords.Any() )
                {
                    var pageRecord = new CoursePageRecord
                    {
                        CoursePage = page,
                        ChapterRecord = chapterRecord
                    };
                    coursePageRecordService.Add( pageRecord );
                    coursePageRecordService.Context.SaveChanges();
                    return pageRecord;
                }

                //Return incomplete page
                if ( pageRecords.Where( r => r.CompletionDateTime == null ).Any() )
                {
                    return pageRecords.Where( r => r.CompletionDateTime == null ).FirstOrDefault();
                }
            }
            return null; //There are no more page records to complete for this chapter
        }

        private ChapterRecord GetNextChapterRecord( CourseRecord courseRecord, ChapterRecordService chapterRecordService )
        {
            var chapters = courseRecord.Course.Chapters.OrderBy( c => c.Order ).ToList();
            foreach ( var chapter in chapters )
            {
                var chapterRecords = courseRecord.ChapterRecords
                    .Where( cr => cr.ChapterId == chapter.Id );

                //If no records for this chapter make a new one
                if ( !chapterRecords.Any() )
                {
                    var chapterRecord = new ChapterRecord
                    {
                        Chapter = chapter,
                        CourseRecord = courseRecord
                    };
                    chapterRecordService.Add( chapterRecord );
                    chapterRecordService.Context.SaveChanges();
                    return chapterRecord;
                }

                //If there is an incomplete record select that one
                if ( chapterRecords.Where( cr => cr.CompletionDateTime == null ).Any() )
                {
                    return chapterRecords.Where( cr => cr.CompletionDateTime == null ).FirstOrDefault();

                }

                //If there are no passed records make a new one
                if ( !chapterRecords.Where( cr => cr.Passed ).Any() )
                {
                    var chapterRecord = new ChapterRecord
                    {
                        Chapter = chapter,
                        CourseRecord = courseRecord
                    };
                    chapterRecordService.Add( chapterRecord );
                    chapterRecordService.Context.SaveChanges();
                    return chapterRecord;
                }

                //Continue loop because this chapter has been passed
            }
            return null;
        }

        #endregion

        #region Events


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
        private CourseRecord GetCourseRecord()
        {
            return GetCourseRecord( new CourseRecordService( new RockContext() ) );
        }

        private CourseRecord GetCourseRecord( CourseRecordService courseRecordService )
        {
            if ( CurrentPerson == null )
            {
                return null;
            }

            var rockContext = courseRecordService.Context as RockContext;
            var courseId = PageParameter( PageParameterKey.CourseId ).AsInteger();
            var slug = PageParameter( PageParameterKey.Slug );
            CourseService courseService = new CourseService( rockContext );
            var course = courseService.Queryable().Where( c => c.Slug == slug ).FirstOrDefault();


            if ( course == null )
            {
                course = courseService.Get( courseId );
            }

            if ( course == null || !course.PersonCanView( CurrentPerson ) )
            {
                return null;
            }

            var currentPersonAliasIds = CurrentPerson.Aliases.Select( a => a.Id ).ToList();

            var courseRecord = courseRecordService.Queryable()
                .Where( cr => cr.CourseId == course.Id ) //this course
                .Where( cr => currentPersonAliasIds.Contains( cr.PersonAliasId ) ) //for this person
                .Where( cr => cr.CompletionDateTime == null ) //not complete
                .FirstOrDefault();

            if ( courseRecord == null )
            {
                courseRecord = new CourseRecord
                {
                    CourseId = course.Id,
                    PersonAliasId = CurrentPersonAliasId.Value
                };
                courseRecordService.Add( courseRecord );
                rockContext.SaveChanges();
            }

            if ( course.ExternalCourseUrl.IsNotNullOrWhiteSpace() )
            {
                if ( PageParameter( PageParameterKey.Complete ).IsNotNullOrWhiteSpace() && Request.UrlReferrer != null )
                {
                    //This course is external and it has come back to completion.
                    courseRecord.CompletionDateTime = Rock.RockDateTime.Now;
                    courseRecord.Passed = true;
                    rockContext.SaveChanges();
                }
                else //This course is external and we must redirect!
                {

                    if ( course.IsAuthorized( Rock.Security.Authorization.EDIT, CurrentPerson ) )
                    {
                        externalRedirectDebug = course.ExternalCourseUrl;
                        return null;
                    }
                    else
                    {
                        Response.Redirect( course.ExternalCourseUrl, true );
                    }

                }
            }
            return courseRecord;
        }


        #endregion


        protected void btnNext_Click( object sender, EventArgs e )
        {
            //Need to recreate the coursePageRecord with our own context
            RockContext rockContext = new RockContext();
            CoursePageRecordService coursePageRecordService = new CoursePageRecordService( rockContext );
            coursePageRecord = coursePageRecordService.Get( coursePageRecord.Id );

            var component = GetPageComponent();
            component.ScoreCourse( controls, coursePageRecord );
            coursePageRecord.CompletionDateTime = Rock.RockDateTime.Now;
            rockContext.SaveChanges();

            coursePageRecord = GetCurrentCoursePageRecord( rockContext );
            if ( coursePageRecord != null )
            {
                DisplayCoursePage();
            }
        }

        private void DisplayCompletion()
        {
            pnlTOC.Visible = false;
            pnlElements.Visible = false;
            pnlFail.Visible = false;
            phContent.Controls.Clear();
            ViewState["coursePageRecordId"] = null;
            pnlSuccess.Visible = true;
            pnlElements.Visible = false;
        }

        protected void btnFail_Click( object sender, EventArgs e )
        {
            DisplayCoursePage();
        }

        protected void btnComplete_Click( object sender, EventArgs e )
        {
            if ( GetAttributeValue( AttributeKey.CompletionPage ).IsNotNullOrWhiteSpace() )
            {
                NavigateToLinkedPage( AttributeKey.CompletionPage );
            }
            else
            {
                NavigateToParentPage();
            }
        }
    }
}