using Xunit;

namespace org.secc.Communication.Tests
{
    /// <summary>
    /// Tests the two-argument overload only — the parameterless overload reads the
    /// OrganizationName global attribute, which needs a Rock database context.
    /// </summary>
    public class SmsDisclosureTests
    {
        [Fact]
        public void Html_EncodesOrganizationName()
        {
            var html = SmsDisclosure.Html( "Org <script>alert(1)</script>", SmsDisclosure.DefaultMargin );

            Assert.DoesNotContain( "<script>", html );
            Assert.Contains( "&lt;script&gt;", html );
        }

        [Fact]
        public void Html_NamesShortCode()
        {
            var html = SmsDisclosure.Html( "Org", SmsDisclosure.DefaultMargin );

            Assert.Contains( SmsDisclosure.ShortCode, html );
            Assert.Contains( "733733", html );
        }

        [Fact]
        public void Html_ContainsFiledLinks()
        {
            var html = SmsDisclosure.Html( "Org", SmsDisclosure.DefaultMargin );

            Assert.Contains( "https://se.church/privacy-policy", html );
            Assert.Contains( "https://se.church/terms", html );
        }

        [Theory]
        [InlineData( "4px; } body { display:none" )]
        [InlineData( "expression(alert(1))" )]
        [InlineData( "0' onmouseover='alert(1)" )]
        [InlineData( "" )]
        [InlineData( null )]
        public void Html_InvalidMarginFallsBackToDefault( string margin )
        {
            var html = SmsDisclosure.Html( "Org", margin );

            Assert.Contains( $"margin:{SmsDisclosure.DefaultMargin};", html );
        }

        [Theory]
        [InlineData( "4px 0 12px 0" )]
        [InlineData( "12px 0 4px 0" )]
        [InlineData( "-8px 0 12px 0" )]
        [InlineData( "0" )]
        [InlineData( "1.5em 0" )]
        public void Html_ValidMarginIsPreserved( string margin )
        {
            var html = SmsDisclosure.Html( "Org", margin );

            Assert.Contains( $"margin:{margin};", html );
        }

        [Fact]
        public void ConsentText_EncodesOrganizationName()
        {
            var text = SmsDisclosure.ConsentText( "Org <script>alert(1)</script>" );

            Assert.DoesNotContain( "<script>", text );
            Assert.Contains( "&lt;script&gt;", text );
        }

        [Fact]
        public void ConsentText_IsAnAffirmativeCheckboxStatement()
        {
            var text = SmsDisclosure.ConsentText( "Org" );

            // Carriers require the wording to describe the act of ticking the box, and to say
            // consent is not a condition of purchase. Both are audited.
            Assert.StartsWith( "By checking this box", text );
            Assert.Contains( "Org", text );
            Assert.Contains( "Consent is not a condition of purchase.", text );
        }

        [Fact]
        public void ConsentText_CarriesNoMarkup()
        {
            // Rendered as a checkbox label, not as pass-through HTML.
            var text = SmsDisclosure.ConsentText( "Org" );

            Assert.DoesNotContain( "<", text );
            Assert.DoesNotContain( ">", text );
        }
    }
}
