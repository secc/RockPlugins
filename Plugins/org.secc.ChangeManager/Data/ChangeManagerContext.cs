using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.secc.ChangeManager.Model;

namespace org.secc.ChangeManager.Data
{
    public class ChangeManagerContext : Rock.Data.DbContext
    {
        public DbSet<ChangeRecord> ChangeRecord { get; set; }
        public DbSet<ChangeRequest> ChangeRequest { get; set; }
        public ChangeManagerContext()
            : base( "RockContext" )
        {

        }

        protected override void OnModelCreating( DbModelBuilder modelBuilder )
        {
            // we don't want this context to create a database or look for EF Migrations, do set the Initializer to null
            Database.SetInitializer<ChangeManagerContext>( new NullDatabaseInitializer<ChangeManagerContext>() );

            Rock.Data.ContextHelper.AddConfigurations( modelBuilder );
            modelBuilder.Configurations.AddFromAssembly( System.Reflection.Assembly.GetExecutingAssembly() );
        }
    }
}
