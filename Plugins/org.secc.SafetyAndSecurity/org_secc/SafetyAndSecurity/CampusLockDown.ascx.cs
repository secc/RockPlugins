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
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using org.secc.SafetyAndSecurity.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.SafetyAndSecurity
{
    /// <summary>
    /// Block to execute a sql command and display the result (if any).
    /// </summary>
    [DisplayName( "Campus Lock Down" )]
    [Category( "SECC > Safety And Security" )]
    [Description( "Campus Lock Down" )]

    #region Block Settings
    [TextField(
        name: "Standard Alert Title",
        description: "Default title of an alert",
        required: false,
        defaultValue: "Alert",
        order: 0,
        key: "StandardTitle" )]
    [MemoField(
        name: "Standard Alert Message",
        description: "Message that will be delivered when standard alert is sent",
        required: false,
        defaultValue: "This is the Standard Alert Message",
        order: 1,
        key: "StandardAlert" )]
   
    [DataViewField(
        name: "DataView",
        description: "The DataView to use for the review process.",
        required: false )]

    #endregion Block Settings

    public partial class CampusLockDown : RockBlock
    {
        #region Control Methods

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RockContext rockContext = new RockContext();
            var campusList = CampusCache.All()
                .Select( a => new CampusItem { Name = a.Name, Id = a.Id } )
                .ToList();

            rptCampuses.DataSource = campusList;
            rptCampuses.DataBind();

            bddlCampus.DataSource = CampusCache.All();
            bddlCampus.DataBind();

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
                lMessage.Text = GetAttributeValue( "StandardAlert" );
                lAlertTitle.Text = GetAttributeValue( "StandardTitle" );

                if ( GetAttributeValue( "DataView" ).IsNotNullOrWhiteSpace() )
                {
                    DataViewService dataViewService = new DataViewService( new RockContext() );
                    //var dvipReviewDataView.SetValue( dataViewService.Get( GetAttributeValue( "DefaultReviewDataView" ).AsGuid() ) );
                }

                var campus = CampusCache.Get( GetBlockUserPreference( "Campus" ).AsInteger() );
                if ( campus != null )
                {
                    bddlCampus.Title = campus.Name;
                    bddlCampus.SetValue( campus.Id );
                    lCampusTitle.Text = bddlCampus.Title;
                    pnlCampuses.Visible = false;
                    pnlMain.Visible = true;
                }
                else
                {
                    pnlCampuses.Visible = true;
                    pnlMain.Visible = false;
                }
            }




        }


        #endregion

        #region Internal Methods


        private void ShowMessage( string message, string header = "Information", string cssClass = "panel panel-warning" )
        {
            pnlMain.Visible = false;
            pnlInfo.Visible = true;
            ltHeading.Text = header;
            ltBody.Text = message;
            pnlInfo.CssClass = cssClass;
        }


        #endregion

        #region Events

        protected void btnStaff_Click( object sender, EventArgs e )
        {
            GetStaff();

            RockContext rockContext = new RockContext();

            AlertNotificationService alertNotificationService = new AlertNotificationService( rockContext );
            AlertMessageService alertMessageService = new AlertMessageService( rockContext );

            int alertTypeId = 31480;
            int alertAudienceId = 31476;

            var alertnotification = new AlertNotification
            {
                Title = lAlertTitle.Text,
                AlertNotificationTypeValueId = alertTypeId,
                AudienceValueId = alertAudienceId,
                IsActive = true,
            };



            alertNotificationService.Add( alertnotification );

            rockContext.SaveChanges();



            alertMessageService.Add( new AlertMessage
            {
                AlertNotificationId = alertnotification.Id,
                Message = lMessage.Text,
                CommunicationId = 40558,
            } );

            rockContext.SaveChanges();
        }

        protected void btnStaffVol_Click( object sender, EventArgs e )
        {
            //GetCheckedInVolunteers( 1 );

        }
        protected void bddlCampus_SelectionChanged( object sender, EventArgs e )
        {
            SetBlockUserPreference( "Campus", bddlCampus.SelectedValue );
            var campus = CampusCache.Get( bddlCampus.SelectedValueAsInt() ?? 0 );
            bddlCampus.Title = campus != null ? campus.Name : "All Campuses";
            lCampusTitle.Text = bddlCampus.Title;

        }

        protected void mdCustomMessage_SaveClick( object sender, EventArgs e )
        {
            string message = tbAlertMessage.Text.Trim();
            //           if ( message.IsNullOrWhiteSpace() )
            //           {
            //               hfCustomMessage.Value = " " ;
            //               return;
            //           }
            string title = tbAlertName.Text.Trim();

            lMessage.Text = message;
            lAlertTitle.Text = title;

            mdCustomMessage.Hide();
        }

        /// <summary>
        /// Handles the ItemCommand event of the rptCampuses control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterCommandEventArgs"/> instance containing the event data.</param>
        protected void rptCampuses_ItemCommand( object source, RepeaterCommandEventArgs e )
        {
            var campusId = e.CommandArgument.ToString();
            int cId = Int32.Parse( campusId );


            if ( campusId != null )
            {
                SetBlockUserPreference( "Campus", campusId );
                var campus = CampusCache.Get( cId );
                bddlCampus.Title = campus != null ? campus.Name : "All Campuses";
                lCampusTitle.Text = bddlCampus.Title;
                pnlCampuses.Visible = false;
                pnlMain.Visible = true;


            }
        }

        #endregion

        #region Methods


        private void GetStaff()
        {

            SendMessage( "Staff", lMessage.Text );
        }

        private void SendMessage( string send, string msg )
        {
            maPopup.Show( "The LockDown message has been sent to all " + send + ".  " + msg, ModalAlertType.Information );

        }

        #endregion
        protected class VolunteerByCampus
        {
            public Person VolunteerPerson { get; set; }
            public Campus CampusId { get; set; }

        }

        protected class Staff
        {
            public Person StaffPerson { get; set; }

        }
        /// <summary>
        /// Campus Item
        /// </summary>
        public class CampusItem
        {
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the identifier.
            /// </summary>
            /// <value>
            /// The identifier.
            /// </value>
            public int Id { get; set; }
        }




        protected void btnEdit_Click( object sender, EventArgs e )
        {
            mdCustomMessage.Show();
        }
    }
}