using System;

namespace org.secc.Purchasing.Intacct.Model
{
    public class IntacctException : System.Exception
    {
        public IntacctException() { }

        public IntacctException( string message ) : base( message ) { }

        public IntacctException( string message, Exception innerException )
            : base( message, innerException ) { }
    }
}
