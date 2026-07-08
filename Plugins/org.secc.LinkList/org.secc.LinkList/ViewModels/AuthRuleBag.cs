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
    /// Read-only projection of an AuthRule that grants EDIT on a Link List
    /// item, surfaced for the "Also has edit access" panel in the editor.
    /// Either <see cref="GroupName"/> or <see cref="PersonName"/> is populated
    /// (the other is null) depending on the AuthRule subject.
    /// </summary>
    public class AuthRuleBag
    {
        public string Kind { get; set; } // "Group" | "Person" | "SpecialRole"

        public string GroupName { get; set; }

        public string PersonName { get; set; }

        public string SpecialRoleLabel { get; set; }

        public int Order { get; set; }
    }
}
