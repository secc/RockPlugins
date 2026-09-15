using DotLiquid;
using org.secc.QRManager.Lava;
using Rock.Lava;

namespace org.secc.QRManger.Startup
{
    public partial class LoadCustomFilters : Rock.Utility.IRockOwinStartup
    {
        public int StartupOrder
        {
            get
            {
                return 0;
            }
        }
        public void OnStartup( global::Owin.IAppBuilder app )
        {
            // Register for both engines during the DotLiquid -> Fluid transition.
            Template.RegisterFilter( typeof( CustomFilters ) );        // legacy DotLiquid (RockLiquid)
            LavaService.RegisterFilters( typeof( CustomFilters ) );    // Fluid / Lava library
        }
    }
}
