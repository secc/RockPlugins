namespace org.secc.FamilyCheckin.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 3, "1.4.0" )]
    public partial class PhoneNumberReversed : Migration
    {
        public override void Up()
        {
            Sql( @"IF COL_LENGTH('PhoneNumber', 'NumberReversed') IS NULL ALTER TABLE [dbo].[PhoneNumber] ADD [NumberReversed] AS( reverse([Number] ) ) PERSISTED
                   GO
                   IF NOT EXISTS( SELECT * FROM sys.indexes WHERE name = 'IX_NumberReversed' AND object_id = OBJECT_ID( N'[dbo].[PhoneNumber]' ) ) CREATE NONCLUSTERED INDEX[IX_NumberReversed] ON[dbo].[PhoneNumber]([NumberReversed]) 
                   GO");
        }
        public override void Down()
        {
            DropIndex( "dbo.PhoneNumber", "IX_NumberReversed" );
            DropColumn( "dbo.PhoneNumber", "NumberReversed" );
        }
    }
}
