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
using System.Globalization;
using org.secc.Rest.Controllers;
using Xunit;

namespace org.secc.Rest.Tests
{
    /// <summary>
    /// ROCK-8934: the expiry rule shared by ConfirmAccount and the expired-code cleanup.
    /// </summary>
    public class ConfirmationCodeExpiryTests
    {
        private static readonly DateTime Now = new DateTime( 2026, 10, 16, 12, 0, 0 );

        private static string Ticks( DateTime value )
        {
            return value.Ticks.ToString( CultureInfo.InvariantCulture );
        }

        private static bool IsExpired( string issued )
        {
            return AccountController.IsConfirmationCodeExpired( AccountController.ParseConfirmationCodeIssued( issued ), Now );
        }

        [Fact]
        public void Parse_ReadsStoredTicks()
        {
            var issued = Now.AddMinutes( -5 );

            Assert.Equal( issued, AccountController.ParseConfirmationCodeIssued( Ticks( issued ) ) );
        }

        [Fact]
        public void IssuedFiftyNineMinutesAgo_IsLive()
        {
            Assert.False( IsExpired( Ticks( Now.AddMinutes( -59 ) ) ) );
        }

        [Fact]
        public void IssuedExactlySixtyMinutesAgo_IsLive()
        {
            Assert.False( IsExpired( Ticks( Now.AddMinutes( -60 ) ) ) );
        }

        [Fact]
        public void IssuedSixtyMinutesAndOneTickAgo_IsExpired()
        {
            Assert.True( IsExpired( Ticks( Now.AddMinutes( -60 ).AddTicks( -1 ) ) ) );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "   " )]
        [InlineData( "abc" )]
        [InlineData( "1.5" )]
        [InlineData( "0" )]
        [InlineData( "-638000000000000000" )]
        [InlineData( "3155378976000000000" )] // DateTime.MaxValue.Ticks + 1
        [InlineData( "99999999999999999999" )] // does not fit in a long
        public void MissingOrUnparseableValue_IsExpired( string issued )
        {
            Assert.Null( AccountController.ParseConfirmationCodeIssued( issued ) );
            Assert.True( IsExpired( issued ) );
        }

        [Fact]
        public void LatestPossibleValue_IsLiveAndDoesNotThrow()
        {
            bool expired = true;
            var exception = Record.Exception( () => expired = IsExpired( Ticks( DateTime.MaxValue ) ) );

            Assert.Null( exception );
            Assert.False( expired );
        }
    }
}
