using RestSharp.Serializers;

namespace org.secc.Purchasing.Intacct.Functions
{
    public class ReadMore : IntacctFunction
    {
        public ReadMore()
        {
        }

        public ReadMore( string resultId )
        {
            Function.ResultId = resultId;
        }

        [SerializeAs( Name = "readMore" )]
        public ReadMoreClass Function { get; set; } = new ReadMoreClass();

        public class ReadMoreClass
        {
            [SerializeAs( Name = "resultId" )]
            public string ResultId { get; set; }

        }
    }
}
