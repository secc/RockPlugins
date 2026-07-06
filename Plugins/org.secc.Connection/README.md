# org.secc.Connection

> Public-facing volunteer signup blocks that turn Rock connection opportunities into configurable, partition-driven signup pages.

## Overview

This is Southeast's volunteer "Signup Wizard" plugin (packaged as `SignupWizard.plugin`). It
provides Rock blocks that let a person browse and sign up for connection opportunities from the
public website: a wizard block that renders a configurable, multi-partition signup grid (campus /
defined-type / role / schedule) via Lava, and a form block that captures the registrant, matches or
creates the person, and writes a `ConnectionRequest`. A third block (Opportunity Search) is a Rock
opportunity-search variant kept in source but excluded from the packaged plugin.

## Project Info

- **Packaged plugin:** `SignupWizard.plugin` (built by `BuildPlugin.ps1`, zipped via 7-Zip)
- **Root namespace:** `org.secc.Connection`
- **Build model:** No `.csproj` / compiled assembly — block code-behind (`.ascx.cs`) compiles in
  RockWeb; the plugin ships markup + Lava only.
- **Deploys to:** `RockWeb/Plugins/org_secc/Connection/` (block `.ascx`/`.ascx.cs`, Lava templates,
  CSS)

## Project Layout

```
/org_secc/Connection/
  VolunteerSignupWizard.ascx(.cs)            Configurable signup-grid block (RockBlockCustomSettings)
  VolunteerSignupFormConnections.ascx(.cs)   Registrant form → person match/create → ConnectionRequest
  ConnectionOpportunitySearch.ascx(.cs)      Opportunity search block (excluded from the .plugin package)
  VolunteerSignupWizard.css                  Styles for the card layouts
  VolunteerGenius.lava / VolunteerGenius/    "Genius" table-style signup output
  CardPage.lava       / CardPage/            Single-page card-panel output
  CardWizard.lava     / CardWizard/          Animated left-to-right card output (Single/Multiple mode)
BuildPlugin.ps1                              Packages /org_secc into SignupWizard.plugin
```

## Components

### Blocks

Category in Rock: **SECC > Connection**.

| Block | Type | Purpose |
|-------|------|---------|
| Volunteer Signup Wizard | `RockBlockCustomSettings` | Renders a configurable signup grid from a set of partitions (campus / defined type / role / schedule) and a selectable Lava output template. |
| Volunteer Signup Form - Connections | `RockBlock` | Collects a registrant, matches or creates the person, and creates a `ConnectionRequest` for the chosen opportunity. |
| Connection Opportunity Search | `RockBlock` | Lists active opportunities for a configured connection type with name/campus/attribute filters. Present in source but removed by `BuildPlugin.ps1`, so not shipped in the `.plugin`. |

### Block Settings

**Volunteer Signup Wizard** — settings are edited through the block's custom-settings panel:

| Setting | Type | Notes |
|---------|------|-------|
| **Settings** | code (JSON) | Serialized `SignupSettings` (partitions, entity type/guid, signup page). |
| **Counts** / **Groups** | key-value list | Count and group maps used to build the partition tree. |
| **Lava** | code | Output template; defaults to a switch that `include`s `VolunteerGenius`, `CardPage`, or `CardWizard`. |
| **Enable Debug** | bool (default false) | Show available Lava merge fields. |

**Volunteer Signup Form - Connections** — standard block attributes:

| Setting | Type | Notes |
|---------|------|-------|
| Display Phone / Birthdate / Comments | bool | Toggle form fields. |
| **Connect Button Text** | text (default "Connect") | Submit button label. Note: as of ROCK-8710 the primary submit button label is hardcoded to "I Agree & Connect" in code, so this setting is currently not applied to the primary button. |
| **Lava Template** | code (Lava) | Response message; default `OpportunityResponseMessage.lava`. |
| Enable Campus Context | bool (default true) | Use page campus context as a filter. |
| **Connection Status** | defined value | Status for newly created individuals (default Web Prospect). |
| **Record Status** | defined value | Record status for newly created individuals (default Pending). |
| **UrlKeys** / **FormKeys** | text (CSV) | Group-member attribute keys set from the URL / shown as edit controls. |
| Display Add Another | bool (default false) | Show "Connect and Add Another". |
| Comment label text / Comments Required | text / bool | Comment box label and requirement. |

