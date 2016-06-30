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
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using org.secc.FamilyCheckin.Model;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace RockWeb.Plugins.org_secc.FamilyCheckin
{
    [DisplayName( "AutoConfigure" )]
    [Category( "SECC > Check-in" )]
    [Description( "Checkin auto configure block" )]
    public partial class AutoConfigure : CheckInBlock
    {
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {

                AttemptKioskMatchByIpOrName();
            }
        }

        /// <summary>
        /// Attempts to match a known kiosk based on the IP address of the client.
        /// </summary>
        private void AttemptKioskMatchByIpOrName()
        {
            // try to find matching kiosk by REMOTE_ADDR (ip/name).
            using ( var rockContext = new RockContext() )
            {
                var kiosk = new KioskService( rockContext ).GetByIPAddress( Rock.Web.UI.RockPage.GetClientIpAddress() );
                if ( kiosk != null )
                {
                    DeviceService deviceService = new DeviceService( rockContext );
                    //Load matching device and update or create information
                    var device = deviceService.Queryable().Where( d => d.Name == kiosk.Name ).FirstOrDefault();
                    
                    //create new device to match our kiosk
                    if ( device == null )
                    {
                        device = new Device();
                        device.DeviceTypeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.DEVICE_TYPE_CHECKIN_KIOSK ).Id;
                        deviceService.Add( device );
                    }

                    device.Name = kiosk.Name;
                    device.IPAddress = kiosk.IPAddress;
                    device.Locations.Clear();
                    foreach(var loc in kiosk.KioskType.Locations.ToList())
                    {
                        device.Locations.Add( loc );
                    }
                    device.PrintFrom = kiosk.PrintFrom;
                    device.PrintToOverride = kiosk.PrintToOverride;
                    device.PrinterDeviceId = kiosk.PrinterDeviceId;
                    rockContext.SaveChanges();
                    CurrentKioskId = device.Id;
                    CurrentGroupTypeIds = kiosk.KioskType.GroupTypes.Select( gt => gt.Id ).ToList();

                    CurrentCheckinTypeId = kiosk.KioskType.CheckinTemplateId;

                    CurrentCheckInState = null;
                    CurrentWorkflow = null;
                    Session["KioskTypeId"] = kiosk.KioskType.Id;
                    SaveState();
                    NavigateToNextPage();
                }
            }
        }
    }
}