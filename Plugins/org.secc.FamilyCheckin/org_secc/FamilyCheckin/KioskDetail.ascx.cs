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
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using org.secc.FamilyCheckin.Model;
using org.secc.FamilyCheckin.Data;
using System.Data.Entity;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.FamilyCheckin
{
    [DisplayName( "Kiosk Detail" )]
    [Category( "SECC > Check-in" )]
    [Description( "Displays the details of the given device." )]
    public partial class KioskDetail : RockBlock, IDetailBlock
    {
        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

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
            Kiosk Kiosk = null;

            var rockContext = new RockContext();
            var kioskService = new KioskService( rockContext );
            var attributeService = new AttributeService( rockContext );

            int KioskId = int.Parse( hfKioskId.Value );

            if ( KioskId != 0 )
            {
                Kiosk = kioskService.Get( KioskId );
            }

            if ( Kiosk == null )
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
                    Kiosk = new Kiosk();
                    kioskService.Add( Kiosk );
                }
            }


            if ( Kiosk != null )
            {
                Kiosk.Name = tbName.Text;
                Kiosk.Description = tbDescription.Text;
                Kiosk.IPAddress = tbIPAddress.Text;

                if ( !Kiosk.IsValid || !Page.IsValid )
                {
                    // Controls will render the error messages
                    return;
                }

                //Save kiosk's checkin type
                Kiosk.KioskTypeId = ddlKioskType.SelectedValue.AsInteger();

                Kiosk.PrintToOverride = ( PrintTo ) System.Enum.Parse( typeof( PrintTo ), ddlPrintTo.SelectedValue );
                Kiosk.PrinterDeviceId = ddlPrinter.SelectedValueAsInt();
                Kiosk.PrintFrom = ( PrintFrom ) System.Enum.Parse( typeof( PrintFrom ), ddlPrintFrom.SelectedValue );


                rockContext.SaveChanges();

                Rock.CheckIn.KioskDevice.FlushAll( );

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
        public void ShowDetail( int KioskId )
        {
            pnlDetails.Visible = true;
            Kiosk Kiosk = null;

            var rockContext = new RockContext();

            if ( !KioskId.Equals( 0 ) )
            {
                Kiosk = new KioskService( rockContext ).Get( KioskId );
                lActionTitle.Text = ActionTitle.Edit( Kiosk.FriendlyTypeName ).FormatAsHtmlTitle();
            }

            if ( Kiosk == null )
            {
                Kiosk = new Kiosk { Id = 0 };
                lActionTitle.Text = ActionTitle.Add( Kiosk.FriendlyTypeName ).FormatAsHtmlTitle();
            }

            hfKioskId.Value = Kiosk.Id.ToString();

            tbName.Text = Kiosk.Name;
            tbDescription.Text = Kiosk.Description;
            tbIPAddress.Text = Kiosk.IPAddress;

            // render UI based on Authorized and IsSystem
            bool readOnly = false;

            nbEditModeMessage.Text = string.Empty;
            if ( !IsUserAuthorized( Authorization.EDIT ) )
            {
                readOnly = true;
                nbEditModeMessage.Text = EditModeMessage.ReadOnlyEditActionNotAllowed( Kiosk.FriendlyTypeName );
            }

            if ( readOnly )
            {
                lActionTitle.Text = ActionTitle.View( Kiosk.FriendlyTypeName );
                btnCancel.Text = "Close";
            }

            tbName.ReadOnly = readOnly;
            tbDescription.ReadOnly = readOnly;

            btnSave.Visible = !readOnly;
            BindDropDownList( Kiosk );
        }

        private void BindDropDownList( Kiosk kiosk = null )
        {
            RockContext rockContext = new RockContext();
            KioskTypeService kioskTypeService = new KioskTypeService( rockContext );
            
           
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