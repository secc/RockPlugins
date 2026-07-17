# org.secc.FamilyCheckin

> Southeast's custom check-in stack — kiosk/kiosk-type models, a library of check-in workflow actions, a cached check-in state layer, mobile/touchless check-in, and the consent and quick-check-in UI blocks.

## Overview

FamilyCheckin replaces and extends Rock's stock check-in for Southeast's children's and event
ministries. It ships its own **Kiosk** and **KioskType** entities (so a physical device's
configuration, theme, label printer, and grace window live in plugin tables), ~25 custom check-in
**workflow actions** that override Rock's load/filter/save pipeline, a custom cache layer that
keeps check-in state and mobile reservations hot, a **MobileCheckinRecord** entity for phone/SMS
reservations, and the kiosk-facing UI blocks (quick check-in, quick search, medical consent,
pre-registration, mobile start). Used by check-in staff and parents at kiosks and on phones.

## Project Info

- **Project file:** `org.secc.FamilyCheckin.csproj`
- **Root namespace:** `org.secc.FamilyCheckin`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly), `RockWeb/Plugins/org_secc/` (block `.ascx` markup),
  `RockWeb/Themes/` (check-in themes), and `RockWeb/Scripts/CheckinClient/` (`ZebraPrint.js`)
- **Cross-plugin dependency:** [org.secc.DevLib](../org.secc.DevLib/README.md)

## Project Layout

```
/Model/          Kiosk, CheckinKioskType, MobileCheckinRecord entities + services (EF, custom tables)
/Data/           FamilyCheckinContext — plugin EF DbContext (NullDatabaseInitializer; uses RockContext db)
/Cache/          CheckinCache<T> + KioskType/MobileCheckinRecord/Attendance/Occurrence caches
/Workflows/      ~25 custom CheckInActionComponent actions (load, filter, save, reserve, labels)
/Rest/           FamilyCheckinController + session-enabled route/controller handlers
/Jobs/           Quartz IJob — expire mobile reservations, reset disabled group-location-schedules
/FieldTypes/     CheckinGroupFieldType (single/multi check-in group picker)
/Attribute/      CheckinGroupFieldAttribute (block-attribute helper for the above)
/UI/ /Controls/  CheckinGroupPicker control, archiving-aware group row
/Utilities/      CheckinLabelGen, LabelPrinter, KioskDeviceHelpers, Constants
/Exceptions/     CheckInStateLost
/org_secc/FamilyCheckin/   UI blocks (.ascx + .ascx.cs)
/Themes/         SE Kids + SEKids2020 (+ Shine / Students) check-in themes
/Scripts/        ZebraPrint.js (client-side label printing)
/Migrations/     Rock plugin migrations (entities, defined types, pages, attributes)
```

## Components

### Custom Models (EF entities in plugin tables)

| Entity | Table | Purpose |
|--------|-------|---------|
| `Kiosk` | `_org_secc_FamilyCheckin_Kiosk` | A check-in device: name, KioskType, IP, printer device, print-from/to override. `ISecured`, `ICategorized`. |
| `CheckinKioskType` | `_org_secc_FamilyCheckin_KioskType` | Reusable kiosk config: check-in template, campus, minutes-valid / grace minutes, theme. `ISecured`. |
| `MobileCheckinRecord` | `_org_secc_FamilyCheckin_MobileCheckinRecord` | A mobile/SMS check-in reservation, keyed by a unique `AccessKey` and `UserName`, with an expiration. |

### REST Endpoints

Registered via `FamilyCheckinController` (`IHasCustomHttpRoutes`); routed through a
session-enabled handler so check-in state persists in `HttpContext.Session`.

| Route | Method | Auth | Purpose |
|-------|--------|------|---------|
| `api/org.secc/familycheckin/Family/{param}` | GET | Kiosk-session gated | Run the configured check-in workflow for a phone-number search and return matching families. Requires `Session["BlockGuid"]` to resolve to the **QuickSearch** kiosk block type; enforces the block's min/max phone length server-side; rate-limited per session; returns a projected result (`Caption`, `SubCaption`, `Group.Id`) rather than the full check-in graph. See ROCK-8765. |
| `api/org.secc/familycheckin/ProcessMobileCheckin/{param}` | GET | `[Authenticate, Secured]` | Process a mobile (UserLogin-search) check-in; adds the user's primary family to check-in state. Verifies the authenticated caller owns the session's `UserLogin` (Rock administrators exempt to preserve `?UserName=` debug impersonation). |
| `api/org.secc/familycheckin/KioskStatus/{param}` | GET | Anonymous (by design) | Report whether a kiosk type is currently open / has available locations. Returns only an `{active: bool}` boolean; kiosks are unauthenticated browsers, so this stays open. |

