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
