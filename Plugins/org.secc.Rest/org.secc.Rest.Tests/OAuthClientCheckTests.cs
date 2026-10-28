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
using System.Collections.Generic;
using org.secc.OAuth.Model;
using org.secc.Rest.Controllers;
using Xunit;

namespace org.secc.Rest.Tests
{
    /// <summary>
    /// ROCK-8820: the caller check shared by create, confirmaccount, forgotpassword and smslogin.
    /// </summary>
    public class OAuthClientCheckTests
    {
        private static readonly Guid ApiKey = new Guid( "3B0D5C0E-8F51-4C38-9E0B-6A1F2D7C4E90" );

        private readonly List<Guid> _lookups = new List<Guid>();

        private Func<Guid, Client> Lookup( Client client )
        {
            return apiKey =>
            {
                _lookups.Add( apiKey );
                return client;
            };
        }

        /// <summary>
        /// Runs the check and returns its result, or the exception it threw.
        /// </summary>
        private static bool? Run( string clientId, Func<Guid, Client> lookup, out Exception exception )
        {
            bool? result = null;
            exception = Record.Exception( () => result = AccountController.IsActiveOAuthClient( clientId, lookup ) );
            return result;
        }

        [Fact]
        public void ActiveClient_IsAccepted()
        {
            Exception exception;
            var result = Run( ApiKey.ToString(), Lookup( new Client { ApiKey = ApiKey, Active = true } ), out exception );

            Assert.Null( exception );
            Assert.True( result );
            Assert.Equal( new[] { ApiKey }, _lookups );
        }

        [Fact]
        public void InactiveClient_IsRejected()
        {
            Exception exception;
            var result = Run( ApiKey.ToString(), Lookup( new Client { ApiKey = ApiKey, Active = false } ), out exception );

            Assert.Null( exception );
            Assert.False( result );
        }

        [Theory]
        [InlineData( "3B0D5C0E-8F51-4C38-9E0B-6A1F2D7C4E90" )]
        [InlineData( "someone@example.com" )]
        public void NameWithNoClient_IsRejected( string clientId )
        {
            // A Rock cookie session or an OAuth user token carries a username, which matches no client.
            Exception exception;
            var result = Run( clientId, Lookup( null ), out exception );

            Assert.Null( exception );
            Assert.False( result );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "   " )]
        public void BlankName_IsRejectedWithoutALookup( string clientId )
        {
            Exception exception;
            var result = Run( clientId, Lookup( new Client { ApiKey = Guid.Empty, Active = true } ), out exception );

            Assert.Null( exception );
            Assert.False( result );
            Assert.Empty( _lookups );
        }
    }
}
