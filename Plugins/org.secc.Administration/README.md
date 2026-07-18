# org.secc.Administration

> A grab-bag of admin-only Rock UI blocks for Southeast staff — system diagnostics, one-off data migrations, and group/device housekeeping tools.

## Overview

Administration is a small set of Rock **UI blocks** in the `SECC > Administration` category, used by
Rock admins for maintenance and one-off data work. It covers system diagnostics (a SECC variant of
Rock's System Info), bulk data migrations (baptism attribute-matrix → Step records), group surgery
(change a group's type, merge neighborhood-group rosters), and personal-device management synced to
the Front Porch WiFi API. There is no service layer or REST surface — each block is a standalone
admin tool dropped on a page.

## Project Info

- **Project file:** none — no `.csproj`; RockWeb-compiled (block `.ascx` + `.ascx.cs` only).
- **Root namespace:** `RockWeb.Plugins.org_secc.Administration`
- **Target framework:** compiles in-place under RockWeb (ASP.NET WebForms / .NET Framework).
- **Deploys to:** `RockWeb/Plugins/org_secc/Administration/` (the `.ascx` markup + code-behind).

## Project Layout

```
/org_secc/Administration/   UI blocks (.ascx + .ascx.cs), category "SECC > Administration"
```

## Components

### Blocks

Category in Rock: **SECC > Administration**.

| Block (`DisplayName`) | Purpose | Key settings |
|-----------------------|---------|--------------|
| `System Information` | SECC variant of Rock's System Info — version, DB info/size/snapshot-isolation, migrations, cache stats, routes, transaction queue; buttons to clear cache, restart the app, and dump a diagnostics text file. | (none) |
| `Baptism Data Migrator` | One-off migration: converts baptism data stored in a person attribute-matrix into Step records of a Baptism Step Type. Runs as a SignalR background task with live progress. | **BaptismStepType** (StepProgramStepType), **StepStatus** (completed status), **BaptismPersonAttribute** (matrix person attribute) |
| `Group Type Changer` | Changes a group's `GroupType`, remapping member roles and group-member attributes onto the new type via UI dropdowns. | (none — driven by `GroupId` page param) |
| `Neighborhood Group Merge` | Merges a source neighborhood group into a target: adds missing members as Inactive, repoints attendance occurrences, and unions the `LWYACycle` attribute. | (none — group pickers in UI) |
| `Personal Devices` | Person-context block listing a person's `PersonalDevice` rows (Lava-rendered); add/edit/inactivate devices and sync MAC addresses to the Front Porch WiFi API. | **InteractionsPage** (LinkedPage), **LavaTemplate**, **Remove Device** (bool), **SecureNetworkSSID** |

## Dependencies & Integrations

- **Rock:** `RockBlock` / WebForms UI framework, `RockContext` and Rock services (`Group`, `GroupType`,
  `Attendance`, `Step`, `PersonalDevice`, `Attribute`/`AttributeValue`, `Person`), attribute-matrix
  model, Step program model, `RockCache` / cache & migration introspection, SignalR (`RockMessageHub`),
  DotLiquid/Lava.
- **Third-party APIs:** Front Porch WiFi user API (`PersonalDevices` — add/modify/delete/get users by
  MAC; host + token from Global Attributes `FrontporchHost` / `FrontporchAPIToken`).
- **Cross-plugin:** none in code. Front Porch device removal is also done by the
  `FrontPorchDeviceRemoval` job in [org.secc.Jobs](../org.secc.Jobs/README.md).

## Observations

*Noticed while documenting — not a full audit.*

- **Security (low):** `System Information`'s diagnostics dump and restart/clear-cache actions are
  high-privilege operations (it can `UnloadAppDomain`, delete `App_Data/Cache`, and emit server
  variables/DB details). Confirm the page hosting this block is locked to admins. The dump explicitly
  skips `HTTP_COOKIE` but still includes all other server variables.
- **Security (low):** `Personal Devices` reaches the Front Porch API with a token from the
  `FrontporchAPIToken` Global Attribute. Confirm that attribute is stored encrypted and not broadly
  readable, and that MAC addresses (PII-adjacent) are only exposed to authorized staff.
- **Improvement:** Several blocks open multiple `new RockContext()` instances within one operation and
  call `SaveChanges()` mid-flow (e.g. `GroupTypeChanger` loads `group` on one context but resolves the
  new group type on a throwaway context in `OnLoad`; `NeighborhoodGroupMerge` saves between steps).
  These multi-context, multi-save flows aren't transactional — a partial failure can leave a half-merged
  group or half-remapped roles. Worth confirming these tools are run carefully / on backed-up data.
- **Improvement:** `Baptism Data Migrator`, `Group Type Changer`, and `Neighborhood Group Merge` read as
  one-off / occasional-use migration tools. If they've already served their purpose, consider retiring
  the blocks (and their pages) rather than leaving destructive group/attribute edits a click away.

## Making Changes

- Each block is self-contained — edit the matching `*.ascx.cs` (and `.ascx` markup) under
  `org_secc/Administration/`. There's no shared service layer, no migrations, and no DI to thread through.
- `Personal Devices` is `[ContextAware(typeof(Person))]` and also accepts a `PersonGuid` page param;
  its Front Porch endpoints (`/api/user/add|modify|delete|get`) live in the private FP helper methods.
- Front Porch host/token come from Global Attributes (`FrontporchHost`, `FrontporchAPIToken`); see the
  `FrontPorchDeviceRemoval` job in [org.secc.Jobs](../org.secc.Jobs/README.md) for the batch counterpart.
