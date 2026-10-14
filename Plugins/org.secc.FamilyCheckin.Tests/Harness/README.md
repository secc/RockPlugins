# AttendanceCache harness

A local-only harness for `AttendanceCache` (ROCK-8818). It checks bulk-load correctness, the
stale-write guard, shared cold loads, key cleanup and warm-path allocations against a running Rock
instance with real data. The unit tests in the parent folder cover the guard logic in isolation; this
harness covers the integration that the unit tests cannot (RockCache, EF, the message bus, IIS).

**Local dev instances only.** `AttendanceCacheHarness.ashx` has no authentication beyond rejecting
non-local requests, the `race` mode writes to `Attendance`, and the SQL scripts insert and delete rows.
Never copy these files to a shared or production server. Nothing in this folder is built or deployed:
the test project compiles only `*.cs`, and this folder is not linked into RockWeb.

## Run it

1. Build and deploy the plugin to the local RockWeb (`org.secc.FamilyCheckin.dll` in `RockWeb\Bin`), then
   start the site.
2. Load the fixture into the local database:
   `sqlcmd -S "(localdb)\MSSQLLocalDB" -d <RockDb> -i fixture.sql`
   - Defaults: 8,615 rows (a Sunday-sized key list) on Group 24 / Schedule 6 at Locations 11, 4, 5, 6, 7,
     plus 3 rows with no PersonAlias. Change the variables at the top for other data. The group must be a
     check-in group whose group locations have that schedule, so the rows map to an `OccurrenceCache` entry.
   - Every row is tagged `ForeignKey = 'attendance-cache-harness'`.
3. Copy `AttendanceCacheHarness.ashx` into the RockWeb root and call it with `mode=<mode>`, e.g.
   `curl "http://localhost:6229/AttendanceCacheHarness.ashx?mode=flush"`.
4. Clean up: run `cleanup.sql` (all residuals must be 0), call `mode=flush`, and delete the `.ashx` from RockWeb.

| Mode | What it checks |
|---|---|
| `flush` | `AttendanceCache.Clear()`. |
| `all` | Cold then warm `All()`: count vs today's DB rows, state breakdown, access keys, no-person rows. |
| `verify` | `AttendanceCache.Verify` (no exceptions; error count). |
| `par&threads=8` | Concurrent cold `All()` callers: all get every row and share one load (similar times). |
| `badkey` | Injects `AttendanceCache:0`, `:abc` and `: <id>` into the key list; one `All()` must remove them. |
| `race&n=100` | Checkout (save + `AddOrUpdate`, as RoomScanner does) timed across a cold `All()`. `stale` must be 0. |
| `isfull` | Group 24 / Schedule 6 occurrences: `IsFull` and active attendance from the cache vs the DB. |
| `alloc&calls=200` | Warm `All()` list capacity and whether its buffer is on the Large Object Heap (gen 2 right after allocation); average time. |

The `alloc` gen 2 collection count is noisy (background allocations in the app); use the buffer
capacity and generation as the result.

## Results (2026-10-12, Rock 16.12 LocalDB, 8,618 rows)

- `all`: 8,618 / 8,618; state counts match the fixture; no-person rows show as PersonId 0 / "Unnamed Person".
- `verify`: 0 errors. `par` (8 threads): all 8,618, ~3.6 s each (one shared load).
- `badkey`: all three removed by the first call; second call 10 ms.
- `race&n=100`: 0 stale (73 checkouts landed during the load). The pre-review build (2aba1c4) went stale
  10 times in 200 iterations at 3,003 rows.
- `alloc`: warm list capacity 8,618 (~69 KB, gen 0, not LOH), ~12 ms per warm call. Before the
  presized list, the warm list grew to 16,384 slots (128 KB, LOH) on every call.

## Not covered

The fixture uses the existing occurrences only (8 locally), so it does not reproduce a production-sized
cold `OccurrenceCache` load (~2,600 group-location-schedules). That is tracked in ROCK-9259.
