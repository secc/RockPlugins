namespace org.secc.GroupManager.Migrations
{
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<org.secc.GroupManager.Data.GroupManagerContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed( org.secc.GroupManager.Data.GroupManagerContext context )
        {
        }
    }
}
