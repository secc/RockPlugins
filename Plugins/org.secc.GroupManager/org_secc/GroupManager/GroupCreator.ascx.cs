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
using System.Web.UI;
using Newtonsoft.Json;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.org_secc.GroupManager
{
    [DisplayName( "Group Creator" )]
    [Category( "SECC > Groups" )]
    [Description( "Block for creating groups." )]

    public partial class GroupCreator : RockBlock
    {

        #region Page Cycle

        /// <summary>Raises the <see cref="E:System.Web.UI.Control.Init"/> event.</summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> object that contains the event data.</param>
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
            }
        }

        #endregion

        #region Methods
        /// <summary>
        /// Gets the allowed group types. (cf. Group Detail Block)
        /// </summary>
        /// <param name="parentGroupType">Type of the parent group.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private IQueryable<GroupType> GetAllowedGroupTypes( GroupTypeCache parentGroupGroupType, RockContext rockContext )
        {
            rockContext = rockContext ?? new RockContext();

            GroupTypeService groupTypeService = new GroupTypeService( rockContext );

            var groupTypeQry = groupTypeService.Queryable();


            // Limit GroupType to ChildGroupTypes that the ParentGroup allows
            if ( parentGroupGroupType != null )
            {
                if ( !parentGroupGroupType.AllowAnyChildGroupType )
                {
                    List<int> allowedChildGroupTypeIds = parentGroupGroupType.ChildGroupTypes.Select( a => a.Id ).ToList();
                    groupTypeQry = groupTypeQry.Where( a => allowedChildGroupTypeIds.Contains( a.Id ) );
                }
            }

            return groupTypeQry;
        }
        #endregion

        #region Event Handlers

        /// <summary>Handles the SelectItem event of the gpGroup control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gpGroup_SelectItem( object sender, EventArgs e )
        {
            var parentGroups = new List<int>();
            parentGroups = gpGroup.SelectedValuesAsInt().ToList();
            var rockContext = new RockContext();
            var groupService = new GroupService( rockContext );
            var groupServiceQry = groupService.Queryable();
            var parentGroupTypeIds = new List<int>();
            foreach ( var group in parentGroups )
            {
                parentGroupTypeIds.Add( ( groupServiceQry.Where( g => g.Id == group ).FirstOrDefault() ).GroupTypeId );
            }
            var allowedChildGroupTypes = new List<GroupType>();
            foreach ( var groupType in parentGroupTypeIds )
            {
                var groupTypeQry = GetAllowedGroupTypes( GroupTypeCache.Get( groupType ), rockContext );
                if ( allowedChildGroupTypes.Count == 0 )
                {
                    allowedChildGroupTypes = groupTypeQry.ToList();
                }
                else
                {
                    allowedChildGroupTypes = allowedChildGroupTypes.Intersect( groupTypeQry ).ToList();
                }
            }

            //foreach(var groupType in parentGroupTypeIds)
            //{
            //    var groupTypes= GetAllowedGroupTypes( GroupTypeCache.Get( groupType ), rockContext ).ToList();
            //    allowedChildGroupTypes.AddRange(
            //            groupTypes.Where( g => !allowedChildGroupTypes.Select( a => a.Id ).Contains( g.Id ) )
            //    );
            //}

            ddlGroupType.DataSource = allowedChildGroupTypes;
            ddlGroupType.DataValueField = "Id";
            ddlGroupType.DataTextField = "Name";
            ddlGroupType.DataBind();
        }

        /// <summary>Handles the Click event of the btnSave control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            //var parentGroups = new List<int>();
            //parentGroups = gpGroup.SelectedValuesAsInt().ToList();
            //NavigateToPage( Rock.SystemGuid.Page.GROUP_VIEWER.AsGuid(), new Dictionary<string, string> { { "GroupId", publishGroup.GroupId.ToString() } } );
            var parentGroups = new List<int>();
            parentGroups = gpGroup.SelectedValuesAsInt().ToList();
            var groupType = ddlGroupType.SelectedValue.AsInteger();
            var groupNames = tbGroupNames.Text.SplitDelimitedValues( "," ).ToList();
            var rockContext = new RockContext();
            var groupService = new GroupService( rockContext );

            foreach ( var parentGroup in parentGroups )
            {

                foreach ( var groupName in groupNames )
                {
                    Group group = new Group();
                    group.IsSystem = false;
                    group.Name = groupName.Trim();
                    group.GroupTypeId = groupType;
                    group.ParentGroupId = parentGroup;
                    groupService.Add( group );
                    rockContext.SaveChanges();
                }
            }

            nbResult.Visible = true;
            nbResult.Text = "Groups created";



        }
        #endregion
    }
}
