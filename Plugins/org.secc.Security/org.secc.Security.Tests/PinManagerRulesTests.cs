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
using System.Linq;
using org.secc.Security.Utility;
using Rock.Model;
using Xunit;

namespace org.secc.Security.Tests
{
    /// <summary>
    /// PinManagerRules.IsPersonPin for each kind of login, and PinManagerRules.CheckPin for new, changed and
    /// unchanged PINs.
    /// </summary>
    public class PinManagerRulesTests
    {
        private const int PinTypeId = 233;
        private const int DatabaseTypeId = 27;
        private const int PagePersonId = 72;
        private const int OtherPersonId = 73;

        private const string ConvertMessage = "Error: Requested PIN could not be converted to number.";
        private const string TakenMessage = "This PIN has already been taken.";

        #region IsPersonPin

        [Fact]
        public void IsPersonPin_PinOfThePagePerson_IsTrue()
        {
            var login = new UserLogin { Id = 10, UserName = "7088541", PersonId = PagePersonId, EntityTypeId = PinTypeId };

            Assert.True( PinManagerRules.IsPersonPin( login, PagePersonId, PinTypeId ) );
        }

        [Fact]
        public void IsPersonPin_PinOfAnotherPerson_IsFalse()
        {
            var login = new UserLogin { Id = 11, UserName = "7088542", PersonId = OtherPersonId, EntityTypeId = PinTypeId };

            Assert.False( PinManagerRules.IsPersonPin( login, PagePersonId, PinTypeId ) );
        }

        [Fact]
        public void IsPersonPin_OtherLoginTypeOfThePagePerson_IsFalse()
        {
            var login = new UserLogin { Id = 12, UserName = "pageperson", PersonId = PagePersonId, EntityTypeId = DatabaseTypeId };

            Assert.False( PinManagerRules.IsPersonPin( login, PagePersonId, PinTypeId ) );
        }

        [Fact]
        public void IsPersonPin_LoginWithApiKeyOfAnotherPerson_IsFalse()
        {
            var login = new UserLogin { Id = 13, UserName = "restkey", ApiKey = "abc123", PersonId = OtherPersonId, EntityTypeId = DatabaseTypeId };

            Assert.False( PinManagerRules.IsPersonPin( login, PagePersonId, PinTypeId ) );
        }

        [Fact]
        public void IsPersonPin_NoLogin_IsFalse()
        {
            Assert.False( PinManagerRules.IsPersonPin( null, PagePersonId, PinTypeId ) );
        }

        [Fact]
        public void IsPersonPin_PinWithoutPerson_IsFalse()
        {
            var login = new UserLogin { Id = 14, UserName = "7088543", PersonId = null, EntityTypeId = PinTypeId };

            Assert.False( PinManagerRules.IsPersonPin( login, PagePersonId, PinTypeId ) );
        }

        [Fact]
        public void IsPersonPin_LoginOfThePagePersonWithoutType_IsFalse()
        {
            var login = new UserLogin { Id = 15, UserName = "7088544", PersonId = PagePersonId, EntityTypeId = null };

            Assert.False( PinManagerRules.IsPersonPin( login, PagePersonId, PinTypeId ) );
        }

        #endregion

        #region CheckPin

        /// <summary>
        /// Stands in for the login table: answers whether a user name is taken and records each user name asked about.
        /// User names are compared the way the database compares them: case-insensitive, ignoring trailing spaces.
        /// </summary>
        private class Logins
        {
            private readonly List<string> _userNames;

            public Logins( params string[] userNames )
            {
                _userNames = new List<string>( userNames );
            }

            public List<string> Asked { get; } = new List<string>();

            public bool IsTaken( string userName )
            {
                Asked.Add( userName );
                return _userNames.Any( u => string.Equals( u.TrimEnd( ' ' ), userName.TrimEnd( ' ' ), StringComparison.OrdinalIgnoreCase ) );
            }
        }

        private static string Check( string text, string currentPin, Logins logins, out string pin, int minimumLength = 6 )
        {
            return PinManagerRules.CheckPin( text, currentPin, minimumLength, logins.IsTaken, out pin );
        }

        [Fact]
        public void CheckPin_NewPinWithLeadingZero_KeepsItAndCountsIt()
        {
            var logins = new Logins();

            Assert.Null( Check( "012345", null, logins, out var pin ) );
            Assert.Equal( "012345", pin );
            Assert.Equal( new[] { "012345" }, logins.Asked );
        }

        [Fact]
        public void CheckPin_NewPinWithLeadingZero_IsNotTakenByTheSameNumberWithoutIt()
        {
            var logins = new Logins( "123456" );

            Assert.Null( Check( "0123456", null, logins, out var pin ) );
            Assert.Equal( "0123456", pin );
            Assert.Equal( new[] { "0123456" }, logins.Asked );
        }

