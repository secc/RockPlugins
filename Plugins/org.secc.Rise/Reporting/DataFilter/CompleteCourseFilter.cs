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
//
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Web.UI;
using org.secc.Rise.Model;
using org.secc.xAPI.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Reporting;
using Rock.Web.UI.Controls;

namespace org.secc.Rise.Reporting.DataFilter
{
    /// <summary>
    /// 
    /// </summary>
    [Description( "Filter people by comleted course." )]
    [Export( typeof( DataFilterComponent ) )]
    [ExportMetadata( "ComponentName", "Completed Course" )]
    public class CompletedCoursesFilter : DataFilterComponent
    {
        #region Properties

        /// <summary>
        /// Gets the entity type that filter applies to.
        /// </summary>
        /// <value>
        /// The entity that filter applies to.
        /// </value>
        public override string AppliesToEntityType
        {
            get { return typeof( Rock.Model.Person ).FullName; }
        }

        /// <summary>
        /// Gets the section.
        /// </summary>
        /// <value>
        /// The section.
        /// </value>
        public override string Section
        {
            get { return "Learning Management"; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        /// <value>
        /// The title.
        /// </value>
        public override string GetTitle( Type entityType )
        {
            return "Completed Course";
        }

        /// <summary>
        /// Formats the selection on the client-side.  When the filter is collapsed by the user, the Filterfield control
        /// will set the description of the filter to whatever is returned by this property.  If including script, the
        /// controls parent container can be referenced through a '$content' variable that is set by the control before 
        /// referencing this property.
        /// </summary>
        /// <value>
        /// The client format script.
        /// </value>
        public override string GetClientFormatSelection( Type entityType )
        {
            return $@"
function() {{
    var courseName = $('#{ddlCourses.ClientID} option:selected').text();
    return 'Completed ' + courseName;
}}";
        }

        /// <summary>
        /// Formats the selection.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override string FormatSelection( Type entityType, string selection )
        {
            CourseService courseService = new CourseService( new RockContext() );
            var course = courseService.Get( selection.AsInteger() );
            string result = $"Completed {course.Name}";
            return result;
        }

        private RockDropDownList ddlCourses;

        /// <summary>
        /// Creates the child controls.
        /// </summary>
        /// <returns></returns>
        public override Control[] CreateChildControls( Type entityType, FilterField filterControl )
        {
            ddlCourses = new RockDropDownList
            {
                ID = filterControl.ID + "_ddlCourses",
                Label = "Course",
                DataTextField = "Name",
                DataValueField = "Id",
                Required = true
            };
            filterControl.Controls.Add( ddlCourses );

            RockContext rockContext = new RockContext();
            CourseService courseService = new CourseService( rockContext );
            var courses = courseService.Queryable().OrderBy( c => c.Name ).ToList();
            ddlCourses.DataSource = courses;
            ddlCourses.DataBind();

            return new System.Web.UI.Control[1] { ddlCourses };
        }

        /// <summary>
        /// Renders the controls.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="filterControl">The filter control.</param>
        /// <param name="writer">The writer.</param>
        /// <param name="controls">The controls.</param>
        public override void RenderControls( Type entityType, FilterField filterControl, HtmlTextWriter writer, Control[] controls )
        {
            base.RenderControls( entityType, filterControl, writer, controls );
        }

        /// <summary>
        /// Gets the selection.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="controls">The controls.</param>
        /// <returns></returns>
        public override string GetSelection( Type entityType, Control[] controls )
        {
            if ( controls.Count() >= 1 )
            {
                var ddlCourses = controls[0] as RockDropDownList;
                if ( ddlCourses != null )
                {
                    return ddlCourses.SelectedValue;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Sets the selection.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="controls">The controls.</param>
        /// <param name="selection">The selection.</param>
        public override void SetSelection( Type entityType, Control[] controls, string selection )
        {
            if ( controls.Count() >= 0 )
            {
                var ddlCourses = controls[0] as RockDropDownList;
                if ( ddlCourses != null )
                {
                    ddlCourses.SelectedValue = selection;
                }
            }
        }

        public override Expression GetExpression( Type entityType, IService serviceInstance, ParameterExpression parameterExpression, string selection )
        {
            if ( selection.AsIntegerOrNull().HasValue )
            {
                var rockContext = ( RockContext ) serviceInstance.Context;
                CourseService courseService = new CourseService( rockContext );
                ExperienceService experienceService = new ExperienceService( rockContext );

                var course = courseService.Get( selection.AsInteger() );

                var xObject = course.GetExperienceObject( rockContext );
                var experiences = experienceService.Queryable()
                    .Where( x => x.xObjectId == xObject.Id && x.Result.WasSuccess == true && x.Result.IsComplete == true )
                    .Select( x => x.PersonAlias );

                var qry = new PersonService( rockContext ).Queryable()
                   .Where( p => experiences.Any( xx => xx.PersonId == p.Id ) );

                var expression = FilterExpressionExtractor.Extract<Person>( qry, parameterExpression, "p" );
                return expression;
            }
            return null;
        }
        #endregion
    }
}