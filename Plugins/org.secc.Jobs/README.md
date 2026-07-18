# org.secc.Jobs

> A grab-bag of Southeast's custom Rock scheduled jobs (Quartz `IJob`s) for data maintenance, attendance, communications, and one-off migrations.

## Overview

This plugin is Southeast's collection of custom Rock **scheduled jobs**. Each class implements
Quartz's `IJob` and is wired up as a Rock Job (Admin > System Settings > Jobs), configured entirely
through Rock job attributes — no code change is needed to schedule or re-point one. The jobs span
ongoing maintenance (closing workflows, syncing attendance, cleaning phone numbers) and targeted
one-time data fixes (pickleball-credit migration, facilities-team relocation). It also ships two EF
plugin migrations that stand up supporting data and a tracking table.

## Project Info

- **Project file:** `org.secc.Jobs.csproj`
- **Root namespace:** `org.secc.Jobs`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly + `.pdb`, via the PostBuildEvent `xcopy`)
- **Cross-plugin dependency:** [org.secc.FamilyCheckin](../org.secc.FamilyCheckin/README.md)

## Project Layout

```
/ (root)        Most jobs — workflow, communication, attendance, person/device maintenance
/Event/         Event-area jobs: camp-placement import, event check-in relationships, GivesBack
/Rock13/        Jobs targeting Rock v13 behavior (phone-number type cleanup)
/Migrations/    Rock plugin migrations (group-type roles, camp-import tracking table)
```

## Components

### Jobs

Each is a Quartz `IJob`; most carry `[DisallowConcurrentExecution]`. Configuration is per-job via
Rock job attributes (read from the `JobDataMap`).

| Job (class) | Purpose | Key settings |
|-------------|---------|--------------|
| `CloseDeseasedPastoralWorkflows` | Close pastoral-care workflows for people now marked deceased. | Hospital Admission / Nursing Home Resident / Homebound Person workflow types |
| `CompleteWorkflows` | Bulk-close workflows of the selected type(s) past an expiration age. | Workflow Types, Close Status, Expiration Age, Expiration Calc Last Used |
| `DisableCommunicationsForInactivePeople` | Turn off email/SMS for inactive people; snapshot original prefs to a DefinedType. | Inactive Person Communication Overrides Defined Type, Lookback Days, Dry Run |
| `RestoreCommunicationsForReactivatedPeople` | Restore email/SMS prefs for re-activated people from the stored snapshots. | Inactive Person Communication Overrides Defined Type, Dry Run |
| `EasterChildrensAttendance` | Pull Easter SEKids vs. worship-service attendance analytics. | Easter Sunday Date, SE!Kids / Worship Schedule Categories, Command Timeout |
| `SetFirstAttendanceDate` | Backfill each person's first-attendance date (batched SQL). | Command Timeout, Take |
| `StoreAttendanceFromInteraction` | Convert WiFi-presence interactions into attendance records. | Interaction Channel, Component Campus Mapping, Operation, Group Type, Date Range |
| `GroupLeaderMedicationNotifications` | Notify small-group leaders of campers with medications. | Parent Group, Communication List, Medication Checkin Days, Communication Template |
| `RemoveDevicesFromPersons` | Remove personal devices for people in a DataView. | DataView (Person) |
| `FrontPorchDeviceRemoval` | Delete stale personal devices via the Front Porch external API. | Date Range |
| `PushPayDownloadCheckNumbers` | Backfill check numbers on PushPay check transactions via the PushPay API. | Transaction Source, Currency Type, Check Number Attribute, Date Range |
| `MigratePickleballCredits` | One-off: migrate pickleball session credits to a Group Fitness group. | Pickleball / Group Fitness groups + attribute keys, Note Type |
| `UpdateFacilitiesTeamLocation` | One-off: migrate facilities staff & requisitions to a central support location. | Central Support Location, Facilities Ministry Area, Location / Ministry Area attribute keys |
| `Event.AddEventCanCheckinRelationships` | Create "Event Can/Allow Checkin" known-relationships for a registration template. | Registration Template, Expiration Date Time |
| `RemoveEventCanCheckinRelationships` | Remove the "Event Can/Allow Checkin" relationships. | (none) |
| `Event.CopyCommunityGivesBackSchools` | Copy Community Gives Back school records into a new campaign. | Destination Campaign Name, Source Campaign, Copy Inactive Schools |
| `Event.CampPlacementImportBackgroundJob` | Background runner for a camp-placement CSV import (driven by a `RunId`). | RunId (passed via JobDataMap, not a job attribute) |
| `Rock13.PhoneNumberCleanup` | Clean up duplicate phone-number types (Rock v13). **Note: not in the `.csproj` `<Compile>` list, so it is not currently built.** | (none) |

