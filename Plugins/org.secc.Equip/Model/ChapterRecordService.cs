using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Data;

namespace org.secc.Equip.Model
{
    /// <summary></summary>
    /// <seealso cref="org.secc.Equip.Data.LearningService{org.secc.Equip.Model.ChapterRecord}" />
    public class ChapterRecordService : Service<ChapterRecord>
    {
        /// <summary>Initializes a new instance of the <see cref="ChapterRecordService"/> class.</summary>
        /// <param name="context">The context.</param>
        public ChapterRecordService( RockContext context ) : base( context ) { }
    }
}
