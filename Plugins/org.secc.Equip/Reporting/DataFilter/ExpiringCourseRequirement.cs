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
    [ExportMetadata( "ComponentName", "Expiring Course Requirement" )]
    [Export( typeof( DataFilterComponent ) )]
    [Description( "Filter people based on whose course requirement completion is about to expire." )]
    public class ExpiringCourseRequirement : DataFilterComponent
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
            return "Expiring Course Requirement";
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
var result =    $content.find('select[id*=_courseRequirement] option:selected').text() +
                ' expiring in the next '+
                $content.find('input[id*=_tbDays]').val() +
                ' days ';

                if ($content.find('input[id*=_cbIncludeExpired]').prop('checked') ){
                    result += 'including expired'
                } else {
                    result += 'not including expired'
                }
                return result;
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
            string result = "Expiring Course Requirement";
            string[] selectionValues = selection.Split( '|' );
            if ( selectionValues.Length >= 3 )
            {
                RockContext rockContext = new RockContext();
                var courseRequirementId = selectionValues[0].AsInteger();
                CourseRequirementService courseRequirementService = new CourseRequirementService( rockContext );
                var courseName = courseRequirementService.Queryable( "Group,DataView" )
                    .Where( r => r.Id == courseRequirementId )
                    .Select( r => r.Course.Name + " - " + ( r.GroupId.HasValue ? r.Group.Name : "" ) + ( r.DataViewId.HasValue ? r.DataView.Name : "" ) )
                    .FirstOrDefault();

                result = string.Format( "{0} expiring in the next {1} days {2}",
                    courseName,
                    selectionValues[1],
                    selectionValues[2].AsBoolean() ? "including expired" : "not including expired" );
            }

            return result;
        }
        private RockDropDownList _ddlCourseRequirement = null;

        private RockTextBox _tbDays = null;

        private RockCheckBox _cbIncludeExpired = null;

        /// <summary>
        /// Creates the child controls.
        /// </summary>
        /// <returns></returns>
        public override Control[] CreateChildControls( Type entityType, FilterField filterControl )
        {
            _ddlCourseRequirement = new RockDropDownList();
            _ddlCourseRequirement.ID = filterControl.ID + "_courseRequirement";
            _ddlCourseRequirement.Label = "Course";
            _ddlCourseRequirement.Required = true;
            _ddlCourseRequirement.DataTextField = "Value";
            _ddlCourseRequirement.DataValueField = "Key";
            filterControl.Controls.Add( _ddlCourseRequirement );

            RockContext rockContext = new RockContext();
            CourseRequirementService courseRequirementService = new CourseRequirementService( rockContext );
            var courseRequirements = courseRequirementService.Queryable( "Group,DataView" ).ToList()
                .Where( cr => cr.IsAuthorized( Rock.Security.Authorization.VIEW, filterControl.RockBlock().CurrentPerson ) )
                .ToDictionary( k => k.Id.ToString(),
                    v => v.Course.Name + " - " + ( v.GroupId.HasValue ? v.Group.Name : "" ) + ( v.DataViewId.HasValue ? v.DataView.Name : "" ) );

            _ddlCourseRequirement.DataSource = courseRequirements;
            _ddlCourseRequirement.DataBind();

            _tbDays = new RockTextBox();
            _tbDays.Label = "Days Until Expiration";
            _tbDays.ID = filterControl.ID + "_tbDays";
            _tbDays.Required = true;
            filterControl.Controls.Add( _tbDays );

            _cbIncludeExpired = new RockCheckBox();
            _cbIncludeExpired.ID = filterControl.ID + "_cbIncludeExpired";
            _cbIncludeExpired.Text = "Include Expired";
            filterControl.Controls.Add( _cbIncludeExpired );

            return new Control[] { _ddlCourseRequirement, _tbDays, _cbIncludeExpired };
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
            var courseRequirementId = ( controls[0] as RockDropDownList ).SelectedValue;
            var daysExpiring = ( controls[1] as RockTextBox ).Text.AsIntegerOrNull() ?? 30;
            var includeExpired = ( controls[2] as RockCheckBox ).Checked;

            return string.Format( "{0}|{1}|{2}", courseRequirementId, daysExpiring, includeExpired );
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

            if ( selectionValues.Length >= 3 )
            {
                RockDropDownList ddlCourseRequirement = controls[0] as RockDropDownList;
                ddlCourseRequirement.SetValue( selectionValues[0] );

                RockTextBox tbDays = controls[1] as RockTextBox;
                tbDays.Text = selectionValues[1];

                RockCheckBox cbIncludeExpired = controls[2] as RockCheckBox;
                cbIncludeExpired.Checked = selectionValues[2].AsBoolean();
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
            if ( selectionValues.Length >= 3 )
            {
                CourseRequirementStatusService courseRequirementStatusService = new CourseRequirementStatusService( ( RockContext ) serviceInstance.Context );

                var qry = new PersonService( ( RockContext ) serviceInstance.Context ).Queryable();

                var courseRequirementId = selectionValues[0].AsInteger();
                var startDate = RockDateTime.Today.AddDays( selectionValues[1].AsInteger() );
                var includeExpired = selectionValues[2].AsBoolean();

                var statusQry = courseRequirementStatusService.Queryable()
                    .Where(s =>
                        s.CourseRequirementId == courseRequirementId 
                        && s.IsComplete
                        && s.ValidUntil.HasValue && s.ValidUntil < startDate );

                if ( !includeExpired )
                {
                    statusQry = statusQry.Where( s => s.ValidUntil >= RockDateTime.Today );
                }

                qry = qry.Where( p => statusQry.Select( s => s.PersonAlias.PersonId ).Contains( p.Id ) );

                return FilterExpressionExtractor.Extract<Rock.Model.Person>( qry, parameterExpression, "p" );
            }

            return null;
        }

        #endregion
    }

}