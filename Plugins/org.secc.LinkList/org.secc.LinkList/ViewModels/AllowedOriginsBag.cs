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
    /// WS13: the admin Allowed Origins editor state. <see cref="BuiltIn"/> is the
    /// hardcoded bootstrap allowlist (read-only, always trusted); <see cref="Custom"/>
    /// is the admin-managed "Link List Allowed Origins" Defined Type values.
    /// </summary>
    public class AllowedOriginsBag
    {
        public List<string> BuiltIn { get; set; } = new List<string>();

        public List<string> Custom { get; set; } = new List<string>();
    }
}
