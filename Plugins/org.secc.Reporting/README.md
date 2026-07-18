# org.secc.Reporting

> Southeast's custom reporting surface for Rock — a large set of admin/ministry report blocks, a "SQL Filter" data-view component, a camp-medication Excel export workflow action, and supporting EF models, migrations, and SQL objects.

## Overview

This plugin is Southeast's grab-bag of custom **reporting** building blocks. The bulk of it is RockBlock
report screens (`.ascx` under `org_secc/Reporting/`) grouped by ministry area — Children's, NextGen,
Home Groups, metrics entry, decision analytics, financial aid, worship-attendance entry. Alongside the
blocks it ships a reusable **SQL Filter** data-view component (filter any entity by a raw `SELECT [Id]`
query), an **Event Medication Export** workflow action that produces an Excel file, and the EF models /
migrations / stored procedures / views / triggers those screens read from. It is used by staff and
ministry leaders for operational reporting and data entry.

## Project Info

- **Project file:** `org.secc.Reporting.csproj`
- **Root namespace:** `org.secc.Reporting`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/Plugins/org_secc/` (the `.ascx` markup) and `RockWeb/bin/` (assembly, `.pdb`,
  and `Google.Apis.*`), via the PostBuildEvent `xcopy`.

## Project Layout

```
/org_secc/Reporting/      Report blocks (.ascx + .ascx.cs), grouped by ministry:
                            (root)        DecisionAnalytics, AttendanceQuickEntry, FinancialAidFamilyProfile,
                                          GroupAttendanceAnalytics, MetricsEntry, ServiceMetricsEntry,
                                          BirthdayListFetchingData, MedReportTest
                            Children/     BreakoutGroup* , ChildrenAudit, WithoutBreakoutGroupAttendance
                            NextGen/      Believe(+Leader), BibleAndBeach, FallRetreat, Mix, SignatureAudit,
                                          MedicationDispense, MedicationInformation
                            HomeGroups/   HomeGroupAttendance
                            Scripts/      isotope.pkgd.js / isotope.pkgd.min.js (layout for some grids)
