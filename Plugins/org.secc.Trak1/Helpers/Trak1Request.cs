using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.Trak1.Helpers
{
    class Trak1Request
    {
        public Trak1Authentication Authentication { get; set; }
        public Trak1Applicant Applicant { get; set; }
        public string PackageName { get; set; }
    }
}
