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
using System.Linq;
using org.secc.Rise.Data;
using Rock;
using Rock.Data;

namespace org.secc.Rise.Model
{
    /// <summary></summary>
    /// <seealso cref="org.secc.Rise.Data.RiseService{org.secc.Rise.Model.Course}" />
    public class CourseService : RiseService<Course>
    {
        public CourseService( RockContext context ) : base( context )
        {
        }

        /// <summary>Gets the by course identifier.</summary>
        /// <param name="courseId">The course identifier.</param>
        /// <returns>Course</returns>
        public Course GetByCourseId( string courseId )
        {
            return Queryable().Where( c => c.CourseId == courseId ).FirstOrDefault();
        }


        /// <summary>Gets the by URL.</summary>
        /// <param name="url">The URL.</param>
        /// <returns>Course</returns>
        public Course GetByUrl( string url )
        {
            return Queryable().Where( c => c.Url == url ).FirstOrDefault();
        }
    }

}
