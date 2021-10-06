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
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using OpenXmlPowerTools;
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
    [DisplayName( "Course Detail" )]
    [Category( "SECC > Equip" )]
    [Description( "Block for managing a learning course." )]

    public partial class CourseDetail : Rock.Web.UI.RockBlock
    {

        #region Attribute Keys

        /// <summary>
        /// Keys to use for Block Attributes
        /// </summary>
        protected static class AttributeKey
        {

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
            internal const string ChapterId = "ChapterId";
            internal const string CoursePageId = "CoursePageId";
        }

        #endregion PageParameterKeys

        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            btnSecurity.EntityTypeId = EntityTypeCache.Get( typeof( Course ) ).Id;

            pCategory.EntityTypeId = EntityTypeCache.Get( typeof( Course ) ).Id;
            btnDelete.Attributes["onclick"] = string.Format( "javascript: return Rock.dialogs.confirmDelete(event, '{0}');", Course.FriendlyTypeName );

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
                var course = GetCourse();

                if ( course == null || !course.IsAuthorized( Rock.Security.Authorization.EDIT, CurrentPerson ) )
                {
                    this.Visible = false;
                    return;
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

                ltTitle.Text = course.Id != 0 ? "<i class='"+course.IconCssClass+"'></i> "+ course.Name : "New Course";
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

        private Course GetCourse()
        {
            return GetCourse( new CourseService( new RockContext() ) );
        }

        private Course GetCourse( CourseService courseService )
        {
            //Check to see if the user really wants to look at a chapter or a course page and hide if that's the case
            var chaperId = PageParameter( PageParameterKey.ChapterId ).AsIntegerOrNull();
            var coursepageId = PageParameter( PageParameterKey.CoursePageId ).AsIntegerOrNull();

            if ( chaperId.HasValue || coursepageId.HasValue )
            {
                return null;
            }

            var courseId = PageParameter( PageParameterKey.CourseId ).AsIntegerOrNull();

            if ( courseId.HasValue )
            {
                var course = courseService.Get( courseId.Value );

                if ( course == null )
                {
                    course = new Course()
                    {
                        CategoryId = PageParameter( PageParameterKey.ParentCategoryId ).AsInteger(),
                        Guid = Guid.NewGuid()
                    };

                }
                return course;
            }
            return null;
        }

        #endregion

        #region Events
        protected void btnSave_Click( object sender, EventArgs e )
        {
            if ( !CheckSlug() )
            {
                return;
            }

            RockContext rockContext = new RockContext();
            CourseService courseService = new CourseService( rockContext );
            var course = GetCourse( courseService );
            course.Name = tbName.Text;
            course.IsActive = cbIsActive.Checked;
            course.Description = tbDescription.Text;
            course.IconCssClass = tbIconCssClass.Text;
            course.CategoryId = pCategory.SelectedValueAsId();
            course.Slug = tbSlug.Text;

            var oldImageId = course.ImageId;
            course.ImageId = uImage.BinaryFileId;

            if ( oldImageId != course.ImageId )
            {
                BinaryFileService binaryFileService = new BinaryFileService( rockContext );
                var oldImage = binaryFileService.Get( oldImageId ?? 0 );
                if ( oldImage != null )
                {
                    oldImage.IsTemporary = true;
                }
                var newImage = binaryFileService.Get( course.ImageId ?? 0 );
                if ( newImage != null )
                {
                    newImage.IsTemporary = false;
                }
            }

            switch ( ddlViewSecurity.SelectedValue )
            {
                case "Group":
                    course.AllowedDataViewId = null;
                    course.AllowedGroupId = pAllowedGroup.GroupId;
                    break;
                case "DataView":
                    course.AllowedGroupId = null;
                    course.AllowedDataViewId = pAllowedDataView.SelectedValueAsId();
                    break;
                default:
                    course.AllowedGroupId = null;
                    course.AllowedDataViewId = null;
                    break;
            }

            course.ExternalCourseUrl = tbExternalUrl.Text;

            if ( course.Id == 0 )
            {
                courseService.Add( course );
            }

            rockContext.SaveChanges();
            NavigateToCurrentPage( new Dictionary<string, string> { { PageParameterKey.CourseId, course.Id.ToString() } } );
        }


        protected void btnDelete_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            CourseService courseService = new CourseService( rockContext );
            var course = GetCourse( courseService );
            courseService.Delete( course );
            rockContext.SaveChanges();
            NavigateToCurrentPage();
        }

        protected void btnEdit_Click( object sender, EventArgs e )
        {
            ShowEdit();
        }

        protected void btnCancel_Click( object sender, EventArgs e )
        {
            var course = GetCourse();
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
            var course = GetCourse();
            pnlView.Visible = true;
            pnlEdit.Visible = false;
            ltName.Text = course.Name;
            ltDescription.Text = course.Description;
            ltSlug.Text = course.Slug;
            if ( course.Image != null )
            {
                ltImage.Text = string.Format( "<img src='/GetImage.ashx?Guid={0}' class='img-responsive'>", course.Image.Guid );
            }

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

            btnSecurity.Visible = course.IsAuthorized( Rock.Security.Authorization.ADMINISTRATE, CurrentPerson );
            btnSecurity.Title = course.Name;
            btnSecurity.EntityId = course.Id;

        }

        private void ShowEdit()
        {
            var course = GetCourse();
            pnlEdit.Visible = true;
            pnlView.Visible = false;
            tbName.Text = course.Name;
            tbIconCssClass.Text = course.IconCssClass;
            uImage.BinaryFileId = course.ImageId;
            cbIsActive.Checked = course.IsActive;
            tbDescription.Text = course.Description;
            pCategory.SetValue( course.CategoryId );
            tbExternalUrl.Text = course.ExternalCourseUrl;
            tbSlug.Text = course.Slug;

            if ( course.AllowedGroupId.HasValue )
            {
                ddlViewSecurity.SelectedValue = "Group";
                pAllowedGroup.Visible = true;
                pAllowedGroup.GroupId = course.AllowedGroupId;
            }
            else if ( course.AllowedDataViewId.HasValue )
            {
                ddlViewSecurity.SelectedValue = "DataView";
                pAllowedDataView.Visible = true;
                pAllowedDataView.SetValue( course.AllowedDataViewId );
            }

        }
        #endregion

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
            var course = GetCourse( courseService );
            if ( course != null && slug == course.Slug )
            {
                //The slug already belongs to the course
                nbSlug.Visible = false;
                return true;
            }

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

        protected void tbSlug_TextChanged( object sender, EventArgs e )
        {
            CheckSlug();
        }

        protected void ddlViewSecurity_SelectedIndexChanged( object sender, EventArgs e )
        {
            switch ( ddlViewSecurity.SelectedValue )
            {
                case "Group":
                    pAllowedGroup.Visible = true;
                    pAllowedDataView.Visible = false;
                    break;
                case "DataView":
                    pAllowedGroup.Visible = false;
                    pAllowedDataView.Visible = true;
                    break;
                default:
                    pAllowedGroup.Visible = false;
                    pAllowedDataView.Visible = false;
                    break;
            }
        }
    }
}