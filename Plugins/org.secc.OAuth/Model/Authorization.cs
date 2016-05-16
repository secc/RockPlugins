namespace org.secc.OAuth.Model
{
    using Rock.Model;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Runtime.Serialization;
    [Table("_org_secc_OAuth_Authorization")]
    public partial class Authorization : Rock.Data.Model<Authorization>, Rock.Security.ISecured
    {
        [DataMember]
        public int ClientId { get; set; }

        public Client Client { get; set; }

        [DataMember]
        public int ScopeId { get; set; }

        public Scope Scope { get; set; }

        [DataMember]
        public int UserLoginId { get; set; }

        public UserLogin UserLogin { get; set; }

        [DataMember]
        public bool Active { get; set; }

    }
}
