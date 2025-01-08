using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using System.Data.SqlClient;

namespace org.secc.Jobs
{

    /// <summary>
    /// Migrates the Facilities Team to Central Support and 
    /// Updates their requisitions to Central Support
    /// </summary>
    [DisplayName( "Migrate Facilties Staff & Requisitions" )]
    [DefinedValueField( "Centeral Support Location",
        Description = "The defined value location for Central Support.",
        AllowMultiple = false,
        IsRequired = true,
        DefinedTypeGuid = "729dd959-6081-4b47-b639-cf7be460a8df",
        Key = "Location" )]
    [DefinedValueField( "Facilities Ministry Area",
        Description = "The defiend value for the Facilities Ministry Area",
        AllowMultiple = false,
        IsRequired = true,
        DefinedTypeGuid = "d5074ec8-9572-492c-b074-1b91d4c7a176",
        Key = "MinistryArea" )]
    [TextField( "Location Attribute Key",
        Description = "SECC Location Person Attribute Key",
        IsRequired = true,
        Key = "LocationAttributeKey" )]
    [TextField( "Ministry Area Attribute Key",
        Description = "Ministry Area Person Attribute Key",
        IsRequired = true,
        Key = "MinistryAreaKey" )]

    [DisallowConcurrentExecution]
    public class UpdateFacilitiesTeamLocation : IJob
    {
        DefinedValueCache CentralSupportDV = null;
        DefinedValueCache FacilitiesDV = null;
        string LocationKey = null;
        string MinistryKey = null;

        public void Execute( IJobExecutionContext context )
        {
            JobDataMap datamap = context.JobDetail.JobDataMap;
            var rockContext = new RockContext();

            CentralSupportDV = DefinedValueCache.Get( datamap.GetString( "Location" ).AsGuid(), rockContext );
            FacilitiesDV = DefinedValueCache.Get( datamap.GetString( "MinistryArea" ).AsGuid(), rockContext );
            LocationKey = datamap.GetString( "LocationAttributeKey" ).Trim();
            MinistryKey = datamap.GetString( "MinistryAreaKey" ).Trim();

            var facilityStaffToMove = GetStaffMembersToUpdate( rockContext );



            foreach (var s in facilityStaffToMove)
            {
                UpdateStaffMember( s );
            }

            var sql = @"
                SELECT requisition_id 
                FROM dbo._org_secc_purchasing_requisition
                WHERE location_luid <> @CentralSupport
                  AND ministry_luid = @Facilities ";

            var sqlParams = new List<SqlParameter>();
            sqlParams.Add( new SqlParameter( "@CentralSupport", CentralSupportDV.Id ) );
            sqlParams.Add( new SqlParameter( "@Facilities", FacilitiesDV.Id ) );

            var requisitionIds = rockContext.Database.SqlQuery<int>( sql, sqlParams.ToArray() )
                .ToList();

            foreach (var id in requisitionIds)
            {
                UpdateRequisition( id, rockContext );
            }


            var capitalSql = @"
                SELECT capital_request_id
                FROM dbo._org_secc_purchasing_capitalrequest
                WHERE location_luid <> @CentralSupport
                    AND ministry_luid = @Facilities ";

            sqlParams = new List<SqlParameter>();
            sqlParams.Add( new SqlParameter( "@CentralSupport", CentralSupportDV.Id ) );
            sqlParams.Add( new SqlParameter( "@Facilities", FacilitiesDV.Id ) );

            var capitalRequests = rockContext.Database.SqlQuery<int>( capitalSql, sqlParams.ToArray() )
                .ToList();

            foreach(var id in capitalRequests)
            {
                UpdateCapitalRequests(id, rockContext);
            }




        }

        private List<StaffMemberSummary> GetStaffMembersToUpdate( RockContext rockContext )
        {
            var attributeValueService = new AttributeValueService( rockContext );
            var personService = new PersonService( rockContext );
            var personEntityType = EntityTypeCache.Get( typeof( Person ) );


            var locationQry = attributeValueService.Queryable().AsNoTracking()
                .Where( v => v.Attribute.EntityTypeId == personEntityType.Id )
                .Where( v => v.Attribute.Key == LocationKey )
                .Where( v => v.Value != "" );

            var ministryAreaQry = attributeValueService.Queryable().AsNoTracking()
                .Where( v => v.Attribute.EntityTypeId == personEntityType.Id )
                .Where( v => v.Attribute.Key == MinistryKey )
                .Where( v => v.Value != "" );

            return personService.Queryable().AsNoTracking()
                .Join( locationQry, p => p.Id, l => l.EntityId,
                    ( p, l ) => new { PersonId = p.Id, l.Value } )
                .Join( ministryAreaQry, p => p.PersonId, m => m.EntityId,
                    ( p, m ) => new { PersonId = p.PersonId, LocationGuid = p.Value, MinistryAreaGuid = m.Value } )
                .ToList()
                .SelectMany( p => p.MinistryAreaGuid.SplitDelimitedValues(), ( p, l ) => new StaffMemberSummary
                {
                    PersonId = p.PersonId,
                    LocationGuid = p.LocationGuid.AsGuid(),
                    MinistryAreaGuid = l.AsGuid()
                } )
                .Where( p => p.MinistryAreaGuid == FacilitiesDV.Guid )
                .Where( p => p.LocationGuid != CentralSupportDV.Guid )
                .ToList();
        }

        private void UpdateCapitalRequests(int id, RockContext context)
        {
            var sql = @"UPDATE dbo._org_secc_purchasing_CapitalRequest
                    SET location_luid = @LocationLUID
                    WHERE capital_request_id = @CapitalRequestId ";

            var sqlParams = new List<SqlParameter>();
            sqlParams.Add( new SqlParameter( "@LocationLUID", CentralSupportDV.Id ) );
            sqlParams.Add( new SqlParameter( "@CapitalRequestId", id ) );

            context.Database.ExecuteSqlCommand( sql, sqlParams.ToArray() );
        }

        private void UpdateRequisition( int id, RockContext context )
        {
            var sql = @"UPDATE dbo._org_secc_Purchasing_Requisition 
                        SET location_luid = @LocationLUID
                        WHERE requisition_id = @RequisitionId ";

            var sqlParams = new List<SqlParameter>();
            sqlParams.Add( new SqlParameter( "@LocationLUID", CentralSupportDV.Id ) );
            sqlParams.Add( new SqlParameter( "@RequisitionId", id ) );

            context.Database.ExecuteSqlCommand( sql, sqlParams.ToArray() );

        }

        private void UpdateStaffMember( StaffMemberSummary s )
        {
            using (var rockContext = new RockContext())
            {
                var person = new PersonService( rockContext ).Get( s.PersonId );
                person.LoadAttributes( rockContext );
                person.SetAttributeValue( LocationKey, CentralSupportDV.Guid );
                person.SaveAttributeValue( LocationKey );
                rockContext.SaveChanges();

            }
        }
    }

    class StaffMemberSummary
    {
        public int PersonId { get; set; }
        public Guid LocationGuid { get; set; }
        public Guid MinistryAreaGuid { get; set; }
    }
}
