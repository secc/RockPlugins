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
using System.Web.UI;
using org.secc.Widgities.Controls;
using Rock.Model;
using Rock.Web.Cache;

namespace RockWeb.Plugins.org.secc.Widgities
{
    [DisplayName( "Widgity Content" )]
    [Category( "SECC > Widgities" )]
    [Description( "Block for displaying widgities on a webpage" )]
    public partial class WidgityContent : Rock.Web.UI.RockBlockCustomSettings
    {

        WidgityControl widgityControl;

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            if ( Page.IsPostBack )
            {
                widgityControl = new WidgityControl
                {
                    ID = ID = this.ID + "_widgityControl",
                    EntityTypeId = EntityTypeCache.Get( typeof( Block ) ).Id,
                    EntityGuid = BlockCache.Guid,
                };
                phPlaceholder.Controls.Add( widgityControl );
            }
        }

        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );
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
                widgityControl = new WidgityControl
                {
                    ID = ID = this.ID + "_widgityControl",
                    EntityTypeId = EntityTypeCache.Get( typeof( Block ) ).Id,
                    EntityGuid = BlockCache.Guid,
                };
                phPlaceholder.Controls.Add( widgityControl );
                widgityControl.DataBind();
            }

        }

        protected override void ShowSettings()
        {
            if ( UserCanEdit )
            {
                widgityControl.ShowSettings();
            }
        }
    }
}