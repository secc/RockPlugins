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
        name: "Lockdown Alert Title",
        description: "Default title of a lockdown alert",
        required: false,
        defaultValue: "Lockdown Alert",
        order: 0,
        key: "LockdownTitle" )]
    [MemoField(
        name: "Lockdown Alert Message",
        description: "Message that will be delivered when a lockdown alert is sent",
        required: false,
        defaultValue: "Lock down the campus",
        order: 1,
        key: "LockdownAlert" )]

    [DefinedTypeField(
        name: "Defined Type",
        description: "The Defined Value to use for audience selection.",
        required: true )]

    [DefinedValueField( Rock.SystemGuid.DefinedType.COMMUNICATION_SMS_FROM, "From", "The number to originate message from (configured under Admin Tools > Communications > SMS Phone Numbers).", true, false, "", "", 0 )]


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
                lMessage.Text = GetAttributeValue( "LockdownAlert" );
                lAlertTitle.Text = GetAttributeValue( "LockdownTitle" );

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
            var campus = bddlCampus.SelectedValueAsInt();

            CreateAlert( GetAudience( false, campus ) );

        }

        protected void btnStaffVol_Click( object sender, EventArgs e )
        {
            var campus = bddlCampus.SelectedValueAsInt();

            CreateAlert( GetAudience( true, campus ) );

        }

        private DefinedValueCache GetAudience( bool hasVolunteer, int? campusId )
        {

            var campusObj = CampusCache.Get( campusId ?? 0 );
            var campusGuid = campusObj.Guid;

            var definedTypeGuid = GetAttributeValue( "DefinedType" ).AsGuid();
            var definedType = DefinedTypeCache.Get( definedTypeGuid );
            var definedValues = definedType.DefinedValues;

            foreach ( var definedValue in definedValues )
            {
                var definedValueCampusGuid = definedValue.GetAttributeValue( "Campus" ).AsGuid();
                bool definedValueHasVolunteer = definedValue.GetAttributeValue( "HasVolunteer" ).AsBoolean();
                if ( campusGuid == definedValueCampusGuid && definedValueHasVolunteer == hasVolunteer )
                {
                    return definedValue;
                }
            }

            return null;
        }

        private void CreateAlert( DefinedValueCache audience )
        {
            if ( audience == null )
            {
                maPopup.Show( "The audience is not defined.", ModalAlertType.Information );
                return;

            }

            RockContext rockContext = new RockContext();

            AlertNotificationService alertNotificationService = new AlertNotificationService( rockContext );
            AlertMessageService alertMessageService = new AlertMessageService( rockContext );

            int alertTypeId = 31480;

            var alertnotification = new AlertNotification
            {
                Title = lAlertTitle.Text,
                AlertNotificationTypeValueId = alertTypeId,
                AudienceValueId = audience.Id,
                IsActive = true,
            };

            alertNotificationService.Add( alertnotification );

            rockContext.SaveChanges();

            var alertMessage = new AlertMessage
            {
                AlertNotificationId = alertnotification.Id,
                Message = lMessage.Text,
                
            };

            alertMessageService.Add( alertMessage );


            rockContext.SaveChanges();




            alertMessage.SendCommunication( GetAttributeValue( "From" ).AsGuid() );

            mdCustomMessage.Hide();
            mdLockdownAlert.Hide();

            maPopup.Show( "The LockDown message has been sent.", ModalAlertType.Information );
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
            string title = tbAlertName.Text.Trim();

            lMessage.Text = message;
            lAlertTitle.Text = title;

            var campus = bddlCampus.SelectedValueAsInt();

            CreateAlert( GetAudience( cbCustomMessageIncludeVols.Checked, campus ) );

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





        #endregion

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

        protected void btnLockdownAlert_Click( object sender, EventArgs e )
        {
            mdLockdownAlert.Show();
        }

        protected void btnCustomAlert_Click( object sender, EventArgs e )
        {
            mdCustomMessage.Show();
        }


        protected void mdLockdownAlert_SaveClick( object sender, EventArgs e )
        {
            mdLockdownAlert.Hide();
        }

    }
}