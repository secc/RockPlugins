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
using System.Data.Entity;
using System.Linq;
using System.Runtime.Serialization;
using org.secc.Rise.Model;
using org.secc.xAPI.Model;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Rise.Utilities
{
    /// <summary>Set of helpers for Enrolling groups and people in courses.</summary>
    public static class EnrollmentHelper
    {
        /// <summary>Gets the person courses.</summary>
        /// <param name="person">The person.</param>
        /// <returns></returns>
        public static List<CourseResult> GetPersonCourses( Person person )
        {
            return GetPersonCourses( person, null );
        }

        /// <summary>Gets the person courses.</summary>
        /// <param name="person">The person.</param>
        /// <param name="categories">The categories.</param>
        /// <returns></returns>
        public static List<CourseResult> GetPersonCourses( Person person, List<CategoryCache> categories, bool enrolledOnly = true )
        {
            if ( person == null )
            {
                return null;
            }

            RockContext rockContext = new RockContext();
            CourseService courseService = new CourseService( rockContext );
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            AttributeValueService attributeValueService = new AttributeValueService( rockContext );

            var courseEntityTypeId = EntityTypeCache.Get( typeof( Course ) ).Id;

            var attributeValueQry = attributeValueService.Queryable().AsNoTracking()
                .Where( av => av.Attribute.EntityTypeId == courseEntityTypeId );

            var qry = courseService.Queryable().AsNoTracking();

            if ( categories != null && categories.Any() )
            {
                var categoryIds = categories.Select( ca => ca.Id ).ToList();
                qry = qry.Where( c => c.Categories.Any( ca => categoryIds.Contains( ca.Id ) ) );
            }

            if ( enrolledOnly )
            {
                var riseGroupTypeId = Constants.GetRiseGroupTypeId();
                var riseGroups = groupMemberService.Queryable().AsNoTracking()
                    .Where( gm => gm.PersonId == person.Id && gm.Group.GroupTypeId == riseGroupTypeId )
                    .Select( gm => gm.GroupId );

                qry = qry.Where( c => c.AvailableToAll == true || c.EnrolledGroups.Any( g => riseGroups.Contains( g.Id ) ) );
            }

            var mixedResults = qry.GroupJoin(
                attributeValueQry,
                c => c.Id,
                av => av.EntityId,
                ( c, av ) => new { Course = c, AttributeValues = av } )
                .OrderBy( m => m.Course.Name )
                .ToList();

            // Get the Experiences
            int[] personAliasIds = person.Aliases.Select( a => a.Id ).ToArray();
            string[] courseIds = mixedResults.Select( m => m.Course.Id.ToString() ).ToArray();

            ExperienceService experienceService = new ExperienceService( rockContext );
            var experiences = experienceService.Queryable( "xObject" ).Where( e => personAliasIds.Contains( e.PersonAliasId ) && courseIds.Contains( e.xObject.ObjectId ) ).ToList();

            var courses = new List<CourseResult>();
            foreach ( var result in mixedResults )
            {
                var courseResult = new CourseResult();
                courseResult.Course = result.Course;
                courseResult.Course.AttributeValues = result.AttributeValues.ToDictionary( av => av.AttributeKey, av => new AttributeValueCache( av ) );
                courseResult.Course.Attributes = result.AttributeValues.ToDictionary( av => av.AttributeKey, av => AttributeCache.Get( av.AttributeId ) );
                courseResult.Experiences = experiences.Where( e => e.xObject != null && e.xObject.ObjectId == result.Course.Id.ToString() ).ToList();
                courses.Add( courseResult );
            }

            return courses;
        }

        [DataContract]
        public class CourseResult : DotLiquid.Drop
        {
            [DataMember]
            public Course Course { get; set; }

            [DataMember]
            public List<Experience> Experiences { get; set; }
        }
    }

}