### Migrations-driven data

| Migration | Installs |
|-----------|----------|
| `001_EventRelationshipRoles` | "Event Can Checkin" / "Event Allow Checkin By" roles on the Known Relationships group type. |
| `002_CampPlacementImportRun` | `_org_secc_CampPlacementImportRun` table tracking camp-import run status/progress. |

## Dependencies & Integrations

- **Rock:** Quartz job framework (`IJob`, `JobDataMap`), `RockContext` and Rock services, workflow
  engine, defined types/values, attribute framework, plugin migrations (`Rock.Plugin`).
- **Cross-plugin:** [org.secc.FamilyCheckin](../org.secc.FamilyCheckin/README.md) — `StoreAttendanceFromInteraction`
  uses `org.secc.FamilyCheckin.Utilities`.
- **Third-party APIs:** PushPay (`api.pushpay.com`, OAuth token obtained reflectively from
  `com.pushpay.RockRMS.dll`), Front Porch device API (host + token from Global Attributes).
- **Other:** Newtonsoft.Json (snapshot serialization), Ical.Net / NodaTime (transitive).

## Migrations

Ships Rock plugin migrations under `/Migrations/`:

- `001_EventRelationshipRoles` — adds the event check-in known-relationship roles (`MigrationNumber 1`).
- `002_CampPlacementImportRun` — creates the camp-placement import tracking table (`MigrationNumber 2`).

## Observations

*Noticed while documenting — not a full audit.*

- **Improvement:** Two files are present on disk but **not referenced in the `.csproj`** `<Compile>`
  list, so they never compile or run: `Migrations/AddOtherPhoneTypes.cs` and `Rock13/PhoneNumberCleanup.cs`.
  `AddOtherPhoneTypes` also declares `[MigrationNumber(1, "1.13.0")]`, which would collide with
  `001_EventRelationshipRoles` (also number 1) if it were ever included. Worth wiring up, removing, or
  renumbering as appropriate.
- **Improvement:** `PushPayDownloadCheckNumbers` loads `com.pushpay.RockRMS.dll` via reflection at
  runtime (`Assembly.LoadFrom` + `GetMethod("GetAccessToken")`). This is a brittle, hard-to-detect
  coupling — a rename in that plugin breaks this job only at runtime. The job's `errors` counter is
  also never incremented, so the result string always reports `0 Error(s)`.
- **Security (low):** `FrontPorchDeviceRemoval` and `PushPayDownloadCheckNumbers` reach external HTTP
  APIs using credentials from Global Attributes / merchant rows. Confirm those tokens are stored
  encrypted and that the Front Porch `Host`/token global attributes are not broadly readable.
- **Improvement:** `AssemblyInfo.cs` still carries the default scaffolded company/copyright
  (`"Microsoft"`, `"Copyright © Microsoft 2017"`) rather than Southeast Christian Church.

## Making Changes

- To add a job, drop a new `IJob` class in the matching folder (root, `Event/`, or `Rock13/`),
  decorate it with `[DisplayName]`/`[Description]` and its Rock `*Field` attributes, then register
  it as a Rock Job in the admin UI; follow an existing sibling as a template.
- Most jobs read their config from `context.JobDetail.JobDataMap` — keep the attribute `Key` and the
  `GetString(...)` lookup in sync.
- New supporting data (roles, tables, defined values) belongs in a new numbered migration under
  `/Migrations/` — don't hand-edit migrations that have already run.
- Related: the pastoral workflows closed by `CloseDeseasedPastoralWorkflows` are installed by
  [org.secc.PastoralCare](../org.secc.PastoralCare/README.md); attendance utilities live in
  [org.secc.FamilyCheckin](../org.secc.FamilyCheckin/README.md).
