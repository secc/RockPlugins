using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.RoomScanner.Utilities
{
    static class InMemoryPersonStatus
    {
        private static List<int> _inWorshipPersonIds;
        private static List<int> InWorshipPersonIds
        {
            get
            {
                if ( _inWorshipPersonIds == null )
                {
                    _inWorshipPersonIds = new List<int>();
                }
                return _inWorshipPersonIds;
            }
            set
            {
                _inWorshipPersonIds = value;
            }
        }

        private static List<int> _withParentPersonIds;
        private static List<int> WithParentPersonIds
        {
            get
            {
                if ( _withParentPersonIds == null )
                {
                    _withParentPersonIds = new List<int>();
                }
                return _withParentPersonIds;
            }
            set
            {
                _withParentPersonIds = value;
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

            if ( !InWorshipPersonIds.Contains( personId ) )
            {
                InWorshipPersonIds.Add( personId );
            }
        }

        public static void RemoveFromWorship( int personId )
        {
            UpdateEffectiveDate();
            if ( InWorshipPersonIds.Contains( personId ) )
            {
                InWorshipPersonIds.Remove( personId );
            }
        }

        public static bool IsInWorship( int personId )
        {
            UpdateEffectiveDate();
            return InWorshipPersonIds.Contains( personId );
        }

        public static void AddToWithParent( int personId )
        {
            UpdateEffectiveDate();

            if ( !WithParentPersonIds.Contains( personId ) )
            {
                WithParentPersonIds.Add( personId );
            }
        }

        public static void RemoveFromWithParent( int personId )
        {
            UpdateEffectiveDate();
            if ( WithParentPersonIds.Contains( personId ) )
            {
                WithParentPersonIds.Remove( personId );
            }
        }

        public static bool IsWithParent( int personId )
        {
            UpdateEffectiveDate();
            return WithParentPersonIds.Contains( personId );
        }

        private static void UpdateEffectiveDate()
        {
            if ( EffectiveDate != Rock.RockDateTime.Today )
            {
                EffectiveDate = Rock.RockDateTime.Today;
                InWorshipPersonIds = new List<int>();
            }
        }
    }
}
