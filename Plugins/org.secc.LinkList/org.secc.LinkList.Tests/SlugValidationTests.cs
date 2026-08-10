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

        // ---- Write-path canonicalization (mirrors Rock's MakeSlugValid) ----

        [Theory]
        [InlineData( "my-list", "my-list" )]           // already canonical
        [InlineData( "My-List", "my-list" )]
        [InlineData( "  MY-LIST  ", "my-list" )]
        [InlineData( "Give Now", "give-now" )]          // space -> dash
        [InlineData( "  My_List  ", "my-list" )]        // underscore -> dash
        [InlineData( "Summer Camp 2026", "summer-camp-2026" )] // the title-derived case
        [InlineData( "give--now", "give-now" )]         // dash run collapsed
        [InlineData( "a&nbsp;b", "a-b" )]               // dash entities
        [InlineData( "a&#8212;b", "a-b" )]
        [InlineData( "foo-", "foo" )]                   // trailing dash trimmed
        [InlineData( "-foo", "-foo" )]                  // leading dash KEPT (Rock parity)
        [InlineData( "café!", "caf" )]             // non-ASCII dropped, not folded
        [InlineData( "list/../etc", "listetc" )]
        [InlineData( "<script>", "script" )]
        [InlineData( "-", "" )]                         // nothing usable survives
        [InlineData( "---", "" )]
        [InlineData( "&", "" )]
        [InlineData( "!!!", "" )]
        [InlineData( "   ", "" )]
        [InlineData( "", "" )]
        [InlineData( null, "" )]
        public void CanonicalizeSlug_Produces_The_Text_Rock_Stores( string input, string expected )
        {
            Assert.Equal( expected, LinkListService.CanonicalizeSlug( input ) );
        }

        [Fact]
        public void CanonicalizeSlug_Collapses_Dashes_Left_By_Stripped_Characters()
        {
            // Rock's MakeSlugValid collapses dash runs BEFORE stripping invalid
            // characters, so its single pass yields "rock--roll" here. We strip
            // first so the result survives a second pass unchanged - see
            // CanonicalizeSlug_Is_A_Fixed_Point for why that matters.
            Assert.Equal( "rock-roll", LinkListService.CanonicalizeSlug( "Rock & Roll" ) );
        }

        [Fact]
        public void CanonicalizeSlug_Enforces_Max_Length()
        {
            var overLength = new string( 'a', LinkListService.MaxSlugLength + 1 );
            Assert.Equal( new string( 'a', LinkListService.MaxSlugLength ), LinkListService.CanonicalizeSlug( overLength ) );

            // Truncation happens before the trailing dash is trimmed, so a cut
            // landing on a dash yields one character less.
            var cutOnDash = new string( 'a', LinkListService.MaxSlugLength - 1 ) + "-b";
            Assert.Equal( new string( 'a', LinkListService.MaxSlugLength - 1 ), LinkListService.CanonicalizeSlug( cutOnDash ) );
        }

        [Theory]
        [InlineData( "Rock & Roll" )]
        [InlineData( "Give Now" )]
        [InlineData( "give--now" )]
        [InlineData( "  My_List  " )]
        [InlineData( "-foo-" )]
        [InlineData( "café!" )]
        [InlineData( "a&mdash;b" )]
        [InlineData( "-" )]
        [InlineData( null )]
        public void CanonicalizeSlug_Is_A_Fixed_Point( string input )
        {
            // The guarantee the whole write path rests on: Rock's SaveSlug re-runs
            // MakeSlugValid on whatever it is given, so the text validated here must
            // survive canonicalization again unchanged - otherwise stored text
            // diverges from validated text and the conflict check, reconcile diff and
            // primary-flag match all compare the wrong thing.
            var once = LinkListService.CanonicalizeSlug( input );
            Assert.Equal( once, LinkListService.CanonicalizeSlug( once ) );
        }

        [Theory]
        [InlineData( "Rock & Roll" )]
        [InlineData( "Summer Camp 2026" )]
        [InlineData( "give--now" )]
        [InlineData( "-foo-" )]
        public void CanonicalizeSlug_Output_Passes_IsValidSlug( string input )
        {
            var slug = LinkListService.CanonicalizeSlug( input );
            Assert.NotEqual( string.Empty, slug );
            Assert.True( LinkListService.IsValidSlug( slug ) );
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
