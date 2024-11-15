using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

using Rock;
using Rock.Data;
using Rock.Web.Cache;

namespace org.secc.Reporting.Model
{
    [Table( "_org_secc_DecisionForm_Analytics", Schema = "dbo" )]
    [DataContract]
    [HideFromReporting]
    public partial class DecisionReport : Model<DecisionReport>
    {
        [Key]
        [DataMember]
        [DatabaseGenerated( DatabaseGeneratedOption.None )]
        public int WorkflowId { get; set; }
        [DataMember]
        public int PersonAliasId { get; set; }
        [DataMember]
        public int PersonId { get; set; }
        [DataMember]
        [MaxLength( 50 )]
        public string LastName { get; set; }
        [DataMember]
        [MaxLength( 50 )]
        public string NickName { get; set; }
        [DataMember]
        public int? Age { get; set; }
        [DataMember]
        [MaxLength( 1 )]
        public string Gender { get; set; }
        [DataMember]
        [MaxLength( 75 )]
        public string Email { get; set; }
        [MaxLength( 50 )]
        [DataMember]
        public string MobilePhone { get; set; }
        [DataMember]
        public int? GraduationYear { get; set; }
        [DataMember]
        public int? HomeLocationId { get; set; }
        [DataMember]
        public int ConnectionStatusValueId { get; set; }
        [DataMember]
        [MaxLength( 250 )]
        public string ConnectionStatusValue { get; set; }
        [DataMember]
        public DateTime? BaptismDate { get; set; }
        [DataMember]

        public string DecisionType { get; set; }
        [DataMember]
        public DateTime? FormDate { get; set; }
        [DataMember]
        public int DecisionCampusId { get; set; }
        [DataMember]
        [MaxLength( 100 )]
        public string DecisionCampusName { get; set; }
        [DataMember]
        public int? FamilyCampusId { get; set; }
        [DataMember]
        [MaxLength( 100 )]
        public string FamilyCampusName { get; set; }
        [DataMember]
        public string EventName { get; set; }
        [DataMember]
        public int? BaptismTypeValueId { get; set; }
        [DataMember]
        [MaxLength( 250 )]
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
        [MaxLength( 100 )]
        public string HomeStreet1 { get; set; }
        [DataMember]
        [MaxLength( 100 )]
        public string HomeStreet2 { get; set; }
        [DataMember]
        [MaxLength( 50 )]
        public string HomeCity { get; set; }
        [DataMember]
        [MaxLength( 50 )]
        public string HomeState { get; set; }
        [DataMember]
        [MaxLength( 50 )]
        public string HomePostalCode { get; set; }
        [DataMember]
        [MaxLength( 50 )]
        public string HomeCountry { get; set; }

        [JsonIgnore]
        public string EmailGridValue
        {
            get
            {
                if (Age < 18)
                {
                    return string.Empty;
                }
                else
                {
                    return Email;
                }
            }
        }

        [JsonIgnore]
        public string FullName
        {
            get
            {
                return $"{NickName} {LastName}";
            }
        }

        [JsonIgnore]
        public string FullNameReversed
        {
            get
            {
                return $"{LastName}, {NickName}";
            }
        }

        [JsonIgnore]
        public string FullAddress
        {
            get
            {
                var sb = new StringBuilder();
                if (HomeStreet1.IsNullOrWhiteSpace())
                {
                    return null;
                }
                sb.Append( $"{HomeStreet1} " );

                if (HomeStreet2.IsNotNullOrWhiteSpace())
                {
                    sb.Append( $"{HomeStreet2} " );
                }
                sb.Append( $"{HomeCity}, {HomeState} {HomePostalCode}" );
                return sb.ToString();
            }
        }

        [JsonIgnore]
        public string FullAddressGrid
        {
            get
            {
                if(Age < 18)
                {
                    return String.Empty;
                }
                else
                {
                    return FullAddress;
                }
            }
        }

        [JsonIgnore]
        public string FullAddressHtml
        {
            get
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                if (HomeStreet1.IsNullOrWhiteSpace())
                {
                    return null;
                }

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
        public string Grade
        {
            get
            {
                string grade = null;
                if (GraduationYear.HasValue)
                {
                    var gradeOffset = GraduationYear - RockDateTime.CurrentGraduationYear;

                    if (gradeOffset >= 0)
                    {
                        var gradeValue = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.SCHOOL_GRADES.AsGuid() )
                            .DefinedValues.Where( v => v.Value == gradeOffset.ToString() ).FirstOrDefault();

                        grade = gradeValue.GetAttributeValue( "Abbreviation" );
                    }
                }
                return grade;
            }

        }

        [JsonIgnore]
        public string MobilePhoneGridValue
        {
            get
            {
                if (Age < 18)
                {
                    return string.Empty;
                }
                else
                {
                    return MobilePhone;
                }
            }
        }




    }
    public partial class DecisionReportConfiguration : EntityTypeConfiguration<DecisionReport>
    {
        public DecisionReportConfiguration()
        {
            HasKey( s => s.WorkflowId );
            HasEntitySetName( "DecisionReport" );
        }
    }
}
