namespace org.secc.Microframe.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.ModelConfiguration;
    using System.Runtime.Serialization;
    using Rock.Model;

    [Table( "_org_secc_Microframe_SignCategory" )]
    [DataContract]
    public partial class SignCategory : Rock.Data.Model<SignCategory>, Rock.Security.ISecured, Rock.Data.IRockEntity
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
        public string Codes { get; set; }
    }

    /// <summary>
    /// File Configuration class.
    /// </summary>
    public partial class SignCategoryConfiguration : EntityTypeConfiguration<SignCategory>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KioskTypeConfiguration"/> class.
        /// </summary>
        public SignCategoryConfiguration()
        {
            this.HasEntitySetName( "SignCategories" );
        }
    }

}
