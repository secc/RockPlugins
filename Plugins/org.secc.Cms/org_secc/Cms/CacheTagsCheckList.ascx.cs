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
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Attribute;
using System.Collections.Generic;
using Rock;
using Rock.Web.Cache;
using System.Text;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Cms
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Cache Tags Check List" )]
    [Category( "SECC > CMS" )]
    [Description( "A simple block to list checkboxes to clear cache" )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.CACHE_TAGS, "Cache Tags", "Cached tags are used to link cached content so that it can be expired as a group", allowMultiple:true )]
  
    public partial class CacheTagsCheckList : RockBlock
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

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
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
                GetCacheTags();
                LoadPreferences();
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

        protected void btnClearCache_Click( object sender, EventArgs e )
        {
            var checkboxempty = cbl.SelectedValues;

            if ( checkboxempty.Count>0 )
            {
                RockCache.RemoveForTags( GetSelectedValues() );
                DisplayNotification( nbMessage, string.Format( "Removed cached items tagged with \"{0}\".", GetSelectedValues() ), NotificationBoxType.Success );
            }
            else
            {
                DisplayNotification( nbMessage, string.Format( "No checkbox selected" ), NotificationBoxType.Warning );
            }
            Save();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Displays the notification.
        /// </summary>
        /// <param name="notificationBox">The notification box.</param>
        /// <param name="message">The message.</param>
        /// <param name="notificationBoxType">Type of the notification box.</param>
        protected void DisplayNotification( NotificationBox notificationBox, string message, NotificationBoxType notificationBoxType )
        {
            notificationBox.Text = message;
            notificationBox.NotificationBoxType = notificationBoxType;
            notificationBox.Visible = true;
        }

        protected string GetSelectedValues()
        {
            var checkboxselected = cbl.SelectedValues;
            return string.Join( ",", checkboxselected );
        }

        protected void GetCacheTags()
        {
            var cachetags = GetAttributeValues( "CacheTags" );

            foreach ( var tag in cachetags )
            {
                cbl.Items.Add( DefinedValueCache.Get( tag.AsGuid() ).Value );

            }           
        }

        protected void Save()
        {
            SetUserPreference( "checkbox-selected-values-preference", GetSelectedValues() );
        }

        protected void LoadPreferences()
        {
            string usersselectedvalues = GetUserPreference( "checkbox-selected-values-preference" );
            IEnumerable<string> setvalues = usersselectedvalues.Split( ',' ).ToList();
            cbl.SetValues( setvalues );
        }

        #endregion
    }
}
