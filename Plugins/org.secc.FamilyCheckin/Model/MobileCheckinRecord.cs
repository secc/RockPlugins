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
    using Rock.Model;
    [Table( "_org_secc_FamilyCheckin_MobileCheckinRecord" )]
    [DataContract]
    public partial class MobileCheckinRecord : Rock.Data.Model<MobileCheckinRecord>, Rock.Data.IRockEntity
    {

        /// <summary>
        /// Gets or sets the access key.
        /// </summary>
        /// <value>
        /// The access key.
        /// </value>
        [Index( IsUnique = true )]
        [Required]
        [DataMember]
        [MaxLength( 255 )]
        public string AccessKey { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        /// <value>
        /// The username.
        /// </value>
        [Index( IsUnique = true )]
        [DataMember]
        [MaxLength( 255 )]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the family group identifier.
        /// </summary>
        /// <value>
        /// The family group identifier.
        /// </value>
        [DataMember]
        public int FamilyGroupId { get; set; }

        /// <summary>
        /// Gets or sets the family group.
        /// </summary>
        /// <value>
        /// The family group.
        /// </value>
        [LavaInclude]
        public virtual Group FamilyGroup { get; set; }

        /// <summary>
        /// Gets or sets the state of the serialized check in.
        /// </summary>
        /// <value>
        /// The state of the serialized check in.
        /// </value>
        [DataMember]
        public string SerializedCheckInState { get; set; }

        /// <summary>
        /// Gets or sets the reserved until date time.
        /// </summary>
        /// <value>
        /// The reserved until date time.
        /// </value>
        [DataMember]
        public DateTime? ReservedUntilDateTime { get; set; }

        /// <summary>
        /// Gets or sets the expiration date time.
        /// </summary>
        /// <value>
        /// The expiration date time.
        /// </value>
        [DataMember]
        public DateTime? ExpirationDateTime { get; set; }


        /// <summary>
        /// Gets or sets the campus identifier.
        /// </summary>
        /// <value>
        /// The campus identifier.
        /// </value>
        [DataMember]
        public int CampusId { get; set; }


        /// <summary>
        /// Gets or sets the campus.
        /// </summary>
        /// <value>
        /// The campus.
        /// </value>
        [LavaInclude]
        public virtual Campus Campus { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        [DataMember]
        [Index]
        public MobileCheckinStatus Status { get; set; } = MobileCheckinStatus.Active;


        /// <summary>
        /// Gets or sets a value indicating whether this instance is dirty.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is dirty; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool? IsDirty { get; set; } = false;

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

    public enum MobileCheckinStatus
    {
        Active = 0,
        Complete = 1,
        Canceled = 2
    }
}
