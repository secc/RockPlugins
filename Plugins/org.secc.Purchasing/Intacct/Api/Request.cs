using RestSharp.Serializers;

namespace org.secc.Purchasing.Intacct.Api
{
    [SerializeAs( Name = "request" )]
    public class Request
    {
        [SerializeAs( Name = "control" )]
        public Control Control { get; set; } = new Control();

        [SerializeAs( Name = "operation" )]
        public Operation Operation { get; set; } = new Operation();

    }
}
