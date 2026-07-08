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
    public class LinkListDetailInitializationBox
    {
        public bool CanEdit { get; set; }

        public bool CanDelete { get; set; }

        public LinkListBag LinkList { get; set; }

        /// <summary>
        /// Available design presets for the editor's design dropdown.
        /// Each entry: Value = DefinedValue GUID (string), Text = preset name.
        /// </summary>
        public List<DesignOptionBag> Designs { get; set; } = new List<DesignOptionBag>();
    }
}
