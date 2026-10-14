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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using org.secc.FamilyCheckin.Cache;

using Xunit;

namespace org.secc.FamilyCheckin.Tests
{
    /// <summary>
    /// ROCK-8818: a bulk load must never overwrite a cache entry that was updated after the load read
    /// from the database. A dictionary stands in for RockCache.
    /// </summary>
    public class CheckinWriteGuardTests
    {
        private readonly CheckinWriteGuard _guard = new CheckinWriteGuard();
        private readonly Dictionary<string, string> _cache = new Dictionary<string, string>();

        private void LiveWrite( string key, string value )
        {
            _guard.RecordWrite( key, () => _cache[key] = value );
        }

        private void LiveRemove( string key )
        {
            _guard.RecordWrite( key, () => _cache.Remove( key ) );
        }

        private bool LoadedWrite( string key, string value, long readStamp )
        {
            return _guard.TryWriteLoaded( key, readStamp, () => _cache[key] = value );
        }

        [Fact]
        public void LiveUpdateDuringLoad_IsNotOverwritten()
        {
            // Bulk load reads X as InRoom, RoomScanner checks X out, then the bulk load writes.
            var stamp = _guard.BeginLoad();
            LiveWrite( "X", "CheckedOut" );

            Assert.False( LoadedWrite( "X", "InRoom", stamp ) );
            _guard.EndLoad();

            Assert.Equal( "CheckedOut", _cache["X"] );
        }

        [Fact]
        public void RemoveDuringLoad_IsNotResurrected()
        {
            var stamp = _guard.BeginLoad();
            LiveWrite( "X", "InRoom" );
            LiveRemove( "X" );

            Assert.False( LoadedWrite( "X", "InRoom", stamp ) );
            _guard.EndLoad();

            Assert.False( _cache.ContainsKey( "X" ) );
        }

        [Fact]
        public void ClearDuringLoad_BlocksEveryLoadedWrite()
        {
            var stamp = _guard.BeginLoad();
            _guard.RecordClear( () => _cache.Clear() );

            Assert.False( LoadedWrite( "X", "InRoom", stamp ) );
            Assert.False( LoadedWrite( "Y", "InRoom", stamp ) );
            _guard.EndLoad();

            Assert.Empty( _cache );
        }

        [Fact]
        public void WriteBeforeLoad_DoesNotBlockLoadedWrite()
        {
            LiveWrite( "X", "EnRoute" );
            var stamp = _guard.BeginLoad();

            Assert.True( LoadedWrite( "X", "InRoom", stamp ) );
            _guard.EndLoad();

            Assert.Equal( "InRoom", _cache["X"] );
        }

        [Fact]
        public void WriteToOtherKeyDuringLoad_DoesNotBlockLoadedWrite()
        {
            var stamp = _guard.BeginLoad();
            LiveWrite( "Y", "CheckedOut" );

            Assert.True( LoadedWrite( "X", "InRoom", stamp ) );
            _guard.EndLoad();
        }

        [Fact]
        public void LaterLoad_IsNotBlockedByWriteRecordedForEarlierLoad()
        {
            var first = _guard.BeginLoad();
            LiveWrite( "X", "CheckedOut" );
            var second = _guard.BeginLoad();

            Assert.False( LoadedWrite( "X", "InRoom", first ) );
            Assert.True( LoadedWrite( "X", "CheckedOut", second ) );

            _guard.EndLoad();
            _guard.EndLoad();
        }

        [Fact]
        public void Stamps_AreKeptUntilTheLastLoadEnds()
        {
            _guard.BeginLoad();
            _guard.BeginLoad();
            LiveWrite( "X", "CheckedOut" );

            _guard.EndLoad();
            Assert.Equal( 1, _guard.TrackedKeyCount );

            _guard.EndLoad();
            Assert.Equal( 0, _guard.TrackedKeyCount );
        }

        [Fact]
        public void WritesWithNoLoadRunning_AreNotTracked()
        {
            LiveWrite( "X", "InRoom" );
            LiveRemove( "Y" );

            Assert.Equal( 0, _guard.TrackedKeyCount );
        }

