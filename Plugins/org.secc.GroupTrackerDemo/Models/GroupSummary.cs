using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.GroupTrackerDemo.Models
{
    public class GroupSummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int GroupTypeId { get; set; }
        public string GroupTypeName { get; set; }
        public Guid Guid { get; set; }
        public bool IsActive { get; set; }
        

    }
}
