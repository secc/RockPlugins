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
using org.secc.LinkList.Services;

using Xunit;

namespace org.secc.LinkList.Tests
{
    public class OriginValidationTests
    {
        [Theory]
        [InlineData( "https://se.church" )]
        [InlineData( "https://www.se.church" )]
        [InlineData( "http://localhost:64706" )]
        [InlineData( "https://southeastchristian.org:443" )]
        public void Accepts_Valid_Origins( string origin )
        {
            Assert.True( LinkListService.IsValidOrigin( origin ) );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "se.church" )]                 // no scheme
        [InlineData( "ftp://se.church" )]           // wrong scheme
        [InlineData( "https://se.church/links" )]   // has a path
        [InlineData( "https://se.church/" )]        // trailing slash
        [InlineData( "https://se.church?x=1" )]     // query
        [InlineData( "javascript:alert(1)" )]
        public void Rejects_Invalid_Origins( string origin )
        {
            Assert.False( LinkListService.IsValidOrigin( origin ) );
        }
    }
}
