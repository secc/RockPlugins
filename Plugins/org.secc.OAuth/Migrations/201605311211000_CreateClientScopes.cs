using Rock.Plugin;
using Rock;
using System.Text;

namespace org.secc.OAuth.Migrations
{

    [MigrationNumber(4, "1.4.5")]
    class CreateClientScopes : Rock.Plugin.Migration
    {
        public override void Up()
        {
            StringBuilder sql = new StringBuilder();
            sql.Append(@"DECLARE @ClientId INT;");
            sql.Append(@"DECLARE @ScopeId INT;");
            sql.Append(@"INSERT INTO [dbo].[_org_secc_OAuth_Client]
                   ([ClientName]
                   ,[ApiKey]
                   ,[ApiSecret]
                   ,[CallbackUrl]
                   ,[Active]
                   ,[CreatedDateTime]
                   ,[Guid])
                VALUES
                   ('Localhost Test'
                   ,'ABF391DA-27DE-495D-B8BE-7ED6DDE5CBDD'
                   ,'9FE03185-03A7-4A1B-AA64-4ADC64A0492F'
                   ,'https://localhost:44300/'
                   ,1
                   ,GETDATE()
                   ,NEWID());
                ");
            sql.Append(@"SET @ClientId = SCOPE_IDENTITY();");
            sql.Append(@"INSERT INTO [dbo].[_org_secc_OAuth_Scope]
                    ([Identifier]
                    , [Description]
                    , [Active]
                    , [CreatedDateTime]
                    , [Guid])
                VALUES(
                    'Profile'
                    , 'Allows access to user''s profile data (name, birthdate, email)'
                    , 1
                    ,GETDATE()
                    ,NEWID());
                ");
            sql.Append(@"SET @ScopeId = SCOPE_IDENTITY();");
            sql.Append(@"INSERT INTO [dbo].[_org_secc_OAuth_ClientScope]
                           ([ClientId]
                           ,[ScopeId]
                           ,[Active]
                           ,[CreatedDateTime]
                           ,[Guid])
                        VALUES
                            (@ClientId
                            ,@ScopeId
                            ,1
                            ,GETDATE()
                            ,NEWID());");

            Sql(@"INSERT INTO [dbo].[_org_secc_OAuth_Scope]
                    ([Identifier]
                    , [Description]
                    , [Active]
                    , [CreatedDateTime]
                    , [Guid])
                VALUES(
                    'Family'
                    , 'Allows access to profile data for the user''s family'
                    , 1
                    ,GETDATE()
                    ,NEWID());
                ");
            sql.Append(@"SET @ScopeId = SCOPE_IDENTITY();");
            sql.Append(@"INSERT INTO [dbo].[_org_secc_OAuth_ClientScope]
                           ([ClientId]
                           ,[ScopeId]
                           ,[Active]
                           ,[CreatedDateTime]
                           ,[Guid])
                        VALUES
                            (@ClientId
                            ,@ScopeId
                            ,1
                            ,GETDATE()
                            ,NEWID());");

            try { 
                Sql(sql.ToString());
            } catch (System.Exception e)
            {
                // Debug stuff
                throw new System.Exception(e.Message + " - " + sql.ToString());
            }
        }
        public override void Down()
        {
            Sql("Truncate [dbo].[_org_secc_OAuth_Authorization];");
            Sql("Truncate [dbo].[_org_secc_OAuth_ClientScope];");
            Sql("Truncate [dbo].[_org_secc_OAuth_Scope];");
            Sql("Truncate [dbo].[_org_secc_OAuth_Client];");
        }

    }
}
