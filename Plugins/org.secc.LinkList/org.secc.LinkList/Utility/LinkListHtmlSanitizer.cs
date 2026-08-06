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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace org.secc.LinkList.Utility
{
    /// <summary>
    /// Idempotent, entity-preserving HTML sanitizer for the freeform intro /
    /// footer blobs (ROCK-8880).
    ///
    /// This used to be a tag BLACKLIST (drop script/iframe/... , pass everything
    /// else). A blacklist can only ever remove the vectors we already thought of,
    /// so it leaked stored XSS through anything novel - mutation-XSS (mXSS) via
    /// malformed / half-entity-encoded attribute soup, foreign-content elements,
    /// and any tag/attribute we simply hadn't enumerated. Real prod content
    /// exercised exactly that (e.g. item 9644's broken
    /// <c>style=" text-align:="" left;"=""</c> attribute mess).
    ///
    /// It is now an ALLOWLIST: only known-safe tags survive, and on each surviving
    /// tag only known-safe attributes survive. Anything not explicitly allowed is
    /// removed. Non-allowlisted tags fall into two buckets:
    ///   * dangerous / foreign-content tags (script, iframe, object, svg, math,
    ///     ...) are removed. We call Remove() on the element, which drops the
    ///     subtree for well-formed nesting; but the ALLOWLIST is the real
    ///     guarantee, NOT subtree removal - HAP 1.4.9.5 marks some tags (e.g.
    ///     <c>form</c>) CanOverlap, so their "children" can parse as siblings and
    ///     survive the Remove(). Those orphaned children are still scrubbed
    ///     because every surviving node must independently clear the allowlist;
    ///     nothing relies on the subtree going away as its sole defense. And
    ///   * benign-but-unknown tags are UNWRAPPED (tag dropped, inner text/children
    ///     kept) so a stray wrapper never eats visible copy.
    ///
    /// URL-bearing attributes (href, src) are scheme-checked against the DECODED,
    /// whitespace/control-char-stripped value so entity tricks
    /// (e.g. <c>java&amp;#9;script:</c>) can't slip past - blocking
    /// javascript:/data:/vbscript:/file: while allowing http/https/mailto/tel and
    /// relative URLs. The <c>style</c> attribute is allowed but its value is run
    /// through a CSS property allowlist (see <see cref="FilterStyle"/>).
    ///
    /// HTML comments are stripped - they render nothing, and one prod footer
    /// carried a broken half-commented block; dropping them is safe and avoids
    /// re-serializing malformed comment syntax.
    ///
    /// One attribute is ADDED rather than filtered: an <c>&lt;a&gt;</c> that keeps
    /// its <c>target</c> gets <c>rel="noopener noreferrer"</c> forced onto it (see
    /// <see cref="InjectTabnabbingGuard"/>), because <c>rel</c> is on no allowlist
    /// and an author therefore cannot supply the mitigation themselves.
    ///
    /// BYTE-FAITHFULNESS, ONE EXCEPTION: the value is otherwise emitted with its
    /// original bytes (entities untouched, see below), but a bare <c>&lt;</c> that
    /// cannot open a tag is rewritten to <c>&amp;lt;</c> BEFORE parsing (see
    /// <see cref="BareLessThan"/>). Those bytes were invalid HTML and
    /// <c>&amp;lt;</c> is what a browser renders them as, so this is a correction,
    /// not a mutation - and it is still idempotent, since the rewritten form has no
    /// bare <c>&lt;</c> left to rewrite. Without it, HAP's mis-recovery silently
    /// deleted every word after the <c>&lt;</c>.
    ///
    /// Emits via HtmlAgilityPack's OuterHtml so HTML entities (e.g.
    /// <c>&amp;copy;</c>) are preserved verbatim - the operation is idempotent
    /// (<c>Sanitize(Sanitize(x)) == Sanitize(x)</c>), so sanitizing on read AND
    /// the editor saving the value back never double-encodes. (Rock's own
    /// HtmlSanitizer round-trips through XmlTextWriter, which re-encodes entities
    /// and compounds on every save - which is why we don't use it.)
    ///
    /// CRITICAL: HAP 1.4.9.5's OuterHtml does NOT encode interior double-quotes in
    /// an attribute value, so a smuggled unquoted-attribute payload
    /// (<c>&lt;img src=x alt=a"onerror="alert(1)&gt;</c>) parses as a single
    /// <c>alt</c> value <c>a"onerror="alert(1)</c> and re-serializes RAW, letting
    /// the browser's "missing whitespace between attributes" error recovery
    /// re-tokenize it into a live event handler (mXSS). We therefore do NOT trust
    /// HAP re-serialization to be XSS-safe: every retained attribute (including
    /// <c>style</c>) is written back through <see cref="SetSafeAttribute"/> /
    /// <see cref="EncodeAttributeValue"/>, which forces a double-quote delimiter
    /// and entity-encodes the characters that could break the value out of its
    /// quotes or start a tag (<c>" &lt; &gt;</c>). <c>&amp;</c> is deliberately
    /// left untouched on ALL paths so pre-existing entities survive verbatim and
    /// stored bytes stay byte-faithful (see <see cref="FilterStyle"/>).
    ///
    /// SECURITY DECODE: the scheme check (<see cref="IsDangerousUrl"/>) and the
    /// CSS dangerous-value check (<see cref="FilterStyle"/>) must see the text a
    /// browser would after HTML-decoding, so both decode entities first - but via
    /// <see cref="SafeDeEntitize"/>, NOT HtmlAgilityPack.HtmlEntity.DeEntitize.
    /// HAP 1.4.9.5's DeEntitize THROWS <c>KeyNotFoundException</c> on any
    /// semicolon-terminated named entity absent from its HTML4-era table
    /// (<c>&amp;colon;</c>, <c>&amp;Tab;</c>, <c>&amp;lpar;</c>, a bogus
    /// <c>&amp;foo;</c>, ...); since no call site has a try/catch, one stored
    /// value carrying such a token would permanently 500 the public list viewer.
    /// <see cref="SafeDeEntitize"/> never throws: it degrades undecodable tokens
    /// to literal text while still decoding the security-relevant HTML5 named
    /// entities HAP lacks, so an obfuscated <c>javascript&amp;colon;</c> is still
    /// neutralized.
    ///
    /// Depends only on HtmlAgilityPack + the BCL (no Rock types) so it is
    /// unit-testable in isolation.
    /// </summary>
    public static class LinkListHtmlSanitizer
    {
        // Tags that are allowed to survive. Everything real prod intro/footer
        // content uses: layout, inline formatting, headings, tables, lists,
        // legacy <font>, images and links.
        //
        // A tag missing from this set is UNWRAPPED, which silently drops the
        // `class` it carried - so every class-bearing wrapper the email builder
        // emits has to be listed here or the CSS scoped to it stops applying (the
        // `class` allowance below is pointless without the element that holds it).
        // The table sub-elements matter for a second reason: unwrapping <caption>
        // leaves a bare text node inside <table>, which a browser's "in table"
        // insertion mode FOSTER-PARENTS out of the table entirely, so the caption
        // renders above the table as loose body text.
        //
        // Listing a tag here allows only the TAG. Its attributes are still filtered
        // independently by FilterAttributes.
        private static readonly HashSet<string> AllowedTags = new HashSet<string>( StringComparer.OrdinalIgnoreCase )
        {
            "div", "span", "p", "a", "img", "br",
            "b", "i", "u", "strong", "em", "small", "s", "strike",
            "h1", "h2", "h3", "h4", "h5", "h6",
            "table", "thead", "tbody", "tfoot", "tr", "td", "th",
            "caption", "colgroup", "col",
            "ul", "ol", "li", "dl", "dt", "dd",
            "section", "article", "header", "footer", "nav",
            "figure", "figcaption", "pre", "code", "label",
            "font", "hr", "blockquote", "sub", "sup"
        };

        // Non-allowlisted tags removed wholesale (with their subtree) rather than
        // unwrapped, because for these the child content IS the attack payload or
        // is foreign-content that browsers parse under different rules (mXSS).
        // Superset of the old blacklist: Rock's defaults + svg/style/base/noscript/
        // applet + math/template/frame/frameset.
        private static readonly HashSet<string> DangerousTags = new HashSet<string>( StringComparer.OrdinalIgnoreCase )
        {
            "script", "iframe", "form", "object", "embed", "link", "head", "meta",
            "svg", "style", "base", "noscript", "applet", "math", "template", "frame", "frameset"
        };

        // Attributes allowed on ANY allowlisted tag. Intentionally excludes every
        // on* handler and every data-* attribute - prod email-builder markup is
        // class-based (esd-block-*, es-text-*), so `class` covers it. `style` is
        // handled specially (value-filtered) before this set is consulted.
        // The presentational table attributes (align/valign/border/cellpadding/
        // cellspacing/bgcolor/nowrap) are here as a set: email-builder output paints
        // cell and table backgrounds with the legacy `bgcolor` ATTRIBUTE rather than
        // CSS, because email clients strip <style>. Omitting one of them renders
        // those blocks white/transparent.
        private static readonly HashSet<string> GlobalAttributes = new HashSet<string>( StringComparer.OrdinalIgnoreCase )
        {
            "class", "id", "title", "alt", "align", "width", "height",
            "colspan", "rowspan", "valign", "border", "cellpadding", "cellspacing",
            "bgcolor", "nowrap"
        };

        // Attributes allowed only on specific tags. Note `rel` is deliberately
        // absent from <a>: it is not carried over from the author at all, it is
        // FORCE-INJECTED by FilterAttributes whenever `target` survives (see
        // InjectTabnabbingGuard).
        private static readonly Dictionary<string, HashSet<string>> PerTagAttributes = new Dictionary<string, HashSet<string>>( StringComparer.OrdinalIgnoreCase )
        {
            ["a"] = new HashSet<string>( StringComparer.OrdinalIgnoreCase ) { "href", "target" },
            ["img"] = new HashSet<string>( StringComparer.OrdinalIgnoreCase ) { "src" },
            ["font"] = new HashSet<string>( StringComparer.OrdinalIgnoreCase ) { "color", "face", "size" },
            // The `span` ATTRIBUTE on <col>/<colgroup> (column count) - unrelated to
            // the <span> tag. Kept per-tag so it cannot leak onto other elements.
            ["col"] = new HashSet<string>( StringComparer.OrdinalIgnoreCase ) { "span" },
            ["colgroup"] = new HashSet<string>( StringComparer.OrdinalIgnoreCase ) { "span" }
        };

        // Attributes whose value is a URL and must pass the scheme check. Kept in
        // sync with the URL-bearing entries actually present in the allowlist
        // above (href on <a>, src on <img>). Other URL-bearing attributes
        // (formaction, poster, background, xlink:href, ...) are simply absent from
        // the allowlist and therefore stripped outright.
        private static readonly HashSet<string> UrlAttributes = new HashSet<string>( StringComparer.OrdinalIgnoreCase )
        {
            "href", "src"
        };

        // CSS properties allowed to survive inside a `style` value. Chosen from a
        // 2026-08-05 audit of the 59 prod rows that carry style=. Everything else
        // (position, z-index, display, ...) is dropped.
        private static readonly HashSet<string> AllowedCssProperties = new HashSet<string>( StringComparer.OrdinalIgnoreCase )
        {
            "color", "background-color", "font-family", "font-size",
            "text-align", "width", "line-height", "margin-left"
        };

        private static readonly Regex ControlAndWhitespace = new Regex( @"[\s\x00-\x1F]", RegexOptions.Compiled );

        // A '<' that cannot open a tag, i.e. literal text. Per the HTML5 tokenizer's
        // tag-open state, '<' only starts markup when followed by an ASCII letter
        // (start tag), '/' (end tag), '!' (comment/doctype) or '?' (bogus comment);
        // anything else - a space, a digit, end-of-input - is emitted as a literal
        // '<' character by a browser.
        //
        // HAP 1.4.9.5 does NOT follow that rule. It parses `<p>Ages < 12 welcome</p>`
        // as <p> containing the text "Ages " plus an ELEMENT WITH AN EMPTY NAME whose
        // swallowed words become ATTRIBUTES (`12=""`, `welcome=""`). That empty name
        // is on neither allowlist, so Clean() unwraps it - and Unwrap moves only
        // ChildNodes, of which it has none, so the node and the words held in its
        // attributes are DISCARDED. Real list copy ("Ages < 12", "Cost < $5") lost
        // everything after the '<' on the next render.
        //
        // Escaping these up front, before HAP sees them, restores browser parity and
        // is the one place the sanitizer is not byte-faithful (see class remarks).
        private static readonly Regex BareLessThan = new Regex( @"<(?![a-zA-Z/!?])", RegexOptions.Compiled );

        // CultureInvariant is REQUIRED: IgnoreCase folds case using the current
        // thread culture, and Rock sets per-request culture from globalization
        // settings. Under tr-TR/az, ASCII 'I' does NOT fold to 'i', so a bare
        // IgnoreCase would let `JAVASCRIPT:` slip past on such a request thread.
        private static readonly Regex DangerousScheme = new Regex( @"^(javascript|data|vbscript|file):", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled );

        // Defense in depth: reject any declaration whose VALUE can reach out
        // (url(), @import) or execute (expression(), behavior:, javascript:),
        // even when the property itself is on the allowlist. CultureInvariant for
        // the same Turkish-i reason as DangerousScheme above.
        //
        // LIMITATION - THIS REGEX IS DECORATIVE, NOT A GUARANTEE. It is BYPASSABLE
        // by CSS backslash escapes. SafeDeEntitize decodes HTML entities but does
        // NOT canonicalize CSS escapes, so `\75 rl(` (the `u` of `url` written as
        // the CSS escape `\75 `) never matches `url\(` here, yet the browser's CSS
        // tokenizer folds `\75 rl(` back into a real `url(` function token. Proven:
        // `background-color: \75 rl(//evil/x)` and `width: \75 rl(//evil/x)` both
        // survive this filter unchanged (see the Style_CssEscaped_Url_* tests).
        // This is NOT exploitable TODAY only because none of the 8 allowlisted
        // properties accepts a url()/image value, so the browser drops the whole
        // declaration - the PROPERTY ALLOWLIST is the sole real guarantee here, NOT
        // this regex. WARNING: if AllowedCssProperties ever gains a URL- or
        // image-bearing property (`background`, `background-image`,
        // `list-style-image`, `cursor`, `content`), this becomes a LIVE
        // external-fetch / exfiltration vector with no second line of defense.
        // DangerousCssValue MUST be made escape-aware (canonicalize CSS escapes
        // before matching) BEFORE any such property is added to the allowlist.
        private static readonly Regex DangerousCssValue = new Regex( @"url\(|expression\(|behavior|javascript|@import", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled );

        // A single HTML entity reference: numeric (decimal or hex) or a named
        // token. Used by SafeDeEntitize to decode one token at a time so an
        // undecodable token can degrade to literal text without failing the rest.
        //
        // The terminating ';' is OPTIONAL (';?'). SafeDeEntitize feeds SECURITY
        // CHECKS ONLY, and a checking decoder must decode AT LEAST as aggressively
        // as a browser: browsers consume and decode a numeric reference in an
        // attribute value even without the trailing ';' (an HTML5 parse error, but
        // the reference is still decoded), so `javascript&#58alert(1)`,
        // `&#x3a`, and `java&#9script:` all execute if we require the ';'. Under-
        // decoding is fail-OPEN (an exploit); over-decoding only costs a false-
        // positive rejection (fail-CLOSED, acceptable). We therefore also decode
        // named references without a ';' - we deliberately do NOT replicate the
        // browser's legacy "&name not followed by '=' or alphanumeric" rule; the
        // resulting false positive on a literal like `&copy` in a URL is harmless
        // because the scheme regex is anchored at '^' (a decoded query string can
        // never turn an https URL into a dangerous scheme).
        private static readonly Regex EntityReference = new Regex(
            @"&(#[xX][0-9a-fA-F]+|#[0-9]+|[A-Za-z][A-Za-z0-9]*);?", RegexOptions.Compiled );

        // Security-relevant HTML5 named entities that HtmlAgilityPack 1.4.9.5's
        // HTML4-era table does NOT know. A browser DOES decode these when parsing
        // the attribute value, and several decode to characters that can spell a
        // dangerous scheme or CSS function (&colon; -> ':' completes
        // "javascript:", &lpar;/&rpar; -> '(' ')' completes "url(", &Tab;/&NewLine;
        // -> control chars the URL parser strips). We MUST decode them so the
        // security checks see what the browser will. Case-sensitive, per the HTML
        // spec (StringComparer.Ordinal). Anything NOT here and not known to HAP is
        // left literal - harmless for the checks, and byte-faithful on write-back.
        private static readonly Dictionary<string, string> SupplementalEntities = new Dictionary<string, string>( StringComparer.Ordinal )
        {
            ["amp"] = "&", ["lt"] = "<", ["gt"] = ">", ["quot"] = "\"", ["apos"] = "'", ["nbsp"] = " ",
            ["colon"] = ":", ["semi"] = ";", ["period"] = ".", ["sol"] = "/", ["bsol"] = "\\",
            ["lpar"] = "(", ["rpar"] = ")", ["lbrace"] = "{", ["rbrace"] = "}", ["lcub"] = "{", ["rcub"] = "}",
            ["lbrack"] = "[", ["rbrack"] = "]", ["lsqb"] = "[", ["rsqb"] = "]",
            ["num"] = "#", ["percnt"] = "%", ["commat"] = "@", ["excl"] = "!", ["quest"] = "?",
            ["equals"] = "=", ["ast"] = "*", ["comma"] = ",", ["plus"] = "+", ["lowbar"] = "_",
            ["dollar"] = "$", ["verbar"] = "|", ["vert"] = "|", ["grave"] = "`", ["Hat"] = "^",
            ["Tab"] = "\t", ["NewLine"] = "\n"
        };

        /// <summary>
        /// Best-effort, single-level, NEVER-throwing HTML entity decode used ONLY
        /// for the security decisions (URL scheme check and CSS dangerous-value
        /// check). A drop-in safe replacement for
        /// HtmlAgilityPack.HtmlEntity.DeEntitize, which throws
        /// <see cref="KeyNotFoundException"/> on any semicolon-terminated named
        /// entity outside its HTML4 table (e.g. <c>&amp;colon;</c>, <c>&amp;foo;</c>).
        ///
        /// Decodes one entity token at a time so one undecodable token cannot take
        /// down the whole value: numeric references are parsed with range/surrogate
        /// guards; named references resolve against <see cref="SupplementalEntities"/>
        /// first (the HTML5 gaps that matter for security), then fall through to
        /// HAP's table wrapped in try/catch, and finally degrade to the literal
        /// token text. Like a browser's attribute-value decode, it is single-level
        /// (<c>&amp;amp;quot;</c> -> <c>&amp;quot;</c>), which is exactly the depth
        /// the security checks need.
        /// </summary>
        private static string SafeDeEntitize( string value )
        {
            if ( string.IsNullOrEmpty( value ) )
            {
                return value ?? string.Empty;
            }
            return EntityReference.Replace( value, DecodeEntity );
        }

        private static string DecodeEntity( Match m )
        {
            var token = m.Value;            // full reference incl. '&' (';' optional)
            var body = m.Groups[1].Value;   // "colon" / "#58" / "#x3a"

            if ( body[0] == '#' )
            {
                int code;
                bool parsed;
                if ( body.Length > 1 && ( body[1] == 'x' || body[1] == 'X' ) )
                {
                    parsed = int.TryParse( body.Substring( 2 ), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code );
                }
                else
                {
                    parsed = int.TryParse( body.Substring( 1 ), NumberStyles.Integer, CultureInfo.InvariantCulture, out code );
                }

                // Leave the literal token on anything that is not a valid Unicode
                // scalar value (out of range, surrogate, or an absurdly long number
                // that overflows int) - best effort, never throw.
                if ( !parsed || code < 0 || code > 0x10FFFF || ( code >= 0xD800 && code <= 0xDFFF ) )
                {
                    return token;
                }
                try
                {
                    return char.ConvertFromUtf32( code );
                }
                catch
                {
                    return token;
                }
            }

            if ( SupplementalEntities.TryGetValue( body, out var mapped ) )
            {
                return mapped;
            }

            // Long tail of HTML4 names HAP does know (e.g. &copy;). DeEntitize
            // THROWS on names it does not know, so wrap it; on throw (or a no-op
            // result) fall back to the literal token.
            try
            {
                var decoded = HtmlAgilityPack.HtmlEntity.DeEntitize( token );
                return string.IsNullOrEmpty( decoded ) ? token : decoded;
            }
            catch
            {
                return token;
            }
        }

        public static string Sanitize( string html )
        {
            if ( string.IsNullOrWhiteSpace( html ) )
            {
                return html;
            }

            // Escape any '<' that a browser would treat as literal text BEFORE
            // parsing - HAP mis-recovers those into an empty-named element whose
            // text ends up in attributes, and the unwrap path then drops it. See
            // the BareLessThan remarks.
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml( BareLessThan.Replace( html, "&lt;" ) );
            Clean( doc.DocumentNode );
            return doc.DocumentNode.OuterHtml;
        }

        private static void Clean( HtmlAgilityPack.HtmlNode node )
        {
            foreach ( var child in node.ChildNodes.ToList() )
            {
                if ( child.NodeType == HtmlAgilityPack.HtmlNodeType.Comment )
                {
                    // Comments render nothing; strip them (see class remarks).
                    child.Remove();
                    continue;
                }

                if ( child.NodeType != HtmlAgilityPack.HtmlNodeType.Element )
                {
                    // Text, and ONLY Text. HtmlNodeType in HAP 1.4.9.5 has exactly
                    // four members - Document, Element, Comment, Text - so by the
                    // time we reach here Comment is already removed above, Element
                    // is handled below, and Document never appears as a child.
                    //
                    // There is NO declaration or processing-instruction node type in
                    // this HAP version, so there is nothing extra to drop here.
                    // Verified against the shipped 1.4.9.5 assembly: `<!DOCTYPE html>`
                    // and `<! foo >` are parsed as COMMENT nodes (already stripped by
                    // the branch above), and `<? php ?>` / `<?xml ... ?>` are parsed
                    // as an ELEMENT literally named `?`, which is on neither allowlist
                    // and so falls to the benign-unknown unwrap path below - it has no
                    // children, so it disappears. Sanitize() emits none of them.
                    //
                    // So this is the text-node path, and text passes through verbatim
                    // to keep entities byte-faithful. Adding an explicit Remove() for
                    // "other" node types would be dead code.
                    continue;
                }

                var name = child.Name.ToLowerInvariant();

                if ( DangerousTags.Contains( name ) )
                {
                    child.Remove();
                    continue;
                }

                if ( !AllowedTags.Contains( name ) )
                {
                    // Benign unknown tag: clean the subtree, then unwrap so the
                    // inner text/children survive while the tag itself is dropped.
                    Clean( child );
                    Unwrap( child );
                    continue;
                }

                FilterAttributes( child, name );
                Clean( child );
            }
        }

        private static void Unwrap( HtmlAgilityPack.HtmlNode node )
        {
            var parent = node.ParentNode;
            if ( parent == null )
            {
                node.Remove();
                return;
            }

            foreach ( var moved in node.ChildNodes.ToList() )
            {
                moved.Remove();
                parent.InsertBefore( moved, node );
            }
            node.Remove();
        }

        private static void FilterAttributes( HtmlAgilityPack.HtmlNode node, string tag )
        {
            foreach ( var attr in node.Attributes.ToList() )
            {
                var name = attr.Name.ToLowerInvariant();

                if ( name == "style" )
                {
                    // FilterStyle returns the surviving RAW declaration bytes with
                    // only the attribute-boundary chars (" < >) encoded and '&'
                    // left verbatim, so it is already safe to emit; we only force
                    // the quote delimiter here.
                    var filtered = FilterStyle( attr.Value );
                    if ( string.IsNullOrEmpty( filtered ) )
                    {
                        attr.Remove();
                    }
                    else
                    {
                        attr.QuoteType = HtmlAgilityPack.AttributeValueQuote.DoubleQuote;
                        attr.Value = filtered;
                    }
                    continue;
                }

                // KNOWN, DELIBERATELY UNFIXED: HAP 1.4.9.5 mis-tokenizes
                // slash-separated attributes - `<img/src="x"/alt="y">` yields
                // attributes literally named `rc` and `lt` (it eats the `/s` and
                // `/a`), where a browser would read `src` and `alt`. Neither name
                // is allowlisted, so both are dropped here. That is fail-CLOSED and
                // no worse than the old blacklist, which kept the same bogus `rc`/
                // `lt` names and so produced an equally src-less broken image.
                // Normalizing the separators would mean rewriting raw attribute soup
                // ahead of the parser - new mXSS surface for no visual gain.
                if ( !IsAttributeAllowed( tag, name ) )
                {
                    attr.Remove();
                    continue;
                }

                if ( UrlAttributes.Contains( name ) && IsDangerousUrl( attr.Value ) )
                {
                    attr.Remove();
                    continue;
                }

                // Attribute survives. Write it back safely: HAP 1.4.9.5 will emit
                // interior double-quotes RAW, so a smuggled `alt=a"onerror="...`
                // would re-tokenize into a live handler (mXSS). Force a
                // double-quote delimiter and entity-encode the break-out chars.
                SetSafeAttribute( attr, EncodeAttributeValue( attr.Value ) );
            }

            if ( tag == "a" && node.Attributes["target"] != null )
            {
                InjectTabnabbingGuard( node );
            }
        }

        // rel value forced onto any <a> that keeps a target. noreferrer is included
        // as well as noopener because the pre-Chrome-88 / pre-Firefox-79 / IE and
        // embedded-webview engines that lack implicit noopener are largely the same
        // ones that only honor noreferrer.
        private const string TabnabbingRel = "noopener noreferrer";

        /// <summary>
        /// Closes reverse tabnabbing on a link that kept its <c>target</c>.
        ///
        /// A page opened via <c>target="_blank"</c> receives a live
        /// <c>window.opener</c> on any engine without implicit noopener (all IE /
        /// Edge Legacy, Chrome &lt; 88, Firefox &lt; 79, Safari &lt; 12.1, and many
        /// in-app webviews) and can navigate the ORIGINAL tab - e.g. to a phishing
        /// clone of the site the reader thinks they are still on.
        ///
        /// <c>rel</c> is on no allowlist, so the loop above has already STRIPPED any
        /// author-supplied value; this always writes our constant rather than merging
        /// with theirs. That is deliberate: because this sanitizer is the single
        /// chokepoint for list content, an author cannot add the mitigation by hand
        /// (it would just be stripped), so it cannot be left to them to remember. The
        /// cost is that a deliberate <c>rel="nofollow"</c> is also overridden.
        ///
        /// Idempotent: on a second pass the injected <c>rel</c> is stripped as
        /// non-allowlisted and re-appended here in the same trailing position, so the
        /// bytes are identical.
        /// </summary>
        private static void InjectTabnabbingGuard( HtmlAgilityPack.HtmlNode node )
        {
            SetSafeAttribute( node.SetAttributeValue( "rel", string.Empty ), TabnabbingRel );
        }

        // Force a stable double-quote delimiter for a retained attribute so its
        // value can only be terminated by a literal '"' (which the encoders above
        // have already removed). Without this, an attribute whose source was
        // unquoted or single-quoted could still re-serialize in a way that lets
        // the value break out.
        private static void SetSafeAttribute( HtmlAgilityPack.HtmlAttribute attr, string encodedValue )
        {
            attr.QuoteType = HtmlAgilityPack.AttributeValueQuote.DoubleQuote;
            attr.Value = encodedValue;
        }

        // Entity-encode ONLY the characters that can break a value out of its
        // double-quoted attribute or open a tag. '&' is deliberately NOT encoded:
        // attr.Value here is HAP's RAW (still entity-encoded) text, so touching
        // '&' would double-encode legitimate entities (&copy;, &quot;) and break
        // both the verbatim-entity contract and idempotency. A literal '"' in this
        // raw text can only be a smuggled/mal-formed boundary (well-formed values
        // carry it as &quot;), so encoding it is always safe.
        private static string EncodeAttributeValue( string rawValue )
        {
            if ( string.IsNullOrEmpty( rawValue ) )
            {
                return rawValue;
            }
            return rawValue
                .Replace( "\"", "&quot;" )
                .Replace( "<", "&lt;" )
                .Replace( ">", "&gt;" );
        }

        private static bool IsAttributeAllowed( string tag, string attribute )
        {
            if ( GlobalAttributes.Contains( attribute ) )
            {
                return true;
            }
            return PerTagAttributes.TryGetValue( tag, out var allowed ) && allowed.Contains( attribute );
        }

        private static bool IsDangerousUrl( string value )
        {
            // Decode entities (via the never-throwing SafeDeEntitize - HAP's
            // DeEntitize would throw KeyNotFoundException on e.g. href="&colon;"),
            // then drop all whitespace/control chars before testing the scheme so
            // href="java&#9;script:..." or href="javascript&colon;..." can't slip
            // past (the browser decodes/strips + executes).
            var url = ControlAndWhitespace.Replace(
                SafeDeEntitize( value ?? string.Empty ),
                string.Empty );
            return DangerousScheme.IsMatch( url );
        }

        /// <summary>
        /// Filters a raw <c>style</c> attribute value down to the allowlisted CSS
        /// properties, dropping any declaration whose value looks like it reaches
        /// out or executes.
        ///
        /// The raw value HAP hands us is STILL ENTITY-ENCODED. We do NOT decode
        /// the whole value before splitting: a single-level decode of a
        /// DOUBLE-encoded entity (<c>&amp;amp;quot;</c> -> <c>&amp;quot;</c>) leaves
        /// a literal ';' that <c>Split(';')</c> would then treat as a declaration
        /// separator, silently discarding the remainder of a font stack. Instead
        /// we split the RAW value on ';' and REATTACH (with its ';') any segment
        /// that does not itself begin a new "property:value" declaration onto the
        /// current declaration's value - so no bytes are ever dropped, at any
        /// entity-nesting depth (see <see cref="StartsNewDeclaration"/>).
        ///
        /// Security decisions (the property-allowlist match and the
        /// <see cref="DangerousCssValue"/> test) run on a best-effort
        /// <see cref="SafeDeEntitize"/> decode so they see the same text the
        /// browser will after HTML-decoding (<c>&amp;#117;rl(</c> -> <c>url(</c>
        /// is still caught). But the STORED bytes written back are the ORIGINAL
        /// raw declaration text - only the attribute-boundary chars (" &lt; &gt;)
        /// are encoded, '&amp;' is left verbatim - so the value is byte-faithful
        /// and needs no re-encoding step (this also removes the old
        /// <c>&amp;nbsp;</c> -> U+00A0 / <c>&amp;#39;</c> -> ' round-trip mutation).
        ///
        /// Because the stored bytes are raw, a value that clears validation in its
        /// raw form could still DECODE, in the browser, into a string carrying a
        /// declaration separator plus a second <c>property:value</c> - re-splitting
        /// past this filter into a live second declaration.
        /// <see cref="DecodedValueSmugglesDeclaration"/> closes that: a declaration
        /// is kept only if BOTH its raw and its aggressively-decoded forms are safe.
        ///
        /// Property is the text before the first ':'. Returns the surviving
        /// declarations joined with "; ", or an empty string if none survive
        /// (caller removes the attribute). Output is normalized so re-running the
        /// filter over it is a no-op (idempotency).
        /// </summary>
        private static string FilterStyle( string style )
        {
            if ( string.IsNullOrWhiteSpace( style ) )
            {
                return string.Empty;
            }

            // Split the RAW (still entity-encoded) value on ';', then re-join any
            // segment that is not itself a new declaration back onto the current
            // value (restoring the ';' we split on). This is byte-faithful and
            // immune to entity nesting depth - no DeEntitize-before-split.
            var declarations = new List<string>();
            string current = null;
            foreach ( var segment in style.Split( ';' ) )
            {
                if ( StartsNewDeclaration( segment ) )
                {
                    if ( current != null )
                    {
                        declarations.Add( current );
                    }
                    current = segment;
                }
                else if ( current != null )
                {
                    // Continuation of the current value: restore the split ';'.
                    current = current + ";" + segment;
                }
                // else: leading text with no property to attach to - drop it.
            }
            if ( current != null )
            {
                declarations.Add( current );
            }

            var kept = new List<string>();
            foreach ( var declaration in declarations )
            {
                var trimmed = declaration.Trim();
                var colon = trimmed.IndexOf( ':' );
                if ( colon <= 0 )
                {
                    continue;
                }

                var rawProperty = trimmed.Substring( 0, colon ).Trim();
                var rawValue = trimmed.Substring( colon + 1 ).Trim();
                if ( rawValue.Length == 0 )
                {
                    continue;
                }

                // Best-effort decode ONLY for the security decisions; the browser
                // HTML-decodes the attribute value before the CSS parser sees it,
                // so decode to test the same text. The bytes kept are the raw ones.
                var decodedValue = SafeDeEntitize( rawValue );
                if ( !AllowedCssProperties.Contains( SafeDeEntitize( rawProperty ) ) )
                {
                    continue;
                }
                // NOTE: DangerousCssValue is DECORATIVE, not a guarantee - a CSS
                // backslash escape (`\75 rl(`) slips past it (SafeDeEntitize does
                // not canonicalize CSS escapes). The property allowlist above is
                // what actually keeps a url()/image value from ever mattering. See
                // the LIMITATION note on the DangerousCssValue field.
                if ( DangerousCssValue.IsMatch( decodedValue ) )
                {
                    continue;
                }
                // The declaration bytes are written back RAW (byte-faithful). The
                // browser HTML-decodes the attribute BEFORE the CSS parser runs, so
                // a value that is safe in its raw form can DECODE into text that
                // carries a declaration separator and a second "property:value" -
                // re-splitting, in the browser, past this allowlist into a live
                // second declaration (external url() fetch, position:fixed overlay,
                // expression()). Reject the declaration if its DECODED value could
                // form more declarations than we validated.
                if ( DecodedValueSmugglesDeclaration( decodedValue ) )
                {
                    continue;
                }

                kept.Add( rawProperty + ": " + rawValue );
            }

            if ( kept.Count == 0 )
            {
                return string.Empty;
            }

            // Encode ONLY the attribute-boundary chars (" < >) so a smuggled
            // literal quote can't break the style value out of its double quotes.
            // '&' is left untouched, so entity-encoded content (&quot; font names,
            // &amp;quot; double-encoded) is preserved verbatim and idempotent.
            return EncodeAttributeValue( string.Join( "; ", kept ) );
        }

        // A style segment (post Split(';')) begins a NEW declaration only if the
        // text before its first ':' is a plausible CSS property name (a non-empty
        // run of ASCII letters and '-'). Segments with no ':' - or whose pre-':'
        // text is not property-shaped, e.g. a font-family fragment like
        // <c>Segoe UI&amp;quot</c> - are continuations of the preceding value and
        // must be re-joined rather than parsed as their own declaration. The
        // property name is SafeDeEntitize'd first for parity with the browser.
        private static bool StartsNewDeclaration( string segment )
        {
            var colon = segment.IndexOf( ':' );
            if ( colon <= 0 )
            {
                return false;
            }
            var beforeColon = SafeDeEntitize( segment.Substring( 0, colon ) ).Trim();
            return beforeColon.Length > 0
                && beforeColon.All( c => c == '-' || ( c >= 'a' && c <= 'z' ) || ( c >= 'A' && c <= 'Z' ) );
        }

        // Post-decode gate for a single declaration's VALUE (BLOCKER B). This runs
        // on the SafeDeEntitize'd (entity-decoded) value. Its job is to reject a
        // value that could act as a property/value SEPARATOR once the browser's
        // CSS tokenizer sees it. The MECHANISM matters, because the tokenizer
        // treats the different "colon" spellings very differently, and the naive
        // reading ("a browser re-materializes a ':' out of whatever we stored, so
        // catch a literal ':'") is BACKWARDS for CSS escapes:
        //
        //   * A LITERAL ':' and an ENTITY-ENCODED colon (`&#58;`, `&#x3a;`,
        //     `&colon;`) both become a bare <colon-token> - exactly the token that
        //     separates a property from a value and can open a smuggled second
        //     declaration. Because the value is stored RAW (byte-faithful), an
        //     entity-encoded colon survives write-back and the browser HTML-decodes
        //     it back to a real ':' BEFORE the CSS parser runs. SafeDeEntitize
        //     decodes every colon form a browser would, so testing the DECODED
        //     value for ':' catches the literal AND every entity spelling. This is
        //     the SOLE reason the entity-decode step must stay: without it,
        //     `position&#58;fixed` clears this gate in raw form and then
        //     re-materializes into a live declaration in the browser. Do NOT
        //     "simplify away" that decode.
        //
        //   * A CSS BACKSLASH ESCAPE (`position\3a fixed`, or the same written as
        //     an HTML entity `position&#92;3a fixed` / `position&bsol;3a fixed`,
        //     which the browser decodes to a backslash FIRST) does NOT produce a
        //     separator. `\3a` is a CSS escape that yields a colon CODE POINT
        //     appended to the current identifier - ident content, never a
        //     <colon-token>. (Same reason `.foo\:bar` selects a class literally
        //     named `foo:bar`.) So an escaped colon can NEVER separate a property
        //     from a value: the browser folds `position\3a fixed` into one invalid
        //     identifier and drops the declaration. Escaped colons are INERT, we do
        //     not need to catch them, and SafeDeEntitize deliberately leaves the
        //     CSS-escape backslash untouched - so an escaped colon reaches this
        //     gate with no literal ':' and correctly passes.
        //
        // So a new live CSS declaration always needs that bare <colon-token>; a
        // rule block needs '{' or '}'. NONE of the 8 allowlisted property VALUES
        // legitimately contain a ':' , '{' or '}' (verified against the 2026-08-05
        // 59-row prod audit), so their presence in the DECODED value means a
        // smuggled declaration - reject it.
        //
        // A bare ';' is deliberately NOT rejected: on its own (with no following
        // property:value) it cannot form a new live declaration, and single-level
        // decoding of legitimate DOUBLE-encoded font stacks (`&amp;quot;` -> the
        // literal `&quot;`, `&amp;#59;` -> `&#59;`) yields inert ';' that must stay
        // byte-faithful. Keying on the ':' (and braces) catches every confirmed
        // smuggling payload while leaving those intact.
        private static bool DecodedValueSmugglesDeclaration( string decodedValue )
        {
            if ( string.IsNullOrEmpty( decodedValue ) )
            {
                return false;
            }
            return decodedValue.IndexOf( ':' ) >= 0
                || decodedValue.IndexOf( '{' ) >= 0
                || decodedValue.IndexOf( '}' ) >= 0;
        }
    }
}
