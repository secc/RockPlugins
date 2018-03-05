// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
namespace org.secc.OAuth.Model
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Runtime.Serialization;
    [Table("_org_secc_OAuth_Client")]
    public partial class Client : Rock.Data.Model<Client>, Rock.Security.ISecured
    {
        [StringLength(255)]
        [DataMember]
        public string ClientName { get; set; }
        
        [DataMember]
        public Guid ApiKey { get; set; }

        [DataMember]
        public Guid ApiSecret { get; set; }
        
        [DataMember]
        public string CallbackUrl { get; set; }

        [DataMember]
        public bool Active { get; set; }
        
    }
}
