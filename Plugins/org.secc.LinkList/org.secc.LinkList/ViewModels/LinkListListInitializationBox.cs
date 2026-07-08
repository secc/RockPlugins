// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System.Collections.Generic;

namespace org.secc.LinkList.ViewModels
{
    /// <summary>
    /// Configuration surfaced to the List block's Obsidian frontend
    /// (via GetObsidianBlockInitialization), following Rock's list-block
    /// convention: navigation URLs (with the ((Key)) placeholder) plus the
    /// add/delete capability flags.
    /// </summary>
    public class LinkListListInitializationBox
    {
        public Dictionary<string, string> NavigationUrls { get; set; } = new Dictionary<string, string>();

        public bool IsAddEnabled { get; set; }

        public bool IsDeleteEnabled { get; set; }

        public bool IsBlockVisible { get; set; } = true;

        /// <summary>
        /// WS12: true when the current person may edit the org-wide global
        /// header/footer (Administrate on the LinkList ContentChannel). Gates
        /// the Global Settings panel in the admin block.
        /// </summary>
        public bool CanManageGlobalSettings { get; set; }
    }
}
