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
using Quartz;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Jobs
{
    [DisallowConcurrentExecution]
    public class SetFirstAttendanceDate
        : IJob
    {
        public void Execute( IJobExecutionContext context )
        {
            var rockContext = new RockContext();
            rockContext.Database.CommandTimeout = 300;

            AttributeValueService attributeValueService = new AttributeValueService( rockContext );
            PersonService personService = new PersonService( rockContext );
            var personAliasQry = new PersonAliasService( rockContext ).Queryable();
            var familyGroupMemberQry = new GroupMemberService( rockContext ).Queryable().Where( gm => gm.Group.GroupTypeId == 10 );
            var groupQry = new GroupService( rockContext ).Queryable().Where( g => g.GroupTypeId == 10 );
            var attendanceQry = new AttendanceService( rockContext ).Queryable().Where( a => a.DidAttend == true );

            var currentlyEraAttribue = AttributeCache.Get( "CE5739C5-2156-E2AB-48E5-1337C38B935E" );
            var firstAttendanceAttribute = AttributeCache.Get( "8F404727-82A5-4855-9714-62DFAB834BB2" );
            var eraStartDateAttribute = AttributeCache.Get( "A106610C-A7A1-469E-4097-9DE6400FDFC2" );
            var attendanceVerificationMethod = AttributeCache.Get( "7244EF82-FD1E-4B00-B2A1-114A79F09555" );
            var attendanceVerifiedType = AttributeCache.Get( "64C05D55-1697-4773-AE14-5C9596B71FF4" );
            var historyCategory = CategoryCache.Get( Rock.SystemGuid.Category.HISTORY_PERSON_DEMOGRAPHIC_CHANGES );
            var personEntityType = EntityTypeCache.Get( typeof( Rock.Model.Person ) );
            var attributeEntityType = EntityTypeCache.Get( typeof( Rock.Model.Attribute ) );

            var inEraQry = attributeValueService.Queryable()
                .Where( av => av.AttributeId == currentlyEraAttribue.Id && av.Value == "True" )
                .Select( av => av.EntityId );

            var firstAttQry = attributeValueService.Queryable()
                .Where( av => av.AttributeId == firstAttendanceAttribute.Id && av.Value != null && av.Value != "" )
                .Select( av => av.EntityId );

            var eraStartQry = attributeValueService.Queryable()
                .Where( av => av.AttributeId == eraStartDateAttribute.Id );


            //Linq!
            var people = personService.Queryable()              //Get all the people
                .Where( p => inEraQry.Contains( p.Id ) )        //Who are era
                .Where( p => !firstAttQry.Contains( p.Id ) )    //And don't have a first attendance
                .Join(                                          //Get the ERA Start Date
                    eraStartQry,
                    p => p.Id,
                    a => a.EntityId,
                    ( p, a ) => new { Person = p, EraStartDate = a.Value } )
                .GroupJoin(                                     //Get group membership for all family groups
                    familyGroupMemberQry,
                    o => o.Person.Id,
                    gm => gm.PersonId,
                    ( o, gm ) => new
                    {
                        o.Person,
                        o.EraStartDate,
                        FirstAttendance = gm.Select( gm2 => gm2.Group ).SelectMany( g => g.Members.Select( gm3 => gm3.Person ) ) //Get all family members
                           .GroupJoin(
                            personAliasQry,                     //Get all person alias ids for all family members
                            p => p.Id,
                            pa => pa.PersonId,
                            ( p, pa ) =>
                                pa.GroupJoin(
                                    attendanceQry,              //Get all attendance records for all family members
                                    pa2 => pa2.Id,
                                    a => a.PersonAliasId,
                                    ( pa2, a ) => a )
                                .SelectMany( a => a ) )         //Compact
                            .SelectMany( a => a )               //Compact
                            .OrderBy( a2 => a2.StartDateTime )  //Sort
                            .FirstOrDefault()                   //Get first
                    } )
                .ToList();

            var counter = 0;

            foreach ( var person in people )
            {
                try
                {
                    //Before we continue. We need to remove any existing attribute values.
                    var removeQry = string.Format( @"delete from AttributeValue where EntityId = {0} and AttributeId in ({1},{2},{3})",
                    person.Person.Id, firstAttendanceAttribute.Id, attendanceVerificationMethod.Id, attendanceVerifiedType.Id );
                    rockContext.Database.ExecuteSqlCommand( removeQry );


                    //Lets add in the attribute values!
                    if ( person.FirstAttendance != null )
                    {
                        var methodAV = new AttributeValue
                        {
                            IsSystem = false,
                            AttributeId = attendanceVerificationMethod.Id,
                            EntityId = person.Person.Id,
                            Value = "Automatic"
                        };

                        var methodHistory = new History
                        {
                            IsSystem = false,
                            CategoryId = historyCategory.Id,
                            EntityTypeId = personEntityType.Id,
                            EntityId = person.Person.Id,
                            RelatedEntityTypeId = attributeEntityType.Id,
                            RelatedEntityId = attendanceVerificationMethod.Id,
                            Verb = "MODIFY",
                            ChangeType = "Property",
                            ValueName = "Attendance Verification Method",
                            NewValue = "Automatic"
                        };

                        var attendanceTypeAV = new AttributeValue
                        {
                            IsSystem = false,
                            AttributeId = attendanceVerifiedType.Id,
                            EntityId = person.Person.Id,
                            Value = "SECC Attendance"
                        };

                        var attendanceTypeHistory = new History
                        {
                            IsSystem = false,
                            CategoryId = historyCategory.Id,
                            EntityTypeId = personEntityType.Id,
                            EntityId = person.Person.Id,
                            RelatedEntityTypeId = attributeEntityType.Id,
                            RelatedEntityId = attendanceVerifiedType.Id,
                            Verb = "MODIFY",
                            ChangeType = "Property",
                            ValueName = "Attendance Verification Type",
                            NewValue = "SECC Attendance"
                        };

                        var firstAttendanceAV = new AttributeValue
                        {
                            IsSystem = false,
                            AttributeId = firstAttendanceAttribute.Id,
                            EntityId = person.Person.Id,
                            Value = person.FirstAttendance.StartDateTime.Date.ToString( "MM/dd/yyyy" )
                        };

                        var firstAttendanceHistory = new History
                        {
                            IsSystem = false,
                            CategoryId = historyCategory.Id,
                            EntityTypeId = personEntityType.Id,
                            EntityId = person.Person.Id,
                            RelatedEntityTypeId = attributeEntityType.Id,
                            RelatedEntityId = firstAttendanceAttribute.Id,
                            Verb = "MODIFY",
                            ChangeType = "Property",
                            ValueName = "Attendance 1st Verified Date",
                            NewValue = person.FirstAttendance.StartDateTime.Date.ToString( "MM/dd/yyyy" )
                        };

                        rockContext.BulkInsert( new List<AttributeValue> { methodAV, attendanceTypeAV, firstAttendanceAV } );
                        rockContext.BulkInsert( new List<History> { methodHistory, attendanceTypeHistory, firstAttendanceHistory } );

                    }
                    else
                    {
                        var methodAV = new AttributeValue
                        {
                            IsSystem = false,
                            AttributeId = attendanceVerificationMethod.Id,
                            EntityId = person.Person.Id,
                            Value = "Automatic"
                        };

                        var methodHistory = new History
                        {
                            IsSystem = false,
                            CategoryId = historyCategory.Id,
                            EntityTypeId = personEntityType.Id,
                            EntityId = person.Person.Id,
                            RelatedEntityTypeId = attributeEntityType.Id,
                            RelatedEntityId = attendanceVerificationMethod.Id,
                            Verb = "MODIFY",
                            ChangeType = "Property",
                            ValueName = "Attendance Verification Method",
                            NewValue = "Automatic"
                        };

                        var attendanceTypeAV = new AttributeValue
                        {
                            IsSystem = false,
                            AttributeId = attendanceVerifiedType.Id,
                            EntityId = person.Person.Id,
                            Value = "eRA Start Date"
                        };

                        var attendanceTypeHistory = new History
                        {
                            IsSystem = false,
                            CategoryId = historyCategory.Id,
                            EntityTypeId = personEntityType.Id,
                            EntityId = person.Person.Id,
                            RelatedEntityTypeId = attributeEntityType.Id,
                            RelatedEntityId = attendanceVerifiedType.Id,
                            Verb = "MODIFY",
                            ChangeType = "Property",
                            ValueName = "Attendance Verification Type",
                            NewValue = "eRA Start Date"
                        };

                        var firstAttendanceAV = new AttributeValue
                        {
                            IsSystem = false,
                            AttributeId = firstAttendanceAttribute.Id,
                            EntityId = person.Person.Id,
                            Value = person.EraStartDate.AsDateTime().Value.Date.ToString( "MM/dd/yyyy" )
                        };

                        var firstAttendanceHistory = new History
                        {
                            IsSystem = false,
                            CategoryId = historyCategory.Id,
                            EntityTypeId = personEntityType.Id,
                            EntityId = person.Person.Id,
                            RelatedEntityTypeId = attributeEntityType.Id,
                            RelatedEntityId = firstAttendanceAttribute.Id,
                            Verb = "MODIFY",
                            ChangeType = "Property",
                            ValueName = "Attendance 1st Verified Date",
                            NewValue = person.EraStartDate.AsDateTime().Value.Date.ToString( "MM/dd/yyyy" )
                        };

                        rockContext.BulkInsert( new List<AttributeValue> { methodAV, attendanceTypeAV, firstAttendanceAV } );
                        rockContext.BulkInsert( new List<History> { methodHistory, attendanceTypeHistory, firstAttendanceHistory } );
                    }
                    counter++;
                }
                catch ( Exception e )
                {
                    var ex = string.Format( "Could not set first Attendance data for {0} - {1}", person.Person.FullName, person.Person.Id );
                    ExceptionLogService.LogException( new Exception( ex, e ) );
                }

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
