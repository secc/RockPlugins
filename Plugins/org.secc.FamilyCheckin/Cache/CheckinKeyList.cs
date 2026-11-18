using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace org.secc.FamilyCheckin.Cache
{
    /// <summary>
    /// Where a <see cref="CheckinKeyList"/> keeps its published key list.
    /// </summary>
    internal interface IKeyListStore
    {
        /// <summary>
        /// Returns the published key list, or null when no list is cached.
        /// </summary>
        IReadOnlyList<string> Get();

        void Set( IReadOnlyList<string> keys );

        void Remove();
    }

    /// <summary>
    /// Maintains the list of qualified keys for one check-in cache type.
    /// <para>
    /// The published list is an immutable snapshot: every change builds a new list and publishes it,
    /// so readers can enumerate a list without locking. A null list means "not loaded"; an empty list
    /// is a valid, cached result. The key loader (a database query) runs outside the update lock, and
    /// changes made while it runs are replayed onto its result before that result is published.
    /// </para>
    /// </summary>
    internal sealed class CheckinKeyList
    {
        private readonly IKeyListStore _store;
        private readonly TimeSpan _refreshInterval;
        private readonly Func<TimeSpan> _clock;
        private readonly object _updateLock = new object();

        private TimeSpan? _lastRefresh;

        // Non-null while a refresh is loading keys; collects the changes to replay onto its result.
        private List<KeyChange> _pendingChanges;

        // The thread running the load, so a key factory that calls back into this list cannot wait on itself.
        private int _loadingThreadId;

        /// <param name="store">Where the published list is kept.</param>
        /// <param name="refreshInterval">Minimum time between reloads triggered by key changes.</param>
        /// <param name="clock">Monotonic elapsed time (not wall-clock time).</param>
        public CheckinKeyList( IKeyListStore store, TimeSpan refreshInterval, Func<TimeSpan> clock )
        {
            _store = store;
            _refreshInterval = refreshInterval;
            _clock = clock;
        }

        /// <summary>
        /// The published key list, or null when it has not been loaded.
        /// </summary>
        public IReadOnlyList<string> Current => _store.Get();

        /// <summary>
        /// Returns the published key list, loading it first if it is not cached.
        /// </summary>
        public IReadOnlyList<string> GetOrLoad( Func<IEnumerable<string>> loadKeys )
        {
            // Update returns null only when called from inside our own key factory.
            return Current ?? Update( loadKeys, KeyChange.None ) ?? Array.Empty<string>();
        }

        /// <summary>
        /// Adds the key if missing. Reloads from <paramref name="loadKeys"/> when the list is not cached
        /// or the refresh interval has passed.
        /// </summary>
        public void Ensure( Func<IEnumerable<string>> loadKeys, string key )
        {
            Update( loadKeys, new KeyChange( key, null ) );
        }

        /// <summary>
        /// Removes the key if present. Reloads from <paramref name="loadKeys"/> when the refresh interval
        /// has passed; does nothing when the list is not cached.
        /// </summary>
        public void Remove( Func<IEnumerable<string>> loadKeys, string key )
        {
            Update( loadKeys, new KeyChange( null, key ) );
        }

        /// <summary>
        /// Applies a change reported by another node. Never loads keys; when no list is cached the change
        /// is left for the next load to pick up.
        /// </summary>
        public void ApplyRemoteChange( string addedKey, string removedKey )
        {
            var change = new KeyChange( addedKey, removedKey );
            if ( change.IsEmpty )
            {
                return;
            }

            lock ( _updateLock )
            {
                _pendingChanges?.Add( change );

                var current = _store.Get();
                if ( current != null )
                {
                    Publish( current, change );
                }
            }
        }

        /// <summary>
        /// Drops the published list so the next read reloads it.
        /// </summary>
        public void Invalidate()
        {
            lock ( _updateLock )
            {
                _store.Remove();
            }
        }

        private IReadOnlyList<string> Update( Func<IEnumerable<string>> loadKeys, KeyChange change )
        {
            lock ( _updateLock )
            {
                while ( true )
                {
                    var current = _store.Get();

                    if ( current != null && ( _pendingChanges != null || !IsRefreshDue() ) )
                    {
                        // Up to date, or another thread is already reloading: change the snapshot now and
                        // let that reload replay the change.
                        _pendingChanges?.Add( change );
                        return Publish( current, change );
                    }

                    if ( current == null && change.AddedKey == null && !change.IsEmpty )
                    {
                        // Removing from a list that is not cached: nothing to do unless a load is in
                        // progress, in which case its result must not contain the key.
                        _pendingChanges?.Add( change );
                        return null;
                    }

                    if ( _pendingChanges == null )
                    {
                        break;
                    }

                    if ( _loadingThreadId == Thread.CurrentThread.ManagedThreadId )
                    {
                        // Called from inside our own key factory: the load will pick this change up.
                        _pendingChanges.Add( change );
                        return null;
                    }

                    // No list to change and another thread is loading one: wait for it.
                    Monitor.Wait( _updateLock );
                }

                _pendingChanges = new List<KeyChange>();
                _loadingThreadId = Thread.CurrentThread.ManagedThreadId;
            }

            List<string> loaded;
            try
            {
                loaded = loadKeys().Distinct().ToList();
            }
            catch
            {
                lock ( _updateLock )
                {
                    _pendingChanges = null;
                    _loadingThreadId = 0;
                    Monitor.PulseAll( _updateLock );
                }

                throw;
            }

            lock ( _updateLock )
            {
                foreach ( var pending in _pendingChanges )
                {
                    ApplyTo( loaded, pending );
                }

                ApplyTo( loaded, change );

                var published = loaded.AsReadOnly();
                _store.Set( published );
                _lastRefresh = _clock();

                _pendingChanges = null;
                _loadingThreadId = 0;
                Monitor.PulseAll( _updateLock );
                return published;
            }
        }

        private bool IsRefreshDue()
        {
            return !_lastRefresh.HasValue || _clock() - _lastRefresh.Value >= _refreshInterval;
        }

        /// <summary>
        /// Publishes a copy of <paramref name="current"/> with the change applied. Returns
        /// <paramref name="current"/> without copying when the change would not alter it.
        /// </summary>
        private IReadOnlyList<string> Publish( IReadOnlyList<string> current, KeyChange change )
        {
            var removeAt = change.RemovedKey == null ? -1 : IndexOf( current, change.RemovedKey );
            var add = change.AddedKey != null && IndexOf( current, change.AddedKey ) < 0;

            if ( removeAt < 0 && !add )
            {
                return current;
            }

            var updated = new List<string>( current.Count + 1 );
            for ( var i = 0; i < current.Count; i++ )
            {
                if ( i != removeAt )
                {
                    updated.Add( current[i] );
                }
            }

            if ( add )
            {
                updated.Add( change.AddedKey );
            }

            var published = updated.AsReadOnly();
            _store.Set( published );
            return published;
        }

        private static void ApplyTo( List<string> keys, KeyChange change )
        {
            if ( change.RemovedKey != null )
            {
                keys.Remove( change.RemovedKey );
            }

            if ( change.AddedKey != null && !keys.Contains( change.AddedKey ) )
            {
                keys.Add( change.AddedKey );
            }
        }

        private static int IndexOf( IReadOnlyList<string> keys, string key )
        {
            for ( var i = 0; i < keys.Count; i++ )
            {
                if ( keys[i] == key )
                {
                    return i;
                }
            }

            return -1;
        }

        private struct KeyChange
        {
            public static readonly KeyChange None = new KeyChange( null, null );

            public KeyChange( string addedKey, string removedKey )
            {
                AddedKey = string.IsNullOrEmpty( addedKey ) ? null : addedKey;
                RemovedKey = string.IsNullOrEmpty( removedKey ) ? null : removedKey;
            }

            public string AddedKey { get; }

            public string RemovedKey { get; }

            public bool IsEmpty => AddedKey == null && RemovedKey == null;
        }
    }
}
