namespace org.secc.Communication.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 2, "1.10.0" )]
    public partial class Index : Migration
    {
        public override void Up()
        {
            Sql( "ALTER TABLE [dbo].[_org_secc_Communication_TwilioHistory] ADD CONSTRAINT [PK_dbo.org.secc.Communication.TwilioHistory] PRIMARY KEY CLUSTERED (Id)" );
            Sql( "CREATE INDEX [IX_SID] ON [dbo].[_org_secc_Communication_TwilioHistory] (SID)" );
            Sql( "CREATE INDEX [IX_DateCreated] ON [dbo].[_org_secc_Communication_TwilioHistory] (DateCreated)" );
        }

        public override void Down()
        {

        }
    }
}
