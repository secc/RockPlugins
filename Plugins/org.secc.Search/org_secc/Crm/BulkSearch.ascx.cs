using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Search
{
    [DisplayName( "Bulk Search" )]
    [Category( "SECC > Search" )]
    [Description( "Takes the ten two csv and puts it in a grid of people." )]
    public partial class BulkSearch : RockBlock
    {

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gGrid.GridRebind += GGrid_GridRebind;
        }

        private void GGrid_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindGrid();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>


        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
        }


        protected void btnNumbers_Click( object sender, EventArgs e )
        {
            BindGrid();
        }

        public void BindGrid()
        {
            RockContext rockContext = new RockContext();
            var phoneNumberService = new PhoneNumberService( rockContext );

            var numbers = tbNumbers.Text.Split( new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
            var people = phoneNumberService.Queryable().Where( ph => numbers.Contains( ph.Number ) && ph.NumberTypeValueId==12)
                .Select( pn => pn.Person )
                .DistinctBy( p => p.Id )
                .ToList();

            gGrid.DataSource = people;
            gGrid.PersonIdField = "Id";
            gGrid.DataBind();
        }
    }
}
