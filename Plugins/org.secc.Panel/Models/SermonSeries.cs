using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.Panel.Models
{
    class SermonSeries
    {
        public int id { get; set; }
        public string title { get; set; }
        public string slug { get; set; }
        public string image { get; set; }
        public string description { get; set; }
        public long lastupdated { get; set; }
        public bool deleted { get; set; }
        public List<Sermon> sermons { get; set; }

        public override string ToString()
        {
            return this.title;
        }
    }
}
