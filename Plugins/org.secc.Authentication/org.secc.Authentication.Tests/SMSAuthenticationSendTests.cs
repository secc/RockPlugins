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
using System.Runtime.Serialization;
using System.Threading;
using Rock.Data;
using Rock.Model;
using Rock.Security.ExternalAuthentication;
using Xunit;
using static Rock.Security.ExternalAuthentication.SMSRecords;

namespace org.secc.Authentication.Tests
{
    /// <summary>
    /// SendSMSAuthentication and SendSMS, with the number's owner, the client address and the attributes
    /// supplied by the test, and either the whole step that replaces and sends the code or only the part
    /// that replaces it.
    /// </summary>
    public class SMSAuthenticationSendTests
    {
        private const int WindowMinutes = 15;

        private static string NewKey( string kind )
        {
            return kind + "-" + Guid.NewGuid().ToString( "N" );
        }

        private static int NewPersonId()
        {
            return 1000000 + ( Guid.NewGuid().GetHashCode() & 0x3FFFFFFF );
        }

        [Fact]
        public void SendSMSAuthentication_ReplacesAndSends_WithinTheBudget()
        {
            var authentication = SendingSMSAuthentication.Create( new Person { Id = NewPersonId() }, NewKey( "ip" ), maxCodeRequests: 2 );
            var phone = NewKey( "phone" );

            Assert.True( authentication.SendSMSAuthentication( phone ) );
            Assert.Equal( 1, authentication.ReplaceCount );
            Assert.Equal( phone, authentication.ReplacedPhoneNumber );

            // The number stays reserved until the send releases it.
            Assert.False( ReserveItems( NewKey( "ip" ), phone ) );
            ReleaseItems( authentication.IpAddress, phone );
        }

        [Fact]
        public void SendSMSAuthentication_LeavesTheCode_WhenThePersonIsOverBudget()
        {
            var person = new Person { Id = NewPersonId() };
            var personKey = "SMS_" + person.Id;
            var authentication = SendingSMSAuthentication.Create( person, NewKey( "ip" ), maxCodeRequests: 2 );

            // The person's two requests in the window were made from other numbers and addresses.
            for ( var i = 0; i < 2; i++ )
            {
                var ip = NewKey( "ip" );
                var otherPhone = NewKey( "phone" );
                Assert.Equal( IssuanceResult.Allowed, TryBeginIssuance( personKey, otherPhone, ip, 2, WindowMinutes ) );
                ReleaseItems( ip, otherPhone );
            }

            var phone = NewKey( "phone" );
            Assert.False( authentication.SendSMSAuthentication( phone ) );
            Assert.Equal( 0, authentication.ReplaceCount );

            // The refused request reserved nothing.
            Assert.True( ReserveItems( authentication.IpAddress, phone ) );
            ReleaseItems( authentication.IpAddress, phone );
        }

        [Fact]
        public void SendSMSAuthentication_LeavesTheCode_WhileACodeIsBeingSentFromTheSameAddress()
        {
            var authentication = SendingSMSAuthentication.Create( new Person { Id = NewPersonId() }, NewKey( "ip" ), maxCodeRequests: 2 );
            var otherPhone = NewKey( "phone" );
            var phone = NewKey( "phone" );

            // A code is still being sent from this address to another number.
            Assert.True( ReserveItems( authentication.IpAddress, otherPhone ) );
            try
            {
                Assert.True( authentication.SendSMSAuthentication( phone ) );
                Assert.Equal( 0, authentication.ReplaceCount );

                // The refused request left the number unreserved.
                var newIp = NewKey( "ip" );
                Assert.True( ReserveItems( newIp, phone ) );
                ReleaseItems( newIp, phone );
            }
            finally
            {
                ReleaseItems( authentication.IpAddress, otherPhone );
            }
        }

        [Fact]
        public void SendSMSAuthentication_SendsTheNewCode_ThenReleasesTheReservation()
        {
            var ip = NewKey( "ip" );
            var phone = NewKey( "phone" );
            var authentication = ReplacingSMSAuthentication.Create( new Person { Id = NewPersonId() }, ip );

            Assert.True( authentication.SendSMSAuthentication( phone ) );
            Assert.Equal( 1, authentication.ReplaceCount );

            // The send runs in the background. Without Rock's database it can't be delivered, and when it
            // finishes it releases the address and number.
            Assert.True( SpinWait.SpinUntil( () => ReserveItems( ip, phone ), TimeSpan.FromSeconds( 30 ) ) );
            ReleaseItems( ip, phone );
        }

