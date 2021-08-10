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
using System.Text;
using System.Web.UI;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Plugins.org_secc.Cms
{
    /// <summary>
    /// Block for Interactions Modal
    /// </summary>
    [DisplayName( "Interactions" )]
    [Category( "SECC > CMS" )]
    [Description( "A block to show interactions data." )]

    public partial class Interactions : RockBlock
    {
        #region Fields

        // used for private variables

        #endregion

        #region Properties



        #endregion

        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // NOTE: moment.js needs to be loaded before chartjs
            RockPage.AddScriptLink( "~/Scripts/moment.min.js", true );
            RockPage.AddScriptLink( "~/Scripts/Chartjs/Chart.js", true );


            // Define the name and type of the client script on the page.
            String csName = "VueJS";
            Type csType = this.GetType();

            // Get a ClientScriptManager reference from the Page class.
            ClientScriptManager cs = Page.ClientScript;

            // Check to see if the client script is already registered.
            if ( !cs.IsClientScriptBlockRegistered( csType, csName ) )
            {
                StringBuilder csText = new StringBuilder();
                csText.Append( "<script type=\"text/javascript\" src='https://unpkg.com/vue@next/dist/vue.global.js'></script>" );
                cs.RegisterClientScriptBlock( csType, csName, csText.ToString() );
            }

        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
            }
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

        }

        #endregion

        #region Methods
        protected void mdInteractions_SaveClick( object sender, EventArgs e )
        {
            mdInteractions.Hide();
        }
        protected void btnInteractionsModal_Click( object sender, EventArgs e )
        {
            mdInteractions.Show();
            ScriptManager.RegisterStartupScript( this.Page, this.Page.GetType(), "key1", string.Format( "modalScript({0})", RockPage.PageId ), true );
            mdInteractions.Title = "Visits & Visitors > (Page ID: " + RockPage.PageId.ToString() + ")";
        }



        #endregion
    }
}
