using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.RoomScanner.Utilities
{
    static class InMemoryWorshipRecord
    {
        private static List<int> _personIds;
        private static List<int> PersonIds
        {
            get
            {
                if ( _personIds == null )
                {
                    _personIds = new List<int>();
                }
                return _personIds;
            }
            set
            {
                _personIds = value;
            }
        }

        private static DateTime _effectiveDate;
        private static DateTime EffectiveDate
        {
            get
            {
                if ( _effectiveDate == null )
                {
                    _effectiveDate = Rock.RockDateTime.Today;
                }
                return _effectiveDate;
            }
            set
            {
                _effectiveDate = value;
            }
        }

        public static void AddToWorship( int personId )
        {
            UpdateEffectiveDate();

            if ( !PersonIds.Contains( personId ) )
            {
                PersonIds.Add( personId );
            }
        }

        public static void RemoveFromWorship( int personId )
        {
            UpdateEffectiveDate();
            if ( PersonIds.Contains( personId ) )
            {
                PersonIds.Remove( personId );
            }
        }

        public static bool IsInWorship( int personId )
        {
            UpdateEffectiveDate();
            return PersonIds.Contains( personId );
        }

        private static void UpdateEffectiveDate()
        {
            if ( EffectiveDate != Rock.RockDateTime.Today )
            {
                EffectiveDate = Rock.RockDateTime.Today;
                PersonIds = new List<int>();
            }
        }
    }
}
