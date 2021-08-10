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
namespace org.secc.Microframe.Model
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.ModelConfiguration;
    using System.Runtime.Serialization;

    [Table( "_org_secc_Microframe_Sign" )]
    [DataContract]
    public partial class Sign : Rock.Data.Model<Sign>, Rock.Security.ISecured, Rock.Data.IRockEntity
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

        [MaxLength( 45 )]
        [DataMember]
        public string IPAddress { get; set; }

        [MaxLength( 8 )]
        [DataMember]
        public string Port { get; set; }

        [MaxLength( 4 )]
        [DataMember]
        public string PIN { get; set; }

        [DataMember]
        public virtual ICollection<SignCategory> SignCategories
        {
            get { return _signCategories ?? ( _signCategories = new Collection<SignCategory>() ); }
            set { _signCategories = value; }
        }
        private ICollection<SignCategory> _signCategories;
    }

    /// <summary>
    /// File Configuration class.
    /// </summary>
    public partial class SignConfiguration : EntityTypeConfiguration<Sign>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KioskTypeConfiguration"/> class.
        /// </summary>
        public SignConfiguration()
        {
            this.HasMany( s => s.SignCategories ).WithMany().Map( s => { s.MapLeftKey( "SignId" ); s.MapRightKey( "SignCategoryId" ); s.ToTable( "_org_secc_Microframe_SignSignCategory" ); } );
            this.HasEntitySetName( "Signs" );
        }
    }

}
