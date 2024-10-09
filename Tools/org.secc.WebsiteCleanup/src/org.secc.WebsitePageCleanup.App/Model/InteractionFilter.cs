using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.WebsitePageCleanup.App.Model
{
    public record InteractionFilter
    {
        public int ChannelId { get; set; }
        public int PageId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }


    }
}
