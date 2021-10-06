using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Data;

namespace org.secc.Equip.Model
{
    /// <summary></summary>
    /// <seealso cref="org.secc.Equip.Data.LearningService{org.secc.Equip.Model.CourseRequirement}" />
    public class CourseRequirementService : Service<CourseRequirement>
    {
        /// <summary>Initializes a new instance of the <see cref="CourseRequirementService"/> class.</summary>
        /// <param name="context">The context.</param>
        public CourseRequirementService( RockContext context ) : base( context ) { }
    }
}
