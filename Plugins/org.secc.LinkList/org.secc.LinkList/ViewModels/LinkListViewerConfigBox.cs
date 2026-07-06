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
namespace org.secc.LinkList.ViewModels
{
    /// <summary>
    /// Configuration surfaced to the Viewer block's Obsidian frontend.
    /// </summary>
    public class LinkListViewerConfigBox
    {
        /// <summary>
        /// URL to redirect to when no list matches the slug: the configured
        /// "Not Found Page", or the legacy default (/page/255) when unset.
        /// The block always supplies a value, so the viewer always redirects.
        /// </summary>
        public string NotFoundUrl { get; set; }
    }
}
