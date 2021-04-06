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
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Jobs
{
    [DisallowConcurrentExecution]

    [IntegerField( "Matrix Template Id",
        Description = "The id of the attribute matrix template",
        IsRequired = true,
        DefaultIntegerValue = 11,
        Order = 0,
        Key = AttributeKeys.MaxtrixTemplate )]

    [IntegerField( "Command Timeout",
        Description = "Maximum amount of time (in seconds) to wait for the SQL Query to complete. Leave blank to use the default for this job (3600). Note, it could take several minutes, so you might want to set it at 3600 (60 minutes) or higher",
        IsRequired = false,
        DefaultIntegerValue = 60 * 60,
        Order = 1,
        Key = AttributeKeys.CommandTimeout )]

    [TextField( "Baptisms Person Attribute Key",
        Description = "The person attribute of the baptisms matrix",
        IsRequired = true,
        DefaultValue = "Baptisms",
        Order = 2,
        Key = AttributeKeys.BaptismsKey )]

    [TextField( "Matrix Baptism Date Attribute Key",
        Description = "The attribute key in the matrix of the baptism date.",
        IsRequired = true,
        DefaultValue = "BaptismDate",
        Order = 3,
        Key = AttributeKeys.MatrixBaptismDateKey )]

    [TextField( "Person Baptism Date Attribute Key",
        Description = "The person attribute key of their baptism date.",
        IsRequired = true,
        DefaultValue = "BaptismDate",
        Order = 4,
        Key = AttributeKeys.PersonBaptismDateKey )]

    [TextField( "Matrix Baptized By Attribute Key",
        Description = "The attribute key in the matrix of the baptized by value.",
        IsRequired = true,
        DefaultValue = "BaptizedBy",
        Order = 5,
        Key = AttributeKeys.MatrixBaptizedByKey )]

    [TextField( "Person Baptism Date Attribute Key",
        Description = "The person attribute key of their baptism by value.",
        IsRequired = true,
        DefaultValue = "BaptizedBy",
        Order = 6,
        Key = AttributeKeys.PersonBaptizedByKey )]


    public class BaptismStandardDateUpdate
        : IJob
    {
        internal static class AttributeKeys
        {
            internal const string MaxtrixTemplate = "MatrixTemplate";
            internal const string CommandTimeout = "CommandTimeout";
            internal const string BaptismsKey = "BaptismsKey";
            internal const string MatrixBaptismDateKey = "MatrixBaptismDateKey";
            internal const string PersonBaptismDateKey = "PersonBaptismDateKey";
            internal const string MatrixBaptizedByKey = "MatrixBaptizedByKey";
            internal const string PersonBaptizedByKey = "PersonBaptizedByKey";
        }

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

            var personEntityType = EntityTypeCache.Get( typeof( Rock.Model.Person ) );
            var attributeMatrixItemEntityType = EntityTypeCache.Get( typeof( Rock.Model.AttributeMatrixItem ) );
            var attributeMatrixTemplateId = dataMap.GetString( AttributeKeys.MaxtrixTemplate ).AsInteger();

            //Attribute Keys
            var baptismsKey = dataMap.GetString( AttributeKeys.BaptismsKey );
            var baptismDatePersonKey = dataMap.GetString( AttributeKeys.PersonBaptismDateKey );
            var baptizedByPersonKey = dataMap.GetString( AttributeKeys.PersonBaptizedByKey );
            var baptismDateMatrixKey = dataMap.GetString( AttributeKeys.MatrixBaptismDateKey );
            var baptizedByMatrixKey = dataMap.GetString( AttributeKeys.MatrixBaptizedByKey );

            //Attributes
            var baptismsAttribute = attributeService.Queryable().AsNoTracking()
               .Where( a => a.EntityTypeId == personEntityType.Id && a.Key == baptismsKey ).FirstOrDefault();
            var baptismDatePersonAttribute = attributeService.Queryable().AsNoTracking()
               .Where( a => a.EntityTypeId == personEntityType.Id && a.Key == baptismDatePersonKey ).FirstOrDefault();
            var baptizedByPersonAttribute = attributeService.Queryable().AsNoTracking()
                .Where( a => a.EntityTypeId == personEntityType.Id && a.Key == baptizedByPersonKey ).FirstOrDefault();
            var baptismDateMatrixAttribute = attributeService.Queryable().AsNoTracking()
                .Where( a => a.EntityTypeId == attributeMatrixItemEntityType.Id && a.Key == baptismDateMatrixKey ).FirstOrDefault();
            var baptizedByMatrixAttribute = attributeService.Queryable().AsNoTracking()
                .Where( a => a.EntityTypeId == attributeMatrixItemEntityType.Id && a.Key == baptizedByMatrixKey ).FirstOrDefault();

            //Attribute Value Queries
            var baptismsAttributeValues = attributeValueService.Queryable().AsNoTracking()
              .Where( av => av.AttributeId == baptismsAttribute.Id );
            var baptismDatePersonAttributeValues = attributeValueService.Queryable().AsNoTracking()
              .Where( av => av.AttributeId == baptismDatePersonAttribute.Id );
            var baptizedByPersonAttributeValues = attributeValueService.Queryable().AsNoTracking()
              .Where( av => av.AttributeId == baptizedByPersonAttribute.Id );
            var baptismDateMatrixAttributeValues = attributeValueService.Queryable().AsNoTracking()
                .Where( av => av.AttributeId == baptismDateMatrixAttribute.Id );
            var baptizedByMatrixAttributeValues = attributeValueService.Queryable().AsNoTracking()
                .Where( av => av.AttributeId == baptizedByMatrixAttribute.Id );


            //Loads the newest baptism date and baptized by for each matrix
            var matrixData = attributeMatrixService.Queryable().Where( m => m.AttributeMatrixTemplateId == attributeMatrixTemplateId )
                .SelectMany( m => m.AttributeMatrixItems )
                .Select( i => new
                {
                    Item = i,
                    BaptismDateAttribute = baptismDateMatrixAttributeValues.FirstOrDefault( a => a.EntityId == i.Id ),
                    BaptizedByAttribute = baptizedByMatrixAttributeValues.FirstOrDefault( a => a.EntityId == i.Id )
                } )
                .GroupBy( i => i.Item.AttributeMatrix.Guid )
                .ToList()
                .Select( i => new
                {
                    MatrixGuid = i.Key,
                    BaptismDate = i.OrderByDescending( a => a.BaptismDateAttribute != null && a.BaptismDateAttribute.ValueAsDateTime.HasValue ? a.BaptismDateAttribute.ValueAsDateTime.Value : ( a.Item.CreatedDateTime.HasValue ? a.Item.CreatedDateTime.Value.AddYears( -50 ) : new DateTime() ) ).Select( a => a.BaptismDateAttribute != null ? a.BaptismDateAttribute.ValueAsDateTime : null ).FirstOrDefault(), //I'm sorry you had to scroll all the way out here
                    BaptismDateValue = i.OrderByDescending( a => a.BaptismDateAttribute != null && a.BaptismDateAttribute.ValueAsDateTime.HasValue ? a.BaptismDateAttribute.ValueAsDateTime.Value : ( a.Item.CreatedDateTime.HasValue ? a.Item.CreatedDateTime.Value.AddYears( -50 ) : new DateTime() ) ).Select( a => a.BaptismDateAttribute != null ? a.BaptismDateAttribute.Value : "" ).FirstOrDefault(),
                    BaptizedBy = i.OrderByDescending( a => a.BaptismDateAttribute != null && a.BaptismDateAttribute.ValueAsDateTime.HasValue ? a.BaptismDateAttribute.ValueAsDateTime.Value : ( a.Item.CreatedDateTime.HasValue ? a.Item.CreatedDateTime.Value.AddYears( -50 ) : new DateTime() ) ).Select( a => a.BaptizedByAttribute != null ? a.BaptizedByAttribute.Value : "" ).FirstOrDefault()
                } )
                .ToList();

            //Loads all the people and their data
            var idsOfPeopleWithBaptismMatrix = attributeValueService.Queryable()
                .Where( av => av.AttributeId == baptismsAttribute.Id && av.Value != null && av.Value != "" )
                .Select( av => av.EntityId );

            var people = personService.Queryable()
                .Where( p => idsOfPeopleWithBaptismMatrix.Contains( p.Id ) )
                .Join( baptismsAttributeValues,
                p => p.Id,
                av => av.EntityId,
                ( p, av ) => new
                {
                    Person = p,
                    MatrixGuid = av.Value
                } )
                .GroupJoin( baptismDatePersonAttributeValues, //Group join because the attribute values may not exist
                p => p.Person.Id,
                av => av.EntityId,
                ( p, av ) => new
                {
                    Person = p.Person,
                    MatrixGuid = p.MatrixGuid,
                    BaptismDate = av.FirstOrDefault()
                } )
                .GroupJoin( baptizedByPersonAttributeValues,
                p => p.Person.Id,
                av => av.EntityId,
                ( p, av ) => new
                {

                    Person = p.Person,
                    MatrixGuid = p.MatrixGuid,
                    BaptismDate = p.BaptismDate,
                    BaptizedBy = av.FirstOrDefault()
                } )
                .ToList();


            var counter = 0;
            var updated = 0;


            foreach ( var person in people )
            {
                counter++;

                var matchedData = matrixData.Where( d => d.MatrixGuid == person.MatrixGuid.AsGuid() ).FirstOrDefault();

                if ( matchedData == null )
                {
                    continue;
                }

                if ( ( person.BaptismDate == null && matchedData.BaptismDate != null )
                    || ( person.BaptismDate != null && person.BaptismDate.ValueAsDateTime.HasValue != matchedData.BaptismDate.HasValue )
                    || ( person.BaptismDate != null && person.BaptismDate.ValueAsDateTime.HasValue && person.BaptismDate.ValueAsDateTime.Value != matchedData.BaptismDate )
                    || ( person.BaptizedBy == null && matchedData.BaptizedBy.IsNotNullOrWhiteSpace() )
                    || ( person.BaptizedBy != null && person.BaptizedBy.Value != matchedData.BaptizedBy ) )
                {
                    updated++;
                    var personEntity = person.Person;
                    personEntity.LoadAttributes();
                    personEntity.SetAttributeValue( dataMap.GetString( AttributeKeys.PersonBaptismDateKey ), matchedData.BaptismDateValue );
                    personEntity.SetAttributeValue( dataMap.GetString( AttributeKeys.PersonBaptizedByKey ), matchedData.BaptizedBy );
                    personEntity.SaveAttributeValues();

                   //Need to add history saving

                }

                if ( counter % 1000 == 0 )
                {
                    var jobId = context.GetJobId();
                    var jobService = new ServiceJobService( rockContext );
                    var job = jobService.Get( jobId );
                    if ( job != null )
                    {
                        job.LastStatusMessage = string.Format( "Processed {0}  people of {1} ({2} updated)", counter, people.Count(), updated );
                        rockContext.SaveChanges( false );
                    }
                }
            }
            context.UpdateLastStatusMessage( string.Format( "Processed {0}  people of {1} ({2} updated)", counter, people.Count(), updated ) );
        }
    }
}
