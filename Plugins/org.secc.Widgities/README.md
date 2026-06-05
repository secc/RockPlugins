# org.secc.Widgities

> A drag-and-drop content-builder: admins define reusable "widgity" types (a Lava/Markdown template plus an attribute schema), then place and configure them against any Rock entity.

## Overview

Widgities is a lightweight page-builder framework. A **WidgityType** is an admin-defined component
— a name, icon, category, enabled Lava commands, a Markdown/Lava template, and a set of attributes
(plus optional repeating "item" attributes). Editors drag instances of those types onto an entity
(today, a Rock **Block**) via the `WidgityContent` block, fill in the attribute values, and publish.
At render time each instance's attribute values are merged into its type's Markdown template through
Lava. WidgityTypes can be exported/imported as JSON for moving between environments.

## Project Info

- **Project file:** `org.secc.Widgities.csproj`
- **Root namespace:** `org.secc.Widgities`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly) and `RockWeb/Plugins/org_secc/` (block `.ascx` markup + `Widgities.css`)

## Project Layout

```
/Model/                   EF entities + services (Widgity, WidgityItem, WidgityType)
/Cache/                   ModelCache wrappers (WidgityCache, WidgityItemCache, WidgityTypeCache)
/Data/                    WidgityContext (Rock DbContext)
/Controls/                WidgityControl — the drag/drop edit+render CompositeControl
/Lava/                    CustomFilters.cs — the `TwoPass` Lava filter
/Startup/                 LoadCustomFilters — IRockOwinStartup hook that registers the filter
/Utility/EntityCoding/    WidgityType JSON export/import (IExporter / processor)
/org_secc/Widgities/      UI blocks (.ascx + .ascx.cs) and Widgities.css
/Migrations/              Rock plugin migration (Init) creating the three tables + junction
```

## Components

### Models

| Entity | Table | Purpose |
|--------|-------|---------|
| `WidgityType` | `_org_secc_Widgities_WidgityType` | Component definition: Name, Icon, Description, Markdown template, EnabledLavaCommands, HasItems, Category, and a many-to-many set of allowed `EntityType`s (`_org_secc_Widgities_WidgityTypeEntityType`). |
| `Widgity` | `_org_secc_Widgities_Widgity` | A placed instance of a type, bound to a host entity (`EntityTypeId` + `EntityGuid`) with an `Order`. Carries attribute values keyed to its type. |
| `WidgityItem` | `_org_secc_Widgities_WidgityItem` | Repeating child row under a `Widgity` (used when the type has `HasItems`), with its own attribute values and `Order`. |

Attributes for `Widgity` and `WidgityItem` are stored as Rock attributes qualified by
`WidgityTypeId` (`EntityTypeQualifierColumn = "WidgityTypeId"`), so each WidgityType effectively
owns its own attribute schema.

### Blocks

Category in Rock: **SECC > Widgities**.

| Block | Purpose | Key settings |
|-------|---------|--------------|
| Widgity Content | Renders the published widgities for the block and, in custom-settings mode, hosts the drag-and-drop editor. Extends `RockBlockCustomSettings`; binds to `EntityType = Block`, `EntityGuid = BlockCache.Guid`. | (none) |
| Widgity Type List | Grid of all WidgityTypes; add/edit links to the detail page; JSON import via uploaded file. | **DetailsPage** (LinkedPage) |
| Widgity Type Detail | Editor for a WidgityType (template, icon, category, allowed entity types, has-items toggle) and its Widgity / WidgityItem attribute grids; JSON export of the type. | (none) |

### Lava Filters

Registered at startup via `LoadCustomFilters` (`IRockOwinStartup`).

| Filter | Purpose |
|--------|---------|
| `TwoPass` | Re-resolves its input string as Lava against the current environment's merge fields and enabled commands — lets a WidgityType template emit Lava that is itself evaluated in a second pass. |

### Controls

| Control | Purpose |
|---------|---------|
| `WidgityControl` | Server `CompositeControl` that powers both display and editing. View mode merges each widgity's attribute values into its type's Markdown via `ResolveMergeFields`. Edit mode renders a Dragula-based drag/drop palette (loads `dragula.min.js` + `Widgities.css` at runtime), builds attribute editors, and publishes changes in a single wrapped transaction. |

