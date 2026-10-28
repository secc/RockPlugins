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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using org.secc.FamilyCheckin.Cache;

using Xunit;

namespace org.secc.FamilyCheckin.Tests
{
    /// <summary>
    /// ROCK-8772: exercises the check-in cache key list with an in-memory store and a controllable
    /// clock - no RockCache.
    /// </summary>
    public class CheckinKeyListTests
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds( 10 );

        // Mirrors RockCacheKeyListStore: stores the list by reference.
        private sealed class FakeStore : IKeyListStore
        {
            private volatile IReadOnlyList<string> _keys;

            public int SetCount;

            public IReadOnlyList<string> Get()
            {
                return _keys;
            }

            public void Set( IReadOnlyList<string> keys )
            {
                _keys = keys;
                Interlocked.Increment( ref SetCount );
            }

            public void Remove()
            {
                _keys = null;
            }
        }

        private sealed class Harness
        {
            public readonly FakeStore Store = new FakeStore();
            public TimeSpan Now = TimeSpan.FromMinutes( 1 );
            public List<string> DbKeys = new List<string>();
            public int LoadCount;
            public readonly CheckinKeyList KeyList;

            public Harness()
            {
                KeyList = new CheckinKeyList( Store, Interval, () => Now );
            }

            public IEnumerable<string> Load()
            {
                Interlocked.Increment( ref LoadCount );
                return DbKeys.ToList();
            }
        }

        [Fact]
        public void PublishedSnapshot_IsReadOnly()
        {
            var h = new Harness { DbKeys = { "a" } };

            var keys = h.KeyList.GetOrLoad( h.Load );

            Assert.False( keys is List<string> );
            Assert.True( ( ( ICollection<string> ) keys ).IsReadOnly );
            Assert.Throws<NotSupportedException>( () => ( ( ICollection<string> ) keys ).Add( "x" ) );
        }

        [Fact]
        public void EnsureAndRemove_DoNotChangeAnEarlierSnapshot()
        {
            var h = new Harness { DbKeys = { "a", "b" } };
            var before = h.KeyList.GetOrLoad( h.Load );

            h.KeyList.Ensure( h.Load, "c" );
            h.KeyList.Remove( h.Load, "a" );

            Assert.Equal( new[] { "a", "b" }, before );
            Assert.Equal( new[] { "b", "c" }, h.KeyList.Current );
            Assert.Equal( 1, h.LoadCount );
        }

        [Fact]
        public void EnumeratingASnapshot_SurvivesConcurrentChanges()
        {
            var h = new Harness { DbKeys = Enumerable.Range( 0, 100 ).Select( i => $"k{i}" ).ToList() };
            var snapshot = h.KeyList.GetOrLoad( h.Load );

            var seen = new List<string>();
            using ( var e = snapshot.GetEnumerator() )
            {
                var i = 0;
                while ( e.MoveNext() )
                {
                    seen.Add( e.Current );
                    h.KeyList.Ensure( h.Load, $"new{i}" );
                    h.KeyList.Remove( h.Load, $"k{i++}" );
                }
            }

            Assert.Equal( h.DbKeys, seen );
            Assert.Equal( Enumerable.Range( 0, 100 ).Select( i => $"new{i}" ), h.KeyList.Current );
        }

        [Fact]
        public void EmptyList_IsCachedAndNotReloaded()
        {
            var h = new Harness();

            var first = h.KeyList.GetOrLoad( h.Load );
            var second = h.KeyList.GetOrLoad( h.Load );
            h.KeyList.Remove( h.Load, "missing" );

            Assert.Empty( first );
            Assert.Same( first, second );
            Assert.Equal( 1, h.LoadCount );
        }

        [Fact]
        public void RemovingTheLastKey_LeavesAnEmptyCachedList()
        {
            var h = new Harness { DbKeys = { "a" } };
            h.KeyList.GetOrLoad( h.Load );

            h.KeyList.Remove( h.Load, "a" );

            Assert.NotNull( h.KeyList.Current );
            Assert.Empty( h.KeyList.GetOrLoad( h.Load ) );
            Assert.Equal( 1, h.LoadCount );
        }

