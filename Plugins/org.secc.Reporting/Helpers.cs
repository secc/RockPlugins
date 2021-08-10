using System.Security.Cryptography;
using System.Text;

namespace org.secc.Reporting
{
    public static class Helpers
    {
        public static string Md5Hash( string str )
        {
            using ( var crypt = MD5.Create() )
            {
                var hash = crypt.ComputeHash( Encoding.UTF8.GetBytes( str ) );

                StringBuilder sb = new StringBuilder();
                foreach ( byte b in hash )
                {
                    // Can be "x2" if you want lowercase
                    sb.Append( b.ToString( "x2" ) );
                }
                return sb.ToString();
            }
        }
    }
}
