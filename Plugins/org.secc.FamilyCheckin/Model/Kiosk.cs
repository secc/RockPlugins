namespace org.secc.FamilyCheckin.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.ModelConfiguration;
    using System.Runtime.Serialization;
    using Rock.Model;
    [Table( "_org_secc_FamilyCheckin_Kiosk" )]
    [DataContract]
    public partial class Kiosk : Rock.Data.Model<Kiosk>, Rock.Security.ISecured, Rock.Data.IRockEntity
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

        public virtual KioskType KioskType { get; set; }

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
