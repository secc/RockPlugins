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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalanche;
using Avalanche.Attribute;
using Avalanche.Models;
using Newtonsoft.Json;
using org.secc.GroupManager.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.GroupManager
{
    [DisplayName( "Group Publish List" )]
    [Category( "SECC > Groups" )]
    [Description( "List of publish groups" )]

    [LinkedPage( "Publish Group Detail Page" )]

    public partial class GroupPublishList : RockBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summarysni>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                BindGrid();
            }
        }

        private void BindGrid()
        {
            RockContext rockContext = new RockContext();
            PublishGroupService publishGroupService = new PublishGroupService( rockContext );
            gGroups.DataSource = publishGroupService.Queryable().ToList();
            gGroups.DataBind();
        }

        protected void gGroups_RowSelected( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            NavigateToLinkedPage( "PublishGroupDetailPage", new Dictionary<string, string> { { "PublishGroupId", e.RowKeyValue.ToString() } } );

        }
    }
}