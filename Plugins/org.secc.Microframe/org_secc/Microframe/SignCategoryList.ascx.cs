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
//

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
    [DisplayName( "Sign Category List" )]
    [Category( "SECC > Microframe" )]
    [Description( "Lists all the sign categories." )]

    [LinkedPage( "Detail Page" )]
    public partial class SignCategoryList : RockBlock
    {
        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gSignCategories.DataKeyNames = new string[] { "Id" };
            gSignCategories.Actions.ShowAdd = true;
            gSignCategories.Actions.AddClick += gSignCategories_Add;
            gSignCategories.GridRebind += gSigns_GridRebind;

            // Block Security and special attributes (RockPage takes care of View)
            bool canAddEditDelete = IsUserAuthorized( Authorization.EDIT );
            gSignCategories.Actions.ShowAdd = canAddEditDelete;
            gSignCategories.IsDeleteEnabled = canAddEditDelete;
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

        protected void gSignCategories_Add( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "SignCategoryId", 0 );
        }


        protected void gSignCategories_Edit( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "SignCategoryId", e.RowKeyId );
        }


        protected void gSignCategories_Delete( object sender, RowEventArgs e )
        {
            var rockContext = new RockContext();
            SignCategoryService signCategoryService = new SignCategoryService( rockContext );
            SignCategory signCategory = signCategoryService.Get( e.RowKeyId );

            if ( signCategory != null )
            {

                signCategoryService.Delete( signCategory );
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
            var signCategoryService = new SignCategoryService( new RockContext() );

            var signCategories = signCategoryService.Queryable().Select( s => s ).OrderBy( s => s.Name ).ToList();
            


            gSignCategories.DataSource = signCategories;
            gSignCategories.DataBind();
        }

        #endregion
    }
}