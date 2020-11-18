// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System.Data.Entity;
using org.secc.xAPI.Model;

namespace org.secc.xAPI.Data
{
    public class xAPIContext : Rock.Data.DbContext
    {
        public DbSet<Experience> Experiences { get; set; }

        public DbSet<ExperienceObject> ExperienceObjects { get; set; }

        public DbSet<ExperienceQualifier> ExperienceQualifiers { get; set; }

        public DbSet<ExperienceResult> ExperienceResults { get; set; }

        public xAPIContext()
         : base( "RockContext" ) { }

        protected override void OnModelCreating( DbModelBuilder modelBuilder )
        {
            // we don't want this context to create a database or look for EF Migrations, do set the Initializer to null
            Database.SetInitializer<xAPIContext>( new NullDatabaseInitializer<xAPIContext>() );

            Rock.Data.ContextHelper.AddConfigurations( modelBuilder );
            modelBuilder.Configurations.AddFromAssembly( System.Reflection.Assembly.GetExecutingAssembly() );
        }
    }
}

