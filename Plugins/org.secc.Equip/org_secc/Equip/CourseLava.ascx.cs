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
using System.Text;
using Rock.Lava;
using org.secc.Equip.Helpers;

namespace RockWeb.Plugins.org.secc.Equip
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Course Lava" )]
    [Category( "SECC > Equip" )]
    [Description( "Block for displaying courses using lava." )]

    [CodeEditorField(
        "Lava Template",
        Description = "Template for output",
        Key = AttributeKeys.LavaTemplate,
        EditorMode = Rock.Web.UI.Controls.CodeEditorMode.Lava
        )]

    [CategoryField(
        "Categories",
        AllowMultiple = true,
        Description = "Categories of lessons to display.",
        EntityTypeName = "org.secc.Equip.Model.Course"
        )]

    public partial class CourseLava : Rock.Web.UI.RockBlock
    {


        protected static class AttributeKeys
        {
            internal const string LavaTemplate = "LavaTemplate";
            internal const string OnlyRequired = "OnlyRequired";
            internal const string Categories = "Categories";
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
                var categoryGuids = GetAttributeValue( AttributeKeys.Categories )
                    .SplitDelimitedValues()
                    .Select( i => i.AsGuid() )
                    .ToList();

                RockContext rockContext = new RockContext();


                CategoryService categoryService = new CategoryService( rockContext );
                var categoryIds = categoryService.Queryable()
                    .Where( c => categoryGuids.Contains( c.Guid ) )
                    .Select( c => c.Id )
                    .ToList();

                CourseRequirementStatusService courseRequirementStatusService = new CourseRequirementStatusService( rockContext );
                var statusQry = courseRequirementStatusService.Queryable()
                    .Where( s => s.PersonAlias.PersonId == CurrentPersonId );

                CourseRecordService courseRecordService = new CourseRecordService( rockContext );
                var recordQueryable = courseRecordService.Queryable()
                    .GroupJoin(statusQry,
                    r => r.CourseId,
                    s => s.CourseRequirement.CourseId,
                    (r,s) => new { Record = r, Statuses = s }
                    )
                    .Where( r => r.Record.PersonAlias.PersonId == CurrentPersonId )
                    .OrderByDescending( r => r.Record.CompletionDateTime );

                CourseService courseService = new CourseService( rockContext );

                var courses = courseService.Queryable()
                    .Where( c => c.IsActive )
                    .Where( c => categoryIds.Contains( c.CategoryId ?? 0 ) )
                    .GroupJoin( recordQueryable,
                    c => c.Id,
                    r => r.Record.CourseId,
                    ( c, r ) => new
                    {
                        Course = c,
                        Records = r,
                        Category = c.Category
                    } )
                    .ToList();

                var courseItems = new List<PersonCourseInfo>();

                foreach ( var course in courses )
                {
                    if ( !course.Course.PersonCanView( CurrentPerson ) )
                    {
                        continue;
                    }

                    var courseItem = new PersonCourseInfo()
                    {
                        Course = course.Course,
                        Category = course.Category,
                        IsComplete = false
                    };

                    var completedRecords = course.Records.Where( r => r.Record.Passed ).ToList();
                    if ( completedRecords.Any() )
                    {
                        var completedCourse = completedRecords.First();
                        courseItem.IsComplete = true;
                        courseItem.CompletedDateTime = completedCourse.Record.CompletionDateTime;
                        var expired = completedCourse.Statuses
                            .Where( s => s.State == CourseRequirementState.Expired ).Any();
                        if ( expired )
                        {
                            courseItem.IsExpired = true;
                        }
                    }
                    courseItems.Add( courseItem );
                }


                var mergeFields = LavaHelper.GetCommonMergeFields( RockPage, CurrentPerson );
                mergeFields.Add( "CourseItems", courseItems );
                ltOutput.Text = GetAttributeValue( AttributeKeys.LavaTemplate ).ResolveMergeFields( mergeFields );


            }
        }
        #endregion
    }
}