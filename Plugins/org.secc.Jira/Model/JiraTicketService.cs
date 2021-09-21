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
using System.Data.Entity;
using System.Linq;
using org.secc.Jira.Data;
using Rock.Data;

namespace org.secc.Jira.Model
{
    public class JiraTicketService : JiraService<JiraTicket>
    {
        public JiraTicketService( RockContext context ) : base( context ) { }

        public JiraTicket Get( string key )
        {
            return Queryable().Where( t => t.Key == key ).FirstOrDefault();
        }

        public JiraTicket GetAsNoTracking( string key )
        {
            return Queryable().AsNoTracking().Where( t => t.Key == key ).FirstOrDefault();
        }
    }
}
