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

using Rock.Plugin;

namespace org.secc.Reporting.Migrations
{
	[MigrationNumber(3,"1.12.0")]
    public partial class DecisionReportView : Migration
    {
        public override void Down()
        {
            var dropView = @"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.VIEWS WHERE TABLE_SCHEMA = 'dbo' and TABLE_NAME='_org_secc_DecisionForm_Analytics')
                BEGIN 
                    DROP VIEW [dbo].[_org_secc_DecisionForm_Analytics]
                END
            ";

			Sql( dropView );
        }

        public override void Up()
        {
            var dropView = @"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.VIEWS WHERE TABLE_SCHEMA = 'dbo' and TABLE_NAME='_org_secc_DecisionForm_Analytics')
                BEGIN 
                    DROP VIEW [dbo].[_org_secc_DecisionForm_Analytics]
                END
            ";

            var insertView = @"
				CREATE VIEW [dbo].[_org_secc_DecisionForm_Analytics]
				as

				SELECT 
					a.WorkflowId as Id,
					a.WorkflowId,
					a.PersonAliasId,
					a.PersonId,
					a.LastName,
					a.FirstName,
					a.NickName,
					a.Age,
					CASE  when a.Age < 18 then 1 else 0 end as IsMinor,
					a.Gender,
					a.Email,
					a.MobilePhone,
					a.GraduationYear,
					a.HomeLocationId,
					a.ConnectionStatusValueId,
					a.ConnectionStatusValue,
					a.BaptismDate,
					a.DecisionType,
					a.FormDate,
					a.DecisionCampusId,
					a.DecisionCampusName,
					a.FamilyCampusId,
					a.FamilyCampusName,
					a.EventName,
					a.BaptismTypeValueId,
					a.BaptismTypeValue,
					a.ParentGuardianName,
					a.ParentEmail,
					a.ParentPhone,
					a.StatementOfFaithSignedDate,
					a.MembershipDate,
					a.MembershipClassDate,
					l.Street1 as HomeStreet1,
					l.Street2 as HomeStreet2,
					l.City as  HomeCity,
					l.State as HomeState,
					l.PostalCode as HomePostalCode,
					l.Country as HomeCountry,
					a.Guid,
					a.ForeignId,
					a.ForeignKey,
					a.ForeignGuid,
					a.CreatedByPersonAliasId,
					a.ModifiedByPersonAliasId,
					a.CreatedDateTime,
					a.ModifiedDateTime
				FROM 
				(
					SELECT
						w.Id as WorkflowId,
						pa.Id as PersonAliasId,
						p.Id as PersonId,
						p.LastName,
						p.FirstName,
						p.NickName,
						dbo.ufnCrm_GetAge(p.Birthdate) as Age,
						CASE p.Gender WHEN 1 THEN 'M' WHEN 2 THEN 'F' ELSE NULL END AS Gender,
						p.Email,
						(SELECT TOP 1 pn.NumberFormatted FROM PhoneNumber pn where pn.PersonId = p.Id and pn.NumberTypeValueId = 12 and pn.IsUnlisted = 0) as MobilePhone,
						p.GraduationYear,
						(SELECT TOP 1 LocationId from GroupLocation where GroupLocationTypeValueId = 19 and GroupId = p.PrimaryFamilyId order by ModifiedDateTime) as HomeLocationId,
						p.ConnectionStatusValueId,
						connectStatus.Value as ConnectionStatusValue,
						(SELECT TOP 1 
							s1.CompletedDateTime
						 FROM Step s1
						 INNER JOIN PersonAlias pa1 on s1.PersonAliasId = pa.Id
						 WHERE s1.StepTypeId = 5 
							and pa1.PersonId = p.Id
						 ORDER BY CompletedDateTime ) as BaptismDate,
						 decisionType.Value as DecisionType,
						 w.CreatedDateTime as FormDate,
						 decisionCampus.Id as DecisionCampusId,
						 decisionCampus.Name as DecisionCampusName,
						 familyCampus.Id as FamilyCampusId,
						 familyCampus.Name as FamilyCampusName,
						 eventValue.Value as EventName,
						 baptismType.Id as BaptismTypeValueId,
						 baptismType.Value as BaptismTypeValue,
						 parentGuardianName.Value as ParentGuardianName,
						 parentEmail.Value as ParentEmail,
						 parentPhone.Value as ParentPhone,
						 sof.ValueAsDateTime as StatementOfFaithSignedDate,
						(SELECT TOP 1 
							s1.CompletedDateTime
						 FROM Step s1
						 INNER JOIN PersonAlias pa1 on s1.PersonAliasId = pa.Id
						 WHERE pa1.PersonId = p.Id and StepTypeId = 6
						 ORDER BY CompletedDateTime ) as MembershipDate,
						 (SELECT TOP 1 a.StartDateTime 
							FROM Attendance a 
							INNER JOIN PersonAlias pa on a.PersonAliasId = pa.Id 
							INNER JOIN AttendanceOccurrence ao on a.OccurrenceId = ao.Id 
							INNER JOIN [Group] g on ao.GroupId = g.Id 
							WHERE a.DidAttend = 1 and pa.PersonId = p.Id and g.GroupTypeId = 269
							Order by a.StartDateTime) as MembershipClassDate,
						w.Guid,
						w.ForeignId,
						w.ForeignKey,
						w.ForeignGuid,
						w.CreatedByPersonAliasId,
						w.ModifiedByPersonAliasId,
						w.CreatedDateTime,
						w.ModifiedDateTime
					FROM Workflow w
					INNER JOIN AttributeValue personAV on w.Id = personAV.EntityId and personAV.AttributeId = 190707 
					INNER JOIN PersonAlias pa on TRY_CAST(personAV.Value as UNIQUEIDENTIFIER) = pa.Guid
					INNER JOIN Person p on pa.PersonId = p.Id
					LEFT OUTER JOIN DefinedValue connectStatus on p.ConnectionStatusValueId = connectStatus.Id 
					LEFT OUTER JOIN AttributeValue decisionType on w.Id = decisionType.EntityId and decisionType.AttributeId = 190875
					LEFT OUTER JOIN AttributeValue decisionCampusValue on w.Id = decisionCampusValue.EntityId and decisionCampusValue.AttributeId = 190709
					LEFT OUTER JOIN AttributeValue eventValue on w.Id = eventValue.EntityId and eventValue.AttributeId = 190708
					LEFT OUTER JOIN AttributeValue as baptismTypeValue on w.Id = baptismTypeValue.EntityId and baptismTypeValue.AttributeId = 192125
					LEFT OUTER JOIN DefinedValue as baptismType on TRY_CAST(baptismTypeValue.Value as UNIQUEIDENTIFIER) = baptismType.Guid
					LEFT OUTER JOIN AttributeValue as parentGuardianName on w.Id = parentGuardianName.EntityId and parentGuardianName.AttributeId = 190713
					LEFT OUTER JOIN AttributeValue as parentEmail on w.Id = parentEmail.EntityId and parentEmail.AttributeId = 190696
					LEFT OUTER JOIN AttributeValue as parentPhone on w.Id = parentPhone.EntityId and parentPhone.AttributeId = 190697
					LEFT OUTER JOIN AttributeValue as sof on p.Id = sof.EntityId and sof.AttributeId = 4031
					LEFT OUTER JOIN Campus decisionCampus on TRY_CAST(decisionCampusValue.Value as UNIQUEIDENTIFIER) = decisionCampus.Guid
					LEFT OUTER JOIN Campus familyCampus on p.PrimaryCampusId = familyCampus.Id
					WHERE w.WorkflowTypeId = 762
				) AS a
				LEFT OUTER JOIN dbo.[Location] l on a.HomeLocationId = l.id
            ";

			Sql( dropView );
			Sql( insertView );
        }
    }
}
