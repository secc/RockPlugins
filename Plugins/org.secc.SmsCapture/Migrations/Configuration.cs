namespace org.secc.SmsCapture.Migrations
{
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<org.secc.SmsCapture.Data.SmsCaptureContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed( org.secc.SmsCapture.Data.SmsCaptureContext context )
        {
            //  This method will be called after migrating to the latest version.
        }
    }
}
