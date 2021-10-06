using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.secc.Equip.Model;
using Rock.Data;
using Rock.Model;

namespace org.secc.Equip.Helpers
{
    public class PersonCourseInfo : DotLiquid.Drop
    {
        [LavaInclude]
        public Course Course { get; set; }

        [LavaInclude]
        public bool IsComplete { get; set; }

        [LavaInclude]
        public DateTime? CompletedDateTime { get; set; }

        [LavaInclude]
        public DateTime? ValidUntil { get; set; }

        [LavaInclude]
        public Category Category { get; set; }

        [LavaInclude]
        public bool IsExpired { get; set; }
    }
}
