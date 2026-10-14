using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace org.secc.Trak1.Tests
{
    /// <summary>
    /// Returns a canned response per URL and records every request, so no test reaches Trak-1.
    /// </summary>
    internal class FakeHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, (HttpStatusCode Status, string Body)> _responses = new Dictionary<string, (HttpStatusCode, string)>();

        public List<string> RequestedUrls { get; } = new List<string>();

        public List<HttpMethod> RequestedMethods { get; } = new List<HttpMethod>();

        private readonly Dictionary<string, Exception> _failures = new Dictionary<string, Exception>();

        public FakeHandler Respond( string url, HttpStatusCode status, string body )
        {
            _responses[url] = (status, body);
            return this;
        }

        public FakeHandler Fail( string url, Exception exception )
        {
            _failures[url] = exception;
            return this;
        }

        protected override Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken )
        {
            var url = request.RequestUri.ToString();
            RequestedUrls.Add( url );
            RequestedMethods.Add( request.Method );

            if ( _failures.TryGetValue( url, out var exception ) )
            {
                return Task.FromException<HttpResponseMessage>( exception );
            }

            if ( !_responses.TryGetValue( url, out var response ) )
            {
                return Task.FromResult( new HttpResponseMessage( HttpStatusCode.NotFound ) { Content = new StringContent( string.Empty ) } );
            }

            return Task.FromResult( new HttpResponseMessage( response.Status )
            {
                Content = new StringContent( response.Body ?? string.Empty, Encoding.UTF8, "application/json" )
            } );
        }
    }
}
