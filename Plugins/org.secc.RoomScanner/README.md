# org.secc.RoomScanner

> A REST API (plus two admin blocks) that lets a kids' room-scanner app move children and volunteers in and out of check-in rooms with PIN-authorized overrides and two-volunteer safety rules.

## Overview

RoomScanner is the server side of Southeast's room-door scanning app for SE!Kids check-in. A
scanner at a classroom door reads a check-in code, and the app calls these `api/org.secc/roomscanner/*`
endpoints to look up the room roster, mark a child as entering or exiting, move a child to a
different room (with a staff PIN override), and enforce ratio rules (e.g. a child can't enter until
two volunteers are checked in, and a volunteer can't check out if it would drop a room with children
below two volunteers). It also ships two Rock blocks — a small config block for two global-attribute
settings, and a "Room Report" timeline visualization of where a child was during the day.

## Project Info

- **Project file:** `org.secc.RoomScanner.csproj`
- **Root namespace:** `org.secc.RoomScanner`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly + `.pdb`) and `RockWeb/Plugins/org_secc/` (block `.ascx` markup), via the PostBuildEvent `xcopy`
- **Cross-plugin dependency:** [org.secc.FamilyCheckin](../org.secc.FamilyCheckin/README.md)

## Project Layout

```
/Rest/Controllers/   RoomScannerController.Partial.cs — all api/org.secc/roomscanner/* endpoints
/Models/             DTOs: Request, MultiRequest, Response, Template, Attendee, AttendanceEntry, AttendanceCodes
/Utilities/          ValidationHelper (PIN/room/threshold checks), DataHelper (attendance mutation + person history)
/org_secc/RoomScanner/  Two blocks (.ascx + .ascx.cs): Configuration, RoomReport
```

## Components

### REST Endpoints

All routes are `[Authenticate, Secured]`. Errors are logged via `ExceptionLogService` and returned
as an empty/failure result rather than thrown.

| Route | Method | Purpose |
|-------|--------|---------|
| `api/org.secc/roomscanner/test` | GET | Connectivity smoke test; returns `"TEST GOOD!"`. |
| `api/org.secc/roomscanner/pin/{pinCode}` | GET | Validates a PIN (a `UserLogin` username) is in the configured override group. |
| `api/org.secc/roomscanner/templates` | GET | Check-in templates (group types) whose name contains "kids". |
| `api/org.secc/roomscanner/areas/{templateId}` | GET | Child group types (areas) under a template. |
| `api/org.secc/roomscanner/groups/{groupTypeId}` | GET | Active groups of a group type. |
| `api/org.secc/roomscanner/locations/{groupId}` | GET | Scheduled locations for a group. |
| `api/org.secc/roomscanner/locationbyguid/{guid}` | GET | Resolve a location by Guid (name + campus). |
| `api/org.secc/roomscanner/attendees/{locationId}` | GET | Today's attendees at a location (name, did-attend, checked-out). |
| `api/org.secc/roomscanner/getroster/{locationId}` | GET | Full room roster for today (handles subrooms via parent location). |
| `api/org.secc/roomscanner/GetAttendanceCode/{attendanceGuid}` | GET | The person's check-in code(s) for today. |
| `api/org.secc/roomscanner/entry` | POST | Mark an attendee as entering a room; supports `Override` to move from another location, enforces the 2-volunteer rule and room thresholds. |
| `api/org.secc/roomscanner/exit` | POST | Check an attendee out of a room; blocks volunteer checkout that would leave children under-staffed. |
| `api/org.secc/roomscanner/insert` | POST | PIN-authorized direct insert of a person (volunteer) into a room's volunteer occurrence. |
| `api/org.secc/roomscanner/movetowithparent` | POST | Flag people (pipe-delimited ids) as "with parent" and log history. |
| `api/org.secc/roomscanner/returntoroom` | POST | Clear the "with parent" flag and log history. |

POST bodies are `Request` (`AttendanceGuid`, `LocationId`, `Override`, `PIN`) or `MultiRequest`
(`PersonIds`, pipe-delimited). Responses are the `Response` DTO (`Success`, `Message`, `Overridable`,
`RequireConfirmation`, `PersonId`, `BirthdayText`).

### Blocks

Category in Rock: **SECC > Security**.

| Block | Purpose | Key settings |
|-------|---------|--------------|
| RoomScanner Configuration | Edits the `RoomScannerSettings` global attribute — `AllowedGroupId` (override-approval group) and `SubroomLocationType` (defined-value id). | Roomscanner Config Attribute Key (`TextField`, default `RoomScannerSettings`) |
| RoomScanner RoomReport | Timeline (Google Charts) of a child's room entries/exits for a date, built from person History records (`CategoryId` 4) related to the page's `LocationId`. | (none; reads `LocationId` page param and a per-block date user preference) |

