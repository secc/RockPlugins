using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.Rest.Models
{
    public class ChannelItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public List<string> Tags { get; set; }
        public DateTime DateTime { get; set; }
        public List<int> ChildItems { get; set; }
        public List<int> ParentItems { get; set; }
        public int Order { get; set; }
        public int Priority { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
        public string Slug { get; set; }
        public string CreatedBy { get; set; }
    }
}
