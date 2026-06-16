# org.secc.SystemsMonitor

> A pluggable system-monitoring framework: define "system tests" (HTTP, SQL, or Lava checks), run them on a schedule, record pass/fail history, and alert staff by email/SMS when an alarm condition is met.

## Overview

SystemsMonitor lets Southeast define **system tests** that probe Rock's health — is a website
reachable, does a SQL query return an expected value, does a Lava template match a pattern. Each
test type is a MEF-discovered `SystemTestComponent`, so new check types can be added without touching
the framework. A scheduled job runs due tests, writes a `SystemTestHistory` row per run, and sends
email/SMS notifications to a configured group when a test trips its alarm condition. A separate
`ServerHealth` block provides a lightweight HTTP probe endpoint for load-balancer health checks.

## Project Info

- **Project file:** `org.secc.SystemsMonitor.csproj`
- **Root namespace:** `org.secc.SystemsMonitor`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly + `.pdb`) and `RockWeb/Plugins/org_secc/` (block `.ascx` markup), via the PostBuildEvent `xcopy`
- **Cross-plugin dependency:** [org.secc.DevLib](../org.secc.DevLib/README.md) (project reference)

## Project Layout

```
/ (root)        SystemTestComponent (abstract base), SystemTestContainer (MEF), SystemTestResult
/Component/     The three built-in test types — ServerIsUp, SQLMatchExpression, LavaMatchExpression
/Model/         SystemTest + SystemTestHistory entities, services, AlarmCondition/AlarmNotification enums
/Data/          SystemsMonitorContext (EF DbContext bound to RockContext) + SystemsMonitorService<T> base service
/Controllers/   SystemTestController — REST endpoint to run a single test on demand
/Jobs/          RunSystemTests — Quartz job that runs due tests and sends alarm notifications
/Helpers/       TestResult (name + alarm-notification holder used by the job)
/Migrations/    Rock plugin migrations (create the two tables, add AlarmNotification column)
/org_secc/SystemsMonitor/   UI blocks (.ascx + .ascx.cs)
```

## Components

### Test Types (`SystemTestComponent`)

MEF-exported (`[Export(typeof(SystemTestComponent))]`), discovered via `SystemTestContainer`. Each
declares its config as Rock attributes (created automatically on container refresh). `RunTest`
returns a `SystemTestResult` (`Passed`, `Score`, `Message`).

| Component | Purpose | Config attributes | Supported alarms |
|-----------|---------|-------------------|------------------|
| `ServerIsUp` | Opens a URL and passes if it responds before timeout. | **Url**, **Timeout** (seconds, default 30) | Never, Fail |
| `SQLMatchExpression` | Runs a scalar SQL query; passes if the result matches a regex. | **SQL Query** (CodeEditor/Sql), **Matching Expression** (regex) | Never, Fail |
| `LavaMatchExpression` | Resolves a Lava template; passes if the output matches a regex. | **Lava** (CodeEditor/Lava), **Lava Commands**, **Matching Expression** (regex) | Never, Fail |

The base `SystemTestComponent` advertises `Never`, `Fail`, `ScoreAbove`, `ScoreBelow`; all three
built-ins narrow this to `Never`/`Fail` (none currently emit a non-zero `Score`).

### Jobs

| Job (class) | Purpose | Key settings |
|-------------|---------|--------------|
| `RunSystemTests` | Runs each test whose `RunIntervalMinutes` has elapsed since its last history row; sends notifications for any test meeting its alarm condition. `[DisallowConcurrentExecution]`. | **Notification Communication** (SystemCommunication), **Notification Group**, **From Number** (SMS defined value) |

> Note: the class lives in namespace `org.secc.Jobs`, not `org.secc.SystemsMonitor`.

### REST Endpoints

| Route | Method | Purpose |
|-------|--------|---------|
| `api/systemtest/runtest/{id}` | GET (`[Authenticate]`) | Runs test `{id}` immediately; returns `200 "Passed"` or `500` on failure. |

### Blocks

Category in Rock: **SECC > Systems Monitor**.

| Block | Purpose | Key settings |
|-------|---------|--------------|
| Monitor Tests | Grid of all defined tests; add / run / drill into a test. | Details Page (LinkedPage) |
| Monitor Test Detail | Create/edit a single test — pick the component type and set its attributes, interval, and alarm. | (none) |
| Server Health | HTTP health probe for this node; returns `200 Healthy` or `503` when `~/_maintenance.flag` exists. | (none) |

### Models

