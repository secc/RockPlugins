using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Data;

namespace org.secc.Widgities.Model
{

    public class WidgityTypeService : Service<WidgityType>
    {
        public WidgityTypeService( RockContext context ) : base( context ) { }
    }
}
