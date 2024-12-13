using org.secc.Reporting.Data;
using Rock.Data;

namespace org.secc.Reporting.Model
{
    public class DecisionReportItemService : ReportingService<DecisionReportItem>
    {
        public DecisionReportItemService( RockContext context ) : base( context )
        {
        }
    }
}
