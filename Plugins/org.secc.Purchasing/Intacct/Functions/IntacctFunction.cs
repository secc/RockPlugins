using System;
using RestSharp.Serializers;

namespace org.secc.Purchasing.Intacct.Functions
{
    public abstract class IntacctFunction
    {
        [SerializeAs( Name = "controlid", Attribute = true )]
        public Guid ControlId { get; set;} = Guid.NewGuid();
    }
}
