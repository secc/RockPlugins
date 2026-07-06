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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;

namespace org.secc.SmsCapture.Model
{
    [Table( "_org_secc_SmsCapture_CapturedSms" )]
    [DataContract]
    public partial class CapturedSms : Rock.Data.Model<CapturedSms>, Rock.Security.ISecured, Rock.Data.IRockEntity
    {
        [DataMember]
        [MaxLength( 20 )]
        public string FromNumber { get; set; }

        [DataMember]
        [Index]
        [MaxLength( 20 )]
        public string ToNumber { get; set; }

        [DataMember]
        public int? RecipientPersonAliasId { get; set; }

        [DataMember]
        public string Body { get; set; }

        /// <summary>
        /// Comma-delimited BinaryFile ids of the SMS attachments (references only; media is not downloaded).
        /// </summary>
        [DataMember]
        public string AttachmentBinaryFileIds { get; set; }

        /// <summary>
        /// Id of the Rock Communication this message came from, when applicable.
        /// Stored without a foreign key so communication cleanup jobs can delete
        /// old communications without being blocked by capture rows.
        /// </summary>
        [DataMember]
        public int? CommunicationId { get; set; }

        [DataMember]
        public int? CommunicationRecipientId { get; set; }

        /// <summary>
        /// Which transport Send() overload produced the capture, e.g. "Communication" or "RockMessage".
        /// </summary>
        [DataMember]
        [MaxLength( 100 )]
        public string Source { get; set; }

        public virtual Rock.Model.PersonAlias RecipientPersonAlias { get; set; }
    }

    public partial class CapturedSmsConfiguration : EntityTypeConfiguration<CapturedSms>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CapturedSmsConfiguration"/> class.
        /// </summary>
        public CapturedSmsConfiguration()
        {
            this.HasOptional( c => c.RecipientPersonAlias ).WithMany().HasForeignKey( c => c.RecipientPersonAliasId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "CapturedSmsMessages" );
        }
    }
}
