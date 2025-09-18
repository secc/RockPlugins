using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.OAuth.Model
{
    [DataObject]
    internal class AuthToken
    {
        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public string Token { get; set; }
        [DataMember]
        public string Ticket { get; set; }
        [DataMember]
        public string DateCreated { get; set; }
    }
}
