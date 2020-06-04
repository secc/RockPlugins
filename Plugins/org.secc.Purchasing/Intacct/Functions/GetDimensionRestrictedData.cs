using RestSharp.Serializers;

namespace org.secc.Purchasing.Intacct.Functions
{

    public class GetDimensionRestrictedData : IntacctFunction
    {

        [SerializeAs( Name = "getDimensionRestrictedData" )]
        public GetDimensionRestrictedDataClass Function { get; set; } = new GetDimensionRestrictedDataClass();
    }

    public class GetDimensionRestrictedDataClass
    {

        [SerializeAs( Name = "DimensionValue" )]
        public DimensionValue DimensionValue { get; set; } = new DimensionValue();
    }

    [SerializeAs( Name = "DimensionValue" )]
    public class DimensionValue
    {
        [SerializeAs( Name = "dimension" )]
        public string Dimension { get; set; }
        [SerializeAs( Name = "value" )]
        public string Value { get; set; }
    }

}
