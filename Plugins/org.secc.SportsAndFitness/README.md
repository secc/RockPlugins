# org.secc.SportsAndFitness

> Rock blocks and a theme for Southeast's Activities Center — member/guest check-in, group-fitness session tracking, guest self-registration, and a staff "control center".

## Overview

SportsAndFitness is the UI for Southeast's Sports & Fitness (Activities Center) ministry. It ships
kiosk check-in blocks (members and group-fitness sessions), a multi-step guest self-registration
flow with a liability waiver and emergency contacts, and a set of "control center" blocks staff use
to search participants, check in guests, manage PINs, and view per-person actions/history. It also
carries its own Rock **theme** (`Themes/SportsAndFitness`). Behavior is driven by Rock check-in
workflows, group membership, and person/group-member attributes the blocks read and write.

## Project Info

- **Project file:** `org.secc.SportsAndFitness.csproj` — but it compiles **only** `Properties/AssemblyInfo.cs`; the block `.ascx.cs` files are **not** in the `<Compile>` list, so they are **RockWeb-compiled** (the project's job is really to `xcopy` markup into RockWeb).
- **Root namespace:** `org.secc.SportsAndFitness` (blocks live under `RockWeb.Plugins.org_secc.SportsAndFitness`)
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/Plugins/org_secc/` (block `.ascx`/`.cs`/`.lava`) and `RockWeb/Themes/` (the theme), via the **PreBuildEvent** `xcopy` of `org_secc` and `Themes\*`.
- **Cross-plugin dependency:** [org.secc.FamilyCheckin](../org.secc.FamilyCheckin/README.md) — `GroupFitness` uses `org.secc.FamilyCheckin.Utilities.LabelPrinter`.

## Project Layout

```
/org_secc/SportsAndFitness/                Kiosk + guest blocks (.ascx + .cs) and guest-flow Lava partials
/org_secc/SportsAndFitness/ControlCenter/  Staff "control center" blocks (search, guest check-in, PINs, person actions)
/Themes/SportsAndFitness/                  Rock theme — Layouts, Styles (.less), Assets/Lava partials
/Migrations/                               A single hand-run SQL stored proc (not an EF plugin migration)
/Properties/                               AssemblyInfo.cs (the only compiled file)
```

## Components

### Blocks

All blocks are RockWeb-compiled `.ascx` controls. Categories vary (`SECC > Check-in`,
`SECC > Sports and Fitness`, `Sports and Fitness > Control Center`).

| Block (class) | Category | Purpose | Key settings |
|---------------|----------|---------|--------------|
| `ActivitiesCheckin` | SECC > Check-in | Kiosk check-in into the Activities Center; shows member cards with expiration/alert notes. | Checkin Activity, Expiration Date Attribute (group-member) |
| `GroupFitness` | SECC > Check-in | Phone-number kiosk check-in for group-fitness classes; decrements/reads remaining sessions and prints labels. | Process Activity, Checkin Activity, Sessions Attribute Key, Group Fitness Parent Group, Minimum Digits |
| `GuestRegistration` | SECC > Sports and Fitness | Multi-panel guest self-registration: welcome, returning-guest lookup, new-guest form, waiver, emergency contacts, finish; launches a registration workflow. | Guest Registration Workflow, Connection Status, Default Campus, Lava message attributes (welcome/new/finish/waiver/etc.) |
| `ControlCenter/GuestCheckin` | SECC > Sports and Fitness | Staff tool to check in guests against the guest-registration workflow. | Guest Registration Workflow, Workflow Status, Cancel/Checkin Activity Type Name, Checkin Group Type, Maximum Guests |
| `ControlCenter/Search` | Sports and Fitness > Control Center | Search participants by PIN and/or phone number. | Search By PIN, Search By Phone Number, Search Result Page |
| `ControlCenter/SearchResults` | Sports and Fitness > Control Center | Renders the participant result list via Lava. | Person Detail (linked page), Sports and Fitness PIN Purpose (defined value), Results Lava, Lava Commands, Show Debug Panel |
| `ControlCenter/PersonActions` | Sports and Fitness > Control Center | Per-person action panel with links to Sports/Group-Fitness/Childcare history. | Sports & Fitness / Group Fitness / Childcare History pages, Group Fitness Group |
| `ControlCenter/ManageSportsPINs` | Sports and Fitness > Control Center | Manage a person's sign-in PINs (used alongside Person Actions). | (none) |

#### GuestRegistration attribute keys

Keys are defined in a `GuestRegistration.AttributeKeys` constants class. Most message fields are
Lava `CodeEditorField`s rendered through a local `ProcessLava` helper.

| Setting | Type | Notes |
|---------|------|-------|
| **GuestWorkflow** | workflow type, required | Registration workflow activated on finish (`Workflow.Activate`, sets `Guest` + `IsAdult`). |
| **ConnectionStatus** | defined value, required | Connection status stamped on newly created guests. |
| **DefaultCampus** | campus, required | Campus for the new guest's family group. |
| **WelcomeIntro / ExistingGuestMessage / NeweGuestMessage / GuestDetailMessage / ConfirmEmergencyContact / FinishMessage / GuestWaiverText** | Lava code editors | Panel copy for each step (note the misspelled `NeweGuestMessage` key). |
| **LavaCommands** | lava commands | Commands enabled when rendering the above. |

### Lava partials

The guest flow and theme ship `.lava` files rather than registered filters — e.g.
`GuestRegistration_*.lava` (waiver, confirm-info, emergency contacts, finish),
`ControlCenter/PersonDetails.lava`, `ControlCenter/SearchResults.lava`, and the theme's
`Themes/SportsAndFitness/Assets/Lava/*` (calendar/event/group/page templates). These are content,
not C# components.

## Dependencies & Integrations

- **Rock:** `RockContext` and Rock services (`PersonService`, `GroupService`, `GroupMemberService`, `PhoneNumberService`, `AttributeMatrix*Service`), the check-in framework (`CheckInBlock`, `CheckInState`, `ProcessActivity`), Rock workflow engine, attribute framework / attribute matrices, `RockBlock`, Lava (`ResolveMergeFields`).
- **Cross-plugin:** [org.secc.FamilyCheckin](../org.secc.FamilyCheckin/README.md) — `LabelPrinter` for kiosk label printing in `GroupFitness`.
- **Third-party:** none beyond Rock/.NET. Kiosk label printing pulls in `cordova-2.4.0.js` / `ZebraPrint.js` from RockWeb.

## Migrations

There is a `/Migrations/` folder, but it is **not** a Rock EF plugin-migration set — it contains a
single hand-run script, **`sf stored proc.sql`**, that creates
`_org_secc_spSportsAndFitnessMigrate`: a **one-off, hard-coded** stored proc that copies legacy
group members (active / expired / red-flagged card groups and the old group-fitness group) into new
groups by **literal Id** and backfills expiration/session attribute values. It targets `RockDBStaging`
and is not wired into any build or `Rock.Plugin.Migration`.

## Observations

*Noticed while documenting — not a full audit.*

- **Improvement:** `Migrations/sf stored proc.sql` hard-codes database **Ids** (group Ids, attribute Ids) and the `RockDBStaging` database name. It's a point-in-time data move that will not be correct against any other environment — keep it as historical reference, not something to re-run.
- **Security (low):** `GuestRegistration` is a public-facing self-service kiosk block that **creates Person/Family records** and writes a liability-waiver acceptance (with the client IP read from `HTTP_X_FORWARDED_FOR`). It records a `Pending` record status and dedupes against existing people, but confirm the page hosting it is locked to the kiosk and that the trusted-proxy assumption behind `X_FORWARDED_FOR` holds (otherwise the stored "accepted from" IP is client-spoofable).
- **Improvement:** Several blocks `new RockContext()` repeatedly within a single request/handler (e.g. `GuestRegistration` opens many short-lived contexts), and `GetEmergencyContactMatrixTypeId()` dereferences the looked-up attribute without a null check. Works in practice but is fragile if the expected attribute/matrix template is missing.
- **Improvement:** `ActivitiesCheckin`'s membership/notes logic keys off a `ConnectionStatusValueId == Member` or `Person.GetAttributeValue("Employer") == "Southeast Christian Church"` string check (`ActivitiesCheckin.ascx.cs` lines 172 and 329) — brittle business rules embedded in code rather than configuration.

## Making Changes

- Kiosk check-in behavior lives in `org_secc/SportsAndFitness/ActivitiesCheckin.ascx.cs` and `GroupFitness.ascx.cs`; both are `CheckInBlock`s that call `ProcessActivity(...)` against workflow-activity names set in block attributes — change the activity wiring there, and the check-in workflow itself in Rock.
- The guest self-registration flow (panels, waiver, emergency contacts) is `org_secc/SportsAndFitness/GuestRegistration.ascx(.cs)` plus the `GuestRegistration_*.lava` partials; per-step copy is all block attributes (Lava), so wording changes don't need a rebuild.
- Staff tooling is under `org_secc/SportsAndFitness/ControlCenter/`; search rendering is Lava (`SearchResults.lava`, `PersonDetails.lava`).
- Theme/layout/styling changes go in `Themes/SportsAndFitness/`.
- This project is RockWeb-compiled — editing a `.cs` here changes behavior only after the markup is deployed to `RockWeb/Plugins/org_secc/`; the `.csproj` itself only compiles `AssemblyInfo.cs`.
- Label printing relies on [org.secc.FamilyCheckin](../org.secc.FamilyCheckin/README.md)'s `LabelPrinter`.
</content>
</invoke>
