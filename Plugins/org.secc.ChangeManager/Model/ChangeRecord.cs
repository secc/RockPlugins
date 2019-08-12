using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.ChangeManager.Model
{
    [Table( "_org_secc_ChangeManager_ChangeRecord" )]
    [DataContract]
    public class ChangeRecord : Rock.Data.Model<ChangeRecord>, Rock.Security.ISecured, Rock.Data.IRockEntity
    {
        #region Properties

        [Index]
        [DataMember]
        public int ChangeRequestId { get; set; }

        public virtual ChangeRequest ChangeRequest { get; set; }

        [Index]
        [DataMember]
        public bool IsRejected { get; set; }

        [Index]
        [DataMember]
        public bool WasApplied { get; set; }

        [Index]
        [DataMember]
        public int? RelatedEntityTypeId { get; set; }

        public virtual EntityType RelatedEntityType { get; set; }

        [Index]
        [DataMember]
        public int? RelatedEntityId { get; set; }

        [DataMember]
        public string OldValue { get; set; }

        [DataMember]
        public string NewValue { get; set; }

        [DataMember]
        public ChangeRecordAction Action { get; set; }

        [DataMember]
        public string Property { get; set; }

        [DataMember]
        public string Comment { get; set; }

        #endregion
    }

    public partial class ChangeRecordConfiguration : EntityTypeConfiguration<ChangeRecord>
    {
        public ChangeRecordConfiguration()
        {
            this.HasOptional<EntityType>( s => s.RelatedEntityType ).WithMany().WillCascadeOnDelete( false );
            this.HasEntitySetName( "ChangeRecord" );
        }
    }

    public enum ChangeRecordAction
    {
        Update = 0,
        Create = 1,
        Delete = 2
    }
}
