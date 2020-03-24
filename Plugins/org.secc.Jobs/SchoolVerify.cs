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
using Rock.Web.Cache;
using Rock.Communication;
using System.Text;

namespace Rock.Jobs
{
    /// The job will launch a workflow if a person is in a grade that doesn't match school.
    /// </summary
    [WorkflowTypeField( "School Grade Workflow", "The workflow type to launch for person whose school is invalid. The attributes 'Person', 'School', and 'Grade' will be passed to the workflow as attributes.", false, required: true )]
    [DisallowConcurrentExecution]

    public class SchoolVerify : IJob
    {
        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Execute( IJobExecutionContext context )
        {
            RockContext dbContext = new RockContext();
            PersonService personService = new PersonService( dbContext );
            AttributeService attributeService = new AttributeService( dbContext );
            AttributeValueService attributeValueService = new AttributeValueService( dbContext );;
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            List<Rock.Model.Person> people = personService.Queryable().Where( p => p.GraduationYear >= DateTime.Now.Year ).ToList();

            var rockContext = new RockContext();
            var errors = new List<string>();

            if ( people.Any() )
            {
                foreach (Person person in people)
                {
                    String gradenumber = null;
                    String grade = null;
                    person.LoadAttributes();
                    var school = person.GetAttributeValue( "School" ).IfEmpty( null );

                    if (school != null)
                    {
                        var gradeOffset = person.GradeOffset;

                        if (gradeOffset.HasValue && gradeOffset >= 0)
                        {
                            var schoolGrades = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.SCHOOL_GRADES.AsGuid() );

                            if (schoolGrades != null)
                            {
                                var sortedGradeValues = schoolGrades.DefinedValues.OrderBy( a => a.Value.AsInteger() );
                                var schoolGradeValue = sortedGradeValues.Where( a => a.Value.AsInteger() >= gradeOffset.Value ).FirstOrDefault();

                                if (schoolGradeValue != null)
                                {
                                    gradenumber = schoolGradeValue.Description;

                                    switch (gradenumber)
                                    {
                                        case "Freshman":
                                            grade = "9";
                                            break;
                                        case "Sophmore":
                                            grade = "10";
                                            break;
                                        case "Junior":
                                            grade = "11";
                                            break;
                                        case "Senior":
                                            grade = "12";
                                            break;
                                        default:
                                            grade = gradenumber.First().ToString();
                                            break;
                                    }
                                }
                            }
                        }
                        var schoolId = DefinedValueCache.Get( school ).Id;
                        var attribute = attributeService.Queryable().Where( a => a.Name == "Grades" ).FirstOrDefault();
                        var schoolgrades = attributeValueService.GetByAttributeIdAndEntityId( attribute.Id, schoolId );
                        var isgrade = schoolgrades.Value.Contains( grade );

                        if (!isgrade)
                        {
                            Guid? workflowGuid = dataMap.GetString( "SchoolGradeWorkflow" ).AsGuidOrNull();
                            WorkflowTypeCache workflowType = null;
                            var workflowService = new WorkflowService( rockContext );

                            if (workflowGuid != null)
                            {
                                workflowType = WorkflowTypeCache.Get( workflowGuid.Value );
                            }  

                            // Start workflow for this person
                            if (workflowType != null)
                            {
                                Dictionary<string, string> attributes = new Dictionary<string, string>();
                                attributes.Add( "Request", person.FullName );
                                attributes.Add( "School", DefinedValueCache.Get( school ).Value );
                                attributes.Add( "Grade", gradenumber );
                                StartWorkflow( workflowService, workflowType, attributes, string.Format( "{0}", person.FullName ), person );
                            }
                        }
                    }
                }
            }
            dbContext.SaveChanges();
            context.Result = string.Format( "{0} people were examined.", people.Count() );
        }

        /// <summary>
        /// Starts the workflow.
        /// </summary>
        /// <param name="workflowService">The workflow service.</param>
        /// <param name="workflowType">Type of the workflow.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="workflowNameSuffix">The workflow instance name suffix (the part that is tacked onto the end fo the name to distinguish one instance from another).</param>
        protected void StartWorkflow( WorkflowService workflowService, WorkflowTypeCache workflowType, Dictionary<string, string> attributes, string workflowNameSuffix, Person p )
        {
            // launch workflow if configured
            if ( workflowType != null && (workflowType.IsActive ?? true) )
            {
                var workflow = Rock.Model.Workflow.Activate( workflowType, "SendInvalidSchoolNotice " + workflowNameSuffix );

                // set attributes
                foreach ( KeyValuePair<string, string> attribute in attributes )
                {
                    workflow.SetAttributeValue( attribute.Key, attribute.Value );
                }
                // launch workflow
                List<string> workflowErrors;
                workflowService.Process( workflow, p, out workflowErrors );
            }
        }
    }
    }