using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.secc.Rise.Response;

namespace org.secc.Rise
{
    public class RiseClient
    {

        public RiseClient( )
        {
          
        }

        public IEnumerable<RiseUser> GetUsers()
        {
            return ClientManager.GetSet<RiseUser>();
        }

        public IEnumerable<RiseGroup> GetGroups()
        {
            return ClientManager.GetSet<RiseGroup>();
        }

        public RiseUser GetUser( string id )
        {
            return ClientManager.Get<RiseUser>( id );
        }
    }
}
