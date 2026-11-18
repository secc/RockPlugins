using System.Linq;
using System.Net;
using org.secc.Trak1.Helpers;
using Xunit;

namespace org.secc.Trak1.Tests
{
    public class Trak1StatusUpdateTests
    {
        private const string StatusUrl = "https://trak1.test/GetReportStatus/sub/co/tx";
        private const string ReportUrl = "https://trak1.test/GetReportUrl/sub/co/tx";

        private static readonly string Complete = Trak1StatusUpdate.CompleteStatuses[0];

        [Theory]
        [InlineData( null, null )]
        [InlineData( "", null )]
        [InlineData( "  ", null )]
        [InlineData( "Green", false )]
        [InlineData( "green", false )]
        [InlineData( " Green ", false )]
        [InlineData( "Yellow", true )]
        [InlineData( "Red", true )]
        [InlineData( "Orange", true )]
        [InlineData( "Unknown", true )]
        public void MapRecordFound_MapsHitColor( string hitColor, bool? expected )
        {
            Assert.Equal( expected, Trak1StatusUpdate.MapRecordFound( hitColor ) );
        }

        [Fact]
        public void IsCompleteStatus_MatchesCompleteStatusesTrimmedAndCaseInsensitive()
        {
            foreach ( var status in Trak1StatusUpdate.CompleteStatuses )
            {
                Assert.True( Trak1StatusUpdate.IsCompleteStatus( status ) );
                Assert.True( Trak1StatusUpdate.IsCompleteStatus( status.ToLowerInvariant() ) );
                Assert.True( Trak1StatusUpdate.IsCompleteStatus( " " + status.ToUpperInvariant() + " " ) );
            }
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "  " )]
        [InlineData( "Assigned" )]
        [InlineData( "Pending Verification" )]
        [InlineData( "Needs Attention" )]
        [InlineData( "Error" )]
        public void IsCompleteStatus_IsFalseForOtherStatuses( string reportStatus )
        {
            Assert.False( Trak1StatusUpdate.IsCompleteStatus( reportStatus ) );
        }

        [Theory]
        [InlineData( "Pending Verification", "" )]
        [InlineData( "Assigned", "Green" )]
        [InlineData( "Needs Attention", "Red" )]
        public void Fetch_Pending_ReturnsStatusOnlyAndNeverRequestsReport( string reportStatus, string hitColor )
        {
            var handler = new FakeHandler()
                .Respond( StatusUrl, HttpStatusCode.OK, "{\"ReportStatus\":\"" + reportStatus + "\",\"HitColor\":\"" + hitColor + "\"}" )
                .Respond( ReportUrl, HttpStatusCode.OK, "{\"ReportUrl\":\"https://trak1.test/report/1\"}" );

            var update = Trak1StatusUpdate.Fetch( new Trak1Client( handler ), StatusUrl, ReportUrl );

            Assert.False( update.IsComplete );
            Assert.Equal( reportStatus, update.ReportStatus );
            Assert.Equal( hitColor, update.HitColor );
            Assert.Null( update.RecordFound );
            Assert.Null( update.ReportUrl );
            Assert.DoesNotContain( ReportUrl, handler.RequestedUrls );
        }

        [Theory]
        [InlineData( "Green", false )]
        [InlineData( "Red", true )]
        [InlineData( "Orange", true )]
        [InlineData( "", null )]
        public void Fetch_Complete_ReturnsReportAndRecordFound( string hitColor, bool? expectedRecordFound )
        {
            var handler = new FakeHandler()
                .Respond( StatusUrl, HttpStatusCode.OK, "{\"ReportStatus\":\"" + Complete + "\",\"HitColor\":\"" + hitColor + "\"}" )
                .Respond( ReportUrl, HttpStatusCode.OK, "{\"ReportUrl\":\"https://trak1.test/report/1\"}" );

            var update = Trak1StatusUpdate.Fetch( new Trak1Client( handler ), StatusUrl, ReportUrl );

            Assert.True( update.IsComplete );
            Assert.Equal( expectedRecordFound, update.RecordFound );
            Assert.Equal( "https://trak1.test/report/1", update.ReportUrl );
            Assert.Equal( new[] { StatusUrl, ReportUrl }, handler.RequestedUrls );
        }

        [Theory]
        [InlineData( HttpStatusCode.InternalServerError, "" )]
        [InlineData( HttpStatusCode.NotFound, "{\"Error\":{\"Message\":\"Report not ready\"}}" )]
        [InlineData( HttpStatusCode.OK, "" )]
        [InlineData( HttpStatusCode.OK, "null" )]
        [InlineData( HttpStatusCode.OK, "{}" )]
        [InlineData( HttpStatusCode.OK, "{\"RequestValidation\":\"Invalid transaction\"}" )]
        public void Fetch_Complete_ReportFailureThrowsWithoutUpdate( HttpStatusCode reportStatus, string reportBody )
        {
            var handler = new FakeHandler()
                .Respond( StatusUrl, HttpStatusCode.OK, "{\"ReportStatus\":\"" + Complete + "\",\"HitColor\":\"Red\"}" )
                .Respond( ReportUrl, reportStatus, reportBody );

            Assert.Throws<Trak1Exception>( () => Trak1StatusUpdate.Fetch( new Trak1Client( handler ), StatusUrl, ReportUrl ) );
        }

        [Theory]
        [InlineData( HttpStatusCode.InternalServerError, "" )]
        [InlineData( HttpStatusCode.BadRequest, "<html>Bad Request</html>" )]
        [InlineData( HttpStatusCode.OK, "" )]
        [InlineData( HttpStatusCode.OK, "null" )]
        [InlineData( HttpStatusCode.OK, "{}" )]
        [InlineData( HttpStatusCode.OK, "<html>not json</html>" )]
        [InlineData( HttpStatusCode.OK, "{\"RequestValidation\":\"Invalid transaction\"}" )]
        public void Fetch_StatusFailureThrowsWithoutRequestingReport( HttpStatusCode status, string body )
        {
            var handler = new FakeHandler()
                .Respond( StatusUrl, status, body )
                .Respond( ReportUrl, HttpStatusCode.OK, "{\"ReportUrl\":\"https://trak1.test/report/1\"}" );

            Assert.Throws<Trak1Exception>( () => Trak1StatusUpdate.Fetch( new Trak1Client( handler ), StatusUrl, ReportUrl ) );
            Assert.Equal( new[] { StatusUrl }, handler.RequestedUrls );
        }

        [Fact]
        public void Fetch_StatusRequestValidationIsInTheMessage()
        {
            var handler = new FakeHandler()
                .Respond( StatusUrl, HttpStatusCode.OK, "{\"RequestValidation\":\"Invalid transaction\"}" );

            var ex = Assert.Throws<Trak1Exception>( () => Trak1StatusUpdate.Fetch( new Trak1Client( handler ), StatusUrl, ReportUrl ) );
            Assert.Contains( "Invalid transaction", ex.Message );
        }

        [Fact]
        public void Fetch_KeepsLongReportStatusUnchanged()
        {
            // The action truncates the BackgroundCheck.Status column, not the update itself.
            var longStatus = new string( 'x', 40 );
            var handler = new FakeHandler()
                .Respond( StatusUrl, HttpStatusCode.OK, "{\"ReportStatus\":\"" + longStatus + "\"}" );

            var update = Trak1StatusUpdate.Fetch( new Trak1Client( handler ), StatusUrl, ReportUrl );

            Assert.Equal( longStatus, update.ReportStatus );
            Assert.Single( handler.RequestedUrls.Where( u => u == StatusUrl ) );
        }
    }
}
