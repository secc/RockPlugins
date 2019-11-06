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
using System.Web.UI.WebControls;
using org.secc.GroupManager.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

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
              //.Where( pg => pg.Group!=null && pg.Group.Name != null );

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
        protected void btnLink_Click( object sender, EventArgs e )
        {
            LinkButton btn = ( LinkButton ) sender;
            GridViewRow gvr = ( GridViewRow ) btn.NamingContainer;
            var hfGroupId = ( HiddenField ) gvr.FindControl( "hfGroupId" );
            var groupId = hfGroupId.Value;
            NavigateToPage( Rock.SystemGuid.Page.GROUP_VIEWER.AsGuid(), new Dictionary<string, string> { { "GroupId", groupId } } );
        }

        protected void btnEdit_Click( object sender, EventArgs e )
        {
            LinkButton btn = ( LinkButton ) sender;
            GridViewRow gvr = ( GridViewRow ) btn.NamingContainer;
            var hfPublishGroupId = ( HiddenField ) gvr.FindControl( "hfPublishGroupId" );
            var publishGroupId = hfPublishGroupId.Value;
            NavigateToLinkedPage( "PublishGroupDetailPage", new Dictionary<string, string> { { "PublishGroupId", publishGroupId } } );
        }

        protected void btnDelete_Click( object sender, EventArgs e )
        {
            LinkButton btn = ( LinkButton ) sender;
            GridViewRow gvr = ( GridViewRow ) btn.NamingContainer;
            var hfPublishGroupId = ( HiddenField ) gvr.FindControl( "hfPublishGroupId" );
            var publishGroupId = hfPublishGroupId.Value.AsInteger();

            using ( RockContext rockContext = new RockContext() )
            {
                PublishGroupService publishGroupService = new PublishGroupService( rockContext );
                var publishGroup = publishGroupService.Get( publishGroupId );
                if ( publishGroup != null )
                {
                    if ( this.IsUserAuthorized( Rock.Security.Authorization.EDIT ) ||
                        publishGroup.ContactPersonAlias.Person.Id == CurrentPersonId ||
                        publishGroup.CreatedByPersonId == CurrentPersonId )
                    {
                        publishGroupService.Delete( publishGroup );
                        rockContext.SaveChanges();
                    }
                    else
                    {
                        maAlert.Show( "We are sorry, you are not permitted to delete this publish group.", ModalAlertType.Alert );
                    }
                }
            }
            BindGrid();
        }
    }
}