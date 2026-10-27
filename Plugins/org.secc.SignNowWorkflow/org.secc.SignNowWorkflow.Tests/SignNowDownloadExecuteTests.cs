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
using Rock.Model;
using Xunit;

namespace org.secc.SignNowWorkflow.Tests
{
    public class SignNowDownloadExecuteTests
    {
        private const string HexId = "0123456789abcdef0123456789abcdef01234567";
        private const string TokenErrorMessage = "Token request stopped by the test.";

        /// <summary>
        /// SignNow Download with the attribute read and the token request replaced, so Execute runs without Rock's
        /// database or SignNow. The token request counts the calls and fails, which ends Execute.
        /// </summary>
        private class TestSignNowDownload : SignNowDownload
        {
            private readonly string _documentIdValue;

            public TestSignNowDownload( string documentIdValue )
            {
                _documentIdValue = documentIdValue;
            }

            public int TokenRequests { get; private set; }

            protected override string GetDocumentIdValue( WorkflowAction action )
            {
                return _documentIdValue;
            }

            protected override string RequestAccessToken( out string errorMessage )
            {
                TokenRequests++;
                errorMessage = TokenErrorMessage;
                return null;
            }
        }

        [Theory]
        [InlineData( " " )]
        [InlineData( "abc123" )]
        [InlineData( " " + HexId + "a" )]
        [InlineData( "0123456789abcdef0123/56789abcdef01234567" )]
        [InlineData( "0123456789abcdef0123é56789abcdef01234567" )]
        public void Execute_InvalidDocumentId_ReturnsFalseBeforeTokenRequest( string value )
        {
            var download = new TestSignNowDownload( value );

            bool result = download.Execute( null, new WorkflowAction(), null, out List<string> errorMessages );

            Assert.False( result );
            Assert.Equal( 0, download.TokenRequests );
            Assert.Equal( new[] { "A valid SignNow document id is required to complete this action." }, errorMessages );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        public void Execute_BlankDocumentId_ReturnsFalseBeforeTokenRequest( string value )
        {
            var download = new TestSignNowDownload( value );

            bool result = download.Execute( null, new WorkflowAction(), null, out List<string> errorMessages );

            Assert.False( result );
            Assert.Equal( 0, download.TokenRequests );
            Assert.Equal( new[] { "A sign now document is required to complete this action" }, errorMessages );
        }

        [Theory]
        [InlineData( HexId )]
        [InlineData( " " + HexId + "\r\n" )]
        [InlineData( "AZaz09_AZaz09_AZaz09_AZaz09_AZaz09_AZaz0" )]
        public void Execute_ValidDocumentId_PassesTheCheckAndRequestsAToken( string value )
        {
            var download = new TestSignNowDownload( value );

            bool result = download.Execute( null, new WorkflowAction(), null, out List<string> errorMessages );

            // The test's token request fails, so Execute stops right after it.
            Assert.False( result );
            Assert.Equal( 1, download.TokenRequests );
            Assert.Equal( new[] { TokenErrorMessage }, errorMessages );
        }
    }
}
