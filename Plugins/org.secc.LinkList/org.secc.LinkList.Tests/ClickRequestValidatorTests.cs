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
using org.secc.LinkList.Utility;

using Xunit;

using Decision = org.secc.LinkList.Utility.ClickRequestValidator.BodyLengthDecision;

namespace org.secc.LinkList.Tests
{
    /// <summary>
    /// ROCK-8881: the pre-read Content-Length gate for the click beacon.
    /// </summary>
    public class ClickRequestValidatorTests
    {
        [Fact]
        public void Null_Content_Length_Requires_Length()
        {
            Assert.Equal( Decision.LengthRequired, ClickRequestValidator.CheckContentLength( null ) );
        }

        [Theory]
        [InlineData( 0 )]
        [InlineData( 1 )]
        [InlineData( 100 )]
        [InlineData( ClickPayload.MaxBodyLength - 1 )]
        [InlineData( ClickPayload.MaxBodyLength )] // exactly at the cap is accepted
        public void Within_Cap_Is_Accepted( long contentLength )
        {
            Assert.Equal( Decision.Accept, ClickRequestValidator.CheckContentLength( contentLength ) );
        }

        [Theory]
        [InlineData( ClickPayload.MaxBodyLength + 1 )] // one over the cap is rejected
        [InlineData( 4096 )]
        [InlineData( 10 * 1024 * 1024 )]
        [InlineData( long.MaxValue )]
        public void Over_Cap_Is_Too_Large( long contentLength )
        {
            Assert.Equal( Decision.TooLarge, ClickRequestValidator.CheckContentLength( contentLength ) );
        }
    }
}
