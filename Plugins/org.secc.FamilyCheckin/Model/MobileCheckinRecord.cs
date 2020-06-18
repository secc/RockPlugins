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
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.ModelConfiguration;
    using System.Runtime.Serialization;
    using Rock.Data;
    using Rock.Field.Types;
    using Rock.Model;
    [Table( "_org_secc_FamilyCheckin_MobileCheckinRecord" )]
    [DataContract]
    public partial class MobileCheckinRecord : Rock.Data.Model<MobileCheckinRecord>, Rock.Data.IRockEntity
    {
        [Index( IsUnique = true )]
        [Required]
        [DataMember]
        [MaxLength( 255 )]
        public string AccessKey { get; set; }

        [Index( IsUnique = true )]
        [DataMember]
        [MaxLength( 255 )]
        public string UserName { get; set; }

        [Index( IsUnique = true )]
        [DataMember]
        public int FamilyGroupId { get; set; }

        [LavaInclude]
        public virtual Group FamilyGroup { get; set; }

        [DataMember]
        public string SerializedCheckInState { get; set; }

        [DataMember]
        public DateTime? ReservedUntilDateTime { get; set; }

        [DataMember]
        public DateTime? ExpirationDateTime { get; set; }

        [DataMember]
        public int CampusId { get; set; }

        [LavaInclude]
        public virtual Campus Campus { get; set; }

        public virtual ICollection<Attendance> Attendances
        {
            get { return _attendances ?? ( _attendances = new Collection<Attendance>() ); }
            set { _attendances = value; }
        }
        private ICollection<Attendance> _attendances;
    }

    /// <summary>
    /// File Configuration class.
    /// </summary>
    public partial class MobileCheckinRecordConfiguration : EntityTypeConfiguration<MobileCheckinRecord>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KioskTypeConfiguration"/> class.
        /// </summary>
        public MobileCheckinRecordConfiguration()
        {
            this.HasMany( r => r.Attendances ).WithMany().Map( r => { r.MapLeftKey( "MobileCheckinRecordId" ); r.MapRightKey( "AttendanceId" ); r.ToTable( "_org_secc_FamilyCheckin_MobileCheckinRecordAttendances" ); } );
            this.HasEntitySetName( "MobileCheckinRecords" );
        }
    }
}
