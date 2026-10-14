using System;
using System.Collections.Generic;

namespace org.secc.FamilyCheckin.Cache
{
    /// <summary>
    /// Keeps a load from overwriting a newer cache write.
    /// <para>
    /// A load reads from the database and then writes what it read into the cache. If a live update
    /// (a check-in, a checkout, a message from another node) writes the same key between that read and
    /// that write, the load's copy is older and must be dropped. A load takes a stamp with
    /// <see cref="BeginLoad"/> before it reads; every live write made while any load is running records
    /// a newer stamp for its key; <see cref="TryWriteLoaded"/> skips the load's write when the key was
    /// written after the load's stamp.
    /// </para>
    /// <para>
    /// The cache writes themselves run under the guard's lock so the check and the write cannot be
    /// split by another write. They must be in-memory only; database work stays outside the lock.
    /// Stamps are only kept while a load is running, so the map stays small.
    /// </para>
    /// </summary>
    internal sealed class CheckinWriteGuard
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, long> _lastWrite = new Dictionary<string, long>();

        private long _sequence;
        private int _loadsInFlight;

        // Stamp of the last clear made while a load was running; a clear counts as a write to every key.
        private long _lastClear;

        /// <summary>
        /// Starts a load. Call before reading from the database and pass the result to
        /// <see cref="TryWriteLoaded"/>. Every call must be paired with <see cref="EndLoad"/>.
        /// </summary>
        public long BeginLoad()
        {
            lock ( _lock )
            {
                _loadsInFlight++;
                return _sequence;
            }
        }

        /// <summary>
        /// Ends a load started with <see cref="BeginLoad"/>.
        /// </summary>
        public void EndLoad()
        {
            lock ( _lock )
            {
                if ( _loadsInFlight > 0 )
                {
                    _loadsInFlight--;
                }

                if ( _loadsInFlight == 0 )
                {
                    _lastWrite.Clear();
                    _lastClear = 0;
                }
            }
        }

        /// <summary>
        /// Runs a live write (add, update or remove) of <paramref name="key"/> and records it so a load
        /// that read before it will not overwrite it.
        /// </summary>
        public void RecordWrite( string key, Action write )
        {
            lock ( _lock )
            {
                write();
                if ( _loadsInFlight > 0 )
                {
                    _lastWrite[key] = ++_sequence;
                }
            }
        }

        /// <summary>
        /// Runs a live clear of every key and records it so no running load writes its older copies back.
        /// </summary>
        public void RecordClear( Action clear )
        {
            lock ( _lock )
            {
                clear();
                if ( _loadsInFlight > 0 )
                {
                    _lastWrite.Clear();
                    _lastClear = ++_sequence;
                }
            }
        }

        /// <summary>
        /// Runs the load's write of <paramref name="key"/> unless the key was written or cleared after
        /// <paramref name="readStamp"/>. Returns false when the write was skipped.
        /// </summary>
        public bool TryWriteLoaded( string key, long readStamp, Action write )
        {
            lock ( _lock )
            {
                long lastWrite;
                if ( _lastClear > readStamp || ( _lastWrite.TryGetValue( key, out lastWrite ) && lastWrite > readStamp ) )
                {
                    return false;
                }

                write();
                return true;
            }
        }

        /// <summary>
        /// The number of keys with a recorded write. Used by tests.
        /// </summary>
        internal int TrackedKeyCount
        {
            get
            {
                lock ( _lock )
                {
                    return _lastWrite.Count;
                }
            }
        }
    }
}
