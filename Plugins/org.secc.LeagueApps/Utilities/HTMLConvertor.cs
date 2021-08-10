using System.Text;
using System.Text.RegularExpressions;

namespace org.secc.LeagueApps
{
    public class HTMLConvertor
    {
        public static string Convert( string HTML )
        {
            // Remove new lines since they are not visible in HTML
            HTML = HTML.Replace( "\n", " " );

            // Remove tab spaces
            HTML = HTML.Replace( "\t", " " );

            // Remove multiple white spaces from HTML
            HTML = Regex.Replace( HTML, "\\s+", " " );

            // Remove HEAD tag
            HTML = Regex.Replace( HTML, "<head.*?</head>", ""
                                , RegexOptions.IgnoreCase | RegexOptions.Singleline );

            // Remove any JavaScript
            HTML = Regex.Replace( HTML, "<script.*?</script>", ""
              , RegexOptions.IgnoreCase | RegexOptions.Singleline );

            // Replace special characters like &, <, >, " etc.
            StringBuilder stringHTML = new StringBuilder( HTML );
            // Note: There are many more special characters, these are just
            // most common. You can add new characters in this arrays if needed

            string[] specialchars = {"&nbsp;", "&amp;", "&quot;", "&lt;",
   "&gt;", "&reg;", "&copy;", "&bull;", "&trade;"};
            string[] replacechars = { " ", "&", "\"", "<", ">", "Â®", "Â©", "â€¢", "â„¢" };
            for ( int i = 0; i < specialchars.Length; i++ )
            {
                stringHTML.Replace( specialchars[i], replacechars[i] );
            }
            var x = stringHTML;
            // Check if there are line breaks (<br>) or paragraph (<p>)
            stringHTML.Replace( "<br>", "\n<br>" );
            stringHTML.Replace( "<br ", "\n<br " );
            stringHTML.Replace( "<p>", "\n<p>" );
            stringHTML.Replace( "<p ", "\n<p " );

            // Finally, remove all HTML tags and return plain text
            return Regex.Replace(
              stringHTML.ToString(), "<[^>]*>", "" );
        }
    }
}
