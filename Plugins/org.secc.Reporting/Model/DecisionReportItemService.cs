using org.secc.Reporting.Data;

namespace org.secc.Reporting.Model
{
    public class DecisionReportItemService : ReportingService<DecisionReportItem>
    {
        public DecisionReportItemService( Rock.Data.DbContext context ) : base( context )
        {
        }
    }
}
