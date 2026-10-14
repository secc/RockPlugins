<%@ WebHandler Language="C#" Class="AttendanceCacheHarness" %>
// Local test harness for AttendanceCache (ROCK-8818). See README.md in this folder.
// LOCAL DEV INSTANCES ONLY: copy it into RockWeb for a test run and delete it afterwards. It has no
// authentication beyond rejecting non-local requests, and some modes write to the Attendance table.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using org.secc.FamilyCheckin.Cache;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

public class AttendanceCacheHarness : IHttpHandler
{
    const string Fk = "attendance-cache-harness";
    const string KeyListKey = "AttendanceCache:All";
    const string KeyListRegion = "AllItems";

    public bool IsReusable { get { return false; } }

    public void ProcessRequest( HttpContext context )
    {
        context.Response.ContentType = "text/plain";
        if ( !context.Request.IsLocal )
        {
            context.Response.StatusCode = 403;
            return;
        }

        var sb = new StringBuilder();
        var mode = context.Request["mode"] ?? "all";
        try
        {
            switch ( mode )
            {
                case "flush": AttendanceCache.Clear(); sb.AppendLine( "flushed" ); break;
                case "all": All( sb ); break;
                case "verify": Verify( sb ); break;
                case "par": Par( sb, ( context.Request["threads"] ?? "8" ).AsInteger() ); break;
                case "badkey": BadKey( sb ); break;
                case "race": Race( sb, ( context.Request["n"] ?? "40" ).AsInteger() ); break;
                case "isfull": IsFull( sb ); break;
                case "alloc": Alloc( sb, ( context.Request["calls"] ?? "200" ).AsInteger() ); break;
                default: sb.AppendLine( "unknown mode" ); break;
            }
        }
        catch ( Exception ex )
        {
            sb.AppendLine( "EXCEPTION: " + ex );
        }

        context.Response.Write( sb.ToString() );
    }

    static int DbTodayCount()
    {
        var today = RockDateTime.Today;
        return new AttendanceService( new RockContext() ).Queryable().Count( a => a.CreatedDateTime != null && a.CreatedDateTime >= today );
    }

    static void All( StringBuilder sb )
    {
        var sw = Stopwatch.StartNew();
        var items = AttendanceCache.All();
        sw.Stop();
        var sw2 = Stopwatch.StartNew();
        var again = AttendanceCache.All();
        sw2.Stop();
        sb.AppendLine( string.Format( "all: count={0} db_today={1} first_ms={2} warm_ms={3} warm_count={4} distinct_ids={5}",
            items.Count, DbTodayCount(), sw.ElapsedMilliseconds, sw2.ElapsedMilliseconds, again.Count, items.Select( i => i.Id ).Distinct().Count() ) );
        foreach ( var g in items.GroupBy( i => i.AttendanceState.ToString() + ( i.WithParent ? "+WithParent" : "" ) ).OrderBy( g => g.Key ) )
        {
            sb.AppendLine( string.Format( "  state {0}: {1}", g.Key, g.Count() ) );
        }

        sb.AppendLine( "  access keys: " + string.Join( ",", items.Select( i => i.OccurrenceAccessKey ).Distinct().OrderBy( k => k ) ) );
        foreach ( var a in items.Where( i => i.PersonId == 0 ) )
        {
            sb.AppendLine( string.Format( "  no-person row: Id={0} PersonId={1} Name='{2}' State={3} Loc={4}", a.Id, a.PersonId, a.PersonName, a.AttendanceState, a.LocationId ) );
        }
    }

