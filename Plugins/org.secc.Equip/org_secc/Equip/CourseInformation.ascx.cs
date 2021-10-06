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
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web.UI;
using DocumentFormat.OpenXml.Office2013.Word;
using org.secc.Equip.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace RockWeb.Plugins.org.secc.Equip
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Course Information" )]
    [Category( "SECC > Equip" )]
    [Description( "Block for course information" )]

    [LinkedPage(
        "Edit Page",
        Description = "Page to edit the course",
        Key = AttributeKey.EditPage )]

    [LinkedPage(
        "Course Requirement Detail Page",
        Description = "Page which contains the details to set the requirements for a given course.",
        Key = AttributeKey.RequirementDetailPage )]

    public partial class CourseInformation : Rock.Web.UI.RockBlock
    {

        #region Attribute Keys

        /// <summary>
        /// Keys to use for Block Attributes
        /// </summary>
        protected static class AttributeKey
        {
            internal const string EditPage = "EditPage";
            internal const string RequirementDetailPage = "RequirementDetailPage";
        }

        #endregion Attribute Keys

        #region PageParameterKeys

        /// <summary>
        /// Keys to use for Page Parameters
        /// </summary>
        protected static class PageParameterKey
        {
            internal const string CourseId = "CourseId";
            internal const string ParentCategoryId = "parentCategoryId";
        }

        #endregion PageParameterKeys

        #region Fields
        private Course course;
        private RockContext rockContext;
        private CourseService courseService;
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

            var courseId = PageParameter( PageParameterKey.CourseId ).AsIntegerOrNull();

            if ( courseId.HasValue )
            {
                rockContext = new RockContext();
                courseService = new CourseService( rockContext );
                course = courseService.Get( courseId.Value );

                if ( course == null )
                {
                    course = new Course()
                    {
                        CategoryId = PageParameter( PageParameterKey.ParentCategoryId ).AsInteger(),
                        Guid = Guid.NewGuid()
                    };

                }
                else
                {
                    if ( !course.IsAuthorized( Rock.Security.Authorization.VIEW, CurrentPerson ) )
                    {
                        course = null;
                    }
                }
            }

            pCategory.EntityTypeId = EntityTypeCache.Get( typeof( Course ) ).Id;

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
                if ( course == null )
                {
                    this.Visible = false;
                    return;
                }
                ltTitle.Text = course.Id != 0 ? "<i class='" + course.IconCssClass + "'></i> " + course.Name : "New Course";
                pdAuditDetails.SetEntity( course, "/" );
                if ( course.Id == 0 )
                {
                    pdAuditDetails.Visible = false;
                    ShowEdit();
                }
                else
                {
                    ShowDetails();
                }
            }
        }

        #endregion

        #region Events
        protected void btnSave_Click( object sender, EventArgs e )
        {
            if ( !CheckSlug() )
            {
                return;
            }

            course.Name = tbName.Text;
            course.Slug = tbSlug.Text;
            course.Description = tbDescription.Text;
            course.CategoryId = pCategory.SelectedValueAsId();
            course.IsActive = false;
            course.ImageId = uImage.BinaryFileId;
            course.ExternalCourseUrl = tbExternalUrl.Text;

            if ( course.ImageId.HasValue )
            {
                BinaryFileService binaryFileService = new BinaryFileService( rockContext );
                var img = binaryFileService.Get( course.ImageId.Value );
                if ( img != null )
                {
                    img.IsTemporary = false;
                }
            }

            if ( course.Id == 0 )
            {
                courseService.Add( course );
            }

            rockContext.SaveChanges();
            //If they want a new course take them to the edit page

            NavigateToLinkedPage( AttributeKey.EditPage,
                new Dictionary<string, string> {
                        { PageParameterKey.CourseId, course.Id.ToString()}
                } );
        }

        private bool CheckSlug()
        {
            var slug = tbSlug.Text;

            if ( slug.IsNullOrWhiteSpace() )
            {
                nbSlug.Visible = false;
                return true;
            }

            slug = slug.RemoveAllNonAlphaNumericCharacters().ToLower();
            tbSlug.Text = slug;

            RockContext rockContext = new RockContext();
            CourseService courseService = new CourseService( rockContext );

            if ( !courseService.Queryable().Where( c => c.Slug == slug ).Any() )
            {
                //The slug is available
                nbSlug.Visible = false;
                return true;
            }

            //The slug is not available

            nbSlug.Visible = true;
            return false;
        }

        protected void btnEdit_Click( object sender, EventArgs e )
        {
            NavigateToLinkedPage( AttributeKey.EditPage,
                new Dictionary<string, string> {
                        { PageParameterKey.CourseId,  PageParameter(PageParameterKey.CourseId)}
                } );
        }

        protected void btnCancel_Click( object sender, EventArgs e )
        {
            if ( course.Id == 0 )
            {
                NavigateToCurrentPage( new Dictionary<string, string> { { PageParameterKey.CourseId, PageParameter( PageParameterKey.ParentCategoryId ) } } );
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
            if ( !course.IsAuthorized( Rock.Security.Authorization.EDIT, CurrentPerson ) )
            {
                btnEdit.Visible = false;
            }

            HideSecondaryBlocks( false );
            pnlView.Visible = true;
            pnlEdit.Visible = false;
            ltDescription.Text = course.Description;
            if ( course.Image != null )
            {
                ltImage.Text = string.Format( "<img src='/GetImage.ashx?Guid={0}' class='img-responsive'>", course.Image.Guid );
            }

            if ( course.IsActive )
            {
                ltLabels.Text = " <span class='label label-success' data-toggle='tooltip'>Active</span>";
            }
            else
            {
                ltLabels.Text = " <span class='label label-danger' data-toggle='tooltip'>Inactive</span>";
            }

            if ( course.ExternalCourseUrl.IsNotNullOrWhiteSpace() )
            {
                ltLabels.Text += " <span class='label label-info' data-toggle='tooltip'data-original-title='Course is hosted outside of Rock.'>External Course</span>";
            }

            CourseRecordService courseRecordService = new CourseRecordService( rockContext );
            var recordsQry = courseRecordService.Queryable()
                .Where( r => r.CourseId == course.Id );

            var attempts = recordsQry.Count();
            var completes = recordsQry.Where( r => r.Passed ).Count();
            var attemptPeople = recordsQry.DistinctBy( r => r.PersonAlias.PersonId ).Count();
            var completesPeople = recordsQry.Where( r => r.Passed ).DistinctBy( r => r.PersonAlias.PersonId ).Count();

            ltStatus.Text = string.Format( "Taken {0} times by {1} {2}.<br>Completed {3} times by {4} {5}.",
                attempts,
                attemptPeople,
                attemptPeople == 1 ? "person" : "people",
                completes,
                completesPeople,
                completesPeople == 1 ? "person" : "people"
                );

            var records = recordsQry
                 .GroupBy( r => r.PersonAlias.Person )
                 .Select( r => new
                 {
                     Person = r.Key,
                     TimesCompleted = r.Where( cr => cr.Passed ).Count(),
                     CompletedDateTime = r.Select( cr => cr.CompletionDateTime ).OrderByDescending( dt => dt ).FirstOrDefault()
                 } )
                 .ToList();
            gReport.DataSource = records;
            gReport.DataBind();

            CourseRequirementService courseRequirementService = new CourseRequirementService( rockContext );
            var courseRequirements = courseRequirementService.Queryable( "Group,DataView" ).AsNoTracking().Where( cr => cr.CourseId == course.Id ).ToList();


            var sb = new StringBuilder();
            foreach ( var requirement in courseRequirements )
            {
                if ( requirement.Group != null )
                {
                    sb.Append( string.Format( "<a href='{0}'>Group: {1}</a><br>",
                        LinkedPageUrl( AttributeKey.RequirementDetailPage, new Dictionary<string, string> { { "CourseRequirementId", requirement.Id.ToString() } } ),
                        requirement.Group.Name ) );
                }
                else if ( requirement.DataView != null )
                {
                    sb.Append( string.Format( "<a href='{0}'>DataView: {1}</a><br>",
                        LinkedPageUrl( AttributeKey.RequirementDetailPage, new Dictionary<string, string> { { "CourseRequirementId", requirement.Id.ToString() } } ),
                        requirement.DataView.Name ) );
                }
            }

            var requirementsPage = LinkedPageUrl( AttributeKey.RequirementDetailPage, new Dictionary<string, string> { { PageParameterKey.CourseId, course.Id.ToString() } } );

            sb.Append( string.Format( "<small><a href='{0}'><i>Add New Course Requirement</i></a></small>", requirementsPage ) );

            ltRequirements.Text = sb.ToString();

            if ( course.AllowedGroupId.HasValue && course.AllowedGroup != null )
            {
                ltAllowed.Visible = true;
                ltAllowed.Label = "Restricted To Group";
                ltAllowed.Text = course.AllowedGroup.Name;

            }
            else if ( course.AllowedDataViewId.HasValue && course.AllowedDataView != null )
            {
                ltAllowed.Visible = true;
                ltAllowed.Label = "Restricted To Data View";
                ltAllowed.Text = course.AllowedDataView.Name;
            }

            if ( course.ExternalCourseUrl.IsNotNullOrWhiteSpace() )
            {
                ltExternalUrl.Visible = true;
                ltExternalUrl.Text = course.ExternalCourseUrl;
            }

        }

        private void ShowEdit()
        {
            HideSecondaryBlocks( true );
            pnlEdit.Visible = true;
            pnlView.Visible = false;
            tbName.Text = course.Name;
            tbDescription.Text = course.Description;
            pCategory.SetValue( course.CategoryId );
        }
        #endregion




        protected void tbSlug_TextChanged( object sender, EventArgs e )
        {
            CheckSlug();
        }
    }
}