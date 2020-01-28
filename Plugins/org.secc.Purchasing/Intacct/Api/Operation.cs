using org.secc.Purchasing.Intacct.Auth;
using RestSharp.Deserializers;
using RestSharp.Serializers;

namespace org.secc.Purchasing.Intacct.Api
{
    public class Operation
    {
        [SerializeAs( Name = "authentication" )]
        [DeserializeAs( Name = "authentication" )]
        public Authentication Authentication { get; set; } = new Authentication();

        [SerializeAs( Name = "content" )]
        public Content Content { get; set; } = new Content();

        [SerializeAs( Name = "result" )]
        [DeserializeAs( Name = "result" )]
        public Result Result { get; set; }
    }
}
