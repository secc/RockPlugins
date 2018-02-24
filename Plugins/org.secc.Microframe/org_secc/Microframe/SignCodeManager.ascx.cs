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
using org.secc.Microframe.Utilities;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Microframe
{
    [DisplayName( "Sign Code Manager" )]
    [Category( "SECC > Microframe" )]
    [Description( "Manages all of the codes sent out." )]

    [LinkedPage( "Detail Page" )]
    public partial class SignCodeManager : RockBlock
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
            gSignCategories.GridRebind += gSigns_GridRebind;
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
            if ( hfSignCategory.ValueAsInt() != 0 )
            {
                ShowCategory( hfSignCategory.ValueAsInt() );
            }

            base.OnLoad( e );
        }

        #endregion

        #region Grid Events (main grid)


        protected void gSignCategories_Edit( object sender, RowEventArgs e )
        {
            ShowCategory( e.RowKeyId );
        }

        private void ShowCategory( int signCategoryId )
        {
            phCodes.Controls.Clear();
            SignCategory signCategory = new SignCategoryService( new RockContext() ).Get( signCategoryId );
            if ( signCategory == null )
            {
                return;
            }
            hfSignCategory.Value = signCategoryId.ToString();
            pnlCategory.Visible = true;
            ltCategory.Text = signCategory.Name;
            var codes = ( signCategory.Codes ?? "" ).Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
            if ( codes.Any() )
            {
                foreach(var code in codes )
                {
                    Panel pnlCode = new Panel();
                    pnlCode.CssClass = "codes";
                    phCodes.Controls.Add( pnlCode );

                    Literal ltCode = new Literal();
                    ltCode.Text = code + " ";
                    pnlCode.Controls.Add( ltCode );

                    LinkButton lbCode = new LinkButton();
                    lbCode.ID = code + signCategory.Id.ToString();
                    lbCode.Text = "<i class='fa fa-close'></i>";
                    lbCode.Click += ( s,e ) => RemoveCode( code, signCategoryId );
                    pnlCode.Controls.Add( lbCode );
                }
            }
            else
            {
                Literal ltEmpty = new Literal();
                ltEmpty.Text = "<i>This category currently contains no codes</i>";
                phCodes.Controls.Add( ltEmpty );
            }
        }

        private void RemoveCode( string code, int signCategoryId )
        {
            RockContext rockContext = new RockContext();
            SignCategory signCategory = new SignCategoryService( rockContext ).Get( signCategoryId );

            if ( signCategory != null )
            {
                var codes = ( signCategory.Codes ?? "" ).Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
                if ( codes.Contains( code ) )
                {
                    codes.Remove( code );
                    signCategory.Codes = string.Join( ",", codes );
                    rockContext.SaveChanges();
                    UpdateSigns( signCategoryId );
                }
                ShowCategory( signCategoryId );
                BindGrid();
            }
            else
            {
                pnlCategory.Visible = false;
                return;
            }
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

            var signCategories = signCategoryService.Queryable()
                .Select( sc => sc )
                .OrderBy( s => s.Name )
                .ToList()
                .Select( sc => new
                {
                    Id = sc.Id,
                    Name = sc.Name,
                    Count = ( ( sc.Codes ?? "" ).Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ) ).Count().ToString()
                }
                ).ToList();


            gSignCategories.DataSource = signCategories;
            gSignCategories.DataBind();
        }

        #endregion

        protected void btnAdd_Click( object sender, EventArgs e )
        {
            var signCategoryId = hfSignCategory.Value.AsInteger();
            RockContext rockContext = new RockContext();
            SignCategory signCategory = new SignCategoryService( rockContext ).Get( signCategoryId );
            var code = tbCode.Text.Trim().ToUpper();
            tbCode.Text = "";
            if ( string.IsNullOrWhiteSpace( code ) )
            {
                return;
            }
            if ( code.Length > 4 )
            {
                code = code.Substring( 0, 4 );
            }

            if ( signCategory != null )
            {
                var codes = ( signCategory.Codes ?? "" ).Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
                if ( !codes.Contains( code ) )
                {
                    codes.Add( code );
                    signCategory.Codes = string.Join( ",", codes );
                    rockContext.SaveChanges();
                    UpdateSigns( signCategoryId );
                }
                ShowCategory( signCategoryId );
                BindGrid();
            }
            else
            {
                pnlCategory.Visible = false;
                return;
            }
        }

        private void UpdateSigns(int signCategoryId)
        {
            SignUtilities.UpdateSignCategorySigns( signCategoryId );
        }
    }
}