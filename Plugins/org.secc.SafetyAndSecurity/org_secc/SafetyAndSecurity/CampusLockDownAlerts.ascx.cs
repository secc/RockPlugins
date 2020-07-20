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
using System.Data;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using Rock.Web.Cache;
using System.Web.UI.HtmlControls;
using org.secc.SafetyAndSecurity.Model;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace RockWeb.Plugins.org_secc.SafetyAndSecurity
{

    [DisplayName( "Campus Lock Down Active Alerts" )]
    [Category( "SECC > Safety And Security" )]
    [Description( "A list of all active Campus Lock Down alerts" )]

    #region Block Settings

    [MemoField(
        name: "Standard Alert",
        description: "Message that will be delivered when standard alert is sent",
        required: true,
        defaultValue: "This is the Standard Alert Message",
        order: 0,
        key: "StandardAlert" )]
    [DataViewField(
        name: "Default Review DataView",
        description: "The default DataView to use for the review process.",
        required: false )]

    #endregion Block Settings

    public partial class CampusLockDownAlerts : RockBlock
    {
        public bool IsActive { get; private set; }
        #region Control Methods

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );               
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
                BindRepeater();
                
            }

            



        }

        private void BindRepeater()
        {
            RockContext rockContext = new RockContext();

            AlertNotificationService alertNotificationService = new AlertNotificationService( rockContext );
            AlertMessageService alertMessageService = new AlertMessageService( rockContext );


            var alerts = alertNotificationService
                .Queryable()
                .Where( a => a.IsActive )
                .OrderBy( a => a.CreatedDateTime )
                .ToList();


            rNotifications.DataSource = alerts;
            rNotifications.DataBind();
        }

        protected void ItemBound( object sender, RepeaterItemEventArgs args )
        {
            if ( args.Item.ItemType == ListItemType.Item || args.Item.ItemType == ListItemType.AlternatingItem )
            {


                var alertNotification = ( AlertNotification ) args.Item.DataItem;

                RockContext rockContext = new RockContext();

                AlertMessageService alertMessageService = new AlertMessageService( rockContext );

                var alertMessages = alertMessageService
                    .Queryable()
                    .Where( a => a.AlertNotificationId == alertNotification.Id )
                    .OrderByDescending( a => a.Id )
                    .ToList();


                Repeater rNotificationsMessages = ( Repeater ) args.Item.FindControl( "rNotificationsMessages" );
                rNotificationsMessages.DataSource = alertMessages;
                rNotificationsMessages.DataBind();
            }

           
        }
        #endregion

        #region Internal Methods
        #endregion

        #region Events
        #endregion

        #region Methods
        #endregion

        protected class ActiveNotification
        {
            public string AlertTitle { get; set; }
            public string Initiator { get; set; }
            public DateTime? DateTime { get; set; }
            public string Messages { get; set; }
            public int Id { get; set; }
        }

        protected void AllClear_Click( object sender, CommandEventArgs e )
        {
            if (e.CommandName == "AllClear_Click")
            {
                int index;
                bool bIsConverted = int.TryParse( e.CommandArgument.ToString(), out index );

                RockContext rockContext = new RockContext();

                AlertNotificationService alertNotificationService = new AlertNotificationService( rockContext );
                AlertMessageService alertMessageService = new AlertMessageService( rockContext );

                var alert = alertNotificationService
                    .Get( index );
                
               alert.IsActive = false;
               

                rockContext.SaveChanges();

                BindRepeater();

            }
        }

        protected void SendUpdate_Click( object sender, CommandEventArgs e )
        {
            if ( e.CommandName == "SendUpdate_Click" )
            {
                string AlertID;
                AlertID = e.CommandArgument.ToString();
                hfAlertID.Value = AlertID;
            }
            tbAlertMessage.Text = "";
            mdSendUpdate.Show();

        }

        protected void mdSendUpdate_SendClick( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            AlertMessageService alertMessageService = new AlertMessageService( rockContext );

            string message = tbAlertMessage.Text.Trim();

            int AlertID = Int32.Parse( hfAlertID.Value );
            alertMessageService.Add( new AlertMessage
            {
                AlertNotificationId = AlertID,
                Message = message,
                CommunicationId = 40558,
            } );

            rockContext.SaveChanges();

            mdSendUpdate.Hide();

            BindRepeater();
        }
    }


}