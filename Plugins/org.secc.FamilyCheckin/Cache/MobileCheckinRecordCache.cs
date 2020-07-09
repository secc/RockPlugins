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
using System.Data.Entity;
using System.Linq;
using System.Runtime.Serialization;
using org.secc.FamilyCheckin.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.ServiceObjects.GeoCoder;
using Rock.Web.Cache;

namespace org.secc.FamilyCheckin.Cache
{
    [Serializable]
    [DataContract]
    public class MobileCheckinRecordCache : ModelCache<MobileCheckinRecordCache, MobileCheckinRecord>
    {
        [DataMember]
        public string AccessKey { get; set; }

        [DataMember]
        public string UserName { get; set; }

        [DataMember]
        public int FamilyGroupId { get; set; }

        [DataMember]
        public string SerializedCheckInState { get; set; }

        [DataMember]
        public DateTime? CreatedDateTime { get; set; }

        [DataMember]
        public DateTime? ReservedUntilDateTime { get; set; }

        [DataMember]
        public DateTime? ExpirationDateTime { get; set; }

        [DataMember]
        public MobileCheckinStatus Status { get; set; }

        [DataMember]
        public bool? IsDirty { get; set; }

        [DataMember]
        public List<int> AttendanceIds { get; set; }

        [DataMember]
        public int CampusId { get; set; }

        public List<Attendance> GetAttendances( RockContext rockContext )
        {
            AttendanceService attendanceService = new AttendanceService( rockContext );
            return attendanceService.Queryable().Where( a => AttendanceIds.Contains( a.Id ) ).ToList();
        }

        public static MobileCheckinRecordCache GetByAccessKey( string accessKey )
        {
            return All().Where( r => r.AccessKey == accessKey ).FirstOrDefault();
        }

        public static MobileCheckinRecordCache GetActiveByUserName( string userName )
        {
            return AllActive().Where( r => r.UserName == userName ).FirstOrDefault();
        }

        public static MobileCheckinRecordCache GetActiveByFamilyGroupId( int familyGroupId )
        {
            return AllActive().Where( r => r.FamilyGroupId == familyGroupId ).FirstOrDefault();
        }

        public static List<MobileCheckinRecordCache> AllActive()
        {
            return All()
                .Where( r => r.Status == MobileCheckinStatus.Active
                          && r.CreatedDateTime >= Rock.RockDateTime.Today )
                .ToList();
        }

        public static MobileCheckinRecordCache GetByAttendanceId( int id )
        {
            return All().Where( m => m.AttendanceIds.Contains( id ) ).FirstOrDefault();
        }

        public static MobileCheckinRecordCache Update( int id )
        {
            Remove( id );
            return Get( id );
        }

        public static bool CancelReservation( MobileCheckinRecordCache record, bool cancelEvenIfNotExpired = false )
        {
            if ( record.Status != MobileCheckinStatus.Active )
            {
                //We can't cancel a mobile record that is not active.
                return false;
            }

            //Keeps us from accidentally pulling the rug out from under someone
            if ( !cancelEvenIfNotExpired && record.ReservedUntilDateTime.HasValue && record.ReservedUntilDateTime.Value > Rock.RockDateTime.Now )
            {
                return false;
            }

            using ( RockContext rockContext = new RockContext() )
            {
                MobileCheckinRecordService mobileCheckinRecordService = new MobileCheckinRecordService( rockContext );
                AttendanceService attendanceService = new AttendanceService( rockContext );
                var mobileCheckinRecord = mobileCheckinRecordService.Get( record.Id );
                var attendances = mobileCheckinRecord.Attendances.ToList();

                foreach ( var attendance in attendances )
                {
                    attendance.EndDateTime = Rock.RockDateTime.Now;
                    attendance.QualifierValueId = null;
                }

                mobileCheckinRecord.Status = MobileCheckinStatus.Canceled;

                rockContext.SaveChanges();
                attendances.ForEach( a => AttendanceCache.AddOrUpdate( a ) );
                Update( mobileCheckinRecord.Id );
            }

            return true;
        }

