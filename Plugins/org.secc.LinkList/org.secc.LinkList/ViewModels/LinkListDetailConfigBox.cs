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
    /// Configuration surfaced to the Detail block's Obsidian frontend
    /// (via GetObsidianBlockInitialization). Carries the entity key read from
    /// the page parameter (null/"0" =&gt; add mode) and back-navigation URLs.
    /// </summary>
    public class LinkListDetailConfigBox
    {
        /// <summary>List item key (GUID) from the page parameter; null when adding.</summary>
        public string ItemKey { get; set; }

        public bool IsAddMode { get; set; }

        /// <summary>Default design preset (DefinedValue GUID) applied to new lists.</summary>
        public string DefaultDesignGuid { get; set; }

        public Dictionary<string, string> NavigationUrls { get; set; } = new Dictionary<string, string>();
    }
}
