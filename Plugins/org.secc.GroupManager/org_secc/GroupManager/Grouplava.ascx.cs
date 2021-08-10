﻿// <copyright>
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.GroupManager
{
    /// <summary>
    /// Block for people to find a home group that matches their filters.
    /// </summary>
    [DisplayName( "Group Lava" )]
    [Category( "SECC > Groups" )]
    [Description( "Block to output groups in lava" )]
    [GroupTypeField( "Group Type" )]
    [CodeEditorField( "Lava Template", "Lava to display groups", CodeEditorMode.Lava )]

    public partial class GroupLava : Rock.Web.UI.RockBlock
    {
        #region Fields

        // used for private variables

        #endregion

        #region Properties

        // used for public / protected properties

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                ShowGroups();
            }
        }

        protected void ShowGroups()
        {
            RockContext rockContext = new RockContext();
            var groupTypeGuid = GetAttributeValue( "GroupType" ).AsGuid();
            var groupTypeId = new GroupTypeService( rockContext ).Get( groupTypeGuid ).Id;
            var groupTypeIdString = groupTypeId.ToString();
            var groupEntityTypeId = EntityTypeCache.Get( typeof( Group ) ).Id;
            var groupAttributesQry = new AttributeService( rockContext ).Queryable()
                .Where( a => a.EntityTypeId == groupEntityTypeId )
                .Where( a => a.EntityTypeQualifierValue == groupTypeIdString )
                .Where( a => a.EntityTypeQualifierColumn == "GroupTypeId" );
            var groupAttributesValueQry = new AttributeValueService( rockContext ).Queryable()
                .Join( groupAttributesQry,
                av => av.AttributeId,
                a => a.Id,
                ( av, a ) => av );

            var items = new GroupService( rockContext )
                .Queryable()
                .Where( g => g.GroupTypeId == groupTypeId )
                .Where( g => g.IsActive && g.IsPublic && !g.IsArchived )
                .Where( g =>
                    g.GroupCapacity == null
                    || g.GroupCapacity == 0
                    || g.Members.Where( m => m.GroupMemberStatus == GroupMemberStatus.Active ).Count() < g.GroupCapacity )
                .GroupJoin( groupAttributesValueQry,
                g => g.Id,
                av => av.EntityId,
                ( g, av ) => new { g, av }
                )
                .ToList();

            var groups = new List<Group>();
            foreach ( var item in items )
            {
                item.g.AttributeValues = item.av.ToDictionary( av => av.AttributeKey, av => new AttributeValueCache( av ) );
                item.g.Attributes = item.av.ToDictionary( av => av.AttributeKey, av => AttributeCache.Get( av.AttributeId ) );
                groups.Add( item.g );
            }

            var mergeFields = LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            mergeFields.Add( "Groups", groups );

            lOutput.Text = GetAttributeValue( "LavaTemplate" ).ResolveMergeFields( mergeFields );
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
            ShowGroups();
        }

        #endregion

        #region Methods

        // helper functional methods (like BindGrid(), etc.)

        #endregion
    }
}