namespace org.secc.OAuth.Model
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Runtime.Serialization;
    [Table("_org_secc_OAuth_ClientScope")]
    public partial class Scope : Rock.Data.Model<Scope>, Rock.Security.ISecured
    {
        [StringLength(255)]
        [DataMember]
        public string Identifier { get; set; }
        
        [DataMember]
        public Guid Description { get; set; }

        [DataMember]
        public bool Active { get; set; }
        
    }
}
