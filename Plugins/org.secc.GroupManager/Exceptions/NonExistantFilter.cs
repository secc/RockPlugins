using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.GroupManager.Exceptions
{
    class NonExistantFilter : Exception
    {
        public NonExistantFilter()
            : base() { }

        public NonExistantFilter( string message )
            : base(message) { }
    }
}
