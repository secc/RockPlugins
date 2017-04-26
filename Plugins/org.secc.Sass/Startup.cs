using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Utility;
using Rock;
using Rock.Model;
using Rock.Web.UI;

namespace org.secc.Sass
{
    class Startup : IRockStartup
    {
        public int StartupOrder { get { return 0; } }

        public void OnStartup()
        {
            foreach ( var theme in RockTheme.GetThemes() )
            {
                string themeMessage = string.Empty;
                bool themeSuccess = theme.CompileSass( out themeMessage );
            }
        }
    }
}
