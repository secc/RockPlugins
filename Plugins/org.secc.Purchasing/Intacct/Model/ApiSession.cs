using RestSharp.Deserializers;

namespace org.secc.Purchasing.Intacct.Model
{
    [DeserializeAs( Name = "api" )]
    class ApiSession : IntacctModel
    {
        public override int Id
        {
            get { 
                return 0;
            }
        }

        public override string ApiId
        {
            get
            {
                return SessionId;
            }
        }

        [DeserializeAs( Name = "sessionid" )]
        public string SessionId { get; set; }

        [DeserializeAs( Name = "endpoint" )]
        public string EndPoint { get; set; }

        [DeserializeAs( Name = "locationid" )]
        public string LocationId { get; set; }
    }
}
