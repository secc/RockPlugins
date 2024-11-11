using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.Reporting.Model
{
    [Table("_org_secc_DecisionForm_Analytics", Schema = "dbo")]
    public  class DecisionReport
    {
        [Key]
        [DataMember]
        public int WorkflowId { get; set; }
        [DataMember]
        public int PesonAliasId { get; set; }
        [DataMember]
        public int PersonId { get; set; }
        [DataMember]
        [MaxLength(50)]
        public string LastName { get; set; }
        [DataMember]
        [MaxLength(50)]
        public string NickName { get; set; }
        [DataMember]
        public int? Age { get; set; }
        [DataMember]
        [MaxLength(1)]
        public string Gender { get; set; }
    }
}
