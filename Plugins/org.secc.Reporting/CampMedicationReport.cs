using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Tasks;
using Rock.Web.Cache;


namespace org.secc.Reporting
{
    public class CampMedicationReport
    {
        private readonly CampMedicationReportFilter _filter;
        private char _delimiter = ',';
        private char _textQualifier = '\"';
        private string _medicationAttributeKey = "Medications";
        private string _medicationScheduleDefinedTypeGuid = "81b51822-50d7-4be6-a462-04077405bb7e";
        private Dictionary<int, List<int>> _instanceSmallGroups = new Dictionary<int, List<int>>();


        public CampMedicationReport( CampMedicationReportFilter filter )
        {
            _filter = filter;
        }

        public string GenerateMedicationReport()
        {

            using (var context = new RockContext())
            {
                var campuses = CampusCache.All().Select( c => new { c.Id, c.Name } );
                var medSchedules = DefinedTypeCache.Get( _medicationScheduleDefinedTypeGuid.AsGuid() )
                    .DefinedValues;
                var camperMedications = new List<CampMedicationReportItem>();


                var personEntityTypeId = EntityTypeCache.GetId( typeof( Person ) );
                var medicationMatrixAttribute = new AttributeService( context ).GetByEntityTypeId( personEntityTypeId )
                    .Where( a => a.Key == _medicationAttributeKey )
                    .FirstOrDefault();

                var registrantService = new RegistrationRegistrantService( context );

                var medAttributeQry = new AttributeValueService( context ).Queryable().AsNoTracking()
                    .Where( v => v.AttributeId == medicationMatrixAttribute.Id );

                var registrantQry = registrantService.Queryable().AsNoTracking()
                    .Include( r => r.Registration.RegistrationInstance )
                    .Include( r => r.Registration.RegistrationInstance.RegistrationTemplate )
                    .Include( r => r.PersonAlias.Person )
                    .Where( r => r.OnWaitList == false );

                if (_filter.RegistrationInstanceId.HasValue)
                {
                    registrantQry = registrantQry.Where( r => r.Registration.RegistrationInstanceId == _filter.RegistrationInstanceId.Value );
                }

                else if (_filter.RegistrationTemplateId.HasValue)
                {
                    registrantQry = registrantQry.Where( r => r.Registration.RegistrationInstance.RegistrationTemplateId == _filter.RegistrationTemplateId );
                }

                var registrantsWithMedAttributes = registrantQry.Join( medAttributeQry, r => r.PersonAlias.PersonId,
                    v => v.EntityId, ( r, v ) => new { Registrant = r, AttributeGridGuid = v.Value } )
                    .AsEnumerable();

                List<CampMedicationReportItem> reportItems = new List<CampMedicationReportItem>();

                foreach (var r in registrantsWithMedAttributes)
                {

                    if (r.AttributeGridGuid.IsNullOrWhiteSpace())
                    {
                        continue;
                    }

                    var smallGroup = GetSmallGroup( r.Registrant.PersonAlias.PersonId, r.Registrant.Registration.RegistrationInstanceId );
                    var meds = GetMedications( r.AttributeGridGuid.AsGuid() );
                    var campus = campuses.Where( c => r.Registrant.Registration.RegistrationInstance.Name.EndsWith( c.Name, StringComparison.InvariantCultureIgnoreCase ) ).FirstOrDefault();
                    foreach (var med in meds)
                    {
                        var scheduleGuids = med.ScheduleGuids
                            .SplitDelimitedValues()
                            .AsGuidList();

                        reportItems.Add( new CampMedicationReportItem
                        {
                            PersonId = r.Registrant.PersonAlias.Person.Id,
                            LastName = r.Registrant.PersonAlias.Person.LastName,
                            NickName = r.Registrant.PersonAlias.Person.NickName,
                            BirthDate = r.Registrant.PersonAlias.Person.BirthDate,
                            SmallGroup = smallGroup != null ? smallGroup.GroupName : null,
                            GroupLeader = smallGroup != null ? smallGroup.GroupLeader : null,
                            RegistrationInstanceName = r.Registrant.Registration.RegistrationInstance.Name,
                            CampusId = campus != null ? (int?) campus.Id : null,
                            CampusName = campus != null ? campus.Name : null,
                            MedicationName = med.Medication,
                            Instructions = med.Instructions,
                            ScheduleDefinedValueIds = medSchedules.Where( s => scheduleGuids.Contains( s.Guid ) ).Select( s => s.Id ).ToList()
                        } );


                    }

                }

                return reportItems.Take( 10 ).ToJson();
            }



        }

