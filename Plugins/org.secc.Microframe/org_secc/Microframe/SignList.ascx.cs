using System;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using org.secc.Microframe.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Microframe
{
    [DisplayName( "Sign List" )]
    [Category( "SECC > Microframe" )]
    [Description( "Lists all the Signs." )]

    [LinkedPage( "Detail Page" )]
    public partial class SignList : RockBlock
    {
        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );


            gSigns.DataKeyNames = new string[] { "Id" };
            gSigns.Actions.ShowAdd = true;
            gSigns.Actions.AddClick += gSigns_Add;
            gSigns.GridRebind += gSigns_GridRebind;

            // Block Security and special attributes (RockPage takes care of View)
            bool canAddEditDelete = IsUserAuthorized( Authorization.EDIT );
            gSigns.Actions.ShowAdd = canAddEditDelete;
            gSigns.IsDeleteEnabled = canAddEditDelete;
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

        protected void gSigns_Add( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "SignId", 0 );
        }


        protected void gSigns_Edit( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "SignId", e.RowKeyId );
        }


        protected void gSigns_Delete( object sender, RowEventArgs e )
        {
            var rockContext = new RockContext();
            SignService signService = new SignService( rockContext );
            Sign sign = signService.Get( e.RowKeyId );

            if ( sign != null )
            {

                signService.Delete( sign );
                rockContext.SaveChanges();
            }

            BindGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gDevice control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void gSigns_GridRebind( object sender, EventArgs e )
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
            var signService = new SignService( new RockContext() );

            var signs = signService.Queryable()
                .ToList()
                .Select( s => new
                {
                    Id = s.Id,
                    Name = s.Name,
                    IPAddress = s.IPAddress,
                    Categories = string.Join(", ", s.SignCategories)
                } )
                .OrderBy(s => s.Name).ToList();
            
            gSigns.DataSource = signs;
            gSigns.DataBind();
        }

        #endregion
    }
}