# org.secc.RecurringCommunications

> Lets staff define email/SMS/push communications that re-send on a Rock schedule to a Data View audience, sent by a scheduled job.

## Overview

RecurringCommunications adds a custom entity (`RecurringCommunication`) plus two admin blocks for
authoring communications that fire on a repeating Rock **Schedule** against a **Data View** of
people. A scheduled job walks the saved definitions, and for any whose schedule has fired since the
last run it builds a real Rock `Communication` and enqueues it for sending. Optionally a
**Data Transform** can map the Data View's people to a different recipient set (e.g. parents of the
matched people) while carrying the original person through as an `AppliesTo` merge field.

## Project Info

- **Project file:** `org.secc.RecurringCommunications.csproj`
- **Root namespace:** `org.secc.RecurringCommunications`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly + `.pdb`) and `RockWeb/Plugins/org_secc/` (block `.ascx` markup), via the PostBuildEvent `xcopy`

## Project Layout

```
/Model/         RecurringCommunication entity + RecurringCommunicationService
/Data/          RecurringCommunicationsContext (EF DbContext) + RecurringCommunicationsService<T> (generic base service)
/Jobs/          SendRecurringCommunications — Quartz IJob that builds & enqueues communications
/Migrations/    Rock plugin migrations (table create, transformation column)
/org_secc/RecurringCommunications/   List + Detail admin blocks (.ascx + .ascx.cs)
```

## Components

### Models

| Model | Table | Notes |
|-------|-------|-------|
| `RecurringCommunication` | `_org_secc_RecurringCommunications_RecurringCommunication` | `ISecured`/`IRockEntity` Rock entity. Holds the audience (`DataViewId`), repeat `ScheduleId`, `CommunicationType`, the email/SMS/push payload fields, optional SMS `PhoneNumberValueId`, optional `TransformationEntityTypeId`, and `LastRunDateTime` (the de-dupe watermark). |

### Jobs

Quartz `IJob`, registered as a Rock Job in the admin UI.

| Job (class) | Purpose | Key settings |
|-------------|---------|--------------|
| `SendRecurringCommunications` | For each saved `RecurringCommunication` whose `Schedule` has a scheduled start time in the last day not yet covered by `LastRunDateTime`, builds an `Approved` `Communication` for the Data View recipients and enqueues it via `ProcessSendCommunication`. | **`SQLCommandTimeout`** (IntegerField, default 30s) |

### Blocks

Category in Rock: **SECC > Communication**.

| Block | Purpose | Key settings |
|-------|---------|--------------|
| Recurring Communications List | Grid of all recurring communications; "Add" navigates to the detail page. | **`DetailsPage`** (LinkedPage) |
| Recurring Communications Detail | Create/edit a recurring communication (name, Data View, schedule, type, payload, optional transform). | **`EnabledCommunicationTypes`** (CustomCheckboxListField: `0`=Recipient Preference, `1`=Email, `2`=SMS, `3`=Push Notification; default `0,1,2`) |

## Dependencies & Integrations

- **Rock:** EF `DbContext` (`Rock.Data.DbContext` with a `NullDatabaseInitializer`), Rock model
  framework (`Model<T>`, `ISecured`), Schedule (`GetScheduledStartTimes`), Data Views
  (`DataView.GetQuery`), Data Transforms (`DataTransformContainer` / `FilterExpressionExtractor`),
  the Communication engine (`CommunicationService`, `ProcessSendCommunication.Message`), Quartz job
  framework, the Rock block/UI framework, and plugin migrations (`Rock.Plugin`).
- **Third-party:** Quartz, EntityFramework 6, Common.Logging (transitive); DotLiquid / Rock.Lava.Shared (project references).
- No external HTTP APIs.

## Migrations

Ships Rock plugin migrations under `/Migrations/` (both `[MigrationNumber]`, not EF code-first):

- `201910311455314_Init` — `MigrationNumber 1`: creates `_org_secc_RecurringCommunications_RecurringCommunication` with FKs to DataView, Schedule, DefinedValue, PersonAlias.
- `20200107104400_AddTransformationColumn` — `MigrationNumber 2`: adds the nullable `TransformationEntityTypeId` column, FK to `EntityType`, and its index.

## Observations

*Noticed while documenting — not a full audit.*

- **Improvement:** `Init.Down()` drops `dbo.RecurringCommunication`, but the table the migration
  actually creates is `dbo._org_secc_RecurringCommunications_RecurringCommunication`. A rollback would
  fail to find the table (and leave the real one behind). Worth correcting the `DropTable` name.
- **Improvement:** In the transform branch of `EnqueRecurringCommunication`, `communication.Recipients`
  is reassigned to the shared `recipients` list inside the per-person loop, and a single
  `fieldValues`/`recipients` accumulation is reused across people — worth reviewing that the
  `AppliesTo` merge value lands on the right recipients and that duplicates aren't produced.
- **Improvement:** The job sets `LastRunDateTime` and calls `SaveChanges()` *before* attempting the
  send; if `EnqueRecurringCommunication` throws, the error is logged and the communication is added to
  the `errors` list, but the watermark has already advanced — so a failed run will be skipped next
  time rather than retried. Confirm that's the intended behavior.
- **Note:** There are two service classes, but they aren't duplicates: `Data/RecurringCommunicationsService<T>`
  is a generic `Rock.Data.Service<T>` base (with a no-op `CanDelete`), and
  `Model/RecurringCommunicationService` is the concrete subclass for `RecurringCommunication` that the job
  and blocks use. This is the standard Rock plugin base/derived service pattern.

## Making Changes

- To change the authoring UI or available communication types, edit
  `org_secc/RecurringCommunications/RecurringCommunicationsDetail.ascx[.cs]` (the
  `EnabledCommunicationTypes` block setting controls the type picker).
- To change how/when communications are built and sent, edit `Jobs/SendRecurringCommunications.cs`;
  the schedule-vs-`LastRunDateTime` watermark logic in `Execute` decides what fires.
- New columns/data belong in a new numbered migration under `/Migrations/` (don't hand-edit
  migrations that have already run); keep the `RecurringCommunication` model and migration in sync.
- Related: this plugin builds standard Rock `Communication` records, so it rides on Rock's core
  communication mediums and transports rather than a sibling plugin.
