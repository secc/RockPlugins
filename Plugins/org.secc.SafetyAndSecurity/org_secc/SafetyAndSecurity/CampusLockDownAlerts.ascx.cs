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

        /*private string GetAudience( int audienceValueId )
        {
            var definedTypeGuid = GetAttributeValue( "DefinedType" ).AsGuid();
            var definedType = DefinedTypeCache.Get( definedTypeGuid );
            

            return null;
        }*/

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
            if ( e.CommandName == "AllClear_Click" )
            {

                ShowMessageModal( e.CommandArgument.ToString(), true );
            }
        }



        protected void SendUpdate_Click( object sender, CommandEventArgs e )
        {
            if ( e.CommandName == "SendUpdate_Click" )
            {
                ShowMessageModal( e.CommandArgument.ToString(), false );
            }

        }

        private void ShowMessageModal( string alertId, bool isAllClear )
        {
            hfAllClear.Value = isAllClear.ToString();
            hfAlertID.Value = alertId;
            tbAlertMessage.Text = "";
            mdSendUpdate.Show();
            if ( isAllClear )
            {
                mdSendUpdate.Title = "Send All Clear";
                tbAlertMessage.Required = false;
            }
            else
            {
                mdSendUpdate.Title = "Send Update";
                tbAlertMessage.Required = true;
            }
            

        }

        

        protected void mdSendUpdate_SendClick( object sender, EventArgs e )
        {
            int alertID = hfAlertID.Value.AsInteger();

            if ( tbAlertMessage.Text.IsNotNullOrWhiteSpace() )
            {
                RockContext rockContext = new RockContext();
                AlertMessageService alertMessageService = new AlertMessageService( rockContext );
                AlertNotificationService alertNotificationService = new AlertNotificationService( rockContext );


                var alert = alertNotificationService
                    .Get( alertID );

                var alertMessage = new AlertMessage
                {
                    AlertNotification = alert,
                    Message = tbAlertMessage.Text,
                    
                };

                alertMessageService.Add( alertMessage );

                rockContext.SaveChanges();

                alertMessage.SendCommunication( GlobalAttributesCache.Value( "DefaultSMSFromNumber" ).AsGuid() );
            }


            if ( hfAllClear.Value.AsBoolean() )
            {
                ClearAlert( alertID );
            }
            mdSendUpdate.Hide();

            BindRepeater();
        }

        private void ClearAlert( int alertNotificationId )
        {
            RockContext rockContext = new RockContext();

            AlertNotificationService alertNotificationService = new AlertNotificationService( rockContext );
            AlertMessageService alertMessageService = new AlertMessageService( rockContext );

            var alert = alertNotificationService
                .Get( alertNotificationId );

            alert.IsActive = false;

            rockContext.SaveChanges();
        }
    }


}