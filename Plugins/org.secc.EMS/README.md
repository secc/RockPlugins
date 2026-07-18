# org.secc.EMS

> Thin SOAP client + Rock block that pulls room/event bookings from the EMS (Event Management System) facility-scheduling product and lists them in Rock.

## Overview

EMS is Southeast's integration with the **EMS Software** room/facility-booking system. The plugin
wraps the EMS SOAP web service behind a small `API` facade, normalizes raw bookings into web-friendly
`webEvent` objects, and surfaces them through a single Rock block ("View Events") that
shows the day's scheduled events. The facade also exposes HVAC-zone logic (`GetHVACZones` /
`CombineAdjacentEventsInZone`, producing `zoneEvent` objects) that coalesces adjacent bookings per HVAC
zone — but nothing in this plugin calls it; the View Events block only uses `GetWebEvents`. All data is
read-only and fetched live from EMS on each request.

## Project Info

- **Project file:** `org.secc.EMS.csproj`
- **Root namespace:** `org.secc.EMS`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly) and `RockWeb/Plugins/org_secc/` (block `.ascx` markup), via the PostBuildEvent `xcopy`
- **Service reference:** WCF/SOAP `basicHttpBinding` proxy generated under `Service References/EMSServiceRef/` (`ServiceSoapClient`)

## Project Layout

```
API.cs          Public facade — GetWebEvents / GetHVACZones and the filtering/zone-combining logic
APIMethods.cs   Internal SOAP calls (GetAllBookings, GetEventTypes) + XML deserialization
APIData.cs      Auto-generated (xsd) DTOs: Bookings/BookingsData, EventTypes, Errors
Events.cs       Plain output models: webEvent, zoneEvent
Settings.cs     EMS credentials (app settings) + hard-coded status-id filters
app.config      WCF basicHttpBinding config for the EMS SOAP endpoint
Service References/EMSServiceRef/   Generated WCF proxy (Reference.cs) + WSDL/svcmap
/org_secc/EMS/  ViewEvents block (.ascx + .ascx.cs)
```

## Components

### Blocks

Category in Rock: **SECC > EMS**.

| Block | Purpose | Key settings |
|-------|---------|--------------|
| View Events | Grid of EMS events for a chosen day; user-pref date picker and "Show All Events" toggle (off = web-enabled event types only). | **Building IDs** (`TextField`, comma-separated EMS building ids; blank = all buildings) |

### Public API (facade — `API` class)

Not a Rock REST endpoint — a plain C# facade other code (the block, and potentially jobs/Lava) calls in-process.

| Method | Purpose |
|--------|---------|
| `GetWebEvents(...)` | Fetch bookings for a date range across one or more buildings, then filter to web-displayable events (optionally web-enabled-only). Returns `List<webEvent>`. |
| `FilterEventsForWeb(...)` | Filter raw `BookingsData` to `webEvent`s: restrict to allowed status ids, and (optionally) only event types flagged `DisplayOnWeb`. Has a `showSetup` flag to drop `SET UP` rows, but the block's call path always passes `showSetup: true`, so `SET UP` rows are *not* filtered out on the web path. |
| `GetHVACZones(...)` | Fetch bookings and group them into combined per-zone occupancy windows (`zoneEvent`). Not called anywhere in this plugin — available for external callers. |
| `CombineAdjacentEventsInZone(...)` | Merge contiguous/overlapping bookings within the same HVAC zone into a single time window. |

### Output models (`Events.cs`)

| Type | Purpose |
|------|---------|
| `webEvent` | Booking/event start+end times, activity name, location, optional `DisplayOnWeb`. Bound to the grid. |
| `zoneEvent` | An HVAC zone with a combined occupancy window and the rooms it covers. |

## Dependencies & Integrations

- **Rock:** `RockBlock`, Rock grid/filter UI (`Rock:Grid`, `GridFilter`, `DatePicker`), block attributes, user preferences. No `RockContext`/EF — the plugin reads no Rock data.
- **Third-party:** EMS Software SOAP web service (`ServiceSoapClient` over `basicHttpBinding`); `System.Runtime.Serialization` / `System.Xml.Serialization` for the response XML.
- No migrations, jobs, Lava filters, or field types.

## Observations

*Noticed while documenting — not a full audit.*

- **Security (medium):** The SOAP binding in `app.config` uses `<security mode="None">`, so EMS
  credentials (`Settings.UserName` / `Settings.Password`) and all booking data travel **unencrypted**
  unless the EMS endpoint URL is itself HTTPS. Worth confirming the configured endpoint is `https://`
  and that the binding/transport security matches.
- **Security (low):** EMS credentials are read from plain Rock/web `AppSettings` keys (`EmsUser`,
  `EmsPassword`) via `ConfigurationManager`, not Rock's encrypted attribute storage. Confirm
  `web.config` is not exposed and that these keys are managed as secrets.
- **Improvement:** `Settings.HVAC_enabled_booking_status_ids` and `Default_web_event_status_ids` are
  hard-coded to `{ "1" }` in code. Changing which EMS booking statuses are considered "confirmed"
  requires a recompile rather than configuration.
- **Improvement:** Filtering keys off English string matching — `CombineAdjacentEventsInZone` (HVAC path)
  drops bookings whose name begins with `"SET UP"`, and event-type web-enablement is compared against the
  literal string `"true"`. Both are brittle to data-entry/casing changes in EMS. (Note: the block's web
  path passes `showSetup: true`, so `SET UP` rows are *not* dropped there.)
- **Improvement:** `BindGrid` (block) sets `gScroll.PageSize = 5000` and pulls a full day of bookings on
  every load with no server-side paging — fine for a day view, but a wide building/date range could be heavy.

## Making Changes

- To change what the block shows (columns, date handling, the "Show All" behavior), edit
  `org_secc/EMS/ViewEvents.ascx[.cs]`; building scope comes from the block's **Building IDs** setting.
- To change which EMS statuses/event types count as web-displayable or HVAC-relevant, edit the filter
  logic in `API.cs` and the hard-coded lists in `Settings.cs`.
- The only EMS SOAP operations actually wired up are `GetAllBookings` and `GetEventTypes` (in
  `APIMethods.cs`); the generated proxy exposes many more (`GetRooms`, `AddReservation`, `GetStatuses`,
  etc.) that are available but unused. Add new calls there, following the existing error-XML-detection pattern.
- EMS endpoint/credentials live in `app.config` (binding) and host `web.config` AppSettings
  (`EmsUser` / `EmsPassword`), surfaced through `Settings.cs`.