        [Fact]
        public void NoOpChanges_DoNotCopyOrPublish()
        {
            var h = new Harness { DbKeys = { "a" } };
            var keys = h.KeyList.GetOrLoad( h.Load );
            var sets = h.Store.SetCount;

            h.KeyList.Ensure( h.Load, "a" );
            h.KeyList.Remove( h.Load, "missing" );
            h.KeyList.ApplyRemoteChange( "a", null );

            Assert.Same( keys, h.KeyList.Current );
            Assert.Equal( sets, h.Store.SetCount );
        }

        [Fact]
        public void RemoveWhenNotLoaded_DoesNotLoad()
        {
            var h = new Harness { DbKeys = { "a" } };

            h.KeyList.Remove( h.Load, "a" );

            Assert.Null( h.KeyList.Current );
            Assert.Equal( 0, h.LoadCount );
        }

        [Fact]
        public void Throttle_ReloadsOnlyAfterTheInterval()
        {
            var h = new Harness { DbKeys = { "a" } };
            h.KeyList.GetOrLoad( h.Load );

            h.DbKeys.Add( "db-only" );
            h.Now += TimeSpan.FromSeconds( 9 );
            h.KeyList.Ensure( h.Load, "b" );
            Assert.Equal( 1, h.LoadCount );
            Assert.Equal( new[] { "a", "b" }, h.KeyList.Current );

            h.Now += TimeSpan.FromSeconds( 1 );
            h.KeyList.Ensure( h.Load, "c" );
            Assert.Equal( 2, h.LoadCount );
            Assert.Equal( new[] { "a", "db-only", "c" }, h.KeyList.Current );
        }

        [Fact]
        public void Throttle_UsesTheSuppliedClock()
        {
            // CheckinCache supplies a Stopwatch, so a wall-clock change cannot hold the list in the
            // throttle window. Here the clock never advances, so no reload happens.
            var h = new Harness { DbKeys = { "a" } };
            h.KeyList.GetOrLoad( h.Load );

            for ( var i = 0; i < 5; i++ )
            {
                h.KeyList.Ensure( h.Load, $"k{i}" );
            }

            Assert.Equal( 1, h.LoadCount );
        }

        [Fact]
        public void Load_RunsOutsideTheLock_AndReplaysChangesMadeMeanwhile()
        {
            var h = new Harness { DbKeys = { "a", "gone" } };
            h.KeyList.GetOrLoad( h.Load );
            h.Now += Interval;

            var loading = new ManualResetEventSlim();
            var release = new ManualResetEventSlim();
            Func<IEnumerable<string>> slowLoad = () =>
            {
                loading.Set();
                release.Wait();
                return new[] { "a", "gone", "from-db" };
            };

            var refresh = Task.Run( () => h.KeyList.Ensure( slowLoad, "refresher" ) );
            Assert.True( loading.Wait( TimeSpan.FromSeconds( 5 ) ) );

            // These must not block behind the load.
            var others = Task.Run( () =>
            {
                h.KeyList.Ensure( h.Load, "during" );
                h.KeyList.Remove( h.Load, "gone" );
                h.KeyList.ApplyRemoteChange( "remote", null );
            } );
            Assert.True( others.Wait( TimeSpan.FromSeconds( 5 ) ), "changes blocked behind the key load" );
            Assert.Equal( new[] { "a", "during", "remote" }, h.KeyList.Current );

            release.Set();
            Assert.True( refresh.Wait( TimeSpan.FromSeconds( 5 ) ) );

            Assert.Equal( new[] { "a", "from-db", "during", "remote", "refresher" }, h.KeyList.Current );
            Assert.Equal( 1, h.LoadCount );
        }

