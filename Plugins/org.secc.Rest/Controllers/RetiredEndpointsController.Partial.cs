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
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace org.secc.Rest.Controllers
{
    /// <summary>
    /// Stubs for removed endpoints whose URLs would otherwise fall through to a
    /// Rock core route with different behavior.
    /// </summary>
    public class RetiredEndpointsController : Rock.Rest.ApiControllerBase
    {
        /// <summary>
        /// Removed in ROCK-8827. Without this stub the URL matches Rock's DefaultApiPost
        /// route (api/{controller}/{id}) and reaches core PeopleController.Post, which
        /// ignores the old { person, location } body and creates a blank person and family
        /// on every call. Always returns 410 Gone so stale callers fail loudly.
        /// </summary>
        [System.Web.Http.Route( "api/People/MatchOrCreatePerson" )]
        [HttpPost]
        public HttpResponseMessage MatchOrCreatePerson()
        {
            return Request.CreateErrorResponse( HttpStatusCode.Gone, "api/People/MatchOrCreatePerson has been removed." );
        }
    }
}
