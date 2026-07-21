# org.secc.GroupManager

> A suite of Rock blocks for self-service group management — leader rosters, attendance, group finder, and a "Publish Group" model that lets ministries advertise and register groups.

## Overview

GroupManager is Southeast's catch-all groups plugin. The bulk of it is 21 Rock UI **blocks**
that group leaders and members use directly: roster views, mark-attendance screens, a map-based
group finder, group creation/recommit flows, and camp-specific leader tools. It also ships its
own EF entity — `PublishGroup` — that wraps a Rock `Group` with publishing metadata (schedule,
audience, registration/childcare options, contact info) so ministries can list groups publicly
and take registrations. A single REST endpoint backs the group finder's distance search.

## Project Info

- **Project file:** `org.secc.GroupManager.csproj`
- **Root namespace:** `org.secc.GroupManager`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly + `.pdb`) and `RockWeb/Plugins/org_secc/` (block `.ascx` markup), via the PostBuildEvent `xcopy`
- **Cross-plugin dependency:** [org.secc.Mapping](../org.secc.Mapping/README.md)

## Project Layout

```
/org_secc/GroupManager/   UI blocks (.ascx + .ascx.cs) — rosters, attendance, finder, registration
/Model/                   PublishGroup entity + service (the publishable-group data model)
/Data/                    GroupManagerContext (EF DbContext) + generic GroupManagerService<T>
/Rest/Controllers/        GroupManagerController — distance-based home-group lookup
/UI/                      GroupManagerBlock — shared base block (current group/member, filters)
/Exceptions/              NonExistantFilter exception
/Migrations/              Rock plugin migrations (PublishGroup table evolution + allergy proc)
```

## Components

### REST Endpoints

| Route | Method | Purpose |
|-------|--------|---------|
| `api/groupmanager/homegroups/{groupTypeId}/{zipcode}` | GET (authenticated) | Returns a map of `groupId -> travel distance` for active/public groups of a type, ranked by distance from a 5-digit ZIP (uses [org.secc.Mapping](../org.secc.Mapping/README.md)). |

### Base Block

`UI/GroupManagerBlock` (subclass of Rock `RockBlock`) is the shared base for several member-facing
blocks. It resolves the current group (from `GroupId` page param or the `CurrentGroupManagerGroup`
user preference), the current group member, and an optional member-attribute filter, then enforces
access. Block settings: **Home Page** (redirect target), **GroupType** (restrict to one type),
**Leaders Only** (leaders-only access).

### Blocks

Categories in Rock: **SECC > Groups**, **SECC > Camp**, and **Groups**.

| Block (DisplayName) | Base | Purpose |
|---------------------|------|---------|
| Group List | GroupManagerBlock | Members of a group in roster format. |
| Group Roster | GroupManagerBlock | Members of a group in roster format. |
| LWYA Roster | GroupManagerBlock | Roster variant (Live Where You Are). |
| Group Detail Lava | GroupManagerBlock | Group info rendered via Lava. |
| Group Manager Attendance | GroupManagerBlock | List members for an occurrence and mark attendance. |
| Group Member Tracker | GroupManagerBlock | Let a leader confirm a member attended after check-in. |
| Group Registration Modal | GroupManagerBlock | Register a person for a group (modal). |
| Group Creator | RockBlock | Create new groups. |
| Group Recommit | RockBlock | Sign up to lead a new group or copy an existing one. |
| Group Finder Map | RockBlockCustomSettings | Map-based group search by parameters/location. |
| Group Lava | RockBlock | Output groups via Lava. |
| Group Detail Button Inject | ContextEntityBlock | Add custom buttons to the core Group Detail block. |
| Small Group Content | RockBlockCustomSettings | Dynamic, paginated small-group content for a group. |
| Personal Group Information Panel | RockBlockCustomSettings | Edit a person's sports/fitness status. |
| Breakout Group Migration | RockBlock | Tool to migrate kids into groups. |
| Camp Group | RockBlock | Camp leaders view their group. |
| Camp Allergies | RockBlock | Campers in a group who have allergies (backed by a stored proc). |
| Publish Groups Lava | RockBlock | Output PublishGroups via Lava. |
| Publish Group List | RockBlock | List of PublishGroups. |
| Publish Group Registration | RockBlock | Register a person for a published group. |
| Publish Group Request | RockBlock | Display a publish-group request. |

### Models

