using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.Trak1.Helpers
{
    public class ErrorInformation
    {
        public Guid IncidentId { get; set; }
        public string Message { get; set; }
        public ErrorInformation Error { get; set; }
    }
}
