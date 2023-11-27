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
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;
using Rock;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Security;

namespace org.secc.GroupManager.Model
{
    [Table( "_org_secc_GroupManager_PublishGroup" )]
    [DataContract]
    public class PublishGroup : Model<PublishGroup>, ISecured, IRockEntity
    {
        [Index]
        [DataMember]
        public int GroupId { get; set; }

        [LavaVisible]
        public virtual Group Group { get; set; }

        [DataMember]
        public string Name { get; set; }

        [LavaVisible]
        public string Title
        {
            get
            {
                if ( Name.IsNotNullOrWhiteSpace() )
                {
                    return Name;
                }
                return Group.Name;
            }
        }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public int? ImageId { get; set; }

        [LavaVisible]
        public BinaryFile Image { get; set; }

        [Index]
        [DataMember]
        public DateTime StartDateTime { get; set; }

        [Index]
        [DataMember]
        public DateTime EndDateTime { get; set; }

        [LavaVisible]
        public virtual ICollection<DefinedValue> AudienceValues
        {
            get { return _audienceValues ?? ( _audienceValues = new Collection<DefinedValue>() ); }
            set { _audienceValues = value; }
        }
        private ICollection<DefinedValue> _audienceValues;

        [Index]
        [DataMember]
        public int RequestorAliasId { get; set; }

        [LavaVisible]
        public virtual PersonAlias RequestorAlias { get; set; }

        [Index]
        [DataMember]
        public int ContactPersonAliasId { get; set; }

        [LavaVisible]
        public virtual PersonAlias ContactPersonAlias { get; set; }

        [DataMember]
        public string ContactEmail { get; set; }

        [DataMember]
        public RegistrationRequirement RegistrationRequirement { get; set; } = RegistrationRequirement.NoRegistration;

        [DataMember]
        public string RegistrationLink { get; set; }

        [DataMember]
        public bool ChildcareAvailable { get; set; } = false;

        [DataMember]
        public ChildcareOptions ChildcareOptions { get; set; } = 0;

        [DataMember]
        public string RegistrationDescription { get; set; }

        [DataMember]
        public string ChildcareRegistrationDescription { get; set; }

        [DataMember]
        public string ChildcareRegistrationLink { get; set; }

        [DataMember]
        public string ContactPhoneNumber { get; set; }

        [DataMember]
        public string ConfirmationFromName { get; set; }

        [DataMember]
        public string ConfirmationEmail { get; set; }

        [DataMember]
        public string ConfirmationSubject { get; set; }

        [DataMember]
        public string ConfirmationBody { get; set; }

        [DataMember]
        public PublishGroupStatus PublishGroupStatus { get; set; }

        [DataMember]
        public bool AllowSpouseRegistration { get; set; }

        [DataMember]
        public DayOfWeek? WeeklyDayOfWeek { get; set; }

        [DataMember]
        public TimeSpan? WeeklyTimeOfDay { get; set; }

        [DataMember]
        public string CustomSchedule { get; set; }


        [LavaVisible]
        public string ScheduleText
        {
            get
            {
                if ( CustomSchedule.IsNotNullOrWhiteSpace() )
                {
                    return CustomSchedule;
                }
                if ( WeeklyDayOfWeek.HasValue )
                {
                    return string.Format( "{0}s {1}",
                        WeeklyDayOfWeek.Value.ToString(),
                        WeeklyTimeOfDay.HasValue ? new DateTime( WeeklyTimeOfDay.Value.Ticks ).ToString( "h:mm tt" ) : "" );
                }
                return "";
            }
        }

        [DataMember]
        public DateTime? StartDate { get; set; }

        [DataMember]
        public string MeetingLocation { get; set; }

        [DataMember]
        public bool IsHidden { get; set; }

        [StringLength( 75 )]
        [DataMember]
        public string Slug { get; set; }

        [NotMapped]
        [LavaVisible]
        public bool IsActive { get => WasActive( Rock.RockDateTime.Today ); }

        public bool WasActive( DateTime dateTime )
        {
            return StartDateTime <= dateTime && EndDateTime >= dateTime;
        }

        [LavaVisible]
        public bool IsFull
        {
            get
            {

                if ( Group.GroupType.GroupCapacityRule == GroupCapacityRule.None
                   || !Group.GroupCapacity.HasValue )
                {
                    return false;
                }
                else
                {
                    return Group.GroupCapacity < Group.ActiveMembers().Count();
                }
            }
        }

        [LavaVisible]
        public bool IsNotFull { get => !IsFull; }
    }

    public enum PublishGroupStatus
    {
        PendingApproval = 0,
        Approved = 1,
        Denied = 2,
        Draft = 3,
        PendingIT = 4
    }

    public enum RegistrationRequirement
    {
        NoRegistration = 0,
        RegistrationAvailable = 1,
        RegistrationRequired = 2,
        CustomRegistration = 3,
        NeedCustomRegistration = 4


    }

    public enum ChildcareOptions
    {
        NoChildcare = 0,
        ChildcareNoRegistration = 1,
        ChildcareRegistrationRequired = 2,
        ChildareIncludedInCustomRegistration = 3
    }

    public partial class PublishGroupConfiguration : EntityTypeConfiguration<PublishGroup>
    {
        public PublishGroupConfiguration()
        {
            this.HasMany( pg => pg.AudienceValues ).WithMany().Map( pg => { pg.MapLeftKey( "PublishGroupdId" ); pg.MapRightKey( "DefinedValueId" ); pg.ToTable( "_org_secc_GroupManager_PublishGroupAudienceValue" ); } );
            this.HasEntitySetName( "PublishGroups" );
        }
    }
}
