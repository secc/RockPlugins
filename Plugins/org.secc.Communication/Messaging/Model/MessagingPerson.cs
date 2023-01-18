using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Data;
using Rock.Model;

namespace org.secc.Communication.Messaging.Model
{
    public class MessagingPerson
    {
        public Guid AliasGuid { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }


        public MessagingPerson() { }
        public MessagingPerson( Person p)
        {
            LoadPerson( p );
        }
        public MessagingPerson(Guid aliasGuid)
        {
            var person = new PersonAliasService( new RockContext() )
                .Get( aliasGuid );

            LoadPerson( person.Person );
        }

        public void LoadPerson(Person p)
        {
            AliasGuid = p.PrimaryAlias.Guid;
            FirstName = p.FirstName;
            LastName = p.LastName;
        }

        public override string ToString()
        {
            return $"{FirstName} {LastName}";
        }

    }
}
