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
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using System.Linq;
using org.secc.OAuth.Model;
using org.secc.OAuth.Data;

namespace RockWeb.Plugins.org_secc.OAuth
{
    /// <summary>
    /// OAuth configuration
    /// </summary>
    [DisplayName("OAuth Configuration")]
    [Category("SECC > Security")]
    [Description("Configuration settings for OAuth.")]
    [TextField("OAuth Config Attribute Key", "The OAuth Configuration Attribute's Key", true, "OAuthSettings")]
    [SiteField("OAuth Site", "The OAuth Porta/Site", true, "1")]
    public partial class Configuration : Rock.Web.UI.RockBlock
    {


        private OAuthContext _oAuthContext;
        private OAuthContext OAuthContext {
            get
            {
                if (_oAuthContext == null)
                {
                    _oAuthContext = new OAuthContext();
                }
                return _oAuthContext;
            }
            set
            {
                _oAuthContext = value;
            }
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            gOAuthClients.GridRebind += gOAuthClients_Bind;
            gOAuthScopes.GridRebind += gOAuthScopes_Bind;
        }

        private void gOAuthClients_Bind(object sender, EventArgs e)
        {

            ClientService clientService = new ClientService(OAuthContext);
            gOAuthClients.DataSource = clientService.Queryable().ToList();
            gOAuthClients.DataBind();
        }

        private void gOAuthScopes_Bind(object sender, EventArgs e)
        {
            ScopeService scopeService = new ScopeService(OAuthContext);
            gOAuthScopes.DataSource = scopeService.Queryable().ToList();
            gOAuthScopes.DataBind();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!IsPostBack) { 
                var settings = GlobalAttributesCache.Value(GetAttributeValue("OAuthConfigAttributeKey")).AsDictionary();

                int OAuthSiteId = GetAttributeValue("OAuthSite").AsInteger();

                PageRouteService pageRouteService = new PageRouteService(new RockContext());
                List<PageRoute> routes = new List<PageRoute>();
                routes.Add(new PageRoute() { Route="Select One" });
                routes.AddRange(pageRouteService.Queryable().Where(pr => pr.Page.Layout.SiteId == OAuthSiteId).ToList());

                ddlAuthorizeRoute.DataSource = routes;
                ddlAuthorizeRoute.DataBind();
                ddlAuthorizeRoute.SelectedValue = settings["OAuthAuthorizePath"].Trim('/');

                ddlLoginRoute.DataSource = routes;
                ddlLoginRoute.DataBind();
                ddlLoginRoute.SelectedValue = settings["OAuthLoginPath"].Trim('/');

                ddlLogoutRoute.DataSource = routes;
                ddlLogoutRoute.DataBind();
                ddlLogoutRoute.SelectedValue = settings["OAuthLogoutPath"].Trim('/');

                ddlTokenRoute.DataSource = routes;
                ddlTokenRoute.DataBind();
                ddlTokenRoute.SelectedValue = settings["OAuthTokenPath"].Trim('/');

                cbSSLRequired.Checked = settings["OAuthRequireSsl"].AsBoolean();

                tbTokenLifespan.Text = settings["OAuthTokenLifespan"];
                if (settings.ContainsKey("OAuthRefreshTokenLifespan"))
                {
                    tbRefreshTokenLifespan.Text = settings["OAuthRefreshTokenLifespan"];
                }

                gOAuthClients_Bind(null, e);
                gOAuthScopes_Bind(null, e);

            }
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            gOAuthClients.Actions.ShowAdd = true;
            gOAuthClients.Actions.AddClick += gOAuthClient_Add;
            gOAuthScopes.Actions.ShowAdd = true;
            gOAuthScopes.Actions.AddClick += gOAuthScope_Add;
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            string attributeKey = GetAttributeValue("OAuthConfigAttributeKey");
            Dictionary<string, string> settings = GlobalAttributesCache.Value(attributeKey).AsDictionary();
            settings["OAuthAuthorizePath"] = "/" + ddlAuthorizeRoute.SelectedValue;
            settings["OAuthLoginPath"] = "/" + ddlLoginRoute.SelectedValue;
            settings["OAuthLogoutPath"] = "/" + ddlLogoutRoute.SelectedValue;
            settings["OAuthTokenPath"] = "/" + ddlTokenRoute.SelectedValue;
            settings["OAuthRequireSsl"] = cbSSLRequired.Checked.ToString();
            settings["OAuthTokenLifespan"] = tbTokenLifespan.Text;
            settings["OAuthRefreshTokenLifespan"] = tbRefreshTokenLifespan.Text;