        public override void SetFromEntity( IEntity entity )
        {
            base.SetFromEntity( entity );

            var record = entity as MobileCheckinRecord;
            if ( record == null )
                return;

            Id = record.Id;
            Guid = record.Guid;
            AccessKey = record.AccessKey;
            UserName = record.UserName;
            FamilyGroupId = record.FamilyGroupId;
            ReservedUntilDateTime = record.ReservedUntilDateTime;
            ExpirationDateTime = record.ExpirationDateTime;
            CreatedDateTime = record.CreatedDateTime;
            SerializedCheckInState = record.SerializedCheckInState;
            Status = record.Status;
            IsDirty = record.IsDirty;
            AttendanceIds = record.Attendances.Select( a => a.Id ).ToList();
            CampusId = record.CampusId;
        }

        public MobileCheckinRecord GetEntity( RockContext rockContext )
        {
            MobileCheckinRecordService mobileCheckinRecordService = new MobileCheckinRecordService( rockContext );
            return mobileCheckinRecordService.Get( Id );
        }

        #region BaseOverrides
        /// <summary>
        /// Gets all the instances of this type of model/entity that are currently in cache.
        /// </summary>
        /// <returns></returns>
        public static new List<MobileCheckinRecordCache> All()
        {
            return All( null );
        }

        /// <summary>
        /// Gets all the instances of this type of model/entity that are currently in cache.
        /// </summary>
        /// <returns></returns>
        public static new List<MobileCheckinRecordCache> All( RockContext rockContext )
        {
            var cachedKeys = GetOrAddKeys( () => QueryDbForAllIds( rockContext ) );
            if ( cachedKeys == null )
                return new List<MobileCheckinRecordCache>();

            var allValues = new List<MobileCheckinRecordCache>();
            foreach ( var key in cachedKeys.ToList() )
            {
                var value = Get( key.AsInteger(), rockContext );
                if ( value != null )
                {
                    allValues.Add( value );
                }
            }

            return allValues;
        }

        /// <summary>
        /// Queries the database for all ids.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private static List<string> QueryDbForAllIds( RockContext rockContext )
        {
            if ( rockContext != null )
            {
                return QueryDbForAllIdsWithContext( rockContext );
            }

            using ( var newRockContext = new RockContext() )
            {
                return QueryDbForAllIdsWithContext( newRockContext );
            }
        }

        /// <summary>
        /// Queries the database for all ids with context.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private static List<string> QueryDbForAllIdsWithContext( RockContext rockContext )
        {
            //We have to make our own version of this to only cache todays requests.
            var service = new MobileCheckinRecordService( rockContext );
            return service.Queryable().AsNoTracking()
                .Where( r => r.CreatedDateTime >= Rock.RockDateTime.Today )
                .Select( r => r.Id )
                .ToList()
                .ConvertAll( r => r.ToString() );
        }

        #endregion

