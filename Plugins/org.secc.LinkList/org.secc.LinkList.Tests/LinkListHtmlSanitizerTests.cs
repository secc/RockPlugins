// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using org.secc.LinkList.Utility;

using Xunit;

namespace org.secc.LinkList.Tests
{
    public class LinkListHtmlSanitizerTests
    {
        [Fact]
        public void Drops_Script_Keeps_Surrounding_Markup()
        {
            var result = LinkListHtmlSanitizer.Sanitize( "<p>hi</p><script>alert(1)</script>" );
            Assert.DoesNotContain( "script", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "<p>hi</p>", result );
        }

        [Fact]
        public void Strips_Event_Handler_Attribute()
        {
            var result = LinkListHtmlSanitizer.Sanitize( "<img src=\"x\" onerror=\"alert(1)\">" );
            Assert.DoesNotContain( "onerror", result, System.StringComparison.OrdinalIgnoreCase );
        }

        [Fact]
        public void Neutralizes_Plain_Javascript_Href()
        {
            var result = LinkListHtmlSanitizer.Sanitize( "<a href=\"javascript:alert(1)\">x</a>" );
            Assert.DoesNotContain( "javascript", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( ">x</a>", result );
        }

        [Fact]
        public void Neutralizes_Entity_Encoded_Javascript_Href()
        {
            // java&#9;script: -> decoded tab -> stripped -> javascript: detected.
            var result = LinkListHtmlSanitizer.Sanitize( "<a href=\"java&#9;script:alert(1)\">x</a>" );
            Assert.DoesNotContain( "script:", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( ">x</a>", result );
        }

        [Theory]
        [InlineData( "math" )]
        [InlineData( "template" )]
        [InlineData( "frame" )]
        [InlineData( "frameset" )]
        [InlineData( "iframe" )]
        [InlineData( "object" )]
        [InlineData( "svg" )]
        public void Drops_Blacklisted_Tags( string tag )
        {
            var result = LinkListHtmlSanitizer.Sanitize( $"<p>ok</p><{tag}>x</{tag}>" );
            Assert.DoesNotContain( "<" + tag, result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "<p>ok</p>", result );
        }

        [Fact]
        public void Preserves_Html_Entities()
        {
            var result = LinkListHtmlSanitizer.Sanitize( "<p>&copy; SE Church</p>" );
            Assert.Contains( "&copy;", result );
        }

        [Fact]
        public void Is_Idempotent()
        {
            const string input = "<p>&copy; <a href=\"https://se.church/give\">give</a></p>";
            var once = LinkListHtmlSanitizer.Sanitize( input );
            var twice = LinkListHtmlSanitizer.Sanitize( once );
            Assert.Equal( once, twice );
        }

        [Fact]
        public void Preserves_Legacy_Table_Font_Footer_Content()
        {
            var result = LinkListHtmlSanitizer.Sanitize(
                "<table><tr><td>Hi <font color=\"red\">there</font> <img src=\"https://x/y.png\"></td></tr></table>" );
            Assert.Contains( "Hi", result );
            Assert.Contains( "there", result );
            Assert.Contains( "https://x/y.png", result );
        }

        [Fact]
        public void Keeps_Safe_Https_Href()
        {
            var result = LinkListHtmlSanitizer.Sanitize( "<a href=\"https://se.church/give\">give</a>" );
            Assert.Contains( "https://se.church/give", result );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "   " )]
        public void Passes_Through_Null_Or_Empty( string input )
        {
            Assert.Equal( input, LinkListHtmlSanitizer.Sanitize( input ) );
        }
    }
}
