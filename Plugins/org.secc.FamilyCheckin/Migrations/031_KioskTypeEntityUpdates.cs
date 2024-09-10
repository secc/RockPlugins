using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock.Plugin;

namespace org.secc.FamilyCheckin.Migrations
{
    [MigrationNumber(31,"1.13.0")]
    public partial class KioskTypeEntityUpdates : Migration
    {
        public override void Up()
        {
            Sql( @"UPDATE EntityType SET Name = 'org.secc.FamilyCheckin.Model.CheckinKioskType' WHERE Name = 'org.secc.FamilyCheckin.Model.KioskType'" );
            Sql( @"UPDATE EntityType SET Name = 'org.secc.FamilyCheckin.Cache.CheckinKioskTypeCache' WHERE Name = 'org.secc.FamilyCheckin.Cache.KioskTypeCache'" );
        }

        public override void Down()
        {
            Sql( @"UPDATE EntityType SET Name = 'org.secc.FamilyCheckin.Model.KioskType' WHERE Name = 'org.secc.FamilyCheckin.Model.CheckinKioskType'" );
            Sql( @"UPDATE EntityType SET Name = 'org.secc.FamilyCheckin.Cache.KioskTypeCache' WHERE Name = 'org.secc.FamilyCheckin.Cache.CheckinKioskTypeCache'" );

        }


    }
}
