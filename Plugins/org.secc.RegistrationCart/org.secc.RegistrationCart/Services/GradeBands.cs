// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System.Linq;
using Rock.Model;
using org.secc.RegistrationCart.Domain;

namespace org.secc.RegistrationCart.Services
{
    /// <summary>
    /// Maps Rock's grade offset (years until high-school graduation, 0 = senior) to the
    /// cart's coarse bands. Adults get their own band so non-camp offerings (retreats,
    /// classes, events) can sell to grown-ups; a child with no grade on file is None.
    /// </summary>
    public static class GradeBands
    {
        public static GradeBand FromGradeOffset( int? gradeOffset, bool isChild )
        {
            if ( gradeOffset == null || gradeOffset < 0 )
            {
                return isChild ? GradeBand.None : GradeBand.Adults;
            }

            if ( gradeOffset <= 3 ) return GradeBand.HighSchool;
            if ( gradeOffset <= 6 ) return GradeBand.MiddleSchool;
            if ( gradeOffset <= 12 ) return GradeBand.Elementary;
            return GradeBand.None;
        }

        public static GradeBand ForPerson( Person person )
        {
            if ( person == null )
            {
                return GradeBand.None;
            }

            return FromGradeOffset( person.GradeOffset, person.AgeClassification == AgeClassification.Child );
        }

        /// <summary>A representative grade offset for a band, used when creating a new child.</summary>
        public static int GradeOffsetFor( GradeBand band )
        {
            switch ( band )
            {
                case GradeBand.HighSchool: return 2;
                case GradeBand.MiddleSchool: return 5;
                default: return 9;
            }
        }

        /// <summary>A set of bands in words: "all students", "everyone", or the bands listed.</summary>
        public static string Describe( GradeBand bands )
        {
            switch ( bands )
            {
                case GradeBand.None: return "no one";
                case GradeBand.All: return "all students";
                case GradeBand.Everyone: return "everyone";
            }

            var names = new[] { GradeBand.Elementary, GradeBand.MiddleSchool, GradeBand.HighSchool, GradeBand.Adults }
                .Where( b => bands.HasFlag( b ) )
                .Select( b => b == GradeBand.Adults ? "adults" : Friendly( b ) );
            return string.Join( ", ", names );
        }

        public static string Friendly( GradeBand band )
        {
            switch ( band )
            {
                case GradeBand.Elementary: return "Elementary";
                case GradeBand.MiddleSchool: return "Middle School";
                case GradeBand.HighSchool: return "High School";
                case GradeBand.Adults: return "Adult";
                case GradeBand.All: return "All students";
                case GradeBand.Everyone: return "Everyone";
                default: return "Grade not set";
            }
        }
    }
}