    // Warm-path allocation check (CheckinMonitor calls All() once per room row every 10 s).
    // An array over 85 KB goes on the Large Object Heap, which .NET reports as generation 2 right after
    // allocation; LOH allocations also count toward the gen 2 budget, so gen 2 collections rise with them.
    static void Alloc( StringBuilder sb, int calls )
    {
        var items = AttendanceCache.All(); // make sure the cache is warm
        var field = typeof( List<AttendanceCache> ).GetField( "_items", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance );
        var buffer = ( Array ) field.GetValue( AttendanceCache.All() );
        sb.AppendLine( string.Format( "alloc: keys={0} list_capacity={1} buffer_bytes~{2:N0} buffer_gen={3}{4}",
            items.Count, buffer.Length, buffer.Length * IntPtr.Size, GC.GetGeneration( buffer ), GC.GetGeneration( buffer ) == 2 ? " (LOH)" : "" ) );

        var gen2Before = GC.CollectionCount( 2 );
        var sw = Stopwatch.StartNew();
        for ( int i = 0; i < calls; i++ )
        {
            AttendanceCache.All();
        }

        sw.Stop();
        sb.AppendLine( string.Format( "  {0} warm All() calls: avg_ms={1:N2} gen2_collections={2}",
            calls, sw.Elapsed.TotalMilliseconds / calls, GC.CollectionCount( 2 ) - gen2Before ) );
    }

    static void Verify( StringBuilder sb )
    {
        var errors = new List<string>();
        AttendanceCache.Verify( ref errors );
        sb.AppendLine( "verify: errors=" + errors.Count );
        foreach ( var e in errors.Take( 30 ) )
        {
            sb.AppendLine( "  " + e );
        }
    }

    static void Par( StringBuilder sb, int threads )
    {
        AttendanceCache.Clear();
        var counts = new int[threads];
        var ms = new long[threads];
        var errors = new string[threads];
        var gate = new ManualResetEvent( false );
        var list = new List<Thread>();
        for ( int t = 0; t < threads; t++ )
        {
            int i = t;
            var th = new Thread( () =>
            {
                gate.WaitOne();
                var sw = Stopwatch.StartNew();
                try { counts[i] = AttendanceCache.All().Count; }
                catch ( Exception ex ) { errors[i] = ex.GetBaseException().Message; }
                ms[i] = sw.ElapsedMilliseconds;
            } );
            th.Start();
            list.Add( th );
        }

        Thread.Sleep( 50 );
        gate.Set();
        list.ForEach( t => t.Join() );
        sb.AppendLine( string.Format( "par: threads={0} counts={1} ms={2} db_today={3} errors={4}", threads,
            string.Join( ",", counts ), string.Join( ",", ms ), DbTodayCount(), string.Join( ";", errors.Where( e => e != null ) ) ) );
    }

    static IReadOnlyList<string> KeyList()
    {
        return RockCache.Get( KeyListKey, KeyListRegion ) as IReadOnlyList<string>;
    }

    static void BadKey( StringBuilder sb )
    {
        var before = AttendanceCache.All().Count;
        var list = KeyList();
        var someId = list.First( k => k.StartsWith( "AttendanceCache:" ) );
        var id = someId.Substring( "AttendanceCache:".Length );
        var bad = new[] { "AttendanceCache:0", "AttendanceCache:abc", "AttendanceCache: " + id };
        var injected = new List<string>( list );
        injected.AddRange( bad );
        RockCache.AddOrUpdate( KeyListKey, KeyListRegion, injected.AsReadOnly() );
        sb.AppendLine( string.Format( "badkey: before_count={0} list_before={1} list_injected={2} injected={3}", before, list.Count, KeyList().Count, string.Join( ",", bad.Select( b => "'" + b + "'" ) ) ) );

        var sw = Stopwatch.StartNew();
        var c1 = AttendanceCache.All().Count;
        var ms1 = sw.ElapsedMilliseconds;
        var after = KeyList();
        sb.AppendLine( string.Format( "  first All(): count={0} ms={1} list={2} remaining_bad={3} canonical_present={4}",
            c1, ms1, after == null ? -1 : after.Count, after == null ? "n/a(list null)" : string.Join( ",", bad.Where( b => after.Contains( b ) ) ), after != null && after.Contains( someId ) ) );
        sw.Restart();
        var c2 = AttendanceCache.All().Count;
        var after2 = KeyList();
        sb.AppendLine( string.Format( "  second All(): count={0} ms={1} list={2} remaining_bad={3}",
            c2, sw.ElapsedMilliseconds, after2 == null ? -1 : after2.Count, after2 == null ? "n/a" : string.Join( ",", bad.Where( b => after2.Contains( b ) ) ) ) );
    }

