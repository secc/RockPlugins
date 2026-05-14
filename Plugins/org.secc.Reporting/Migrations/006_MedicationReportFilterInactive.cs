using Rock.Plugin;

namespace org.secc.Reporting.Migrations
{
    /// <summary>
    /// Re-creates _org_secc_CampManager_GetMedicationReport with a filter that excludes
    /// medications marked inactive (the MedicationActive attribute on the matrix template
    /// added for ROCK-8328 follow-up). Items with no stored MedicationActive value are
    /// treated as active to match the AsBoolean(true) default in the C# consumers.
    /// </summary>
    [MigrationNumber( 6, "1.12.0" )]
    public class MedicationReportFilterInactive : Migration
    {

        public override void Up()
        {
            Sql( @"
                IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_SCHEMA='dbo' and  ROUTINE_NAME = '_org_secc_CampManager_GetMedicationReport' and ROUTINE_TYPE='PROCEDURE')
                BEGIN
	                DROP PROCEDURE [dbo].[_org_secc_CampManager_GetMedicationReport]
                END
            " );
            Sql( @"
                CREATE PROCEDURE [dbo].[_org_secc_CampManager_GetMedicationReport]
                (
                    @RegistrationTemplateId INT,
                    @RegistrationInstanceId INT = NULL
                )
                AS

                DECLARE @MedicationPA INT  = 67024
                DECLARE @MedicationNameMXA INT = 42457
                DECLARE @MedicationInsructionMXA INT = 42592
                DECLARE @MedicationScheduleMXA INT = 42458

                -- Look up MedicationActive attribute id at runtime so this migration
                -- works on environments where the attribute id differs.
                DECLARE @MedicationActiveMXA INT = (
                    SELECT TOP 1 a.Id
                    FROM Attribute a
                    INNER JOIN AttributeMatrixTemplate amt
                        ON CAST(amt.Id AS NVARCHAR(20)) = a.EntityTypeQualifierValue
                    WHERE a.[Key] = 'MedicationActive'
                      AND a.EntityTypeQualifierColumn = 'AttributeMatrixTemplateId'
                      AND amt.[Guid] = 'd2ed9b3d-309a-4a70-bda4-2c090acd1384'
                )

                DECLARE @CampSmallGroupTypeId INT = 445

                CREATE TABLE #tmpSmallGroups
                (
                    InstanceId INT,
                    GroupId INT
                )

                CREATE TABLE #tmpCamperMeds
                (
                    Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
                    PersonId INT,
                    LastName NVARCHAR(50),
                    NickName NVARCHAR(50),
                    Gender INT,
                    BirthDate DATE,
                    RegistrationTemplateId INT,
                    RegistrationTemplateName NVARCHAR(100),
                    RegistrationInstanceId INT,
                    RegistrationInstanceName NVARCHAR(100),
                    Medication NVARCHAR(MAX),
                    Instructions NVARCHAR(MAX),
                    Schedule NVARCHAR(MAX),
                    GroupId INT,
                    GroupName NVARCHAR(100),
                    LeaderName NVARCHAR(100)
                )


                insert into #tmpSmallGroups
                (
                    InstanceId,
                    GroupId
                )
                select re.SourceEntityId , re.TargetEntityId
                from RegistrationTemplatePlacement p
                inner join RelatedEntity re on TRY_CAST(p.id as nvarchar(2000)) = re.QualifierValue
                WHERE p.RegistrationTemplateId = @RegistrationTemplateId
                    and p.GroupTypeId = @CampSmallGroupTypeId


                INSERT INTO #tmpCamperMeds
                (
                    PersonId,
                    LastName,
                    NickName,
                    Gender,
                    BirthDate,
                    RegistrationTemplateId,
                    RegistrationTemplateName,
                    RegistrationInstanceId,
                    RegistrationInstanceName,
                    Medication,
                    Instructions,
                    Schedule,
                    GroupId
                )
                SELECT
                    p.Id,
                    p.LastName,
                    p.NickName,
                    p.Gender,
                    p.BirthDate,
                    rt.Id as RegistrationTemplateId,
                    rt.Name as RegistrationTemplateName,
                    ri.Id as RegistrationInstanceId,
                    ri.Name as RegistrationInstanceName,
                    medication.[Value] as Medication,
                    medInstruction.[Value] as Instructions,
                    medSchedule.[Value] as Schedule,
                    (
                        SELECT TOP 1 gm.GroupId
                        FROM #tmpSmallGroups sg
                        INNER JOIN GroupMember gm on sg.GroupId = gm.GroupId  and sg.InstanceId = ri.Id
                        WHERE gm.PersonId = p.Id
                            and gm.GroupMemberStatus = 1
                    ) as GroupId
                FROM  RegistrationRegistrant rr
                INNER JOIN Registration r on rr.RegistrationId = r.Id
                INNER JOIN RegistrationInstance ri on r.RegistrationInstanceId = ri.Id
                INNER JOIN RegistrationTemplate rt on ri.RegistrationTemplateId = rt.Id
                INNER JOIN PersonAlias pa on rr.PersonAliasId = pa.Id
                INNER JOIN Person p on pa.PersonId = p.id
                INNER JOIN AttributeValue av on p.Id = av.EntityId and av.AttributeId = @MedicationPA
                INNER JOIN AttributeMatrix ax on TRY_CAST(av.VALUE AS uniqueidentifier) = ax.Guid
                INNER JOIN AttributeMatrixItem axi on ax.Id = axi.AttributeMatrixId
                LEFT OUTER JOIN AttributeValue medication on axi.Id = medication.EntityId and medication.AttributeId = @MedicationNameMXA
                LEFT OUTER JOIN AttributeValue medInstruction on axi.Id = medInstruction.EntityId and medInstruction.AttributeId = @MedicationInsructionMXA
                LEFT OUTER JOIN AttributeValue medSchedule on axi.Id = medSchedule.EntityId and medSchedule.AttributeId = @MedicationScheduleMXA
                LEFT OUTER JOIN AttributeValue medActive on axi.Id = medActive.EntityId and medActive.AttributeId = @MedicationActiveMXA
                WHERE rr.OnWaitList = 0
                    and rt.id = @RegistrationTemplateId
                    AND (ISNULL(@RegistrationInstanceId,0) = 0  or ri.Id = @RegistrationInstanceId)
                    AND (medActive.[Value] IS NULL OR medActive.[Value] != 'False')

                UPDATE t
                SET
                    t.GroupName = g.Name,
                    t.LeaderName = (
                        SELECT TOP 1 p.NickName + ' ' + p.LastName
                        FROM GroupMember gm
                        INNER JOIN GroupTypeRole gr on gm.GroupRoleId = gr.Id and gr.isleader = 1
                        INNER JOIN Person p on gm.PersonId = p.Id
                        WHERE gm.GroupMemberStatus = 1 and groupId = t.GroupId)
                FROM #tmpCamperMeds t
                INNER JOIN [Group] g on t.GroupId = g.Id

                SELECT
                    Id,
                    PersonId,
                    LastName,
                    NickName,
                    Gender,
                    Birthdate,
                    RegistrationTemplateId,
                    RegistrationTemplateName,
                    RegistrationInstanceId,
                    RegistrationInstanceName,
                    Medication,
                    Instructions,
                    Schedule,
                    GroupId,
                    GroupName,
                    LeaderName
                FROM #tmpCamperMeds
                ORDER BY LastName,
                    NickName,
                    PersonId

                DROP TABLE #tmpCamperMeds
                DROP TABLE #tmpSmallGroups
            " );
        }

        public override void Down()
        {
            // Restoring previous SP body would re-introduce the bug where inactive meds are exported.
            // Drop it; re-running migration 005 will re-create the original.
            Sql( @"
                IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_SCHEMA='dbo' and  ROUTINE_NAME = '_org_secc_CampManager_GetMedicationReport' and ROUTINE_TYPE='PROCEDURE')
                BEGIN
	                DROP PROCEDURE [dbo].[_org_secc_CampManager_GetMedicationReport]
                END
            " );
        }


    }
}
