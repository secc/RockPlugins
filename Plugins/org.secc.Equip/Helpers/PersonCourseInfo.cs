using System;
using org.secc.Equip.Model;
using Rock.Lava;
using Rock.Model;

namespace org.secc.Equip.Helpers
{
    public class PersonCourseInfo : DotLiquid.Drop
    {
        [LavaVisible]
        public Course Course { get; set; }

        [LavaVisible]
        public bool IsComplete { get; set; }

        [LavaVisible]
        public DateTime? CompletedDateTime { get; set; }

        [LavaVisible]
        public DateTime? ValidUntil { get; set; }

        [LavaVisible]
        public Category Category { get; set; }

        [LavaVisible]
        public bool IsExpired { get; set; }
    }
}
