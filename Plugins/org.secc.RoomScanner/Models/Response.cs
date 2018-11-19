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
    public class Response
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public bool Overridable { get; set; }
        public bool RequireConfirmation { get; set; }
        public int PersonId { get; set; }
        public string BirthdayText { get; set; }

        public Response( bool success, string message, bool overridable, bool requireConfirmation = false, int personId = 0, string birthdayText = "" )
        {
            this.Success = success;
            this.Message = message;
            this.Overridable = overridable;
            this.RequireConfirmation = requireConfirmation;
            this.PersonId = personId;
            this.BirthdayText = birthdayText;
        }
    }
}
