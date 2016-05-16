using org.secc.OAuth.Data;
using System;
using System.Data.Entity;
using System.Linq;

namespace org.secc.OAuth.Model
{
    public class ClientService : OAuthService<Client>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientService"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public ClientService(OAuthContext context) : base( context ) { }

        public Client GetByApiKey(Guid apiKey)
        {
            return Queryable().FirstOrDefault(t => t.ApiKey == apiKey);
        }
    }
}
