using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Rock.Data;
using Rock.Mobile;

namespace org.secc.SystemsMonitor.Model
{
    [Table( "_org_secc_SystemsMonitor_SystemTestHistory" )]
    [DataContract]
    public class SystemTestHistory : Model<SystemTestHistory>, IRockEntity
    {
        [DataMember]
        public int SystemTestId { get; set; }

        [LavaInclude]
        public virtual SystemTest SystemTest { get; set; }

        [DataMember]
        public int Score { get; set; }

        [DataMember]
        public bool Passed { get; set; }

        [DataMember]
        public string Message { get; set; }
    }

    public partial class SytemTestHistoryConfiguration : EntityTypeConfiguration<SystemTestHistory>
    {
        public SytemTestHistoryConfiguration()
        {
            this.HasRequired<SystemTest>( t => t.SystemTest ).WithMany().WillCascadeOnDelete( false );
            this.HasEntitySetName( "SystemTestHistory" );
        }
    }
}
