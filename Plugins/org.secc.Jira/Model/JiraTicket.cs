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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;

namespace org.secc.Jira.Model
{
    [Table( "_org_secc_Jira_JiraTicket" )]
    [DataContract]
    public partial class JiraTicket : Rock.Data.Model<JiraTicket>, Rock.Security.ISecured, Rock.Data.IRockEntity
    {
        [Index]
        [DataMember]
        public int JiraTopicId { get; set; }

        [LavaInclude]
        public virtual JiraTopic JiraTopic { get; set; }

        [DataMember]
        public int JiraId { get; set; }

        [Index( IsUnique = true )]
        [MaxLength( 100 )]
        [DataMember]
        public string Key { get; set; }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public TicketType TicketType { get; set; }
    }

    public enum TicketType
    {
        Bug,
        Task,
        Subtask,
        Story,
        Epic

    }

    public partial class JiraTicketConfiguration : EntityTypeConfiguration<JiraTicket>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KioskTypeConfiguration"/> class.
        /// </summary>
        public JiraTicketConfiguration()
        {
            this.HasRequired( t => t.JiraTopic ).WithMany().HasForeignKey( t => t.JiraTopicId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "JiraTicket" );
        }
    }
}
