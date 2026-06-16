# org.secc.DevLib

> A small shared library of developer utilities for other SECC plugins — a MEF-backed "settings component" base, EF-migration helper extensions, and a stray search DTO.

## Overview

DevLib is a low-level helper assembly, not an end-user feature. It has no blocks, jobs, REST
endpoints, or migrations of its own — it exists to be referenced by other SECC plugins. Two of its
three pieces are genuinely reusable infrastructure: a base class + MEF container for storing plugin
configuration as a Rock `Component` (attribute-backed "settings"), and a set of extension methods
that let plugin EF migrations emit primary-key / foreign-key / index SQL the way Rock expects. The
third file is a single DTO that looks misplaced.

## Project Info

- **Project file:** `org.secc.DevLib.csproj`
- **Root namespace:** `org.secc.DevLib`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly, via the PostBuildEvent `xcopy`)

## Project Layout

```
/Components/             SettingsComponent (Rock.Extension.Component base) + SettingsContainer (MEF singleton)
/Extensions/Migration/   RockMigrationExtensions + TableBuilderExtensions — EF migration SQL helpers for Rock.Plugin.Migration
/SportsAndFitness/        ControlCenterSearchItem — a lone search DTO (JSON-serializable)
/Properties/             AssemblyInfo
```

## Components

### Types

| Type | Kind | Purpose |
|------|------|---------|
| `SettingsComponent` | abstract `Rock.Extension.Component` | Base for a "settings as a Rock component" pattern; declares an abstract `Name` property. Static `GetComponent<T>()` resolves the registered component by matching `EntityType.Name` against `typeof(T).FullName` over `SettingsContainer.Instance.Dictionary`, then force-reloads its attributes (safe on multi-server). |
| `SettingsContainer` | `Container<SettingsComponent, IComponentData>` | MEF singleton (`[ImportMany]`) that discovers `SettingsComponent` implementations; `GetComponent`/`GetComponentName` look one up by entity-type string. |
| `ControlCenterSearchItem` | plain DTO | `SearchTerm` + `SearchByPIN` / `SearchByPhone` flags; `ToString()` returns `ToJson()`. No callers in this assembly. |

### Migration extension methods

Extend `Rock.Plugin.Migration` so plugin migrations can add keys/indexes that the EF `TableBuilder`
flow doesn't emit cleanly. The key/index helpers build operations on a throwaway `MiniMigration`
(a private `DbMigration` subclass), run them through `SqlServerMigrationSqlGenerator`, and hand the
resulting SQL to `migration.Sql(...)`. All three helpers ship single-column and `string[]`
multi-column overloads. Both files default a missing FK principal column to `Id`, and pick SQL Server
`"2008"` vs `"2005"` syntax based on `SqlConnection.ServerVersion`.

| Method | On | Purpose |
|--------|----|---------|
| `AddPrimaryKey` | `Rock.Plugin.Migration` | Emit an `ADD PRIMARY KEY` (single- or multi-column). |
| `AddForeignKey` | `Rock.Plugin.Migration` | Emit an `ADD FOREIGN KEY`; defaults a missing principal column to `Id`. |
| `CreateIndex` | `Rock.Plugin.Migration` | Emit a `CREATE INDEX` (single- or multi-column, optional unique/clustered). |
| `TableBuilder<T>.Run(migration)` | EF `TableBuilder<TColumns>` | Reflects the builder's `_migration` field and its `Operations`; rewrites each generated `CREATE TABLE` statement into an `ALTER TABLE … ADD CONSTRAINT` so the PK/constraints layer onto a Rock-created table. |

## Dependencies & Integrations

- **Rock:** project references to `Rock`, `Rock.Common`, and `DotLiquid` (per the `.csproj`). Uses
  `Rock.Extension.Component` / `Container<,>` (MEF component framework), `Rock.Plugin.Migration`, and
  `RockContext`-backed attribute loading (`LoadAttributes`).
- **Third-party:** EntityFramework 6.1.3 (`DbMigration`, `SqlServerMigrationSqlGenerator`,
  `MigrationOperation`); used reflectively in places.
- **Consumed by:** other SECC plugins that reference this assembly (no inbound calls within DevLib itself).

## Observations

*Noticed while documenting — not a full audit.*

- **Improvement:** `SettingsComponent.GetComponent<T>()` will throw a `NullReferenceException` if no
  matching component is registered — `FirstOrDefault()` can return `null`, but the next line
  immediately sets `component.Attributes = null`. Worth a null guard.
- **Improvement:** Both migration helpers reach into EF internals via reflection on the non-public
  `Operations` property; `TableBuilderExtensions.Run` additionally reflects the `_migration` private
  field of the `TableBuilder`. This is brittle across EF upgrades. The `Operations` lookup is
  null-guarded and silently no-ops if missing, but `Run`'s `_migration` reflection is **not** guarded —
  a missing/renamed field would throw a `NullReferenceException` on `fiMigration.GetValue(...)`. Worth
  confirming against the pinned EntityFramework 6.1.3 before any EF bump.
- **Improvement:** `SportsAndFitness/ControlCenterSearchItem` has no references inside this assembly
  and reads like it was orphaned here rather than living with its Sports/Fitness feature. Worth
  confirming whether anything still consumes it; otherwise it's a candidate to move or delete.

## Making Changes

- To add a new settings-style component, subclass `SettingsComponent` in a consuming plugin and
  decorate it for MEF export; `SettingsContainer` will discover it. Resolve it via
  `SettingsComponent.GetComponent<YourType>()`.
- To extend the migration helpers, add methods to `Extensions/Migration/RockMigrationExtensions.cs`
  (key/index helpers) or `TableBuilderExtensions.cs` (table-builder rewriting); keep the reflection
  member names in sync with the referenced EntityFramework version.
- This is a library — there are no pages, jobs, or endpoints to wire up here. Behavior surfaces in
  whichever plugin references the assembly.