        [Fact]
        public void ConcurrentFirstLoads_LoadOnce()
        {
            var h = new Harness { DbKeys = { "a" } };
            var release = new ManualResetEventSlim();
            var loads = 0;
            Func<IEnumerable<string>> slowLoad = () =>
            {
                Interlocked.Increment( ref loads );
                release.Wait();
                return new[] { "a" };
            };

            var readers = Enumerable.Range( 0, 8 ).Select( _ => Task.Run( () => h.KeyList.GetOrLoad( slowLoad ) ) ).ToArray();
            Thread.Sleep( 100 );
            release.Set();
            Assert.True( Task.WaitAll( readers, TimeSpan.FromSeconds( 5 ) ) );

            Assert.Equal( 1, loads );
            Assert.All( readers, r => Assert.Equal( new[] { "a" }, r.Result ) );
        }

        [Fact]
        public void KeyFactoryThatCallsBack_DoesNotDeadlock()
        {
            var h = new Harness();
            Func<IEnumerable<string>> load = null;
            load = () =>
            {
                Assert.Empty( h.KeyList.GetOrLoad( load ) );
                h.KeyList.Ensure( load, "added-in-factory" );
                h.KeyList.Remove( load, "removed-in-factory" );
                return new[] { "a", "removed-in-factory" };
            };

            var task = Task.Run( () => h.KeyList.GetOrLoad( load ) );

            Assert.True( task.Wait( TimeSpan.FromSeconds( 5 ) ), "key factory re-entry deadlocked" );
            Assert.Equal( new[] { "a", "added-in-factory" }, task.Result );
        }

        [Fact]
        public void FailedLoad_LetsTheNextCallerLoad()
        {
            var h = new Harness { DbKeys = { "a" } };

            Assert.Throws<InvalidOperationException>( () => h.KeyList.GetOrLoad( () => throw new InvalidOperationException() ) );

            Assert.Equal( new[] { "a" }, h.KeyList.GetOrLoad( h.Load ) );
        }

        [Fact]
        public void RemoteChanges_ApplyToTheCachedList_AndAreIgnoredWhenNotLoaded()
        {
            var h = new Harness { DbKeys = { "a", "b" } };

            h.KeyList.ApplyRemoteChange( "x", null );
            Assert.Null( h.KeyList.Current );

            h.KeyList.GetOrLoad( h.Load );
            h.KeyList.ApplyRemoteChange( "c", null );
            h.KeyList.ApplyRemoteChange( null, "a" );

            Assert.Equal( new[] { "b", "c" }, h.KeyList.Current );
            Assert.Equal( 1, h.LoadCount );
        }

        [Fact]
        public void Invalidate_MakesTheNextReadReload()
        {
            var h = new Harness { DbKeys = { "a" } };
            h.KeyList.GetOrLoad( h.Load );

            h.KeyList.Invalidate();
            h.DbKeys.Add( "b" );

            Assert.Null( h.KeyList.Current );
            Assert.Equal( new[] { "a", "b" }, h.KeyList.GetOrLoad( h.Load ) );
            Assert.Equal( 2, h.LoadCount );
        }

        [Fact]
        public void ConcurrentReadersAndWriters_NeverThrowOrSeeDuplicates()
        {
            var h = new Harness { DbKeys = Enumerable.Range( 0, 2000 ).Select( i => $"k{i}" ).ToList() };
            h.KeyList.GetOrLoad( h.Load );

            var stop = DateTime.UtcNow.AddSeconds( 2 );
            var writer = Task.Run( () =>
            {
                var i = 0;
                while ( DateTime.UtcNow < stop )
                {
                    h.KeyList.Ensure( h.Load, $"n{i}" );
                    h.KeyList.Remove( h.Load, $"n{i - 50}" );
                    h.KeyList.Remove( h.Load, $"k{i % 2000}" );
                    h.KeyList.ApplyRemoteChange( $"k{i % 2000}", null );
                    i++;
                }
            } );

            var readers = Enumerable.Range( 0, 4 ).Select( _ => Task.Run( () =>
            {
                while ( DateTime.UtcNow < stop )
                {
                    var keys = h.KeyList.GetOrLoad( h.Load );
                    var copy = keys.ToList();
                    Assert.DoesNotContain( null, copy );
                    Assert.Equal( copy.Count, copy.Distinct().Count() );
                }
            } ) ).ToArray();

            Task.WaitAll( readers.Concat( new[] { writer } ).ToArray() );
        }
    }
}
