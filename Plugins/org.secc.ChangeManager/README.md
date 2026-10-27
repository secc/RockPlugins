# org.secc.ChangeManager

> A change-request/approval framework that captures edits to any Rock entity as reviewable `ChangeRecord`s and applies (or reverses) them transactionally on approval.

## Overview

ChangeManager lets users propose edits to Rock data — a person's profile, phone numbers, addresses, attribute values — without writing them straight to the live record. Each submission becomes a `ChangeRequest` holding one or more `ChangeRecord` rows (the old/new value for a property, attribute, create, or delete). Public self-service blocks (profile edit, family-member removal) generate these requests; staff blocks review and approve them. Approved changes are applied to the target entity via reflection inside a DB transaction, and rejected ones can be undone. It's used for self-service profile updates where some people's edits auto-apply and others (staff, VIPs) get held for review.

## Project Info

- **Project file:** `org.secc.ChangeManager.csproj`
- **Root namespace:** `org.secc.ChangeManager`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly + `.pdb`) and `RockWeb/Plugins/org_secc/` (block `.ascx` markup), via the PostBuildEvent `xcopy`

## Project Layout

```
/Model/        EF entities — ChangeRequest, ChangeRecord (+ services); BasicEntity (Hashids-keyed IEntity stub)
/Data/         ChangeManagerContext (DbContext on RockContext, NullDatabaseInitializer) + base ChangeManagerService<T>
/Utilities/    PropertyChangeEvaluators (extension methods that diff an entity into ChangeRecords); SecurityChangeDetail DTO
/org_secc/ChangeManager/   UI blocks (.ascx + .ascx.cs)
/Migrations/   Rock plugin migrations (tables, family-group column, campus-type defined value)
```

## Components

### Models

EF entities on `ChangeManagerContext` (shares the `RockContext` connection; initializer set to null — no auto-migration). Both are `Rock.Data.Model<T>`, `ISecured`, `IRockEntity`.

| Entity | Table | Notes |
|--------|-------|-------|
| `ChangeRequest` | `_org_secc_ChangeManager_ChangeRequest` | One proposed change-set against an entity (`EntityTypeId` + `EntityId`). Holds requestor/approver aliases, comments, `IsComplete`, and an optional `FamilyGroupOfPersonAliasId` (head-of-household snapshot, for family-merge resilience). `PreSaveChanges` resolves the target entity's display name into `Name`. |
| `ChangeRecord` | `_org_secc_ChangeManager_ChangeRecord` | A single field/attribute/create/delete within a request: `OldValue`/`NewValue` (JSON or string), `Property`, `Action` enum, `IsRejected`, `WasApplied`, and an optional `RelatedEntityType`/`RelatedEntityId` for changes to a related entity (e.g. a phone number on a person). |

`ChangeRecordAction` enum: `Update (0)`, `Create (1)`, `Delete (2)`, `Attribute (3)`.

