using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Rock;
using Rock.Data;
using Rock.Web.Cache;

namespace org.secc.Reporting.Model
{
    [Table( "_org_secc_Reporting_DecisionForm" )]
    [DataContract]
    [HideFromReporting]
    public class DecisionReportItem : Model<DecisionReportItem>, IRockEntity
    {
        [DataMember]
        public string RecordType { get; set; }
        [DataMember]
        [Column("RecordId")]
        public int ReportItemId { get; set; }
        [DataMember]
        public int PersonAliasId { get; set; }
        [DataMember]
        public int PersonId { get; set; }
        [DataMember]
        public string LastName { get; set; }
        [DataMember]
        public string FirstName { get; set; }
        [DataMember]
        public string NickName { get; set; }
        [DataMember]
        public int? Age { get; set; }
        [DataMember]

        public bool IsMinor { get; set; }
        [DataMember]
        public string Gender { get; set; }
        [DataMember]
        public string Email { get; set; }
        [DataMember]
        public string MobilePhone { get; set; }
        [DataMember]
        public int? GraduationYear { get; set; }
        [DataMember]
        public int? HomeLocationId { get; set; }
        [DataMember]
        public int? ConnectionStatusValueId { get; set; }
        [DataMember]
        public string ConnectionStatusValue { get; set; }
        [DataMember]
        public DateTime? BaptismDate { get; set; }
        [DataMember]
        public string DecisionType { get; set; }
        [DataMember]
        public DateTime FormDate { get; set; }
        [DataMember]
        public int? DecisionCampusId { get; set; }
        [DataMember]
        public string DecisionCampusName { get; set; }
        [DataMember]
        public int? FamilyCampusId { get; set; }
        [DataMember]
        public string FamilyCampusName { get; set; }
        [DataMember]
        public string EventName { get; set; }
        [DataMember]
        public int? BaptismTypeValueId { get; set; }
        [DataMember]
        public string BaptismTypeValue { get; set; }
        [DataMember]
        public string ParentGuardianName { get; set; }
        [DataMember]
        public string ParentEmail { get; set; }
        [DataMember]
        public string ParentPhone { get; set; }
        [DataMember]
        public DateTime? StatementOfFaithSignedDate { get; set; }
        [DataMember]
        public DateTime? MembershipDate { get; set; }
        [DataMember]    
        public DateTime? MembershipClassDate { get; set; }
        [DataMember]
        public string HomeStreet1 { get; set; }
        [DataMember]
        public string HomeStreet2 { get; set; }
        [DataMember]
        public string HomeCity { get; set; }
        [DataMember]
        public string HomeState { get; set; }
        [DataMember]
        public string HomePostalCode { get; set; }
        [DataMember]
        public string HomeCountry { get; set; }
        [DataMember]
        public DateTime? Birthdate { get; set; }

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
