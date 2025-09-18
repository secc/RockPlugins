using Rock.Plugin;

namespace org.secc.OAuth.Migrations
{
    [MigrationNumber(5, "1.13.0")]
    public class CreateTokenStorage : Migration
    {
        public override void Up()
        {
            //Create Token Storage
            Sql(@"
                CREATE TABLE [dbo].[_org_secc_OAuth_Token](
	                [Id] [int] IDENTITY(1,1) NOT NULL,
	                [Token] [nvarchar](100) NOT NULL,
	                [Ticket] [nvarchar](500) NOT NULL,
	                [DateCreated] [datetime] NOT NULL,
                 CONSTRAINT [PK__org_secc_OAuth_Token] PRIMARY KEY CLUSTERED 
                (
	                [Id] ASC
                )WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
                ) ON [PRIMARY]
            ");

            //Add Unique Index
            Sql(@"
                CREATE UNIQUE NONCLUSTERED INDEX [IX__org_secc_OAuth_Token] ON [dbo].[_org_secc_OAuth_Token]
                (
	                [Token] ASC
                )WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
            ");

            //Add Add SP
            Sql(@"
                CREATE PROCEDURE [dbo].[_org_secc_OAuth_spAddToken]
                (
	                @Token NVARCHAR(100),
	                @Ticket NVARCHAR(500)
                )
                AS
                BEGIN
	                INSERT INTO dbo._org_secc_OAuth_Token
	                (
		                Token,
		                Ticket,
		                DateCreated
	                )
	                VALUES
	                (
		                @Token,
		                @Ticket,
		                dbo._org_secc_Utility_GetDateLocalTime()
	                )
                END
            ");

            //Add Read Ticket SP
            Sql(@"
                CREATE PROCEDURE [dbo].[_org_secc_OAuth_spGetTicket]
                (
	                @Token NVARCHAR(100)
                )
                AS
                BEGIN

                SELECT  Ticket
                FROM dbo._org_secc_OAuth_Token
                WHERE Token = @Token

                END
            ");

            //Add Delete Token SP
            Sql(@"
                CREATE PROCEDURE [dbo].[_org_secc_OAuth_spDeleteToken]
                (
	                @Token NVARCHAR(100)
                )
                AS
                BEGIN
	                DELETE 
	                FROM dbo._org_secc_OAuth_Token
	                WHERE Token = @Token
                END
            ");
        }

        public override void Down()
        {
            Sql(@"DROP PROCEDURE [dbo].[_org_secc_OAuth_spAddToken]");
            Sql(@"DROP PROCEDURE [dbo].[_org_secc_OAuth_spGetTicket]");
            Sql(@"DROP PROCEDURE [dbo].[_org_secc_OAuth_spDeleteToken]");
            Sql(@"DROP TABLE [dbo].[_org_secc_OAuth_Token]");
        }
    }
}
