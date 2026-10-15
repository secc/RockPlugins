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
using System.IO;
using Xunit;

namespace org.secc.SignNowWorkflow.Tests
{
    public class SignNowCreateTempFilePathTests
    {
        private static readonly string TempDirectory = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() );

        [Theory]
        [InlineData( @"x\..\..\y.pdf" )]
        [InlineData( "../y.pdf" )]
        [InlineData( ".." )]
        [InlineData( ". ." )]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "Doe, Jane.pdf" )]
        [InlineData( "MedicalIncidentReport_Jane Doe.pdf" )]
        public void BuildTempFilePath_StaysDirectlyInsideTempDirectory( string fileName )
        {
            var fullPath = Path.GetFullPath( SignNowCreate.BuildTempFilePath( TempDirectory, fileName ) );

            Assert.Equal( TempDirectory, Path.GetDirectoryName( fullPath ) );
            Assert.EndsWith( ".pdf", fullPath );
        }

        [Theory]
        [InlineData( @"x\..\..\y.pdf", "x_.._.._y.pdf" )]
        [InlineData( "../y.pdf", ".._y.pdf" )]
        [InlineData( "..", "document.pdf" )]
        [InlineData( ". .", "document.pdf" )]
        [InlineData( null, "document.pdf" )]
        [InlineData( "", "document.pdf" )]
        [InlineData( "Doe, Jane.pdf", "Doe Jane.pdf" )]
        [InlineData( "MedicalIncidentReport_Jane Doe.pdf", "MedicalIncidentReport_Jane Doe.pdf" )]
        public void BuildTempFilePath_UsesValidFileNameOrDefault( string fileName, string expectedFileName )
        {
            var path = SignNowCreate.BuildTempFilePath( TempDirectory, fileName );

            Assert.Equal( Path.Combine( TempDirectory, expectedFileName ), path );
        }
    }
}
