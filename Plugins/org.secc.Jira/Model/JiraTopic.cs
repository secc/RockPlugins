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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;
using org.secc.Jira.Contracts;
using Rock;
using Rock.Data;
using Rock.Web.UI.Controls;

namespace org.secc.Jira.Model
{
    [Table( "_org_secc_Jira_JiraTopic" )]
    [DataContract]
    public partial class JiraTopic : Rock.Data.Model<JiraTopic>, Rock.Security.ISecured, Rock.Data.IRockEntity, IOrdered
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string JQL { get; set; }

        [DataMember]
        public int Order { get; set; } = 0;

        [LavaInclude]
        public virtual ICollection<JiraTicket> JiraTickets
        {
            get { return _jiraTickets ?? ( _jiraTickets = new Collection<JiraTicket>() ); }
            set { _jiraTickets = value; }
        }


        private ICollection<JiraTicket> _jiraTickets;


        public void UpdateTickets()
        {
            var mergeValues = Rock.Lava.LavaHelper.GetCommonMergeFields( null );
            mergeValues.Add( "JiraTopic", this );
            var jql = JQL.ResolveMergeFields( mergeValues );

            JiraClient jiraClient = new JiraClient();
            var issues = jiraClient.GetTickets( jql ).GetAwaiter().GetResult();

            foreach ( var issue in issues )
            {
                using ( RockContext rockContext = new RockContext() )
                {
                    JiraTicketService jiraTicketService = new JiraTicketService( rockContext );

                    var ticket = jiraTicketService.Get( issue.Key );

                    if ( ticket == null )
                    {
                        ticket = new JiraTicket();
                        jiraTicketService.Add( ticket );
                    }

                    ticket.JiraTopicId = Id;
                    ticket.JiraId = issue.Id;
                    ticket.Key = issue.Key;
                    ticket.Title = issue.Fields.Summary;
                    ticket.Description = issue.Fields.Description;
                    ticket.TicketType = ( TicketType ) Enum.Parse( typeof( TicketType ), issue.Fields.IssueType.Name.Replace( "-", "" ) );

                    rockContext.SaveChanges();
                }

                //Now delete keys are in this topic but no longer used
                var activeKeys = issues.Select( i => i.Key ).ToList();

                RockContext deleteContext = new RockContext();

                JiraTicketService jiraTicketService2 = new JiraTicketService( deleteContext );

                var oldTickets = jiraTicketService2.Queryable().Where( t => t.JiraTopicId == Id && !activeKeys.Contains( t.Key ) ).ToList();

                jiraTicketService2.DeleteRange( oldTickets );

                deleteContext.SaveChanges();
            }
        }
    }

    public partial class JiraTopicConfiguration : EntityTypeConfiguration<JiraTopic>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KioskTypeConfiguration"/> class.
        /// </summary>
        public JiraTopicConfiguration()
        {
            this.HasMany<JiraTicket>( t => t.JiraTickets ).WithRequired( t => t.JiraTopic ).HasForeignKey( t => t.JiraTopicId );
            this.HasEntitySetName( "JiraTopic" );
        }
    }
}
