using System.Collections.Generic;
using System.Linq;
using org.secc.Rise.Model;
using org.secc.Rise.Response;
using org.secc.Rise.Utilities;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Rise
{
    public class RiseClient
    {

        public RiseClient()
        {

        }

        /// <summary>
        /// Synchronizes the courses.
        /// </summary>
        public int SyncCourses()
        {
            var i = 0;
            var riseCourses = ClientManager.GetSet<RiseCourse>();

            List<string> syncedCourseIds = new List<string>();

            foreach ( var riseCourse in riseCourses )
            {
                SyncCourse( riseCourse );
                syncedCourseIds.Add( riseCourse.Id );
                i++;
            }

            RockContext rockContext = new RockContext();
            CourseService courseService = new CourseService( rockContext );
            var coursesToArchive = courseService.Queryable().Where( c => !syncedCourseIds.Contains( c.CourseId ) ).ToList();
            coursesToArchive.ForEach( c => c.IsArchived = true );
            rockContext.SaveChanges();

            return i;
        }

        public Course SyncCourse( RiseCourse riseCourse )
        {
            return SyncCourse( riseCourse, new CourseService( new RockContext() ) );
        }

        public Course SyncCourse( RiseCourse riseCourse, CourseService courseService )
        {
            RockContext rockContext = ( RockContext ) courseService.Context;

            var course = courseService.GetByCourseId( riseCourse.Id );
            if ( course.IsNull() )
            {
                course = new Course();

                courseService.Add( course );
            }

            course.Name = riseCourse.Title;
            course.Url = riseCourse.Url;
            course.CourseId = riseCourse.Id;
            course.IsArchived = false;

            rockContext.SaveChanges();

            course.GetExperienceObject(); //make sure we have the xObject in the DB

            return course;
        }

        public int SyncAllUsers()
        {
            var i = 0;
            var users = GetUsers();
            foreach ( var user in users )
            {
                user.GetRockPerson();
                i++;
            }
            return i;
        }

        public bool SyncPerson( Person person )
        {
            person.LoadAttributes();

            RiseUser riseUser = null;

            var riseId = person.GetAttributeValue( Constants.PERSON_ATTRIBUTE_KEY_RISEID );
            if ( riseId.IsNotNullOrWhiteSpace() )
            {
                riseUser = ClientManager.Get<RiseUser>( riseId );
            }

            if ( riseUser == null )
            {
                riseUser = QueryForUser( person );
            }

            if ( riseUser == null )
            {
                return false;
            }

            riseUser.SyncGroupMembership( person );

            return true;
        }

        private RiseUser QueryForUser( Person person )
        {
            var users = ClientManager.GetSet<RiseUser>( new Dictionary<string, string> { { "email", person.Email } } );
            foreach ( var user in users )
            {
                if ( ( user.FirstName.ToLower() == person.FirstName.ToLower() || user.FirstName.ToLower() == person.NickName.ToLower() )
                    && user.LastName.ToLower() == person.LastName.ToLower() )
                {
                    person.SetAttributeValue( Constants.PERSON_ATTRIBUTE_KEY_RISEID, user.Id );
                    person.SaveAttributeValue( Constants.PERSON_ATTRIBUTE_KEY_RISEID );

                    user.SaveUserCreated( person );

                    return user;
                }
            }
            return null;
        }

        public int SyncAllGroups()
        {
            var riseGroupTypeId = Constants.GetRiseGroupTypeId();

            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            var groups = groupService.Queryable()
                .Where( g => g.GroupTypeId == riseGroupTypeId )
                .ToList();

            foreach ( var group in groups )
            {
                var riseGroup = GetOrCreateGroup( group );
                riseGroup.SyncGroupMembers( group );
            }

            //Handle groups that have been deleted in Rock...

            return groups.Count();
        }

        public RiseGroup GetOrCreateGroup( Group group )
        {
            group.LoadAttributes();
            var riseGroupId = group.GetAttributeValue( Constants.GROUP_ATTRIBUTE_KEY_RISEID );

            RiseGroup riseGroup;

            if ( riseGroupId.IsNullOrWhiteSpace() )
            {
                riseGroup = CreateNewRiseGroup( group );
            }
            else
            {
                riseGroup = ClientManager.Get<RiseGroup>( riseGroupId );
            }

            return riseGroup;
        }

        private RiseGroup CreateNewRiseGroup( Group group )
        {
            var riseGroup = ClientManager.Post<RiseGroup>( new Dictionary<string, object> { { "name", group.Name } } );

            group.SetAttributeValue( Constants.GROUP_ATTRIBUTE_KEY_RISEID, riseGroup.Id );
            group.SaveAttributeValue( Constants.GROUP_ATTRIBUTE_KEY_RISEID );

            group.SetAttributeValue( Constants.GROUP_ATTRIBUTE_KEY_RISEURL, riseGroup.Url );
            group.SaveAttributeValue( Constants.GROUP_ATTRIBUTE_KEY_RISEURL );

            return riseGroup as RiseGroup;
        }

        public IEnumerable<RiseUser> GetUsers()
        {
            return ClientManager.GetSet<RiseUser>();
        }

        public IEnumerable<RiseGroup> GetGroups()
        {
            return ClientManager.GetSet<RiseGroup>();
        }

        public RiseUser GetUser( string id )
        {
            return ClientManager.Get<RiseUser>( id );
        }

        public RiseCourse GetCourse( Course course )
        {
            return ClientManager.Get<RiseCourse>( course.CourseId );
        }

        #region Enrollment

        public void Enroll( Course course, Group group )
        {
            var riseGroup = GetOrCreateGroup( group );
            var riseCourse = GetCourse( course );

            ClientManager.Put<RiseGroup>( riseCourse, riseGroup.Id );
        }

        public void Unenroll( Course course, Group group )
        {
            var riseGroup = GetOrCreateGroup( group );
            var riseCourse = GetCourse( course );

            ClientManager.Delete<RiseGroup>( riseCourse, riseGroup.Id );
        }

        #endregion

        #region Webhooks

        public IEnumerable<RiseWebhook> GetWebhooks()
        {
            return ClientManager.GetSet<RiseWebhook>();
        }

        public void DeleteWebhook( string id )
        {
            ClientManager.Delete<RiseWebhook>( id );
        }

        public void CreateWebhook( string url, List<string> events )
        {

            var parameters = new Dictionary<string, object>
            {
                { "targetUrl", url},
                { "events", events },
                { "sharedSecret", ClientManager.SharedSecret },
                { "apiVersion", Constants.API_VERSION }
            };

            ClientManager.Post<RiseWebhook>( parameters );
        }

        #endregion
    }
}
