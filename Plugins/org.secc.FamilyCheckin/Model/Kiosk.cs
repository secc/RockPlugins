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
namespace org.secc.FamilyCheckin.Model
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.ModelConfiguration;
    using System.Runtime.Serialization;
    using Rock.Data;
    using Rock.Model;
    using Rock.Enums;
    [Table( "_org_secc_FamilyCheckin_Kiosk" )]
    [DataContract]
    public partial class Kiosk : Rock.Data.Model<Kiosk>, Rock.Security.ISecured, Rock.Data.IRockEntity, ICategorized
    {
        public override string ToString()
        {
            return this.Name;
        }

        [StringLength( 255 )]
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public int? KioskTypeId { get; set; }

        public virtual CheckinKioskType KioskType { get; set; }

        [MaxLength( 45 )]
        [DataMember]
        public string IPAddress { get; set; }

        [DataMember]
        public int? PrinterDeviceId { get; set; }

        public virtual Device PrinterDevice { get; set; }

        [DataMember]
        public PrintFrom PrintFrom { get; set; }

        [DataMember]
        public PrintTo PrintToOverride { get; set; }

        [DataMember]
        [MaxLength( 64 )]
        public string AccessKey { get; set; }

        [DataMember]
        public int? CategoryId { get; set; }

        public virtual Category Category { get; set; }
    }

    /// <summary>
    /// File Configuration class.
    /// </summary>
    public partial class KioskConfiguration : EntityTypeConfiguration<Kiosk>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KioskTypeConfiguration"/> class.
        /// </summary>
        public KioskConfiguration()
        {
            this.HasOptional( k => k.KioskType ).WithMany().HasForeignKey( k => k.KioskTypeId ).WillCascadeOnDelete( false );
            this.HasOptional( k => k.PrinterDevice ).WithMany().HasForeignKey( k => k.PrinterDeviceId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "Kiosks" );
        }
    }
}
