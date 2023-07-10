using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.Communication.Messaging
{
    public class KeywordReorderItem
    {
        public Guid KeywordId { get; set; }
        public int OldIndex { get; set; }
        public int NewIndex { get; set; }
    }
}
