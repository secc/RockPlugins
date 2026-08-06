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
using System.Globalization;
using System.Linq;
using System.Threading;

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

        // ----------------------------------------------------------------------
        // ROCK-8880: allowlist rewrite — new coverage
        // ----------------------------------------------------------------------

        [Theory]
        [InlineData( "color: rgb(255,255,255)" )]
        [InlineData( "text-align: center" )]
        [InlineData( "width: 50%" )]
        [InlineData( "font-size: 16px" )]
        public void Style_Keeps_Allowlisted_Property( string declaration )
        {
            var result = LinkListHtmlSanitizer.Sanitize( $"<p style=\"{declaration}\">x</p>" );
            Assert.Contains( declaration, result );
        }

        [Theory]
        [InlineData( "position: fixed" )]
        [InlineData( "z-index: 9999" )]
        public void Style_Drops_Disallowed_Property( string declaration )
        {
            var result = LinkListHtmlSanitizer.Sanitize( $"<p style=\"{declaration}\">x</p>" );
            // The only declaration is disallowed, so the whole style attr goes.
            Assert.DoesNotContain( "style", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( ">x</p>", result );
        }

        [Fact]
        public void Style_Mixed_Keeps_Only_Safe_Declarations()
        {
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"color: red; position: fixed; text-align: left\">x</p>" );
            Assert.Contains( "color: red", result );
            Assert.Contains( "text-align: left", result );
            Assert.DoesNotContain( "position", result, System.StringComparison.OrdinalIgnoreCase );
        }

        [Theory]
        [InlineData( "background-color: url(javascript:alert(1))" )]
        [InlineData( "width: expression(alert(1))" )]
        public void Style_Rejects_Dangerous_Value_Even_On_Allowed_Property( string declaration )
        {
            var result = LinkListHtmlSanitizer.Sanitize( $"<p style=\"{declaration}\">x</p>" );
            Assert.DoesNotContain( "style", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.DoesNotContain( "url(", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.DoesNotContain( "expression(", result, System.StringComparison.OrdinalIgnoreCase );
        }

        [Fact]
        public void Style_Empty_Attribute_Removed()
        {
            var result = LinkListHtmlSanitizer.Sanitize( "<p style=\"\">x</p>" );
            Assert.DoesNotContain( "style", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( ">x</p>", result );
        }

        [Fact]
        public void Style_Preserves_Important_On_Allowed_Property()
        {
            // Real prod value: line-height: 150% !important
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"line-height: 150% !important\">x</p>" );
            Assert.Contains( "line-height: 150% !important", result );
        }

        [Theory]
        [InlineData( "formaction" )]
        [InlineData( "poster" )]
        [InlineData( "background" )]
        [InlineData( "xlink:href" )]
        public void Non_Allowlisted_Url_Attribute_Is_Stripped( string attribute )
        {
            var result = LinkListHtmlSanitizer.Sanitize(
                $"<img src=\"https://x/y.png\" {attribute}=\"javascript:alert(1)\">" );
            Assert.DoesNotContain( attribute, result, System.StringComparison.OrdinalIgnoreCase );
            Assert.DoesNotContain( "javascript", result, System.StringComparison.OrdinalIgnoreCase );
        }

        [Theory]
        [InlineData( "data:text/html,<script>alert(1)</script>" )]
        [InlineData( "vbscript:msgbox(1)" )]
        public void Blocks_Data_And_Vbscript_On_Href( string url )
        {
            var result = LinkListHtmlSanitizer.Sanitize( $"<a href=\"{url}\">x</a>" );
            Assert.DoesNotContain( "data:", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.DoesNotContain( "vbscript:", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( ">x</a>", result );
        }

        [Theory]
        [InlineData( "data:image/png;base64,AAAA" )]
        [InlineData( "vbscript:msgbox(1)" )]
        public void Blocks_Data_And_Vbscript_On_Src( string url )
        {
            var result = LinkListHtmlSanitizer.Sanitize( $"<img src=\"{url}\">" );
            Assert.DoesNotContain( "data:", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.DoesNotContain( "vbscript:", result, System.StringComparison.OrdinalIgnoreCase );
        }

        [Fact]
        public void Non_Allowlisted_Benign_Tag_Is_Unwrapped_Keeping_Text()
        {
            var result = LinkListHtmlSanitizer.Sanitize( "<p>a <marquee>scroll</marquee> b</p>" );
            Assert.DoesNotContain( "<marquee", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "scroll", result );
            Assert.Contains( "a ", result );
            Assert.Contains( " b", result );
        }

        [Fact]
        public void Non_Allowlisted_Attribute_Stripped_From_Allowlisted_Tag()
        {
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p data-track=\"1\" contenteditable=\"true\" class=\"keep\">x</p>" );
            Assert.DoesNotContain( "data-track", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.DoesNotContain( "contenteditable", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "class=\"keep\"", result );
        }

        [Fact]
        public void Real_Prod_Malformed_Attribute_Soup_Is_Neutralized_And_Idempotent()
        {
            // Verbatim from prod item 9644 — mangled span/heading with a broken
            // style attribute and entity-encoded angle brackets. Must not yield
            // executable markup, and must be stable under re-sanitizing.
            const string input =
                "<h2 style=\"text-align: left;\" class=\"\"><span style=\"color: inherit; font-family: inherit;&gt;Changes to Registration&lt;/span&gt;&lt;/h2&gt;&lt;p style=\" text-align:=\"\" left;\"=\"\">Changes to Registration</span></h2>";

            var once = LinkListHtmlSanitizer.Sanitize( input );
            var twice = LinkListHtmlSanitizer.Sanitize( once );

            Assert.DoesNotContain( "<script", once, System.StringComparison.OrdinalIgnoreCase );
            Assert.DoesNotContain( "javascript", once, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "Changes to Registration", once );
            Assert.Equal( once, twice );
        }

        [Fact]
        public void Fragment_Root_Bare_Td_Survives_With_Structure()
        {
            // Prod FooterContent starts at a bare <td>, not a full document.
            const string input =
                "<td align=\"center\" class=\"esd-block-image\" style=\"font-size: 0\"><img src=\"https://x/y.png\"></td>";
            var result = LinkListHtmlSanitizer.Sanitize( input );

            Assert.Contains( "<td", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "align=\"center\"", result );
            Assert.Contains( "esd-block-image", result );
            Assert.Contains( "https://x/y.png", result );
            // font-size is on the allowlist, so the style survives here.
            Assert.Contains( "font-size: 0", result );
        }

        [Fact]
        public void Html_Comments_Are_Stripped()
        {
            var result = LinkListHtmlSanitizer.Sanitize( "<p>a<!-- secret comment -->b</p>" );
            Assert.DoesNotContain( "<!--", result );
            Assert.DoesNotContain( "secret", result );
            Assert.Contains( "a", result );
            Assert.Contains( "b", result );
        }

        // ----------------------------------------------------------------------
        // ROCK-8880 fix round 2 — mXSS attribute-boundary breakout (BLOCKER 1)
        // ----------------------------------------------------------------------

        // Re-tokenization check: RE-PARSE the sanitized output with
        // HtmlAgilityPack and prove nothing broke out of its attribute. Two
        // invariants:
        //   1. No element carries an event-handler (on*) attribute.
        //   2. No attribute's raw value contains a literal double-quote, which is
        //      the only character that can terminate a double-quoted value and is
        //      exactly what an interior-quote breakout leaves behind.
        // CAVEAT: this re-parses with HtmlAgilityPack - the SAME parser that
        // produced the output - so it CANNOT detect a HAP-vs-browser tokenizer
        // differential; it proves the output is self-consistent under HAP, not
        // that it is browser-equivalent. The blocker-1 closure does not rest on
        // this test: it holds on HTML-spec grounds (a forced double-quote
        // delimiter around a value from which literal " < > have been encoded out
        // cannot be re-tokenized by any conformant parser), which this check
        // corroborates but does not by itself establish. It still catches the
        // whole payload class regardless of substring spelling, unlike a naive
        // DoesNotContain("onerror") check.
        private static void AssertNoAttributeBreakout( string sanitized )
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml( sanitized );

            foreach ( var el in doc.DocumentNode.DescendantsAndSelf().Where( n => n.NodeType == HtmlAgilityPack.HtmlNodeType.Element ) )
            {
                foreach ( var attr in el.Attributes )
                {
                    Assert.False(
                        attr.Name.StartsWith( "on", System.StringComparison.OrdinalIgnoreCase ),
                        $"Re-parse found event handler '{attr.Name}' on <{el.Name}> in: {sanitized}" );

                    Assert.False(
                        attr.Value.Contains( "\"" ),
                        $"Re-parse found a literal quote in {el.Name}.{attr.Name} value '{attr.Value}' in: {sanitized}" );
                }
            }
        }

        // Every confirmed blocker-1 payload: an allowlisted attribute whose
        // UNQUOTED source smuggles an embedded quote + handler. HAP parses the
        // handler INSIDE the allowlisted attribute's value; the fix must ensure
        // the emitted string cannot re-tokenize into a live handler.
        public static IEnumerable<object[]> Blocker1Payloads()
        {
            yield return new object[] { "<img src=x alt=a\"onerror=\"alert(1)>" };                 // img / alt
            yield return new object[] { "<p title=a\"onmouseover=\"alert(1)>x</p>" };              // p / title
            yield return new object[] { "<font color=a\"onmouseover=\"alert(1)>x</font>" };        // font / color
            yield return new object[] { "<table><tr><td align=a\"onmouseover=\"alert(1)>x</td></tr></table>" }; // td / align
            yield return new object[] { "<a href=x\"onmouseover=\"alert(1)>x</a>" };               // a / href
            yield return new object[] { "<a href=/ok title=a\"onmouseover=\"alert(1)>x</a>" };     // a / title alongside href
            yield return new object[] { "<p style=width:1\"onmouseover=\"alert(1)>x</p>" };        // style path
        }

        [Theory]
        [MemberData( nameof( Blocker1Payloads ) )]
        public void Blocker1_Payload_Cannot_Break_Out_On_First_Pass( string payload )
        {
            var once = LinkListHtmlSanitizer.Sanitize( payload );

            // The FIRST pass must already be safe — BuildBag sanitizes exactly
            // once per render, so the first-pass output is what ships.
            AssertNoAttributeBreakout( once );
        }

        [Theory]
        [MemberData( nameof( Blocker1Payloads ) )]
        public void Blocker1_Payload_Is_Idempotent( string payload )
        {
            var once = LinkListHtmlSanitizer.Sanitize( payload );
            var twice = LinkListHtmlSanitizer.Sanitize( once );
            Assert.Equal( once, twice );
        }

        [Fact]
        public void Smuggled_Handler_Does_Not_Produce_A_Live_Onerror_Attribute()
        {
            // Concrete replay of the headline payload. The literal text
            // "onerror" may survive ENCODED inside the alt value (that is inert);
            // what must never happen is a real onerror ATTRIBUTE on the element.
            var once = LinkListHtmlSanitizer.Sanitize( "<img src=x alt=a\"onerror=\"alert(1)>" );

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml( once );
            var img = doc.DocumentNode.Descendants( "img" ).Single();
            Assert.Null( img.Attributes["onerror"] );
        }

        // ----------------------------------------------------------------------
        // ROCK-8880 fix round 2 — entity-aware CSS parsing (BLOCKER 2)
        // ----------------------------------------------------------------------

        [Fact]
        public void Style_Preserves_Entity_Quoted_Font_Family()
        {
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"font-family: Arial, &quot;Helvetica Neue&quot;, sans-serif; color: red\">x</p>" );

            // The whole stack survives, quoted family name intact, color kept.
            Assert.Contains( "font-family: Arial, &quot;Helvetica Neue&quot;, sans-serif", result );
            Assert.Contains( "color: red", result );
            AssertNoAttributeBreakout( result );
        }

        [Fact]
        public void Style_Preserves_Real_Prod_Apple_System_Font_Stack()
        {
            const string stack =
                "font-family: -apple-system, BlinkMacSystemFont, &quot;Segoe UI&quot;, Helvetica, Arial, sans-serif, &quot;Apple Color Emoji&quot;, &quot;Segoe UI Emoji&quot;, &quot;Segoe UI Symbol&quot;";
            var result = LinkListHtmlSanitizer.Sanitize( $"<p style=\"{stack}\">x</p>" );

            Assert.Contains( "-apple-system", result );
            Assert.Contains( "BlinkMacSystemFont", result );
            Assert.Contains( "&quot;Segoe UI&quot;", result );
            Assert.Contains( "&quot;Apple Color Emoji&quot;", result );
            Assert.Contains( "&quot;Segoe UI Emoji&quot;", result );
            Assert.Contains( "&quot;Segoe UI Symbol&quot;", result );
            Assert.Contains( "sans-serif", result );
            AssertNoAttributeBreakout( result );
        }

        [Fact]
        public void Style_Entity_Quoted_Font_Family_Is_Idempotent()
        {
            const string input =
                "<p style=\"font-family: Arial, &quot;Helvetica Neue&quot;, sans-serif; color: red\">x</p>";
            var once = LinkListHtmlSanitizer.Sanitize( input );
            var twice = LinkListHtmlSanitizer.Sanitize( once );
            Assert.Equal( once, twice );
        }

        [Fact]
        public void Style_Entity_Obfuscated_Url_Is_Rejected()
        {
            // &#117;rl( decodes to url( — must be caught now that the CSS value
            // test runs on the DE-ENTITIZED declaration (MINOR fix).
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"background-color: &#117;rl(x)\">y</p>" );
            Assert.DoesNotContain( "url(", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.DoesNotContain( "style", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( ">y</p>", result );
        }

        // ----------------------------------------------------------------------
        // ROCK-8880 fix round 2 — culture-invariant security regexes (MAJOR)
        // ----------------------------------------------------------------------

        [Fact]
        public void Blocks_Uppercase_Javascript_Scheme_Under_Turkish_Culture()
        {
            // Under tr-TR, ASCII 'I' does not case-fold to 'i'. Without
            // RegexOptions.CultureInvariant, JAVASCRIPT: would slip past the
            // scheme filter on a Turkish-culture request thread.
            var original = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo( "tr-TR" );
                var result = LinkListHtmlSanitizer.Sanitize( "<a href=\"JAVASCRIPT:alert(1)\">x</a>" );
                Assert.DoesNotContain( "JAVASCRIPT", result, System.StringComparison.OrdinalIgnoreCase );
                Assert.Contains( ">x</a>", result );
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }

        [Fact]
        public void Blocks_Uppercase_Css_Expression_Under_Turkish_Culture()
        {
            var original = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo( "tr-TR" );
                var result = LinkListHtmlSanitizer.Sanitize(
                    "<p style=\"width: EXPRESSION(alert(1))\">x</p>" );
                Assert.DoesNotContain( "EXPRESSION", result, System.StringComparison.OrdinalIgnoreCase );
                Assert.DoesNotContain( "style", result, System.StringComparison.OrdinalIgnoreCase );
                Assert.Contains( ">x</p>", result );
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }

        // ----------------------------------------------------------------------
        // ROCK-8880 fix round 2 — file: scheme coverage (NIT)
        // ----------------------------------------------------------------------

        [Fact]
        public void Blocks_File_Scheme_On_Href()
        {
            var result = LinkListHtmlSanitizer.Sanitize( "<a href=\"file:///etc/passwd\">x</a>" );
            Assert.DoesNotContain( "file:", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( ">x</a>", result );
        }

        // ----------------------------------------------------------------------
        // ROCK-8880 fix round 2 — re-tokenization safety on the prod mXSS fixture
        // ----------------------------------------------------------------------

        [Fact]
        public void Item_9644_Mxss_Fixture_Cannot_Break_Out_On_First_Pass()
        {
            // Verbatim prod substring reconciled against the DB query (item 9644).
            const string input =
                "<h2 style=\"text-align: left;\" class=\"\"><span style=\"color: inherit; font-family: inherit;&gt;Changes to Registration&lt;/span&gt;&lt;/h2&gt;&lt;p style=\" text-align:=\"\" left;\"=\"\">Changes to Registration</span></h2>";

            var once = LinkListHtmlSanitizer.Sanitize( input );
            AssertNoAttributeBreakout( once );
            Assert.Contains( "Changes to Registration", once );
        }

        // ----------------------------------------------------------------------
        // ROCK-8880 fix round 3 — DeEntitize must never throw (BLOCKER)
        //
        // HAP 1.4.9.5 HtmlEntity.DeEntitize THROWS KeyNotFoundException on any
        // semicolon-terminated named entity absent from its HTML4 table. No call
        // site had a try/catch, so one stored value with e.g. &colon; would
        // permanently 500 the public list viewer. Sanitize must tolerate these in
        // href, src, AND style values.
        // ----------------------------------------------------------------------

        public static IEnumerable<object[]> UndecodableEntityTokens()
        {
            yield return new object[] { "&colon;" };      // HTML5 named, HAP lacks -> throws
            yield return new object[] { "&Tab;" };        // HTML5 named -> throws
            yield return new object[] { "&semi;" };       // HTML5 named -> throws
            yield return new object[] { "&foo;" };        // bogus named -> throws
            yield return new object[] { "&#999999;" };    // numeric, out of BMP
            yield return new object[] { "&" };            // bare ampersand
            yield return new object[] { "x&" };           // ampersand at end of value
        }

        [Theory]
        [MemberData( nameof( UndecodableEntityTokens ) )]
        public void Sanitize_Does_Not_Throw_For_Undecodable_Entity_In_Href( string token )
        {
            // Must not throw (the whole point). Return value is not asserted here.
            var ex = Record.Exception(
                () => LinkListHtmlSanitizer.Sanitize( $"<a href=\"{token}\">x</a>" ) );
            Assert.Null( ex );
        }

        [Theory]
        [MemberData( nameof( UndecodableEntityTokens ) )]
        public void Sanitize_Does_Not_Throw_For_Undecodable_Entity_In_Src( string token )
        {
            var ex = Record.Exception(
                () => LinkListHtmlSanitizer.Sanitize( $"<img src=\"{token}\">" ) );
            Assert.Null( ex );
        }

        [Theory]
        [MemberData( nameof( UndecodableEntityTokens ) )]
        public void Sanitize_Does_Not_Throw_For_Undecodable_Entity_In_Style( string token )
        {
            // Put the token inside an allowlisted property value so it reaches the
            // FilterStyle DeEntitize path.
            var ex = Record.Exception(
                () => LinkListHtmlSanitizer.Sanitize( $"<p style=\"color: {token}\">x</p>" ) );
            Assert.Null( ex );
        }

        [Fact]
        public void Entity_Encoded_Colon_Does_Not_Enable_Scheme_Bypass()
        {
            // &colon; decodes to ':' in a browser, completing "javascript:". The
            // safe decoder must decode it so the scheme check still neutralizes
            // the href. (A best-effort decoder that left it literal would ship
            // this XSS.)
            var result = LinkListHtmlSanitizer.Sanitize(
                "<a href=\"javascript&colon;alert(1)\">x</a>" );

            Assert.Contains( ">x</a>", result );
            // No live javascript: URL survived on the anchor.
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml( result );
            var a = doc.DocumentNode.Descendants( "a" ).Single();
            Assert.Null( a.Attributes["href"] );
        }

        [Fact]
        public void Entity_Encoded_Lpar_Does_Not_Enable_Css_Url_Bypass()
        {
            // url&lpar; decodes to "url(" — must be caught on the allowed property.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"background-color: url&lpar;x&rpar;\">y</p>" );
            Assert.DoesNotContain( "style", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( ">y</p>", result );
        }

        // ----------------------------------------------------------------------
        // ROCK-8880 fix round 3 — double-encoded style truncation (MAJOR)
        //
        // DeEntitize decodes one level, leaving a literal ';' that Split(';') then
        // treats as a declaration separator, discarding the remainder. The raw-
        // split-with-reattachment fix must preserve the FULL value byte-faithfully.
        // ----------------------------------------------------------------------

        [Fact]
        public void Style_Double_Encoded_Quot_Font_Stack_Preserved_Whole()
        {
            const string stack =
                "font-family: Arial, &amp;quot;Segoe UI&amp;quot;, sans-serif";
            var result = LinkListHtmlSanitizer.Sanitize( $"<p style=\"{stack}\">x</p>" );
            Assert.Contains( stack, result );
        }

        [Fact]
        public void Style_Double_Encoded_Amp_In_Font_Family_Preserved_Whole()
        {
            const string style = "font-family: A &amp;amp; B; color: red";
            var result = LinkListHtmlSanitizer.Sanitize( $"<p style=\"{style}\">x</p>" );
            Assert.Contains( "font-family: A &amp;amp; B", result );
            Assert.Contains( "color: red", result );
        }

        [Fact]
        public void Style_Double_Encoded_Numeric_Semicolon_Preserved_Whole()
        {
            const string style = "color: red &amp;#59; more";
            var result = LinkListHtmlSanitizer.Sanitize( $"<p style=\"{style}\">x</p>" );
            Assert.Contains( "color: red &amp;#59; more", result );
        }

        [Fact]
        public void Style_Double_Encoded_Cases_Are_Idempotent()
        {
            foreach ( var style in new[]
            {
                "font-family: Arial, &amp;quot;Segoe UI&amp;quot;, sans-serif",
                "font-family: A &amp;amp; B; color: red",
                "color: red &amp;#59; more"
            } )
            {
                var once = LinkListHtmlSanitizer.Sanitize( $"<p style=\"{style}\">x</p>" );
                var twice = LinkListHtmlSanitizer.Sanitize( once );
                Assert.Equal( once, twice );
            }
        }

        // ----------------------------------------------------------------------
        // ROCK-8880 fix round 3 — style bytes are byte-faithful (dissolved MINOR)
        //
        // The old style path mutated &nbsp; -> U+00A0 and &#39; -> ' in stored
        // values. Writing back the RAW declaration keeps them verbatim.
        // ----------------------------------------------------------------------

        [Fact]
        public void Style_Preserves_Nbsp_Entity_As_Written()
        {
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"font-family: A&nbsp;B\">x</p>" );
            Assert.Contains( "font-family: A&nbsp;B", result );
            // Must remain the &nbsp; entity, NOT be mutated to a literal U+00A0.
            Assert.DoesNotContain( "\u00A0", result );
        }

        [Fact]
        public void Style_Preserves_Numeric_Apostrophe_Entity_As_Written()
        {
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"font-family: A&#39;B\">x</p>" );
            Assert.Contains( "font-family: A&#39;B", result );
        }

        // ----------------------------------------------------------------------
        // ROCK-8880 fix round 4 — BLOCKER A: SafeDeEntitize must decode at least
        // as aggressively as a browser.
        //
        // Browsers decode a numeric character reference in an attribute value even
        // WITHOUT the terminating ';' (an HTML5 parse error, but the reference is
        // still consumed and decoded). The round-3 decoder required the ';', so a
        // scheme colon written as `&#58` (no ';'), `&#x3a` (no ';'), or a tab as
        // `&#9` (no ';') slipped past the scheme check and executed on click.
        // These assert the SECURITY OUTCOME (the url-bearing attribute is stripped),
        // not the decoder internals.
        // ----------------------------------------------------------------------

        public static IEnumerable<object[]> BlockerA_SemicolonlessSchemePayloads()
        {
            // scheme colon as a decimal ref with NO ';' (the digit run stops at
            // the non-digit 'a', so the colon materializes before "alert")
            yield return new object[] { "javascript&#58alert(1)" };
            // scheme colon as a hex ref with NO ';'. The hex-digit run must be
            // terminated by a NON-hex char for the colon to materialize — here the
            // 'w' of "window" (a real attack payload `javascript:window...`). NB:
            // `&#x3aalert` is NOT used: 'a' is a hex digit, so a browser AND this
            // decoder both fold it into `&#x3aa` (U+03AA) and no colon appears —
            // that spelling is a non-exploit at parity, not a bypass.
            yield return new object[] { "javascript&#x3awindow.alert(1)" };
            // tab as a decimal ref with NO ';' — URL parser strips it, leaving
            // "javascript:"
            yield return new object[] { "java&#9script:alert(1)" };
        }

        [Theory]
        [MemberData( nameof( BlockerA_SemicolonlessSchemePayloads ) )]
        public void Semicolonless_Scheme_Entity_Is_Stripped_On_Href( string payload )
        {
            var result = LinkListHtmlSanitizer.Sanitize( $"<a href=\"{payload}\">x</a>" );

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml( result );
            var a = doc.DocumentNode.Descendants( "a" ).Single();
            Assert.Null( a.Attributes["href"] );   // scheme detected -> href removed
            Assert.Contains( ">x</a>", result );
        }

        [Theory]
        [MemberData( nameof( BlockerA_SemicolonlessSchemePayloads ) )]
        public void Semicolonless_Scheme_Entity_Is_Stripped_On_Src( string payload )
        {
            var result = LinkListHtmlSanitizer.Sanitize( $"<img src=\"{payload}\">" );

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml( result );
            var img = doc.DocumentNode.Descendants( "img" ).Single();
            Assert.Null( img.Attributes["src"] );   // scheme detected -> src removed
        }

        [Theory]
        [MemberData( nameof( BlockerA_SemicolonlessSchemePayloads ) )]
        public void Semicolonless_Scheme_Payload_Is_Idempotent( string payload )
        {
            var once = LinkListHtmlSanitizer.Sanitize( $"<a href=\"{payload}\">x</a>" );
            var twice = LinkListHtmlSanitizer.Sanitize( once );
            Assert.Equal( once, twice );
        }

        // The regression risk of the more-aggressive decoder: it must NOT reject a
        // legitimate https URL whose query string contains '&', '&amp;', and param
        // names that look like entity prefixes (&centerId=, &copyright=). The
        // scheme regex is anchored at '^', so query-string decoding can never turn
        // an https URL into a dangerous scheme — the href must survive intact and
        // byte-faithful.
        [Fact]
        public void Aggressive_Decoder_Keeps_Legit_Url_With_Entity_Like_Query_Params()
        {
            const string url =
                "https://se.church/give?centerId=2&amp;ref=1&copyright=se&sol=x";
            var result = LinkListHtmlSanitizer.Sanitize( $"<a href=\"{url}\">give</a>" );

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml( result );
            var a = doc.DocumentNode.Descendants( "a" ).Single();
            Assert.NotNull( a.Attributes["href"] );                       // NOT rejected
            Assert.Contains( "se.church/give", a.Attributes["href"].Value );
            Assert.Contains( "&amp;ref=1", result );                      // entity kept verbatim
            Assert.Contains( "&copyright=se", result );                   // no mangling
            Assert.Contains( ">give</a>", result );
        }

        [Fact]
        public void Aggressive_Decoder_Legit_Url_Is_Idempotent()
        {
            const string input =
                "<a href=\"https://se.church/give?centerId=2&amp;ref=1&copyright=se\">give</a>";
            var once = LinkListHtmlSanitizer.Sanitize( input );
            var twice = LinkListHtmlSanitizer.Sanitize( once );
            Assert.Equal( once, twice );
        }

        // ----------------------------------------------------------------------
        // ROCK-8880 fix round 4 — BLOCKER B: a CSS declaration written back RAW
        // must be incapable of DECODING, in the browser, into more declarations
        // than were validated.
        //
        // Each payload is an ALLOWED property (`color` / `width`) whose value, in
        // raw form, clears the property allowlist, yet decodes in the browser into
        // a declaration separator + a second live declaration (external url()
        // fetch, position:fixed clickjacking overlay, expression()). The fix must
        // drop the whole declaration. Assertions run on the browser-DECODED form of
        // the emitted output, not a raw substring — the round-3 mistake was a raw
        // substring check that missed the entity-encoded form.
        // ----------------------------------------------------------------------

        // Browser-equivalent HTML decode of the emitted markup, so the assertion
        // sees what a browser's CSS parser would after HTML-decoding the attribute.
        private static string HtmlDecodeLikeBrowser( string s )
        {
            return System.Net.WebUtility.HtmlDecode( s );
        }

        public static IEnumerable<object[]> BlockerB_SmuggledDeclarationPayloads()
        {
            // allowed color; then ';' (&#59) then background:url(...) with entity
            // parens -> live external fetch
            yield return new object[]
            {
                "<div style=\"color: red&#59background:url&#40//evil&#41\">x</div>",
                "background:"
            };
            // allowed color; then ';' (&#59) then position:fixed ('o' as &#111) ->
            // full-viewport clickjacking overlay
            yield return new object[]
            {
                "<div style=\"color: red&#59p&#111sition:fixed\">x</div>",
                "position:fixed"
            };
            // allowed width; expression(...) with entity parens -> defeats a raw
            // DangerousCssValue check
            yield return new object[]
            {
                "<div style=\"width: expression&#40alert(1)&#41\">x</div>",
                "expression("
            };
        }

        [Theory]
        [MemberData( nameof( BlockerB_SmuggledDeclarationPayloads ) )]
        public void Smuggled_Css_Declaration_Does_Not_Survive_In_Any_Form( string payload, string forbiddenDecoded )
        {
            var once = LinkListHtmlSanitizer.Sanitize( payload );

            // No style attribute should survive: the sole declaration is a smuggle,
            // so the whole attribute is removed.
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml( once );
            var div = doc.DocumentNode.Descendants( "div" ).Single();
            Assert.Null( div.Attributes["style"] );

            // And on the browser-DECODED form, none of the smuggled declaration
            // survives (this is the assertion a raw substring check would miss).
            var decoded = HtmlDecodeLikeBrowser( once );
            Assert.DoesNotContain( forbiddenDecoded, decoded, System.StringComparison.OrdinalIgnoreCase );
            Assert.DoesNotContain( "url(", decoded, System.StringComparison.OrdinalIgnoreCase );
            Assert.DoesNotContain( "expression(", decoded, System.StringComparison.OrdinalIgnoreCase );

            Assert.Contains( ">x</div>", once );
        }

        [Theory]
        [MemberData( nameof( BlockerB_SmuggledDeclarationPayloads ) )]
        public void Smuggled_Css_Declaration_Payload_Is_Idempotent( string payload, string forbiddenDecoded )
        {
            _ = forbiddenDecoded;
            var once = LinkListHtmlSanitizer.Sanitize( payload );
            var twice = LinkListHtmlSanitizer.Sanitize( once );
            Assert.Equal( once, twice );
        }

        // ----------------------------------------------------------------------
        // ROCK-8880 fix round 4 — re-confirm prod-content fixtures still round-trip
        // under the round-4 gate (the double-encoded font stacks carry an inert ';'
        // in their single-level decode; the gate keys on ':' / '{' / '}', so they
        // must survive).
        // ----------------------------------------------------------------------

        [Fact]
        public void Round4_Gate_Keeps_Real_Prod_Margin_Left()
        {
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"margin-left: 25px\">x</p>" );
            Assert.Contains( "margin-left: 25px", result );
        }

        [Fact]
        public void Round4_Gate_Keeps_Double_Encoded_Quot_Font_Stack()
        {
            // Single-level decode yields the literal `&quot;` (which contains ';'),
            // but NO ':' — so the gate must keep it byte-faithfully.
            const string stack =
                "font-family: Arial, &amp;quot;Segoe UI&amp;quot;, sans-serif";
            var result = LinkListHtmlSanitizer.Sanitize( $"<p style=\"{stack}\">x</p>" );
            Assert.Contains( stack, result );
        }

        [Fact]
        public void Round4_Gate_Keeps_Apple_System_Font_Stack_And_Line_Height()
        {
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"font-family: -apple-system, &quot;Segoe UI&quot;, sans-serif; line-height: 150% !important\">x</p>" );
            Assert.Contains( "&quot;Segoe UI&quot;", result );
            Assert.Contains( "line-height: 150% !important", result );
        }

        // ----------------------------------------------------------------------
        // ROCK-8880 final polish — CSS-escape attack class (documentation/coverage).
        //
        // WHY these tests exist and WHY the expected results are correct — do NOT
        // "fix" a passing test here without re-reading this. The security-review
        // established the ACTUAL mechanism by which the CSS declaration gate holds:
        //
        //   A new CSS declaration requires a bare <colon-token> emitted by the CSS
        //   tokenizer. A CSS backslash escape (`\3a`) produces a colon CODE POINT
        //   appended to an identifier — ident content, NEVER a separator token.
        //   (Same reason `.foo\:bar` selects a class literally named `foo:bar`.)
        //   So an escaped colon can NEVER separate a property from a value and can
        //   NEVER form a smuggled declaration. The browser folds `position\3a fixed`
        //   into a single invalid identifier and DROPS that declaration — only the
        //   preceding valid declaration applies.
        //
        //   The gate's real job is therefore narrower than "catch any colon": it
        //   catches LITERAL colons and ENTITY-ENCODED colons (`&#58;` `&#x3a;`
        //   `&colon;`) — the forms a browser genuinely treats as separators —
        //   because SafeDeEntitize decodes every colon form a browser would. It does
        //   NOT (and need not) catch CSS escapes; those are inert.
        //
        // The two groups below pin BOTH halves so a future edit that changed either
        // (started mangling inert escapes, or stopped catching real separators)
        // fails loudly. NOTE: C# requires `\\` in source to emit a single literal
        // backslash into the CSS text.
        // ----------------------------------------------------------------------

        // GROUP 1 — survive-but-INERT, asserted byte-faithful. These carry an
        // ESCAPED colon (or escaped parens), which is ident content, not a
        // separator; the sanitizer must pass the bytes through UNCHANGED (mangling
        // them would corrupt legitimate content), and the browser then folds the
        // escape into one invalid identifier and drops the declaration.

        [Fact]
        public void Style_CssEscaped_Colon_Survives_Byte_Faithful_But_Is_Inert()
        {
            // `position\3a fixed`: `\3a` is a CSS escape -> colon code point folded
            // INTO the identifier, never a <colon-token>, so it cannot begin a new
            // declaration. Value carries no LITERAL ':' and its SafeDeEntitize'd
            // form (no entities present) carries none either, so the gate keeps it.
            // Browser folds `position\3a fixed` into one invalid ident -> dropped;
            // only `color: red` applies. Bytes must pass through verbatim.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"color: red; position\\3a fixed\">x</p>" );
            Assert.Contains( "color: red; position\\3a fixed", result );
        }

        [Fact]
        public void Style_CssEscaped_Colon_And_Parens_Survive_Byte_Faithful_But_Inert()
        {
            // `background\3aurl\28//evil\29`: escaped colon AND escaped parens all
            // become ident chars -> no <colon-token>, no `url(` function token in
            // the raw bytes, so DangerousCssValue's `url\(` never matches and the
            // colon gate sees no literal ':'. Browser folds the whole run into one
            // invalid ident -> dropped, NO fetch. Bytes must survive verbatim.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"width: 1; background\\3aurl\\28//evil\\29\">x</p>" );
            Assert.Contains( "width: 1; background\\3aurl\\28//evil\\29", result );
        }

        [Fact]
        public void Style_CssEscaped_Semicolon_And_Colon_Survive_Byte_Faithful_But_Inert()
        {
            // `red\3bposition\3afixed`: escaped `;` and escaped `:` are both ident
            // content -> no declaration separator, no property/value separator. The
            // raw value contains no literal ';' (so no split) and no literal ':'
            // beyond the property's own, so it is kept whole. Browser folds it into
            // one invalid `background-color` value -> declaration dropped.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"background-color: red\\3bposition\\3afixed\">x</p>" );
            Assert.Contains( "background-color: red\\3bposition\\3afixed", result );
        }

        [Fact]
        public void Style_EntityWritten_Backslash_Numeric_Colon_Escape_Survives_Byte_Faithful_But_Inert()
        {
            // `position&#92;3a fixed`: `&#92;` is an HTML entity the browser decodes
            // to a backslash FIRST, yielding the CSS escape `\3a` — ident content,
            // not a separator. SafeDeEntitize likewise decodes `&#92;` to `\`, so
            // the gate sees `position\3a fixed` (a real backslash, NO literal ':')
            // and keeps it. Stored bytes stay as the `&#92;` entity, verbatim.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"color: red; position&#92;3a fixed\">x</p>" );
            Assert.Contains( "color: red; position&#92;3a fixed", result );
        }

        [Fact]
        public void Style_EntityWritten_Backslash_Named_Colon_Escape_Survives_Byte_Faithful_But_Inert()
        {
            // `position&bsol;3a fixed`: `&bsol;` decodes to a backslash (same as
            // above), producing the inert CSS escape `\3a`. SafeDeEntitize decodes
            // `&bsol;` -> `\`, so the gate sees no literal ':' and keeps the bytes.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"color: red; position&bsol;3a fixed\">x</p>" );
            Assert.Contains( "color: red; position&bsol;3a fixed", result );
        }

        // GROUP 2 — REJECTED, asserted stripped. These carry a REAL separator (a
        // literal ':' or an entity-encoded colon the browser decodes to ':'), which
        // WOULD open a smuggled declaration, so the gate must drop it.

        [Fact]
        public void Style_Literal_Colon_Smuggle_Drops_Declaration_Keeps_Color()
        {
            // `position:fixed` after `color: red` is a genuine second declaration
            // with a LITERAL <colon-token>. `position` is not on the property
            // allowlist, so that declaration is dropped and `color` is kept.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"color: red; position:fixed\">x</p>" );
            Assert.Contains( "color: red", result );
            Assert.DoesNotContain( "position", result, System.StringComparison.OrdinalIgnoreCase );
        }

        [Fact]
        public void Style_EntityEncoded_Numeric_Colon_Smuggle_Strips_Style()
        {
            // `position&#58;fixed`: `&#58;` decodes to a real ':' in the browser AND
            // in SafeDeEntitize. Because the raw split keeps `color: red;
            // position&#58;fixed` as ONE declaration (the entity-encoded ';'-less
            // colon is not a raw separator), its decoded value carries a ':' -> the
            // smuggle gate rejects the whole `color` declaration -> style removed.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"color: red; position&#58;fixed\">x</p>" );
            Assert.DoesNotContain( "style", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( ">x</p>", result );
        }

        [Fact]
        public void Style_EntityEncoded_Named_Colon_Smuggle_Strips_Style()
        {
            // `position&colon;fixed`: `&colon;` decodes to ':' — same mechanism as
            // the numeric form; the decoded value carries a real separator, so the
            // smuggle gate strips the style.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"color: red; position&colon;fixed\">x</p>" );
            Assert.DoesNotContain( "style", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( ">x</p>", result );
        }

        // GROUP 3 — per-declaration gating with NO truncation. A middle declaration
        // that decodes to an extra separator is dropped in isolation; the valid
        // declarations on either side must both survive (proving the drop did not
        // truncate the rest of the value).

        [Fact]
        public void Style_EntityColon_Middle_Declaration_Dropped_Without_Truncation_Numeric()
        {
            // `width: 1&#58;&#58;2` decodes to `1::2` (two real colons) -> smuggle,
            // middle declaration dropped. `color` (before) and `font-size` (after)
            // must BOTH survive — the drop is per-declaration, not a truncation.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"color: red; width: 1&#58;&#58;2; font-size: 10px\">x</p>" );
            Assert.Contains( "color: red", result );
            Assert.Contains( "font-size: 10px", result );
            Assert.DoesNotContain( "width", result, System.StringComparison.OrdinalIgnoreCase );
        }

        [Fact]
        public void Style_EntityColon_Middle_Declaration_Dropped_Without_Truncation_Alpha()
        {
            // `width: a&#58;b` decodes to `a:b` (a real colon) -> smuggle, middle
            // declaration dropped; `color` and `font-size` both survive.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"color: red; width: a&#58;b; font-size: 10px\">x</p>" );
            Assert.Contains( "color: red", result );
            Assert.Contains( "font-size: 10px", result );
            Assert.DoesNotContain( "width", result, System.StringComparison.OrdinalIgnoreCase );
        }

        // GROUP 4 — the LATENT DangerousCssValue escape gap, pinned as a test so the
        // behavior is visible and documented rather than surprising. This value is
        // ACCEPTED-BECAUSE-INERT, not accepted-because-safe: `\75 rl(` is a CSS
        // escape of the `u` in `url`, so DangerousCssValue's `url\(` regex never
        // matches and the value survives sanitization unchanged. It is harmless
        // ONLY because `background-color` (and every other allowlisted property)
        // does not accept a url()/image value, so the browser — which DOES fold
        // `\75 rl(` back into a real `url(` function token — drops the declaration.
        // The PROPERTY ALLOWLIST is the guarantee here, not DangerousCssValue. If a
        // URL-/image-bearing property is ever allowlisted, this test's payload
        // becomes a live external fetch and DangerousCssValue must be made
        // escape-aware first (see the LIMITATION note on the field).

        [Fact]
        public void Style_CssEscaped_Url_Survives_Filter_Accepted_Because_Inert_Not_Safe()
        {
            // Survives sanitization byte-faithfully because `\75 rl(` is not a
            // literal `url(`; inert only because no allowlisted property accepts a
            // url() value. NOT a guarantee provided by DangerousCssValue.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p style=\"background-color: \\75 rl(//evil/x)\">y</p>" );
            Assert.Contains( "background-color: \\75 rl(//evil/x)", result );
        }

        // ----------------------------------------------------------------------
        // ROCK-8880 fix round 5 — reverse tabnabbing (`target` kept, `rel` stripped).
        //
        // `target` is allowlisted on <a> but `rel` is on NO allowlist, so before this
        // fix the sanitizer kept `target="_blank"` while actively STRIPPING an
        // author's `rel="noopener noreferrer"` — removing the mitigation and leaving
        // no way for an editor to add it back, since this sanitizer is the single
        // chokepoint. `rel` is now force-injected whenever `target` survives.
        // ----------------------------------------------------------------------

        [Fact]
        public void Target_Blank_Link_Gets_Noopener_Noreferrer()
        {
            var result = LinkListHtmlSanitizer.Sanitize(
                "<a href=\"https://example.org\" target=\"_blank\">go</a>" );

            Assert.Contains( "rel=\"noopener noreferrer\"", result );
            Assert.Contains( "target=\"_blank\"", result );
            Assert.Contains( "https://example.org", result );
        }

        [Theory]
        [InlineData( "_blank" )]
        [InlineData( "_new" )]
        [InlineData( "someWindowName" )]
        public void Any_Surviving_Target_Value_Gets_The_Guard( string target )
        {
            var result = LinkListHtmlSanitizer.Sanitize(
                "<a href=\"https://example.org\" target=\"" + target + "\">go</a>" );

            Assert.Contains( "rel=\"noopener noreferrer\"", result );
        }

        [Fact]
        public void Author_Rel_Is_Replaced_Not_Appended()
        {
            // The author value is stripped (rel is on no allowlist) and ours is
            // injected — there must be exactly one rel, carrying our value.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<a href=\"https://example.org\" target=\"_blank\" rel=\"nofollow\">go</a>" );

            Assert.DoesNotContain( "nofollow", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "rel=\"noopener noreferrer\"", result );
            Assert.Equal( 1, CountOccurrences( result, "rel=" ) );
        }

        [Fact]
        public void Author_Rel_Is_Stripped_When_There_Is_No_Target()
        {
            // No target means no tabnabbing exposure, so no guard is injected — and
            // the author's rel is still stripped, as a non-allowlisted attribute.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<a href=\"https://example.org\" rel=\"nofollow\">go</a>" );

            Assert.DoesNotContain( "rel=", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "https://example.org", result );
        }

        [Fact]
        public void Link_Without_Target_Gets_No_Rel()
        {
            var result = LinkListHtmlSanitizer.Sanitize( "<a href=\"https://example.org\">go</a>" );

            Assert.DoesNotContain( "rel=", result, System.StringComparison.OrdinalIgnoreCase );
        }

        [Fact]
        public void Target_On_A_Dropped_Href_Still_Gets_The_Guard()
        {
            // The javascript: href is removed but the element (and its target)
            // survive, so the guard must still be applied.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<a href=\"javascript:alert(1)\" target=\"_blank\">go</a>" );

            Assert.DoesNotContain( "javascript:", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "rel=\"noopener noreferrer\"", result );
        }

        [Fact]
        public void Target_Attribute_On_Non_Anchor_Does_Not_Inject_Rel()
        {
            // target is not allowlisted on <p>, so it is stripped and nothing is
            // injected — the guard must key off a target that actually SURVIVED.
            var result = LinkListHtmlSanitizer.Sanitize( "<p target=\"_blank\">x</p>" );

            Assert.DoesNotContain( "target=", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.DoesNotContain( "rel=", result, System.StringComparison.OrdinalIgnoreCase );
        }

        [Fact]
        public void Injected_Rel_Is_Idempotent()
        {
            // Second pass strips the rel we injected (non-allowlisted) and re-injects
            // it in the same trailing position — the bytes must not drift.
            const string input = "<a href=\"https://example.org\" target=\"_blank\">go</a>";
            var once = LinkListHtmlSanitizer.Sanitize( input );
            var twice = LinkListHtmlSanitizer.Sanitize( once );

            Assert.Equal( once, twice );
        }

        // ----------------------------------------------------------------------
        // ROCK-8880 fix round 5 — bare '<' in text destroyed the rest of the block.
        //
        // HAP parses `<p>Ages < 12 welcome</p>` as <p> + text "Ages " + an element
        // with an EMPTY NAME whose swallowed words become ATTRIBUTES (`12=""`,
        // `welcome=""`). The empty name is on neither allowlist, so Clean() unwraps
        // it; Unwrap moves only ChildNodes, of which it has none, so the node and the
        // words held in its attributes were DISCARDED. Bare '<' is now escaped before
        // parsing, matching the HTML5 tokenizer's tag-open rule.
        // ----------------------------------------------------------------------

        [Fact]
        public void Bare_LessThan_Does_Not_Eat_The_Rest_Of_The_Text()
        {
            var result = LinkListHtmlSanitizer.Sanitize( "<p>Ages < 12 welcome</p>" );

            Assert.Contains( "12", result );
            Assert.Contains( "welcome", result );
            Assert.Contains( "Ages", result );
            // The words must be TEXT, not an attribute soup like `12=""`.
            Assert.DoesNotContain( "12=", result );
            Assert.DoesNotContain( "welcome=", result );
        }

        [Fact]
        public void Bare_LessThan_And_GreaterThan_Both_Survive_As_Text()
        {
            var result = LinkListHtmlSanitizer.Sanitize( "<p>3 < 4 and 5 > 2</p>" );

            Assert.Contains( "4", result );
            Assert.Contains( "2", result );
            Assert.Contains( "and", result );
        }

        [Theory]
        [InlineData( "<p>Cost < $5 per person</p>", "$5 per person" )]
        [InlineData( "<p>a<3 forever</p>", "3 forever" )]
        [InlineData( "<p>x < y</p>", "y" )]
        public void Bare_LessThan_Real_Copy_Keeps_Its_Remainder( string input, string mustSurvive )
        {
            var result = LinkListHtmlSanitizer.Sanitize( input );

            Assert.Contains( mustSurvive, result );
        }

        [Fact]
        public void Trailing_LessThan_Does_Not_Throw_Or_Truncate()
        {
            var result = LinkListHtmlSanitizer.Sanitize( "<p>ends with <" );

            Assert.Contains( "ends with", result );
        }

        [Fact]
        public void Bare_LessThan_Is_Idempotent()
        {
            const string input = "<p>Ages < 12 welcome</p>";
            var once = LinkListHtmlSanitizer.Sanitize( input );
            var twice = LinkListHtmlSanitizer.Sanitize( once );

            Assert.Equal( once, twice );
        }

        // The pre-parse escape must not defang tag detection: '<' followed by a
        // letter, '/', '!' or '?' still opens markup, so comments, end tags and
        // dangerous elements are all handled exactly as before.

        [Fact]
        public void LessThan_Escape_Does_Not_Break_Comment_Stripping()
        {
            var result = LinkListHtmlSanitizer.Sanitize( "<p>a<!-- secret comment -->b</p>" );

            Assert.DoesNotContain( "secret", result );
            Assert.DoesNotContain( "<!--", result );
        }

        [Fact]
        public void LessThan_Escape_Does_Not_Break_End_Tag_Parsing()
        {
            var result = LinkListHtmlSanitizer.Sanitize( "<p>one</p><p>two</p>" );

            Assert.Contains( "one", result );
            Assert.Contains( "two", result );
            Assert.Equal( 2, CountOccurrences( result.ToLowerInvariant(), "<p" ) );
        }

        [Fact]
        public void LessThan_Escape_Does_Not_Break_Dangerous_Tag_Removal()
        {
            var result = LinkListHtmlSanitizer.Sanitize( "<p>a</p><script>alert(1)</script>" );

            Assert.DoesNotContain( "<script", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.DoesNotContain( "alert(1)", result );
        }

        [Fact]
        public void LessThan_Escape_Does_Not_Reopen_The_Mxss_Breakout()
        {
            // A bare '<' alongside the smuggled-quote payload must not produce a live
            // handler; the escape runs before parsing, so the mXSS gate still applies.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<p>3 < 4</p><img src=x alt=a\"onerror=\"alert(1)>" );

            Assert.DoesNotContain( "onerror=\"alert(1)\"", result );
            Assert.Contains( "4", result );
        }

        // ----------------------------------------------------------------------
        // ROCK-8880 fix round 5 — allowlist gaps that silently dropped prod markup.
        //
        // Under an allowlist, anything omitted disappears: a missing ATTRIBUTE is
        // stripped, and a missing TAG is unwrapped, which takes the `class` that
        // scoped its CSS with it.
        // ----------------------------------------------------------------------

        [Fact]
        public void Legacy_Bgcolor_Survives_On_Table_And_Cell()
        {
            // The email builder paints backgrounds with the bgcolor ATTRIBUTE
            // (email clients strip <style>), so dropping it renders blocks white.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<table bgcolor=\"#ffffff\"><tr><td bgcolor=\"#eeeeee\">x</td></tr></table>" );

            Assert.Contains( "bgcolor=\"#ffffff\"", result );
            Assert.Contains( "bgcolor=\"#eeeeee\"", result );
        }

        [Fact]
        public void Legacy_Nowrap_Survives_On_Cell()
        {
            var result = LinkListHtmlSanitizer.Sanitize( "<table><tr><td nowrap>x</td></tr></table>" );

            Assert.Contains( "nowrap", result, System.StringComparison.OrdinalIgnoreCase );
        }

        [Theory]
        [InlineData( "section" )]
        [InlineData( "article" )]
        [InlineData( "header" )]
        [InlineData( "footer" )]
        [InlineData( "nav" )]
        [InlineData( "figure" )]
        [InlineData( "pre" )]
        [InlineData( "code" )]
        [InlineData( "small" )]
        [InlineData( "label" )]
        [InlineData( "dl" )]
        public void Class_Bearing_Wrapper_Survives_With_Its_Class( string tag )
        {
            // Unwrapping the wrapper drops the class that scopes its CSS — which
            // would defeat the whole reason `class` is allowlisted.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<" + tag + " class=\"esd-block\"><p>copy</p></" + tag + ">" );

            Assert.Contains( "<" + tag, result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "class=\"esd-block\"", result );
            Assert.Contains( "copy", result );
        }

        [Fact]
        public void Definition_List_Structure_Survives()
        {
            var result = LinkListHtmlSanitizer.Sanitize( "<dl><dt>Term</dt><dd>Def</dd></dl>" );

            Assert.Contains( "<dt", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "<dd", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "Term", result );
            Assert.Contains( "Def", result );
        }

        [Fact]
        public void Table_Caption_Stays_Inside_The_Table()
        {
            // Unwrapping <caption> leaves a bare text node in <table>, which a
            // browser's "in table" insertion mode foster-parents OUT of the table —
            // the caption would render above it as loose body text.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<table><caption>Cap</caption><tr><td>B</td></tr></table>" );

            Assert.Contains( "<caption", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "Cap", result );
        }

        [Fact]
        public void Table_Head_And_Foot_Groupings_Survive()
        {
            var result = LinkListHtmlSanitizer.Sanitize(
                "<table><thead><tr><th>H</th></tr></thead><tbody><tr><td>B</td></tr></tbody>"
                + "<tfoot><tr><td>F</td></tr></tfoot></table>" );

            Assert.Contains( "<thead", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "<tfoot", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "<tbody", result, System.StringComparison.OrdinalIgnoreCase );
        }

        [Fact]
        public void Colgroup_Survives_With_Its_Span_Attribute()
        {
            var result = LinkListHtmlSanitizer.Sanitize(
                "<table><colgroup span=\"2\"><col span=\"1\"></colgroup><tr><td>x</td></tr></table>" );

            Assert.Contains( "<colgroup", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "span=\"2\"", result );
        }

        [Fact]
        public void Span_Attribute_Does_Not_Leak_Onto_Other_Tags()
        {
            // `span` is per-tag (col/colgroup), not global.
            var result = LinkListHtmlSanitizer.Sanitize( "<p span=\"2\">x</p>" );

            Assert.DoesNotContain( "span=", result, System.StringComparison.OrdinalIgnoreCase );
        }

        [Fact]
        public void Newly_Allowed_Tags_Still_Have_Attributes_Filtered()
        {
            // Allowlisting a TAG must not allowlist anything on it.
            var result = LinkListHtmlSanitizer.Sanitize(
                "<section onclick=\"alert(1)\" data-x=\"1\" class=\"keep\">y</section>" );

            Assert.DoesNotContain( "onclick", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.DoesNotContain( "data-x", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "class=\"keep\"", result );
        }

        [Fact]
        public void Still_Unlisted_Benign_Tag_Is_Still_Unwrapped()
        {
            var result = LinkListHtmlSanitizer.Sanitize( "<p>a <marquee>scroll</marquee> b</p>" );

            Assert.DoesNotContain( "<marquee", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Contains( "scroll", result );
        }

        [Fact]
        public void Round5_Additions_Are_Idempotent()
        {
            const string input =
                "<section class=\"esd-block\"><table bgcolor=\"#ffffff\"><caption>Cap</caption>"
                + "<thead><tr><th nowrap>H</th></tr></thead><tbody><tr><td bgcolor=\"#eee\">"
                + "<a href=\"https://example.org\" target=\"_blank\">go</a></td></tr></tbody>"
                + "</table><p>Ages < 12 welcome</p></section>";
            var once = LinkListHtmlSanitizer.Sanitize( input );
            var twice = LinkListHtmlSanitizer.Sanitize( once );

            Assert.Equal( once, twice );
        }

        // ----------------------------------------------------------------------
        // ROCK-8880 fix round 5 — pins the "non-element, non-text nodes never reach
        // the output" claim in Clean().
        //
        // HtmlNodeType in HAP 1.4.9.5 has exactly four members (Document, Element,
        // Comment, Text), so there is no declaration or processing-instruction node
        // type for Clean() to drop explicitly. Doctypes and `<! ... >` parse as
        // COMMENT nodes; `<? ... ?>` parses as an ELEMENT named `?` that falls to the
        // benign-unknown unwrap path. These tests assert none of them are emitted, so
        // the comment stays honest if a future HAP upgrade changes the parse.
        // ----------------------------------------------------------------------

        [Theory]
        [InlineData( "<!DOCTYPE html><p>x</p>", "x" )]
        [InlineData( "<! foo >x", "x" )]
        [InlineData( "<? php echo 1 ?>x", "x" )]
        [InlineData( "<?xml version=\"1.0\"?><p>y</p>", "y" )]
        public void Declarations_And_Processing_Instructions_Are_Never_Emitted( string input, string mustSurvive )
        {
            var result = LinkListHtmlSanitizer.Sanitize( input );

            Assert.Contains( mustSurvive, result );
            Assert.DoesNotContain( "<!", result );
            Assert.DoesNotContain( "<?", result );
            Assert.DoesNotContain( "doctype", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.DoesNotContain( "php", result, System.StringComparison.OrdinalIgnoreCase );
            Assert.Equal( result, LinkListHtmlSanitizer.Sanitize( result ) );
        }

        private static int CountOccurrences( string haystack, string needle )
        {
            var count = 0;
            var index = haystack.IndexOf( needle, System.StringComparison.OrdinalIgnoreCase );
            while ( index >= 0 )
            {
                count++;
                index = haystack.IndexOf( needle, index + needle.Length, System.StringComparison.OrdinalIgnoreCase );
            }
            return count;
        }
    }
}
