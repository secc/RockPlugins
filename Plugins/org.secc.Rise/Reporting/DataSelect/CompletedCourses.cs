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
using System.Web.UI.WebControls;
using org.secc.Rise.Model;
using org.secc.Rise.Utilities;
using org.secc.xAPI.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Reporting;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace org.secc.Rise.Reporting.DataSelect
{
    /// <summary>
    /// 
    /// </summary>
    [Description( "Select courses that have been completed" )]
    [Export( typeof( DataSelectComponent ) )]
    [ExportMetadata( "ComponentName", "Select courses that have been completed" )]
    public class CompletedCourses : DataSelectComponent
    {
        #region Properties

        /// <summary>
        /// Gets the name of the entity type. Filter should be an empty string
        /// if it applies to all entities
        /// </summary>
        /// <value>
        /// The name of the entity type.
        /// </value>
        public override string AppliesToEntityType
        {
            get
            {
                return typeof( Rock.Model.Person ).FullName;
            }
        }

        /// <summary>
        /// The PropertyName of the property in the anonymous class returned by the SelectExpression
        /// </summary>
        /// <value>
        /// The name of the column property.
        /// </value>
        public override string ColumnPropertyName
        {
            get
            {
                return "Completed Courses";
            }
        }

        /// <summary>
        /// Gets the type of the column field.
        /// </summary>
        /// <value>
        /// The type of the column field.
        /// </value>
        public override Type ColumnFieldType
        {
            get { return typeof( string ); }
        }

        /// <summary>
        /// Gets the grid field.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override DataControlField GetGridField( Type entityType, string selection )
        {
            BoundField result = new BoundField();
            result.HtmlEncode = false;

            return result;
        }

        /// <summary>
        /// Gets the default column header text.
        /// </summary>
        /// <value>
        /// The default column header text.
        /// </value>
        public override string ColumnHeaderText
        {
            get
            {
                return "Completed Courses";
            }
        }

        #endregion

        #region Methods

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
            return "Completed Courses";
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="entityIdProperty">The entity identifier property.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override Expression GetExpression( RockContext rockContext, MemberExpression entityIdProperty, string selection )
        {
            var categoryIds = selection.SplitDelimitedValues( false ).Select( i => i.AsInteger() ).ToList();

            CourseService courseService = new CourseService( rockContext );
            ExperienceService experienceService = new ExperienceService( rockContext );

            var courses = courseService.Queryable();
            if ( categoryIds.Any() )
            {
                courses = courses.Where( c => categoryIds.Any( i => c.Categories.Select( ca => ca.Id ).Contains( i ) ) );
            }

            var xObject = course.GetExperienceObject( rockContext );
            var experiences = experienceService.Queryable()
                .Where( x => x.xObjectId == xObject.Id && x.Result.WasSuccess == true && x.Result.IsComplete == true )
                .Select( x => x.PersonAlias );

            var qry = new PersonService( rockContext ).Queryable()
               .Where( p => experiences.Any( xx => xx.PersonId == p.Id ) );

            var expression = SelectExpressionExtractor.Extract( qry, entityIdProperty, "p" );


            var qryPersonNotes = new PersonService( rockContext ).Queryable().Select( p => qryNotes.Where( xx => xx.Id == qryNotes.Where( a => a.EntityId == p.Id ).Max( x => x.Id ) ).Select( s => s.Text ).FirstOrDefault() );

            var selectNoteExpression = SelectExpressionExtractor.Extract( qryPersonNotes, entityIdProperty, "p" );

            return selectNoteExpression;
        }

        /// <summary>
        /// Creates the child controls.
        /// </summary>
        /// <param name="parentControl"></param>
        /// <returns></returns>
        public override System.Web.UI.Control[] CreateChildControls( System.Web.UI.Control parentControl )
        {
            EntityTypeCache courseEntityType = EntityTypeCache.Get( typeof( Course ) );

            CategoryPicker categoryPicker = new CategoryPicker
            {
                EntityTypeId = courseEntityType.Id,
                Label = "Course Categories",
                AllowMultiSelect = true
            };
            parentControl.Controls.Add( categoryPicker );

            return new System.Web.UI.Control[] { categoryPicker };
        }

        /// <summary>
        /// Renders the controls.
        /// </summary>  
        /// <param name="parentControl">The parent control.</param>
        /// <param name="writer">The writer.</param>
        /// <param name="controls">The controls.</param>
        public override void RenderControls( System.Web.UI.Control parentControl, System.Web.UI.HtmlTextWriter writer, System.Web.UI.Control[] controls )
        {
            base.RenderControls( parentControl, writer, controls );
        }

        /// <summary>
        /// Gets the selection.
        /// </summary>
        /// <param name="controls">The controls.</param>
        /// <returns></returns>
        public override string GetSelection( System.Web.UI.Control[] controls )
        {
            if ( controls.Count() >= 0 )
            {
                CategoryPicker categoryPicker = controls[0] as CategoryPicker;
                return string.Join( ",", categoryPicker.SelectedValues );
            }

            return string.Empty;
        }

        /// <summary>
        /// Sets the selection.
        /// </summary>
        /// <param name="controls">The controls.</param>
        /// <param name="selection">The selection.</param>
        public override void SetSelection( System.Web.UI.Control[] controls, string selection )
        {
            if ( controls.Count() >= 0 )
            {
                var sel = selection.SplitDelimitedValues( false ).Select( i => i.AsInteger() );
                CategoryPicker categoryPicker = controls[0] as CategoryPicker;
                categoryPicker.SetValues( sel );
            }
        }

        #endregion
    }
}