        [Fact]
        public void SendSMSAuthentication_LeavesTheCode_WhenTheNumberHasNoSingleOwner()
        {
            var authentication = SendingSMSAuthentication.Create( null, NewKey( "ip" ), maxCodeRequests: 2 );

            Assert.False( authentication.SendSMSAuthentication( NewKey( "phone" ) ) );
            Assert.Equal( 0, authentication.ReplaceCount );
        }

        [Fact]
        public void SendSMS_ReleasesTheReservation_WhenTheDelayIsNotUsable()
        {
            var phone = NewKey( "phone" );
            var ip = NewKey( "ip" );
            Assert.True( ReserveItems( ip, phone ) );

            // A negative delay makes Task.Delay throw before the message is sent.
            var context = new RecordingSynchronizationContext();
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext( context );
            try
            {
                SendingSMSAuthentication.Create( null, ip, 3 ).SendSMS( null, phone, ip, -2 );
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext( previous );
            }

            Assert.Empty( context.Exceptions );
            Assert.True( ReserveItems( ip, phone ) );
            ReleaseItems( ip, phone );
        }

        /// <summary>
        /// Keeps the exceptions an async void method raises instead of letting them end the process.
        /// </summary>
        private class RecordingSynchronizationContext : SynchronizationContext
        {
            public List<Exception> Exceptions { get; } = new List<Exception>();

            public override void Post( SendOrPostCallback d, object state )
            {
                try
                {
                    d( state );
                }
                catch ( Exception ex )
                {
                    Exceptions.Add( ex );
                }
            }
        }
    }

    /// <summary>
    /// SMSAuthentication with the number's owner, the client address and the attributes set by the test, and a
    /// replace-and-send step that only counts its calls. It is created without running its constructors, because
    /// Rock's Component constructor loads the component's attributes from Rock's database.
    /// </summary>
    public class SendingSMSAuthentication : SMSAuthentication
    {
        private Person _owner;
        private Dictionary<string, string> _attributes;

        public string IpAddress { get; private set; }

        public int ReplaceCount { get; private set; }

        public string ReplacedPhoneNumber { get; private set; }

        public static SendingSMSAuthentication Create( Person owner, string ipAddress, int maxCodeRequests )
        {
            var authentication = ( SendingSMSAuthentication ) FormatterServices.GetUninitializedObject( typeof( SendingSMSAuthentication ) );
            authentication._owner = owner;
            authentication.IpAddress = ipAddress;
            authentication._attributes = new Dictionary<string, string>
            {
                { "MaxCodeRequests", maxCodeRequests.ToString() },
                { "CodeRequestWindowMinutes", "15" }
            };
            return authentication;
        }

        public override string GetAttributeValue( string key )
        {
            return _attributes.TryGetValue( key, out var value ) ? value : null;
        }

        public override Person GetNumberOwner( string phoneNumber, RockContext rockContext, out string error )
        {
            error = _owner == null ? "There was an issue with your request" : string.Empty;
            return _owner;
        }

        protected override RockContext CreateRockContext()
        {
            return null;
        }

        protected override string GetIpAddress()
        {
            return IpAddress;
        }

        protected override void ReplaceCodeAndSend( RockContext rockContext, Person person, string phoneNumber, string ipAddress )
        {
            ReplaceCount++;
            ReplacedPhoneNumber = phoneNumber;
        }
    }

    /// <summary>
    /// SMSAuthentication with the number's owner and the client address set by the test, every attribute blank,
    /// and a code-replacing step that only counts its calls, so the rest of ReplaceCodeAndSend, including the
    /// background send, runs as it does in Rock. Like SendingSMSAuthentication, it is created without running
    /// its constructors.
    /// </summary>
    public class ReplacingSMSAuthentication : SMSAuthentication
    {
        private Person _owner;

        public string IpAddress { get; private set; }

        public int ReplaceCount { get; private set; }

        public static ReplacingSMSAuthentication Create( Person owner, string ipAddress )
        {
            var authentication = ( ReplacingSMSAuthentication ) FormatterServices.GetUninitializedObject( typeof( ReplacingSMSAuthentication ) );
            authentication._owner = owner;
            authentication.IpAddress = ipAddress;
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

        protected override string GetIpAddress()
        {
            return IpAddress;
        }

        protected override string ReplaceCode( RockContext rockContext, Person person )
        {
            ReplaceCount++;
            return "123456";
        }
    }
}
