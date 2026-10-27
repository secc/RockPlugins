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
using System.Collections.Generic;
using Xunit;

namespace org.secc.SignNowWorkflow.Tests
{
    public class SignNowDownloadDocumentIdTests
    {
        // 40 lowercase hex characters, the form SignNow issues.
        private const string HexId = "0123456789abcdef0123456789abcdef01234567";

        [Theory]
        [InlineData( HexId )]
        [InlineData( "AbCdEfGhIjKlMnOpQrStUvWxYz0123456789AbCd" )]
        [InlineData( "abc_def_0123456789abcdef0123456789abcdef" )]
        [InlineData( "AZaz09_AZaz09_AZaz09_AZaz09_AZaz09_AZaz0" )]
        [InlineData( "________________________________________" )]
        public void IsValidDocumentId_AcceptsSignNowIds( string documentId )
        {
            Assert.Equal( 40, documentId.Length );
            Assert.True( SignNowDownload.IsValidDocumentId( documentId ) );
        }

        public static IEnumerable<object[]> OtherValues()
        {
            yield return new object[] { null };
            yield return new object[] { "" };
            yield return new object[] { " " };
            yield return new object[] { new string( ' ', 40 ) };

            // Wrong length.
            yield return new object[] { HexId.Substring( 1 ) };
            yield return new object[] { HexId + "a" };
            yield return new object[] { HexId + HexId };

            // Valid characters with leading or trailing whitespace (Execute trims before checking).
            yield return new object[] { HexId + "\n" };
            yield return new object[] { HexId + "\r\n" };
            yield return new object[] { " " + HexId };
            yield return new object[] { HexId.Substring( 1 ) + "\n" };

            // 40 characters, one of which is outside A-Z, a-z, 0-9 and _.
            foreach ( var c in new[] { '/', '\\', '.', '?', '#', '&', '=', '%', '+', '-', ' ', '\t', '\0', ':', 'é', 'K', '١', '０' } )
            {
                yield return new object[] { HexId.Substring( 0, 20 ) + c + HexId.Substring( 21 ) };
            }
        }

        [Theory]
        [MemberData( nameof( OtherValues ) )]
        public void IsValidDocumentId_RejectsOtherValues( string documentId )
        {
            Assert.False( SignNowDownload.IsValidDocumentId( documentId ) );
        }

        [Theory]
        [InlineData( HexId )]
        [InlineData( " " + HexId )]
        [InlineData( HexId + "\n" )]
        [InlineData( HexId + "\r\n" )]
        [InlineData( "\t" + HexId + " " )]
        public void GetDocumentId_ReturnsTheTrimmedId( string value )
        {
            var errorMessages = new List<string>();

            Assert.Equal( HexId, SignNowDownload.GetDocumentId( value, errorMessages ) );
            Assert.Empty( errorMessages );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        public void GetDocumentId_RejectsBlankValues( string value )
        {
            var errorMessages = new List<string>();

            Assert.Null( SignNowDownload.GetDocumentId( value, errorMessages ) );
            Assert.Equal( new[] { "A sign now document is required to complete this action" }, errorMessages );
        }

        [Theory]
        [InlineData( " " )]
        [InlineData( "abc123" )]
        [InlineData( " " + HexId + "a" )]
        [InlineData( "0123456789abcdef0123/56789abcdef01234567" )]
        [InlineData( "0123456789abcdef0123é56789abcdef01234567" )]
        public void GetDocumentId_RejectsValuesNotInTheIdFormat( string value )
        {
            var errorMessages = new List<string>();

            Assert.Null( SignNowDownload.GetDocumentId( value, errorMessages ) );
            Assert.Equal( new[] { "A valid SignNow document id is required to complete this action." }, errorMessages );
        }
    }
}
