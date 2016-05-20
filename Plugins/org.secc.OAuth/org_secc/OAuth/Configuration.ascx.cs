// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
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
using System.Web.UI;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using System.Web;
using System.Security.Claims;
using Microsoft.Owin.Security;
using System.Linq;
using org.secc.OAuth.Model;
using org.secc.OAuth.Data;

namespace RockWeb.Plugins.org_secc.OAuth
{
    /// <summary>
    /// OAuth configuration
    /// </summary>
    [DisplayName( "OAuth Configuration" )]
    [Category( "SECC > Security" )]
    [Description( "Configuration settings for OAuth." )]
    [TextField("OAuth Config Attribute Key", "The OAuth Configuration Attribute's Key", true, "OAuthSettings")]
    public partial class Configuration : Rock.Web.UI.RockBlock
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            var attribute = GlobalAttributesCache.Value(GetAttributeValue("OAuthConfigAttributeKey"));
            kvlConfig.Value = attribute;

            ClientService clientService = new ClientService(new OAuthContext());
            gOauthClients.DataSource = clientService.Queryable().ToList();
            gOauthClients.DataBind();
        }
    }
}
