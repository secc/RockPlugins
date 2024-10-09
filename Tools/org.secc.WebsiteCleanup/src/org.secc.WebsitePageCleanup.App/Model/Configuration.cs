using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.WebsitePageCleanup.App.Model;

public class Configuration
{
    public int SiteId { get; set; }
    public int MinimumInteractionCount { get; set; }
    public int InteractionTimeframeMonths { get; set; }
    public Dictionary<string, string> ConnectionStrings { get; set; } = new Dictionary<string, string>();
}
