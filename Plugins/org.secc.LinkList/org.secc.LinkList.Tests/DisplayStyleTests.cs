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
using org.secc.LinkList.SystemGuids;

using Xunit;

namespace org.secc.LinkList.Tests
{
    /// <summary>ROCK-9361: link display style (button | card) normalization.</summary>
    public class DisplayStyleTests
    {
        [Theory]
        [InlineData( "card" )]
        [InlineData( "Card" )]
        [InlineData( "  CARD  " )]
        public void NormalizeDisplayStyle_Card_Variants_Are_Card( string input )
        {
            Assert.Equal( LinkListGuids.DisplayStyleValue.Card, LinkListService.NormalizeDisplayStyle( input ) );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "   " )]
        [InlineData( "button" )]
        [InlineData( "Button" )]
        [InlineData( "tile" )]
        public void NormalizeDisplayStyle_Defaults_To_Button( string input )
        {
            // Blank is every row saved before migration 012 - it must stay a button.
            Assert.Equal( LinkListGuids.DisplayStyleValue.Button, LinkListService.NormalizeDisplayStyle( input ) );
        }

        [Theory]
        [InlineData( null, "link" )]
        [InlineData( "Section", "section" )]
        [InlineData( " separator ", "separator" )]
        [InlineData( "bogus", "link" )]
        public void NormalizeItemType_Maps_To_Known_Types( string input, string expected )
        {
            Assert.Equal( expected, LinkListService.NormalizeItemType( input ) );
        }
    }
}
