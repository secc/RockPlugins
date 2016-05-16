namespace org.secc.OAuth.Model
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Runtime.Serialization;
    [Table("_org_secc_OAuth_Client")]
    public partial class Client : Rock.Data.Model<Client>, Rock.Security.ISecured
    {
        [StringLength(255)]
        [DataMember]
        public string ClientName { get; set; }
        
        [DataMember]
        public Guid ApiKey { get; set; }

        [DataMember]
        public Guid ApiSecret { get; set; }
        
        [DataMember]
        public string CallbackUrl { get; set; }

        [DataMember]
        public bool Active { get; set; }
        
    }
}
