using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Activation;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace org.secc.Rise.Response
{
    [Url( "invitations" )]
    public class RiseInvitation : RiseBase
    {
        [JsonProperty( "id" )]
        public string Id { get; set; }

        [JsonProperty( "email" )]
        public string Email { get; set; }

        [JsonProperty( "role" )]
        public string Role { get; set; }

        [JsonProperty( "firstName" )]
        public string FirstName { get; set; }

        [JsonProperty( "lastName" )]
        public string LastName { get; set; }

        [JsonProperty( "groups" )]
        public List<string> Groups { get; set; }

    }
}
