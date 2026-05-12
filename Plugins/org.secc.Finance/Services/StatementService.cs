using System.Collections.Generic;
using System.Linq;

namespace org.secc.Finance.Services
{
    public static class StatementService
    {
        public class GivingGroupMember
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public bool IsBusiness { get; set; }
        }

        public static string BuildSalutation( IList<GivingGroupMember> orderedMembers )
        {
            if ( orderedMembers == null || orderedMembers.Count == 0 )
            {
                return string.Empty;
            }

            var first = orderedMembers[0];

            if ( first.IsBusiness )
            {
                return first.LastName;
            }

            string salutation;
            if ( orderedMembers.GroupBy( g => g.LastName ).Count() == 1 )
            {
                salutation = string.Join( ", ", orderedMembers.Select( g => g.FirstName ) ) + " " + first.LastName;
            }
            else
            {
                salutation = string.Join( ", ", orderedMembers.Select( g => g.FirstName + " " + g.LastName ) );
            }

            int lastComma = salutation.LastIndexOf( ',' );
            if ( lastComma >= 0 )
            {
                salutation = salutation.Substring( 0, lastComma ) + " &" + salutation.Substring( lastComma + 1 );
            }

            return salutation;
        }
    }
}
