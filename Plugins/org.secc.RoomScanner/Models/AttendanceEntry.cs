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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.RoomScanner.Models
{
    public class AttendanceEntry
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public string NickName { get; set; }
        public string LastName { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public bool DidAttend { get; set; }
        public string AttendanceGuid { get; set; }
        public bool InWorship { get; set; }
        public bool WithParent { get; set; }
        public bool IsVolunteer { get; set; }
    }
}
