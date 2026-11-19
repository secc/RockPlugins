// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
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
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using Rock.Data;

namespace org.secc.RegistrationCart.Services
{
    /// <summary>
    /// SQL Server application locks on registration instances, so two checkouts can't both
    /// count the same last seat. Locks are session-owned on this RockContext's connection
    /// (kept open while held), taken in id order so two carts can't deadlock, and released
    /// on dispose.
    /// </summary>
    public sealed class SeatLock : IDisposable
    {
        private const int TimeoutMilliseconds = 10000;

        private readonly RockContext _rockContext;
        private readonly bool _closeConnection;
        private readonly List<string> _resources = new List<string>();

        private SeatLock( RockContext rockContext )
        {
            _rockContext = rockContext;

            // A session lock lives on one connection, so keep EF from closing it between commands.
            var connection = rockContext.Database.Connection;
            if ( connection.State != System.Data.ConnectionState.Open )
            {
                connection.Open();
                _closeConnection = true;
            }
        }

        /// <summary>
        /// Locks every listed instance, or returns null (holding nothing) if one is still
        /// locked by another checkout after the timeout.
        /// </summary>
        public static SeatLock Acquire( RockContext rockContext, IEnumerable<int> registrationInstanceIds )
        {
            var seatLock = new SeatLock( rockContext );
            try
            {
                foreach ( var instanceId in registrationInstanceIds.Distinct().OrderBy( id => id ) )
                {
                    var resource = "org.secc.RegistrationCart.Seats." + instanceId;
                    var status = rockContext.Database.SqlQuery<int>(
                        "DECLARE @Status int; EXEC @Status = sp_getapplock @Resource = @Resource, @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = @Timeout; SELECT @Status;",
                        new SqlParameter( "@Resource", System.Data.SqlDbType.NVarChar, 255 ) { Value = resource },
                        new SqlParameter( "@Timeout", System.Data.SqlDbType.Int ) { Value = TimeoutMilliseconds } ).Single();

                    if ( status < 0 )
                    {
                        seatLock.Dispose();
                        return null;
                    }

                    seatLock._resources.Add( resource );
                }

                return seatLock;
            }
            catch
            {
                seatLock.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            foreach ( var resource in Enumerable.Reverse( _resources ) )
            {
                try
                {
                    _rockContext.Database.ExecuteSqlCommand( TransactionalBehavior.DoNotEnsureTransaction,
                        "EXEC sp_releaseapplock @Resource = @Resource, @LockOwner = 'Session';",
                        new SqlParameter( "@Resource", System.Data.SqlDbType.NVarChar, 255 ) { Value = resource } );
                }
                catch
                {
                    // A broken connection ends the session, which releases its locks anyway.
                }
            }

            _resources.Clear();

            if ( _closeConnection )
            {
                _rockContext.Database.Connection.Close();
            }
        }
    }
}