            RockContext context = new RockContext();
            AttributeService attributeService = new AttributeService(context);
            Rock.Model.Attribute attribute = attributeService.Queryable().Where(a => a.Key== attributeKey).FirstOrDefault();
            if (attribute == null)
            { 
                attribute = new Rock.Model.Attribute();
                attribute.Name = "OAuth Settings";
                attribute.Description = "Settings for the OAuth server plugin.";
                attribute.Key = "OAuthSettings";
                FieldTypeService fieldTypeService = new FieldTypeService(context);
                attribute.FieldType = fieldTypeService.Get(Rock.SystemGuid.FieldType.KEY_VALUE_LIST.AsGuid());
                context.SaveChanges();
            }
            // Update the actual attribute value.
            AttributeValueService attributeValueService = new AttributeValueService(context);
            AttributeValue attributeValue = attributeValueService.GetByAttributeIdAndEntityId(attribute.Id, null);
            if (attributeValue == null)
            {
                attributeValue = new AttributeValue();
                attributeValue.AttributeId = attribute.Id;
                attributeValueService.Add(attributeValue);
            }
            attributeValue.Value = string.Join("|", settings.Select(a => a.Key + "^" + a.Value).ToList());
            context.SaveChanges();

            // Flush the cache(s)
            AttributeCache.Remove(attribute.Id);
            GlobalAttributesCache.Clear();
        }

        #region Clients
        /// <summary>
        /// Handles the Add event of the gOAuthClients control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void gOAuthClient_Add(object sender, EventArgs e)
        {
            hfClientId.Value = null;
            tbClientName.Text = "";
            tbApiKey.Text = "";
            tbApiSecret.Text = "";
            tbCallbackUrl.Text = "";
            cbActive.Checked = true;
            gOAuthClientEdit.Show();
        }

        protected void gOAuthClientEdit_SaveClick(object sender, EventArgs e)
        {
            divErrors.Visible = false;
            if (String.IsNullOrEmpty(tbClientName.Text))
            {
                divErrors.InnerText = "A valid Client Name must be provided.";
                divErrors.Visible = true;
                return;
            }
            if (tbApiKey.Text.AsGuidOrNull() == null)
            {
                divErrors.InnerText = "A valid GUID must be provided for the API Key.";
                divErrors.Visible = true;
                return;
            }
            if (tbApiSecret.Text.AsGuidOrNull() == null)
            {
                divErrors.InnerText = "A valid GUID must be provided for the API Secret.";
                divErrors.Visible = true;
                return;
            }
            if (string.IsNullOrEmpty(tbCallbackUrl.Text))
            {
                divErrors.InnerText = "A valid Callback URL must be provided.";
                divErrors.Visible = true;
                return;
            }
            ClientScopeService clientScopeService = new ClientScopeService(OAuthContext);
            ClientService clientService = new ClientService(OAuthContext);
            Client client = null;
            if (hfClientId.Value.AsIntegerOrNull().HasValue)
            {
                client = clientService.Get(hfClientId.Value.AsInteger());
            }
            else
            {
                client = new Client();
                clientService.Add(client);
            }

            client.ClientName = tbClientName.Text;
            client.ApiKey = tbApiKey.Text.AsGuid();
            client.ApiSecret = tbApiSecret.Text.AsGuid();
            client.CallbackUrl = tbCallbackUrl.Text;
            client.Active = cbActive.Checked;
            
            foreach (System.Web.UI.WebControls.ListItem item in cblClientScopes.Items)
            {
                int scopeId = item.Value.AsInteger();
                ClientScope clientScope = clientScopeService.Queryable().Where(cs => cs.ClientId == client.Id && cs.ScopeId == scopeId).FirstOrDefault();
                if (clientScope != null)
                {
                    clientScope.Active = item.Selected;
                }
                else if (item.Selected)
                {
                    clientScope = new ClientScope();
                    clientScope.ClientId = client.Id;
                    clientScope.ScopeId = item.Value.AsInteger();
                    clientScope.Active = item.Selected;
                    clientScopeService.Add(clientScope);
                }
            }
            OAuthContext.SaveChanges();
            OAuthContext = new OAuthContext();
            gOAuthClients_Bind(sender, e);
            gOAuthClientEdit.Hide();
        }
        

