// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Model;

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
        Delete = 2,
        Attribute = 3
    }
}
