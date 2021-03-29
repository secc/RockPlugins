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
using org.secc.Rise.Helpers;
using org.secc.Rise.Model;
using org.secc.Rise.Utilities;
using Rock;
using Rock.Attribute;
using Rock.Lava;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.org_secc.Rise
{
    [DisplayName( "Course Lava" )]
    [Category( "SECC > Rise" )]
    [Description( "Lava display for course information" )]

    [CategoryField( "Categories",
        AllowMultiple = true,
        Description = "Selecting categories will limit courses displayed.",
        EntityTypeName = "org.secc.Rise.Model.Course",
        IsRequired = false,
        Order = 0,
        Key = AttributeKeys.Categories )]

    [CodeEditorField( "Lava",
        Description = "Lava to be rendered",
        EditorMode = Rock.Web.UI.Controls.CodeEditorMode.Lava,
        Order = 1,
        Key = AttributeKeys.Lava )]

    [CodeEditorField( "Default Lava",
        Description = "Lava to render when the user does not yet have a Rise id",
        EditorMode = Rock.Web.UI.Controls.CodeEditorMode.Lava,
        Order = 2,
        IsRequired = false,
        Key = AttributeKeys.Default )]

    [LavaCommandsField( "Enabled Commands",
        Description = "Enabled commands for above lava,",
        Order = 3,
        IsRequired = false,
        Key = AttributeKeys.EnabledCommands )]

    public partial class CourseLava : RockBlock
    {
        #region AttributKeys

        private static class AttributeKeys
        {
            internal const string Categories = "Categories";
            internal const string Lava = "Lava";
            internal const string Default = "Default";
            internal const string EnabledCommands = "EnabledCommands";
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
            CurrentPerson.LoadAttributes();

            if ( CurrentPerson.GetAttributeValue( Constants.PERSON_ATTRIBUTE_KEY_RISEID ).IsNullOrWhiteSpace()
                && GetAttributeValue( AttributeKeys.Default ).IsNotNullOrWhiteSpace() )
            {
                ShowDefault();
            }
            else
            {
                ShowCourses();
            }

        }

        private void ShowDefault()
        {
            var mergeFields = LavaHelper.GetCommonMergeFields( RockPage, CurrentPerson );
            ltContent.Text = GetAttributeValue( AttributeKeys.Default )
                .ResolveMergeFields( mergeFields, CurrentPerson, GetAttributeValue( AttributeKeys.EnabledCommands ) );
        }

        private void ShowCourses()
        {
            var categoryGuids = GetAttributeValues( AttributeKeys.Categories );
            var categories = new List<CategoryCache>();
            foreach ( var guid in categoryGuids )
            {
                var category = CategoryCache.Get( guid );
                if ( category != null )
                {
                    categories.Add( category );
                }
            }

            List<CourseResult> courses = EnrollmentHelper.GetPersonCourses( CurrentPerson, categories )
                .Where( c => c.Course.IsArchived != true )
                .ToList();

            var categoryCourses = new List<CategoryCourseResults>();
            foreach ( var category in categories )
            {
                var categoryCourse = new CategoryCourseResults
                {
                    Category = category,
                    CourseResults = courses.Where( c => c.CategoryIds.Contains( category.Id ) ).ToList()
                };
                categoryCourses.Add( categoryCourse );
            }

            categoryCourses = categoryCourses.OrderBy( c => c.Category.Order ).ToList();

            var mergeFields = LavaHelper.GetCommonMergeFields( RockPage, CurrentPerson );
            mergeFields.Add( "Courses", courses );
            mergeFields.Add( "CategoryCourseResults", categoryCourses );

            ltContent.Text = GetAttributeValue( AttributeKeys.Lava )
                .ResolveMergeFields( mergeFields, CurrentPerson, GetAttributeValue( AttributeKeys.EnabledCommands ) );
        }

        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            ShowDetails();
        }

        #endregion
    }
}