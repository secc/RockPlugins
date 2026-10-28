using System;
using System.Threading;

namespace org.secc.FamilyCheckin.Cache
{
    /// <summary>
    /// Runs one load at a time and lets callers that arrive while it runs share its outcome.
    /// <para>
    /// A caller that finds a load running waits for it and gets the same result, or the same
    /// exception, instead of queuing up to run its own load after it. Once a load finishes the next
    /// caller starts a new one, so a failed load is retried by the next request, not by every waiter.
    /// A load that calls back into <see cref="Run"/> on its own thread runs its inner load directly.
    /// </para>
    /// </summary>
    internal sealed class SingleFlight<TResult>
    {
        private readonly object _lock = new object();
        private Lazy<TResult> _current;
        private int _ownerThreadId;

        /// <summary>
        /// Runs <paramref name="load"/>, or waits for the load already running and returns its result.
        /// </summary>
        /// <param name="joined">True when the result came from another caller's load.</param>
        public TResult Run( Func<TResult> load, out bool joined )
        {
            Lazy<TResult> flight;
            lock ( _lock )
            {
                if ( _current != null && _ownerThreadId == Thread.CurrentThread.ManagedThreadId )
                {
                    // Re-entered from inside our own load; waiting on it would deadlock.
                    joined = false;
                    return load();
                }

                joined = _current != null;
                if ( !joined )
                {
                    _current = new Lazy<TResult>( () =>
                    {
                        _ownerThreadId = Thread.CurrentThread.ManagedThreadId;
                        return load();
                    }, LazyThreadSafetyMode.ExecutionAndPublication );
                }

                flight = _current;
            }

            try
            {
                return flight.Value;
            }
            finally
            {
                lock ( _lock )
                {
                    if ( _current == flight )
                    {
                        _current = null;
                        _ownerThreadId = 0;
                    }
                }
            }
        }
    }
}
