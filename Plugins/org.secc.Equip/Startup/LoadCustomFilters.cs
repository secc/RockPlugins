using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotLiquid;
using Rock.Utility;
using org.secc.Equip.Lava;

namespace org.secc.Equip.Startup
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
