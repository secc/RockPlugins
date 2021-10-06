using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Data;

namespace org.secc.Equip.Model
{
    /// <summary></summary>
    /// <seealso cref="org.secc.Equip.Data.LearningService{org.secc.Equip.Model.CoursePage}" />
    public class CoursePageService : Service<CoursePage>
    {
        /// <summary>Initializes a new instance of the <see cref="CoursePageService"/> class.</summary>
        /// <param name="context">The context.</param>
        public CoursePageService( RockContext context ) : base( context ) { }
    }
}
