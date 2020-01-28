using System;
using RestSharp.Deserializers;
using RestSharp.Serializers;

namespace org.secc.Purchasing.Intacct.Auth
{
    public class Authentication
    {
        [SerializeAs( Name = "sessionid" )]
        public string SessionId { get; set; }

        [SerializeAs( Name = "login" )]
        public Login Login { get; set; }

        [DeserializeAs( Name = "sessiontimeout" )]
        public DateTime? SessionTimeout { get; set; }

        [DeserializeAs( Name = "sessiontimestamp" )]
        public DateTime? SessionTimestamp { get; set; }

    }
}
