using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.Trak1.Helpers
{
    public class Trak1Package
    {
        public string PackageServiceNumber { get; set; }
        public string PackageName { get; set; }
        public string PackagePrice { get; set; }
        public string PackageDescription { get; set; }
        public string TierOrder { get; set; }
        public List<Trak1Component> Components { get; set; }
    }
}
