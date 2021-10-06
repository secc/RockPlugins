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
using org.secc.Equip.Helpers;
using System.Text;
using System.Web.UI.WebControls;

namespace RockWeb.Plugins.org.secc.Equip
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Course Requirement Detail" )]
    [Category( "SECC > Equip" )]
    [Description( "Block for managing a course requirement." )]


    public partial class CourseRequirementDetail : Rock.Web.UI.RockBlock
    {


        #region PageParameterKeys

        /// <summary>
        /// Keys to use for Page Parameters
        /// </summary>
        protected static class PageParameterKey
        {
            internal const string CourseRequirementId = "CourseRequirementId";
            internal const string CourseId = "CourseId";
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

            btnDelete.Attributes["onclick"] = string.Format( "javascript: return Rock.dialogs.confirmDelete(event, '{0}');", CourseRequirement.FriendlyTypeName );

            gStatuses.GridRebind += GStatuses_GridRebind;
            gStatuses.Actions.ShowCommunicate = true;
            gStatuses.Actions.CommunicateClick += Actions_CommunicateClick;
        }

        private void Actions_CommunicateClick( object sender, EventArgs e )
        {
            List<int> keys = null;

            if ( gStatuses.SelectedKeys.Count == 0 )
            {
                keys = new List<int>();
                foreach ( DataKey key in gStatuses.DataKeys )
                {
                    keys.Add( ( int ) key.Value );
                }
            }
            else
            {
                keys = gStatuses.SelectedKeys.Select( o => ( int ) o ).ToList();
            }

            RockContext rockContext = new RockContext();
            CourseRequirementStatusService courseRequirementStatusService = new CourseRequirementStatusService( rockContext );
            var people = courseRequirementStatusService.GetByIds( keys ).Select( s => s.PersonAlias.Person ).ToList();
            var communication = new Communication();
            communication.Status = CommunicationStatus.Transient;
            foreach ( var person in people )
            {
                communication.Recipients.Add( new CommunicationRecipient { PersonAlias = person.PrimaryAlias } );
            }


        }

        private void GStatuses_GridRebind( object sender, Rock.Web.UI.Controls.GridRebindEventArgs e )
        {
            ShowDetails();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            var courseRequirement = GetCourseRequirement();

            if ( !courseRequirement.IsAuthorized( Rock.Security.Authorization.VIEW, CurrentPerson ) )
            {
                return;
            }

            if ( !Page.IsPostBack )
            {

                pdAuditDetails.SetEntity( courseRequirement, "/" );
                cblState.BindToEnum<CourseRequirementState>();

                if ( courseRequirement.Id == 0 )
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

        private void ShowDetails()
        {
            var courseRequirement = GetCourseRequirement();
            pnlDetails.Visible = true;
            pnlEdit.Visible = false;

            if ( courseRequirement.Course != null )
            {

                ltCourseName.Text = courseRequirement.Course.Name;
                ltName.Text = courseRequirement.Course.Name;
            }


            if ( courseRequirement.DataViewId.HasValue )
            {
                ltSource.Text = courseRequirement.DataView.Name;
                ltSource.Label = "Data View";
            }
            else if ( courseRequirement.GroupId.HasValue )
            {
                ltSource.Text = courseRequirement.Group.Name;
                ltSource.Label = "Group";
            }
            else
            {
                ltSource.Visible = false;
            }

            if ( courseRequirement.DaysValid.HasValue )
            {
                ltDaysValid.Text = courseRequirement.DaysValid.Value.ToString();
            }
            else
            {
                ltDaysValid.Visible = false;
            }

            RockContext rockContext = new RockContext();
            CourseRequirementStatusService courseRequirementStatusService = new CourseRequirementStatusService( rockContext );
            var statuses = courseRequirementStatusService.Queryable( "PersonAlias" )
                .Where( crss => crss.CourseRequirementId == courseRequirement.Id )
                .ToList();

            var completeCount = statuses.Where( s => s.State == CourseRequirementState.Complete ).Count();
            var incompleteCount = statuses.Where( s => s.State == CourseRequirementState.Incomplete ).Count();
            var expiredCount = statuses.Where( s => s.State == CourseRequirementState.Expired ).Count();
            var totalCount = statuses.Count();

            var data = new List<Dictionary<string, object>>();

            data.Add( new Dictionary<string, object> {
                { "YValueTotal", completeCount },
                { "MetricTitle","Complete"} } );

            data.Add( new Dictionary<string, object> {
                { "YValueTotal", incompleteCount },
                { "MetricTitle","Incomplete"} } );

            if ( expiredCount > 0 )
            {
                data.Add( new Dictionary<string, object> {
                { "YValueTotal", expiredCount },
                { "MetricTitle","Expired"} } );
            }

            chart.ChartData = data.ToJson();
            chart.Width = 200;
            chart.Height = 200;

            var totals = new StringBuilder();
            totals.Append( string.Format( "<b>Complete:</b> {0}<br>", completeCount ) );
            totals.Append( string.Format( "<b>Incomplete:</b> {0}<br>", incompleteCount ) );
            if ( expiredCount > 0 )
            {
                totals.Append( string.Format( "<b>Expired:</b> {0}<br>", expiredCount ) );
            }
            totals.Append( string.Format( "<b>Total:</b> {0}<br>", totalCount ) );

            lTotals.Text = totals.ToString();

            if ( cblState.SelectedValues.Any() )
            {
                statuses = statuses
                    .Where( s => cblState.SelectedValuesAsInt.Contains( s.State.ConvertToInt() ) )
                    .ToList();
            }

            gStatuses.DataSource = statuses;
            gStatuses.DataBind();

            if ( !courseRequirement.IsAuthorized( Rock.Security.Authorization.EDIT, CurrentPerson ) )
            {
                pnlParticipants.Visible = false;
                btnEdit.Visible = false;

            }

        }

        private CourseRequirement GetCourseRequirement()
        {
            return GetCourseRequirement( new CourseRequirementService( new RockContext() ) );
        }

        private CourseRequirement GetCourseRequirement( CourseRequirementService courseRequirementService )
        {
            var chapterId = PageParameter( PageParameterKey.CourseRequirementId ).AsInteger();
            var courseRequirement = courseRequirementService.Get( chapterId );
            if ( courseRequirement == null )
            {
                courseRequirement = new CourseRequirement
                {
                    CourseId = PageParameter( PageParameterKey.CourseId ).AsInteger()
                };
            }
            return courseRequirement;
        }

        #endregion

        #region Events
        protected void btnSave_Click( object sender, EventArgs e )
        {
            var isNew = false;
            RockContext rockContext = new RockContext();
            CourseRequirementService courseRequirementService = new CourseRequirementService( rockContext );

            var courseRequirement = GetCourseRequirement( courseRequirementService );

            if ( courseRequirement.Id == 0 )
            {
                isNew = true;
                courseRequirementService.Add( courseRequirement );
            }

            courseRequirement.CourseId = pCourse.SelectedValueAsId() ?? 0;

            if ( ddlSelect.SelectedValue == "DATAVIEW" )
            {
                courseRequirement.DataViewId = pDataview.SelectedValueAsId();
                courseRequirement.GroupId = null;
            }
            else if ( ddlSelect.SelectedValue == "GROUP" )
            {
                courseRequirement.GroupId = pGroup.SelectedValueAsId();
                courseRequirement.DataViewId = null;
            }

            courseRequirement.DaysValid = tbDaysValid.Text.AsIntegerOrNull();

            rockContext.SaveChanges();
            ;

            CourseRequirementHelper.UpdateCourseRequirementStatuses( courseRequirement );

            if ( isNew )
            {
                NavigateToCurrentPage( new Dictionary<string, string> {
                    { PageParameterKey.CourseId, PageParameter( PageParameterKey.CourseId ) },
                    { PageParameterKey.CourseRequirementId, courseRequirement.Id.ToString()},
                } );
            }
            else
            {
                ShowDetails();
            }
        }

        protected void btnCancel_Click( object sender, EventArgs e )
        {
            if ( GetCourseRequirement().Id == 0 )
            {
                NavigateToParentPage( new Dictionary<string, string> { { PageParameterKey.CourseId, PageParameter( PageParameterKey.CourseId ) } } );
            }

            ShowDetails();
        }

        #endregion

        #region Methods

        private void ShowEdit()
        {
            pnlDetails.Visible = false;
            pnlEdit.Visible = true;

            var courseRequirement = GetCourseRequirement();
            pCourse.SetValue( courseRequirement.CourseId );
            pGroup.Visible = false;
            pDataview.Visible = false;
            if ( ddlSelect.SelectedValue == "DATAVIEW" )
            {
                pDataview.SetValue( courseRequirement.DataViewId );
                pDataview.Visible = true;
            }
            else if ( ddlSelect.SelectedValue == "GROUP" )
            {
                pGroup.SetValue( courseRequirement.GroupId );
                pGroup.Visible = true;
            }
            tbDaysValid.Text = courseRequirement.DaysValid.ToString();
        }
        #endregion


        protected void ddlSelect_SelectedIndexChanged( object sender, EventArgs e )
        {
            pGroup.Visible = false;
            pDataview.Visible = false;
            if ( ddlSelect.SelectedValue == "DATAVIEW" )
            {
                pDataview.Visible = true;
            }
            else if ( ddlSelect.SelectedValue == "GROUP" )
            {
                pGroup.Visible = true;
            }
        }

        protected void btnEdit_Click( object sender, EventArgs e )
        {
            ShowEdit();
        }


        protected void fStatuses_ApplyFilterClick( object sender, EventArgs e )
        {
            ShowDetails();
        }

        protected void btnDelete_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            CourseRequirementService courseRequirementService = new CourseRequirementService( rockContext );

            var courseRequirement = GetCourseRequirement( courseRequirementService );

            var courseId = courseRequirement.CourseId;

            courseRequirementService.Delete( courseRequirement );
            rockContext.SaveChanges();

            NavigateToParentPage( new Dictionary<string, string> { { PageParameterKey.CourseId, courseId.ToString() } } );
        }
    }
}