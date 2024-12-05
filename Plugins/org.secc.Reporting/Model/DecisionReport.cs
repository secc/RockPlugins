using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Reporting.Model
{
    public class DecisionReport
    {
        public List<DecisionReportItem> Forms { get; set; }
        public List<DecisionReportItem> Steps { get; set; }

        public static DecisionReport LoadFromDataset( Guid datasetGuid )
        {
            using (var rockContext = new RockContext())
            {
                rockContext.Database.CommandTimeout = 60;
                var dataset = new PersistedDatasetService( rockContext )
                        .Get( datasetGuid );

                if (dataset == null)
                {
                    return null;
                }

                var report = JsonConvert.DeserializeObject<DecisionReport>( dataset.ResultData );

                return report;
            }


        }

        public List<DecisionReportItem> ConsolidateItems()
        {
            var consolidatedList = new List<DecisionReportItem>();

            consolidatedList.AddRange( Forms );
            consolidatedList.AddRange( Steps );

            return consolidatedList;
        }

    }

    public class DecisionReportItem
    {
        public string RecordType { get; set; }
        public int Id { get; set; }
        public int PersonAliasId { get; set; }
        public int PersonId { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string NickName { get; set; }
        public int? Age { get; set; }
        [JsonProperty( "IsMinor" )]
        public int IsMinorRaw { get; set; }
        [JsonIgnore]
        public bool IsMinor { get { return IsMinorRaw == 1; } }
        public string Gender { get; set; }
        public string Email { get; set; }
        public string MobilePhone { get; set; }
        public int? GraduationYear { get; set; }
        public int? HomeLocationId { get; set; }
        public int? ConnectionStatusValueId { get; set; }
        public string ConnectionStatusValue { get; set; }
        public DateTime? BaptismDate { get; set; }
        public string DecisionType { get; set; }
        public DateTime FormDate { get; set; }
        public int? DecisionCampusId { get; set; }
        public string DecisionCampusName { get; set; }
        public int? FamilyCampusId { get; set; }
        public string FamilyCampusName { get; set; }
        public string EventName { get; set; }
        public int? BaptismTypeValueId { get; set; }
        public string BaptismTypeValue { get; set; }
        public string ParentGuardianName { get; set; }
        public string ParentEmail { get; set; }
        public string ParentPhone { get; set; }
        public DateTime? StatementOfFaithSignedDate { get; set; }
        public DateTime? MembershipDate { get; set; }
        public DateTime? MembershipClassDate { get; set; }
        public string HomeStreet1 { get; set; }
        public string HomeStreet2 { get; set; }
        public string HomeCity { get; set; }
        public string HomeState { get; set; }
        public string HomePostalCode { get; set; }
        public string HomeCountry { get; set; }

        [JsonIgnore]
        public string FullName
        {
            get
            {
                return $"{NickName} {LastName}";
            }
        }

        [JsonIgnore]
        public string Grade
        {
            get
            {
                if (!GraduationYear.HasValue || GraduationYear < RockDateTime.CurrentGraduationYear)
                {
                    return null;
                }

                var gradeoffset = GraduationYear.Value - RockDateTime.CurrentGraduationYear;

                var gradeDv = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.SCHOOL_GRADES.AsGuid() )
                    .DefinedValues.Where( v => v.Value == gradeoffset.ToString() )
                    .FirstOrDefault();

                if (gradeDv == null)
                {
                    return null;
                }

                return gradeDv.GetAttributeValue( "Abbreviation" );

            }
        }

        [JsonIgnore]
        public string FullAddressHtml
        {
            get
            {
                if (HomeStreet1.IsNullOrWhiteSpace())
                {
                    return string.Empty;
                }
                var sb = new StringBuilder();
                sb.Append( $"{HomeStreet1}" );

                if (HomeStreet2.IsNotNullOrWhiteSpace())
                {
                    sb.Append( $" {HomeStreet2}" );
                }

                sb.Append( $"<br /> {HomeCity}, {HomeState} {HomePostalCode}" );

                return sb.ToString();
            }
        }

        [JsonIgnore]
        public string FullNameReversed
        {
            get
            {
                return $"{LastName},{NickName}";
            }
        }

        [JsonIgnore]
        public string MobilePhoneGridValue
        {
            get
            {
                if (IsMinor)
                {
                    return null;
                }

                return MobilePhone;
            }
        }

        [JsonIgnore]
        public string EmailGridValue
        {
            get
            {
                if (IsMinor)
                {
                    return null;
                }
                return Email;
            }
        }

        [JsonIgnore]
        public string FullAddressGrid
        {
            get
            {
                if (IsMinor)
                {
                    return null;
                }

                var sb = new StringBuilder();
                sb.Append( $"{HomeStreet1} " );

                if (HomeStreet2.IsNotNullOrWhiteSpace())
                {
                    sb.Append( $"{HomeStreet2} " );
                }

                sb.Append( $"{HomeCity}, {HomeState} {HomePostalCode}" );

                return sb.ToString();
            }
        }
    }
}