### Workflow Actions

Category in Rock: **SECC > Check-In**. All subclass Rock's `CheckInActionComponent`
(except `Save SMS Attendance`, which subclasses `ActionComponent`) and are MEF-exported, so they
drop into a check-in workflow with no code change.

| Action (ComponentName) | Purpose |
|------------------------|---------|
| Load All Groups / Group Types / Locations / Cacheless / Override | Custom replacements for Rock's load steps (cache-backed and override variants). |
| Custom Find Families | Family search step. |
| Load Person By PIN / Load Person By Parent's phone number | Alternate person-lookup load steps. |
| Search By PIN | PIN search step. |
| Select All People | Auto-select everyone in the family. |
| Filter Groups By Ability / Birthday / Grade (Custom) / Membership | Custom group-filter steps. |
| Sideload Groups | Add additional groups into check-in state. |
| Historical Preselect | Pre-select based on prior attendance. |
| Reserve Attendance | Create a (mobile) attendance reservation. |
| Save Attendance Custom | Save attendance. |
| Save Attendance And Remove Session | Save, then clear the mobile reservation/session. |
| Save SMS Attendance | Save attendance arriving via SMS. |
| Remove Full GroupLocationSchedules | Drop locations that are at capacity. |
| Aggregate Checkin Label / Create Event Labels / Create Medication Labels / Reprint Aggregate | Label generation and reprinting. |
| Load All Override | Group-type load override. |
| S&F Childcare Receipt / Deduct Sports Fitness Chidcare Credits | Sports & Fitness childcare receipt + credit deduction. |

### Blocks

Category in Rock: **SECC > Check-in** (medical-consent block: **SECC > Family Check In**).

