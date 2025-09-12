using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
namespace org.secc.Jobs.Event
{
    [TextField("Destination Campaign Name",
        Description = "Enter the year or campaign name for the copied schools.",
        IsRequired = true,
        Key = AttributeKeys.DestinationCampaignKey)]
    [CustomDropdownListField("Source Campaign",
        Description = "Year/Campaign to copy from.",
        ListSource = SourceCampaignSql,
        IsRequired = true,
        Key = AttributeKeys.SourceCampaignKey)]
    [BooleanField("Copy Inactive Schools",
        Description = "Should inactive school records be copied. Default is false.",
        DefaultBooleanValue = false,
        IsRequired = false,
        Key = AttributeKeys.CopyInactiveSchoolsKey)]

    [DisplayName("Community Gives Back - Copy Schools")]
    [DisallowConcurrentExecution]
    public class CopyCommunityGivesBackSchools : IJob
    {
        internal class AttributeKeys
        {
            internal const string DestinationCampaignKey = "CampaignName";
            internal const string CopyInactiveSchoolsKey = "CopyInactiveSchools";
            internal const string SourceCampaignKey = "SourceCampaign";
        }

        public const string SourceCampaignSql = @"
            SELECT DISTINCT av.[Value] as Value, av.[Value] as Text
            FROM AttributeValue av 
            INNER JOIN Attribute a on av.AttributeId = a.Id
            WHERE a.[Guid] = 'BBE01EA2-357C-4289-AFF9-585CB5B3B88C'";

        Guid schoolDefinedValueGuid = Guid.Parse("71bb065c-4368-439d-beab-8539c19c8c99");

        string sourceCampaignName = "";
        string destinationCampaignName = "";
        bool copyInactiveSchools = false;
        int? systemAdminPersonAliasId = 0;

        public void Execute(IJobExecutionContext context)
        {
            var dataMap = context.JobDetail.JobDataMap;

            sourceCampaignName = dataMap.GetString(AttributeKeys.SourceCampaignKey);
            destinationCampaignName = dataMap.GetString(AttributeKeys.DestinationCampaignKey);
            copyInactiveSchools = dataMap.GetString(AttributeKeys.CopyInactiveSchoolsKey).AsBoolean();
            systemAdminPersonAliasId = new PersonService(new RockContext()).Get(1).PrimaryAliasId;

            if (sourceCampaignName.IsNullOrWhiteSpace())
            {
                throw new Exception("Source Campaign Name is required.");
            }

            if (destinationCampaignName.IsNullOrWhiteSpace())
            {
                throw new Exception("Destination Campaign Name is required.");
            }

            var schools = GetSchoolList();

            foreach ( var school in schools )
            {
                CopySchool(school);
            }
        }

        private void CopySchool(SchoolItem school)
        {
            var rockContext = new RockContext();
            var dvService = new DefinedValueService(rockContext);

            var definedValue = new DefinedValue()
            {
                IsSystem = false,
                DefinedTypeId = school.DefinedTypeId,
                Value = school.Name,
                Guid = Guid.NewGuid(),
                CreatedDateTime = RockDateTime.Now,
                ModifiedDateTime = RockDateTime.Now,
                CreatedByPersonAliasId = systemAdminPersonAliasId,
                ModifiedByPersonAliasId = systemAdminPersonAliasId,
                IsActive = school.IsActive
            };

            dvService.Add(definedValue);
            rockContext.SaveChanges();

            definedValue.LoadAttributes(rockContext);
            definedValue.SetAttributeValue("ResourceTeacherName", school.TeacherName);
            definedValue.SetAttributeValue("ResourceTeacherEmail", school.TeacherEmail);
            definedValue.SetAttributeValue("SponsorshipsAvailable", school.SponsorshipsAvailable);
            definedValue.SetAttributeValue("Year", destinationCampaignName);

            definedValue.SaveAttributeValues(rockContext);
            //rockContext.SaveChanges();
        }

        private List<SchoolItem> GetSchoolList()
        {
            var rockContext = new RockContext();
            var schoolDefinedType = DefinedTypeCache.Get(schoolDefinedValueGuid);
            var schoolDTIDAsString = schoolDefinedType.Id.ToString();
            var definedValueETId = EntityTypeCache.Get(typeof(DefinedValue)).Id;

            var avQry = new AttributeValueService(rockContext).Queryable()
                .AsNoTracking()
                .Where(a => a.Attribute.EntityTypeId == definedValueETId)
                .Where(a => a.Attribute.EntityTypeQualifierColumn == "DefinedTypeId")
                .Where(a => a.Attribute.EntityTypeQualifierValue == schoolDTIDAsString);

            var schoolQry = new DefinedValueService(rockContext).Queryable()
                .AsNoTracking()
                .Where(v => v.DefinedTypeId == schoolDefinedType.Id)
                .Join(avQry, dv => dv.Id, av => av.EntityId,
                    (dv, av) => new
                    {
                        School = dv,
                        AttributeValue = av
                    });

            if (!copyInactiveSchools)
            {
                schoolQry = schoolQry.Where(v => v.School.IsActive);
            }

            var schoolTmp = schoolQry.ToList();

            var schools = schoolQry.Select(s => new
            {
                Id = s.School.Id,
                DefinedTypeId = s.School.DefinedTypeId,
                Name = s.School.Value,
                Key = s.AttributeValue.Attribute.Key,
                ValueString = s.AttributeValue.Value,
                ValueNumeric = s.AttributeValue.ValueAsNumeric,
                IsActive = s.School.IsActive
            })
            .GroupBy(s => s.Id)
            .Select(s => new
            {
                Id = s.Key,
                Name = s.Select(s1 => s1.Name).FirstOrDefault(),
                Teacher = s.Where(s1 => s1.Key == "ResourceTeacherName").Select(s1 => s1.ValueString).FirstOrDefault(),
                TeacherEmail = s.Where(s1 => s1.Key == "ResourceTeacherEmail").Select(s1 => s1.ValueString).FirstOrDefault(),
                SponsorshipsAvailable = s.Where(s1 => s1.Key == "SponsorshipsAvailable").Select(s1 => s1.ValueNumeric).FirstOrDefault(),
                Year = s.Where(s1 => s1.Key == "Year").Select(s1 => s1.ValueString).FirstOrDefault(),
                IsActive = s.Select(s1 => s1.IsActive).FirstOrDefault(),
                DefinedTypeId = s.Select(s1 => s1.DefinedTypeId).FirstOrDefault()
            })
            .Where(s => s.Year == sourceCampaignName)
            .ToList()
            .Select(s => new SchoolItem
            {
                Id = s.Id,
                Name = s.Name,
                TeacherName = s.Teacher,
                TeacherEmail = s.TeacherEmail,
                SponsorshipsAvailable = (int?)s.SponsorshipsAvailable,
                IsActive = s.IsActive,
                Campaign = s.Year,
                DefinedTypeId = s.DefinedTypeId
            }).ToList();


            return schools;
        }

        class SchoolItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string TeacherName { get; set; }
            public string TeacherEmail { get; set; }
            public int? SponsorshipsAvailable { get; set; }
            public bool IsActive { get; set; }
            public string Campaign { get; set; }
            public int DefinedTypeId { get; set; }
        }
    }
}

