using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using DotLiquid.Util;
using org.secc.SystemsMonitor.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;
using Rock.Rest.Filters;
using Rock.Web.Cache;

namespace org.secc.SystemsMonitor.Controllers
{
    public partial class SystemTestController : ApiControllerBase
    {

        [Authenticate]
        [HttpGet]
        [System.Web.Http.Route( "api/systemtest/runtest/{id}" )]
        public string RunTest( int id )
        {
            RockContext rockContext = new RockContext();
            SystemTestService systemTestService = new SystemTestService( rockContext );
            var test = systemTestService.Get( id );
            var result = test.Run();
            if ( result.Passed )
            {
                return "Passed";
            }
            //if we fail throw an exception for a 500 status
            throw new Exception( "Failed" );
        }

    }
}
