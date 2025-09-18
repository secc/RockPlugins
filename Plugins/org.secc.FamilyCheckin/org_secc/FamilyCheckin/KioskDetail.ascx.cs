﻿// <copyright>
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
using System;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using org.secc.FamilyCheckin.Model;
using org.secc.FamilyCheckin.Utilities;
using Rock;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.org_secc.FamilyCheckin
{
    [DisplayName( "Kiosk Detail" )]
    [Category( "SECC > Check-in" )]
    [Description( "Displays the details of the given device." )]
    public partial class KioskDetail : RockBlock
    {
        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            var kioskEntityId = EntityTypeCache.Get( typeof( Kiosk ) ).Id;
            pCategory.EntityTypeId = kioskEntityId;

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.AddConfigurationUpdateTrigger( upnlDevice );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            nbDuplicateKiosk.Visible = false;

            if ( !Page.IsPostBack )
            {
                ShowDetail( PageParameter( "KioskId" ).AsInteger() );
            }
        }

        #endregion

        #region Events


        protected void btnSave_Click( object sender, EventArgs e )
        {
            Kiosk kiosk = null;

            var rockContext = new RockContext();
            var kioskService = new KioskService( rockContext );
            var attributeService = new AttributeService( rockContext );

            int kioskId = int.Parse( hfKioskId.Value );

            if ( kioskId != 0 )
            {
                kiosk = kioskService.Get( kioskId );
            }

            if ( kiosk == null )
            {
                // Check for existing
                var existingDevice = kioskService.Queryable()
                    .Where( k => k.Name == tbName.Text )
                    .FirstOrDefault();
                if ( existingDevice != null )
                {
                    nbDuplicateKiosk.Text = string.Format( "A kiosk already exists with the name '{0}'. Please use a different device name.", existingDevice.Name );
                    nbDuplicateKiosk.Visible = true;
                }
                else
                {
                    kiosk = new Kiosk();
                    kioskService.Add( kiosk );
                }
            }


            if ( kiosk != null )
            {
                kiosk.Name = tbName.Text;
                kiosk.Description = tbDescription.Text;
                kiosk.IPAddress = tbIPAddress.Text;

                if ( !kiosk.IsValid || !Page.IsValid )
                {
                    // Controls will render the error messages
                    return;
                }

                kiosk.CategoryId = pCategory.SelectedValueAsId();

                //Save kiosk's checkin type
                kiosk.KioskTypeId = ddlKioskType.SelectedValue.AsInteger();

                kiosk.PrintToOverride = ( PrintTo ) System.Enum.Parse( typeof( PrintTo ), ddlPrintTo.SelectedValue );
                kiosk.PrinterDeviceId = ddlPrinter.SelectedValueAsInt();
                kiosk.PrintFrom = ( PrintFrom ) System.Enum.Parse( typeof( PrintFrom ), ddlPrintFrom.SelectedValue );

                if ( tbAccessKey.Text.IsNullOrWhiteSpace() )
                {
                    kiosk.AccessKey = Guid.NewGuid().ToString();
                }
                else
                {
                    kiosk.AccessKey = tbAccessKey.Text;
                }

                rockContext.SaveChanges();

                DeviceService deviceService = new DeviceService( rockContext );
                var devices = deviceService.Queryable().Where( d => d.Name == kiosk.Name ).ToList();
                foreach ( var device in devices )
                {
                    KioskDeviceHelpers.FlushItem( device.Id );
                }

                NavigateToParentPage();
            }
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            NavigateToParentPage();
        }

        #endregion

        #region Methods


        /// <summary>
        /// Shows the edit.
        /// </summary>
        /// <param name="DeviceId">The device identifier.</param>
        public void ShowDetail( int kioskId )
        {
            pnlDetails.Visible = true;
            Kiosk kiosk = null;

            var rockContext = new RockContext();

            if ( !kioskId.Equals( 0 ) )
            {
                kiosk = new KioskService( rockContext ).Get( kioskId );
                lActionTitle.Text = ActionTitle.Edit( Kiosk.FriendlyTypeName ).FormatAsHtmlTitle();
            }

            if ( kiosk == null )
            {
                kiosk = new Kiosk { Id = 0 };
                lActionTitle.Text = ActionTitle.Add( Kiosk.FriendlyTypeName ).FormatAsHtmlTitle();
            }

            hfKioskId.Value = kiosk.Id.ToString();

            tbName.Text = kiosk.Name;
            tbDescription.Text = kiosk.Description;
            tbIPAddress.Text = kiosk.IPAddress;

            // render UI based on Authorized and IsSystem
            bool readOnly = false;

            nbEditModeMessage.Text = string.Empty;
            if ( !IsUserAuthorized( Authorization.EDIT ) )
            {
                readOnly = true;
                nbEditModeMessage.Text = EditModeMessage.ReadOnlyEditActionNotAllowed( Kiosk.FriendlyTypeName );
            }

            pCategory.SetValue( kiosk.CategoryId );

            if ( readOnly )
            {
                lActionTitle.Text = ActionTitle.View( Kiosk.FriendlyTypeName );
                btnCancel.Text = "Close";
            }

            tbName.ReadOnly = readOnly;
            tbDescription.ReadOnly = readOnly;

            tbAccessKey.Text = kiosk.AccessKey;
            if ( tbAccessKey.Text.IsNullOrWhiteSpace() )
            {
                tbAccessKey.Text = Guid.NewGuid().ToString();
            }

            btnSave.Visible = !readOnly;
            BindDropDownList( kiosk );
        }

        private void BindDropDownList( Kiosk kiosk = null )
        {
            RockContext rockContext = new RockContext();
            CheckinKioskTypeService kioskTypeService = new CheckinKioskTypeService( rockContext );


            ddlKioskType.DataSource = kioskTypeService
                .Queryable()
                .OrderBy( t => t.Name )
                .Select( t => new
                {
                    t.Name,
                    t.Id
                } )
                .ToList();
            ddlKioskType.DataBind();


            ddlPrintFrom.BindToEnum<PrintFrom>();

            ddlPrinter.Items.Clear();
            ddlPrinter.DataSource = new DeviceService( new RockContext() )
                .GetByDeviceTypeGuid( new Guid( Rock.SystemGuid.DefinedValue.DEVICE_TYPE_PRINTER ) )
                .OrderBy( d => d.Name )
                .ToList();
            ddlPrinter.DataBind();
            ddlPrinter.Items.Insert( 0, new ListItem( None.Text, None.IdValue ) );

            if ( kiosk != null )
            {
                ddlKioskType.SetValue( kiosk.KioskTypeId );
                ddlPrintTo.SetValue( kiosk.PrintToOverride.ConvertToInt().ToString() );
                ddlPrinter.SetValue( kiosk.PrinterDeviceId );
                ddlPrintFrom.SetValue( kiosk.PrintFrom.ConvertToInt().ToString() );
            }

        }
        #endregion

        /// <summary>
        /// Handles when the Print To selection is changed.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlPrintTo_SelectedIndexChanged( object sender, EventArgs e )
        {
            SetPrinterVisibility();
        }


        /// <summary>
        /// Decide if the printer drop down list should be hidden.
        /// </summary>
        private void SetPrinterVisibility()
        {
            var printTo = ( PrintTo ) System.Enum.Parse( typeof( PrintTo ), ddlPrintTo.SelectedValue );
            ddlPrinter.Visible = printTo != PrintTo.Location;
        }
    }
}