        [Fact]
        public void CheckPin_NewPinWithLeadingZero_IsTakenByTheSameText()
        {
            var logins = new Logins( "0123456" );

            Assert.Equal( TakenMessage, Check( "0123456", null, logins, out _ ) );
            Assert.Equal( new[] { "0123456" }, logins.Asked );
        }

        [Theory]
        [InlineData( " 0123456 " )]
        [InlineData( "\t0123456\r\n" )]
        public void CheckPin_NewPinWithSurroundingWhitespace_IsTrimmed( string text )
        {
            var logins = new Logins();

            Assert.Null( Check( text, null, logins, out var pin ) );
            Assert.Equal( "0123456", pin );
            Assert.Equal( new[] { "0123456" }, logins.Asked );
        }

        [Fact]
        public void CheckPin_NewPinWithEveryDigit_IsAccepted()
        {
            var logins = new Logins();

            Assert.Null( Check( "9876543210", null, logins, out var pin ) );
            Assert.Equal( "9876543210", pin );
        }

        [Theory]
        // Number formats other than digits.
        [InlineData( "1e5" )]
        [InlineData( "123,456" )]
        [InlineData( "-123456" )]
        [InlineData( "+123456" )]
        [InlineData( "12345.6" )]
        [InlineData( "$123456" )]
        // The characters on either side of 0-9.
        [InlineData( "12/3456" )]
        [InlineData( "12:3456" )]
        // A non-digit in the last place.
        [InlineData( "123456:" )]
        public void CheckPin_NewPinWithNonDigits_IsRefused( string text )
        {
            var logins = new Logins();

            Assert.Equal( ConvertMessage, Check( text, null, logins, out _ ) );
            Assert.Empty( logins.Asked );
        }

        [Theory]
        // Fullwidth and Arabic-Indic digits.
        [InlineData( "１２３４５６７" )]
        [InlineData( "١٢٣٤٥٦٧" )]
        [InlineData( "1 234 567" )]
        [InlineData( "123\n456" )]
        [InlineData( "abcdef" )]
        public void CheckPin_NewPinWithOtherCharacters_IsRefused( string text )
        {
            var logins = new Logins();

            Assert.Equal( ConvertMessage, Check( text, null, logins, out _ ) );
            Assert.Empty( logins.Asked );
        }

        [Theory]
        [InlineData( "000000" )]
        [InlineData( "0000000000" )]
        [InlineData( "0" )]
        [InlineData( "" )]
        [InlineData( "   " )]
        [InlineData( null )]
        public void CheckPin_NewPinBlankOrAllZeros_IsRefused( string text )
        {
            var logins = new Logins();

            Assert.Equal( ConvertMessage, Check( text, null, logins, out _ ) );
            Assert.Empty( logins.Asked );
        }

        [Fact]
        public void CheckPin_NewPinWithOneSignificantDigit_IsAccepted()
        {
            var logins = new Logins();

            Assert.Null( Check( "000001", null, logins, out var pin ) );
            Assert.Equal( "000001", pin );
            Assert.Equal( new[] { "000001" }, logins.Asked );
        }

        [Fact]
        public void CheckPin_NewPinShorterThanTheMinimum_IsRefused()
        {
            var logins = new Logins();

            Assert.Equal( "Error: Requested PIN must be 6 digits or greater.", Check( "12345", null, logins, out _ ) );
            Assert.Empty( logins.Asked );
        }

        [Fact]
        public void CheckPin_NewPinShorterThanTheMinimumOnceTrimmed_IsRefused()
        {
            var logins = new Logins();

            Assert.Equal( "Error: Requested PIN must be 6 digits or greater.", Check( " 12345 ", null, logins, out _ ) );
            Assert.Empty( logins.Asked );
        }

        [Fact]
        public void CheckPin_NewPinOfTheMinimumLength_IsAccepted()
        {
            Assert.Null( Check( "123456", null, new Logins(), out var pin ) );
            Assert.Equal( "123456", pin );
            Assert.Equal( "Error: Requested PIN must be 7 digits or greater.", Check( "123456", null, new Logins(), out _, 7 ) );
        }

        [Fact]
        public void CheckPin_NewPinTaken_IsRefused()
        {
            var logins = new Logins( "123456" );

            Assert.Equal( TakenMessage, Check( "123456", null, logins, out _ ) );
            Assert.Equal( new[] { "123456" }, logins.Asked );
        }

        [Fact]
        public void CheckPin_NewPinNotTaken_IsAccepted()
        {
            var logins = new Logins( "654321" );

            Assert.Null( Check( "123456", null, logins, out var pin ) );
            Assert.Equal( "123456", pin );
            Assert.Equal( new[] { "123456" }, logins.Asked );
        }

