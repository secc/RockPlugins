// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using org.secc.Rise.Model;
using org.secc.Rise.Response.Event;
using Rock.Rest;

namespace org.secc.Rise.Controllers
{
    public partial class RiseController : ApiControllerBase
    {
        /// <summary>
        ///  /// Webhook for when an course is completed
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Could not verify Rise LMS Request</exception>
        [HttpPost]
        [Route( "api/lms/rise/course/completed" )]
        public IHttpActionResult CourseCompleted( [NakedBody] string content )
        {
            if ( !VerifyRequest( content, Request ) )
            {
                throw new Exception( "Could not verify Rise LMS Request" );
            }

            if ( WebhookEventService.TryReserveWebhookEvent( content ) )
            {
                var webhookEvent = JsonConvert.DeserializeObject<CourseCompletedEvent>( content );
                webhookEvent.Sync();
            }

            return StatusCode( System.Net.HttpStatusCode.NoContent );
        }

        /// <summary>
        /// Webhook for when an course is submitted
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Could not verify Rise LMS Request</exception>
        [HttpPost]
        [Route( "api/lms/rise/course/submitted" )]
        public IHttpActionResult CourseSubmitted( [NakedBody] string content )
        {
            if ( !VerifyRequest( content, Request ) )
            {
                throw new Exception( "Could not verify Rise LMS Request" );
            }

            if ( WebhookEventService.TryReserveWebhookEvent( content ) )
            {
                var webhookEvent = JsonConvert.DeserializeObject<CourseSubmittedEvent>( content );
                webhookEvent.Sync();
            }

            return StatusCode( System.Net.HttpStatusCode.NoContent );
        }

        /// <summary>
        /// Webhook for when an enrollment is created
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Could not verify Rise LMS Request</exception>
        [HttpPost]
        [Route( "api/lms/rise/enrollments/created" )]
        public IHttpActionResult EnrollmentCreated( [NakedBody] string content )
        {
            if ( !VerifyRequest( content, Request ) )
            {
                throw new Exception( "Could not verify Rise LMS Request" );
            }

            if ( WebhookEventService.TryReserveWebhookEvent( content ) )
            {
                var webhookEvent = JsonConvert.DeserializeObject<EnrollmentsCreatedEvent>( content );
                webhookEvent.Sync();
            }

            return StatusCode( System.Net.HttpStatusCode.NoContent );
        }

        /// <summary>
        /// Webhook for when a user is created
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Could not verify Rise LMS Request</exception>
        [HttpPost]
        [Route( "api/lms/rise/user/created" )]
        public IHttpActionResult UserCreated( [NakedBody] string content )
        {
            if ( !VerifyRequest( content, Request ) )
            {
                throw new Exception( "Could not verify Rise LMS Request" );
            }

            if ( WebhookEventService.TryReserveWebhookEvent( content ) )
            {
                var webhookEvent = JsonConvert.DeserializeObject<UserCreatedEvent>( content );
                webhookEvent.Sync();
            }

            return StatusCode( System.Net.HttpStatusCode.NoContent );
        }

        /// <summary>
        /// Verifies the request.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        private bool VerifyRequest( string content, HttpRequestMessage request )
        {
            var hmac = HMAC.Create( "HMACSHA1" );
            hmac.Key = Encoding.ASCII.GetBytes( ClientManager.SharedSecret );
            var hash = hmac.ComputeHash( Encoding.ASCII.GetBytes( content ) );

            StringBuilder hex = new StringBuilder( hash.Length * 2 );
            foreach ( byte b in hash )
            {
                hex.AppendFormat( "{0:x2}", b );
            }

            var signature = hex.ToString();
            var hookSignature = request.GetHeader( "x-hook-signature" );
            return signature == hookSignature;
        }
    }
}
