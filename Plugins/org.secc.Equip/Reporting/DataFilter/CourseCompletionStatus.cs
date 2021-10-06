using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using DotLiquid.Util;
using org.secc.Equip.Controls;
using org.secc.Equip.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Reporting;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace org.secc.Equip.Reporting.DataFilter
{
    /// <summary>
    /// 
    /// </summary>
    [ExportMetadata( "ComponentName", "Course Completion Status" )]
    [Export( typeof( DataFilterComponent ) )]
    [Description( "Filter people based if they have completed a course." )]
    public class CourseCompletionStatus : DataFilterComponent
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
            get { return "Rock.Model.Person"; }
        }

        /// <summary>
        /// Gets the section.
        /// </summary>
        /// <value>
        /// The section.
        /// </value>
        public override string Section
        {
            get { return "Learning Tool"; }
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
            return "Course Completion Status";
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
            return @"
function() {
  return  $content.find('select[id*=_filterBy] option:selected').text() + ': ' + $content.find('span.selected-names').text();
}
";
        }

        /// <summary>
        /// Formats the selection.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override string FormatSelection( Type entityType, string selection )
        {
            string result = "Course Completion Status";
            string[] selectionValues = selection.Split( '|' );
            if ( selectionValues.Length >= 2 )
            {
                RockContext rockContext = new RockContext();
                var courseId = selectionValues[1].AsInteger();
                CourseService courseService = new CourseService( rockContext );
                var courseName = courseService.Get( courseId );

                switch ( selectionValues[0] )
                {
                    case "0": //Has Completed
                        result = "Has Completed: " + courseName;
                        break;
                    case "1": //Has Not Completed
                        result = "Has Not Completed: " + courseName;
                        break;
                    case "2": //Has Started Not Completed
                        result = "Has Started Not Completed: " + courseName;
                        break;
                    case "3": //Has Not Started
                        result = "Has Not Started: " + courseName;
                        break;
                    default:
                        break;
                }

            }

            return result;
        }

        private RockDropDownList _ddlFilterBy = null;

        private CoursePicker _pCourse = null;

        /// <summary>
        /// Creates the child controls.
        /// </summary>
        /// <returns></returns>
        public override Control[] CreateChildControls( Type entityType, FilterField filterControl )
        {
            _ddlFilterBy = new RockDropDownList();
            _ddlFilterBy.ID = filterControl.ID + "_filterBy";
            _ddlFilterBy.Label = "Filter By";
            _ddlFilterBy.Required = true;
            _ddlFilterBy.Items.Add( new ListItem( "Has Completed", "0" ) );
            _ddlFilterBy.Items.Add( new ListItem( "Has Not Completed", "1" ) );
            _ddlFilterBy.Items.Add( new ListItem( "Started But Not Completed", "2" ) );
            _ddlFilterBy.Items.Add( new ListItem( "Has Not Started", "3" ) );
            filterControl.Controls.Add( _ddlFilterBy );

            _pCourse = new CoursePicker();
            _pCourse.ID = filterControl.ID + "_course";
            _pCourse.Label = "Course";
            _pCourse.Required = true;
            filterControl.Controls.Add( _pCourse );

            return new Control[2] { _ddlFilterBy, _pCourse };
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
            return ( controls[0] as RockDropDownList ).SelectedValue + "|" + ( controls[1] as CoursePicker ).SelectedValue;
        }

        /// <summary>
        /// Sets the selection.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="controls">The controls.</param>
        /// <param name="selection">The selection.</param>
        public override void SetSelection( Type entityType, Control[] controls, string selection )
        {
            string[] selectionValues = selection.Split( '|' );

            if ( selectionValues.Length >= 2 )
            {
                RockDropDownList ddlFilterBy = controls[0] as RockDropDownList;
                ddlFilterBy.SelectedValue = selectionValues[0];

                CoursePicker coursePicker = controls[1] as CoursePicker;
                coursePicker.SetValue( selectionValues[1].AsInteger() );

            }
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="serviceInstance">The service instance.</param>
        /// <param name="parameterExpression">The parameter expression.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override Expression GetExpression( Type entityType, IService serviceInstance, ParameterExpression parameterExpression, string selection )
        {
            string[] selectionValues = selection.Split( '|' );
            if ( selectionValues.Length >= 2 )
            {
                var courseId = selectionValues[1].AsInteger();

                CourseRecordService courseRecordService = new CourseRecordService( ( RockContext ) serviceInstance.Context );
                var recordQry = courseRecordService.Queryable();
                var qry = new PersonService( ( RockContext ) serviceInstance.Context ).Queryable();


                switch ( selectionValues[0] )
                {
                    case "0": //Has Completed
                        recordQry = recordQry.Where( r => r.CourseId == courseId && r.CompletionDateTime != null );
                        qry = qry.Where( p => recordQry.Select( r => r.PersonAlias.PersonId ).Contains( p.Id ) );
                        break;
                    case "1": //Has Not Completed
                        recordQry = recordQry.Where( r => r.CourseId == courseId && r.CompletionDateTime != null );
                        qry = qry.Where( p => !recordQry.Select( r => r.PersonAlias.PersonId ).Contains( p.Id ) );
                        break;
                    case "2": //Has Started Not Completed
                        var started = recordQry.Where( r => r.CourseId == courseId && r.CompletionDateTime == null )
                            .Select( r => r.PersonAlias.PersonId );
                        var completed = recordQry.Where( r => r.CourseId == courseId && r.CompletionDateTime != null )
                            .Select( r => r.PersonAlias.PersonId );

                        qry = qry.Where( p => started.Contains( p.Id ) && !completed.Contains( p.Id ) );
                        break;
                    case "3": //Has Not Started
                        recordQry = recordQry.Where( r => r.CourseId == courseId );
                        qry = qry.Where( p => !recordQry.Select( r => r.PersonAlias.PersonId ).Contains( p.Id ) );
                        break;
                    default:
                        break;
                }

                return FilterExpressionExtractor.Extract<Rock.Model.Person>( qry, parameterExpression, "p" );
            }

            return null;
        }

        #endregion
    }

}