using System;

namespace org.secc.ServiceReef.Contracts
{
   
    public class Address
    {
        public String Address1 { get; set; }
        public String Address2 { get; set; }
        public String City { get; set; }
        public String State { get; set; }
        public String Zip { get; set; }
        public String Country { get; set; }
        public object Latitude { get; set; }
        public object Longitude { get; set; }
    }
}