        [Fact]
        public void CheckPin_LongPinTaken_IsAskedAboutAsTyped()
        {
            var logins = new Logins( "12345678901234567" );

            Assert.Equal( TakenMessage, Check( "12345678901234567", null, logins, out _ ) );
            Assert.Equal( new[] { "12345678901234567" }, logins.Asked );
        }

        [Fact]
        public void CheckPin_UnchangedPinShorterThanTheMinimum_IsAccepted()
        {
            var logins = new Logins( "1234" );

            Assert.Null( Check( "1234", "1234", logins, out var pin ) );
            Assert.Equal( "1234", pin );
            Assert.Empty( logins.Asked );
        }

        [Fact]
        public void CheckPin_UnchangedPinWithLeadingZero_IsAccepted()
        {
            var logins = new Logins( "0123456", "123456" );

            Assert.Null( Check( "0123456", "0123456", logins, out var pin ) );
            Assert.Equal( "0123456", pin );
            Assert.Empty( logins.Asked );
        }

        [Fact]
        public void CheckPin_UnchangedPinWithNonDigits_KeepsItsValue()
        {
            var logins = new Logins( "123,456" );

            Assert.Null( Check( "123,456", "123,456", logins, out var pin ) );
            Assert.Equal( "123,456", pin );
            Assert.Empty( logins.Asked );
        }

        [Fact]
        public void CheckPin_UnchangedPinWithLeadingSpace_KeepsItsValue()
        {
            var logins = new Logins( " 123456" );

            // The block's PIN box posts its text trimmed.
            Assert.Null( Check( "123456", " 123456", logins, out var pin ) );
            Assert.Equal( " 123456", pin );
            Assert.Empty( logins.Asked );
        }

        [Fact]
        public void CheckPin_UnchangedPinWithTrailingSpace_KeepsItsValue()
        {
            var logins = new Logins( "123456 " );

            // The block's PIN box posts its text trimmed.
            Assert.Null( Check( "123456", "123456 ", logins, out var pin ) );
            Assert.Equal( "123456 ", pin );
            Assert.Empty( logins.Asked );
        }

        [Theory]
        [InlineData( "\t123456" )]
        [InlineData( "123456 " )]
        public void CheckPin_UnchangedPinWithOtherSurroundingWhitespace_KeepsItsValue( string stored )
        {
            var logins = new Logins( stored );

            // The block's PIN box posts its text trimmed.
            Assert.Null( Check( "123456", stored, logins, out var pin ) );
            Assert.Equal( stored, pin );
            Assert.Empty( logins.Asked );
        }

        [Fact]
        public void CheckPin_UnchangedPinTypedWithSurroundingSpaces_KeepsItsValue()
        {
            var logins = new Logins( "1234567" );

            Assert.Null( Check( " 1234567 ", "1234567", logins, out var pin ) );
            Assert.Equal( "1234567", pin );
            Assert.Empty( logins.Asked );
        }

        [Fact]
        public void CheckPin_UnchangedEmptyPin_KeepsItsValue()
        {
            var logins = new Logins( "" );

            Assert.Null( Check( "", "", logins, out var pin ) );
            Assert.Equal( "", pin );
            Assert.Empty( logins.Asked );
        }

        [Fact]
        public void CheckPin_PinDifferingFromTheCurrentPinOnlyInCase_IsCheckedAsChanged()
        {
            var logins = new Logins( "abc123" );

            Assert.Equal( ConvertMessage, Check( "ABC123", "abc123", logins, out _ ) );
            Assert.Empty( logins.Asked );
        }

        [Fact]
        public void CheckPin_ChangedPinGainingLeadingZero_IsCheckedAsTyped()
        {
            var logins = new Logins( "1234567" );

            Assert.Null( Check( "01234567", "1234567", logins, out var pin ) );
            Assert.Equal( "01234567", pin );
            Assert.Equal( new[] { "01234567" }, logins.Asked );
        }

        [Fact]
        public void CheckPin_ChangedPinWithNonDigits_IsRefused()
        {
            var logins = new Logins( "123,456" );

            Assert.Equal( ConvertMessage, Check( "123,4567", "123,456", logins, out _ ) );
            Assert.Empty( logins.Asked );
        }

        [Fact]
        public void CheckPin_ChangedPinShorterThanTheMinimum_IsRefused()
        {
            var logins = new Logins( "1234" );

            Assert.Equal( "Error: Requested PIN must be 6 digits or greater.", Check( "12345", "1234", logins, out _ ) );
            Assert.Empty( logins.Asked );
        }

        #endregion
    }
}
