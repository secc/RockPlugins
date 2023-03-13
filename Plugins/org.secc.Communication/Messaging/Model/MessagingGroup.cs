using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Data;
using Rock.Model;

namespace org.secc.Communication.Messaging.Model
{
    public  class MessagingGroup
    {
        public Guid GroupGuid { get; set; }
        public string GroupName { get; set; }
        public Guid GroupTypeGuid { get; set; }
        public string GroupTypeName { get; set; }


        public MessagingGroup() { }
        public MessagingGroup(Guid groupGuid)
        {
            var rockContext = new RockContext();
            var group = new GroupService( rockContext ).Queryable()
                .Include( g => g.GroupType )
                .Where( g => g.Guid.Equals( groupGuid ) )
                .FirstOrDefault();

            LoadGroup( group );
        }

        public void LoadGroup(Group g)
        {
            GroupGuid = g.Guid;
            GroupName = g.Name;
            GroupTypeGuid = g.GroupType.Guid;
            GroupTypeName = g.GroupType.Name;

        }

        public override string ToString()
        {
            return GroupName;
        }

    }
}
