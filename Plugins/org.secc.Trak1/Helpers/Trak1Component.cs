using System.Collections.Generic;

namespace org.secc.Trak1.Helpers
{
    public class Trak1Component
    {
        public string ComponentName { get; set; }
        public string ComponentDescription { get; set; }
        public List<Trak1RequiredField> RequiredFields { get; set; }
    }
}