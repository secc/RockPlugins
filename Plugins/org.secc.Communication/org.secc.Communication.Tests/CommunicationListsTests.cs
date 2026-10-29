using Xunit;

namespace org.secc.Communication.Tests
{
    /// <summary>
    /// Tests KeywordMatches only — WhereVisible reads GroupTypeCache, which needs a Rock
    /// database context.
    /// </summary>
    public class CommunicationListsTests
    {
        [Theory]
        [InlineData( "VBS" )]
        [InlineData( "KIDS|VBS|MEN" )]
        [InlineData( "VBS|KIDS" )]
        [InlineData( "KIDS|VBS" )]
        public void KeywordMatches_WholeKeyword( string value )
        {
            Assert.True( CommunicationLists.KeywordMatches( value, "VBS" ) );
        }

        [Theory]
        [InlineData( "KIDS|VBSX" )]
        [InlineData( "XVBS|MEN" )]
        [InlineData( "VBS2026" )]
        [InlineData( "KIDS|MEN" )]
        public void KeywordMatches_RejectsPartialKeyword( string value )
        {
            Assert.False( CommunicationLists.KeywordMatches( value, "VBS" ) );
        }

        [Fact]
        public void KeywordMatches_IgnoresCaseAndWhitespace()
        {
            Assert.True( CommunicationLists.KeywordMatches( "kids| vbs |men", "VBS" ) );
            Assert.True( CommunicationLists.KeywordMatches( "VBS", " vbs " ) );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "   " )]
        public void KeywordMatches_EmptyValue( string value )
        {
            Assert.False( CommunicationLists.KeywordMatches( value, "VBS" ) );
            Assert.False( CommunicationLists.KeywordMatches( "VBS", value ) );
        }
    }
}
