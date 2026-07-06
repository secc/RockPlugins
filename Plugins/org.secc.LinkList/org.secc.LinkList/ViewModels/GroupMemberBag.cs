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
using System;

namespace org.secc.LinkList.ViewModels
{
    /// <summary>
    /// Lightweight projection of a person in the list's primary security group.
    /// </summary>
    public class GroupMemberBag
    {
        public int GroupMemberId { get; set; }

        public Guid PersonGuid { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }

        /// <summary>Resolved photo URL (or null when no photo is set).</summary>
        public string PhotoUrl { get; set; }

        public bool IsCurrentUser { get; set; }
    }
}
