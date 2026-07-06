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
    /// One available design preset for the editor's design dropdown.
    /// </summary>
    public class DesignOptionBag
    {
        /// <summary>DefinedValue GUID, formatted as a string.</summary>
        public string Value { get; set; }

        public string Text { get; set; }

        public string Description { get; set; }

        // Preview swatches (resolved color values). Optional, used to render
        // a small color preview next to each option in the dropdown.
        public string ContentTextColor { get; set; }
        public string BackgroundColor { get; set; }
        public string ButtonColor { get; set; }
        public string ButtonTextColor { get; set; }

        // WS10: featured-button preset colors.
        public string FeaturedButtonColor { get; set; }
        public string FeaturedButtonTextColor { get; set; }

        // WS7 fix 7: title preset color.
        public string TitleColor { get; set; }
    }
}
