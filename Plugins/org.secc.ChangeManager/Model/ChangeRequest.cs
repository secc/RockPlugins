using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Rock.Model;

namespace org.secc.ChangeManager.Model
{
    [Table( "_org_secc_ChangeManager_ChangeRequest" )]
    [DataContract]
    public class ChangeRequest : Rock.Data.Model<ChangeRequest>, Rock.Security.ISecured, Rock.Data.IRockEntity
    {
        [Index]
        [DataMember]
        public int EntityTypeId { get; set; }

        public virtual EntityType EntityType { get; set; }

        [Index]
        [DataMember]
        public int EntityId { get; set; }

        [Index]
        [DataMember]
        public int RequestorAliasId { get; set; }

        [Index]
        [DataMember]
        public int ApproverAliasId { get; set; }

        [Index]
        [DataMember]
        public bool IsComplete { get; set; }

        public virtual ICollection<ChangeRecord> ChangeRecords
        {
            get { return _changeRecords ?? ( _changeRecords = new Collection<ChangeRecord>() ); }
            set { _changeRecords = value; }
        }
        private ICollection<ChangeRecord> _changeRecords;
    }
    public partial class ChangeRequestConfiguration : EntityTypeConfiguration<ChangeRequest>
    {
        public ChangeRequestConfiguration()
        {
            this.HasRequired<EntityType>( s => s.EntityType ).WithMany().WillCascadeOnDelete( false );
            this.HasEntitySetName( "ChangeRequest" );
        }
    }
}