        [Fact]
        public void ConcurrentLiveWrites_AreNeverOverwrittenByOlderLoads()
        {
            // Live writers store increasing versions; each load "reads" the latest version before
            // BeginLoad's stamp would allow, then writes it back. The cache must never go backwards
            // from the last version a live writer stored.
            var cache = new ConcurrentDictionary<string, int>();
            var lastLive = new int[4];
            var keys = new[] { "A", "B", "C", "D" };
            var guard = new CheckinWriteGuard();

            var writers = Task.Run( () =>
            {
                Parallel.For( 0, keys.Length, k =>
                {
                    for ( int v = 1; v <= 2000; v++ )
                    {
                        guard.RecordWrite( keys[k], () =>
                        {
                            cache[keys[k]] = v;
                            lastLive[k] = v;
                        } );
                    }
                } );
            } );

            var loaders = Task.Run( () =>
            {
                Parallel.For( 0, 2000, i =>
                {
                    var k = i % keys.Length;
                    var stamp = guard.BeginLoad();
                    try
                    {
                        // The "database read": whatever was committed at or after the stamp was taken.
                        var read = Volatile.Read( ref lastLive[k] );
                        Thread.SpinWait( 50 );
                        guard.TryWriteLoaded( keys[k], stamp, () => cache[keys[k]] = read );
                    }
                    finally
                    {
                        guard.EndLoad();
                    }
                } );
            } );

            Task.WaitAll( writers, loaders );

            for ( int k = 0; k < keys.Length; k++ )
            {
                Assert.Equal( 2000, cache[keys[k]] );
            }
        }
    }

    /// <summary>
    /// ROCK-8818: callers that arrive while a bulk load runs share its result or its failure
    /// instead of queuing to repeat it.
    /// </summary>
    public class SingleFlightTests
    {
        [Fact]
        public void Waiters_ShareTheRunningLoadsResult()
        {
            var flight = new SingleFlight<int>();
            var started = new ManualResetEventSlim();
            var release = new ManualResetEventSlim();
            int loads = 0;

            Func<int> load = () =>
            {
                Interlocked.Increment( ref loads );
                started.Set();
                release.Wait();
                return 42;
            };

            bool ownerJoined = true;
            var owner = Task.Run( () => flight.Run( load, out ownerJoined ) );
            started.Wait();

            var joinedFlags = new ConcurrentBag<bool>();
            var waiters = new Task<int>[5];
            for ( int i = 0; i < waiters.Length; i++ )
            {
                waiters[i] = Task.Run( () =>
                {
                    bool joined;
                    var value = flight.Run( load, out joined );
                    joinedFlags.Add( joined );
                    return value;
                } );
            }

            // Give the waiters time to reach the running flight before it finishes.
            Thread.Sleep( 200 );
            release.Set();

            Assert.Equal( 42, owner.Result );
            Assert.False( ownerJoined );
            foreach ( var waiter in waiters )
            {
                Assert.Equal( 42, waiter.Result );
            }

            Assert.Equal( 1, loads );
            Assert.All( joinedFlags, j => Assert.True( j ) );
        }

        [Fact]
        public void Waiters_ShareTheRunningLoadsFailure_AndTheNextCallRetries()
        {
            var flight = new SingleFlight<int>();
            var started = new ManualResetEventSlim();
            var release = new ManualResetEventSlim();
            int loads = 0;

            Func<int> failing = () =>
            {
                Interlocked.Increment( ref loads );
                started.Set();
                release.Wait();
                throw new TimeoutException( "db timeout" );
            };

            bool joined;
            var owner = Task.Run( () => flight.Run( failing, out joined ) );
            started.Wait();
            var waiter = Task.Run( () => flight.Run( failing, out joined ) );
            Thread.Sleep( 200 );
            release.Set();

            Assert.IsType<TimeoutException>( Assert.Throws<AggregateException>( () => owner.Wait() ).InnerException );
            Assert.IsType<TimeoutException>( Assert.Throws<AggregateException>( () => waiter.Wait() ).InnerException );
            Assert.Equal( 1, loads );

            // The failed flight is gone; the next caller starts a fresh load.
            Assert.Equal( 7, flight.Run( () => 7, out joined ) );
            Assert.False( joined );
        }

        [Fact]
        public void ReentrantCall_RunsDirectlyInsteadOfDeadlocking()
        {
            var flight = new SingleFlight<int>();
            bool outerJoined, innerJoined = true;

            var value = flight.Run( () => flight.Run( () => 5, out innerJoined ) + 1, out outerJoined );

            Assert.Equal( 6, value );
            Assert.False( outerJoined );
            Assert.False( innerJoined );
        }
    }
}
