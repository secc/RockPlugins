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
using org.secc.FamilyCheckin.Data;
using org.secc.FamilyCheckin.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.FamilyCheckin
{
    [DisplayName( "Kiosk Type List" )]
    [Category( "SECC > Check-in" )]
    [Description( "Lists all the kiosk types." )]

    [LinkedPage("Detail Page")]
    public partial class KioskTypeList : RockBlock
    { 
        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            
            gKioskTypes.DataKeyNames = new string[] { "Id" };
            gKioskTypes.Actions.ShowAdd = true;
            gKioskTypes.Actions.AddClick += gKioskType_Add;
            gKioskTypes.GridRebind += gKioskType_GridRebind;

            // Block Security and special attributes (RockPage takes care of View)
            bool canAddEditDelete = IsUserAuthorized( Authorization.EDIT );
            gKioskTypes.Actions.ShowAdd = canAddEditDelete;
            gKioskTypes.IsDeleteEnabled = canAddEditDelete;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                BindGrid();
            }

            base.OnLoad( e );
        }

        #endregion

        #region Grid Events (main grid)

        protected void gKioskType_Add( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "KioskTypeId", 0 );
        }


        protected void gKioskType_Edit( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "KioskTypeId", e.RowKeyId );
        }

       
        protected void gKioskType_Delete( object sender, RowEventArgs e )
        {
            var checkinContext = new FamilyCheckinContext();
            KioskTypeService KioskTypeService = new KioskTypeService( checkinContext );
            KioskType KioskType = KioskTypeService.Get( e.RowKeyId );

            if ( KioskType != null )
            {
                int kosktypeId = KioskType.Id;

                string errorMessage;
                if ( !KioskTypeService.CanDelete( KioskType, out errorMessage ) )
                {
                    mdGridWarning.Show( errorMessage, ModalAlertType.Information );
                    return;
                }

                KioskTypeService.Delete( KioskType );
                checkinContext.SaveChanges();

                Rock.CheckIn.KioskDevice.Flush( kosktypeId );
            }

            BindGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gDevice control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void gKioskType_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        #region Internal Methods
        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            var kioskTypeService = new KioskTypeService( new FamilyCheckinContext() );
            var kioskTypes = kioskTypeService.Queryable().Select( kt => kt ).ToList();

            gKioskTypes.DataSource = kioskTypes;
            gKioskTypes.DataBind();
        }

        #endregion
    }
}