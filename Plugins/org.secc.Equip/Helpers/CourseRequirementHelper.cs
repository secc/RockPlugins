using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.secc.Equip.Model;
using Rock.Data;
using Rock.Model;

namespace org.secc.Equip.Helpers
{
    public static class CourseRequirementHelper
    {

        public static void UpdateCourseRequirementStatuses( CourseRequirement courseRequirement )
        {
            AddAndRemoveRows( courseRequirement.Id );
            UpdateStatuses( courseRequirement.Id );
        }

        private static void UpdateStatuses( int courseRequirementId )
        {
            RockContext rockContext = new RockContext();
            CourseRequirementService courseRequirementService = new CourseRequirementService( rockContext );

            var courseRequirement = courseRequirementService.Get( courseRequirementId );
            if ( courseRequirement == null )
            {
                return;
            }

            CourseRequirementStatusService statusService = new CourseRequirementStatusService( rockContext );

            var statuses = statusService.Queryable( "PersonAlias" )
                .Where( s => s.CourseRequirementId == courseRequirement.Id )
                .ToList();

            CourseRecordService courseRecordService = new CourseRecordService( rockContext );

            foreach ( var status in statuses )
            {
                var newestRecord = courseRecordService.Queryable()
                    .Where( r => r.Passed )
                    .Where( r => r.PersonAlias.PersonId == status.PersonAlias.PersonId )
                    .Where( r => r.CourseId == courseRequirement.CourseId )
                    .OrderByDescending( r => r.CompletionDateTime.Value )
                    .FirstOrDefault();
                if ( newestRecord == null )
                {
                    status.IsComplete = false;
                }
                else
                {
                    status.IsComplete = true;
                    if ( courseRequirement.DaysValid.HasValue )
                    {
                        status.ValidUntil = newestRecord.CompletionDateTime.Value.AddDays( courseRequirement.DaysValid.Value );
                    }
                    else
                    {
                        status.ValidUntil = null;
                    }
                }
            }
            rockContext.SaveChanges();
        }

        private static void AddAndRemoveRows( int courseRequirementId )
        {
            RockContext rockContext = new RockContext();
            CourseRequirementService courseRequirementService = new CourseRequirementService( rockContext );

            var courseRequirement = courseRequirementService.Get( courseRequirementId );
            if ( courseRequirement == null )
            {
                return;
            }

            IQueryable<Person> peopleQry = null;
            if ( courseRequirement.GroupId.HasValue )
            {
                GroupMemberService groupMemberService = new GroupMemberService( rockContext );
                peopleQry = groupMemberService.Queryable()
                    .Where( gm => gm.GroupId == courseRequirement.GroupId )
                    .Select( gm => gm.Person );
            }
            else if ( courseRequirement.DataViewId.HasValue )
            {
                var errorMessages = new List<string>();
                DataViewService dataViewService = new DataViewService( rockContext );
                var dataview = dataViewService.Get( courseRequirement.DataViewId.Value );
                var qry = dataview.GetQuery( null, rockContext, 300, out errorMessages );
                peopleQry = qry.Select( e => ( Person ) e );
            }


            //If you can't take this course, you shouldn't be required to take it
            if ( courseRequirement.Course.AllowedGroupId.HasValue )
            {
                GroupMemberService groupMemberService = new GroupMemberService( rockContext );
                var groupMemberIds = groupMemberService.Queryable()
                    .Where( gm => gm.GroupId == courseRequirement.Course.AllowedGroupId.Value )
                    .Select( gm => gm.PersonId );
                peopleQry = peopleQry.Where( p => groupMemberIds.Contains( p.Id ) );
            }
            else if ( courseRequirement.Course.AllowedDataViewId.HasValue )
            {
                var errorMessages = new List<string>();
                DataViewService dataViewService = new DataViewService( rockContext );
                var dataview = dataViewService.Get( courseRequirement.Course.AllowedDataViewId.Value );
                var entityIds = dataview.GetQuery( null, rockContext, 300, out errorMessages ).Select( e => e.Id );
                peopleQry = peopleQry.Where( p => entityIds.Contains( p.Id ) );
            }

            var people = peopleQry.ToList();

            CourseRequirementStatusService statusService = new CourseRequirementStatusService( rockContext );

            var peopleInStatuses = statusService.Queryable()
                .Where( s => courseRequirement.Id == s.CourseRequirementId )
                .Select( s => s.PersonAlias.PersonId )
                .ToList();

            var peopleWithoutStatus = people.Where( p => !peopleInStatuses.Contains( p.Id ) ).ToList();
            foreach ( var person in peopleWithoutStatus )
            {
                if ( !person.PrimaryAliasId.HasValue )
                {
                    continue;
                }

                var status = new CourseRequirementStatus
                {
                    PersonAliasId = person.PrimaryAliasId.Value,
                    CourseRequirementId = courseRequirement.Id
                };
                statusService.Add( status );
            }

            var personAliasIds = people.SelectMany( p => p.Aliases.Select( pa => pa.Id ) ).ToList();

            var statusesToRemove = statusService.Queryable()
                .Where( s => courseRequirement.Id == s.CourseRequirementId )
                .Where( s => !personAliasIds.Contains( s.PersonAliasId ) )
                .ToList();

            foreach ( var toRemove in statusesToRemove )
            {
                statusService.Delete( toRemove );
            }

            rockContext.SaveChanges();
        }

        public static void UpdateCourseStatuses( int courseId, int personAliasId, bool passed )
        {
            RockContext rockContext = new RockContext();
            CourseRequirementService courseRequirementService = new CourseRequirementService( rockContext );

            var courseRequirements = courseRequirementService.Queryable()
                .Where( r => r.CourseId == courseId )
                .ToList();

            if ( !courseRequirements.Any() )
            {
                return;
            }

            PersonAliasService personAliasService = new PersonAliasService( rockContext );
            var personAliasIds = personAliasService.Get( personAliasId )
                .Person.Aliases.Select( pa => pa.Id ).ToList();
            CourseRequirementStatusService courseStatusService = new CourseRequirementStatusService( rockContext );

            foreach ( var courseRequirement in courseRequirements )
            {
                var status = courseStatusService.Queryable()
                    .Where( s => s.CourseRequirementId == courseRequirement.Id )
                    .Where( s => personAliasIds.Contains( s.PersonAliasId ) )
                    .FirstOrDefault();
                if ( status != null )
                {
                    status.IsComplete = passed;
                    if ( status.IsComplete )
                    {
                        if ( courseRequirement.DaysValid.HasValue )
                        {
                            status.ValidUntil = Rock.RockDateTime.Today.AddDays( courseRequirement.DaysValid.Value );
                        }
                    }
                }

            }
            rockContext.SaveChanges();
        }
    }
}
