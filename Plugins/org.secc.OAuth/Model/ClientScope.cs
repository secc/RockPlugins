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
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Runtime.Serialization;
    [Table("_org_secc_OAuth_ClientScope")]
    public partial class ClientScope : Rock.Data.Model<ClientScope>, Rock.Security.ISecured
    {
        [DataMember]
        public int ClientId { get; set; }

        public Client Client { get; set; }

        [DataMember]
        public int ScopeId { get; set; }

        public Scope Scope { get; set; }
        
        [DataMember]
        public bool Active { get; set; }
        
    }
}
