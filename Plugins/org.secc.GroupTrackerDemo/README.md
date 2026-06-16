# org.secc.GroupTrackerDemo

> REST endpoints that list a leader's groups and let a client view and toggle today's attendance for a group's members.

## Overview

GroupTrackerDemo is a small, REST-only plugin: no UI blocks, no Lava, no migrations. It exposes
three JSON endpoints intended to back a "group tracker" client (the `Demo` name suggests a
prototype) — list the active groups a person leads, pull today's attendance occurrence and roster
for one group, and mark a member present/absent. All read queries use `AsNoTracking`.

## Project Info

- **Project file:** `org.secc.GroupTrackerDemo.csproj`
- **Root namespace:** `org.secc.GroupTrackerDemo`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly)

## Project Layout

```
/Controllers/   GroupTrackerController — the three REST endpoints
/Models/        Plain DTOs returned as JSON (GroupSummary, GroupWithOccurrence,
                GroupMemberWithAttendance)
/Properties/    AssemblyInfo
```

## Components

### REST Endpoints

`GroupTrackerController : ApiControllerBase` — routes registered via `[Route]` attributes.

| Route | Method | Purpose |
|-------|--------|---------|
| `api/GroupTracker/GroupSummaryByLeader/{personAliasId}` | GET | Active, non-archived groups where the person (resolved from `personAliasId`) is a leader. Optional `includedGroupTypeIds` query string (comma-separated) filters by group type. Returns `GroupSummary[]`. |
| `api/GroupTracker/GroupOccurrenceAttendance/{groupGuid}` | GET | For one group (by Guid), the non-leader active members joined to today's attendance occurrence, including mobile phone and per-member attendance state. Returns a single `GroupWithOccurrence`, or `404` if the group has no matching roster. |
| `api/GroupTracker/UpdateAttendedStatus/{attendanceId}/{isPresent}` | POST | Sets `DidAttend`/`PresentDateTime` on an existing `Attendance` record. `404` if the attendance Id is unknown; otherwise `202 Accepted`. |

### Models (JSON DTOs)

| Model | Shape |
|-------|-------|
| `GroupSummary` | `Id`, `Name`, `GroupTypeId`, `GroupTypeName`, `Guid`, `IsActive`. |
| `GroupWithOccurrence` | Group + today's occurrence (`OccurrenceId`, `Location*`, `Schedule*`, `OccurrenceDate`) + `List<GroupMemberWithAttendance>`. |
| `GroupMemberWithAttendance` | `GroupMemberId`, `PersonId`, `NickName`, `LastName`, `BirthDate`, `PhotoId`, `MobilePhone`, `AttendanceId`, `StartDateTime`, `EndDateTime`, `DidAttend`, `PresentDateTime`. |

## Dependencies & Integrations

- **Rock:** `RockContext`, `PersonAliasService`, `GroupMemberService`, `AttendanceService`,
  `PhoneNumberService`, `DefinedValueCache`, Rock REST (`ApiControllerBase`).
- **Third-party:** `Newtonsoft.Json`, ASP.NET Web API.
- No cross-plugin dependencies.

## Observations

*Noticed while documenting — not a full audit.*

- **Security (medium):** All three endpoints inherit only `ApiControllerBase`'s default auth and
  carry no per-method authorization or ownership check. `UpdateAttendedStatus` mutates any
  `Attendance` row by Id with no verification that the caller leads that group, and
  `GroupOccurrenceAttendance` returns member PII (name, birth date, mobile phone, photo) for any
  group Guid. Worth confirming the intended access model before this leaves "demo" status.
- **Improvement:** `GetGroupSummaryByLeader` does not guard against an unknown `personAliasId` —
  `PersonAliasService.GetPerson` can return null and the following `m.PersonId == person.Id` then
  throws a `NullReferenceException`. (Omitting `includedGroupTypeIds` is safe: it defaults to null
  and `IsNotNullOrWhiteSpace()` is a null-safe Rock string extension.) Review the null handling.
- **Improvement:** `GroupOccurrenceAttendance` keys "today's" occurrence off `RockDateTime.Today`
  and the attendance `GroupJoin` takes `FirstOrDefault()` per member, so behavior is ambiguous when
  a group has more than one occurrence on the same day.

## Making Changes

- All behavior lives in `Controllers/GroupTrackerController.cs`; add or change endpoints there and
  adjust the matching DTO under `/Models/`.
- The roster query in `GetGroupOccurrenceAttendance` is the non-trivial part — note the
  leader-exclusion filter (`!gm.GroupRole.IsLeader`) and the mobile-phone defined-value lookup
  (`PERSON_PHONE_TYPE_MOBILE`) if you extend the returned fields.
- This plugin is independent of other org.secc plugins; there are no migrations to add.
