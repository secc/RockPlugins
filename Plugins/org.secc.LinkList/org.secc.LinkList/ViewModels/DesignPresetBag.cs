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
    /// WS13: an editable design preset (a DefinedValue of the Link List Design
    /// Defined Type) for the admin block's Design Presets tab. Carries all six
    /// theme colors plus how many lists currently use it.
    /// </summary>
    public class DesignPresetBag
    {
        /// <summary>DefinedValue GUID (string). Empty/null when creating a new preset.</summary>
        public string Value { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int Order { get; set; }

        public string BackgroundColor { get; set; }

        public string ContentTextColor { get; set; }

        public string TitleColor { get; set; }

        public string ButtonColor { get; set; }

        public string ButtonTextColor { get; set; }

        public string FeaturedButtonColor { get; set; }

        public string FeaturedButtonTextColor { get; set; }

        /// <summary>Number of Link Lists whose Design attribute references this preset (read-only).</summary>
        public int UsageCount { get; set; }
    }
}
