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
    public class SlugValidationTests
    {
        [Theory]
        [InlineData( "my-list", "my-list" )]
        [InlineData( "My-List", "my-list" )]
        [InlineData( "  MY-LIST  ", "my-list" )]
        [InlineData( null, null )]
        public void NormalizeSlug_Trims_And_Lowercases( string input, string expected )
        {
            Assert.Equal( expected, LinkListService.NormalizeSlug( input ) );
        }

        [Theory]
        [InlineData( "my-list" )]
        [InlineData( "list-2024" )]
        [InlineData( "a" )]
        [InlineData( "0" )]
        public void IsValidSlug_Accepts_Canonical_Slugs( string slug )
        {
            Assert.True( LinkListService.IsValidSlug( slug ) );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "   " )]
        [InlineData( "My-List" )]      // uppercase: callers must normalize first
        [InlineData( "my list" )]      // whitespace
        [InlineData( "my_list" )]      // underscore
        [InlineData( "list/../etc" )]  // path characters
        [InlineData( "<script>" )]
        public void IsValidSlug_Rejects_Non_Canonical_Input( string slug )
        {
            Assert.False( LinkListService.IsValidSlug( slug ) );
        }

        [Fact]
        public void IsValidSlug_Enforces_Max_Length()
        {
            Assert.True( LinkListService.IsValidSlug( new string( 'a', LinkListService.MaxSlugLength ) ) );
            Assert.False( LinkListService.IsValidSlug( new string( 'a', LinkListService.MaxSlugLength + 1 ) ) );
        }
    }
}
