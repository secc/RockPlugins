using System;
using System.Linq;

namespace org.secc.Trak1.Helpers
{
    /// <summary>
    /// The values to write back to the workflow and the BackgroundCheck after polling Trak-1.
    /// </summary>
    public class Trak1StatusUpdate
    {
        /// <summary>
        /// The Trak-1 ReportStatus values that mean the report is complete (compared trimmed and
        /// case-insensitively). Production (2026-10) only has "Complete", which is also the value the
        /// Trak-1 background check workflow stops polling on; the pending values are "Assigned",
        /// "Pending Verification" and "Needs Attention".
        /// </summary>
        public static readonly string[] CompleteStatuses = { "Complete" };

        public string ReportStatus { get; private set; }

        public string HitColor { get; private set; }

        public bool IsComplete { get; private set; }

        /// <summary>
        /// Whether a record was found. Always null until the report is complete.
        /// </summary>
        public bool? RecordFound { get; private set; }

        /// <summary>
        /// The report URL. Always null until the report is complete.
        /// </summary>
        public string ReportUrl { get; private set; }

        /// <summary>
        /// Gets the status of a background check and, once it is complete, its report. Any failure
        /// throws before an update is returned, so the caller never has a partial update to apply.
        /// </summary>
        /// <param name="client">The Trak-1 client.</param>
        /// <param name="statusUrl">The absolute GetReportStatus URL, including the transaction id.</param>
        /// <param name="reportUrl">The absolute GetReportUrl URL, including the transaction id.</param>
        /// <returns></returns>
        public static Trak1StatusUpdate Fetch( Trak1Client client, string statusUrl, string reportUrl )
        {
            var status = client.GetReportStatus( statusUrl );
            var update = new Trak1StatusUpdate
            {
                ReportStatus = status.ReportStatus,
                HitColor = status.HitColor,
                IsComplete = IsCompleteStatus( status.ReportStatus )
            };

            if ( update.IsComplete )
            {
                update.ReportUrl = client.GetReport( reportUrl ).ReportUrl;
                update.RecordFound = MapRecordFound( status.HitColor );
            }

            return update;
        }

        /// <summary>
        /// Determines whether a Trak-1 ReportStatus means the report is complete.
        /// </summary>
        /// <param name="reportStatus">The ReportStatus returned by Trak-1.</param>
        /// <returns></returns>
        public static bool IsCompleteStatus( string reportStatus )
        {
            if ( string.IsNullOrWhiteSpace( reportStatus ) )
            {
                return false;
            }

            return CompleteStatuses.Contains( reportStatus.Trim(), StringComparer.OrdinalIgnoreCase );
        }

        /// <summary>
        /// Maps the Trak-1 HitColor of a completed report to Rock's BackgroundCheck.RecordFound
        /// (true means a record/hit was found). An empty HitColor is unknown (null), "Green" is no
        /// record found (false), and any other color is a hit (true).
        /// </summary>
        /// <param name="hitColor">The HitColor returned by Trak-1.</param>
        /// <returns></returns>
        public static bool? MapRecordFound( string hitColor )
        {
            if ( string.IsNullOrWhiteSpace( hitColor ) )
            {
                return null;
            }

            return !hitColor.Trim().Equals( "Green", StringComparison.OrdinalIgnoreCase );
        }
    }
}
