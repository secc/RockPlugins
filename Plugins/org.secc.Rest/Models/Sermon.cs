namespace org.secc.SermonFeed.Models
{
    class Sermon
    {
        public int id { get; set; }
        public string title { get; set; }
        public string slug { get; set; }
        public string description { get; set; }
        public string speaker { get; set; }
        public int duration { get; set; }
        public long vimeo_id { get; set; }
        public string audio_link { get; set; }
        public int auio_size { get; set; } // not a typo
        public string campus { get; set; } = "920";
        public long date { get; set; }
        public int deleted { get; set; } = 0;
        public string notes { get; set; }
        public string questions { get; set; }
        public string image { get; set; }
        public string image_url { get; set; }
    }
}
