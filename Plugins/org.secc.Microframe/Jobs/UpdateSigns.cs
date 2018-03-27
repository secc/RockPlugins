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
using System.Linq;

using Quartz;
using Rock.Attribute;
using Rock;
using Rock.Model;
using Rock.Data;
using org.secc.Microframe.Utilities;

namespace org.secc.Microframe
{
    [DisallowConcurrentExecution]
    public class UpdateSigns : IJob
    {
        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Execute( IJobExecutionContext context )
        {
            SignUtilities.UpdateAllSigns();
            context.Result = "Completed initializing asynchronous update of signs. (This does not mean all signs have finished updating)";
        }
    }
}
