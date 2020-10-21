using System.Data.Entity;
using org.secc.Rise.Model;

namespace org.secc.Rise.Data
{
    public class RiseContext : Rock.Data.DbContext
    {
        public DbSet<Course> Courses { get; set; }

        public RiseContext()
         : base( "RockContext" ) { }

        protected override void OnModelCreating( DbModelBuilder modelBuilder )
        {
            // we don't want this context to create a database or look for EF Migrations, do set the Initializer to null
            Database.SetInitializer<RiseContext>( new NullDatabaseInitializer<RiseContext>() );

            Rock.Data.ContextHelper.AddConfigurations( modelBuilder );
            modelBuilder.Configurations.AddFromAssembly( System.Reflection.Assembly.GetExecutingAssembly() );
        }
    }
}