| Model | Table | Notes |
|-------|-------|-------|
| `PublishGroup` | `_org_secc_GroupManager_PublishGroup` | Wraps a Rock `Group` with publishing metadata: title/description/image, start/end window, weekly or custom schedule, audience (`DefinedValue` many-to-many via `_org_secc_GroupManager_PublishGroupAudienceValue`), requestor/contact aliases, registration + childcare options/links, confirmation email fields, status, slug. Implements `ISecured`/`IRockEntity`; exposes Lava-visible helpers (`Title`, `ScheduleText`, `IsActive`, `IsFull`). |

Enums on the model: `PublishGroupStatus` (PendingApproval/Approved/Denied/Draft/PendingIT),
`RegistrationRequirement`, `ChildcareOptions`.

`PublishGroupService` (and the generic `GroupManagerService<T>`) provide standard Rock service
access; `GroupManagerContext` is a `Rock.Data.DbContext` with a null initializer (no EF-managed
schema — see Migrations).

## Dependencies & Integrations

- **Rock:** Rock block/UI framework (`RockBlock`, `RockBlockCustomSettings`, `ContextEntityBlock`),
  `RockContext` + Rock services (`GroupService`, `GroupTypeService`), attribute framework, Lava
  (DotLiquid), `ApiControllerBase` for REST, plugin migrations (`Rock.Plugin`).
- **Cross-plugin:** [org.secc.Mapping](../org.secc.Mapping/README.md) — `GroupUtilities.GetGroupsDestinations`
  powers the distance search in the REST endpoint and Group Finder Map.
- **Third-party:** EntityFramework 6, Newtonsoft.Json.

## Migrations

Ships Rock plugin migrations under `/Migrations/` that build up the `PublishGroup` table over time
(all `MigrationNumber` 1–11):

- `001_Init` — create the `_org_secc_GroupManager_PublishGroup` table.
- `002_SpouseRegistration` — add spouse-registration support.
- `003_MeetingDetails` — meeting schedule/time/location columns.
- `004_RegistrationOptions` — `RegistrationRequirement` (migrated from the old `RequiresRegistration` flag).
- `005_ChildcareOptions` / `006_ChildcareDescription` — childcare options + description columns.
- `007_RegistrationDescription` — registration description column.
- `008_AddKeys` — foreign key to `[Group]`.
- `009_RequestedChanges` — `IsHidden`/`Slug` columns + indexes.
- `010_DropRequiresRegistration` — drop the obsolete `RequiresRegistration` column.
- `011_AddCampAllergyReportProc` — create the `_org_secc_CampManager_GetGroupMemberAllergies`
  stored procedure (backs the Camp Allergies block).

## Observations

*Noticed while documenting — not a full audit.*

- **Security (medium):** The Camp Allergies block surfaces camper medical/allergy PII, and roster
  blocks expose member contact details. Confirm the Rock **page/block security** on these is
  scoped to the appropriate leader/staff roles. The `Leaders Only` block setting on
  `GroupManagerBlock` gates member-facing pages — worth verifying it's set on every page that
  should be leader-restricted, since it defaults off.
- **Improvement:** `GetFilteredMembers` / `GetCurrentGroupFilterValues` call `member.LoadAttributes()`
  inside a loop over `CurrentGroup.Members`, so attribute loading is per-member (N round-trips) on
  every render of the filtered roster blocks. Fine for small groups, costly for large ones.
- **Improvement:** Several blocks live under different Rock categories (`SECC > Groups`,
  `SECC > Camp`, plain `Groups`); consolidating naming would make them easier to find in the block
  picker. Three roster blocks (Group List / Group Roster / LWYA Roster) share the identical
  description "Presents members of group in roster format."

## Making Changes

- To change a roster/attendance/finder block, edit the matching `*.ascx` / `*.ascx.cs` in
  `org_secc/GroupManager/`; member-facing blocks inherit shared current-group/filter logic from
  `UI/GroupManagerBlock.cs`.
- Distance/location search behavior lives in [org.secc.Mapping](../org.secc.Mapping/README.md), not
  here — the REST endpoint and Group Finder Map just call into it.
- Schema changes to `PublishGroup` belong in a new numbered migration under `/Migrations/` (the
  `GroupManagerContext` uses a null initializer, so EF won't manage the schema) — don't hand-edit
  migrations that have already run.
- The Camp Allergies stored procedure is defined inline in `011_AddCampAllergyReportProc`; change
  it with a new migration rather than editing the proc in the database directly.
