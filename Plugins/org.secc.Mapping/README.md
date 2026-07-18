# org.secc.Mapping

> Computes and caches driving distance/time from an address to a set of Rock entities (campuses, groups) via the Azure Maps Route Matrix API, exposed as a REST endpoint and a workflow action.

## Overview

Mapping answers "which of these is closest to me?" for a supplied address. A REST endpoint takes
an address plus a defined-value that names a mappable entity set (a campus list, a group type, a
parent group's children, or a location-valued attribute), resolves each entity's address, asks
Azure Maps for driving distance and time, and returns them ordered by **shortest travel time**
(`OrderBy(TravelDuration)`), nearest-first. Results are cached
(in-memory + a backing table) so repeat lookups don't re-hit the paid API. It also ships a
"Closest Campus" workflow action for use inside Rock workflows.

## Project Info

- **Project file:** `org.secc.Mapping.csproj`
- **Root namespace:** `org.secc.Mapping`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly + `.pdb`, via the PostBuildEvent `xcopy`). The
  PostBuildEvent also xcopies an `org_secc` folder, but none ships in source (no `.ascx` markup).

## Project Layout

```
/Azure/        AzureDistanceMatrix — calls the Azure Maps Route Matrix API, merges cached results
/Data/         MappingContext (EF DbContext on RockContext) + base MappingService<T>
/Model/        Destination (transient DTO) + LocationDistanceStore entity & service (distance cache)
/Rest/Controllers/  DistanceController — api/mapping/distance endpoint, per-entity-type resolvers
/Utilities/    CampusUtilities, GroupUtilities (build destination lists), Constants (defined-type Guid)
/Workflow/     ClosestCampusAction — "Closest Campus" workflow action
/Migrations/   Rock plugin migrations (distance table, "Distance Mappable Entities" defined type)
```

## Components

### REST Endpoints

| Route | Method | Purpose |
|-------|--------|---------|
| `api/mapping/distance/{definedValueId}/{address}` | GET (authenticated) | Order the entities described by `{definedValueId}` by travel **time** from `{address}` (results are sorted by `TravelDuration`). Returns a `{ entityId: distanceInMiles }` dictionary — the *values* are the travel distance in miles, but the *ordering* is by duration. |

`{definedValueId}` must be a value in the **Distance Mappable Entities** defined type
(`Constants.UniversalDefinedTypeGuid` = `7F70ABE9-1705-4DED-BABE-6D720EC52914`); otherwise the
endpoint returns `BadRequest`. The defined value's **EntityType** attribute selects the resolver:

| EntityType | EntityId means | Resolver |
|------------|----------------|----------|
| `Campus` | (ignored) | All campuses with a `Location.Street1` (`CampusUtilities.OrderCampusesByDistance`). Returns an ordered `List<CampusCache>`, nearest-first — **not** a dictionary like the other resolvers. |
| `ParentGroup` | parent group Id | Active, non-archived, public child groups of that group that also have a group-location whose `PostalCode` is non-empty (`GroupUtilities.GetGroupsDestinations`). Groups with no usable location are dropped. |
| `GroupType` | group type Id | Active, non-archived, public groups of that type, same group-location/`PostalCode` filter as `ParentGroup`. |
| `Attribute` | attribute Id | Locations referenced by that attribute's values (each value is parsed as a `Location` Guid; non-Guid/empty values are skipped). |

### Workflow Actions

| Action (ComponentName) | Category | Purpose | Key settings |
|------------------------|----------|---------|--------------|
| `ClosestCampusAction` ("Closest Campus") | SECC > Mapping | Resolve the campus closest to an address and write its Guid to a Campus attribute. | **Origin** (text-or-attribute, Lava), **Campus** (Campus attribute, output) |

The action runs the async lookup synchronously (`Task.Run(...).Wait()`); exceptions are logged to
the workflow action and execution still returns `true`.

### Models

