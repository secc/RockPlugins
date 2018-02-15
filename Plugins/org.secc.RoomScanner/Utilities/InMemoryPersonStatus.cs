// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
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
