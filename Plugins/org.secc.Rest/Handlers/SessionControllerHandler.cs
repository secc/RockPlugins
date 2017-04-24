using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.WebHost;
using System.Web.Routing;
using System.Web.SessionState;

namespace org.secc.Rest.Handlers
{
    public class SessionControllerHandler : HttpControllerHandler, IRequiresSessionState
    {
            public SessionControllerHandler(RouteData routeData)
                : base(routeData)
            {}
    }
}
