using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Web.Cache;

namespace org.secc.Communication
{
    public class SECCMessagingSettings
    {
        public string MessagingUrl { get; set; }
        public string MessagingKey { get; set; }
        public DefinedTypeCache RockSMSNumbersDefinedType { get; set; }
        public List<Guid> ApproverGroupGuids { get; set; }

    }
}
