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
using System.Data;
using System.Linq;
using System.Net;
using org.secc.Jira.Model;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Jira.Jobs
{

    [DisallowConcurrentExecution]
    public class UpdateJiraTickets : IJob
    {

        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            RockContext rockContext = new RockContext();
            JiraTopicService jiraTopicService = new JiraTopicService( rockContext );
            var topics = jiraTopicService.Queryable().OrderBy( t => t.Order ).ToList();
            foreach ( var topic in topics )
            {
                topic.UpdateTickets();
            }

            context.Result = $"Updated tickets for {topics.Count} Jira topics";
        }
    }
}
