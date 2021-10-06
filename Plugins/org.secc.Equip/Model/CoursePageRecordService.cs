using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Data;

namespace org.secc.Equip.Model
{
    /// <summary></summary>
    /// <seealso cref="org.secc.Equip.Data.LearningService{org.secc.Equip.Model.CoursePageRecord}" />
    public class CoursePageRecordService : Service<CoursePageRecord>
    {
        /// <summary>Initializes a new instance of the <see cref="CoursePageRecordService"/> class.</summary>
        /// <param name="context">The context.</param>
        public CoursePageRecordService( RockContext context ) : base( context ) { }
    }
}