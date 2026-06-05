# org.secc.Event

> Southeast's custom event/registration blocks — calendar Lava, a heavily-customized registration entry/detail pair, QR event passes, SignNow signing, camp-placement import, and the Community Gives Back program.

## Overview

This plugin is a collection of Rock **UI blocks** (`.ascx` user controls) covering Southeast's
public events and registration flows. It includes Lava-driven calendar/event displays, SECC's own
forks of Rock's stock `RegistrationEntry` / `RegistrationDetail` blocks, QR-based "Event Pass"
generation and request, a SignNow digital-signature embed, a CSV camp-placement importer, and the
self-contained "Community Gives Back" school-sponsorship sub-area. Blocks are configured through
Rock block settings and used by ministry/communications staff and public registrants.

## Project Info

- **Project file:** none — **no `.csproj`; RockWeb-compiled.** The `.ascx` controls use
  `CodeFile=` + `Inherits=`, so RockWeb compiles the code-behind at runtime; there is no built
  assembly for this plugin.
- **Root namespace:** `RockWeb.Plugins.org_secc.Event` (calendar blocks use `RockWeb.Blocks.Event`;
  Community Gives Back uses `RockWeb.Plugins.org_secc.CommunityGivesBack`)
- **Target framework:** n/a (compiled in-place by RockWeb, .NET Framework 4.7.2)
- **Deploys to:** `RockWeb/Plugins/org_secc/Event/` (`.ascx` markup + `.ascx.cs` code-behind)

## Project Layout

```
/org_secc/Event/                     Event & registration blocks (.ascx + .ascx.cs)
/org_secc/Event/CommunityGivesBack/  Community Gives Back program blocks
```

## Components

### Blocks

Category in Rock: **SECC > Event** (Community Gives Back blocks: **SECC > Community Gives Back**).

| Block (DisplayName) | Purpose | Key settings |
|---------------------|---------|--------------|
| Yearly Calendar Lava (`CalendarLava`) | Renders an event calendar (Day/Week/Month/Year) via Lava, with campus/audience filters. | Event Calendar, Default View Option, Lava Template, campus/audience filter modes, Cache Duration, Cache Tags |
| Calendar Event Item Lava (`EventItemLava`) | Renders a single calendar event item via Lava, optionally resolved by URL slug. | EventItem, Lava Template, URL Slug Attribute, Set Page Title, Event Not Found template |
| Event Redirect (`EventRedirect`) | Interprets event slugs and forwards to the right page. | Base Route (default `upcomingevents`) |
| Registration Entry (`RegistrationEntry`) | SECC fork of Rock's stock registration-entry block (register for an instance). | Connection Status, Record Status, plus the stock registration-entry settings |
| Registration Detail (`RegistrationDetail`) | SECC fork of Rock's registration-detail block (view/manage a registration). | (stock registration-detail settings) |
| Event Pass (`EventPass`) | Displays a QR "Event Pass" for registrants of an event. | Include Registrants on Waitlist, Pass Not Found Header/Message |
| Generate Event Pass (`RequestEventPass`) | Public form that launches a workflow to request an Event Pass. | Block Title, Event Pass Workflow Type, Pass Person Attribute, Contact Fields Editable |
| Family Registration List Lava (`FamilyRegistrationLava`) | Lava sidebar listing a family's children's registrations. | Lava Template, Max Results, Date Range, Limit To Owed |
| SignNow (`SignNow`) | Embedded sub-control (a plain `System.Web.UI.UserControl`, **not** a registerable Rock block — no `[DisplayName]`/`[Category]`) that hosts the SignNow digital-signature flow inside registration; parses the returned `document_id` (falls back to the referer on iOS). | (none — driven by the registration's digital-signature component) |
| Camp Placement Import (`CampPlacementImport`) | Imports a CSV to place camp registrants into placement groups; queues a background import run. | Default Group Member Status, Batch Size |
| Community Gives Back Registration (`CommunityGivesBackRegistration`) | Public registration for the Community Gives Back program (launches a workflow on submit, with an acknowledgement step). | Community Gives Back Schools (defined type), Acknowledgement Text, Confirmation Text, Registration Complete Text, Registration Workflow Type, Auto Populate Registration Data, Campaign |
| Community Gives Back Registrations List (`CommunityGivesBackRegistrations`) | Lists CGB registrations by school. | Community Gives Back Schools (defined type), Registration Workflow Type, School List Page |
| Community Gives Back School List (`SchoolList`) | Lists CGB schools / sponsorship registrations. | Community Gives Back Schools (defined type), Registration Workflow Type, School Registration Page |

`RegistrationEntry` (6,091 lines) and `RegistrationDetail` (2,799 lines) are large SECC-maintained
copies of the corresponding stock Rock blocks; treat them as forks kept in sync with upstream.

## Dependencies & Integrations

- **Rock:** `RockBlock` / `RockContext`, event-calendar & registration models
  (`EventItem`, `RegistrationInstance`, `RegistrationRegistrant`, `PersonSearchKey`), Lava, the
  workflow engine (Generate Event Pass), defined types (Community Gives Back schools).
- **Cross-plugin:**
  [org.secc.PersonMatch](../org.secc.PersonMatch/README.md) — used by `SignNow` and
  `RegistrationEntry`.
  [org.secc.Jobs](../org.secc.Jobs/README.md) — `CampPlacementImport` queues and runs
  `org.secc.Jobs.Event.CampPlacementImportRunner` and reads its `ImportRunStatus`.
  [org.secc.QRManager](../org.secc.QRManager/README.md) — `EventPass` builds QR URLs against the
  same `GetQRCode.ashx` handler / person search-key codes.
- **Third-party:** SignNow (digital-signature provider, via Rock's `DigitalSignatureComponent`);
  Newtonsoft.Json (registration serialization).

## Observations

*Noticed while documenting — not a full audit.*

- **Security (low):** `EventPass` builds a public QR URL of the form
  `…/GetQRCode.ashx?data={alternateId}` using each registrant's **person search-key** (alternate id),
  read-only. The pass surfaces registrant identity for an event instance — confirm the page hosting
  this block is access-controlled and that "Include Registrants on Waitlist" is intentional where set.
- **Improvement:** `SignNow` recovers a missing `document_id` by regex-parsing
  `Request.UrlReferrer` (an iOS workaround, per the inline comment). Referer is client-supplied and
  can be absent/spoofed; the block already returns early when it can't recover the id, but this path
  is worth keeping an eye on as SignNow's behavior changes.
- **Improvement:** `RegistrationEntry` / `RegistrationDetail` are large forks of stock Rock blocks.
  When upgrading Rock, diff these against the upstream versions so SECC customizations aren't lost
  and upstream fixes are picked up.

## Making Changes

- Block markup and behavior live together in `org_secc/Event/<Block>.ascx` + `.ascx.cs`; because
  there is no `.csproj`, edits compile when RockWeb next loads the control — no separate build step.
- Camp-placement import is split: this block (`CampPlacementImport`) handles upload/queueing, but the
  actual placement work runs in [org.secc.Jobs](../org.secc.Jobs/README.md)
  (`Event.CampPlacementImportRunner` / `CampPlacementImportBackgroundJob`); change the import logic there.
- QR/pass URL conventions are shared with [org.secc.QRManager](../org.secc.QRManager/README.md);
  person-matching for SignNow / Community Gives Back lives in
  [org.secc.PersonMatch](../org.secc.PersonMatch/README.md).
