using System.Data.Entity;
using org.secc.SystemsMonitor.Model;

namespace org.secc.SystemsMonitor.Data
{
    public class SystemsMonitorContext : Rock.Data.DbContext
    {
        public DbSet<SystemTest> SystemTests { get; set; }
        public DbSet<SystemTestHistory> SystemTestHistories { get; set; }

        public SystemsMonitorContext()
         : base( "RockContext" ) { }

        protected override void OnModelCreating( DbModelBuilder modelBuilder )
        {
            // we don't want this context to create a database or look for EF Migrations, do set the Initializer to null
            Database.SetInitializer<SystemsMonitorContext>( new NullDatabaseInitializer<SystemsMonitorContext>() );

            Rock.Data.ContextHelper.AddConfigurations( modelBuilder );
            modelBuilder.Configurations.AddFromAssembly( System.Reflection.Assembly.GetExecutingAssembly() );
        }
    }
}

