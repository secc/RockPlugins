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
using org.secc.Microframe.Utilities;
using Rock.Jobs;

namespace org.secc.Microframe
{
    public class UpdateSigns : RockJob
    {
        /// <summary>
        /// Executes the job.
        /// </summary>
        public override void Execute()
        {
            SignUtilities.UpdateAllSigns();
            Result = "Completed initializing asynchronous update of signs. (This does not mean all signs have finished updating)";
        }
    }
}