| Type | Table | Notes |
|------|-------|-------|
| `LocationDistanceStore` | `_org_secc_Mapping_LocationDistanceStore` | Persistent distance cache: `Origin`, `Destination` (both formatted-address strings, indexed), `TravelDistance` (miles), `TravelDuration` (minutes), `CalculatedBy`. `Model<T>`, `ISecured`, `IRockEntity`. |
| `Destination` | — | Transient DTO (not persisted). Holds `EntityId`, `LocationId`, `Address` (lazily resolved from `LocationId` via `LocationService`), and the computed distance/duration. `LavaVisible` on `EntityId`. |

`LocationDistanceStoreService.LoadDurations` checks an in-memory `RockCache` region
(`org.secc.Mapping.LocationDistance`) first, then the `LocationDistanceStore` table, before
`AzureDistanceMatrix` calls the API for anything still uncalculated; new results are written back to
both.

## Dependencies & Integrations

- **Rock:** `RockContext`, `DefinedValueCache` / `CampusCache` / `GlobalAttributesCache`, `RockCache`,
  `LocationService` / `GroupService` / `GroupLocationService` / `AttributeValueService`, workflow
  engine (`ActionComponent`), Rock REST (`ApiControllerBase`, `[Authenticate]`), plugin migrations,
  EntityFramework 6.
- **Third-party:** Azure Maps **Route Matrix API** (`atlas.microsoft.com/route/matrix`, key from the
  `AzureMapsKey` global attribute), Newtonsoft.Json.

## Migrations

Ships Rock plugin migrations under `/Migrations/`:

- `001_Init` — creates `_org_secc_Mapping_LocationDistanceStore` (the distance cache table).
- `002_AddDefinedType` — creates the **Distance Mappable Entities** defined type (under Tools) with
  two attributes: `EntityType` (DDL field; `values` qualifier
  `GroupType^Group Type,ParentGroup^Child Groups of Group,Campus^Campus,Attribute^Address Attribute`)
  and `EntityId` (text field). It seeds four defined values: **Campus** (`EntityType=Campus`),
  **Home Groups** (`GroupType`, EntityId `60`), **Blankenbaker Home Groups** (`ParentGroup`, EntityId
  `820057`), and **Content Channel Attributes** (`Attribute`, EntityId `89281`).

## Observations

*Noticed while documenting — not a full audit.*

- **Security (low):** `{address}` is passed straight through to the Azure Maps payload and is also
  stored as the `Origin` column (the in-memory cache key is `origin + destinationAddress`). The
  endpoint is `[Authenticate]`-gated, so callers must be
  logged-in Rock users, but the address travels in the URL path (logged in IIS/proxy logs). Worth
  confirming that's acceptable for the addresses being queried.
- **Improvement:** The Azure Maps key is read from the `AzureMapsKey` global attribute on every call;
  confirm that attribute is stored encrypted and not broadly readable, since it bills against a paid
  Azure subscription. If `AzureMapsKey` is empty the request still fires and simply fails (logged),
  rather than short-circuiting.
- **Improvement:** `AzureDistanceMatrix.OrderDestinations` returns **only** destinations where
  `IsCalculated == true`, sorted by `TravelDuration`. Any address Azure can't route (bad/foreign
  address, API failure) is silently dropped from the results rather than reported, so the caller can't
  distinguish "far away" from "couldn't be calculated." Note the sort key is travel *time*, while the
  REST response values are travel *distance* in miles — the two can disagree on ordering.
- **Improvement:** `Destination.Address`'s getter constructs a new `RockContext` and runs a
  `LocationService.Get` on first access — a side effect hidden in a property getter, and one context
  per uncached destination when building large lists.

## Making Changes

- To add a new mappable entity kind, add a `case` in `DistanceController.GetDistance` (and a matching
  resolver) and extend the `EntityType` attribute's DDL `values` qualifier in a new migration; the
  endpoint dispatches purely on the defined value's `EntityType`.
- The Azure Maps request shape (travel mode, route type, units) lives in
  `Azure/AzureDistanceMatrix.cs`; distance is converted to miles and duration to minutes there.
- Caching behavior (in-memory region + `LocationDistanceStore` table) is in
  `Model/LocationDistanceStoreService.cs`.
- The "Closest Campus" workflow action is MEF-discovered (`[Export(typeof(ActionComponent))]`); see
  [org.secc.Workflow](../org.secc.Workflow/README.md) for the broader library of SECC workflow actions.
