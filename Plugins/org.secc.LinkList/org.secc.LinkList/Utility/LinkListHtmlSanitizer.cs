using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace org.secc.LinkList.Utility
{
    /// <summary>
    /// Idempotent, entity-preserving HTML sanitizer for the freeform intro /
    /// footer blobs. Drops blacklisted tags (and their subtree), strips every
    /// on* handler, and removes javascript:/data:/vbscript:/file: URLs from
    /// href/src - testing the DECODED, whitespace/control-char-stripped value so
    /// entity tricks (e.g. <c>java&amp;#9;script:</c>) can't slip past.
    ///
    /// Emits via HtmlAgilityPack's OuterHtml so HTML entities (e.g.
    /// <c>&amp;copy;</c>) are preserved verbatim - the operation is idempotent,
    /// so sanitizing on read AND the editor saving the value back never
    /// double-encodes. (Rock's own HtmlSanitizer round-trips through
    /// XmlTextWriter, which re-encodes entities and compounds on every save.)
    ///
    /// Depends only on HtmlAgilityPack + the BCL (no Rock types) so it is
    /// unit-testable in isolation.
    /// </summary>
    public static class LinkListHtmlSanitizer
    {
        // Tags removed wholesale (with their subtree). Superset of Rock's default
        // blacklist + svg/style/base/noscript/applet + math/template/frame/frameset
        // (foreign-content / mutation-XSS vectors).
        private static readonly HashSet<string> TagBlackList = new HashSet<string>( StringComparer.OrdinalIgnoreCase )
        {
            "script", "iframe", "form", "object", "embed", "link", "head", "meta",
            "svg", "style", "base", "noscript", "applet", "math", "template", "frame", "frameset"
        };

        private static readonly Regex ControlAndWhitespace = new Regex( @"[\s\x00-\x1F]", RegexOptions.Compiled );
        private static readonly Regex DangerousScheme = new Regex( @"^(javascript|data|vbscript|file):", RegexOptions.IgnoreCase | RegexOptions.Compiled );

        public static string Sanitize( string html )
        {
            if ( string.IsNullOrWhiteSpace( html ) )
            {
                return html;
            }

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml( html );
            Clean( doc.DocumentNode );
            return doc.DocumentNode.OuterHtml;
        }

        private static void Clean( HtmlAgilityPack.HtmlNode node )
        {
            foreach ( var child in node.ChildNodes.ToList() )
            {
                if ( child.NodeType != HtmlAgilityPack.HtmlNodeType.Element )
                {
                    continue;
                }

                if ( TagBlackList.Contains( child.Name ) )
                {
                    child.Remove();
                    continue;
                }

                foreach ( var attr in child.Attributes.ToList() )
                {
                    var name = attr.Name.ToLowerInvariant();
                    if ( name.StartsWith( "on" ) )
                    {
                        attr.Remove();
                        continue;
                    }
                    if ( name == "href" || name == "src" )
                    {
                        // Decode entities, then drop all whitespace/control chars
                        // before testing the scheme so href="java&#9;script:..."
                        // can't slip past (the browser strips the tab + executes).
                        var url = ControlAndWhitespace.Replace(
                            HtmlAgilityPack.HtmlEntity.DeEntitize( attr.Value ?? string.Empty ),
                            string.Empty );
                        if ( DangerousScheme.IsMatch( url ) )
                        {
                            attr.Remove();
                        }
                    }
                }

                Clean( child );
            }
        }
    }
}
