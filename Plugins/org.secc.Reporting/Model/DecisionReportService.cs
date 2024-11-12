using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.secc.Reporting.Data;
using Rock.Data;

namespace org.secc.Reporting.Model
{
    public class DecisionReportService : ReportingService<DecisionReport>
    {
        public DecisionReportService( RockContext context ) : base( context )
        {
        }

        public override bool Delete( DecisionReport item )
        {
            throw new NotImplementedException();
        }

        public override bool CanDelete( DecisionReport item, out string errorMessage )
        {
            errorMessage = "Not Implemented";
            return false;
        }
    }
}
