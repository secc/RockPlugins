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
namespace org.secc.LinkList.ViewModels
{
    /// <summary>
    /// One slug for a Link List (a ContentChannelItemSlug row). A list may have
    /// several: the primary is what shared/printed links use, and every slug
    /// (primary or additional) resolves to the same list so renaming never
    /// orphans an old URL. Mirrors the editor's per-slug add/remove/primary UI.
    /// </summary>
    public class LinkListSlugBag
    {
        /// <summary>ContentChannelItemSlug.Id, or 0 for a slug the editor just added.</summary>
        public int Id { get; set; }

        /// <summary>The slug text (canonical lowercase form; callers normalize).</summary>
        public string Slug { get; set; }

        /// <summary>True for the single primary slug (used by the grid, viewer, and web-component payload).</summary>
        public bool IsPrimary { get; set; }
    }
}