### Models (DTOs)

Plain serialization types returned by / posted to the controller — not EF entities.

| Type | Role |
|------|------|
| `Request` / `MultiRequest` | POST bodies (single attendee vs. pipe-delimited person ids). |
| `Response` | Standard result envelope for all mutating endpoints. |
| `Template` | Lightweight `{ Id, Name, Description }` used for templates/areas/groups/locations dropdowns. |
| `Attendee` / `AttendanceEntry` | Roster rows (the latter adds `IsVolunteer` / `WithParent` flags). |
| `AttendanceCodes` | A person's name + today's check-in codes. |

## Dependencies & Integrations

- **Rock:** Rock REST (`ApiController`, `[Authenticate, Secured]`), `RockContext` and Rock services
  (`AttendanceService`, `AttendanceOccurrenceService`, `GroupService`, `GroupTypeService`,
  `LocationService`, `GroupLocationService`, `PersonService`, `PersonAliasService`,
  `UserLoginService`, `GroupMemberService`, `AttributeValueService`, `HistoryService`), the attribute
  framework, `GlobalAttributesCache`, cache types (`AttributeCache`, `CampusCache`, `CategoryCache`,
  `EntityTypeCache`), and the Rock block/UI framework.
- **Cross-plugin:** [org.secc.FamilyCheckin](../org.secc.FamilyCheckin/README.md) — uses its
  `Cache` (`AttendanceCache`, `OccurrenceCache`) for live in-room state and the volunteer/with-parent
  flags, and `Utilities.Constants` for the volunteer attribute Guid.
- **Third-party:** Google Charts (Timeline) loaded client-side in the RoomReport block; Newtonsoft.Json (transitive).

## Observations

*Noticed while documenting — not a full audit.*

- **Security (low):** The "PIN" for overrides/inserts is a person's `UserLogin` **username**
  (`UserLoginService.GetByUserName`), gated on membership in the configured `AllowedGroupId` group.
  It is not a password and is matched as a plain username, so its security rests entirely on that
  group's membership being tightly controlled. Worth confirming `AllowedGroupId` is locked to
  trusted staff and that usernames in scope aren't easily guessable.
- **Improvement:** `ValidationHelper` reads `RoomScannerSettings` into **static fields initialized
  once at type load** (`settings`, `allowedGroupId`, `subroomLocationTypeId`). Changing those global
  attributes via the Configuration block won't take effect until the app domain recycles. Same
  pattern in `DataHelper` static ids. Consider reading the settings per request.
- **Improvement:** `GetAttendeeAttendance` (`ValidationHelper`) has a likely bug in its
  username-fallback branch — the predicate references `attendeeAttendance.StartDateTime` (the
  local being assigned, still null) instead of the row `a.StartDateTime`, so that lambda would throw
  / never match as intended. Worth reviewing; in practice callers pass an `AttendanceGuid`.
- **Improvement:** `CategoryCache.Get( 4 )` and `CategoryId == 4` are **hard-coded integer ids** for
  the person-history category (in `DataHelper` and `RoomReport`). A Guid lookup would survive a
  reseed/import. There are also **OAuth copy-paste leftovers** in the **Configuration** block — its
  XML summary (`/// OAuth configuration`), `[Description( "Configuration settings for OAuth." )]`, and
  the auto-created attribute's description (`"Settings for the OAuth server plugin."`) all still
  reference OAuth. (`RoomReport` only carries the stray `/// OAuth configuration` XML comment; its
  `[Category]`/`[Description]` are correct.)
- **Improvement:** Nearly every endpoint and helper constructs a fresh `RockContext` (several methods
  new up a second context mid-call, e.g. `Entry`'s override branch and `DataHelper.CloneAttendance`).
  Acceptable per-request, but mutations split across contexts can make the save boundary hard to reason about.

## Making Changes

- To add or change an endpoint, edit `Rest/Controllers/RoomScannerController.Partial.cs`; keep the
  ratio/threshold and subroom logic in `Utilities/ValidationHelper.cs` and the attendance-mutation +
  person-history writes in `Utilities/DataHelper.cs`.
- The override group and subroom location type are data in the `RoomScannerSettings` global
  attribute, edited via the **RoomScanner Configuration** block (note the static-cache caveat above).
- Live in-room counts, volunteer detection, and the "with parent" flag come from
  [org.secc.FamilyCheckin](../org.secc.FamilyCheckin/README.md)'s `AttendanceCache` / `OccurrenceCache` —
  changes to room state should go through that cache so the scanner and check-in stay consistent.
