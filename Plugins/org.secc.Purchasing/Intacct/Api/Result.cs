using System;
using org.secc.Purchasing.Intacct.Model;
using RestSharp.Deserializers;

namespace org.secc.Purchasing.Intacct.Api
{
    [DeserializeAs( Name = "result" )]
    public class Result
    {
        [DeserializeAs( Name = "status" )]
        public string Status { get; set; }

        [DeserializeAs( Name = "function" )]
        public string Function { get; set; }

        [DeserializeAs( Name = "controlid" )]
        public Guid? ControlId { get; set; }

        [DeserializeAs( Name = "data" )]
        public Data Data { get; set; } = new Data();

    }
}
