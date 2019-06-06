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
using org.secc.FamilyCheckin.Data;
using System.Data.Entity;
using System.Linq;
using Rock.Data;

namespace org.secc.FamilyCheckin.Model
{
    public class KioskTypeService : FamilyCheckinService<KioskType>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="KioskTypeService"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public KioskTypeService(RockContext context) : base( context ) { }
    }
}
