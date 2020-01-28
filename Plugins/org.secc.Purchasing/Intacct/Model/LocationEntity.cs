using System;
using RestSharp.Deserializers;

namespace org.secc.Purchasing.Intacct.Model
{
    [DeserializeAs( Name = "locationentity" )]
    public class LocationEntity : Location
    {

        [DeserializeAs( Name = "STARTOPEN" )]
        public DateTime StartOpen { get; set; }

        [DeserializeAs(Name = "LOCATIONENTITY.LOCATIONTYPE" )]
        public string LocationType { get; set; }

        [DeserializeAs( Name = "ENABLELEGALCONTACT" )]
        public bool EnableLegalContact { get; set; }

        [DeserializeAs( Name = "LEGALNAME" )]
        public string LegalName { get; set; }

        [DeserializeAs( Name = "LEGALADDRESS1" )]
        public string LegalAddress1 { get; set; }

        [DeserializeAs( Name = "LEGALADDRESS2" )]
        public string LegalAddress2 { get; set; }

        [DeserializeAs( Name = "LEGALCITY" )]
        public string LegalCity { get; set; }

        [DeserializeAs( Name = "LEGALSTATE" )]
        public string LegalState { get; set; }

        [DeserializeAs( Name = "LEGALZIPCODE" )]
        public string LegalZipCode { get; set; }

        [DeserializeAs( Name = "LEGALCOUNTRY" )]
        public string LegalCountry { get; set; }
    }
}
