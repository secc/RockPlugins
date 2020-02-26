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
//
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Rock.Data;
using Rock.Model;

namespace org.secc.Reporting.Model
{
    [Table( "_org_secc_Reporting_DataViewSQLFilterStore" )]
    [DataContract]
    [HideFromReporting]
    public partial class DataViewSQLFilterStore : Model<DataViewSQLFilterStore>, IRockEntity
    {
        [NotMapped]
        public new int Id { get; set; }

        [NotMapped]
        public new Guid Guid { get; set; }
        [NotMapped]
        public new int? ForeignId { get; set; }

        [NotMapped]
        public new Guid? ForeignGuid { get; set; }

        [NotMapped]
        public new string ForeignKey { get; set; }


        [NotMapped]
        public new DateTime? CreatedDateTime { get; set; }


        [NotMapped]
        public new DateTime? ModifiedDateTime { get; set; }

        [NotMapped]
        public new int? CreatedByPersonAliasId { get; set; }

        [NotMapped]
        public new int? ModifiedByPersonAliasId { get; set; }

        /// <summary>
        /// Gets or sets the sql hash.
        /// </summary>
        /// <value>
        /// The sql hash.
        /// </value>
        [DataMember]
        [Key]
        [Index]
        [DatabaseGenerated( DatabaseGeneratedOption.None )]
        [MaxLength( 64 )]
        public string Hash { get; set; }


        /// <summary>
        /// Gets or sets the entity identifier.
        /// </summary>
        /// <value>
        /// The entity identifier.
        /// </value>
        [DataMember]
        [Key]
        [DatabaseGenerated( DatabaseGeneratedOption.None )]
        public int EntityId { get; set; }
    }

    public partial class SQLFilterStoreConfiguration : EntityTypeConfiguration<DataViewSQLFilterStore>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttendanceConfiguration"/> class.
        /// </summary>
        public SQLFilterStoreConfiguration()
        {
            HasKey( s => new { s.Hash, s.EntityId } );
            HasEntitySetName( "DataViewSQLFilterStore" );
        }
    }
}