    static void SetEnd( int id, DateTime? end )
    {
        var ctx = new RockContext();
        var a = new AttendanceService( ctx ).Get( id );
        a.EndDateTime = end;
        ctx.SaveChanges();
    }

    static void Race( StringBuilder sb, int n )
    {
        var targetId = new AttendanceService( new RockContext() ).Queryable()
            .Where( a => a.ForeignKey == Fk && a.EndDateTime == null && a.DidAttend == true && a.PersonAliasId != null && a.QualifierValueId == null )
            .OrderByDescending( a => a.Id ).Select( a => a.Id ).First();
        SetEnd( targetId, null );

        AttendanceCache.Clear();
        var sw = Stopwatch.StartNew();
        AttendanceCache.All();
        var coldMs = Math.Max( 1, sw.ElapsedMilliseconds );
        sb.AppendLine( string.Format( "race: target={0} cold_all_ms={1} iterations={2}", targetId, coldMs, n ) );

        int stale = 0, during = 0, before = 0, afterCount = 0, missing = 0;
        for ( int i = 0; i < n; i++ )
        {
            SetEnd( targetId, null );
            AttendanceCache.Clear();
            var delay = ( int ) ( coldMs * 1.2 * i / n );
            long aStart = 0, aEnd = 0, bDone = 0;
            var clock = Stopwatch.StartNew();
            var gate = new ManualResetEvent( false );

            var ta = new Thread( () =>
            {
                gate.WaitOne();
                aStart = clock.ElapsedTicks;
                AttendanceCache.All();
                aEnd = clock.ElapsedTicks;
            } );
            var tb = new Thread( () =>
            {
                gate.WaitOne();
                Thread.Sleep( delay );
                // RoomScanner checkout: save, then update the cache.
                var ctx = new RockContext();
                var a = new AttendanceService( ctx ).Get( targetId );
                a.EndDateTime = RockDateTime.Now;
                ctx.SaveChanges();
                AttendanceCache.AddOrUpdate( a );
                bDone = clock.ElapsedTicks;
            } );
            ta.Start();
            tb.Start();
            gate.Set();
            ta.Join();
            tb.Join();

            if ( bDone < aStart ) before++;
            else if ( bDone > aEnd ) afterCount++;
            else during++;

            var cached = RockCache.Get( "AttendanceCache:" + targetId ) as AttendanceCache;
            if ( cached == null )
            {
                missing++;
            }
            else if ( cached.AttendanceState != AttendanceState.CheckedOut )
            {
                stale++;
                sb.AppendLine( string.Format( "  iter {0} delay={1}ms: STALE cache state={2} (db=CheckedOut)", i, delay, cached.AttendanceState ) );
            }
        }

        SetEnd( targetId, null );
        AttendanceCache.Clear();
        sb.AppendLine( string.Format( "  result: stale={0} missing={1} | checkout finished before load={2} during load={3} after load={4}", stale, missing, before, during, afterCount ) );
    }

    static void IsFull( StringBuilder sb )
    {
        AttendanceCache.Clear();
        var today = RockDateTime.Today;
        var occurrences = OccurrenceCache.All().Where( o => o.GroupId == 24 && o.ScheduleId == 6 ).ToList();
        sb.AppendLine( "isfull: occurrences(group 24, schedule 6)=" + occurrences.Count );
        foreach ( var o in occurrences )
        {
            var cacheActive = o.Attendances.Count( a => a.AttendanceState != AttendanceState.CheckedOut );
            var dbActive = new AttendanceService( new RockContext() ).Queryable()
                .Count( a => a.CreatedDateTime >= today && a.EndDateTime == null && a.Occurrence.GroupId == o.GroupId && a.Occurrence.LocationId == o.LocationId && a.Occurrence.ScheduleId == o.ScheduleId );
            sb.AppendLine( string.Format( "  {0} loc={1} soft={2} firm={3} IsFull={4} cache_active={5} db_active={6}",
                o.AccessKey, o.LocationId, o.SoftRoomThreshold, o.FirmRoomThreshold, o.IsFull, cacheActive, dbActive ) );
        }
    }
}
