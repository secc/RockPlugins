using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace org.secc.Trak1.Helpers
{
    /// <summary>
    /// Sends requests to the Trak-1 web service and validates the responses. Every failure
    /// (HTTP error, empty or invalid body, vendor error) is raised as a <see cref="Trak1Exception"/>.
    /// </summary>
    public class Trak1Client
    {
        /// <summary>
        /// The shared client. Cookies are disabled so one cookie jar is not replayed across
        /// every request for the life of the app.
        /// </summary>
        public static readonly Trak1Client Default = new Trak1Client( new HttpClientHandler { UseCookies = false } );

        private readonly HttpClient _httpClient;

        public Trak1Client( HttpMessageHandler handler )
        {
            _httpClient = new HttpClient( handler );
        }

        /// <summary>
        /// Gets the packages available to the subscriber. Missing component and required field
        /// lists are returned as empty lists.
        /// </summary>
        /// <param name="url">The absolute GetAvailablePackages URL.</param>
        /// <returns></returns>
        public List<Trak1Package> GetPackages( string url )
        {
            var packages = GetJson<List<Trak1Package>>( url, "package" );

            foreach ( var package in packages )
            {
                package.Components = package.Components ?? new List<Trak1Component>();
                foreach ( var component in package.Components )
                {
                    component.RequiredFields = component.RequiredFields ?? new List<Trak1RequiredField>();
                }
            }

            return packages;
        }

        /// <summary>
        /// Submits a background check request.
        /// </summary>
        /// <param name="url">The absolute RunPackage URL.</param>
        /// <param name="request">The request.</param>
        /// <returns>The response, which always has a transaction id.</returns>
        public Trak1Response RunPackage( string url, Trak1Request request )
        {
            var body = Send( new HttpRequestMessage( HttpMethod.Post, url )
            {
                Content = new StringContent( JsonConvert.SerializeObject( request ), Encoding.UTF8, "application/json" )
            }, "background check" );

            var response = Deserialize<Trak1Response>( body, "background check" );

            var vendorMessage = GetErrorMessage( response.Error );
            if ( vendorMessage != null )
            {
                throw new Trak1Exception( vendorMessage, null, body, vendorMessage );
            }

            if ( response.TransactionId == Guid.Empty )
            {
                throw new Trak1Exception( "Trak-1 did not return a transaction id. Response: " + Trak1Exception.Truncate( body ), null, body );
            }

            return response;
        }

        /// <summary>
        /// Gets the status of a background check.
        /// </summary>
        /// <param name="url">The absolute GetReportStatus URL, including the transaction id.</param>
        /// <returns>The status, which always has a ReportStatus.</returns>
        public Trak1ReportStatus GetReportStatus( string url )
        {
            var status = GetJson<Trak1ReportStatus>( url, "status" );
            if ( string.IsNullOrWhiteSpace( status.ReportStatus ) )
            {
                throw new Trak1Exception( WithValidation( "Trak-1 status response did not include a report status.", status.RequestValidation ) );
            }

            return status;
        }

        /// <summary>
        /// Gets the report of a completed background check.
        /// </summary>
        /// <param name="url">The absolute GetReportUrl URL, including the transaction id.</param>
        /// <returns>The report, which always has a ReportUrl.</returns>
        public Trak1Report GetReport( string url )
        {
            var report = GetJson<Trak1Report>( url, "report" );
            if ( string.IsNullOrWhiteSpace( report.ReportUrl ) )
            {
                throw new Trak1Exception( WithValidation( "Trak-1 report response did not include a report URL.", report.RequestValidation ) );
            }

            return report;
        }

        /// <summary>
        /// Gets the specified Trak-1 resource and deserializes the JSON response.
        /// </summary>
        /// <param name="url">The absolute URL to request.</param>
        /// <param name="description">Short description of the request, used in error messages.</param>
        /// <returns>The deserialized response (never null).</returns>
        public T GetJson<T>( string url, string description ) where T : class
        {
            var body = Send( new HttpRequestMessage( HttpMethod.Get, url ), description );
            return Deserialize<T>( body, description );
        }

        private string Send( HttpRequestMessage request, string description )
        {
            HttpResponseMessage response;
            try
            {
                response = _httpClient.SendAsync( request ).GetAwaiter().GetResult();
            }
            catch ( TaskCanceledException ex )
            {
                request.Dispose();
                throw new Trak1Exception( string.Format( "Trak-1 {0} request timed out.", description ), null, ex );
            }
            catch ( HttpRequestException ex )
            {
                // The outer message is generic ("An error occurred while sending the request."),
                // so use the underlying network or TLS error.
                request.Dispose();
                throw new Trak1Exception( string.Format( "Trak-1 {0} request failed: {1}", description, ex.GetBaseException().Message ), null, ex );
            }

            using ( request )
            using ( var httpResponse = response )
            {
                var body = httpResponse.Content == null ? null : httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if ( httpResponse.IsSuccessStatusCode )
                {
                    return body;
                }

                var statusCode = ( int ) httpResponse.StatusCode;
                var message = string.Format( "Trak-1 {0} request failed with HTTP {1} ({2})", description, statusCode, httpResponse.ReasonPhrase );

                var vendorMessage = GetErrorMessage( TryGetError( body ) );
                if ( vendorMessage != null )
                {
                    throw new Trak1Exception( message + ": " + vendorMessage, statusCode, body, vendorMessage );
                }

                if ( !string.IsNullOrWhiteSpace( body ) )
                {
                    message += ". Response: " + Trak1Exception.Truncate( body );
                }
                else
                {
                    message += ".";
                }

                throw new Trak1Exception( message, statusCode, body );
            }
        }

        private static T Deserialize<T>( string body, string description ) where T : class
        {
            T result;
            try
            {
                result = JsonConvert.DeserializeObject<T>( body ?? string.Empty );
            }
            catch ( JsonException ex )
            {
                throw new Trak1Exception( string.Format( "Trak-1 {0} response could not be parsed. Response: {1}", description, Trak1Exception.Truncate( body ) ), body, ex );
            }

            if ( result == null )
            {
                throw new Trak1Exception( string.Format( "Trak-1 {0} request returned an empty response.", description ), null, body );
            }

            return result;
        }

        private static ErrorInformation TryGetError( string body )
        {
            if ( string.IsNullOrWhiteSpace( body ) )
            {
                return null;
            }

            try
            {
                var envelope = JsonConvert.DeserializeObject<ErrorEnvelope>( body );
                return envelope == null ? null : envelope.Error;
            }
            catch ( JsonException )
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the first error message in a (possibly nested) Trak-1 error, with its incident id.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <returns>The message, or null if the error has none.</returns>
        internal static string GetErrorMessage( ErrorInformation error )
        {
            string message = null;
            var incidentId = Guid.Empty;

            // Trak-1 sometimes nests the error one level down (Error.Error.Message).
            for ( var depth = 0; error != null && depth < 5; depth++, error = error.Error )
            {
                if ( message == null && !string.IsNullOrWhiteSpace( error.Message ) )
                {
                    message = error.Message.Trim();
                }

                if ( incidentId == Guid.Empty )
                {
                    incidentId = error.IncidentId;
                }
            }

            if ( message == null )
            {
                return null;
            }

            return incidentId == Guid.Empty ? message : string.Format( "{0} (Trak-1 incident {1})", message, incidentId );
        }

        private static string WithValidation( string message, string requestValidation )
        {
            return string.IsNullOrWhiteSpace( requestValidation ) ? message : message + " Request validation: " + requestValidation.Trim();
        }

        private class ErrorEnvelope
        {
            public ErrorInformation Error { get; set; }
        }
    }
}
