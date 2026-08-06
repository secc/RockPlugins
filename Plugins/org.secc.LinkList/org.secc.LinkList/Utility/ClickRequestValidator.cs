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
namespace org.secc.LinkList.Utility
{
    /// <summary>
    /// ROCK-8881: the pre-read body-size gate for the click beacon. Pure and
    /// dependency-free so the decision is unit-testable without an
    /// HttpContext. The controller applies this to
    /// <c>Request.Content.Headers.ContentLength</c> BEFORE calling
    /// <c>ReadAsStringAsync()</c>, so an oversized or unbounded body is never
    /// buffered into memory.
    /// </summary>
    public static class ClickRequestValidator
    {
        /// <summary>Outcome of the Content-Length check.</summary>
        public enum BodyLengthDecision
        {
            /// <summary>Within bounds - read and parse the body.</summary>
            Accept,

            /// <summary>No Content-Length header - reject with 411 Length Required.</summary>
            LengthRequired,

            /// <summary>Content-Length exceeds the cap - reject with 413 Payload Too Large.</summary>
            TooLarge,
        }

        /// <summary>
        /// Decides how to handle a click-beacon request given its declared
        /// <paramref name="contentLength"/> (null when absent). A missing
        /// length is rejected (<c>navigator.sendBeacon</c> always sets one, so
        /// its absence signals a hand-crafted request); a length over
        /// <see cref="ClickPayload.MaxBodyLength"/> is rejected before buffering.
        /// </summary>
        public static BodyLengthDecision CheckContentLength( long? contentLength )
        {
            if ( !contentLength.HasValue )
            {
                return BodyLengthDecision.LengthRequired;
            }

            if ( contentLength.Value > ClickPayload.MaxBodyLength )
            {
                return BodyLengthDecision.TooLarge;
            }

            return BodyLengthDecision.Accept;
        }
    }
}
