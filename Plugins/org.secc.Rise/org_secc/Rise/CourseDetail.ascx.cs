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
using System.Threading.Tasks;
using System.Web.UI;
using org.secc.Rise;
using org.secc.Rise.Model;
using org.secc.Rise.Utilities;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Plugins.org_secc.Rise
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Course Detail" )]
    [Category( "SECC > Rise" )]
    [Description( "Details for Rise Course" )]

    public partial class CourseDetail : RockBlock
    {
        #region PageParameterKeys

        private static class PageParameterKey
        {
            public const string COURSE_ID = "CourseId";
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

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            var course = GetCourse();
            if ( course != null )
            {
                course.LoadAttributes();
                Rock.Attribute.Helper.AddEditControls( course, phAttributes, false );
            }

            var groupTypeId = Constants.GetRiseGroupTypeId();
            pGroup.IncludedGroupTypeIds = new List<int> { groupTypeId };

            gGroups.Actions.ShowAdd = true;
            gGroups.Actions.AddClick += gGroups_AddClick;
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
                ShowDetails();
            }
        }

        private void ShowDetails()
        {
            Course course = GetCourse();
            if ( course == null )
            {
                return;
            }

            ltName.Text = course.Name;
            ltUrl.Text = course.Url;
            cbLibrary.Checked = course.AvailableToAll ?? false;

            pCategories.SetValues( course.Categories );

            course.LoadAttributes();
            phAttributes.Controls.Clear();
            Rock.Attribute.Helper.AddEditControls( course, phAttributes, true );

            BindGrid();
        }

        private void BindGrid()
        {
            var course = GetCourse();
            var groups = course.EnrolledGroups.ToList();
            gGroups.DataSource = groups.ToList();
            gGroups.DataBind();
        }

        private Course GetCourse()
        {

            return GetCourse( new CourseService( new RockContext() ) );
        }

        private Course GetCourse( CourseService courseService )
        {
            //normally this is more complicated
            //but we don't make courses so this is all that's here
            return courseService.Get( PageParameter( PageParameterKey.COURSE_ID ).AsInteger() );
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

        // helper functional methods (like BindGrid(), etc.)

        #endregion

        protected void btnSave_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            CourseService courseService = new CourseService( rockContext );

            var course = GetCourse( courseService );

            if ( course == null )
            {
                NavigateToParentPage();
            }

            course.LoadAttributes();
            Rock.Attribute.Helper.GetEditValues( phAttributes, course );
            course.AvailableToAll = cbLibrary.Checked;

            var toRemove = course.Categories.Where( c => !pCategories.SelectedValuesAsInt().Contains( c.Id ) ).ToList();
            foreach ( var item in toRemove )
            {
                course.Categories.Remove( item );
            }

            var categoryIds = pCategories.SelectedValuesAsInt().ToList();
            var currentIds = course.Categories.Select( ca => ca.Id ).ToList();
            var toAdd = new CategoryService( rockContext )
                .GetByIds( categoryIds )
                .Where( c => !currentIds.Contains( c.Id ) )
                .ToList();
            foreach ( var item in toAdd )
            {
                course.Categories.Add( item );
            }

            rockContext.SaveChanges();
            course.SaveAttributeValues();

            NavigateToParentPage();
        }

        protected void btnCancel_Click( object sender, EventArgs e )
        {
            NavigateToParentPage();
        }

        private void gGroups_AddClick( object sender, EventArgs e )
        {
            pGroup.SetValue( null );
            mdEnrollGroup.Show();
        }


        protected void btnGroupDelete_Click( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            var groupId = e.RowKeyId;

            RockContext rockContext = new RockContext();
            CourseService courseService = new CourseService( rockContext );
            GroupService groupService = new GroupService( rockContext );

            var course = GetCourse( courseService );
            var group = groupService.Get( groupId );

            if ( course.EnrolledGroups.Select( g => g.Id ).Contains( groupId ) )
            {
                course.EnrolledGroups.Remove( course.EnrolledGroups.Where( g => g.Id == groupId ).FirstOrDefault() );
                rockContext.SaveChanges();
            }

            //The async code in the client doesn't play nice with Web Forms ¯\_(ツ)_/¯
            Task.Run( () =>
            {
                var riseClient = new RiseClient();
                riseClient.Unenroll( course, group );
            } );

            BindGrid();
        }

        protected void mdEnrollGroup_SaveClick( object sender, EventArgs e )
        {
            var groupId = pGroup.SelectedValueAsId();
            if ( !groupId.HasValue )
            {
                return;
            }

            RockContext rockContext = new RockContext();
            CourseService courseService = new CourseService( rockContext );
            GroupService groupService = new GroupService( rockContext );

            var course = GetCourse( courseService );
            var group = groupService.Get( groupId.Value );

            if ( !course.EnrolledGroups.Select( g => g.Id ).Contains( groupId.Value ) )
            {
                course.EnrolledGroups.Add( group );
                rockContext.SaveChanges();
            }

            //The async code in the client doesn't play nice with Web Forms ¯\_(ツ)_/¯
            Task.Run( () =>
            {
                var riseClient = new RiseClient();
                riseClient.Enroll( course, group );
            } );

            mdEnrollGroup.Hide();
            BindGrid();

        }
    }
}