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
using System.IO;
using System.Linq;
using System.Net;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Administration
{
    [DisplayName( "Personal Devices" )]
    [Category( "SECC > Administration" )]
    [Description( "Shows a list of all person devices." )]
    [LinkedPage( "Interactions Page", "The interactions associated with a specific personal device." )]
    [CodeEditorField( "Lava Template", "Lava template to use to display content", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, @"
<div style=""min-height: 120px;"">
    <h3 class=""margin-v-none"">
        {% if item.DeviceIconCssClass != '' %}
            <i class=""fa {{ item.DeviceIconCssClass }}""></i>
        {% endif %}
        {% if item.PersonalDevice.NotificationsEnabled == true %}
            <i class=""fa fa-comment-o""></i>
        {% endif %}
    </h3>
    <dl>
        {% if item.PlatformValue != '' %}<dt>{{ item.PlatformValue }} {{ item.PersonalDevice.DeviceVersion }}</dt>{% endif %}
        {% if item.PersonalDevice.CreatedDateTime != null %}<dt>Discovered</dt><dd>{{ item.PersonalDevice.CreatedDateTime }}</dd>{% endif %}                              
        {% if item.PersonalDevice.MACAddress != '' and item.PersonalDevice.MACAddress != null %}<dt>MAC Address</dt><dd>{{ item.PersonalDevice.MACAddress }}</dd>{% endif %}
        {% assign archivedMacAddress = item.PersonalDevice | Attribute:'ArchivedMACAddress' %}
        {% if archivedMacAddress != '' and archivedMacAddress != null %}<dt>Archived MAC Address</dt><dd>{{ archivedMacAddress }}</dd>{% endif %}
    </dl>
</div>
", "", 2, "LavaTemplate" )]
    [BooleanField( "Remove Device", "if true, the specific personal device will be removed from front porch", true )]
    [ContextAware( typeof( Person ) )]

    public partial class PersonalDevices : RockBlock
    {
        #region Fields

        private Person _person = null;
        private readonly string fpAuthenticationHeader = string.Format( "authorization-token:{0}", GlobalAttributesCache.Value( "FrontporchAPIToken" ) );
        private readonly string fpHost = GlobalAttributesCache.Value( "FrontporchHost" );

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            Guid? personGuid = PageParameter( "PersonGuid" ).AsGuidOrNull();
            if ( personGuid.HasValue )
            {
                _person = new PersonService( new RockContext() ).Get( personGuid.Value );
            }
            else if ( this.ContextEntity<Person>() != null )
            {
                _person = this.ContextEntity<Person>();
            }

            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( _person == null )
            {
                this.Visible = false;
                return;
            }

            if ( !Page.IsPostBack )
            {
                lPanelHeader.Text = _person.FullName;
                BindRepeater();
            }
            base.OnLoad( e );
        }

        private void BindRepeater()
        {
            RockContext rockContext = new RockContext();

            var personalDeviceService = new PersonalDeviceService( rockContext );
            var personalDevices = personalDeviceService.Queryable().Where( a => a.PersonAlias != null && a.PersonAlias.PersonId == _person.Id );

            var items = personalDevices.Select( a => new PersonalDeviceItem
            {
                PersonalDevice = a
            } ).ToList();

            foreach ( var item in items )
            {
                if ( item.PersonalDevice.PersonalDeviceTypeValueId.HasValue )
                {
                    var value = DefinedValueCache.Get( item.PersonalDevice.PersonalDeviceTypeValueId.Value );
                    item.DeviceIconCssClass = value.GetAttributeValue( "IconCssClass" );
                }

                if ( item.PersonalDevice.PlatformValueId.HasValue )
                {
                    item.PlatformValue = DefinedValueCache.Get( item.PersonalDevice.PlatformValueId.Value ).Value;

                }
            }

            rDevices.DataSource = items;
            rDevices.DataBind();
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
            BindRepeater();
        }

        protected void mdEdit_SaveClick( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            var personalDeviceService = new PersonalDeviceService( rockContext );
            var personalDevice = personalDeviceService.Get( hfDeviceId.ValueAsInt() );

            if ( personalDevice == null )
            {
                personalDevice = new PersonalDevice
                {
                    Guid = Guid.NewGuid()
                };
                personalDeviceService.Add( personalDevice );
            }

            personalDevice.PersonAliasId = ppPerson.PersonAliasId;

            personalDevice.NotificationsEnabled = rblnotifications.SelectedValue.AsBoolean();

            personalDevice.PersonalDeviceTypeValueId = ddldevicetype.SelectedValueAsInt();
            personalDevice.PlatformValueId = ddlplatform.SelectedValueAsInt();
            personalDevice.DeviceUniqueIdentifier = tbdeviceuniqueid.Text;
            personalDevice.DeviceVersion = tbdeviceversion.Text;

            var previousMACAddress = personalDevice.MACAddress != null ? personalDevice.MACAddress.ToLower() : "";
            personalDevice.MACAddress = tbmaddress.Text.RemoveAllNonAlphaNumericCharacters().ToLower();

            if ( personalDevice.MACAddress != previousMACAddress )
            {
                UpdateMAC( personalDevice.MACAddress, previousMACAddress );
            }

            rockContext.SaveChanges();
            mdEdit.Hide();
            BindRepeater();
        }


        protected void btnInteraction_Command( object sender, CommandEventArgs e )
        {
            NavigateToLinkedPage( "InteractionsPage", new Dictionary<string, string> { { "personalDeviceId", ( string ) e.CommandArgument } } );
        }

        protected void rDevices_ItemDataBound( object sender, System.Web.UI.WebControls.RepeaterItemEventArgs e )
        {
            var item = e.Item.DataItem as PersonalDeviceItem;
            var ltLava = e.Item.FindControl( "ltLava" ) as Literal;
            var lava = GetAttributeValue( "LavaTemplate" );
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            mergeFields.Add( "Person", _person );
            mergeFields.Add( "Item", item );
            //covering for some lazy lava
            mergeFields.Add( "item", item );
            ltLava.Text = lava.ResolveMergeFields( mergeFields );

            //If there is no MAC to remove hide this button
            var btnInactivateDevice = e.Item.FindControl( "btnInactivateDevice" ) as LinkButton;
            if ( item.PersonalDevice.MACAddress.IsNullOrWhiteSpace() )
            {
                btnInactivateDevice.Visible = false;
            }
        }

        protected void lbAddDevice_Click( object sender, EventArgs e )
        {
            EditDevice( 0 );
        }

        protected void btnEditDevice_Command( object sender, CommandEventArgs e )
        {
            EditDevice( ( ( string ) e.CommandArgument ).AsInteger() );
        }

        protected void btnInactivateDevice_Command( object sender, CommandEventArgs e )
        {
            RockContext rockContext = new RockContext();
            PersonalDeviceService personalDeviceService = new PersonalDeviceService( rockContext );
            var personalDeviceId = ( ( string ) e.CommandArgument ).AsInteger();
            var personalDevice = personalDeviceService.Get( personalDeviceId );
            if ( personalDevice != null )
            {
                RemoveDeviceFromFP( personalDevice.MACAddress );
                RemoveDeviceMAC( personalDevice, rockContext );
            }
            BindRepeater();
        }
        #endregion

        #region Methods

        private void EditDevice( int personalDeviceId )
        {
            var rockContext = new RockContext();
            var personalDeviceService = new PersonalDeviceService( rockContext );
            var personalDevice = personalDeviceService.Get( personalDeviceId );

            if ( personalDevice == null )
            {
                personalDevice = new PersonalDevice();
                mdEdit.Title = "Add Device";
                //default to the current context person
                ppPerson.SetValue( _person );
            }
            else
            {
                mdEdit.Title = "Edit Device";
                ppPerson.SetValue( personalDevice.PersonAlias.Person );
            }

            tbdeviceregistration.Text = personalDevice.DeviceRegistrationId;
            var notifications = personalDevice.NotificationsEnabled;
            if ( notifications )
            {
                rblnotifications.SetValue( "true" );
            }
            else
            {
                rblnotifications.SetValue( "false" );
            }

            ddldevicetype.BindToDefinedType( DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSONAL_DEVICE_TYPE ), true );
            ddldevicetype.SetValue( personalDevice.PersonalDeviceTypeValueId );
            ddlplatform.BindToDefinedType( DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSONAL_DEVICE_PLATFORM ), true );
            ddlplatform.SetValue( personalDevice.PlatformValueId );
            tbdeviceuniqueid.Text = personalDevice.DeviceUniqueIdentifier;
            tbdeviceversion.Text = personalDevice.DeviceVersion;
            tbmaddress.Text = personalDevice.MACAddress;
            hfDeviceId.SetValue( personalDeviceId );
            mdEdit.Show();
        }

        private void UpdateMAC( string newMAC, string oldMAC )
        {
            RemoveDeviceFromFP( oldMAC ); //Remove the previous device from FP
            AddDeviceToFP( newMAC );

            //Remove mac address from any previous devices in our system
            RockContext rockContext = new RockContext(); //new rockcontext so we can save freely
            PersonalDeviceService personalDeviceService = new PersonalDeviceService( rockContext );
            var personalDevices = personalDeviceService
                .Queryable()
                .Where( d => d.MACAddress == newMAC )
                .ToList();

            foreach ( var device in personalDevices )
            {
                RemoveDeviceMAC( device, rockContext );
            }
        }

        private void RemoveDeviceMAC( PersonalDevice device, RockContext rockContext )
        {
            device.LoadAttributes();
            //Archive the mac address
            device.SetAttributeValue( "ArchivedMACAddress", device.MACAddress );
            device.MACAddress = "";
            device.SaveAttributeValues();
            rockContext.SaveChanges();
        }

        private bool RemoveDeviceFromFP( string macAddress )
        {
            //Remove previously held MAC from FP 
            if ( macAddress.IsNotNullOrWhiteSpace() )
            {
                var url = string.Format( "https://{0}/api/user/delete?mac={1}", fpHost, macAddress );
                HttpWebRequest request = ( HttpWebRequest ) WebRequest.Create( url );
                request.Headers.Add( fpAuthenticationHeader );
                try
                {
                    HttpWebResponse response = ( HttpWebResponse ) request.GetResponse();
                    if ( response.StatusCode != HttpStatusCode.OK )
                    {
                        return false;
                    }
                }
                catch ( Exception e )
                {
                    LogException( e );
                    return false;
                }
            }
            return true;
        }

        private bool AddDeviceToFP( string macAddress )
        {
            var url = string.Format( "https://{0}/api/user/add?mac={1}&fpid={2}", fpHost, macAddress, ppPerson.PersonAliasId );

            HttpWebRequest request = ( HttpWebRequest ) WebRequest.Create( url );
            request.Headers.Add( fpAuthenticationHeader );
            try
            {
                HttpWebResponse response = ( HttpWebResponse ) request.GetResponse();
                Stream resStream = response.GetResponseStream();
                if ( response.StatusCode != HttpStatusCode.OK )
                {
                    return false;
                }
            }
            catch ( Exception e )
            {
                LogException( e );
                return false;
            }
            return true;
        }


        #endregion

        #region Helper Classes

        /// <summary>
        /// A class to store personal device for lava
        /// </summary>
        [DotLiquid.LiquidType( "PersonalDevice", "DeviceIconCssClass", "PlatformValue" )]
        public class PersonalDeviceItem
        {
            /// <summary>
            /// Gets or sets the personal device.
            /// </summary>
            /// <value>
            /// The personal device.
            /// </value>
            public PersonalDevice PersonalDevice { get; set; }

            /// <summary>
            /// Gets or sets the device iconCssClass.
            /// </summary>
            /// <value>
            /// The device iconCssClass.
            /// </value>
            public string DeviceIconCssClass { get; set; }

            /// <summary>
            /// Gets or sets the platform value.
            /// </summary>
            /// <value>
            /// The platform value.
            /// </value>
            public string PlatformValue { get; set; }
        }

        #endregion

    }
}