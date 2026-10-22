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
using org.secc.Rest.Controllers;
using Xunit;

namespace org.secc.Rest.Tests
{
    /// <summary>
    /// ROCK-9116: the mobile phone api/account/create adds to a person it creates.
    /// </summary>
    public class NewPersonMobilePhoneTests
    {
        private const int MobileTypeValueId = 12;

        private static AccountController.Account AccountWith( string mobileNumber, bool? smsOptIn )
        {
            return new AccountController.Account { MobileNumber = mobileNumber, SmsOptIn = smsOptIn };
        }

        [Theory]
        [InlineData( null, false )]
        [InlineData( false, false )]
        [InlineData( true, true )]
        public void MessagingFollowsTheAccountsSmsOptIn( bool? smsOptIn, bool expected )
        {
            var phone = AccountController.CreateNewPersonMobilePhone( AccountWith( "5025550100", smsOptIn ), MobileTypeValueId, "1" );

            Assert.Equal( expected, phone.IsMessagingEnabled );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "   " )]
        public void BlankNumberGivesNoPhone( string mobileNumber )
        {
            Assert.Null( AccountController.CreateNewPersonMobilePhone( AccountWith( mobileNumber, true ), MobileTypeValueId, "1" ) );
        }

        [Fact]
        public void TenDigitNumberIsCleanedAndGetsTheDefaultCountryCode()
        {
            var phone = AccountController.CreateNewPersonMobilePhone( AccountWith( "(502) 555-0100", true ), MobileTypeValueId, "44" );

            Assert.Equal( "5025550100", phone.Number );
            Assert.Equal( "44", phone.CountryCode );
        }

        [Fact]
        public void LeadingDigitsBeyondTenAreTheCountryCode()
        {
            var phone = AccountController.CreateNewPersonMobilePhone( AccountWith( "15025550100", true ), MobileTypeValueId, "44" );

            Assert.Equal( "5025550100", phone.Number );
            Assert.Equal( "1", phone.CountryCode );
        }

        [Fact]
        public void NumberTypeIsThePassedMobileType()
        {
            var phone = AccountController.CreateNewPersonMobilePhone( AccountWith( "5025550100", null ), MobileTypeValueId, "1" );

            Assert.Equal( MobileTypeValueId, phone.NumberTypeValueId );
        }
    }
}
