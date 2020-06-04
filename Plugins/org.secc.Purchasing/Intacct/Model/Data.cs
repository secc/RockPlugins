using RestSharp.Deserializers;

namespace org.secc.Purchasing.Intacct.Model
{
    public class Data
    {
        [DeserializeAs( Attribute = true, Name = "count" )]
        public int? Count { get; set; } = 0;

        [DeserializeAs( Attribute = true, Name = "listtype" )]
        public string ListType { get; set; } = "";

        [DeserializeAs( Attribute = true, Name = "totalcount" )]
        public int? TotalCount { get; set; } = 0;

        [DeserializeAs( Attribute = true, Name = "numremaining" )]
        public int? NumRemaining { get; set; } = 0;

        [DeserializeAs( Attribute = true, Name = "resultId" )]
        public string ResultId { get; set; } = "";

    }
}
