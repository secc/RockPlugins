﻿// <copyright>
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
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace RockWeb.Plugins.org_secc.RoomScanner
{
    /// <summary>
    /// OAuth configuration
    /// </summary>
    [DisplayName( "RoomScanner Configuration" )]
    [Category( "SECC > Security" )]
    [Description( "Configuration settings for OAuth." )]
    [TextField( "Roomscanner Config Attribute Key", "The RoomScanner Configuration Attribute's Key", true, "RoomScannerSettings" )]
    public partial class Configuration : Rock.Web.UI.RockBlock
    {

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            if ( !IsPostBack )
            {
                try
                {
                    var settings = GlobalAttributesCache.Value( GetAttributeValue( "RoomscannerConfigAttributeKey" ) ).AsDictionary();
                    tbAllowedGroupId.Text = settings["AllowedGroupId"];
                    tbSubroomLocationTypeId.Text = settings["SubroomLocationType"];
                }
                catch
                {
                    //If the attribute doesn't exist swallow the error.
                }
            }
        }
        protected void btnSave_Click( object sender, EventArgs e )
        {
            string attributeKey = GetAttributeValue( "RoomscannerConfigAttributeKey" );
            Dictionary<string, string> settings = GlobalAttributesCache.Value( attributeKey ).AsDictionary();
            settings["AllowedGroupId"] = tbAllowedGroupId.Text;
            settings["SubroomLocationType"] = tbSubroomLocationTypeId.Text;

            RockContext context = new RockContext();
            AttributeService attributeService = new AttributeService( context );
            Rock.Model.Attribute attribute = attributeService.Queryable().Where( a => a.Key == attributeKey ).FirstOrDefault();
            if ( attribute == null )
            {
                attribute = new Rock.Model.Attribute();
                attribute.Name = "RoomScanner Settings";
                attribute.Description = "Settings for the OAuth server plugin.";
                attribute.Key = "RoomScannerSettings";
                FieldTypeService fieldTypeService = new FieldTypeService( context );
                attribute.FieldType = fieldTypeService.Get( Rock.SystemGuid.FieldType.KEY_VALUE_LIST.AsGuid() );
                context.SaveChanges();
            }
            // Update the actual attribute value.
            AttributeValueService attributeValueService = new AttributeValueService( context );
            AttributeValue attributeValue = attributeValueService.GetByAttributeIdAndEntityId( attribute.Id, null );
            if ( attributeValue == null )
            {
                attributeValue = new AttributeValue();
                attributeValue.AttributeId = attribute.Id;
                attributeValueService.Add( attributeValue );
            }
            attributeValue.Value = string.Join( "|", settings.Select( a => a.Key + "^" + a.Value ).ToList() );
            context.SaveChanges();

            // Flush the cache(s)
            AttributeCache.Remove( attribute.Id );
            GlobalAttributesCache.Clear();
        }
    }
}
