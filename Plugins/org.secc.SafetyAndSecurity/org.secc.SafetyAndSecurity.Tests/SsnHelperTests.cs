using Xunit;

namespace org.secc.SafetyAndSecurity.Tests
{
    /// <summary>
    /// Fake values only: 900-series numbers are never issued as SSNs.
    /// </summary>
    public class SsnHelperTests
    {
        private const string StoredSsn = "900-12-3456";

        [Theory]
        [InlineData( "900-12-3456" )]
        [InlineData( "900 12 3456" )]
        [InlineData( "900123456" )]
        public void IsValidSsnFormat_AcceptsNineDigits( string ssn )
        {
            Assert.True( SsnHelper.IsValidSsnFormat( ssn ) );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "--" )]
        [InlineData( "abc" )]
        [InlineData( "abc--" )]
        [InlineData( "900-12-345" )]
        [InlineData( "900-12-12a4" )]
        [InlineData( "***-**-3456" )]
        [InlineData( "123-45-6789" )]
        [InlineData( "111-11-1111" )]
        [InlineData( "９００-１２-３４５６" )] // full-width digits
        public void IsValidSsnFormat_RejectsInvalid( string ssn )
        {
            Assert.False( SsnHelper.IsValidSsnFormat( ssn ) );
        }

        [Fact]
        public void ResolveSsn_FreshEntry_ReturnsTypedValue()
        {
            Assert.Equal( "900-12-3456", SsnHelper.ResolveSsn( "900", "12", "3456", null ) );
        }

        [Fact]
        public void ResolveSsn_FreshEntryWithStoredSsn_ReturnsTypedValue()
        {
            Assert.Equal( "900-55-7777", SsnHelper.ResolveSsn( "900", "55", "7777", StoredSsn ) );
        }

        [Theory]
        [InlineData( "3456" )]
        [InlineData( " 3456 " )]
        public void ResolveSsn_UnchangedMask_ReturnsNull( string ssn3 )
        {
            Assert.Null( SsnHelper.ResolveSsn( "***", "**", ssn3, StoredSsn ) );
        }

        [Fact]
        public void ResolveSsn_CorrectedLastFour_RebuildsFromStoredFirstFive()
        {
            var ssn = SsnHelper.ResolveSsn( "***", "**", "3465", StoredSsn );

            Assert.Equal( "900-12-3465", ssn );
            Assert.True( SsnHelper.IsValidSsnFormat( ssn ) );
        }

        [Fact]
        public void ResolveSsn_InvalidLastFour_RebuildsAndIsInvalid()
        {
            var ssn = SsnHelper.ResolveSsn( "***", "**", "12a4", StoredSsn );

            Assert.NotNull( ssn );
            Assert.False( SsnHelper.IsValidSsnFormat( ssn ) );
        }

        [Fact]
        public void ResolveSsn_StoredWithoutDashes_StillResolves()
        {
            Assert.Null( SsnHelper.ResolveSsn( "***", "**", "3456", "900123456" ) );
            Assert.Equal( "900-12-3465", SsnHelper.ResolveSsn( "***", "**", "3465", "900 12 3456" ) );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "not-an-ssn" )]
        public void ResolveSsn_MaskWithoutUsableStoredSsn_IsInvalid( string storedSsn )
        {
            var ssn = SsnHelper.ResolveSsn( "***", "**", "3456", storedSsn );

            Assert.NotNull( ssn );
            Assert.False( SsnHelper.IsValidSsnFormat( ssn ) );
        }

        [Theory]
        [InlineData( "12" )]
        [InlineData( "" )]
        [InlineData( null )]
        public void ResolveSsn_FirstBoxMaskedSecondNot_IsInvalid( string ssn2 )
        {
            var ssn = SsnHelper.ResolveSsn( "***", ssn2, "3456", StoredSsn );

            Assert.NotNull( ssn );
            Assert.False( SsnHelper.IsValidSsnFormat( ssn ) );
        }

        [Fact]
        public void ResolveSsn_Blank_IsInvalid()
        {
            var ssn = SsnHelper.ResolveSsn( "", "", "", null );

            Assert.NotNull( ssn );
            Assert.False( SsnHelper.IsValidSsnFormat( ssn ) );
        }
    }
}
