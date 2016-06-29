using org.secc.FamilyCheckin.Data;
using System.Data.Entity;
using System.Linq;
using Rock.Data;

namespace org.secc.FamilyCheckin.Model
{
    public class KioskService : FamilyCheckinService<Kiosk>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="KioskService"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public KioskService(RockContext context) : base( context ) { }
    }
}