| Entity (table) | Purpose |
|----------------|---------|
| `SystemTest` (`_org_secc_SystemsMonitor_SystemTest`) | A monitor definition: name, the component `EntityType`, run interval, alarm condition/score, and `AlarmNotification` flags. `Run()` loads attributes, invokes the component, and logs a history row. |
| `SystemTestHistory` (`_org_secc_SystemsMonitor_SystemTestHistory`) | One row per test run: `Score`, `Passed`, `Message`. |

`AlarmCondition` = `Never` / `Fail` / `ScoreAbove` / `ScoreBelow`; `AlarmNotification` is a `[Flags]`
enum of `Email` / `SMS`.

## Dependencies & Integrations

- **Rock:** `RockContext`, the Rock entity/model + service framework (`IRockEntity`, `Model<T>`),
  the MEF component container (`Rock.Extension.Container`), attribute framework, REST
  (`ApiControllerBase`), the block/UI framework, `DbService` (SQL scalar), Lava, and Rock
  communications (`RockEmailMessage` / `RockSMSMessage`).
- **Third-party:** Quartz (`IJob`), EntityFramework 6, Newtonsoft.Json, DotLiquid (Lava).
- **Cross-plugin:** [org.secc.DevLib](../org.secc.DevLib/README.md) (project reference).
- **External:** the `ServerIsUp` test reaches arbitrary configured URLs over HTTP; `ServerHealth`
  is designed to back an external probe (e.g. Azure Application Gateway).

## Migrations

Ships Rock plugin migrations under `/Migrations/`:

- `001_Init` — creates `_org_secc_SystemsMonitor_SystemTest` and `_org_secc_SystemsMonitor_SystemTestHistory`
  (keys, FKs to `PersonAlias` / `EntityType`, indices) (`MigrationNumber 1`).
- `002_AlarmNotification` — adds the nullable `AlarmNotification` column to the `SystemTest` table (`MigrationNumber 2`).

## Observations

*Noticed while documenting — not a full audit.*

- **Security (medium):** The `SQLMatchExpression` and `LavaMatchExpression` test bodies are stored
  test attributes that execute arbitrary SQL / Lava (with configurable `Lava Commands`, including
  potentially `Sql`/`Execute`) under Rock's privileges. Whoever can create/edit a test via the
  *Monitor Test Detail* block effectively gets server-side SQL and Lava execution — confirm those
  pages/blocks are locked to administrators.
- **Improvement:** The Rock **pages and blocks are not installed by any migration** (only the two
  tables are). The `.ascx` files are deployed to `RockWeb/Plugins/org_secc/`, but the page/block
  entries must be created by hand in the admin UI. Worth confirming this is intentional rather than
  a missing `003_Pages` migration.
- **Improvement:** `RunSystemTests` keys "due" off whether a history row exists newer than
  `now - RunIntervalMinutes`. Because `SystemTest.Run()` always writes a history row, the next job
  tick after a run sees the fresh row and skips — so effective cadence is the max of the test
  interval and the job interval, and a test with no interval never auto-runs (intentional, but
  subtle).
- **Improvement:** `ServerIsUp.WebClientEx` sets `request.Timeout = Timeout * 10000` — for the
  default `Timeout` of 30 ("seconds" per the attribute description) this is 300,000 ms (5 min), not
  30 s. The unit math looks off; review whether the multiplier should be `1000`.
- **Improvement:** `SystemTest.Run()` opens a second `RockContext` (separate from the caller's) just
  to save the history row, and `MeetsAlarmCondition` is re-checked by the job rather than by `Run`,
  so a manual run via the REST endpoint or block records history but never alarms.

## Making Changes

- To add a new test type, create a class in `/Component/` deriving from `SystemTestComponent`,
  decorate it with `[Export(typeof(SystemTestComponent))]` + `[ExportMetadata("ComponentName", ...)]`
  and its Rock `*Field` config attributes, and implement `Name`, `Icon`, and `RunTest`; MEF picks it
  up and `SystemTestContainer.Refresh()` provisions its attributes. Follow `ServerIsUp` as a template.
- To change alarm delivery, edit `Jobs/RunSystemTests.cs` (`SendNotificationEmail` / `SendNotificationSms`);
  notification recipients come from the configured **Notification Group**.
- New columns/tables belong in a new numbered migration under `/Migrations/` — don't hand-edit
  migrations that have already run.
- Related: scheduled-job patterns and other custom jobs live in [org.secc.Jobs](../org.secc.Jobs/README.md).
</content>
</invoke>
