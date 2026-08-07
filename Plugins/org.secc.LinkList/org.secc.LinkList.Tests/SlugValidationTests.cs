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

using org.secc.LinkList.Services;
using org.secc.LinkList.Utility;

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

        // ---- Multi-slug set validation (every slug in the set is validated) ----

        [Fact]
        public void ValidateSubmitted_Accepts_A_Set_Of_Valid_Slugs()
        {
            var submitted = new List<SubmittedSlug>
            {
                new SubmittedSlug { Slug = "my-list", IsPrimary = true },
                new SubmittedSlug { Slug = "list-2024" },
                new SubmittedSlug { Slug = "0" }
            };
            Assert.Null( SlugReconciler.ValidateSubmitted( submitted ) );
        }

        [Fact]
        public void ValidateSubmitted_Rejects_When_Any_Slug_In_Set_Is_Invalid()
        {
            var submitted = new List<SubmittedSlug>
            {
                new SubmittedSlug { Slug = "good-slug", IsPrimary = true },
                new SubmittedSlug { Slug = "bad slug" } // whitespace -> invalid
            };
            var error = SlugReconciler.ValidateSubmitted( submitted );
            Assert.NotNull( error );
            Assert.Contains( "bad slug", error );
        }

        [Fact]
        public void ValidateSubmitted_Rejects_Duplicate_Slug_In_Set()
        {
            var submitted = new List<SubmittedSlug>
            {
                new SubmittedSlug { Slug = "dupe", IsPrimary = true },
                new SubmittedSlug { Slug = "dupe" }
            };
            var error = SlugReconciler.ValidateSubmitted( submitted );
            Assert.NotNull( error );
            Assert.Contains( "dupe", error );
        }

        [Fact]
        public void ValidateSubmitted_Empty_Set_Is_Valid()
        {
            Assert.Null( SlugReconciler.ValidateSubmitted( new List<SubmittedSlug>() ) );
        }
    }
}
