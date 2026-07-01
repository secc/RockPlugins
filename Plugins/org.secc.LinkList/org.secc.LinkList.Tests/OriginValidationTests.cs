using org.secc.LinkList.Services;

using Xunit;

namespace org.secc.LinkList.Tests
{
    public class OriginValidationTests
    {
        [Theory]
        [InlineData( "https://se.church" )]
        [InlineData( "https://www.se.church" )]
        [InlineData( "http://localhost:64706" )]
        [InlineData( "https://southeastchristian.org:443" )]
        public void Accepts_Valid_Origins( string origin )
        {
            Assert.True( LinkListService.IsValidOrigin( origin ) );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "se.church" )]                 // no scheme
        [InlineData( "ftp://se.church" )]           // wrong scheme
        [InlineData( "https://se.church/links" )]   // has a path
        [InlineData( "https://se.church/" )]        // trailing slash
        [InlineData( "https://se.church?x=1" )]     // query
        [InlineData( "javascript:alert(1)" )]
        public void Rejects_Invalid_Origins( string origin )
        {
            Assert.False( LinkListService.IsValidOrigin( origin ) );
        }
    }
}
