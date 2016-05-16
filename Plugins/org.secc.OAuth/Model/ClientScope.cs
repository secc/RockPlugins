namespace org.secc.OAuth.Model
{
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Runtime.Serialization;
    [Table("_org_secc_OAuth_ClientScope")]
    public partial class ClientScope : Rock.Data.Model<Client>, Rock.Security.ISecured
    {
        [DataMember]
        public int ClientId { get; set; }

        public Client Client { get; set; }

        [DataMember]
        public int ScopeId { get; set; }

        public Scope Scope { get; set; }
        
        [DataMember]
        public bool Active { get; set; }
        
    }
}
