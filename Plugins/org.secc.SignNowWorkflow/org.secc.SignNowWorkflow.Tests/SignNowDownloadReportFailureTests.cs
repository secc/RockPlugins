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
using System.IO;
using Xunit;

namespace org.secc.SignNowWorkflow.Tests
{
    public class SignNowDownloadReportFailureTests
    {
        private const string DownloadErrorMessage = "SignNow Download Error: the signed document could not be downloaded from SignNow.";
        private const string Detail = "{\"errors\":[{\"code\":65582,\"message\":\"sample detail\"}]}";

        [Fact]
        public void ReportFailure_ShowsOnlyTheUserMessageAndLogsTheDetail()
        {
            var logged = new List<Exception>();

            var shown = SignNowDownload.ReportFailure( DownloadErrorMessage, 5579, 1234, Detail, null, logged.Add );

            Assert.Equal( DownloadErrorMessage, shown );
            var entry = Assert.Single( logged );
            Assert.Equal( DownloadErrorMessage + " (action type 5579, workflow 1234) " + Detail, entry.Message );
            Assert.Null( entry.InnerException );
        }

        [Fact]
        public void ReportFailure_LogsTheCaughtExceptionAsInnerException()
        {
            var logged = new List<Exception>();
            var caught = new IOException( @"Could not find a part of the path 'C:\Temp\x\signed.pdf'." );

            var shown = SignNowDownload.ReportFailure( DownloadErrorMessage, 5579, 1234, null, caught, logged.Add );

            Assert.Equal( DownloadErrorMessage, shown );
            var entry = Assert.Single( logged );
            Assert.Equal( DownloadErrorMessage + " (action type 5579, workflow 1234)", entry.Message );
            Assert.Same( caught, entry.InnerException );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( 0 )]
        public void ReportFailure_OmitsTheWorkflowWhenItHasNoId( int? workflowId )
        {
            var logged = new List<Exception>();

            var shown = SignNowDownload.ReportFailure( DownloadErrorMessage, 24377, workflowId, "No response from SignNow.", null, logged.Add );

            Assert.Equal( DownloadErrorMessage, shown );
            var entry = Assert.Single( logged );
            Assert.Equal( DownloadErrorMessage + " (action type 24377) No response from SignNow.", entry.Message );
        }
    }
}
