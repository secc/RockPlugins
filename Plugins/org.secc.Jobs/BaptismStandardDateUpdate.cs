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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Jobs
{
    [DisallowConcurrentExecution]
    [IntegerField( "Command Timeout", "Maximum amount of time (in seconds) to wait for the SQL Query to complete. Leave blank to use the default for this job (3600). Note, it could take several minutes, so you might want to set it at 3600 (60 minutes) or higher", false, 60 * 60, "General", 1, "CommandTimeout" )]

    public class BaptismStandardDateUpdate
        : IJob
    {
        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var rockContext = new RockContext();

            var commandTimeout = dataMap.GetString( "CommandTimeout" ).AsIntegerOrNull() ?? 3600;
            rockContext.Database.CommandTimeout = commandTimeout;
            AttributeService attributeService = new AttributeService( rockContext );
            AttributeMatrixService attributeMatrixService = new AttributeMatrixService( rockContext );
            AttributeMatrixItemService attributeMatrixItemService = new AttributeMatrixItemService( rockContext );
            AttributeValueService attributeValueService = new AttributeValueService( rockContext );
            PersonService personService = new PersonService( rockContext );
            var personAliasQry = new PersonAliasService( rockContext ).Queryable();
 //           var oldBaptismDateAttribute = AttributeCache.Get( "D42763FA-28E9-4A55-A25A-48998D7D7FEF" );
//            var newBaptismDateAttribute = AttributeCache.Get( "E2ED5BCE-6072-4B44-A923-FAF778035C23" );
            var historyCategory = CategoryCache.Get( Rock.SystemGuid.Category.HISTORY_PERSON_DEMOGRAPHIC_CHANGES );
            var personEntityType = EntityTypeCache.Get( typeof( Rock.Model.Person ) );
            var attributeEntityType = EntityTypeCache.Get( typeof( Rock.Model.Attribute ) );

            var baptismsAttribute = attributeService.Queryable()
                .Where( a => a.EntityTypeId == personEntityType.Id && a.Key == "Baptisms" ).FirstOrDefault();




            var newBaptDateQry = attributeValueService.Queryable()
                .Where( av => av.AttributeId == baptismsAttribute.Id && av.Value != null && av.Value != "" )
                .Select( av => av.EntityId );

            var people = personService.Queryable()              //Get all the people
                .Where( p => newBaptDateQry.Contains( p.Id ) ).ToList();     //Who have new baptism dates 

            var counter = 0;

            
            foreach ( var person in people )
            {
                counter++;
                person.LoadAttributes();

                var baptismMatrixGuid = person.GetAttributeValue( "Baptisms" );
                var baptismMatrix = attributeMatrixService.Get( baptismMatrixGuid.AsGuid() );
                var newestItem = baptismMatrix.AttributeMatrixItems.OrderByDescending( i => i.CreatedDateTime ).FirstOrDefault();

                if ( newestItem == null )
                {
                    continue;
                }

                newestItem.LoadAttributes();
                var baptismDate = newestItem.GetAttributeValue( "BaptismDate" );

                person.SetAttributeValue( "BaptismDate", baptismDate );
                person.SaveAttributeValues();
 
                if ( counter % 100 == 0 )
                {
                    var jobId = context.GetJobId();
                    var jobService = new ServiceJobService( rockContext );
                    var job = jobService.Get( jobId );
                    if ( job != null )
                    {
                        job.LastStatusMessage = string.Format( "Updated {0} People of {1}", counter, people.Count );
                        rockContext.SaveChanges( false );
                    }
                }




            }
        }
    }
}
