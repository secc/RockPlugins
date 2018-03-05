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
using org.secc.FamilyCheckin.Data;
using System.Data.Entity;
using System.Linq;
using Rock.Data;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace org.secc.FamilyCheckin.Model
{
    public class KioskService : FamilyCheckinService<Kiosk>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="KioskService"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public KioskService(RockContext context) : base( context ) { }

        public Kiosk GetByIPAddress( string ipAddress, bool skipReverseLookup = false )
        {
            string hostValue = ipAddress;

            if ( !skipReverseLookup )
            {
                // Lookup the system's "name" (as seen in the DNS) using the given IP
                // address because when using DHCP the kiosk may have a different IP from time to time
                // -- however the fully qualified name should always be the same.
                try
                {
                    hostValue = System.Net.Dns.GetHostEntry( ipAddress ).HostName;
                }
                catch ( SocketException )
                {
                    // TODO: consider whether we want to log the IP address that caused this error.
                    // As per http://msdn.microsoft.com/en-us/library/ms143998.aspx it *may* mean 
                    // a stale DNS record for an IPv4 address that actually belongs to a
                    // different host was going to be returned (there is a DNS PTR record for
                    // the IPv4 address, but no DNS A record for the IPv4 address).
                    hostValue = ipAddress;
                }
            }

            Kiosk kiosk = null;
            // If we still have an IPv4 address then try to find it based on IP
            if ( Regex.IsMatch( hostValue, @"\d+\.\d+\.\d+\.\d+" ) )
            {
                // find by IP
                kiosk = Queryable()
                    .Where( k =>
                        k.IPAddress == hostValue )
                    .FirstOrDefault();
            }
            else
            {
                // find by name
                kiosk = Queryable()
                    .Where( k =>
                        k.Name == hostValue )
                    .FirstOrDefault();
            }
            return kiosk;
        }

        public Kiosk GetByClientName( string ClientName )
        {
            Kiosk kiosk = null;
            kiosk = Queryable()
                .Where( k => k.Name == ClientName )
                .FirstOrDefault();
            return kiosk;
        }
    }
}
