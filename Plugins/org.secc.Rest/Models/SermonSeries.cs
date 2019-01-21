using System.Collections.Generic;

namespace org.secc.SermonFeed.Models
{
    class SermonSeries
    {
        public int id { get; set; }
        public string title { get; set; }
        public string slug { get; set; }
        public string image { get; set; }
        public string image_url { get; set; }
        public string description { get; set; }
        public long last_updated { get; set; }
        public int deleted { get; set; } = 0;
        public List<Sermon> sermons { get; set; }
    }
}
