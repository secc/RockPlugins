using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
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
    [ExportMetadata( "ComponentName", "Course Requirement Status" )]
    [Export( typeof( DataFilterComponent ) )]
    [Description( "Filter people based on their course requirment status." )]
    public class CourseRequirementStatus : DataFilterComponent
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
            return "Course Requirement Status";
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
  return  $content.find('select[id*=_filterBy] option:selected').text() + ': ' + $content.find('select[id*=_courseRequirement] option:selected').text();
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
            string result = "Course Requirement Status";
            string[] selectionValues = selection.Split( '|' );
            if ( selectionValues.Length >= 2 )
            {
                RockContext rockContext = new RockContext();
                var courseRequirementId = selectionValues[1].AsInteger();
                CourseRequirementService courseRequirementService = new CourseRequirementService( rockContext );
                var courseName = courseRequirementService.Queryable( "Group,DataView" )
                    .Where( r => r.Id == courseRequirementId )
                    .Select( r => r.Course.Name + " - " + ( r.GroupId.HasValue ? r.Group.Name : "" ) + ( r.DataViewId.HasValue ? r.DataView.Name : "" ) )
                    .FirstOrDefault();


                switch ( selectionValues[0] )
                {
                    case "0": //Has Course Requirement
                        result = "Has Course Requirement: " + courseName;
                        break;
                    case "1": //Has Complete Course Requrirement
                        result = "Has Complete Course Requirement: " + courseName;
                        break;
                    case "2": //Has Incomplete or Expired Course Requrirement
                        result = "Has Incomplete or Expired Course Requirement: " + courseName;
                        break;
                    case "3": //Has Incomplete Course Requirement
                        result = "Has Incomplete Course Requirement: " + courseName;
                        break;
                    case "4": //Has Expired Course Requirement
                        result = "Has Expired Course Requirement: " + courseName;
                        break;
                    default:
                        break;
                }
            }

            return result;
        }

        private RockDropDownList _ddlFilterBy = null;

        private RockDropDownList _ddlCourseRequirement = null;

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
            _ddlFilterBy.Items.Add( new ListItem( "Has Course Requirement", "0" ) );
            _ddlFilterBy.Items.Add( new ListItem( "Has Complete Course Requirement", "1" ) );
            _ddlFilterBy.Items.Add( new ListItem( "Has Incomplete or Expired Course Requirement", "2" ) );
            _ddlFilterBy.Items.Add( new ListItem( "Has Incomplete Course Requirement", "3" ) );
            _ddlFilterBy.Items.Add( new ListItem( "Has Expired Course Requirement", "4" ) );
            filterControl.Controls.Add( _ddlFilterBy );

            _ddlCourseRequirement = new RockDropDownList();
            _ddlCourseRequirement.ID = filterControl.ID + "_courseRequirement";
            _ddlCourseRequirement.Label = "Course Requirement";
            _ddlCourseRequirement.Required = true;
            _ddlCourseRequirement.DataTextField = "Value";
            _ddlCourseRequirement.DataValueField = "Key";
            filterControl.Controls.Add( _ddlCourseRequirement );

            RockContext rockContext = new RockContext();
            CourseRequirementService courseRequirementService = new CourseRequirementService( rockContext );
            var courseRequirements = courseRequirementService.Queryable( "Group,DataView" ).ToList()
                .Where( cr => cr.IsAuthorized( Rock.Security.Authorization.VIEW, filterControl.RockBlock().CurrentPerson ) )
                .ToDictionary( k => k.Id.ToString(), v => v.Course.Name + " - " + ( v.GroupId.HasValue ? v.Group.Name : "" ) + ( v.DataViewId.HasValue ? v.DataView.Name : "" ) );

            _ddlCourseRequirement.DataSource = courseRequirements;
            _ddlCourseRequirement.DataBind();

            return new Control[2] { _ddlFilterBy, _ddlCourseRequirement };
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
            return ( controls[0] as RockDropDownList ).SelectedValue + "|" + ( controls[1] as RockDropDownList ).SelectedValue;
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

                RockDropDownList ddlCourseRequirement = controls[1] as RockDropDownList;
                ddlCourseRequirement.SetValue( selectionValues[1] );
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
                var courseRequirementId = selectionValues[1].AsInteger();

                CourseRequirementStatusService courseRequirementStatusService = new CourseRequirementStatusService( ( RockContext ) serviceInstance.Context );

                var statusQry = courseRequirementStatusService.Queryable();
                var qry = new PersonService( ( RockContext ) serviceInstance.Context ).Queryable();


                switch ( selectionValues[0] )
                {
                    case "0": //Has Course Requirement
                        statusQry = statusQry.Where( s => s.CourseRequirementId == courseRequirementId );
                        qry = qry.Where( p => statusQry.Select( r => r.PersonAlias.PersonId ).Contains( p.Id ) );
                        break;
                    case "1": //Has Complete Course Requirement
                        statusQry = statusQry.Where( s => s.CourseRequirementId == courseRequirementId
                            && s.IsComplete
                            && ( !s.ValidUntil.HasValue || s.ValidUntil >= Rock.RockDateTime.Today ) );
                        qry = qry.Where( p => statusQry.Select( r => r.PersonAlias.PersonId ).Contains( p.Id ) );
                        break;
                    case "2": //Has Incomplete or Expired Course Requirement
                        statusQry = statusQry.Where( s => s.CourseRequirementId == courseRequirementId
                        && !( s.IsComplete && ( !s.ValidUntil.HasValue || s.ValidUntil >= Rock.RockDateTime.Today ) ) );
                        qry = qry.Where( p => statusQry.Select( r => r.PersonAlias.PersonId ).Contains( p.Id ) );
                        break;
                    case "3": //Has Incomplete Course Requirement
                        statusQry = statusQry.Where( s => s.CourseRequirementId == courseRequirementId && !s.IsComplete );
                        qry = qry.Where( p => statusQry.Select( r => r.PersonAlias.PersonId ).Contains( p.Id ) );
                        break;
                    case "4": //Has Expired Course Requirement
                        statusQry = statusQry.Where( s => s.CourseRequirementId == courseRequirementId
                        && s.IsComplete
                        && s.ValidUntil.HasValue
                        && s.ValidUntil < Rock.RockDateTime.Today );
                        qry = qry.Where( p => statusQry.Select( r => r.PersonAlias.PersonId ).Contains( p.Id ) );
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