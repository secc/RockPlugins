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
using org.secc.PersonMatch;
using Xunit;

namespace org.secc.Test.PersonMatch
{
    public class FirstNameMatchesTests
    {
        // Shaped like the Diminutive Names defined values: the defined value first, then its "Goes By" names.
        private static readonly List<List<string>> _diminutiveGroups = new List<List<string>>
        {
            new List<string> { "aaron", "erin", "ronnie", "ron", "" },
            new List<string> { "bob", "robert" },
            new List<string> { "bill", "william", "robert" },
            new List<string> { "zed" }
        };

        [Theory]
        [InlineData( "Adelle", "Adelle", "Adelle", true )]      // first name and nick name both equal the input
        [InlineData( "Adelle", "Adelle", "Pamela", true )]      // ROCK-8822: exact first name, different nick name
        [InlineData( "Pamela", "Adelle", "Pamela", true )]      // exact nick name
        [InlineData( "Taylor", "Taylor", "Ty", true )]          // ROCK-8822: exact first name, nick name not in any group
        [InlineData( "adelle", "ADELLE", "Pamela", true )]      // case-insensitive
        [InlineData( " Adelle ", "Adelle", "Pamela", true )]    // surrounding whitespace ignored
        [InlineData( "Robert", "Robert", "Bob", true )]         // exact first name, also in a diminutive group
        [InlineData( "Bob", "Robert", "Bob", true )]            // exact nick name
        [InlineData( "Bob", "Robert", "Robert", true )]         // diminutive only
        [InlineData( "BILL", "Robert", "Bob", true )]           // diminutive, case-insensitive
        [InlineData( "Adelle", "Taylor", "Ty", false )]         // unrelated name
        [InlineData( "Zed", "Robert", "Bob", false )]           // single-name group does not link to other names
        public void MatchesExpected( string input, string firstName, string nickName, bool expected )
        {
            Assert.Equal( expected, Extension.FirstNameMatches( input, firstName, nickName, _diminutiveGroups ) );
        }

        [Theory]
        [InlineData( "Bob", null, "Robert", true )]      // ROCK-8821: null first name, diminutive nick name
        [InlineData( "Bob", null, "Zack", false )]       // ROCK-8821: null first name, unrelated nick name
        [InlineData( "Zack", null, "Zack", true )]       // null first name, exact nick name
        [InlineData( "Bob", "Robert", null, true )]      // ROCK-8821: null nick name, diminutive first name
        [InlineData( "Bob", "Zack", null, false )]       // ROCK-8821: null nick name, unrelated first name
        [InlineData( "Zack", "Zack", null, true )]       // null nick name, exact first name
        [InlineData( "Bob", null, null, true )]          // no names to compare: kept, as before
        public void NullPersonNamesDoNotThrow( string input, string firstName, string nickName, bool expected )
        {
            Assert.Equal( expected, Extension.FirstNameMatches( input, firstName, nickName, _diminutiveGroups ) );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "   " )]
        public void EmptyInputKeepsCandidate( string input )
        {
            // GetByMatch returns early for a blank first name, and the old loop skipped the name check
            // when the input was null, so a blank input never removes a candidate.
            Assert.True( Extension.FirstNameMatches( input, "Robert", "Bob", _diminutiveGroups ) );
        }

        [Theory]
        [InlineData( "Robert", "Zack", "Bill", true )]   // nick name is a diminutive of the input; first name unrelated
        [InlineData( "Robert", "Bill", "Zack", true )]   // first name is a diminutive of the input; nick name unrelated
        public void DiminutiveOfEitherNameMatchesWhenTheOtherIsUnrelated( string input, string firstName, string nickName, bool expected )
        {
            Assert.Equal( expected, Extension.FirstNameMatches( input, firstName, nickName, _diminutiveGroups ) );
        }

        [Fact]
        public void EmptyPersonNamesKeepCandidateBecauseThereIsNoNameToCompare()
        {
            // Empty first and nick names are treated like null ones: there is no name to compare, so the candidate is kept.
            Assert.True( Extension.FirstNameMatches( "Adelle", string.Empty, string.Empty, _diminutiveGroups ) );
        }

        [Fact]
        public void GroupWithoutGoesByDoesNotThrow()
        {
            // ROCK-8821: a defined value with no "Goes By" value becomes a group with a single name.
            var groups = new List<List<string>> { new List<string> { "zed" }, new List<string> { "bob", "robert" } };

            Assert.True( Extension.FirstNameMatches( "Bob", "Robert", "Robert", groups ) );
            Assert.False( Extension.FirstNameMatches( "Zed", "Robert", "Bob", groups ) );
        }

        [Fact]
        public void MissingGroupsStillMatchExactNames()
        {
            // ROCK-8821: a missing Diminutive Names defined type yields no groups.
            Assert.True( Extension.FirstNameMatches( "Adelle", "Adelle", "Pamela", new List<List<string>>() ) );
            Assert.True( Extension.FirstNameMatches( "Adelle", "Adelle", "Pamela", null ) );
            Assert.False( Extension.FirstNameMatches( "Bob", "Robert", "Robert", null ) );
            Assert.False( Extension.FirstNameMatches( "Bob", "Robert", "Robert", new List<List<string>> { null } ) );
        }
    }
}
