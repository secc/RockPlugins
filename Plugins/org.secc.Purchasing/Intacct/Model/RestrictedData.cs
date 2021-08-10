using System;
using System.Collections.Generic;
using System.Linq;
using RestSharp.Deserializers;

namespace org.secc.Purchasing.Intacct.Model
{
    [DeserializeAs( Name = "RestrictedData" )]
    public class RestrictedData
    {
        [DeserializeAs( Name = "dimension" )]
        public string Dimension { get; set; }

        public List<Value> Values { get; set; } = new List<Value>();

        public List<int> IdValues
        {
            get
            {
                return Values
                    .Select( s => Int32.TryParse( new string( s.ToString().Where( c => ( Char.IsDigit( c ) || c == '.' || c == ',' ) ).ToArray() ), out int n ) ? n : ( int? ) null )
                    .Where( n => n.HasValue )
                    .Select( n => n.Value )
                    .ToList();
            }
        }
    }

    public class Value
    {
        [DeserializeAs( Name = "Value" )]
        public string Content { get; set; }

        public override string ToString()
        {
            return Content;
        }
    }
}
