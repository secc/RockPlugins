using RestSharp.Deserializers;

namespace org.secc.Purchasing.Intacct.Api
{
    [DeserializeAs( Name = "response" )]
    public class Response
    {
        [DeserializeAs( Name = "control" )]
        public Control Control { get; set; }

        [DeserializeAs( Name = "operation" )]
        public Operation Operation { get; set; }

        [DeserializeAs( Name = "errormessage" )]
        public ErrorMesssage ErrorMessage { get; set; }
    }
}
