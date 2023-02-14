using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.Communication.Messaging.Model
{
    public class MessagingOwner
    {
        public MessagingGroup OwnerGroup { get; set; } = null;
        public MessagingPerson OwnerPerson { get; set; } = null;
		
		public override string ToString()
		{
			var name = String.Empty;
			if( OwnerGroup != null )
			{
				name = OwnerGroup.ToString();
			}
			else if( OwnerPerson != null ) 
			{
				name = OwnerPerson.ToString();
			}
			
			return name;
		}
    }
}
