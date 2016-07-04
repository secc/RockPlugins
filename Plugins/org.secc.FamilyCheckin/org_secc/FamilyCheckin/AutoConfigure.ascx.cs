using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
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
            
                var ip = Rock.Web.UI.RockPage.GetClientIpAddress();

                ltDNS.Text = System.Net.Dns.GetHostEntry( ip ).HostName + " : " + ip;

                AttemptKioskMatchByIpOrName( ip );

                if ( string.IsNullOrWhiteSpace( GetAttributeValue( "NextPage" ) ) )
                {
                    nbNotConfigured.Visible = true;
                    nbNotFound.Visible = false;
                }


        }

        /// <summary>
        /// Attempts to match a known kiosk based on the IP address of the client.
        /// </summary>
        private void AttemptKioskMatchByIpOrName( string ip )
        {
            // try to find matching kiosk by REMOTE_ADDR (ip/name).
            using ( var rockContext = new RockContext() )
            {
                var kioskService = new KioskService( rockContext );
                var kiosk = kioskService.GetByIPAddress( ip );
                if ( kiosk != null )
                {
                    //if the kiosk exists but doesn't have a kiosk type, we can't to use it
                    if ( kiosk.KioskType != null )
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
                        foreach ( var loc in kiosk.KioskType.Locations.ToList() )
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
                else
                {
                    //Else make the kiosk so we don't have to manually
                    kiosk = new Kiosk();
                    kiosk.Name = System.Net.Dns.GetHostEntry( ip ).HostName;
                    kiosk.IPAddress = ip;
                    kiosk.Description = "Automatically created Kiosk";
                    kioskService.Add( kiosk );
                    rockContext.SaveChanges();
                }
            }
        }

        protected void Timer1_Tick( object sender, EventArgs e )
        {

        }
    }
}