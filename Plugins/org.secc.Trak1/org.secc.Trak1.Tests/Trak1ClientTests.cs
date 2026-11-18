using System;
using System.Net;
using System.Net.Http;
using org.secc.Trak1.Helpers;
using Xunit;

namespace org.secc.Trak1.Tests
{
    public class Trak1ClientTests
    {
        private const string PackageUrl = "https://trak1.test/GetAvailablePackages/sub/co";
        private const string RequestUrl = "https://trak1.test/RunPackage";

        private static Trak1Client Client( string url, HttpStatusCode status, string body, out FakeHandler handler )
        {
            handler = new FakeHandler().Respond( url, status, body );
            return new Trak1Client( handler );
        }

        [Theory]
        [InlineData( "" )]
        [InlineData( "null" )]
        public void GetPackages_EmptyResponseThrows( string body )
        {
            var client = Client( PackageUrl, HttpStatusCode.OK, body, out _ );

            var ex = Assert.Throws<Trak1Exception>( () => client.GetPackages( PackageUrl ) );
            Assert.Contains( "empty response", ex.Message );
        }

        [Fact]
        public void GetPackages_EmptyArrayIsAnEmptyList()
        {
            var client = Client( PackageUrl, HttpStatusCode.OK, "[]", out _ );

            Assert.Empty( client.GetPackages( PackageUrl ) );
        }

        [Fact]
        public void GetPackages_NormalizesMissingComponentsAndRequiredFields()
        {
            var client = Client( PackageUrl, HttpStatusCode.OK,
                "[{\"PackageName\":\"A\"},{\"PackageName\":\"B\",\"Components\":[{\"ComponentName\":\"C\"}]}]", out _ );

            var packages = client.GetPackages( PackageUrl );

            Assert.Equal( 2, packages.Count );
            Assert.Empty( packages[0].Components );
            Assert.Empty( Assert.Single( packages[1].Components ).RequiredFields );
        }

        [Fact]
        public void GetPackages_HttpErrorThrowsWithStatusCode()
        {
            var client = Client( PackageUrl, HttpStatusCode.InternalServerError, "", out _ );

            var ex = Assert.Throws<Trak1Exception>( () => client.GetPackages( PackageUrl ) );
            Assert.Equal( 500, ex.StatusCode );
            Assert.Contains( "HTTP 500", ex.Message );
        }

        [Fact]
        public void RunPackage_ReturnsTransactionId()
        {
            var transactionId = Guid.NewGuid();
            var client = Client( RequestUrl, HttpStatusCode.OK, "{\"TransactionId\":\"" + transactionId + "\"}", out var handler );

            var response = client.RunPackage( RequestUrl, new Trak1Request { PackageName = "Basic" } );

            Assert.Equal( transactionId, response.TransactionId );
            Assert.Equal( HttpMethod.Post, Assert.Single( handler.RequestedMethods ) );
        }

        [Fact]
        public void RunPackage_HttpErrorSurfacesVendorMessageAndIncident()
        {
            var incidentId = Guid.NewGuid();
            var client = Client( RequestUrl, HttpStatusCode.BadRequest,
                "{\"Error\":{\"Message\":\"SSN invalid\",\"IncidentId\":\"" + incidentId + "\"}}", out _ );

            var ex = Assert.Throws<Trak1Exception>( () => client.RunPackage( RequestUrl, new Trak1Request() ) );

            Assert.Equal( 400, ex.StatusCode );
            Assert.Contains( "HTTP 400", ex.Message );
            Assert.Contains( "SSN invalid", ex.Message );
            Assert.Contains( incidentId.ToString(), ex.Message );
            Assert.Contains( "SSN invalid", ex.VendorMessage );
        }

        [Fact]
        public void RunPackage_HttpErrorWithoutJsonKeepsTruncatedBody()
        {
            var body = "<html>" + new string( 'x', 1000 ) + "</html>";
            var client = Client( RequestUrl, HttpStatusCode.BadGateway, body, out _ );

            var ex = Assert.Throws<Trak1Exception>( () => client.RunPackage( RequestUrl, new Trak1Request() ) );

            Assert.Equal( 502, ex.StatusCode );
            Assert.Null( ex.VendorMessage );
            Assert.Equal( Trak1Exception.MaxResponseBodyLength, ex.ResponseBody.Length );
            Assert.StartsWith( "<html>", ex.ResponseBody );
            Assert.True( ex.Message.Length < 400 );
        }

        [Theory]
        [InlineData( "{\"Error\":{\"Message\":\"Duplicate Detected\"}}" )]
        [InlineData( "{\"Error\":{\"Error\":{\"Message\":\"Duplicate Detected\"}}}" )]
        [InlineData( "{\"TransactionId\":\"00000000-0000-0000-0000-000000000000\",\"Error\":{\"Error\":{\"Message\":\"Duplicate Detected\"}}}" )]
        public void RunPackage_VendorErrorThrowsWithMessage( string body )
        {
            var client = Client( RequestUrl, HttpStatusCode.OK, body, out _ );

            var ex = Assert.Throws<Trak1Exception>( () => client.RunPackage( RequestUrl, new Trak1Request() ) );

            Assert.Equal( "Duplicate Detected", ex.Message );
            Assert.Equal( "Duplicate Detected", ex.VendorMessage );
        }

        [Theory]
        [InlineData( "{}" )]
        [InlineData( "{\"TransactionId\":\"00000000-0000-0000-0000-000000000000\"}" )]
        [InlineData( "{\"Error\":{}}" )]
        public void RunPackage_MissingTransactionIdThrows( string body )
        {
            var client = Client( RequestUrl, HttpStatusCode.OK, body, out _ );

            var ex = Assert.Throws<Trak1Exception>( () => client.RunPackage( RequestUrl, new Trak1Request() ) );

            Assert.Contains( "transaction id", ex.Message );
            Assert.Contains( body, ex.Message );
            Assert.Null( ex.VendorMessage );
        }

        [Fact]
        public void NetworkFailureThrowsWithTheUnderlyingCause()
        {
            var handler = new FakeHandler().Fail( PackageUrl,
                new HttpRequestException( "An error occurred while sending the request.", new WebException( "The remote name could not be resolved: 'trak1.test'" ) ) );

            var ex = Assert.Throws<Trak1Exception>( () => new Trak1Client( handler ).GetPackages( PackageUrl ) );

            Assert.Contains( "Trak-1 package request failed", ex.Message );
            Assert.Contains( "remote name could not be resolved", ex.Message );
        }

        [Fact]
        public void TimeoutThrowsATimeoutMessage()
        {
            var handler = new FakeHandler().Fail( RequestUrl, new System.Threading.Tasks.TaskCanceledException() );

            var ex = Assert.Throws<Trak1Exception>( () => new Trak1Client( handler ).RunPackage( RequestUrl, new Trak1Request() ) );

            Assert.Equal( "Trak-1 background check request timed out.", ex.Message );
        }

        [Theory]
        [InlineData( "" )]
        [InlineData( "null" )]
        public void RunPackage_EmptyResponseThrows( string body )
        {
            var client = Client( RequestUrl, HttpStatusCode.OK, body, out _ );

            var ex = Assert.Throws<Trak1Exception>( () => client.RunPackage( RequestUrl, new Trak1Request() ) );
            Assert.Contains( "empty response", ex.Message );
        }
    }
}
