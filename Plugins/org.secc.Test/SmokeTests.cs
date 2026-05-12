using FluentAssertions;
using Xunit;

namespace org.secc.Test
{
    public class SmokeTests
    {
        [Fact]
        public void TruthIsTrue()
        {
            true.Should().BeTrue();
        }
    }
}
