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
using System.Runtime.Serialization;
using System.Web;
using Rock.Data;
using Rock.Model;
using Rock.Security.ExternalAuthentication;
using Xunit;
using static Rock.Security.ExternalAuthentication.SMSRecords;

namespace org.secc.Authentication.Tests
{
    /// <summary>
    /// The caller address SendSMSAuthentication reserves while a code is sent, read from the request.
    /// </summary>
    public class SMSAuthenticationSendAddressTests
    {
        private static string NewPhone()
        {
            return "phone-" + Guid.NewGuid().ToString( "N" );
        }

        private static Person NewPerson()
        {
            return new Person { Id = 1000000 + ( Guid.NewGuid().GetHashCode() & 0x3FFFFFFF ) };
        }

        [Theory]
        [InlineData( "198.18.0.9, 203.0.113.60:443", "203.0.113.60" )]
        [InlineData( "2001:db8:b::7, 203.0.113.61:443", "203.0.113.61" )]
        [InlineData( "[2001:db8:b::8]:443", "2001:db8:b::8" )]
        public void SendSMSAuthentication_ReservesTheLastForwardedEntry( string forwardedFor, string address )
        {
            var authentication = AddressRecordingSMSAuthentication.Create( NewPerson(), new FakeRequest( forwardedFor, "10.9.9.9" ) );
            var phone = NewPhone();

            Assert.True( authentication.SendSMSAuthentication( phone ) );
            try
            {
                Assert.Equal( address, authentication.SentIpAddress );
                Assert.False( ReserveItems( address, NewPhone() ) );
            }
            finally
            {
                ReleaseItems( authentication.SentIpAddress, phone );
            }
        }

        [Fact]
        public void SendSMSAuthentication_WithoutARequest_ReservesUnknown()
        {
            Assert.Null( HttpContext.Current );
            var authentication = AddressRecordingSMSAuthentication.Create( NewPerson(), null );
            authentication.UseCurrentRequest = true;
            var phone = NewPhone();

            Assert.True( authentication.SendSMSAuthentication( phone ) );
            try
            {
                Assert.Equal( "unknown", authentication.SentIpAddress );
            }
            finally
            {
                ReleaseItems( authentication.SentIpAddress, phone );
            }
        }

        [Theory]
        [InlineData( null, "127.0.0.1" )]
        [InlineData( "198.18.0.77, 203.0.113.77:443", "203.0.113.77" )]
        public void SendSMSAuthentication_ReadsTheCurrentRequest( string forwardedFor, string address )
        {
            var authentication = AddressRecordingSMSAuthentication.Create( NewPerson(), null );
            authentication.UseCurrentRequest = true;
            var phone = NewPhone();

            RealRequest.Run( forwardedFor, () => Assert.True( authentication.SendSMSAuthentication( phone ) ) );
            try
            {
                Assert.Equal( address, authentication.SentIpAddress );
            }
            finally
            {
                ReleaseItems( authentication.SentIpAddress, phone );
            }
        }
    }

    /// <summary>
    /// SMSAuthentication with the number's owner and the request set by the test (or the current request), every
    /// attribute blank, and a replace-and-send step that only records the address it is given and leaves the
    /// reservation for the test to release. It is created without running its constructors, because Rock's
    /// Component constructor loads the component's attributes from Rock's database.
    /// </summary>
    public class AddressRecordingSMSAuthentication : SMSAuthentication
    {
        private Person _owner;

        public HttpRequestBase Request { get; set; }

        public bool UseCurrentRequest { get; set; }

        public string SentIpAddress { get; private set; }

        public static AddressRecordingSMSAuthentication Create( Person owner, HttpRequestBase request )
        {
            var authentication = ( AddressRecordingSMSAuthentication ) FormatterServices.GetUninitializedObject( typeof( AddressRecordingSMSAuthentication ) );
            authentication._owner = owner;
            authentication.Request = request;
            return authentication;
        }

        public override string GetAttributeValue( string key )
        {
            return null;
        }

        public override Person GetNumberOwner( string phoneNumber, RockContext rockContext, out string error )
        {
            error = string.Empty;
            return _owner;
        }

        protected override RockContext CreateRockContext()
        {
            return null;
        }

        protected override HttpRequestBase GetCurrentRequest()
        {
            return UseCurrentRequest ? base.GetCurrentRequest() : Request;
        }

        protected override void ReplaceCodeAndSend( RockContext rockContext, Person person, string phoneNumber, string ipAddress )
        {
            SentIpAddress = ipAddress;
        }
    }
}
