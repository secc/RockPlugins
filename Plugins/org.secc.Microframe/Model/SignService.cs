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
using System.Data.Entity;
using System.Linq;
using Rock.Data;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using org.secc.Microframe.Data;

namespace org.secc.Microframe.Model
{
    public class SignService : MicroframeService<Sign>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="KioskService"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public SignService(RockContext context) : base( context ) { }

    }
}
