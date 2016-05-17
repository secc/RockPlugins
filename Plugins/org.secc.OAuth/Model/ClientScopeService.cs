using org.secc.OAuth.Data;
using System;
using System.Data.Entity;
using System.Linq;

namespace org.secc.OAuth.Model
{
    public class ClientScopeService : OAuthService<Client>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientScopeService"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public ClientScopeService(OAuthContext context) : base( context ) { }
        
    }
}