        protected void gOAuthClients_RowSelected(object sender, Rock.Web.UI.Controls.RowEventArgs e)
        {
            ClientService clientService = new ClientService(OAuthContext);
            Client client = clientService.Get(e.RowKeyId);
            hfClientId.Value = client.Id.ToString();
            tbClientName.Text = client.ClientName;
            tbApiKey.Text = client.ApiKey.ToString();
            tbApiSecret.Text = client.ApiSecret.ToString();
            tbCallbackUrl.Text = client.CallbackUrl;
            cbActive.Checked = client.Active;
            

            ClientScopeService clientScopeService = new ClientScopeService(OAuthContext);

            ScopeService scopeService = new ScopeService(OAuthContext);
            cblClientScopes.DataSource = scopeService.Queryable().Select(s => new { Id = s.Id, Value = s.Identifier + " - " + s.Description }).ToList();
            cblClientScopes.DataBind();

            clientScopeService.Queryable().Where(cs => cs.ClientId == client.Id).ToList().ForEach(cs =>
                cblClientScopes.Items.FindByValue(cs.ScopeId.ToString()).Selected = cs.Active
            );
            gOAuthClientEdit.Show();
        }
        protected void gOAuthClientsDelete_Click(object sender, Rock.Web.UI.Controls.RowEventArgs e)
        {

            ClientService clientService = new ClientService(OAuthContext);
            clientService.Delete(clientService.Get(e.RowKeyId));
            OAuthContext.SaveChanges();
            OAuthContext = new OAuthContext();
            gOAuthClients_Bind(sender, e);
        }
        #endregion Clients

        #region Scopes

        /// <summary>
        /// Handles the Add event of the gOAuthScope control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void gOAuthScope_Add(object sender, EventArgs e)
        {
            hfScopeId.Value = null;
            tbIdentifier.Text = "";
            tbDescription.Text = "";
            cbScopeActive.Checked = true;

            gOAuthScopeEdit.Show();
        }
        

        protected void gOAuthScopeEdit_SaveClick(object sender, EventArgs e)
        {
            ScopeService scopeService = new ScopeService(OAuthContext);
            Scope scope = null;
            if ( hfScopeId.Value.AsIntegerOrNull().HasValue)
            {
                scope = scopeService.Get(hfScopeId.Value.AsInteger());
            }
            else
            {
                scope = new Scope();
                scopeService.Add(scope);
            }
            scope.Identifier = tbIdentifier.Text;
            scope.Description = tbDescription.Text;
            scope.Active = cbScopeActive.Checked;
            OAuthContext.SaveChanges();
            OAuthContext = new OAuthContext();
            gOAuthScopes_Bind(sender, e);
            gOAuthScopeEdit.Hide();
        }

        protected void gOAuthScopes_RowSelected(object sender, Rock.Web.UI.Controls.RowEventArgs e)
        {

            ScopeService scopeService = new ScopeService(OAuthContext);
            Scope scope = scopeService.Get(e.RowKeyId);
            hfScopeId.Value = scope.Id.ToString();
            tbIdentifier.Text = scope.Identifier;
            tbDescription.Text = scope.Description;
            cbScopeActive.Checked = scope.Active;

            gOAuthScopeEdit.Show();
        }

        protected void gOAuthScopesDelete_Click(object sender, Rock.Web.UI.Controls.RowEventArgs e)
        {

            ScopeService scopeService = new ScopeService(OAuthContext);
            scopeService.Delete(scopeService.Get(e.RowKeyId));
            OAuthContext.SaveChanges();
            OAuthContext = new OAuthContext();
            gOAuthScopes_Bind(sender, e);
        }
        #endregion Scopes
    }
}
