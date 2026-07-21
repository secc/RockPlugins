# org.secc.Migrations

> A library of Rock plugin migrations that install SECC's pages, workflows, defined types, attributes, stored procedures, and DB triggers at startup.

## Overview

This assembly contains nothing but Rock plugin migrations — C# classes implementing `Rock.Plugin.Migration`,
each tagged with a `[MigrationNumber(n, "rockVersion")]` attribute. Rock discovers and runs them in
number order the first time the plugin assembly loads, then records each as applied so it never re-runs.
The migrations stand up the foundational data for several SECC ministries (check-in, finance, groups,
pastoral care, MOC, volunteer application, etc.) by calling `RockMigrationHelper` and raw `Sql(...)`.

## Project Info

- **Project file:** `org.secc.Migrations.csproj`
- **Root namespace:** `org.secc.Migrations`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly only; post-build `xcopy` of `org.secc.Migrations.*`)
- **References:** `Rock.csproj` (project ref) and EntityFramework 6.1.3

## Project Layout

```
/                    Root-level migrations (Finance stored procs, Events campaign attribute)
/Connection/         Connection-opportunity attribute migration (Safety & Security)
/GroupApp/           Defined-type migrations backing the Group App
/Metric/             SQL trigger that logs worship-attendance MetricValue changes to history
/Pages/              Migrations that create Rock pages/blocks for SECC features
/WorkflowsTypes/     Migrations that install SECC workflow types
/Properties/         AssemblyInfo
```

## Components

All components are migrations. Each runs once, in `MigrationNumber` order, and most provide a `Down()`
that removes what `Up()` created (the stored-proc/trigger migrations re-create on `Up()` and largely
no-op on `Down()`).

### Migrations

| # | Class | Folder | Installs |
|---|-------|--------|----------|
| 2 | `MOC_WorkflowData` | WorkflowsTypes | MOC (Ministry Opportunity / Connection) workflow type |
| 3 | `FamilyCheckin_PageData` | Pages | Family Check-in pages/blocks |
| 4 | `GroupFinderMap_PageData` | Pages | Group Finder Map page/blocks *(not in .csproj — see Observations)* |
| 5 | `GroupManager_PageData` | Pages | Group Manager pages/blocks |
| 6 | `MOC_PageData` | Pages | MOC pages/blocks |
| 7 | `Purchasing_PageData` | Pages | Purchasing pages/blocks |
| 9 | `SuperyCheckin_PageData` | Pages | Super Check-in pages/blocks |
| 10 | `SuperCheckin_WorkflowData` | WorkflowsTypes | Super Check-in workflow type |
| 11 | `FamilyCheckin_WorkflowData` | WorkflowsTypes | Family Check-in workflow type |
| 12 | `SportsAndFitness_WorkflowData` | WorkflowsTypes | Sports & Fitness workflow type |
| 13 | `Pastoral_WorkflowData` | WorkflowsTypes | Pastoral Care workflow types |
| 14 | `GroupTreasurerReport_WorkflowData` | WorkflowsTypes | Group Treasurer Report workflow type |
| 15 | `VolunteerApplication_WorkflowData` | WorkflowsTypes | Volunteer Application workflow type (largest migration, ~1.4k lines) |
| 16 | `SafetyAndSecurity_ConnectionVerification` | Connection | Connection-opportunity attributes `SecurityToConnect`, `ConnectableStatuses` |
| 17 | `GroupAppGroupTypeMigration` | GroupApp | "Group App Group Types" defined type |
| 18 | `GroupAppTableBasedGroupsMigration` | GroupApp | "Group App Table-Based Group Types" defined type |
| 19 | `WorshipAttendanceTrigger` | Metric | SQL triggers `_org_secc_trgMetricValueWorshipAttendanceInsertLogToHistory` and `_org_secc_trgMetricValueWorshipAttendanceUpdateLogToHistory` on `MetricValue` |
| 19 | `GroupAppStudentGroupsMigration` | GroupApp | "Group App Student Group Types" defined type *(not in .csproj; duplicate # — see Observations)* |
| 30 | `Finance_MoveContributionSummaryStoredProcedure` | / | Recreates proc `_org_secc_Commitment_GetTotalsByPersonId` |
| 31 | `Finance_MoveContributionSummaryStoredProcedure_002` | / | Updates the same contribution-summary proc |
| 32 | `Finance_MoveContributionSummaryStoredProcedure_003` | / | Updates the same contribution-summary proc |
| 33 | `Events_CommunityGivesBackCampaignAttribute` | / | Adds `Year` defined-type attribute; backfills existing values to "2024" |

## Dependencies & Integrations

- **Rock:** `Rock.Plugin.Migration`, `RockMigrationHelper`, `DefinedTypeCache`, and the Rock plugin-migration
  runner (numbered, version-gated, run-once). Raw `Sql(...)` for stored procedures and triggers.
- **Third-party:** EntityFramework 6.1.3 (migration base infrastructure).
- **Cross-plugin:** Installs the data backing many SECC plugins — e.g. check-in
  ([org.secc.FamilyCheckin](../org.secc.FamilyCheckin/README.md)), pastoral care
  ([org.secc.PastoralCare](../org.secc.PastoralCare/README.md)), finance
  ([org.secc.Finance](../org.secc.Finance/README.md)), and connections
  ([org.secc.Connection](../org.secc.Connection/README.md)).

## Observations

*Noticed while documenting — not a full audit.*

- **Improvement:** Two `.cs` files are present in the folder but **not included in `org.secc.Migrations.csproj`**
  (`Pages/GroupFinderMap_PageData.cs`, migration #4; `GroupApp/GroupAppStudentGroups.cs`, migration #19), so
  they are not compiled or run. Worth confirming whether they were intentionally dropped or accidentally
  excluded.
- **Improvement:** Migration number **19 is used twice** — by `WorshipAttendanceTrigger` (compiled) and
  `GroupAppStudentGroupsMigration` (not compiled). Even though only one is built today, re-adding the second
  to the project without renumbering would collide. Migration numbers should be unique within an assembly.
- **Improvement:** Migration #33 (`Events_CommunityGivesBackCampaignAttribute`) **hard-codes the year "2024"**
  when backfilling defined values. That's expected for a one-shot data migration, but the value won't update
  for later campaigns — new years need their own migration or a config-driven approach.
- **Improvement:** Most workflow/stored-proc/trigger migrations have an empty or no-op `Down()`, so rolling
  back is effectively one-way. Acceptable for run-once installs, but note it before relying on `Down()`.

## Making Changes

- To add new data, create a **new** migration class with the next unused `[MigrationNumber]` and add it to
  `org.secc.Migrations.csproj`; never edit a migration that has already run in production (Rock won't re-run
  it). Verify the number isn't already taken (see the duplicate-19 note above).
- Page/block installs go under `/Pages/`, workflow types under `/WorkflowsTypes/`, defined types under the
  feature folder (e.g. `/GroupApp/`); follow the existing `RockMigrationHelper` patterns.
- Stored procedures and triggers are created via raw `Sql(...)` (see `/Metric/` and the root `Finance_*`
  files) — guard with `IF EXISTS ... DROP` so the migration is idempotent on re-create.
</content>
</invoke>
