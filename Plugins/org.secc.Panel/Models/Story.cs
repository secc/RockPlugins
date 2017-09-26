using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.Panel.Models
{
    class Story
    {
        public int id { get; set; }
        public string title { get; set; }
        public string slug { get; set; }
        public string description { get; set; }
        public int duration { get; set; }
        public long vimeo_id { get; set; }
        public string vimeo_sd_url { get; set; }
        public string vimeo_live_url { get; set; }
        public string tags { get; set; }
        public long datecreated { get; set; }
    }
}
