using RestSharp.Deserializers;

namespace org.secc.Purchasing.Intacct.Api
{

    [DeserializeAs( Name = "error" )]
    public class Error
    {
        [DeserializeAs( Name = "errorno" )]
        public string ErrorNo { get; set; }


        [DeserializeAs( Name = "description" )]
        public string Description { get; set; }


        [DeserializeAs( Name = "description2" )]
        public string Description2 { get; set; }


        [DeserializeAs( Name = "correction" )]
        public string Correction { get; set; }
    }

    public class ErrorMesssage
    {
        public Error Error { get; set; }
    }
}
