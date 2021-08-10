using RestSharp.Serializers;

namespace org.secc.Purchasing.Intacct.Functions
{
    public class ReadByQuery : IntacctFunction
    {
        public ReadByQuery()
        {

        }

        public ReadByQuery( string objectName )
        {
            Function.Object = objectName;
        }

        [SerializeAs( Name = "readByQuery" )]
        public ReadByQueryClass Function { get; set; } = new ReadByQueryClass();

        public class ReadByQueryClass
        {
            [SerializeAs( Name = "object" )]
            public string Object { get; set; }

            [SerializeAs( Name = "fields" )]
            public string Fields { get; set; } = "*";

            [SerializeAs( Name = "query" )]
            public string Query { get; set; } = "";

            [SerializeAs( Name = "pagesize" )]
            public int PageSize { get; set; } = 100;
        }
    }
}
