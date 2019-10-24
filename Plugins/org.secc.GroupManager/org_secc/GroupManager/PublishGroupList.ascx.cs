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
using org.secc.GroupManager.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.GroupManager
{
    [DisplayName( "Publish Group List" )]
    [Category( "SECC > Groups" )]
    [Description( "List of publish groups" )]

    [LinkedPage( "Publish Group Detail Page" )]

    public partial class PublishGroupList : RockBlock
    {

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summarysni>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                SetFilters();

                BindGrid();
            }
        }

        private void SetFilters()
        {
            cblStatus.BindToEnum<PublishGroupStatus>();
            PersonService personService = new PersonService( new RockContext() );
            pContactPerson.SetValue( personService.Get( GetBlockUserPreference( "ContactPersonId" ).AsInteger() ) );
            cblStatus.SetValues( GetBlockUserPreference( "PublishGroupStatus" ).SplitDelimitedValues() );
        }

        private void BindGrid()
        {
            RockContext rockContext = new RockContext();
            PublishGroupService publishGroupService = new PublishGroupService( rockContext );

            var qry = publishGroupService.Queryable();

            if ( pContactPerson.SelectedValue.HasValue )
            {
                var contactPersonId = pContactPerson.SelectedValue.Value;
                qry = qry.Where( p => p.ContactPersonAlias.PersonId == contactPersonId );
            }

            if ( cblStatus.SelectedValues.Count != 0 )
            {
                var selectedItems = cblStatus.SelectedValues.Select( i => ( PublishGroupStatus ) i.AsInteger() );
                qry = qry.Where( p => selectedItems.Contains( p.PublishGroupStatus ) );
            }

            gGroups.DataSource = qry
                .OrderBy( p => p.PublishGroupStatus )
                .ThenByDescending( p => p.StartDateTime )
                .ToList();
            gGroups.DataBind();
        }

        protected void gGroups_RowSelected( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            NavigateToLinkedPage( "PublishGroupDetailPage", new Dictionary<string, string> { { "PublishGroupId", e.RowKeyValue.ToString() } } );

        }

        protected void fGroups_ApplyFilterClick( object sender, EventArgs e )
        {
            SetBlockUserPreference( "ContactPersonId", pContactPerson.SelectedValue.ToString() );
            SetBlockUserPreference( "PublishGroupStatus", string.Join( ",", cblStatus.SelectedValues ) );
            BindGrid();
        }
    }
}