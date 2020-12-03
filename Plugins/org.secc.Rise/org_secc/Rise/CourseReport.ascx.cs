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
using org.secc.Rise.Model;
using org.secc.xAPI.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Rise
{
    [DisplayName( "Course Report" )]
    [Category( "SECC > Rise" )]
    [Description( "Reporting for courses in Rise" )]

    public partial class CourseReport : RockBlock
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

            gUsers.GridRebind += GUsers_GridRebind;
        }

        private void GUsers_GridRebind( object sender, Rock.Web.UI.Controls.GridRebindEventArgs e )
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


            if ( !Page.IsPostBack )
            {
                ShowDetails();
            }
        }

        private void ShowDetails()
        {
            var course = GetCourse();
            if ( course != null )
            {
                ltName.Text = course.Name;

                var data = GetData( course );
                BuildChart( data );
                BindGrid( data );
            }
        }

        private List<CourseData> GetData( Course course )
        {
            var xObj = course.GetExperienceObject();
            var completionValueId = org.secc.xAPI.Utilities.VerbHelper.GetOrCreateVerb( "http://activitystrea.ms/schema/1.0/complete" ).Id;

            RockContext rockContext = new RockContext();
            ExperienceService experienceService = new ExperienceService( rockContext );
            var experiences = experienceService.Queryable( "PersonAlias.Person,Result" )
                .Where( x => x.xObjectId == xObj.Id && x.VerbValueId == completionValueId )
                .ToList();

            var data = course.EnrolledGroups
                .SelectMany( g => g.Members )
                .Select( gm => gm.Person )
                .DistinctBy( p => p.Id )
                .Select( p => new CourseData
                {
                    Person = p,
                    Status = "Incomplete",
                    Enrolled = true
                } )
                .ToList();

            foreach ( var exp in experiences )
            {
                var courseData = data.Where( d => exp.PersonAlias.PersonId == d.Person.Id ).FirstOrDefault();

                if ( courseData == null )
                {
                    courseData = new CourseData
                    {
                        Person = exp.PersonAlias.Person,
                        Status = "Incomplete",
                        Enrolled = false
                    };
                    data.Add( courseData );
                }

                if ( exp.Result != null && exp.Result.IsComplete && exp.Result.WasSuccess )
                {
                    courseData.Status = "Complete";
                    var score = exp.Result.GetQualifier( "score" );
                    if ( score != null )
                    {
                        var percent = score.GetQualifier( "percent" );
                        if ( percent != null )
                        {
                            courseData.Score = percent.Value.AsIntegerOrNull();
                        }
                    }
                }
            }

            return data.OrderBy( d => d.Person.LastName ).ThenBy( d => d.Person.NickName ).ToList();
        }

        private void BuildChart( List<CourseData> data )
        {
            ltEnrolled.Text = data.Count( d => d.Enrolled ).ToString();
            ltCompleted.Text = data.Count( d => d.Status == "Complete" ).ToString();
            ltIncomplete.Text = data.Count( d => d.Enrolled && d.Status == "Incomplete" ).ToString();

            var chart = new List<Dictionary<string, object>>();

            chart.Add( new Dictionary<string, object> {
                { "YValueTotal", data.Count( d => d.Enrolled && d.Status == "Complete" ) },
                { "MetricTitle","Complete"} } );

            chart.Add( new Dictionary<string, object> {
                { "YValueTotal", data.Count( d => d.Enrolled && d.Status == "Incomplete" ) },
                { "MetricTitle","Incomplete"} } );

            pcComplete.ChartData = chart.ToJson();
            pcComplete.Width = 200;
            pcComplete.Height = 200;
        }

        private void BindGrid( List<CourseData> data )
        {
            SortProperty sortProperty = gUsers.SortProperty;

            if ( ddlStatus.SelectedValue.IsNotNullOrWhiteSpace() )
            {
                data = data.Where( d => d.Status == ddlStatus.SelectedValue ).ToList();
            }


            if ( ddlEnrolled.SelectedValue.IsNotNullOrWhiteSpace() )
            {
                data = data.Where( d => d.Enrolled == ddlEnrolled.SelectedValue.AsBoolean() ).ToList();
            }

            if ( sortProperty != null )
            {
                data = data.AsQueryable().Sort( sortProperty ).ToList();
            }

            gUsers.DataSource = data;
            gUsers.DataBind();
        }

        private Course GetCourse()
        {
            return GetCourse( new CourseService( new RockContext() ) );
        }

        private Course GetCourse( CourseService courseService )
        {
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
            ShowDetails();
        }

        internal class CourseData
        {
            public int PersonId { get { return Person.Id; } }

            public Person Person { get; set; }

            public string Status { get; set; }

            public DateTime? DateTime { get; set; }

            public int? Score { get; set; }

            public bool Enrolled { get; set; }
        }
        #endregion

        protected void fUsers_ClearFilterClick( object sender, EventArgs e )
        {
            ddlEnrolled.SelectedValue = "";
            ddlStatus.SelectedValue = "";
            ShowDetails();
        }

        protected void fUsers_ApplyFilterClick( object sender, EventArgs e )
        {
            ShowDetails();
        }
    }
}