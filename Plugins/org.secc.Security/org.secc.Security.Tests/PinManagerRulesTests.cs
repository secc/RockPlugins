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
    /// PinManagerRules.DisplayPin for users who can and can't administrate PINs, PinManagerRules.IsPersonPin for
    /// each kind of login, PinManagerRules.CheckPin for new, changed and unchanged PINs, and PinManagerRules.PurposeText
    /// and PurposeItemText for purpose names.
    /// </summary>
    public class PinManagerRulesTests
    {
        private const int PinTypeId = 233;
        private const int DatabaseTypeId = 27;
        private const int PagePersonId = 72;
        private const int OtherPersonId = 73;

        private const string ConvertMessage = "Error: Requested PIN could not be converted to number.";
        private const string TakenMessage = "This PIN has already been taken.";

        #region DisplayPin

        [Fact]
        public void MaskedPin_IsFourBullets()
        {
            Assert.Equal( "\u2022\u2022\u2022\u2022", PinManagerRules.MaskedPin );
        }

        [Theory]
        [InlineData( "123456" )]
        [InlineData( "012345" )]
        [InlineData( "98765432101" )]
        [InlineData( " 123456 " )]
        public void DisplayPin_Administrator_SeesThePin( string pin )
        {
            Assert.Equal( pin, PinManagerRules.DisplayPin( pin, true ) );
        }

        [Theory]
        [InlineData( "123456" )]
        [InlineData( "012345" )]
        [InlineData( "98765432101" )]
        [InlineData( "1234" )]
        [InlineData( "" )]
        [InlineData( null )]
        public void DisplayPin_NonAdministrator_SeesTheMask( string pin )
        {
            var shown = PinManagerRules.DisplayPin( pin, false );

            Assert.Equal( PinManagerRules.MaskedPin, shown );
            Assert.DoesNotContain( shown, char.IsDigit );
        }

        [Fact]
        public void DisplayPin_NonAdministrator_SeesTheSameTextForPinsOfDifferentLengths()
        {
            Assert.Equal( PinManagerRules.DisplayPin( "123456", false ), PinManagerRules.DisplayPin( "98765432101", false ) );
        }

        #endregion

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

        #region PurposeText and PurposeItemText

        private const string MarkupName = "<b>Sports</b> & <script>x()</script>";
        private const string EncodedMarkupName = "&lt;b&gt;Sports&lt;/b&gt; &amp; &lt;script&gt;x()&lt;/script&gt;";

        private static readonly Dictionary<int, string> Purposes = new Dictionary<int, string>
        {
            { 4541, "Children's Ministry" },
            { 4542, "Sports and Fitness" },
            { 9001, MarkupName },
            { 9002, "Q&A" },
            { 9003, "" }
        };

        private static string PurposeName( int id )
        {
            return Purposes.TryGetValue( id, out var name ) ? name : null;
        }

        [Fact]
        public void NoPurpose_IsTheBracketedText()
        {
            Assert.Equal( "[No Purpose]", PinManagerRules.NoPurpose );
        }

        [Fact]
        public void PurposeText_MarkupInAName_IsEncoded()
        {
            Assert.Equal( EncodedMarkupName, PinManagerRules.PurposeText( "9001", PurposeName ) );
        }

        [Fact]
        public void PurposeText_AmpersandInAName_IsEncodedOnce()
        {
            Assert.Equal( "Q&amp;A", PinManagerRules.PurposeText( "9002", PurposeName ) );
        }

        [Fact]
        public void PurposeText_SeveralPurposes_AreEncodedAndJoinedInStoredOrder()
        {
            Assert.Equal( "Sports and Fitness, " + EncodedMarkupName + ", Children&#39;s Ministry",
                PinManagerRules.PurposeText( "4542|9001|4541", PurposeName ) );
        }

        [Fact]
        public void PurposeText_RepeatedId_IsShownEachTime()
        {
            Assert.Equal( "Sports and Fitness, Sports and Fitness", PinManagerRules.PurposeText( "4542|4542", PurposeName ) );
        }

        [Fact]
        public void PurposeText_EmptyName_IsKeptAsAnEmptyEntry()
        {
            Assert.Equal( "Sports and Fitness, , Q&amp;A", PinManagerRules.PurposeText( "4542|9003|9002", PurposeName ) );
        }

        [Fact]
        public void PurposeText_PlainName_IsUnchanged()
        {
            Assert.Equal( "Sports and Fitness", PinManagerRules.PurposeText( "4542", PurposeName ) );
        }

        [Fact]
        public void PurposeText_NameWithApostrophe_DisplaysTheSame()
        {
            var text = PinManagerRules.PurposeText( "4541", PurposeName );

            Assert.Equal( "Children&#39;s Ministry", text );
            Assert.Equal( "Children's Ministry", System.Net.WebUtility.HtmlDecode( text ) );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "|" )]
        [InlineData( "||" )]
        public void PurposeText_NoIds_IsNoPurpose( string value )
        {
            Assert.Equal( PinManagerRules.NoPurpose, PinManagerRules.PurposeText( value, PurposeName ) );
        }

        [Theory]
        [InlineData( "123" )]
        [InlineData( "123|456" )]
        [InlineData( "abc" )]
        [InlineData( " " )]
        public void PurposeText_OnlyUnknownIds_IsEmpty( string value )
        {
            Assert.Equal( string.Empty, PinManagerRules.PurposeText( value, PurposeName ) );
        }

        [Fact]
        public void PurposeText_UnknownIdsAmongKnownOnes_AreLeftOut()
        {
            Assert.Equal( "Sports and Fitness, Q&amp;A", PinManagerRules.PurposeText( "123|4542||abc|9002", PurposeName ) );
        }

        [Fact]
        public void PurposeText_LooksUpEachIdAsANumber()
        {
            var asked = new List<int>();

            PinManagerRules.PurposeText( "4542|x|9001", id => { asked.Add( id ); return PurposeName( id ); } );

            Assert.Equal( new[] { 4542, 0, 9001 }, asked );
        }

        [Fact]
        public void PurposeItemText_MarkupInAName_IsEncoded()
        {
            Assert.Equal( EncodedMarkupName, PinManagerRules.PurposeItemText( MarkupName ) );
        }

        [Theory]
        [InlineData( "Sports and Fitness", "Sports and Fitness" )]
        [InlineData( "Q&A", "Q&amp;A" )]
        [InlineData( "Children's Ministry", "Children&#39;s Ministry" )]
        public void PurposeItemText_EncodesOnce( string name, string expected )
        {
            Assert.Equal( expected, PinManagerRules.PurposeItemText( name ) );
        }

        #endregion
    }
}