## Dependencies & Integrations

- **Rock:** `RockContext` / `Rock.Data.DbContext`, `ModelCache`, Rock attribute framework
  (`Rock.Attribute.Helper`), `RockBlock` / `RockBlockCustomSettings`, `Rock.Utility.EntityCoding`
  (`EntityCoder` / `EntityDecoder`), Rock Lava (`ResolveMergeFields`).
- **Third-party:** EntityFramework 6.1.3, Newtonsoft.Json 11 (ViewState serialization), DotLiquid
  (Lava), Owin (startup), Dragula (client-side drag/drop, loaded from `/Scripts/dragula.min.js`).
- **Cross-plugin:** none required; the host-entity model is generic, so any plugin that places a
  `WidgityControl` against its own `IEntity` can reuse the framework.

## Migrations

Ships a single **Rock plugin migration** (not standard EF code-first) that stands up the schema. The
class derives from `Rock.Plugin.Migration` and is run by Rock's plugin-migration framework, ordered by
its `[MigrationNumber( 1, "1.8.0" )]` attribute rather than an EF timestamp:

- `Init` (file `202001131305110_Init.cs`) — uses Rock's `AddTable` / `AddPrimaryKey` / `AddForeignKey`
  helpers to create `_org_secc_Widgities_Widgity`, `_org_secc_Widgities_WidgityItem`, and
  `_org_secc_Widgities_WidgityType`, plus the `_org_secc_Widgities_WidgityTypeEntityType` junction.
  `Down()` is empty (no rollback). `Configuration.cs` is a leftover EF `DbMigrationsConfiguration`
  with `AutomaticMigrationsEnabled = false` and an empty `Seed` — it does not drive this schema.
  `WidgityContext` sets a `NullDatabaseInitializer` so the context never auto-creates a database or
  runs EF migrations.

## Observations

*Noticed while documenting — not a full audit.*

- **Security (medium):** WidgityType templates are arbitrary **Lava/Markdown authored in the admin
  UI** and rendered with the type's `EnabledLavaCommands`. The `TwoPass` filter additionally
  re-evaluates rendered output as Lava. Anyone who can edit a WidgityType (or has it enable command
  Lava such as `Sql`/`Execute`) effectively controls server-side template execution wherever that
  widgity renders. Worth confirming the Widgity Type Detail/List pages are locked to trusted admins
  and that enabled-command grants are reviewed.
- **Improvement:** `WidgityControl` serializes the full `Widgities` / `WidgityItems` model to
  **ViewState as JSON** on every interaction; pages with many widgities/items will carry a large
  ViewState payload across postbacks.
- **Improvement:** Edit-mode drag/drop drives navigation via `window.location = "javascript:__doPostBack(...)"`
  and parses `__EVENTARGUMENT` by delimiter; the control depends on loading `dragula.min.js` and
  jQuery at runtime. Brittle to client-side ordering/availability — worth a look if drag/drop misbehaves.
- **Improvement:** The host entity is hard-wired to `Block` in the `WidgityContent` block
  (`EntityType = Block`, `EntityGuid = BlockCache.Guid`), even though the model supports any
  `IEntity`. Reusing widgities against other entities needs a new block/control host.

## Making Changes

- To change how a widgity renders or how the editor behaves, edit `Controls/WidgityControl.cs`
  (`ShowWidgities` for view rendering, `ShowEdit` / `Publish` for the editor and save path).
- To change the type editor (template field, attribute grids, JSON export), edit
  `org_secc/Widgities/WidgityTypeDetail.ascx(.cs)`; the list and JSON import live in
  `WidgityTypeList.ascx(.cs)`.
- New columns on the entities require a new Rock plugin migration under `/Migrations/` (next
  `[MigrationNumber]`) and matching updates
  to the `Model/` classes and `Cache/` wrappers (caches are cleared via
  `WidgityCache.Clear()` / `WidgityItemCache.Clear()` / `WidgityTypeCache.Clear()` on publish/save).
- To add a Lava filter, edit `Lava/CustomFilters.cs`; the whole type is registered in
  `Startup/LoadCustomFilters.cs`. See [org.secc.QRManager](../org.secc.QRManager/README.md) for the
  same startup-filter pattern.