`ChangeRequest.CompleteChanges(...)` is the core apply engine: inside a `BeginTransaction`, it applies pending records (set property via reflection, set attribute, create/delete related entity) and reverses any `WasApplied && IsRejected` records, committing only if no errors accumulate. Records are processed in `Id` order (so a request's deletes run before the creates recorded after them), and processing stops at the first error. Related-entity services are resolved by reflection with typed `Add`/`Delete` lookups, because services such as `GroupMemberService` overload those methods. Person email/phone changes are collected into a `SecurityChangeDetail`; only after a successful commit, if there are any, they launch the workflow named in the **`ChangeManagerSecurityWorkflow`** global attribute.

### Utilities

`PropertyChangeEvaluators` — extension methods on `ChangeRequest` that diff a proposed value against the current entity and append a `ChangeRecord` only when something actually changed:

| Method | Purpose |
|--------|---------|
| `EvaluatePropertyChange(...)` | Overloaded for `string`, `int?`, `bool`, `Enum`, `DateTime?`, `IEntity`, `IEntityCache`; emits an `Update` record on a diff. |
| `AddEntity(...)` | Emits a `Create` record (serialized new entity). |
| `DeleteEntity(...)` | Emits a `Delete` record (serialized old entity). |
| `EvaluateAttributes(...)` | Loads the persisted entity's attributes and emits an `Attribute` record per changed attribute value. |

### Blocks

Category in Rock: **SECC > CRM**.

| Block | Purpose | Key settings |
|-------|---------|--------------|
| Change Managed Public Profile Edit (`CMPublicProfileEdit`) | Public self-service profile editor; writes edits as a `ChangeRequest` instead of directly. | Default Connection Status, Disable Name Edit, Address Type, Show Phone Numbers, Phone Types, Required Adult Phone Types, Require Adult Email Address, Show Communication Preference, Display Terms of Service (`DisplayTerms`) / Terms of Service Text (`TermsOfServiceText`), Show Campus Selector, Person Attributes (adults) / (children), Campus Types to Display (`CampusTypes`) |
| Change Managed Profile Remove Family Member (`CMPublicProfileRemovePerson`) | Public block to move/remove a family member, recorded as a change request. | `DisplayPhone`, `DisplayEmail` |
| Change Entry (`ChangeEntry`) | Staff/entry block to enter changes for later review; can auto-apply based on data views. | `AutoApply`, Approved Updaters Data View (`ApprovedDataView`), Blacklist Data View (`BlacklistDataView`), Workflow, `SimpleMode` |
| Change Request Detail (`ChangeRequestDetail`) | Review a single request's records and approve/reject. | Blacklist Data View (`BlacklistDataView`), Workflow |
| Change Requests (`ChangeRequests`) | Grid of all requests, filterable by entity type / completion. | Details Page (linked) |
| Managed Attribute Values (`ManagedAttributeValues`) | Person-detail block to edit a category of person attributes via Change Manager. | Category (attribute category), Attribute Order |

**Auto-apply gating:** `ChangeEntry` applies changes immediately when `AutoApply` is on, but a requestor in the **Blacklist Data View** is always held for review, and an **Approved Updaters Data View** overrides `AutoApply`. `ChangeRequests`/`ChangeRequestDetail` restrict non-`EDIT`-authorized users to requests where they are the requestor.

## Dependencies & Integrations

- **Rock:** `RockContext` / `Rock.Data.DbContext`, `Rock.Data.Model<T>` + `ISecured`, EntityType/DefinedValue/GroupType caches, `Reflection.GetServiceForEntityType` (reflection-based entity service resolution), `IHasAttributes` (load/set/save attribute values), the workflow engine (`LaunchWorkflow`), `GlobalAttributesCache`, the Rock block/UI framework, plugin migrations (`Rock.Plugin`).
- **Third-party:** `Hashids.net` (used only by `BasicEntity.IdKey`), EntityFramework 6.
- **Cross-plugin:** none at build time.

## Migrations

Ships Rock plugin migrations under `/Migrations/`:

- `001_Init` — creates `_org_secc_ChangeManager_ChangeRequest` and `_org_secc_ChangeManager_ChangeRecord` (keys, FKs to PersonAlias/EntityType, indexes).
- `002_FamilyGroupOfPerson` — adds `FamilyGroupOfPersonAliasId` to `ChangeRequest` (head-of-household snapshot for family-merge cases).
- `003_InternalCampusTypeValue` — seeds an "Internal" Campus Type defined value (`DF089AAE-…`).

## Observations

*Noticed while documenting — not a full audit.*

- **Note:** In `CompleteChanges`, errors are accumulated into a list and the whole transaction rolls back on any failure. It stops at the first error because a failed save leaves its entity tracked in the context, and every later `SaveChanges` would throw the same error again. A failure to launch the security-notice workflow after commit is logged to the Exception Log, not returned as an error, since the changes are already saved.
- **Note:** `CompleteChanges` uses a raw `Database.BeginTransaction()`, not Rock's `WrapTransactionIf`, so Rock post-save side effects (cache/bus/index after-commit actions, group-member history) can run even when the request later rolls back.
- **Improvement:** `EvaluateAttributes` news up its own `RockContext` per call to reload the persisted entity, separate from any context the caller holds. Fine for one-offs, an extra round-trip in loops.

## Making Changes

- To change what the public editor exposes or how it builds requests, edit `org_secc/ChangeManager/CMPublicProfileEdit.ascx.cs`; the diffing helpers it calls live in `Utilities/PropertyChangeEvaluators.cs`.
- The apply/reverse engine is `ChangeRequest.CompleteChanges` in `Model/ChangeRequest.cs` — add new `ChangeRecordAction` handling there and a matching evaluator in `PropertyChangeEvaluators`.
- New tables/columns or seed data belong in a new numbered migration under `/Migrations/` — don't hand-edit migrations that have already run.
- The security-notice workflow is keyed off the `ChangeManagerSecurityWorkflow` global attribute; related workflow helpers live in [org.secc.Workflow](../org.secc.Workflow/README.md).

Last updated: 2026-10-27
