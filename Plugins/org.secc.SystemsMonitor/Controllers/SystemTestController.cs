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
using System.Web.Http;
using org.secc.SystemsMonitor.Model;
using Rock.Data;
using Rock.Rest;
using Rock.Rest.Filters;

namespace org.secc.SystemsMonitor.Controllers
{
    public partial class SystemTestController : ApiControllerBase
    {

        [Authenticate]
        [HttpGet]
        [System.Web.Http.Route( "api/systemtest/runtest/{id}" )]
        public IHttpActionResult RunTest( int id )
        {
            RockContext rockContext = new RockContext();
            SystemTestService systemTestService = new SystemTestService( rockContext );
            var test = systemTestService.Get( id );
            var result = test.Run();
            if ( result.Passed )
            {
                return Ok( "Passed" );
            }
            //if we fail throw an exception for a 500 status
            return InternalServerError( new Exception( "System Test Failed" ) );
        }

    }
}