        public static void Verify( ref List<string> errors )
        {
            RockContext rockContext = new RockContext();
            MobileCheckinRecordService mobileCheckinRecordService = new MobileCheckinRecordService( rockContext );
            var mobileCheckinRecords = mobileCheckinRecordService.Queryable().Where( r => r.CreatedDateTime >= Rock.RockDateTime.Today ).ToList();
            var mcrCaches = All();

            if ( mobileCheckinRecords.Count != mcrCaches.Count )
            {
                var recordIds = mobileCheckinRecords.Select( r => r.Id );
                var cacheIds = mcrCaches.Select( r => r.Id );
                var missingCacheIds = recordIds.Except( cacheIds ).ToList();
                var extraCacheIds = cacheIds.Except( recordIds ).ToList();
                foreach ( var id in missingCacheIds )
                {
                    errors.Add( $"Warning: Mobile Check-in Record Cache missing from All(): {id}" );
                }

                foreach ( var id in extraCacheIds )
                {
                    errors.Add( $"Error: Extraneous Mobile Check-in Record Cache: {id}" );
                }
            }

            foreach ( var mobileCheckinRecord in mobileCheckinRecords )
            {
                var mcrCache = Get( mobileCheckinRecord.Id );
                if ( mcrCache == null )
                {
                    errors.Add( "Error: Mobile Checkin Record Cache missing for Mobile Checkin Record Id: " + mobileCheckinRecord.Id.ToString() );
                    continue;
                }

                if ( mobileCheckinRecord.FamilyGroupId != mcrCache.FamilyGroupId )
                {
                    errors.Add( $"Error: Mobile Checkin Record Cache (Id:{mobileCheckinRecord.Id}) Desync: FamilyGroupId - DB:{mobileCheckinRecord.FamilyGroupId} - Cache:{mcrCache.FamilyGroupId}" );
                }

                if ( mobileCheckinRecord.UserName != mcrCache.UserName )
                {
                    errors.Add( $"Error: Mobile Checkin Record Cache (Id:{mobileCheckinRecord.Id}) Desync: UserName - DB:{mobileCheckinRecord.UserName} - Cache:{mcrCache.UserName}" );
                }

                if ( mobileCheckinRecord.Attendances.Count != mcrCache.AttendanceIds.Count )
                {
                    errors.Add( $"Error: Mobile Checkin Record Cache (Id:{mobileCheckinRecord.Id}) Desync: Attendance Count - DB:{mobileCheckinRecord.Attendances.Count} - Cache:{mcrCache.AttendanceIds.Count}" );
                }

                if ( mobileCheckinRecord.AccessKey != mcrCache.AccessKey )
                {
                    errors.Add( $"Error: Mobile Checkin Record Cache (Id:{mobileCheckinRecord.Id}) Desync: AccessKey - DB:{mobileCheckinRecord.AccessKey} - Cache:{mcrCache.AccessKey}" );
                }

                if ( mobileCheckinRecord.CampusId != mcrCache.CampusId )
                {
                    errors.Add( $"Error: Mobile Checkin Record Cache (Id:{mobileCheckinRecord.Id}) Desync: CampusId - DB:{mobileCheckinRecord.CampusId} - Cache:{mcrCache.CampusId}" );
                }

                if ( mobileCheckinRecord.ReservedUntilDateTime != mcrCache.ReservedUntilDateTime )
                {
                    errors.Add( $"Error: Mobile Checkin Record Cache (Id:{mobileCheckinRecord.Id}) Desync: ReservedUntilDateTime - DB:{mobileCheckinRecord.ReservedUntilDateTime} - Cache:{mcrCache.ReservedUntilDateTime}" );
                }

                if ( mobileCheckinRecord.ExpirationDateTime != mcrCache.ExpirationDateTime )
                {
                    errors.Add( $"Error: Mobile Checkin Record Cache (Id:{mobileCheckinRecord.Id}) Desync: ExpirationDateTime - DB:{mobileCheckinRecord.ExpirationDateTime} - Cache:{mcrCache.ExpirationDateTime}" );
                }

                if ( mobileCheckinRecord.SerializedCheckInState != mcrCache.SerializedCheckInState )
                {
                    errors.Add( $"Error: Mobile Checkin Record Cache (Id:{mobileCheckinRecord.Id}) Desync: SerializedCheckInState - DB:{mobileCheckinRecord.SerializedCheckInState} - Cache:{mcrCache.SerializedCheckInState}" );
                }

                if ( mcrCache.SerializedCheckInState.IsNullOrWhiteSpace() )
                {
                    errors.Add( $"Info: Mobile Checkin Record Cache missing serialized check-in data. Id: {mobileCheckinRecord.Id}." );
                }
                //Todo Check Attendance Status
            }
        }
    }
}

