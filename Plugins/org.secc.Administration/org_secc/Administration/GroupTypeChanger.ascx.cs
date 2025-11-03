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
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Administration
{
    /// <summary>
    /// Block to execute a sql command and display the result (if any).
    /// </summary>
    [DisplayName( "Group Type Changer" )]
    [Category( "SECC > Administration" )]
    [Description( "Tool to change the group type of a group." )]

    public partial class GroupTypeChanger : RockBlock
    {
        Group _group;
        RockContext _rockContext;

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            nbMessage.Visible = false;

            _rockContext = new RockContext();

            var groupId = PageParameter( "GroupId" ).AsInteger();
            if (groupId != 0)
            {
                _group = new GroupService( _rockContext ).Get( groupId );
                if (_group != null)
                {
                    ltName.Text = _group.Name;
                    ltGroupTypeName.Text = _group.GroupType.Name;
                }
            }

            if ( !Page.IsPostBack )
            {
                BindGroupTypeDropDown();
            }
            else
            {
                BindSelections();
            }

            base.OnLoad( e );
        }

        private void BindGroupTypeDropDown()
        {
            var groupTypes = new GroupTypeService( new RockContext() ).Queryable()
                .Select( gt => new
                {
                    gt.Id,
                    gt.Name,
                    gt.Order
                } )
                .OrderBy( gt => gt.Order )
                .ThenBy( gt => gt.Name )
                .ToList();

            groupTypes.Insert( 0, new { Id = 0, Name = "", Order = 0 } );

            ddlGroupTypes.DataSource = groupTypes;
            ddlGroupTypes.DataBind();
        }

        protected void ddlGroupTypes_SelectedIndexChanged( object sender, EventArgs e )
        {
            BindSelections();
        }

        private void BindSelections()
        {
            GroupType newGroupType = null;
            var groupTypeId = ddlGroupTypes.SelectedValue.AsInteger();
            if ( groupTypeId != 0 )
            {
                newGroupType = new GroupTypeService( _rockContext ).Get( groupTypeId );
            }

            if ( _group != null && newGroupType != null )
            {
                int sections = 0;
                if ( BindRoles( newGroupType ) ) { sections++; }
                if ( BindGroupAttributes( newGroupType ) ) { sections++; }
                if ( BindMemberAttributes( newGroupType ) ) { sections++; }

                var colClass = $"col-md-{12 / sections}";
                pnlGroupAttributes.AddCssClass( colClass );
                pnlMemberAttributes.AddCssClass( colClass );
                pnlRoles.AddCssClass( colClass );

                btnSave.Visible = true;
            }
            else
            {
                pnlGroupAttributes.Visible = false;
                pnlMemberAttributes.Visible = false;
                pnlRoles.Visible = false;

                btnSave.Visible = false;
            }
        }

        private bool BindRoles( GroupType newGroupType )
        {
            var currentRoles = GetCurrentRoles();

            if ( currentRoles.Any() && newGroupType != null )
            {
                pnlRoles.Visible = true;
                phRoles.Controls.Clear();
                foreach ( var role in currentRoles )
                {
                    RockDropDownList ddlRole = new RockDropDownList()
                    {
                        DataTextField = "Name",
                        DataValueField = "Id",
                        Label = role.Name,
                        ID = role.Id.ToString() + "_ddlRole"
                    };
                    ddlRole.DataSource = newGroupType.Roles;
                    ddlRole.DataBind();

                    phRoles.Controls.Add( ddlRole );

                    var defaultValue = newGroupType.Roles
                        .Where( r => r.Name == role.Name )
                        .FirstOrDefault();

                    if ( defaultValue != null)
                    {
                        ddlRole.SetValue( defaultValue.Id );
                    }
                }
            }
            else
            {
                pnlRoles.Visible = false;
            }

            return true;
        }

        private bool BindGroupAttributes( GroupType newGroupType )
        {
            phGroupAttributes.Controls.Clear();

            var currentAttributes = GetCurrentGroupAttributes();
            var newAttributes = GetNewGroupAttributes( newGroupType );

            // if no current or new attributes, hide the attributes panel
            if ( !currentAttributes.Any() || !newAttributes.Any() )
            {
                pnlGroupAttributes.Visible = false;
                return false;
            }

            pnlGroupAttributes.Visible = true;

            // Add dropdown for each current attribute and set default value
            foreach (var attribute in currentAttributes)
            {
                RockDropDownList ddlAttribute = new RockDropDownList()
                {
                    ID = attribute.Id.ToString() + "_ddlGroupAttribute",
                    Label = attribute.Name,
                    DataValueField = "Id",
                    DataTextField = "Name"
                };
                ddlAttribute.DataSource = newAttributes;
                ddlAttribute.DataBind();

                phGroupAttributes.Controls.Add( ddlAttribute );

                var defaultValue = newAttributes
                    .Where( a => a.Key == attribute.Key )
                    .FirstOrDefault();
                if (defaultValue != null)
                {
                    ddlAttribute.SetValue( defaultValue.Id );
                }
            }

            return true;
        }

        private bool BindMemberAttributes( GroupType newGroupType )
        {
            phMemberAttributes.Controls.Clear();

            var currentAttributes = GetCurrentMemberAttributes();
            var newAttributes = GetNewMemberAttributes( newGroupType );

            // if no current or new attributes, hide the attributes panel
            if (!currentAttributes.Any() || !newAttributes.Any())
            {
                pnlMemberAttributes.Visible = false;
                return false;
            }

            pnlMemberAttributes.Visible = true;

            // Add dropdown for each current attribute and set default value
            foreach (var attribute in currentAttributes)
            {
                RockDropDownList ddlAttribute = new RockDropDownList()
                {
                    ID = attribute.Id.ToString() + "_ddlMemberAttribute",
                    Label = attribute.Name,
                    DataValueField = "Id",
                    DataTextField = "Name"
                };
                ddlAttribute.DataSource = newAttributes;
                ddlAttribute.DataBind();

                phMemberAttributes.Controls.Add( ddlAttribute );

                var defaultValue = newAttributes
                    .Where( a => a.Id == attribute.Id )
                    .FirstOrDefault() ??
                        newAttributes
                            .Where( a => a.Key == attribute.Key )
                            .FirstOrDefault();
                if ( defaultValue != null )
                {
                    ddlAttribute.SetValue( defaultValue.Id );
                }
            }

            return true;
        }

        private List<ItemInfo> GetCurrentGroupAttributes()
        {
            if ( _group == null )
            {
                return new List<ItemInfo>();
            }

            var groupEntityTypeId = EntityTypeCache.Get( Rock.SystemGuid.EntityType.GROUP.AsGuid() ).Id;
            var currentAttributes = new AttributeValueService( _rockContext )
                .Queryable().AsNoTracking()
                .Where( av =>
                    av.EntityId.HasValue &&
                    av.EntityId.Value == _group.Id &&
                    av.Attribute.EntityTypeId == groupEntityTypeId
                )
                .Select( av => av.Attribute )
                .Distinct()
                .ToList()
                .Select( a => new ItemInfo
                {
                    Id = a.Id,
                    Key = a.Key,
                    Name = $"{a.Name} ({a.FieldType.Name})"
                } )
                .ToList();

            return currentAttributes;
        }

        private List<ItemInfo> GetNewGroupAttributes( GroupType newGroupType )
        {
            if ( newGroupType == null )
            {
                return new List<ItemInfo>();
            }

            var groupEntityTypeId = EntityTypeCache.Get( Rock.SystemGuid.EntityType.GROUP.AsGuid() ).Id;
            var newGroupTypeIdStr = newGroupType.Id.ToString();
            var newAttributes = new AttributeService( _rockContext )
                .Queryable().AsNoTracking()
                .Where( a =>
                    a.EntityTypeId == groupEntityTypeId &&
                    a.EntityTypeQualifierColumn == "GroupTypeId" &&
                    a.EntityTypeQualifierValue == newGroupTypeIdStr
                )
                .ToList()
                .Select( a => new ItemInfo
                {
                    Id = a.Id,
                    Key = a.Key,
                    Name = $"{a.Name} ({a.FieldType.Name})"
                } )
                .ToList();
            return newAttributes;
        }

        private List<ItemInfo> GetCurrentMemberAttributes()
        {
            if (_group == null)
            {
                return new List<ItemInfo>();
            }

            var groupMemberEntityTypeId = EntityTypeCache.Get( Rock.SystemGuid.EntityType.GROUP_MEMBER.AsGuid() ).Id;

            var groupMemberIds = new GroupMemberService( _rockContext )
                .Queryable().AsNoTracking()
                .Where( gm => gm.GroupId == _group.Id )
                .Select( gm => gm.Id )
                .ToList();

            var currentAttributes = new AttributeValueService( _rockContext )
                .Queryable().AsNoTracking()
                .Where( av =>
                    av.EntityId.HasValue &&
                    groupMemberIds.Contains( av.EntityId.Value ) &&
                    av.Attribute.EntityTypeId == groupMemberEntityTypeId
                )
                .Select( av => av.Attribute )
                .Distinct()
                .ToList()
                .Select( a => new ItemInfo
                {
                    Id = a.Id,
                    Key = a.Key,
                    Name = $"{a.Name} ({a.FieldType.Name})"
                } )
                .Distinct()
                .ToList();

            return currentAttributes;
        }

        private List<ItemInfo> GetNewMemberAttributes( GroupType newGroupType )
        {
            if (newGroupType == null)
            {
                return new List<ItemInfo>();
            }
            var groupMemberEntityTypeId = EntityTypeCache.Get( Rock.SystemGuid.EntityType.GROUP_MEMBER.AsGuid() ).Id;

            var newGroupTypeIdStr = newGroupType.Id.ToString();
            var groupIdStr = _group.Id.ToString();

            var newAttributes = new AttributeService( _rockContext )
                .Queryable()
                .AsNoTracking()
                .Where( a =>
                    a.EntityTypeId == groupMemberEntityTypeId &&
                    (
                        (
                            a.EntityTypeQualifierColumn == "GroupTypeId" &&
                            a.EntityTypeQualifierValue == newGroupTypeIdStr
                        ) ||
                        (
                            a.EntityTypeQualifierColumn == "GroupId" &&
                            a.EntityTypeQualifierValue == groupIdStr
                        )
                    )
                )
                .ToList()
                .Select( a => new ItemInfo
                {
                    Id = a.Id,
                    Key = a.Key,
                    Name = $"{a.Name} ({a.FieldType.Name})"
                } )
                .ToList();

            return newAttributes;
        }

        private List<ItemInfo> GetCurrentRoles()
        {
            if (_group == null)
            {
                return new List<ItemInfo>();
            }

            var currentRoles = _group.GroupType.Roles
                .Distinct()
                .Select( r => new ItemInfo
                {
                    Id = r.Id,
                    Name = r.Name
                } )
                .ToList();

            return currentRoles;
        }

        protected void btnSave_Click( object sender, EventArgs e )
        {
            var currentRoles = GetCurrentRoles();
            var currentGroupAttributes = GetCurrentGroupAttributes();
            var currentMemberAttributes = GetCurrentMemberAttributes();

            // Make sure that multiple attributes are not being mapped to same target attribute
            var uniqueAttributeIds = new List<int>();
            foreach (var attribute in currentGroupAttributes)
            {
                var ddlAttribute = (RockDropDownList) phGroupAttributes.FindControl( attribute.Id.ToString() + "_ddlGroupAttribute" );
                if (ddlAttribute != null)
                {
                    int attributeId = ddlAttribute.SelectedValue.AsInteger();
                    if (uniqueAttributeIds.Contains( attributeId ))
                    {
                        ShowErrorMessage( "Please make sure that you do not have more than one Group Attribute being mapped to the same target Group Attribute" );
                        return;
                    }
                    uniqueAttributeIds.Add( attributeId );
                }
            }
            uniqueAttributeIds = new List<int>();
            foreach (var attribute in currentMemberAttributes)
            {
                var ddlAttribute = (RockDropDownList) phMemberAttributes.FindControl( attribute.Id.ToString() + "_ddlMemberAttribute" );
                if (ddlAttribute != null)
                {
                    int attributeId = ddlAttribute.SelectedValue.AsInteger();
                    if (uniqueAttributeIds.Contains( attributeId ))
                    {
                        ShowErrorMessage( "Please make sure that you do not have more than one Member Attribute being mapped to the same target Member Attribute" );
                        return;
                    }
                    uniqueAttributeIds.Add( attributeId );
                }
            }

            // Get the new Group Type
            var newGroupTypeId = ddlGroupTypes.SelectedValue.AsInteger();
            var newGroupType = new GroupTypeService( _rockContext ).Get( newGroupTypeId );
            if ( newGroupType == null )
            {
                return;
            }

            // Get all the group members
            var groupMembers = _group.Members;

            // Update group roles
            foreach ( var role in currentRoles )
            {
                var ddlRole = ( RockDropDownList ) phRoles.FindControl( role.Id.ToString() + "_ddlRole" );
                var roleMembers = groupMembers.Where( gm => gm.GroupRoleId == role.Id );
                foreach (var member in roleMembers)
                {
                    if (ddlRole != null)
                    {
                        member.GroupRoleId = ddlRole.SelectedValue.AsInteger();
                    }
                    else
                    {
                        if (newGroupType.DefaultGroupRoleId.HasValue)
                        {
                            member.GroupRoleId = newGroupType.DefaultGroupRoleId.Value;
                        }
                    }
                }
            }

            // Update Group Sync Roles
            var groupSyncs = new GroupSyncService( _rockContext )
                .Queryable()
                .Where( gs => gs.GroupId == _group.Id )
                .ToList();
            foreach ( var groupSync in groupSyncs)
            {
                var ddlRole = (RockDropDownList) phRoles.FindControl( groupSync.GroupTypeRoleId.ToString() + "_ddlRole" );
                if ( ddlRole != null )
                {
                    groupSync.GroupTypeRoleId = ddlRole.SelectedValue.AsInteger();
                }
                else
                {
                    if ( newGroupType.DefaultGroupRoleId.HasValue )
                    {
                        groupSync.GroupTypeRoleId = newGroupType.DefaultGroupRoleId.Value;
                    }
                }
            }

            var attributeValueService = new AttributeValueService( _rockContext );

            // Update Group attribute values
            foreach( var attribute in currentGroupAttributes )
            {
                var ddlAttribute = ( RockDropDownList ) phGroupAttributes.FindControl( attribute.Id.ToString() + "_ddlGroupAttribute" );
                if ( ddlAttribute != null )
                {
                    var newAttributeId = ddlAttribute.SelectedValue.AsInteger();
                    if ( newAttributeId != 0 && newAttributeId != attribute.Id )
                    {
                        var attributeValue = attributeValueService
                            .Queryable()
                            .Where( av =>
                                av.EntityId == _group.Id &&
                                av.AttributeId == attribute.Id )
                            .FirstOrDefault();

                        if ( attributeValue != null )
                        {
                            attributeValue.AttributeId = newAttributeId;
                        }
                    }
                }
            }

            // Update Group Member attribute values
            foreach ( var attribute in currentMemberAttributes )
            {
                var ddlAttribute = ( RockDropDownList ) phMemberAttributes.FindControl( attribute.Id.ToString() + "_ddlMemberAttribute" );
                if ( ddlAttribute != null )
                {
                    var newAttributeId = ddlAttribute.SelectedValue.AsInteger();
                    if ( newAttributeId != 0 )
                    {
                        foreach ( var member in groupMembers )
                        {
                            var attributeValue = attributeValueService.Queryable()
                                .Where( av =>
                                    av.EntityId == member.Id &&
                                    av.AttributeId == attribute.Id )
                                .FirstOrDefault();

                            if ( attributeValue != null )
                            {
                                attributeValue.AttributeId = newAttributeId;
                            }
                        }
                    }
                }
            }

            // Change the group type
            _group.GroupTypeId = newGroupTypeId;
            foreach (var member in groupMembers)
            {
                member.GroupTypeId = newGroupTypeId;
            }

            // Save the changes
            _rockContext.SaveChanges();

            // Show success message
            nbMessage.NotificationBoxType = NotificationBoxType.Success;
            nbMessage.Text = "GroupType successfully changed.";
            nbMessage.Visible = true;
        }

        public void ShowErrorMessage( string message)
        {
            nbMessage.NotificationBoxType = NotificationBoxType.Danger;
            nbMessage.Text = message;
            nbMessage.Visible = true;
        }

    }

    class ItemInfo
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
    }
}