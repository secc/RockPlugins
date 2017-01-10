using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.FamilyCheckin.Exceptions
{
    public class CheckInStateLost : Exception
    {
        public CheckInStateLost()
            : base() { }

        public CheckInStateLost( string message )
            : base(message) { }

        public CheckInStateLost( string format, params object[] args )
            : base(string.Format(format, args)) { }

        public CheckInStateLost( string message, Exception innerException )
            : base(message, innerException) { }

        public CheckInStateLost( string format, Exception innerException, params object[] args )
            : base(string.Format(format, args), innerException) { }
    }
}
