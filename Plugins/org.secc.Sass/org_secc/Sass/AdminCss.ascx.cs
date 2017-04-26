// <copyright>
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
using System.ComponentModel;
using Rock.Model;
using Rock.Security;
using System.Web.UI;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.org_secc.Sass
{
    [DisplayName( "Include Admin CSS" )]
    [Category("SECC > CMS")]
    [Description( "Loads the admin css when it's required." )]
    public partial class AdminCss : Rock.Web.UI.RockBlock
    {

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            var canAdministrateBlockOnPage = false;
            var pageBlocks = PageCache.Blocks;
            foreach (Rock.Web.Cache.BlockCache block in pageBlocks)
            {
                bool canAdministrate = block.IsAuthorized(Authorization.ADMINISTRATE, CurrentPerson);
                bool canEdit = block.IsAuthorized(Authorization.EDIT, CurrentPerson);
                bool canView = block.IsAuthorized(Authorization.VIEW, CurrentPerson);

                // Make sure user has access to view block instance
                if (canAdministrate || canEdit || canView)
                {
                    Control control = null;

                    // Check to see if block is configured to use a "Cache Duration'
                    if (block.OutputCacheDuration > 0)
                    {
                        RockMemoryCache cache = RockMemoryCache.Default;
                        string blockCacheKey = string.Format("Rock:BlockOutput:{0}", block.Id);
                        if (cache.Contains(blockCacheKey))
                        {
                            // If the current block exists in our custom output cache, add the cached output instead of adding the control
                            control = new LiteralControl(cache[blockCacheKey] as string);
                        }
                    }

                    if (control == null)
                    {
                        try
                        {
                            control = TemplateControl.LoadControl(block.BlockType.Path);
                            control.ClientIDMode = ClientIDMode.AutoID;
                        }
                        catch (Exception)
                        {
                            // Swallow this exception--NOM NOM
                        }
                    }

                    if (control != null)
                    {
                        if (canAdministrate || (canEdit && control is RockBlockCustomSettings))
                        {
                            canAdministrateBlockOnPage = true;
                        }
                    }
                }
            }

            if (PageCache.IncludeAdminFooter && (PageCache.IsAuthorized(Authorization.ADMINISTRATE, CurrentPerson) || canAdministrateBlockOnPage))
            {
                RockPage.AddCSSLink(ResolveRockUrl("~~/Styles/theme.css"));
            }
        }

        #endregion
        
    }
}