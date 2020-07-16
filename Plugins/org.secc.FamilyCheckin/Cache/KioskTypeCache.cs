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

        public CampusCache Campus { get => CampusCache.Get( CampusId ?? 0 ); }

        [DataMember]
        public bool IsMobile { get; set; }

        [DataMember]
        public int? MinutesValid { get; set; }

        [DataMember]
        public int? GraceMinutes { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string Theme { get; set; }

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
        public List<int> GroupTypeIds { get; set; }

        public List<GroupTypeCache> GroupTypes { get => GroupTypeCache.All().Where( g => GroupTypeIds.Contains( g.Id ) ).ToList(); }


        public override void SetFromEntity( IEntity entity )
        {
            base.SetFromEntity( entity );

            var kioskType = entity as KioskType;
            if ( kioskType == null )
                return;

            //Rock tries to go fast, but it doesn't work right in redis for what I'm doing.
            RockContext rockContext = new RockContext();
            KioskTypeService kioskTypeService = new KioskTypeService( rockContext );

            var kioskdata = kioskTypeService.Queryable()
                .Where( k => k.Id == kioskType.Id )
                .Select( k => new
                {
                    Kiosk = k,
                    Locations = k.Locations,
                    Schedules = k.Schedules,
                    CheckInSchedules = k.GroupTypes
                     .SelectMany( gt => gt.Groups )
                 .SelectMany( g => g.GroupLocations )
                 .SelectMany( gl => gl.Schedules )
                 .Distinct()
                } ).FirstOrDefault();

            Name = kioskType.Name;
            Description = kioskType.Description;
            CheckinTemplateId = kioskType.CheckinTemplateId;
            CampusId = kioskType.CampusId;
            MinutesValid = kioskType.MinutesValid;
            GraceMinutes = kioskType.GraceMinutes;
            IsMobile = kioskType.IsMobile;
            Theme = kioskType.Theme;
            Locations = kioskdata.Locations.Select( l => l.Clone( false ) ).ToList();
            Schedules = kioskdata.Schedules.Select( s => s.Clone( false ) ).ToList();
            GroupTypeIds = kioskType.GroupTypes.Select( gt => gt.Id ).ToList();
            Message = kioskType.Message;
            CheckInSchedules = kioskdata.CheckInSchedules.Select( s => s.Clone( false ) ).ToList();
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
                FlushItem( id );
            }
        }

        public static void FlushItem( int id )
        {
            var qualifiedKey = QualifiedKey( id.ToString() );
            RockCacheManager<KioskTypeCache>.Instance.Cache.Remove( qualifiedKey );
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
