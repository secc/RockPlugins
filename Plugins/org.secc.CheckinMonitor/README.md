# org.secc.CheckinMonitor

> Staff-facing check-in admin blocks: room-ratio management, a live attendance monitor, mobile check-in printing, a cache inspector, and an advanced "Super" check-in/search tool.

## Overview

CheckinMonitor is Southeast's collection of staff/volunteer UI blocks that sit on top of the
custom check-in stack in [org.secc.FamilyCheckin](../org.secc.FamilyCheckin/README.md). The blocks
let staff manage room capacity and ratios, watch check-ins as they happen across all campuses,
print mobile (parent-pickup) labels, inspect the custom check-in caches, and drive a manual
"Super Checkin" desk that can add people, phone numbers, and PINs. There is no `.csproj` — these
are RockWeb-compiled `.ascx` blocks that link against the FamilyCheckin assemblies at runtime.

## Project Info

- **Project file:** none — no `.csproj`; RockWeb-compiled `.ascx`/`.ascx.cs` blocks.
- **Root namespace:** `RockWeb.Plugins.org_secc.CheckinMonitor`
- **Target framework:** compiled in-place by RockWeb (.NET Framework 4.7.2).
- **Deploys to:** `RockWeb/Plugins/org_secc/CheckinMonitor/` (the `.ascx` markup + code-behind).
- **Cross-plugin dependency:** [org.secc.FamilyCheckin](../org.secc.FamilyCheckin/README.md) (cache, model, utilities).

## Project Layout

```
/org_secc/CheckinMonitor/
  CheckinMonitor.ascx        Room/ratio management (soft & firm thresholds, location labels)
  LiveMonitor.ascx           Live, auto-refreshing feed of recent check-ins system-wide
  MobileCheckinViewer.ascx   Lists active mobile check-in records for label printing
  CacheInspect.ascx          Grid view + flush/verify of the FamilyCheckin custom caches
  SuperCheckin.ascx          Advanced manual check-in desk (add person/phone/PIN, reprint)
  SuperSearch.ascx           Phone/name keypad search that seeds the check-in workflow
```

## Components

### Blocks

Category in Rock: **SECC > Check-in**.

| Block | Base | Purpose |
|-------|------|---------|
| Check-In Monitor | `CheckInBlock` | Manage rooms: view/edit soft & firm room thresholds, room ratios, print a location label. |
| Live Monitor | `RockBlock` | Auto-refreshing feed of the latest ~100 attendances today, a per-minute count, and a server CPU readout. |
| Mobile Check-in Viewer | `CheckInBlock` | Lists active `MobileCheckinRecord`s for the current kiosk type and prints (aggregated) pickup labels. |
| Cache Inspect | `RockBlock` | Browse/verify/flush the FamilyCheckin caches (occurrences, attendances, mobile records, kiosk types). |
| Super Checkin | `CheckInBlock` | Manual check-in desk: search, add/edit a person (including grade), edit attributes, add phone, add a PIN login, complete/reprint tags. |
| Super Search | `CheckInBlock` | On-screen keypad searching by phone number or name; hands the result to the check-in workflow. |

### Block Settings

Keys in **bold** are the attribute keys used in code.

#### Check-In Monitor
| Setting | Type | Notes |
|---------|------|-------|
| **Room Ratio Attribute Key** | text (default `RoomRatio`) | Location attribute key the ratio editor reads/writes. |
| **Approved People** | data view (Person) | Members who may check in. |
| **Location Label** | binary file (`CHECKIN_LABEL`) | Label printed for a location. |
| **EnableHardLimit** | bool (default true) | Allow editing the firm/hard room limit on a location. |

#### Live Monitor
| Setting | Type | Notes |
|---------|------|-------|
| **Volunteer Group Attribute** | attribute (Group) | Group attribute used by the monitor view. |

#### Mobile Check-in Viewer
| Setting | Type | Notes |
|---------|------|-------|
| **Aggregated Label** | binary file (`CHECKIN_LABEL`) | Parent-pickup ("aggregated") label to print. |

#### Super Checkin
| Setting | Type | Notes |
|---------|------|-------|
| **Connection Status** | defined value (Person Connection Status) | Connection status for newly created people. |
| **Checkin Category** | attribute category (Person) | Person attribute category surfaced for editing. |
| **Checkin Activity** | text (required) | Workflow activity name that completes check-in. |
| **Reprint Activity** / **Child Reprint Activity** | text (required) | Activity names for reprinting the family / child tag. |
| **SMS Phone** / **Other Phone** | defined value (Phone Type) | Phone type to save when SMS is / isn't enabled. |
| **Allow Reprint** | bool (default false) | Allow reprinting parent tags from this page. |
| **Approved People** | data view (Person) | People allowed to check in. |
| **AllowNonApproved** | bool (default false) | Allow adults not in the approved list to check in. |
| **Security Role Dataview** | data view (Person) | People in a security role; blocks adding a PIN for them (see Observations). |
| **Data Error URL** | text | Iframe URL for data-error workflow (e.g. `WorkflowEntry/12?PersonId={0}`). |
| **QRCodeCheckReprint** / **QRCodeCheckCheckin** | bool (default false) | Require a QR-code scan to complete a reprint / a super check-in. |
| **ReprintSecurityGroup** | security role (default Staff Members) | Group allowed to reprint tags. |
| **SuperCheckInGroup** | security role (required) | Group allowed to perform super check-in. |

#### Super Search
| Setting | Type | Notes |
|---------|------|-------|
| **Add Family Option** | bool (default true) | Show an "Add New Family" button after search. |

