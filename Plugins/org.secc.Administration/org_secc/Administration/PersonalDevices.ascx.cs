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
<div class=""panel panel-block"">       
    <div class=""panel-heading"">
        <h4 class=""panel-title"">
            <div class=""panel-panel-title""><i class=""fa fa-mobile""></i> {{ Person.FullName }}</div>
        </h4>
    <a class=""pull-right btn btn-xs btn-primary"" href=""#"" onclick=""Rock.dialogs.confirm('Are you sure you want to add a Device?', function (result) { if (result ) setTimeout(function() { {{ Person.Guid | Postback:'AddDevice' }} }, 1) })""><i class=""fa fa-plus""></i></a>
</div>
    <div class=""panel-body"">
        <div class=""row display-flex"">
            {% for item in PersonalDevices %}
                <div class=""col-md-3 col-sm-4"">                  
                    <div class=""well margin-b-xs rollover-container"">                        
                        <a class=""pull-right rollover-item btn btn-xs btn-danger"" href=""#"" onclick=""Rock.dialogs.confirm('Are you sure you want to delete this Device?', function (result) { if (result ) setTimeout(function() { {{ item.PersonalDevice.Id | Postback:'DeleteDevice' }} }, 1) })""><i class=""fa fa-times""></i></a>
                        <a class=""pull-right rollover-item btn btn-xs btn-danger"" href=""#"" onclick=""setTimeout(function() { {{ item.PersonalDevice.Id | Postback:'EditDevice' }} }, 1)""><i class=""fa fa-edit""></i></a>                     
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
                        {% if LinkUrl != '' %}
                            <a href=""{{ LinkUrl | Replace:'[Id]',item.PersonalDevice.Id }}"" class=""btn btn-default btn-xs""> Interactions</a>
                        {% endif %}
                    </div>
                </div>
            {% endfor %}
        </div>
    </div>
