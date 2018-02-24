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
// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.TotalMD
{
    /// <summary>
    /// Block for displaying the history of changes to a particular user.
    /// </summary>
    [DisplayName( "Appointment Detail" )]
    [Category( "SECC > TotalMD" )]
    [Description( "Block for displaying counselor appointments." )]
    [GroupField( "Group", "The Group to use for determining security access", true, order: 1 )]
    public partial class ApptDetail : RockBlock
    {

        #region Fields

        private string ConnString = ConfigurationManager.ConnectionStrings["TotalMDContext"].ConnectionString;
        DataTable dt = new DataTable();

        #endregion

        #region Base Control Methods


        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += ApptDetail_BlockUpdated;

            gfSettings.ApplyFilterClick += gfSettings_ApplyFilterClick;
            gfSettings.DisplayFilterValue += gfSettings_DisplayFilterValue;
            //gAppt.GridRebind += gAppt_GridRebind;
        }

        /// <summary>
        /// Handles the BlockUpdated event of the ApptDetail control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        public void ApptDetail_BlockUpdated( object sender, EventArgs e )
        {
            BindGrid();
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
                if (pnlList.Visible == true)
                {
                    BindFilter();
                    BindGrid();
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the ApplyFilterClick event of the gfSettings control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void gfSettings_ApplyFilterClick( object sender, EventArgs e )
        {
            gfSettings.SaveUserPreference( "Counselor", cblCounselors.SelectedValues.AsDelimited( ";" ) );
            gfSettings.SaveUserPreference( "Appt Date Range", drpDates.DelimitedValues );
            BindGrid();
        }

        /// <summary>
        /// Handles the DisplayFilterValue event of the gfSettings control.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void gfSettings_DisplayFilterValue( object sender, Rock.Web.UI.Controls.GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Counselor":
                    {
                        e.Value = ResolveValues( e.Value, cblCounselors );
                        break;
                    }
                case "Appt Date Range":
                    {
                        e.Value = DateRangePicker.FormatDelimitedValues( e.Value );
                        break;
                    }
                default:
                    {
                        e.Value = string.Empty;
                        break;
                    }
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the gAppt control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        void gAppt_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        protected void rptrDateGrouping_ItemDataBound( object sender, System.Web.UI.WebControls.RepeaterItemEventArgs e )
        {
            if ( e.Item.ItemType == ListItemType.Item |
                    e.Item.ItemType == ListItemType.AlternatingItem )
            {
                Repeater rptr = ( Repeater ) e.Item.FindControl( "rptrApptDetails" );
                rptr.DataSource =
                    dt.AsEnumerable().Where( x => x["Date"].Equals( e.Item.DataItem ) );
                rptr.DataBind();

                for ( int i = 0; i <= rptr.Items.Count - 1; i++ )
                {
                    var WorkPhone = ( HtmlGenericControl ) rptr.Items[i].FindControl( "liWorkPhone" );
                    if ( WorkPhone.InnerText == "Work: " )
                        WorkPhone.Visible = false;

                    var CellPhone = ( HtmlGenericControl ) rptr.Items[i].FindControl( "liCellPhone" );
                    if ( CellPhone.InnerText == "Cell: " )
                        CellPhone.Visible = false;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void BindFilter()
        {
            using ( OdbcConnection con = new OdbcConnection( ConnString ) )
            {
                RockContext rockContext = new RockContext();
                Group group = null;
                var groupGuid = this.GetAttributeValue( "Group" ).AsGuidOrNull();
                var whereClause = String.Empty;

                if ( groupGuid.HasValue )
                {
                    group = new GroupService( rockContext ).Get( groupGuid.Value );
                }

                if ( group != null )
                {
                    var groupMemberService = new GroupMemberService( rockContext );
                    var groupMember = groupMemberService.GetByGroupIdAndPersonId( group.Id, CurrentPerson.Id ).FirstOrDefault();

                    if ( groupMember != null )
                    {
                        if ( groupMember.GroupMemberStatus == GroupMemberStatus.Active )
                        {
                            // Get any existing group member attribute values
                            groupMember.LoadAttributes( rockContext );

                            foreach ( var attributeValue in groupMember.AttributeValues )
                            {
                                if ( attributeValue.Key == "ProviderId" && groupMember.GroupRole.Name == "Member" )
                                {
                                    if ( !String.IsNullOrEmpty( attributeValue.Value.Value ) )
                                    {
                                        // It is possible that the Provider Id was typed incorrectly and
                                        // won't return any results. 
                                        whereClause = "WHERE [Code] = '" + attributeValue.Value.Value + "'";
                                        gfSettings.SaveUserPreference( "Counselor", attributeValue.Value.Value );
                                    }
                                    else
                                    {
                                        // This person was added to the security group as a member and doesn't have
                                        // a Provider Id.
                                        gfSettings.SaveUserPreference( "Counselor", "" );
                                        pnlList.Visible = false;
                                        nbMessage.Title = "You have been added to the security group, but need a Provider Id to view appointments.";
                                        nbMessage.Visible = true;
                                    }

                                    break;
                                }
                            }
                        }
                        else
                        {
                            // This person is not active in the security group.
                            pnlList.Visible = false;
                            nbMessage.Title = "You are not active in the security group.";
                            nbMessage.Visible = true;
                        }
                    }
                    else
                    {
                        // This person is not in the security group at all.
                        gfSettings.SaveUserPreference( "Counselor", "" );
                        pnlList.Visible = false;
                        nbMessage.Title = "You have not been added to the security group.";
                        nbMessage.Visible = true;
                    }
                }

                string qry = @"SELECT [Code], CONCAT(CONCAT([First Name] WITH ' ') WITH [Last Name]) AS [Counselor Name] 
                    FROM Provider " +
                    whereClause +
                    " ORDER BY [Counselor Name]";

                OdbcDataAdapter da = new OdbcDataAdapter( qry, con );
                DataTable dt1 = new DataTable();
                da.Fill( dt1 );
                if ( dt1.Rows.Count > 0 )
                {
                    cblCounselors.DataSource = dt1;
                    cblCounselors.DataTextField = "Counselor Name";
                    cblCounselors.DataValueField = "Code";
                    cblCounselors.DataBind();

                    string selectedCounselors = gfSettings.GetUserPreference( "Counselor" );
                    if ( !string.IsNullOrWhiteSpace( selectedCounselors ) )
                    {
                        cblCounselors.SetValues( selectedCounselors.Split( ';' ).ToList() );
                    }
                }
                else
                {
                    pnlList.Visible = false;
                    nbMessage.Title = "No results found.  If you were expecting to see something, then please verify that your Provider Id information is correct.";
                    nbMessage.Visible = true;
                }

            }

            drpDates.DelimitedValues = gfSettings.GetUserPreference( "Appt Date Range" );
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            // Date Range
            var drp = new DateRangePicker();
            drp.DelimitedValues = gfSettings.GetUserPreference( "Appt Date Range" );

            // Filter by Date
            var fmt = "yyyy-MM-dd";
            var BeginDate = "1900-01-01";
            var EndDate = Convert.ToDateTime( DateTime.Today ).AddDays( 1 ).ToString( fmt );

            if ( drp.LowerValue.HasValue )
                BeginDate = Convert.ToDateTime( drp.LowerValue ).ToString( fmt );
            else
                EndDate = "1900-01-02";

            if ( drp.UpperValue.HasValue )
                EndDate = Convert.ToDateTime( drp.UpperValue ).AddDays( 1 ).ToString( fmt );

            // Filter by Counselors
            string selectedCounselors = "'" + gfSettings.GetUserPreference( "Counselor" ).Replace( ";", "','" ) + "'";

            using ( OdbcConnection con = new OdbcConnection( ConnString ) )
            {
                string qry = @"SELECT CONCAT(CONCAT([First Name] WITH ' ') WITH [Last Name]) AS [Patient Name], [Date], [Time], l.[Text] AS [Status],
                    [Work Phone], [Mobile Phone], CONCAT(CONCAT(p.[First Name] WITH ' ') WITH p.[Last Name]) AS [Counselor Name] 
                    FROM Appointment a
                    LEFT OUTER JOIN Provider p ON a.[Provider] = p.[Code]
                    INNER JOIN lookuplist l ON l.[Table] = 'APPOINTMENT' AND l.[Field] = 'STATUS' AND a.[Status] = CAST(l.[ID] AS Integer)" +
                  " WHERE a.[Date] >= " + "'" + BeginDate + "' AND a.[Date] < '" + EndDate + "' AND a.[Provider] IN (" + selectedCounselors + ")" +
                  " AND l.[Text] IN ('Unconfirmed','Left Message','Confirmed','Ready','Cancelled','Missed')" +
                  " ORDER BY [Date],[Time],[Counselor Name]";

                OdbcDataAdapter da = new OdbcDataAdapter( qry, con );
                da.Fill( dt );

                rptrDateGrouping.DataSource =
                    ( from x in dt.AsEnumerable() select x["Date"] ).Distinct();
                rptrDateGrouping.DataBind();
            }
        }

        /// <summary>
        /// Formats 24 hr timespan to 12 hr time.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <returns></returns>
        protected string Format12Hour( Object time )
        {
            //Checks for nulls
            if ( Convert.IsDBNull( time ) )
            {
                return "N/A";
            }
            else
            {
                //Grabs your Timespan
                TimeSpan ts = ( TimeSpan ) time;

                //Determines AM/PM and outputs accordingly
                if ( ts.Hours >= 12 )
                {
                    return String.Format( "{0:#0}:{1:00} PM", ts.Hours - 12, ts.Minutes );
                }
                else
                {
                    return String.Format( "{0:#0}:{1:00} AM", ts.Hours, ts.Minutes );
                }
            }
        }

        /// <summary>
        /// Resolves the values.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="listControl">The list control.</param>
        /// <returns></returns>
        private string ResolveValues( string values, System.Web.UI.WebControls.CheckBoxList checkBoxList )
        {
            var resolvedValues = new List<string>();

            foreach ( string value in values.Split( ';' ) )
            {
                var item = checkBoxList.Items.FindByValue( value );
                if ( item != null )
                {
                    resolvedValues.Add( item.Text );
                }
            }

            return resolvedValues.AsDelimited( ", " );
        }

        #endregion
    }
}