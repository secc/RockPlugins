using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.Panel.Models
{
    class Sermon
    {
        public int id { get; set; }
        public string title { get; set; }
        public string slug { get; set; }
        public string description { get; set; }
        public string speaker { get; set; }
        public int duration { get; set; }
        public int kitclub_id { get; set; }
        public long vimeo_id { get; set; }
        public string vimeo_sd_url { get; set; }
        public string vimeo_live_url { get; set; }
        public string audio_link { get; set; }
        public string audio_size { get; set; }
        public int series { get; set; }
        public int campus { get; set; }
        public int podcast_views { get; set; }
        public long date { get; set; }
        public int notes_downloaded { get; set; }
        public int questions_downloaded { get; set; }
        public bool deleted { get; set; }
        public override string ToString()
        {
            return this.title;
        }
    }
}
