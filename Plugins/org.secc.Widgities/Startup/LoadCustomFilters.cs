using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotLiquid;
using org.secc.Widgities.Lava;

namespace org.secc.Widgities.Startup
{
    public partial class LoadCustomFilters : Rock.Utility.IRockOwinStartup
    {
        public int StartupOrder => 0;

        public void OnStartup( global::Owin.IAppBuilder app )
        {
            Template.RegisterFilter( typeof( CustomFilters ) );
        }
    }
}
