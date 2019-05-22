// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
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
using System.Web;
using System.IO;
using System.Data.SqlClient;

using Quartz;

using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Data;
using System.Data.Entity;

namespace org.secc.Jobs
{
    /// The job will set a person attribute for each person returned from the dataview. 
    /// </summary>
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "Work With Minors", "Select the person attribute to be set to true if the person can work with minors", required: true, allowMultiple: false, category: "Person Attributes" )]
    [DataViewField( "DataView", "The dataview the attribute will be set for.", true, "", "Rock.Model.Person" )]
    [DisallowConcurrentExecution]

    public class SetDataViewAttribute : IJob
    {
        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Execute( IJobExecutionContext context )
        {
            RockContext dbContext = new RockContext();
            AttributeService attributeService = new AttributeService( dbContext );
            AttributeValueService attributeValueService = new AttributeValueService( dbContext );
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var dataViewGuid = dataMap.GetString( "DataView" ).AsGuidOrNull();
            Rock.Model.Attribute attribute = attributeService.Get( dataMap.GetString( "WorkWithMinors" ).AsGuid() );
            List<Rock.Model.AttributeValue> attributeValues = attributeValueService.Queryable().Where( a => a.AttributeId == attribute.Id && a.Value == "True" ).ToList();
            List<IEntity> resultSet = null;

            if (dataViewGuid != null)
            {
                var rockContext = new RockContext();
                var dataView = new DataViewService( rockContext ).Get( (Guid)dataViewGuid );
                var errorMessages = new List<string>();

                try
                {
                    var qry = dataView.GetQuery( null, rockContext, null, out errorMessages );
                    if (qry != null)
                    {
                        resultSet = qry.ToList();
                    }

                }
                catch (Exception exception)
                {
                    ExceptionLogService.LogException( exception, HttpContext.Current );

                    while (exception != null)
                    {
                        if (exception is SqlException && (exception as SqlException).Number == -2)
                        {
                            // if there was a SQL Server Timeout, have the warning be a friendly message about that.
                            errorMessages.Add( "This dataview did not complete in a timely manner. You can try again or adjust the timeout setting of this block." );
                            exception = exception.InnerException;
                        }
                        else
                        {
                            errorMessages.Add( exception.Message );
                            exception = exception.InnerException;
                        }
                        return;
                    }
                }

                if (resultSet.Any())
                {
                    foreach (Person person in resultSet)
                    {
                        var attributeValue = attributeValues.Where( a => a.EntityId == person.Id ).FirstOrDefault();

                        if (attributeValue != null)
                        {
                            attributeValues.Remove( attributeValue );
                        }
                        person.LoadAttributes();
                        var value = person.GetAttributeValue( attribute.Key );

                        if (value != "True")
                        {
                            person.SetAttributeValue( attribute.Key, "True" );
                            person.SaveAttributeValues();
                        }
                    }
                    dbContext.SaveChanges();
                }

                foreach (AttributeValue attributeValue in attributeValues)
                {
                    attributeValue.Value = "False";

                }
                dbContext.SaveChanges();
                context.Result = "Successfully finished";
            }
        }
    }
}
