using System.Text.RegularExpressions;
using System.Web;
using Rock.Web.Cache;

namespace org.secc.Communication
{
    /// <summary>
    /// Carrier-required SMS program disclosures for the org's short code, rendered directly
    /// beneath any field or control that collects a mobile number for opt-in. The wording
    /// must match the call-to-action template filed with the carriers for 733733 — do not
    /// edit it without re-checking the filed template. The privacy/terms URLs are
    /// deliberately hardcoded because they are the exact links filed with the carrier and
    /// must not drift with CMS changes. The org name comes from the OrganizationName global
    /// attribute and is HTML-encoded.
    /// </summary>
    public static class SmsDisclosure
    {
        public const string ShortCode = "733733";
        public const string DefaultMargin = "-8px 0 12px 0";

        // margin is interpolated into a style attribute; constrain it to a CSS margin
        // shorthand (1-4 signed numeric terms with optional unit) so it can never be a
        // style-injection sink.
        private static readonly Regex MarginPattern = new Regex(
            @"^-?\d+(\.\d+)?(px|em|rem|%)?(\s+-?\d+(\.\d+)?(px|em|rem|%)?){0,3}$",
            RegexOptions.Compiled );

        public static string Html( string margin = DefaultMargin )
        {
            return Html( GlobalAttributesCache.Value( "OrganizationName" ), margin );
        }

        public static string Html( string organizationName, string margin )
        {
            if ( string.IsNullOrWhiteSpace( margin ) || !MarginPattern.IsMatch( margin.Trim() ) )
            {
                margin = DefaultMargin;
            }
            var encodedOrgName = HttpUtility.HtmlEncode( organizationName );
            return $"<div class='small' style='color:#595959;margin:{margin};line-height:1.5;'>"
                + $"{encodedOrgName} text messages from {ShortCode}. Message frequency varies. "
                + "Message &amp; data rates may apply. Reply STOP to opt out, HELP for help. "
                + "<a href='https://se.church/privacy-policy' target='_blank' rel='noopener noreferrer' title='Opens in a new tab'>Privacy Policy</a>"
                + " &middot; "
                + "<a href='https://se.church/terms' target='_blank' rel='noopener noreferrer' title='Opens in a new tab'>Mobile Terms</a>"
                + "</div>";
        }
    }
}
