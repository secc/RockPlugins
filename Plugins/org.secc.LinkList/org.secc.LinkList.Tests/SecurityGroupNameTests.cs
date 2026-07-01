using org.secc.LinkList.Services;

using Xunit;

namespace org.secc.LinkList.Tests
{
    public class SecurityGroupNameTests
    {
        [Fact]
        public void Truncates_Long_Title_To_Column_Length()
        {
            var name = LinkListService.SecurityGroupName( new string( 'x', 200 ) );
            Assert.Equal( 100, name.Length );
            Assert.StartsWith( "RSR - Link List - ", name );
        }

        [Fact]
        public void Leaves_Short_Title_Untruncated()
        {
            Assert.Equal( "RSR - Link List - My List", LinkListService.SecurityGroupName( "My List" ) );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "   " )]
        public void Uses_Untitled_For_Null_Or_Blank( string title )
        {
            Assert.Equal( "RSR - Link List - Untitled", LinkListService.SecurityGroupName( title ) );
        }

        [Fact]
        public void Trims_Title()
        {
            Assert.Equal( "RSR - Link List - My List", LinkListService.SecurityGroupName( "  My List  " ) );
        }
    }
}
