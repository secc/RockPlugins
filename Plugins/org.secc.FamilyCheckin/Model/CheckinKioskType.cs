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
    using System.Linq;
    using System.Runtime.Serialization;
    using Rock;
    using Rock.Lava;
    using Rock.Model;

    [Table( "_org_secc_FamilyCheckin_KioskType" )]
    [DataContract]
    public partial class CheckinKioskType : Rock.Data.Model<CheckinKioskType>, Rock.Security.ISecured, Rock.Data.IRockEntity
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
        public int? CheckinTemplateId { get; set; }

        public virtual GroupType CheckinTemplate { get; set; }

        [DataMember]
        public int? CampusId { get; set; }

        [LavaVisible]
        public virtual Campus Campus { get; set; }

        [DataMember]
        public int? MinutesValid { get; set; }

        [DataMember]
        public int? GraceMinutes { get; set; }

        [DataMember]
        public string Theme { get; set; }

        [DataMember]
        [Index]
        public bool IsMobile { get; set; }

        /// <summary>
        /// Gets or sets a collection containing the <see cref="Rock.Model.Location">Locations</see> that use this kiosktype.
        /// </summary>
        /// <value>
        /// A collection of <see cref="Rock.Model.Location">Locations</see> that use this kiosktype.
        /// </value>
        public virtual ICollection<Location> Locations
        {
            get { return _locations ?? ( _locations = new Collection<Location>() ); }
            set { _locations = value; }
        }
        private ICollection<Location> _locations;

        public virtual ICollection<Schedule> Schedules
        {
            get { return _schedules ?? ( _schedules = new Collection<Schedule>() ); }
            set { _schedules = value; }
        }
        private ICollection<Schedule> _schedules;

        public virtual ICollection<GroupType> GroupTypes
        {
            get { return _groupTypes ?? ( _groupTypes = new Collection<GroupType>() ); }
            set { _groupTypes = value; }
        }
        private ICollection<GroupType> _groupTypes;

        public bool IsOpen( DateTime? dateTime = null )
        {
            if ( !dateTime.HasValue )
            {
                dateTime = RockDateTime.Now;
            }
            return this.Schedules.Where( s => s.WasScheduleActive( dateTime.Value ) ).Any();
        }

        [DataMember]
        public string Message { get; set; }

        public DateTime? GetNextOpen( DateTime? dateTime = null )
        {
            var now = RockDateTime.Now;
            if ( dateTime.HasValue )
            {
                now = dateTime.Value;
            }
            var tomorrow = now.Date.AddDays( 1 );
            var times = this.Schedules
                .Where( s => s.GetScheduledStartTimes( now, tomorrow ).FirstOrDefault() > now )
                .OrderBy( t => t.GetNextStartDateTime( now ) )
                .FirstOrDefault();
            return times != null ? times.GetNextStartDateTime( now ) : ( DateTime? ) null;
        }

    }

    /// <summary>
    /// File Configuration class.
    /// </summary>
    public partial class KioskTypeConfiguration : EntityTypeConfiguration<CheckinKioskType>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KioskTypeConfiguration"/> class.
        /// </summary>
        public KioskTypeConfiguration()
        {
            this.HasOptional( d => d.CheckinTemplate ).WithMany().HasForeignKey( d => d.CheckinTemplateId ).WillCascadeOnDelete( false );
            this.HasMany( kt => kt.Locations ).WithMany().Map( kt => { kt.MapLeftKey( "KioskTypeId" ); kt.MapRightKey( "LocationId" ); kt.ToTable( "_org_secc_FamilyCheckin_KioskTypeLocation" ); } );
            this.HasMany( kt => kt.Schedules ).WithMany().Map( kt => { kt.MapLeftKey( "KioskTypeId" ); kt.MapRightKey( "ScheduleId" ); kt.ToTable( "_org_secc_FamilyCheckin_KioskTypeSchedule" ); } );
            this.HasMany( kt => kt.GroupTypes ).WithMany().Map( kt => { kt.MapLeftKey( "KioskTypeId" ); kt.MapRightKey( "GroupTypeId" ); kt.ToTable( "_org_secc_FamilyCheckin_KioskTypeGroupType" ); } );
            this.HasEntitySetName( "KioskTypes" );
        }
    }

}