| Block | Purpose |
|-------|---------|
| QuickCheckin | Helps parents check in their whole family quickly. |
| QuickSearch | Helps parents find their family quickly. |
| Mobile Check-in Start | Start page for the mobile check-in process. |
| Family Check-In Consent | Consent screen shown during check-in. |
| My Account Medical Consent | Prompts a parent for medical consent for minors in their family. |
| Childrens Pre-Registration | Pre-register families (children's-ministry oriented). |
| AutoConfigure | Auto-configures a kiosk's check-in setup. |
| Kiosk List / Kiosk Detail | Admin CRUD for `Kiosk` records. |
| Kiosk Type List / Kiosk Type Detail | Admin CRUD for `CheckinKioskType` records. |

**Mobile-reservation validation in QuickCheckin:** when a family with an active
`MobileCheckinRecord` arrives at a kiosk, the block validates the reservation against the
**persisted** Attendance → Occurrence → Group records, not `OccurrenceCache`. A reserved room can
be closed (its group-location-schedule detached) between reservation and arrival, which makes the
occurrence unresolvable in cache even though the reservation is still valid and completable — the
completion path works straight off the attendance records. By design, closing a room does **not**
cancel existing mobile reservations for it; the children's team moves those attendees manually.

### Jobs

Quartz `IJob`s (both `[DisallowConcurrentExecution]`); scheduled in Rock, not self-registered.

| Job | Purpose |
|-----|---------|
| `RemoveExpiredMobileReservations` | Cancels mobile check-in records that are expired or created before today. |
| `ResetGroupLocationSchedules` | Re-enables group-location-schedules parked in the "disabled GLS" defined type. |

### Field Type

| Field type | Purpose |
|------------|---------|
| `CheckinGroupFieldType` | Picks a single or (configurably) multiple check-in groups; stores `Group.Guid`. Backed by `CheckinGroupPicker` and `CheckinGroupFieldAttribute`. |

## Dependencies & Integrations

- **Rock:** check-in engine (`CheckInState`, `CheckInActionComponent`), workflow engine,
  `RockContext`, EF `DbContext`, `RockCache` / message bus, REST (`ApiController`,
  `IHasCustomHttpRoutes`), defined types, Quartz scheduler, field-type framework.
- **Cross-plugin:** [org.secc.DevLib](../org.secc.DevLib/README.md).
- **Third-party:** EntityFramework, CacheManager.Core, Newtonsoft.Json, ASP.NET Web API; client-side
  Zebra label printing via `ZebraPrint.js`.
- **Related:** scannable check-in codes are produced by
  [org.secc.QRManager](../org.secc.QRManager/README.md).

## Migrations

Ships Rock plugin migrations that install the plugin's entities, pages, defined types, and
attributes (don't hand-edit ones that have already run):

- `001_Init` — initial Kiosk / KioskType schema.
- `002_Message`, `003_UpdateCacheTags` — messaging + cache-tag setup.
- `004_Touchless`, `007_SearchType` — touchless / search-type config.
- `005_KioskCategories`, `006_KioskNameIndex` — kiosk categorization + unique-name index.
- `008_MobileCheckinRecord`, `009_ExpirationTime`, `014_AccessKey` — mobile-reservation table,
  expiration, and access key.
- `010_AttendanceQualifiers`, `013_GroupFilterAttributes` — attendance qualifiers + group-filter attributes.
- `011_KioskTypeTheme` — kiosk-type theme.
- `012_CheckinCampusFamilyDT`, `021_DisabledGLSsDT` — campus/family + disabled-GLS defined types.
- `020_FamilyCheckinWorkflow`, `022_CheckinGroupTypes`, `027_MoveGroupTypes` — workflow + group-type data.
- `023`–`026` — Kiosk-Type / Family / Super / Mobile check-in pages.
- `030_LastMedicationCheckinAttribute`, `031_MedicalConsentKioskTypeAttributes`,
  `032_KioskTypeEntityUpdates` — medication/medical-consent attributes and kiosk-type entity updates.

## Observations

*Noticed while documenting — not a full audit.*

- **Security (medium):** Check-in handles minors' PII (names, birthdays, grade, medical/medication
  consent, allergy data driving labels). Confirm the Rock **page/block security** on the consent and
  pre-registration blocks, and that the kiosk-facing pages are restricted to the intended check-in
  roles.
- **Security (review):** Mobile check-in is gated by `MobileCheckinRecord.AccessKey` (a unique
  string consumed by `GetByAccessKey`). Worth confirming the access key is generated from a
  cryptographically strong, non-guessable source, since possessing it identifies a family's
  reservation.
- **Security (ROCK-8765, addressed on `sl-bugfix-8765`):** `FamilyCheckinController.Family` returned
  full `CheckInFamily` graphs (family captions + members' first names, incl. minors) to any caller
  whose session had a `BlockGuid`. Investigation found the mobile-checkin vector does **not** actually
  leak — its workflow activity (`Person Search → Find Family Members`) assumes a selected family and
  errors under a phone search — so the real exposure is the kiosk QuickSearch path, which requires a
  staff-provisioned kiosk session. Because a console `fetch` on the kiosk page is indistinguishable
  from the page's own AJAX, the kiosk path can't be blocked in code without breaking check-in; that
  residual risk is accepted and gated by kiosk provisioning + rate limiting. Fix adds a `BlockGuid`
  provenance check (rejects non-QuickSearch sessions, incl. mobile), server-side length enforcement,
  per-session rate limiting, and a projected response. Note `CheckInFamily.SubCaption` is member
  first names by design (`Rock/Workflow/Action/CheckIn/FindFamilies.cs`), so it is still returned on
  the legitimate kiosk path.
- **Security (low):** `Family` decrypts the `LocalDeviceConfig` cookie and trusts `CurrentKioskId`
  from it to pick the kiosk; worth confirming the cookie's integrity protection is sufficient for the
  device-config trust placed in it.
- **Improvement:** The REST `Family` and `ProcessMobileCheckin` handlers are near-duplicates of the
  same activate-workflow / process / save-state block — a shared private helper would cut the
  copy-paste and keep the two paths from drifting.

## Making Changes

- To change a check-in step (load/filter/save/label), edit the matching action in `Workflows/`;
  follow a sibling as a template and keep `[ActionCategory("SECC > Check-In")]` + the MEF export so
  Rock discovers it. The aggregate label actions live in `CreateLabelsAggregate` / `ReprintAggregate`.
- Kiosk/KioskType/MobileCheckinRecord shape changes go in `Model/` plus a new numbered migration
  under `/Migrations/`; the cache wrappers in `Cache/` must be kept in sync (see the desync checks
  in `MobileCheckinRecordCache`).
- Kiosk-facing UI lives in `org_secc/FamilyCheckin/*.ascx[.cs]`; visual theming lives under
  `/Themes/`.
- Related: scannable codes come from [org.secc.QRManager](../org.secc.QRManager/README.md);
  shared helpers from [org.secc.DevLib](../org.secc.DevLib/README.md).

**Last updated:** 2026-07-17