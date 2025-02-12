namespace org.secc.GroupManager.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 11, "1.12.9" )]
    public partial class AddCampAllergyReportProc : Migration
    {

        public override void Up()
        {
            Sql( @"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE' AND ROUTINE_SCHEMA='dbo' AND ROUTINE_NAME='_org_secc_CampManager_GetGroupMemberAllergies')
                BEGIN
                    DROP PROCEDURE dbo._org_secc_CampManager_GetGroupMemberAllergies
                END
            " );

            Sql( @"
                CREATE PROCEDURE [dbo].[_org_secc_CampManager_GetGroupMemberAllergies]
                (
                    @GroupId  INT
                )
                AS

                DECLARE @RegistrantEntityTypeId INT = (SELECT TOP 1 Id FROM EntityType WHERE Name = 'Rock.Model.RegistrationRegistrant')

                CREATE TABLE #tmpAttributeKeys
                (
                    AttributeKey NVARCHAR(1000),
                    FriendlyName NVARCHAR(1000)
                )

                INSERT INTO #tmpAttributeKeys values('Eggs', 'Eggs')
                INSERT INTO #tmpAttributeKeys values('Fishsuchasbasscodflounder', 'Fish')
                INSERT INTO #tmpAttributeKeys values ('Milkdairy', 'Milk and Dairy')
                INSERT INTO #tmpAttributeKeys values ('Peanuts', 'Peanuts')
                INSERT INTO #tmpAttributeKeys values ('Treenutssuchasalmondscashewswalnuts', 'Tree Nuts')
                INSERT INTO #tmpAttributeKeys values ('Shellfishsuchascrabshrimp', 'Shellfish')
                INSERT INTO #tmpAttributeKeys values ('Soysoya', 'Soy')
                INSERT INTO #tmpAttributeKeys values ('Wheatgluten', 'Wheat and Gluten')
                INSERT INTO #tmpAttributeKeys values ('Listotherallergiesthatweneedtobeawareof', 'Other')

                SELECT ri.Name, registrants.RegistrationRegistrantId, g.Name as GroupName, gr.Name as GroupRole, p.Id,  p.LastName, p.NickName, ak.FriendlyName, av.[Value]
                INTO #tmpAllergies
                FROM GroupMember gm 
                INNER JOIN GroupTypeRole gr on gm.GroupRoleId = gr.Id
                INNER JOIN [Group] g on gm.GroupId = g.Id
                INNER JOIN Person p on gm.PersonId = p.Id
                INNER JOIN RelatedEntity re on gm.GroupId = re.TargetEntityId and re.TargetEntityTypeId = 16 and re.SourceEntityTypeId = 290
                INNER JOIN RegistrationInstance ri on re.SourceEntityId = ri.id
                INNER JOIN 
                    (
                        SELECT rr.Id as RegistrationRegistrantId, r.RegistrationInstanceId, pa.PersonId
                        FROM RegistrationRegistrant rr 
                        INNER JOIN PersonAlias pa on rr.PersonAliasId = pa.Id
                        INNER JOIN Registration r on rr.RegistrationId = r.Id
                    ) as registrants on ri.Id = registrants.RegistrationInstanceId and gm.PersonId = registrants.PersonId
                CROSS APPLY #tmpAttributeKeys ak     
                LEFT OUTER JOIN 
                    (
                        SELECT av.Id as AttributeValueId, a.Id as AttributeId, a.[Key], av.EntityId, av.[value]
                        FROM AttributeValue av 
                        INNER JOIN Attribute a on av.AttributeId = a.Id and a.EntityTypeId = @RegistrantEntityTypeId
                    ) av on registrants.RegistrationRegistrantId = av.EntityId and av.[Key] = ak.AttributeKey
                WHERE gm.GroupId = @GroupId
                    and ISNULL(av.[Value], '') <> ''
                ORDER BY LastName, NickName

                SELECT 
                    GroupName,
                    GroupRole,
                    Id as PersonId,
                    LastName,
                    NickName, 
                    GroupRole
                    [Eggs],
                    [Fish],
                    [Milk and Dairy],
                    [Peanuts],
                    [Tree Nuts],
                    [Shellfish],
                    [Soy],
                    [Wheat and Gluten],
                    [Other]
                FROM (
                    SELECT Id, LastName, NickName, FriendlyName, GroupName, GroupRole, ISNULL([Value],'') AS Value
                    FROM #tmpAllergies
                ) p
                PIVOT 
                (
                    MAX([Value])
                    FOR FriendlyName in ([Eggs],[Fish], [Milk and Dairy], [Peanuts], [Tree Nuts], [Shellfish], [Soy], [Wheat and Gluten], [Other])
                ) as pvt
                ORDER BY LastName, NickName

                drop table #tmpAttributeKeys
                drop table #tmpAllergies
            " );
        }
        public override void Down()
        {
            Sql( @"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE' AND ROUTINE_SCHEMA='dbo' AND ROUTINE_NAME='_org_secc_CampManager_GetGroupMemberAllergies')
                BEGIN
                    DROP PROCEDURE dbo._org_secc_CampManager_GetGroupMemberAllergies
                END
            " );
        }


    }
}