## Dependencies & Integrations

- **Rock:** `CheckInBlock` / `RockBlock`, the Rock check-in workflow engine (`CurrentCheckInState`,
  `ProcessActivity`), `RockContext` and Rock services (`AttendanceService`, `PersonService`,
  `UserLoginService`, `DataViewService`, `GroupMemberService`), `AuthenticationContainer`
  (`PINAuthentication`), binary-file labels, Lava merge fields, the Zebra print client
  (`~/Scripts/CheckinClient/ZebraPrint.js`).
- **Cross-plugin:** [org.secc.FamilyCheckin](../org.secc.FamilyCheckin/README.md) — `Cache`
  (`OccurrenceCache`, `AttendanceCache`, `MobileCheckinRecordCache`, `CheckinKioskTypeCache`),
  `Model` (`MobileCheckinRecord`/`MobileCheckinStatus`), and `Utilities`.
- **Third-party:** Newtonsoft.Json (Mobile Check-in Viewer); `System.Diagnostics.PerformanceCounter`
  (Live Monitor CPU readout).

## Observations

*Noticed while documenting — not a full audit.*

- **Security (review):** In `SuperCheckin.mdPIN_SaveClick`, when the selected person is found in the
  **Security Role Dataview** it shows a warning and hides the modal but does **not** `return` — code
  execution falls through and still creates the `PINAuthentication` `UserLogin`. The guard intended
  to block adding a PIN to security-role members appears ineffective; worth confirming and adding an
  early `return`. (PINs are also only validated as numeric and length `> 7`.)
- **Security (low):** This whole plugin lets staff create logins (PINs), edit person attributes/phone
  numbers, and reprint tags. Confirm Rock **page/block security** for these blocks (and the configured
  `SuperCheckInGroup` / `ReprintSecurityGroup`) is locked to trusted check-in staff.
- **Improvement:** `LiveMonitor.OnLoad` calls `Thread.Sleep(200)` on the request thread to sample a
  `PerformanceCounter` ("% Processor Time") on every load, and re-queries the last 100 attendances
  each refresh. The CPU sample blocks the page and the counter is constructed (not disposed) per load.
- **Improvement:** `LiveMonitor` special-cases a campus short code (`"920"` -> `"c920"`) inline for a
  CSS icon class — a hardcoded campus assumption worth pulling into config.

## Making Changes

- Room thresholds / ratios and location-label printing live in `CheckinMonitor.ascx.cs`; the
  firm-vs-soft logic keys off `FirmRoomThreshold` / `SoftRoomThreshold` and the `RoomRatio` location
  attribute.
- Manual check-in behavior (add person/phone/PIN, QR validation, reprint) is all in
  `SuperCheckin.ascx.cs`; the workflow activity names it drives come from the **Checkin Activity** /
  **Reprint Activity** block settings, so wiring it to a different check-in template is a config change.
- **Grade on Add/Edit Person:** the Grade picker is the only grade input; the graduation year is
  calculated on the server from it (`Person.GraduationYearFromGradeOffset`). Both pickers get a
  **"Newborn to Pre-K"** option (value `prek`, added in `OnInit`) that saves no grade, so staff don't
  pick Kindergarten for Early Childhood kids. The school year in the Grade label and the saved graduation
  year come from Rock's `PersonService.GetCurrentGraduationDate()` (the `GradeTransitionDate` global
  attribute), which moves to the new school year the day after the transition date. The Super Checkin
  workflow filters with SECC's **Filter Groups By Grade (Custom)**
  ([org.secc.FamilyCheckin](../org.secc.FamilyCheckin/README.md)), which also moves the day after, so they
  agree on the transition day. That filter reads an area's own `GradeTransitionDate` first; if an area
  sets one that differs from the global, Super Check-in's label and saved year won't match that area.
- **Edit Person grade:** shows "Newborn to Pre-K" for a child whose graduation year is later than every
  school grade's, or who has no grade and was under 5 when the current school year started.
  `GradeTransitionDate` doubles as the Kindergarten age cutoff, so the children's team can move it
  without a code change (moving it also moves grade promotion). Edit only writes the graduation year
  when the Grade dropdown is changed; choosing "Newborn to Pre-K" there clears an existing grade. The
  person is validated before anything is saved, because saving a check-in attribute value also saves the person.
- **Refreshing check-in options:** after Add Person, or an Edit that changes a birthdate or grade, the
  workflow activity runs again (`RefreshFamilyOptions`) so age/grade filtering reflects the change.
  The family's selected rooms are kept, except the changed person's rooms that the filters now exclude.
  If the workflow errors, the block goes back to the previous page (search), like when a family is first
  opened, because Rock stops at the failed action and the filters after it never ran. The person's
  changes are already saved. Because "Newborn to Pre-K" saves
  no grade, a Pre-K child is kept out of grade-only rooms (such as Kindergarten) only when the
  check-in type's **Grade Required** is Yes or the room also has an Age/Birthday range. SECC's campus
  "Kids" check-in types have Grade Required = Yes, and the Kindergarten rooms have an Age Range of 4–8.
- The cache types browsed/flushed by `CacheInspect` are defined in
  [org.secc.FamilyCheckin](../org.secc.FamilyCheckin/README.md), not here — add cache fields there.
- Search input is seeded into the standard check-in workflow in `SuperSearch.ascx.cs`
  (`ProcessActivity`), so the matching/family logic lives in the configured workflow, not this block.

Last updated: 2026-10-20