/CampMedicationReport/    "Event Medication Export" workflow action + filter/item DTOs (EPPlus Excel)
/DataFilter/              SQLFilter — DataFilterComponent that filters by a raw SQL Id query
/Model/                   EF models + services: DataViewSQLFilterStore (skinny cache table), DecisionReportItem
/Data/                    ReportingContext (Rock DbContext) + ReportingService
/Transactions/            SqlFilterCleanupTransaction — prunes orphaned SQL-filter cache rows
/Migrations/              Rock plugin migrations (tables, view, stored proc, triggers)
Helpers.cs                Md5Hash helper (keys the SQL-filter cache)
```

## Components

### Blocks

All are `RockBlock`s; categories shown are the Rock block-type categories. Roughly grouped:

| Block (class) | Category | Purpose |
|---------------|----------|---------|
| `DecisionAnalytics` | SECC > Reporting | Reports of people who made a next-step decision (reads the `_org_secc_DecisionForm` table / analytics view). |
| `AttendanceQuickEntry` | SECC > Reporting | Lets campuses quickly enter worship-service attendance. Config: **Worship Attendance Parent Categories**. |
| `FinancialAidFamilyProfile` | SECC > Reporting | Displays financial-aid information for an entire family. |
| `GroupAttendanceAnalytics` | SECC > Reporting | Attendance graph configurable by group, date range, etc. Config: **DataViewCategories**. |
| `MetricsEntry` | SECC > Reporting | Add/edit metric values for metrics partitioned by campus & service time. Config: **Schedule Categories**. |
| `ServiceMetricsEntry` | SECC > Reporting | As above, service-metrics variant. Config: **Schedule Categories**. |
| `BirthdayListFetchingData` | SECC2 > Project | Simple sample/data-fetch block (`ICustomGridColumns`). |
| `MedReportTest` | SECC > Report | Test/scratch block (`[Description("Test Application")]`). |
| `Children/BreakoutGroupAttendance` | SECC > Reporting > Children | Filterable/sortable list of breakout groups. |
| `Children/BreakoutGroupAttendanceSummary` | SECC > Reporting > Children | Summary variant of the breakout-group list. |
| `Children/BreakoutGroupHandout` | SECC > Reporting > Children | Generates breakout-group handouts. |
| `Children/ChildrenAudit` | SECC > Reporting > Children | Audit information for children's ministry. |
| `Children/WithoutBreakoutGroupAttendance` | SECC > Reporting > Children | Lists children with no breakout group plus attendance. |
| `NextGen/Believe`, `BelieveLeader`, `BibleAndBeach`, `FallRetreat`, `Mix` | SECC > Reporting > NextGen | Trip-attendee management reports for NextGen trips/events. |
| `NextGen/SignatureAudit` | SECC > Reporting > NextGen | Audits signature documents. |
| `NextGen/MedicationDispense` | SECC > Reporting > NextGen | Notes when medications should be given out. |
| `NextGen/MedicationInformation` | SECC > Reporting > NextGen | Medication-information report. |
| `HomeGroups/HomeGroupAttendance` | SECC > Reporting > Home Groups | Home-group attendance reporting tool. |

### Data Filters

| Component | Applies to | Purpose |
|-----------|-----------|---------|
| `SQLFilter` (`DataFilterComponent`, section "Additional Filters") | any entity (`AppliesToEntityType` empty) | Filters a data view by a raw SQL query that returns entity `Id`s. Small result sets (≤1000) filter in-memory; larger sets are staged into the `_org_secc_Reporting_DataViewSQLFilterStore` cache table (keyed by an MD5 hash of the SQL) and sub-selected, to avoid huge `IN (...)` clauses. |

### Workflow Actions

| Action (class) | ComponentName | Purpose | Key attributes |
|----------------|---------------|---------|----------------|
| `CampMedicationReportExportAction` | `Event Medication Export` (action category `SECC > Events`) | Produces an Excel file (EPPlus) of event participants who have medications and stores it on a workflow attribute. | **RegistrationTemplate** (Registration Template), **ExcelFile** (File — output) |

### Models

| Model (table) | Notes |
|---------------|-------|
| `DataViewSQLFilterStore` (`_org_secc_Reporting_DataViewSQLFilterStore`) | Skinny cache table for the SQL Filter; composite key `(Hash, EntityId)`. `[HideFromReporting]`; the standard Rock audit/Id columns are `[NotMapped]`. |
| `DecisionReportItem` (`_org_secc_Reporting_DecisionForm`) | Denormalized decision-form row (person, decision type/date, campus, baptism, address, etc.) backing `DecisionAnalytics`. `[HideFromReporting]`; populated by migration SQL, not EF writes. |

`ReportingContext` is a Rock `DbContext` (uses the `RockContext` connection, `NullDatabaseInitializer`)
exposing `DataViewSQLFilterStores` and `DecisionReportItems`. Each model has a matching `Service<T>`.

## Dependencies & Integrations

- **Rock:** Reporting framework (`DataFilterComponent`, `FilterExpressionExtractor`), workflow engine
  (`ActionComponent`, `WorkflowAttribute`), `RockContext` / Rock services, `RockBlock` UI, EF plugin
  migrations (`Rock.Plugin`), `RockQueue` transactions, `DotLiquid` / `Rock.Lava.Shared`.
- **Third-party:** EPPlus (Excel export), Google.Apis / Google.Apis.Sheets.v4 (referenced in the project;
  Google Sheets client libraries are copied to `RockWeb/bin`), Newtonsoft.Json, EntityFramework 6.
- **Database objects (created by migrations):** stored procedure `_org_secc_CampManager_GetMedicationReport`,
  view `_org_secc_DecisionForm_Analytics`, table `_org_secc_Reporting_DecisionForm`, and two history
  triggers on `MetricValue` (`_org_secc_trgMetricValueWorshipAttendance{Insert,Update}LogToHistory`).
  Note the medication stored proc is named under the `CampManager` prefix — it is shared with / overlaps
  camp-management functionality.

## Migrations

Ships Rock plugin migrations under `/Migrations/` (all compiled per the `.csproj`):

- `001_Init` (`MigrationNumber 1`) — creates `_org_secc_Reporting_DataViewSQLFilterStore` (composite PK on
  `Hash`,`EntityId`; index on `Hash`).
- `WorshipAttendanceHistory` (`MigrationNumber 2`) — creates insert/update triggers on `MetricValue` that
  log worship-attendance metric changes to history.
- `DecisionReportView` (`MigrationNumber 3`) — creates the `_org_secc_DecisionForm_Analytics` view.
- `DecisonFormReportTable` (`MigrationNumber 4`) — creates the `_org_secc_Reporting_DecisionForm` table.
- `005_MedicationReportStoredProcedure` (`MigrationNumber 5`) — creates the
  `_org_secc_CampManager_GetMedicationReport` stored procedure.
- `006_MedicationReportFilterInactive` (`MigrationNumber 6`) — re-creates that stored proc to exclude
  medications whose `MedicationActive` matrix attribute is off.

`Configuration.cs` is the EF `DbMigrationsConfiguration` (automatic migrations disabled, empty `Seed`).

## Observations

*Noticed while documenting — not a full audit.*

- **Security (medium):** `SQLFilter` runs arbitrary operator-supplied SQL directly against the Rock
  database (`Context.Database.SqlQuery<int>(selection)`). The UI shows a prominent warning, and the filter
  is only usable by people who can edit data views, but it is effectively raw DB access via the reporting
  UI. The cache-maintenance paths in `GetExpression` and `SqlFilterCleanupTransaction` also build
  `DELETE`/`IN` statements via `string.Format` with the MD5 hash and entity Ids; the hash and Ids are not
  externally controlled, so injection risk there is low, but the design is worth confirming against
  Southeast's data-access policy.
- **Improvement:** `GetExpression` (a data-view filter evaluation) **writes to the database** — it bulk-inserts
  and deletes cache rows for result sets over 1000. Filter evaluation thus has side effects and isn't
  idempotent; large data views will repeatedly churn the cache table. The orphan cleanup runs as a queued
  transaction enqueued from `GetSelection` (i.e. when the filter is saved), which is a reasonable place,
  but the two paths together make the cache lifecycle non-obvious.
- **Improvement:** Several blocks look like leftovers/scratch: `MedReportTest` ("Test Application"),
  `BirthdayListFetchingData` ("A simple block to fetch some data", category `SECC2 > Project`), and
  `NextGen/MedicationInformation` whose description is just `"T"`. Worth confirming which are still in use.
- **Improvement:** Several NextGen trip blocks (`Believe`, `BelieveLeader`, `FallRetreat`) carry the
  copy-pasted description "A report for managing the trip attendeeds for NextGen's Bible & Beach Trip"
  inherited from `BibleAndBeach`, so the displayed description is wrong for them. (`Mix` was correctly
  updated to "...NextGen's MIX Trip"; note the typo "attendeeds" is present in all of them.)
- **Note:** The medication stored procedure is prefixed `_org_secc_CampManager_` rather than `_Reporting_`,
  indicating shared ownership with camp-management code; renaming/moving it touches both areas.

## Making Changes

- To add a report screen, drop a new `.ascx`/`.ascx.cs` `RockBlock` under the matching folder in
  `org_secc/Reporting/` (use a ministry-area `[Category]`), then register it as a block type in the admin UI;
  the PostBuildEvent copies the markup to `RockWeb/Plugins/org_secc/`.
- To change the SQL-filter staging behavior (size thresholds, caching), edit
  `DataFilter/SQLFilter.cs` and the cache lifecycle in `Transactions/SqlFilterCleanupTransaction.cs`.
- To change the medication export, edit the SQL in `Migrations/006_MedicationReportFilterInactive.cs`
  (the live stored proc) and/or the Excel generation in `CampMedicationReport/CampMedicationReport.cs`.
- New SQL objects (procs, views, triggers) or tables belong in a **new numbered migration** under
  `/Migrations/` — don't hand-edit migrations that have already run; note that the numeric `MigrationNumber`
  is what orders them (file names like `DecisionReportView.cs` carry number 3).
- Related medication/camp logic lives in the camp-management plugins; decision-form data is produced by the
  migration view/table here and consumed by `DecisionAnalytics`.
