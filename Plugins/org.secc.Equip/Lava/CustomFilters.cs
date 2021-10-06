using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.secc.Equip.Helpers;
using org.secc.Equip.Model;
using Rock;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Equip.Lava
{
    public static class CustomFilters
    {
        public static List<PersonCourseInfo> PersonCourseInfo( DotLiquid.Context context, object personObj )
        {
            var courseEntityType = EntityTypeCache.Get( typeof( Course ) );
            var categories = CategoryCache.All().Where( c => c.EntityTypeId == courseEntityType.Id );
            var categoryIds = categories
                .Select( c => c.Id )
                .ToList();

            if ( personObj != null && personObj is Person )
            {
                return PersonCourseInfo( context, personObj as Person, categoryIds );
            }

            return null;
        }


        public static List<PersonCourseInfo> PersonCourseInfo( DotLiquid.Context context, object personObj, string categoryIds )
        {
            if ( personObj != null && personObj is Person )
            {
                var courseEntityType = EntityTypeCache.Get( typeof( Course ) );
                var validCategoryIds = CategoryCache.All()
                    .Where( c => c.EntityTypeId == courseEntityType.Id )
                    .Where( c => categoryIds.Split().Select( i => i.AsInteger() ).ToList().Contains( c.Id ) )
                    .Select( c => c.Id ).ToList();

                return PersonCourseInfo( context, personObj as Person, validCategoryIds );
            }

            return null;
        }

        private static List<PersonCourseInfo> PersonCourseInfo( DotLiquid.Context context, Person person, List<int> categoryIds )
        {
            var currentPerson = GetCurrentPerson( context );

            RockContext rockContext = new RockContext();
            CourseService courseService = new CourseService( rockContext );
            CourseRecordService courseRecordService = new CourseRecordService( rockContext );
            var courseRecordQry = courseRecordService.Queryable().Where( r => r.PersonAlias.PersonId == person.Id );

            var courseItems = new List<PersonCourseInfo>();

            CourseRequirementStatusService courseRequirementStatusService = new CourseRequirementStatusService( rockContext );
            var statusQry = courseRequirementStatusService.Queryable()
                .Where( s => s.PersonAlias.PersonId == person.Id );

            var recordQueryable = courseRecordService.Queryable()
                .GroupJoin( statusQry,
                r => r.CourseId,
                s => s.CourseRequirement.CourseId,
                ( r, s ) => new { Record = r, Statuses = s }
                )
                .Where( r => r.Record.PersonAlias.PersonId == person.Id )
                .OrderByDescending( r => r.Record.CompletionDateTime );

            var courses = courseService.Queryable()
                   .Where( c => c.IsActive )
                   .Where( c => categoryIds.Contains( c.CategoryId ?? 0 ) )
                   .GroupJoin( recordQueryable,
                   c => c.Id,
                   r => r.Record.CourseId,
                   ( c, r ) => new
                   {
                       Course = c,
                       Records = r,
                       Category = c.Category
                   } )
                   .ToList();

            foreach (var course in courses )
            {
                if ( course.Course.IsAuthorized(Rock.Security.Authorization.VIEW, currentPerson ) )
                {
                    var courseItem = new PersonCourseInfo()
                    {
                        Course = course.Course,
                        Category = course.Category,
                        IsComplete = false
                    };

                    var completedRecords = course.Records.Where( r => r.Record.Passed ).ToList();
                    if ( completedRecords.Any() )
                    {
                        var completedCourse = completedRecords.First();
                        courseItem.IsComplete = true;
                        courseItem.CompletedDateTime = completedCourse.Record.CompletionDateTime;
                        var expired = completedCourse.Statuses
                            .Where( s => s.State == CourseRequirementState.Expired ).Any();
                        if ( expired )
                        {
                            courseItem.IsExpired = true;
                        }
                    }
                    courseItems.Add( courseItem );
                }
            }


            return courseItems;
        }

        //Stolen from Rock filters...why you not helper method?
        private static Person GetCurrentPerson( DotLiquid.Context context )
        {
            Person currentPerson = null;

            // First check for a person override value included in lava context
            if ( context.Scopes != null )
            {
                foreach ( var scopeHash in context.Scopes )
                {
                    if ( scopeHash.ContainsKey( "CurrentPerson" ) )
                    {
                        currentPerson = scopeHash["CurrentPerson"] as Person;
                    }
                }
            }

            if ( currentPerson == null )
            {
                var httpContext = System.Web.HttpContext.Current;
                if ( httpContext != null && httpContext.Items.Contains( "CurrentPerson" ) )
                {
                    currentPerson = httpContext.Items["CurrentPerson"] as Person;
                }
            }

            return currentPerson;
        }

    }
}
