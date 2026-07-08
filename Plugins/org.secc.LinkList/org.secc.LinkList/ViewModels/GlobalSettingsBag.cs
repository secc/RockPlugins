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
    /// WS12: the org-wide global header/footer settings stored on the single
    /// LinkList ContentChannel. Edited from the admin List block; the RAW
    /// (unsanitized) content is carried here so the admin edits the original.
    /// The public display path resolves + sanitizes these in BuildBag.
    /// </summary>
    public class GlobalSettingsBag
    {
        public string HeaderContent { get; set; }

        public bool HeaderActive { get; set; }

        public string FooterContent { get; set; }

        public bool FooterActive { get; set; }

        /// <summary>WS7: load the IvyJournal serif (licensed domains only); off = Cormorant/Georgia fallback.</summary>
        public bool UseIvyJournalFont { get; set; }
    }
}
