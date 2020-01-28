using System;
using RestSharp.Deserializers;
using RestSharp.Serializers;

namespace org.secc.Purchasing.Intacct.Api
{
    public class Control
    {
        [SerializeAs( Name = "senderid" )]
        [DeserializeAs( Name = "senderid" )]
        public string SenderId { get; set; }

        [SerializeAs( Name = "password" )]
        [DeserializeAs( Name = "password" )]
        public string Password { get; set; }

        [SerializeAs( Name = "controlid" )]
        [DeserializeAs( Name = "controlid" )]
        public long ControlId { get; set; } = DateTime.Now.Ticks;

        [SerializeAs( Name = "uniqueid" )]
        [DeserializeAs( Name = "uniqueid" )]
        public bool UniqueId { get; set; } = false;

        [SerializeAs( Name = "dtdversion" )]
        [DeserializeAs( Name = "dtdversion" )]
        public string DTDVersion { get; set; } = "3.0";

        [SerializeAs( Name = "includewhitespace" )]
        [DeserializeAs( Name = "includewhitespace" )]
        public bool IncludeWhitespace { get; set; } = false;
    }
}