</div>
", "", 2, "LavaTemplate" )]
    [BooleanField( "Remove Device", "if true, the specific personal device will be removed from front porch", true )]
    //  [TextField( "Host", "The host for the frontporch instance we want to connect to", required: false )]
    //  [TextField( "API Authorization Token", "The token used for deleting the device", required: false )]
    [ContextAware( typeof( Person ) )]

    public partial class PersonalDevices : RockBlock
    {
        #region Fields

        private Person _person = null;

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
            RouteAction();
            if ( !Page.IsPostBack )
            {
                LoadContent();
            }
            base.OnLoad( e );
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
            RouteAction();
            LoadContent();
        }

        protected void mdDialog_SaveClick( object sender, EventArgs e )
        {
            if ( mdDialog.Title == "Edit Device" )
            {
                var rockContext = new RockContext();
                var personalDeviceService = new PersonalDeviceService( rockContext );
                var personalDevice = personalDeviceService.Get( hField.ValueAsInt() );
                if ( tbalias.Text != "" )
                {
                    personalDevice.PersonAliasId = int.Parse( tbalias.Text );
                }

                if ( tbdeviceregistration.Text != "" )
                {
                    personalDevice.DeviceRegistrationId = tbdeviceregistration.Text;
                }

                if ( rblnotifications.SelectedValue.AsBoolean() == true )
                {
                    personalDevice.NotificationsEnabled = true;
                }
                else
                {
                    personalDevice.NotificationsEnabled = false;
                }

                personalDevice.PersonalDeviceTypeValueId = ddldevicetype.SelectedValueAsInt();
                personalDevice.PlatformValueId = ddlplatform.SelectedValueAsInt();
                if ( tbdeviceuniqueid.Text != "" )
                {
                    personalDevice.DeviceUniqueIdentifier = tbdeviceuniqueid.Text;
                }

                if ( tbdeviceversion.Text != "" )
                {
                    personalDevice.DeviceVersion = tbdeviceversion.Text;
                }

                if ( tbmaddress.Text != "" )
                {
                    var authToken = GlobalAttributesCache.Value( "FrontporchAPIToken" );
                    var hostAddr = GlobalAttributesCache.Value( "FrontporchHost" );
                    var authentication = string.Format( "authorization-token:{0}", authToken );
                    var url = string.Format( "https://{0}/api/user/delete?mac={1}", hostAddr, personalDevice.MACAddress );
                    personalDevice.MACAddress = tbmaddress.Text;

                    if ( authToken.IsNotNullOrWhiteSpace() && hostAddr.IsNotNullOrWhiteSpace() )
                    {
                        HttpWebRequest request = ( HttpWebRequest ) WebRequest.Create( url );
                        request.Headers.Add( authentication );
                        try
                        {
                            HttpWebResponse response = ( HttpWebResponse ) request.GetResponse();
                            Stream resStream = response.GetResponseStream();
                            StreamReader reader = new StreamReader( resStream );
                            string text = reader.ReadToEnd();
                            if ( text == "OK" )
                            {
                                Literal1.Text = string.Format( "<div class='alert alert-success'>{0}", text );
                            }
                        }
                        catch
                        {
                            Literal1.Text = "<div class='alert alert-warning'>Unknown";
                        }
                    }
                }
                rockContext.SaveChanges();
                mdDialog.Hide();
                LoadContent();
            }
            if ( mdDialog.Title == "Add Device" )
            {
                var rockContext = new RockContext();
                var personaliasservice = new PersonAliasService( rockContext );
                var personalias = personaliasservice.GetByAliasGuid( hField.Value.AsGuid() );
                var notificationsenabled = true;
                if ( rblnotifications.SelectedValue.AsBoolean() == true )
                {
                    notificationsenabled = true;
                }
                else
                {
                    notificationsenabled = false;
                }

                Guid guid = Guid.NewGuid();
                PersonalDevice p = new PersonalDevice
                {
                    PersonAliasId = personalias.Id,
                    DeviceRegistrationId = tbdeviceregistration.Text,
                    NotificationsEnabled = notificationsenabled,
                    Guid = guid,
                    PersonalDeviceTypeValueId = ddldevicetype.SelectedValueAsInt(),
                    PlatformValueId = ddlplatform.SelectedValueAsInt(),
                    DeviceUniqueIdentifier = tbdeviceuniqueid.Text,
                    DeviceVersion = tbdeviceversion.Text,
                    MACAddress = tbmaddress.Text
                };
                var personalDeviceService = new PersonalDeviceService( rockContext );
                personalDeviceService.Add( p );
                var authToken = GlobalAttributesCache.Value( "APIAuthorizationToken" );
                var hostAddr = GlobalAttributesCache.Value( "Host" );
                var authentication = string.Format( "authorization-token:{0}", authToken );
                var url = string.Format( "https://{0}/api/user/add?mac={1}&fpid={2}", hostAddr, tbmaddress.Text, personalias.Id );
                if ( authToken.IsNotNullOrWhiteSpace() && hostAddr.IsNotNullOrWhiteSpace() )
                {
                    HttpWebRequest request = ( HttpWebRequest ) WebRequest.Create( url );
                    request.Headers.Add( authentication );
                    try
                    {
                        HttpWebResponse response = ( HttpWebResponse ) request.GetResponse();
                        Stream resStream = response.GetResponseStream();
                        StreamReader reader = new StreamReader( resStream );
                        string text = reader.ReadToEnd();
                        if ( text == "OK" )
                        {
                            Literal1.Text = string.Format( "<div class='alert alert-success'>{0}", text );
                        }

                    }
                    catch
                    {
                        Literal1.Text = "<div class='alert alert-warning'>Unknown";
                    }
                }
                rockContext.SaveChanges();
                mdDialog.Hide();
                LoadContent();

            }
        }



        #endregion

        #region Methods

        protected void LoadContent()
        {
            if ( _person != null )
            {
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                mergeFields.Add( "Person", _person );

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

                mergeFields.Add( "PersonalDevices", items );

                var queryParams = new Dictionary<string, string>();
                queryParams.Add( "personalDeviceId", "_PersonDeviceIdParam_" );
                string url = LinkedPageUrl( "InteractionsPage", queryParams );
                if ( !string.IsNullOrWhiteSpace( url ) )
                {
                    url = url.Replace( "_PersonDeviceIdParam_", "[Id]" );
                }

                mergeFields.Add( "LinkUrl", url );


                string template = GetAttributeValue( "LavaTemplate" );
                lContent.Text = template.ResolveMergeFields( mergeFields ).ResolveClientIds( upnlContent.ClientID );
            }
            else
            {
                lContent.Text = string.Format( "<div class='alert alert-warning'>No Person is selected." );
            }
        }

        /// <summary>
        /// Route the request to the correct panel
        /// </summary>
        private void RouteAction()
        {
            int personalDeviceId = 0;
            string aliaspersonguid = null;
            var sm = ScriptManager.GetCurrent( Page );

            if ( Request.Form["__EVENTARGUMENT"] != null )
            {
                string[] eventArgs = Request.Form["__EVENTARGUMENT"].Split( '^' );
                string action = eventArgs[0];

                if ( eventArgs.Length == 2 )
                {
                    string parameters = eventArgs[1];
                    int argument = 0;
                    int.TryParse( parameters, out argument );

                    switch ( action )
                    {
                        case "AddDevice":
                            aliaspersonguid = parameters;
                            AddDevice( aliaspersonguid );
                            break;
                        case "DeleteDevice":
                            personalDeviceId = int.Parse( parameters );
                            DeleteDevice( personalDeviceId );
                            break;
                        case "EditDevice":
                            personalDeviceId = int.Parse( parameters );
                            EditDevice( personalDeviceId );
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Deletes the personal device.
        /// </summary>
        /// <param name="personalDeviceId">The personal device identifier.</param>
        private void DeleteDevice( int personalDeviceId )
        {
            var rockContext = new RockContext();
            var interactionService = new InteractionService( rockContext );
            var personalDeviceService = new PersonalDeviceService( rockContext );
            var personalDevice = personalDeviceService.Get( personalDeviceId );

            if ( personalDevice != null )
            {
                if ( Convert.ToBoolean( GetAttributeValue( "RemoveDevice" ) ) )
                {
                    // Check to see if a mac address exists, if so, delete from front Porch
                    var macaddress = personalDevice.MACAddress;

                    if ( macaddress != null )
                    {
                        var authToken = GlobalAttributesCache.Value( "APIAuthorizationToken" );
                        var hostAddr = GlobalAttributesCache.Value( "Host" );
                        var authentication = string.Format( "authorization-token:{0}", authToken );
                        var url = string.Format( "https://{0}/api/user/delete?mac={1}", hostAddr, personalDevice.MACAddress );
                        if ( authToken.IsNotNullOrWhiteSpace() && hostAddr.IsNotNullOrWhiteSpace() )
                        {
                            HttpWebRequest request = ( HttpWebRequest ) WebRequest.Create( url );
                            request.Headers.Add( authentication );
                            try
                            {
                                HttpWebResponse response = ( HttpWebResponse ) request.GetResponse();
                                Stream resStream = response.GetResponseStream();
                                StreamReader reader = new StreamReader( resStream );
                                string text = reader.ReadToEnd();
                                if ( text == "OK" )
                                {
                                    Literal1.Text = string.Format( "<div class='alert alert-success'>{0}", text );
                                }
                            }
                            catch
                            {
                                Literal1.Text = "<div class='alert alert-warning'>Unknown";
                            }
                        }
                    }
                }
                /*var interactions = interactionService.Queryable( "PersonalDevice" )
                    .Where( a => a.PersonalDeviceId == personalDeviceId )
                    .ToList();

                if ( interactions.Count > 0 )
                {
                foreach ( var interaction in interactions )
                {
                interaction.PersonalDevice = null;
                }
                }
                personalDeviceService.Delete( personalDevice );*/

                if ( personalDevice.MACAddress != null )
                {
                    
                    personalDevice.LoadAttributes();
                    if ( personalDevice.Attributes.ContainsKey( "ArchivedMACAddress" ) )
                    {
                        personalDevice.SetAttributeValue( "ArchivedMACAddress", personalDevice.MACAddress );
                    }
                    personalDevice.MACAddress = null;
                    personalDevice.SaveAttributeValues( rockContext );
                    rockContext.SaveChanges();
                    
                }

                
            }
            LoadContent();
        }

        private void EditDevice( int personalDeviceId )
        {
            var rockContext = new RockContext();
            var personalDeviceService = new PersonalDeviceService( rockContext );
            var personalDevice = personalDeviceService.Get( personalDeviceId );
            tbalias.Text = personalDevice.PersonAliasId.ToString();
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
            hField.SetValue( personalDeviceId );
            mdDialog.Title = "Edit Device";
            mdDialog.Show();
        }

        private void AddDevice( string aliaspersonguid )
        {
            var rockContext = new RockContext();
            var personalDevice = new PersonalDevice();
            rblnotifications.ClearSelection();
            tbdeviceversion.Text = "";
            rblnotifications.SetValue( "false" );
            tbmaddress.Text = "";
            ddldevicetype.BindToDefinedType( DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSONAL_DEVICE_TYPE ), true );
            ddlplatform.BindToDefinedType( DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSONAL_DEVICE_PLATFORM ), true );
            hField.Value = aliaspersonguid;
            mdDialog.Title = "Add Device";
            mdDialog.Show();
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
    }
    #endregion

}