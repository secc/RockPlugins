# org.secc.WorkflowLauncher

> Tools for launching and bulk-managing Rock workflows over groups, registrations, data views, and arbitrary SQL result sets.

## Overview

WorkflowLauncher is a small bag of Southeast's workflow-firing utilities: a scheduled **job** that
launches a workflow for every row of a SQL query or every entity in a Data View, and a set of
**blocks** for staff to launch workflows from the admin UI (pick a group / registration / data view,
or pass an entity by querystring) and to bulk-select and bulk-update existing workflows. It is used
to mass-start or mass-edit workflows without touching each one individually.

## Project Info

- **Project file:** `org.secc.WorkflowLauncher.csproj`
- **Root namespace:** `org.secc.WorkflowLauncher`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly) and `RockWeb/Plugins/org_secc/` (block `.ascx` markup), via the PostBuildEvent `xcopy`

## Project Layout

```
/                       WorkflowLauncher.cs — Quartz IJob (SQL/DataView row -> workflow launcher)
/org_secc/Workflow/     UI blocks (.ascx + .ascx.cs):
                          WorkflowLauncher      — launch from Group / RegistrationInstance / DataView
                          WorkflowEntityLaunch  — launch one workflow for an entity passed by querystring
                          WorkflowBulkSelect    — grid of a workflow type's instances -> EntitySet
                          WorkflowBulkUpdate    — bulk attribute/status/activity edit over an EntitySet
/Migrations/            Rock plugin migration (Workflow Launcher page + block)
```

## Components

### Jobs

A single Quartz `IJob` (`[DisallowConcurrentExecution]`). Note its namespace is `org.secc.Jobs`, not
`org.secc.WorkflowLauncher`. Configuration is per-job via Rock job attributes (read from the `JobDataMap`).

| Job (class) | Purpose | Key settings |
|-------------|---------|--------------|
| `WorkflowLauncher` | Launch the selected workflow once per SQL-query row (columns auto-mapped to workflow attributes by name) and/or once per entity in a Data View. | **SQLQuery**, **DataView**, **CommandTimeout**, **Workflow** |

### Blocks

Category in Rock: **SECC > Workflow** / **SECC > WorkFlow** (the bulk pair use the latter casing).

| Block | Purpose | Key settings |
|-------|---------|--------------|
| Workflow Launcher | Launch a chosen workflow type for a Group's members, a Registration Instance's registrations (one or all), or a Person Data View's people. | (none — `WorkflowTypeField` is picked in the UI) |
| Workflow Entity Launch | Launch one workflow for a single entity resolved from the `EntityId` querystring; group-member launches preset `Group`/`Person` attributes. | **EntityType**, **WorkflowType** |
| Workflow Bulk Select | Filterable grid of one workflow type's instances (the type comes from the `WorkflowTypeId` page parameter; a picker is shown if absent); selected (or all filtered) rows are pushed into a 1-day `EntitySet` and handed to the update page. | **UpdatePage** (linked page); reads `WorkflowTypeId` page parameter |
| Workflow Bulk Update | Over the `EntitySetId`'s workflows: set attribute values (Lava-resolved), reactivate/complete, set status, and activate a new activity, then re-process each. | (none — reads `EntitySetId` page parameter) |

The `WorkflowEntityLaunch` block resolves the entity service/`Get`/`LaunchWorkflow` reflectively from
the configured `EntityType`, so it works against any entity type that exposes a `LaunchWorkflow`
extension. The `WorkflowLauncher` block restricts its entity-type dropdown to Group, Registration
Instance, and Data View.

## Dependencies & Integrations

- **Rock:** Quartz job framework (`IJob`, `JobDataMap`), `RockContext` and Rock services
  (`WorkflowService`, `DataViewService`, `RegistrationService`, `GroupMemberService`, `EntitySetService`,
  `AttributeService`), `DbService.GetDataSet` (raw SQL), the workflow engine (`Workflow.Activate` /
  `LaunchWorkflow` extensions), attribute/cache framework, `RockBlock` UI, plugin migrations.
- **Third-party:** Newtonsoft.Json (bulk-select filter value (de)serialization), DotLiquid (Lava merge-field resolution in bulk update).
- **Cross-plugin:** none required at build time.

## Migrations

Ships one Rock plugin migration under `/Migrations/`:

- `20180630120000_InitialCreate` (`MigrationNumber 1`, `1.7.0`) — adds the **Workflow Launcher** page
  under Rock RMS, registers the `WorkflowLauncher.ascx` block type, and places the block on the page.

## Observations

*Noticed while documenting — not a full audit.*

- **Security (medium):** The `WorkflowLauncher` job runs an **operator-supplied SQL Query** verbatim
  via `DbService.GetDataSet` with no parameterization or restriction. That's by design (the job
  attribute is a SQL code editor), but it means anyone who can configure/edit this Rock Job can run
  arbitrary SQL under Rock's DB credentials. Confirm Jobs admin is locked to trusted staff.
- **Improvement:** Only the `WorkflowLauncher.ascx` block has a migration registering its block
  type/page. `WorkflowEntityLaunch`, `WorkflowBulkSelect`, and `WorkflowBulkUpdate` ship `.ascx`
  markup but have **no migration** — their block types/pages must be created by hand in Rock (or
  installed by another plugin) or they're effectively dormant. Worth a migration if they're meant to
  be deployed.
- **Improvement:** The bulk-select/bulk-update blocks live in namespace
  `RockWeb.Plugins.org_secc.WorkFlowUpdate` (note the capital `F` and the `Update` segment) while the
  other two blocks use `...org_secc.Workflow`. Inconsistent namespacing/category casing
  (`SECC > Workflow` vs `SECC > WorkFlow`) is easy to trip over when wiring pages.
- **Improvement:** In the `WorkflowLauncher` block, `BindData()` opens an outer `RockContext` but then
  news up a **second** `RockContext` inline for the `EntityTypeService` query — one context would do.
- **Improvement:** `WorkflowBulkUpdate` re-processes each workflow on a **fresh `RockContext` per
  workflow** inside the loop (`new WorkflowService( new RockContext() )`), separate from the context
  that saved the changes. Fine for small sets, wasteful and inconsistent for large ones.

## Making Changes

- To change row-driven launching (SQL/DataView), edit `WorkflowLauncher.cs`; its config (query, data
  view, workflow, timeout) is all Rock Job attributes — no code change to re-point it.
- To change the admin launch UI (which entity types are offered, how attributes are seeded), edit
  `org_secc/Workflow/WorkflowLauncher.ascx.cs` or `WorkflowEntityLaunch.ascx.cs`.
- The bulk select/update flow keys off an `EntitySet`: `WorkflowBulkSelect` builds it and links to the
  update page; `WorkflowBulkUpdate` reads `EntitySetId`. Edit the matching `.ascx.cs` and keep the
  linked-page wiring intact.
- New pages/blocks belong in a new numbered migration under `/Migrations/` — don't hand-edit the one
  that has already run.
- Related: [org.secc.Workflow](../org.secc.Workflow/README.md) (custom workflow actions) and the
  workflow-closing jobs in [org.secc.Jobs](../org.secc.Jobs/README.md).
</content>
</invoke>
