// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Web;
using System.Web.Hosting;

namespace org.secc.Authentication.Tests
{
    /// <summary>
    /// A request with the headers and server variables set by the test.
    /// </summary>
    public class FakeRequest : HttpRequestBase
    {
        private static int _lastAddress;

        private readonly NameValueCollection _headers = new NameValueCollection();
        private readonly NameValueCollection _serverVariables = new NameValueCollection();

        public FakeRequest( string forwardedFor = null, string remoteAddress = null )
        {
            if ( forwardedFor != null )
            {
                _headers["X-Forwarded-For"] = forwardedFor;
            }

            if ( remoteAddress != null )
            {
                _serverVariables["REMOTE_ADDR"] = remoteAddress;
            }
        }

        public override NameValueCollection Headers => _headers;

        public override NameValueCollection ServerVariables => _serverVariables;

        /// <summary>
        /// A connecting address (in 198.19.0.0/16) that no other request in the test run uses.
        /// </summary>
        public static string NewAddress()
        {
            var n = Interlocked.Increment( ref _lastAddress );
            return "198.19." + ( n / 256 ) + "." + ( n % 256 );
        }
    }

    /// <summary>
    /// Runs code with HttpContext.Current set to an ASP.NET request built from SimpleWorkerRequest, whose
    /// connecting address (REMOTE_ADDR) is 127.0.0.1.
    /// </summary>
    public static class RealRequest
    {
        /// <summary>
        /// Sets HttpContext.Current to the request, with the X-Forwarded-For header if one is given, runs the
        /// action and restores HttpContext.Current.
        /// </summary>
        public static void Run( string forwardedFor, Action action )
        {
            var previous = HttpContext.Current;
            HttpContext.Current = new HttpContext( new ForwardedWorkerRequest( forwardedFor ) );
            try
            {
                action();
            }
            finally
            {
                HttpContext.Current = previous;
            }
        }

        private class ForwardedWorkerRequest : SimpleWorkerRequest
        {
            private readonly string _forwardedFor;

            public ForwardedWorkerRequest( string forwardedFor )
                : base( "/", AppDomain.CurrentDomain.BaseDirectory, "default.aspx", string.Empty, new StringWriter() )
            {
                _forwardedFor = forwardedFor;
            }

            public override string[][] GetUnknownRequestHeaders()
            {
                return _forwardedFor == null ? null : new[] { new[] { "X-Forwarded-For", _forwardedFor } };
            }
        }
    }
}
