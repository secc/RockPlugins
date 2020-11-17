using System;
using System.Collections.Generic;
using System.Linq;
using org.secc.Rise.Model;
using org.secc.Rise.Response;
using org.secc.Rise.Utilities;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

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
            foreach ( var riseCourse in riseCourses )
            {
                RockContext rockContext = new RockContext();
                CourseService courseService = new CourseService( rockContext );

                var course = courseService.GetByCourseId( riseCourse.Id );
                if ( course.IsNull() )
                {
                    course = new Course();

                    courseService.Add( course );
                }

                course.Name = riseCourse.Title;
                course.Url = riseCourse.Url;
                course.CourseId = riseCourse.Id;

                rockContext.SaveChanges();

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
                    return user;
                }
            }
            return null;
        }

        public bool SyncAllGroups()
        {
            var riseGroupType = GroupTypeCache.Get( Constants.GROUPTYPE_RISE );

            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            var groups = groupService.Queryable()
                .Where( g => g.GroupTypeId == riseGroupType.Id )
                .ToList();

            foreach ( var group in groups )
            {
                var riseGroup = GetOrCreateGroup( group );
                riseGroup.SyncGroupMembers( group );
                //Handle course enrolments???
            }

            //Handle groups that have been deleted in Rock...

            return true;
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
            var riseGroup = ClientManager.Post<RiseGroup>( new Dictionary<string, string> { { "name", group.Name } } );

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
    }
}
