using org.secc.Purchasing.Intacct.Functions;
using RestSharp.Serializers;

namespace org.secc.Purchasing.Intacct.Api
{

    public class Content
    {

        [SerializeAs( Name = "function" )]
        public IntacctFunction Function { get; set; }

    }
}
