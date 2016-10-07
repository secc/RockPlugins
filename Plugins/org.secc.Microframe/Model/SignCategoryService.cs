using org.secc.Microframe.Data;
using System.Data.Entity;
using System.Linq;
using Rock.Data;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace org.secc.Microframe.Model
{
    public class SignCategoryService : MicroframeService<SignCategory>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="KioskService"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public SignCategoryService( RockContext context ) : base( context ) { }
    }
}
