using org.secc.FamilyCheckin.Data;
using System.Data.Entity;
using System.Linq;
using Rock.Data;

namespace org.secc.FamilyCheckin.Model
{
    public class KioskTypeService : FamilyCheckinService<KioskType>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="KioskTypeService"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public KioskTypeService(RockContext context) : base( context ) { }
    }
}