        public List<CampMedicationSummary> GetMedications( Guid medicationGridGuid )
        {
            using (var context = new RockContext())
            {
                var matrixItemEntityId = EntityTypeCache.GetId( typeof( AttributeMatrixItem ) );
                var matrixItemService = new AttributeMatrixItemService( context );

                var attributeValueQry = new AttributeValueService( context )
                    .Queryable().AsNoTracking()
                    .Where( q => q.Attribute.EntityTypeId == matrixItemEntityId.Value );

                var meds = matrixItemService.Queryable().AsNoTracking()
                    .Where( i => i.AttributeMatrix.Guid == medicationGridGuid )
                    .Join( attributeValueQry, i => i.Id, v => v.EntityId,
                        ( i, v ) => new { ItemId = i.Id, AttributeKey = v.Attribute.Key, AttributeValue = v.Value } )
                    .GroupBy( i => i.ItemId )
                    .Select( i => new CampMedicationSummary
                        {
                            ItemId = i.Key,
                            Medication = i.Where( i1 => i1.AttributeKey == "Medication" ).Select( i1 => i1.AttributeValue ).FirstOrDefault(),
                            Instructions = i.Where( i1 => i1.AttributeKey == "Instructions" ).Select( i1 => i1.AttributeValue ).FirstOrDefault(),
                            ScheduleGuids = i.Where( i1 => i1.AttributeKey == "Schedule" ).Select( i1 => i1.AttributeValue ).FirstOrDefault()
                        } )
                    .ToList();

                return meds;
            }


            
        }

        public CampSmallGroupSummary GetSmallGroup( int personId, int registrationInstanceId )
        {
            var smallGroupTypeGuid = Guid.Parse( "2936a009-2552-448e-9c3c-17d9cc0f8742" );

            using (var context = new RockContext())
            {

                if (!_instanceSmallGroups.ContainsKey( registrationInstanceId ))
                {
                    var smallGroupGroupTypeId = GroupTypeCache.GetId( smallGroupTypeGuid ).Value;

                    var placementService = new RegistrationTemplatePlacementService( context );
                    var placementId = placementService.Queryable().AsNoTracking()
                        .Where( p => p.GroupTypeId == smallGroupGroupTypeId )
                        .Where( p => p.RegistrationTemplate.Instances.Select( i => i.Id ).Contains( registrationInstanceId ) )
                        .Select( p => p.Id )
                        .FirstOrDefault();

                    var registrationInstanceSmallGroupIds = new RegistrationInstanceService( context )
                        .GetRegistrationInstancePlacementGroupsByPlacement( registrationInstanceId, placementId )
                        .Select( g => g.Id )
                        .ToList();

                    _instanceSmallGroups.Add( registrationInstanceId, registrationInstanceSmallGroupIds );
                }

                var groupMemberSerivce = new GroupMemberService( context );
                var group = groupMemberSerivce.Queryable().AsNoTracking()
                    .Where( m => _instanceSmallGroups[registrationInstanceId].Contains( m.GroupId ) )
                    .Where( m => m.PersonId == personId && m.GroupMemberStatus == GroupMemberStatus.Active )
                    .Select( m => m.Group )
                    .FirstOrDefault();

                if (group == null)
                {
                    return null;
                }

                var sms = new CampSmallGroupSummary
                {
                    GroupId = group.Id,
                    GroupName = group.Name
                };


                var leader = groupMemberSerivce.Queryable().AsNoTracking()
                    .Where( m => m.GroupId == sms.GroupId )
                    .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active )
                    .Where( m => m.GroupRole.IsLeader )
                    .Select( m => m.Person )
                    .FirstOrDefault();

                if (leader != null)
                {
                    sms.GroupLeader = leader.FullName;
                }

                return sms;
            }


        }

        public class CampSmallGroupSummary
        {
            public int GroupId { get; set; }
            public string GroupName { get; set; }
            public string GroupLeader { get; set; }
        }

        public class CampMedicationSummary
        {
            public int ItemId { get; set; }
            public string Medication { get; set; }
            public string Instructions { get; set; }
            public string ScheduleGuids { get; set; }
        }

        public class CampMedicationReportItem
        {
            public int PersonId { get; set; }
            public string LastName { get; set; }
            public string NickName { get; set; }
            public DateTime? BirthDate { get; set; }
            public string SmallGroup { get; set; }
            public string GroupLeader { get; set; }
            public string MedicationName { get; set; }
            public string Instructions { get; set; }
            public List<int> ScheduleDefinedValueIds { get; set; }
            public string RegistrationInstanceName { get; set; }
            public int? CampusId { get; set; }
            public string CampusName { get; set; }
        }

        public class CampMedicationReportFilter
        {
            public int? RegistrationTemplateId { get; set; }
            public int? RegistrationInstanceId { get; set; }
            public int? CampusId { get; set; }
            public List<int> ScheduleIds { get; set; }
        }
    }
}