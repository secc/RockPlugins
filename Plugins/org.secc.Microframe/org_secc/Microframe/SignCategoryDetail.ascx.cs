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
using Rock;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;
using org.secc.Microframe.Model;

namespace RockWeb.Plugins.org_secc.Microframe
{
    [DisplayName( "Sign Category Detail" )]
    [Category( "SECC > Microframe" )]
    [Description( "Displays the details of the given sign category." )]
    public partial class SignCategoryDetail : RockBlock, IDetailBlock
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

            if ( !Page.IsPostBack )
            {
                ShowDetail( PageParameter( "SignCategoryId" ).AsInteger() );
            }
        }

        #endregion

        #region Events


        protected void btnSave_Click( object sender, EventArgs e )
        {
            SignCategory signCategory = null;

            var rockContext = new RockContext();
            var signCategoryService = new SignCategoryService( rockContext );
            var attributeService = new AttributeService( rockContext );

            int signCategoryId = int.Parse( hfSignCategoryId.Value );

            if ( signCategoryId != 0 )
            {
                signCategory = signCategoryService.Get( signCategoryId );
            }

            if ( signCategory == null )
            {
                // Check for existing
                var existingDevice = signCategoryService.Queryable()
                    .Where( k => k.Name == tbName.Text )
                    .FirstOrDefault();
                if ( existingDevice != null )
                {
                    nbDuplicateSign.Text = string.Format( "A sign category already exists with the name '{0}'. Please use a different device name.", existingDevice.Name );
                    nbDuplicateSign.Visible = true;
                }
                else
                {
                    signCategory = new SignCategory();
                    signCategoryService.Add( signCategory );
                }
            }


            if ( signCategory != null )
            {
                signCategory.Name = tbName.Text;
                signCategory.Description = tbDescription.Text;

                if ( !signCategory.IsValid || !Page.IsValid )
                {
                    // Controls will render the error messages
                    return;
                }

                rockContext.SaveChanges();

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
        public void ShowDetail( int signCategoryId )
        {
            pnlDetails.Visible = true;
            SignCategory signCategory = null;

            var rockContext = new RockContext();

            if ( !signCategoryId.Equals( 0 ) )
            {
                signCategory = new SignCategoryService( rockContext ).Get( signCategoryId );
                lActionTitle.Text = ActionTitle.Edit( SignCategory.FriendlyTypeName ).FormatAsHtmlTitle();
            }

            if ( signCategory == null )
            {
                signCategory = new SignCategory { Id = 0 };
                lActionTitle.Text = ActionTitle.Add( SignCategory.FriendlyTypeName ).FormatAsHtmlTitle();
            }

            hfSignCategoryId.Value = signCategory.Id.ToString();

            tbName.Text = signCategory.Name;
            tbDescription.Text = signCategory.Description;

            // render UI based on Authorized and IsSystem
            bool readOnly = false;

            nbEditModeMessage.Text = string.Empty;
            if ( !IsUserAuthorized( Authorization.EDIT ) )
            {
                readOnly = true;
                nbEditModeMessage.Text = EditModeMessage.ReadOnlyEditActionNotAllowed( SignCategory.FriendlyTypeName );
            }

            if ( readOnly )
            {
                lActionTitle.Text = ActionTitle.View( SignCategory.FriendlyTypeName );
                btnCancel.Text = "Close";
            }

            tbName.ReadOnly = readOnly;
            tbDescription.ReadOnly = readOnly;

            btnSave.Visible = !readOnly;
        }

        #endregion
    }
}
