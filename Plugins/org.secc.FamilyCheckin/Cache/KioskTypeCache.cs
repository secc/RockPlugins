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
using System.Linq;
using System.Runtime.Serialization;
using org.secc.FamilyCheckin.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.FamilyCheckin.Cache
{
    [Serializable]
    [DataContract]
    public class KioskTypeCache : ModelCache<KioskTypeCache, KioskType>
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public int? CheckinTemplateId { get; set; }

        [DataMember]
        public GroupTypeCache CheckinTemplate
        {
            get
            {
                return GroupTypeCache.Get( CheckinTemplateId ?? 0 );
            }
            set
            {
                CheckinTemplateId = value?.Id;
            }
        }

        [DataMember]
        public int? CampusId { get; set; }

        [DataMember]
        public CampusCache Campus { get; set; }

        [DataMember]
        public bool IsMobile { get; set; }

        [DataMember]
        public int? MinutesValid { get; set; }

        [DataMember]
        public int? GraceMinutes { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public List<Schedule> Schedules { get; set; }

        /// <summary>
        /// Gets or sets the check in schedules. 
        /// These are not the kiosk schedules, rather the schedules for the checkin groups and locations themselves.
        /// </summary>
        /// <value>
        /// The check in schedules.
        /// </value>
        [DataMember]
        public List<Schedule> CheckInSchedules { get; set; }

        [DataMember]
        public List<Location> Locations { get; set; }

        [DataMember]
        public List<GroupTypeCache> GroupTypes { get; set; }


        public override void SetFromEntity( IEntity entity )
        {
            base.SetFromEntity( entity );

            var kioskType = entity as KioskType;
            if ( kioskType == null )
                return;

            //Rock tries to go fast, but it doesn't work right in redis for what I'm doing.
            RockContext rockContext = new RockContext();
            KioskTypeService kioskTypeService = new KioskTypeService( rockContext );
            kioskType = kioskTypeService.Get( kioskType.Id );


            Id = kioskType.Id;
            Guid = kioskType.Guid;
            Name = kioskType.Name;
            Description = kioskType.Description;
            CheckinTemplateId = kioskType.CheckinTemplateId;
            CampusId = kioskType.CampusId;
            Campus = CampusCache.Get( kioskType.CampusId ?? 0 );
            MinutesValid = kioskType.MinutesValid;
            GraceMinutes = kioskType.GraceMinutes;
            IsMobile = kioskType.IsMobile;
            Locations = kioskType.Locations.ToList();
            Schedules = kioskType.Schedules.ToList();
            GroupTypes = kioskType.GroupTypes.Select( gt => GroupTypeCache.Get( gt.Id ) ).ToList();
            Message = kioskType.Message;
            CheckInSchedules = kioskType.GroupTypes
                .SelectMany( gt => gt.Groups )
                .SelectMany( g => g.GroupLocations )
                .SelectMany( gl => gl.Schedules )
                .DistinctBy( s => s.Id )
                .ToList();
        }

        public bool IsOpen( DateTime? dateTime = null )
        {
            if ( !dateTime.HasValue )
            {
                dateTime = RockDateTime.Now;
            }
            return this.Schedules.Where( s => s.WasScheduleActive( dateTime.Value ) ).Any();
        }

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

        public static void ClearForTemplateId( int templateId )
        {
            var kioskTypeIds = All().Where( kt => kt.CheckinTemplateId == templateId ).Select( kt => kt.Id );
            foreach ( var id in kioskTypeIds )
            {
                //Why am I removing this item then immediatly getting it again?
                //Because FlushItem is marked internal and I'm too tired to hack anymore
                //This makes me sad
                Remove( id );
                Get( id );
            }
        }

        /// <summary>
        /// Verifies the cache is correct.
        /// </summary>
        /// <param name="errors">The errors.</param>
        public static void Verify( ref List<string> errors )
        {
            RockContext rockContext = new RockContext();
            KioskTypeService kioskTypeService = new KioskTypeService( rockContext );
            var kioskTypes = kioskTypeService.Queryable().ToList();
            var kioskTypeCaches = KioskTypeCache.All();

            if ( kioskTypes.Count != kioskTypeCaches.Count )
            {
                errors.Add( "Kiosk Types count does not match Kiosk Type Caches count" );
            }

            foreach ( var kioskType in kioskTypes )
            {
                var kioskTypeCache = KioskTypeCache.Get( kioskType.Id );

                if ( kioskType.Name != kioskTypeCache.Name
                    || kioskType.CampusId != kioskTypeCache.CampusId
                    || kioskType.Description != kioskTypeCache.Description
                    || kioskType.CheckinTemplateId != kioskTypeCache.CheckinTemplateId
                    || kioskType.MinutesValid != kioskTypeCache.MinutesValid
                    || kioskType.GraceMinutes != kioskTypeCache.GraceMinutes
                    || kioskType.IsMobile != kioskTypeCache.IsMobile
                    || kioskType.Message != kioskTypeCache.Message )
                {
                    errors.Add( "KioskType cache error Id: " + kioskType.Id.ToString() );
                }

                //Todo test KioskType Locations
                //Todo test KioskType GroupTypes
                //Todo test KioskType Schedules
            }
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
