using System;
using RestSharp.Deserializers;

namespace org.secc.Purchasing.Intacct.Model
{
    public abstract class IntacctModel
    {

        /// <summary>
        /// Get the ID for this item
        /// </summary>
        public abstract int Id { get; }

        /// <summary>
        /// Get the API Id for this item
        /// </summary>
        public abstract string ApiId { get; }

        [DeserializeAs( Name = "RECORDNO" )]
        public int RecordNo { get; set; }

    }
}
