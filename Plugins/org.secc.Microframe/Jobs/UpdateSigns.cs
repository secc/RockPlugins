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
