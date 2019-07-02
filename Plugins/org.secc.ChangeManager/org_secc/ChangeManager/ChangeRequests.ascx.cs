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
using System;
using System.ComponentModel;
using Rock;
using Rock.Model;
using Rock.Security;
using System.Web.UI;
using Rock.Web.Cache;
using Rock.Web.UI;
using System.Web;
using Rock.Data;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using org.secc.ChangeManager.Model;
using System.Reflection;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using Rock.Attribute;

namespace RockWeb.Plugins.org_secc.ChangeManager
{
    [DisplayName( "Change Requests" )]
    [Category( "SECC > CRM" )]
    [Description( "Shows all requests" )]

    [LinkedPage( "Details Page", "Page which contains the details of the change request." )]
    public partial class ChangeRequests : Rock.Web.UI.RockBlock
    {
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gRequests.GridRebind += gRequests_GridRebind;
        }

        private void gRequests_GridRebind( object sender, Rock.Web.UI.Controls.GridRebindEventArgs e )
        {
            BindGrid();
        }


        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                var rockContext = new RockContext();
                var entityTypes = new EntityTypeService( rockContext ).GetEntities()
                    .OrderBy( t => t.FriendlyName )
                    .ToList();

                pEntityType.IncludeGlobalOption = false;
                pEntityType.EntityTypes = entityTypes;

                cbShowComplete.Checked = GetBlockUserPreference( "ShowComplete" ).AsBoolean();
                pEntityType.SelectedValue = GetBlockUserPreference( "EntityType" );

                BindGrid();
            }
        }

        private void BindGrid()
        {
            RockContext rockContext = new RockContext();
            ChangeRequestService changeRequestService = new ChangeRequestService( rockContext );
            var changeRequests = changeRequestService.Queryable();
            if ( !cbShowComplete.Checked )
            {
                changeRequests = changeRequests.Where( c => !c.IsComplete );
            }
            if ( pEntityType.SelectedEntityTypeId.HasValue && pEntityType.SelectedEntityTypeId.Value != 0 )
            {
                int entityTypeId = pEntityType.SelectedEntityTypeId.Value;

                //Special case for Person/Person Alias
                if ( entityTypeId == EntityTypeCache.Get( typeof( Person ) ).Id )
                {
                    entityTypeId = EntityTypeCache.Get( typeof( PersonAlias ) ).Id;
                }

                changeRequests = changeRequests.Where( c => c.EntityTypeId == entityTypeId );
            }

            changeRequests = changeRequests.OrderBy( c => c.IsComplete ).ThenByDescending( c => c.CreatedDateTime );
            gRequests.SetLinqDataSource( changeRequests );
            gRequests.DataBind();
        }

        protected void gRequests_RowSelected( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            NavigateToLinkedPage( "DetailsPage", new Dictionary<string, string> { { "ChangeRequest", e.RowKeyId.ToString() } } );
        }

        protected void gRequests_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                e.Row.Cells[1].Text = e.Row.Cells[1].Text.Split( '.' ).Last();
                if ( e.Row.Cells[1].Text == "PersonAlias" )
                {
                    e.Row.Cells[1].Text = "Person";
                }
            }
        }

        protected void fRequests_ApplyFilterClick( object sender, EventArgs e )
        {
            SetBlockUserPreference( "ShowComplete", cbShowComplete.Checked.ToString() );
            SetBlockUserPreference( "EntityType", pEntityType.SelectedValue );
            BindGrid();
        }
    }
}