using System.Data.Entity;
using System.Linq;
using Rock.Data;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using org.secc.Microframe.Data;

namespace org.secc.Microframe.Model
{
    public class SignService : MicroframeService<Sign>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="KioskService"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public SignService(RockContext context) : base( context ) { }

    }
}
