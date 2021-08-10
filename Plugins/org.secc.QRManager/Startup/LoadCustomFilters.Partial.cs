using DotLiquid;
using org.secc.QRManager.Lava;

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
            Template.RegisterFilter( typeof( CustomFilters ) );
        }
    }
}
