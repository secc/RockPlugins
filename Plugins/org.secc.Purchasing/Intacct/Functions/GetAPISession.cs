using RestSharp.Serializers;

namespace org.secc.Purchasing.Intacct.Functions
{
    public class GetAPISession : IntacctFunction
    {

        [SerializeAs( Name = "getAPISession" )]
        public string Function { get; set; } = "";

    }
}
