using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Web.UI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace RockWeb.Themes.MySecc.Layouts
{

public partial class MySeccMasterPage : Rock.Web.UI.RockMasterPage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        SiteName.InnerHtml = ((Rock.Web.UI.RockPage)Page).Layout.Site.Name;
    }
}

}