**Connection Opportunity Search** — `Lava Template`, `Enable Debug`, `Enable Campus Context`,
`Set Page Title`, `Display Name/Campus/Attribute Filters`, `Detail Page` (linked page),
`Connection Type Id` (integer).

### Lava Output Templates

The wizard's `Lava` setting selects one of three prebuilt outputs. Each delegates row rendering to
a `Partition.lava` partial in a matching folder; `CardPage/` and `CardWizard/` additionally carry
per-partition-type partials (`CardCampus`, `CardDefinedType`, `CardRole`, `CardSchedule`), while
`VolunteerGenius/` has only `Partition.lava`.

| Template | Output style |
|----------|--------------|
| `VolunteerGenius.lava` | Structured table, similar to common signup-genius tools. |
| `CardPage.lava` | Single page of card panels (best for 1–2 partitions). |
| `CardWizard.lava` | Animated left-to-right cards; `CardWizardMode` `Single` or `Multiple` (default `Single`). |

## Dependencies & Integrations

- **Rock:** `RockContext`, connection model (`ConnectionRequest`, `ConnectionOpportunity`,
  `ConnectionType`), `PersonService` (`FindPersons`, `SaveNewPerson`), `GroupService`,
  `GroupTypeRoleService`, `ScheduleService`, defined types/values, `HistoryService`, the Rock
  block/Lava framework, campus context.
- **Cross-plugin:** [org.secc.PersonMatch](../org.secc.PersonMatch/README.md) — the form loads it
  **reflectively** (`org.secc.PersonMatch.Extension.GetByMatch`) for person matching, falling back
  to `PersonService.FindPersons` when the assembly isn't present.
- No third-party APIs.

## Observations

*Noticed while documenting — not a full audit.*

- **Security (medium):** *Volunteer Signup Form - Connections* is a public-facing block that, on
  submit, will **create a new `Person` and a `ConnectionRequest`** when no match is found
  (`PersonService.SaveNewPerson` at `VolunteerSignupFormConnections.ascx.cs:321`). Person matching
  uses name + email/birthdate, so unauthenticated form posts can both enumerate-by-side-effect and
  add records. Confirm the page is intended for anonymous use, and consider spam/rate protection
  (e.g. CAPTCHA) — worth confirming.
- **Security (low):** The form sets group-member attributes from URL parameters via the `UrlKeys`
  setting (`PageParameter(urlKey)`). Only keys the admin lists are honored, but those values are
  attacker-controllable in the link; review which attributes are exposed this way.
- **Improvement:** Several services are constructed with their own `new RockContext()` inline
  (e.g. `GroupTypeRoleService`, `ScheduleService`, `DefinedTypeService`) rather than reusing one
  per request, so a single render/bind can spin up multiple contexts. Acceptable, but tidy-able.
- **Improvement:** `ConnectionOpportunitySearch` is shipped in source but explicitly deleted by
  `BuildPlugin.ps1` before packaging. If it's genuinely unused it could be removed; if it's meant
  to ship, the build script's `rm` line is hiding it. Worth confirming intent.

## Making Changes

- To change the signup grid's appearance, edit the Lava in `VolunteerGenius/`, `CardPage/`, or
  `CardWizard/` (the wizard's `Lava` block setting chooses which one is `include`d); copy a template
  rather than editing the defaults, as the in-code default comment advises.
- To change registrant capture, person matching, or `ConnectionRequest` creation, edit
  `VolunteerSignupFormConnections.ascx.cs` (`btnConnect_Click`).
- Person-matching behavior lives in [org.secc.PersonMatch](../org.secc.PersonMatch/README.md); this
  plugin only calls into it reflectively.
- Re-package with `BuildPlugin.ps1` (Windows + 7-Zip) — note it intentionally excludes
  `ConnectionOpportunitySearch.ascx*` from the `.plugin`.
