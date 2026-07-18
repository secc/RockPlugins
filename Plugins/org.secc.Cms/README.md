# org.secc.Cms

> A grab-bag of Southeast's custom Rock CMS blocks — login/profile UI, section/topper page chrome, content-channel and Twitter Lava, QR-code shortlinks, and cache/Font-Awesome admin tools — plus a styled `SectionZone` control.

## Overview

Cms is Southeast's collection of custom Rock **CMS blocks** used to build public-facing pages:
login and account-management blocks, page-layout chrome (section headers, parallax topper), a
timed content-channel renderer, a Twitter timeline block, a QR-code/shortlink generator, and a few
admin utilities (cache-tag clearing, Font Awesome config). Most of the work lives in `.ascx`/`.ascx.cs`
blocks that Rock compiles at runtime; the compiled assembly itself ships only the `SectionZone` Zone
control and a single migration.

## Project Info

- **Project file:** `org.secc.Cms.csproj`
- **Root namespace:** `org.secc.Cms`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly) and `RockWeb/Plugins/org_secc/` (block `.ascx` markup) — see the `PostBuildEvent` `xcopy`
- **Build note:** Only `SectionZone.cs`, `Migrations/001_Init.cs`, and `AssemblyInfo.cs` are in the
  `.csproj` `<Compile>` list. The `org_secc/Cms/*.ascx.cs` blocks are **RockWeb-compiled at runtime**,
  not part of the assembly.

## Project Layout

```
SectionZone.cs        Custom Rock Zone that wraps its contents in a styled <section> (reads a Section Header block's SectionType class)
/org_secc/Cms/        UI blocks (.ascx + .ascx.cs) — login, profile, layout chrome, content, QR, admin tools
/Migrations/          Single Rock plugin migration (adds HtmlContent.Name column)
```

## Components

### Blocks

Category in Rock: **SECC > CMS** (except where noted).

| Block (class) | Category | Purpose |
|---------------|----------|---------|
| `LoginJS` | SECC > CMS | Builds the login / user menu via JavaScript. |
| `LoginStatus` | SECC > Security | Shows the logged-in user's name with log-in/out + account links; optional new-workflow/connection notifications. |
| `PublicProfileEdit` | SECC > CMS | Public block for users to view/edit their own (and family members') profile, addresses, phones, and attributes. |
| `SectionHeader` | SECC > CMS | Renders a styled section header; its `SectionType` defined value drives the CSS class used by `SectionZone`. |
| `Topper` | SECC > CMS | Parallax header/title with an optional info panel (`RockBlockCustomSettings`). |
| `TimedContentChannel` | SECC > CMS | Renders a content channel's items through a Lava template with a per-item timer and cache window. |
| `TwitterLava` | SECC > CMS | Fetches a Twitter user timeline (app-only OAuth2) and renders it via Lava. |
| `ContactGroupLeaders` | SECC > CMS | Public form that emails (or SMS-es) a small group's leaders; optional communication-history record. |
| `QRCodeGenerator` | SECC > CMS | Creates a `PageShortLink` from a URL and returns a QR code (via `GetQRCode.ashx`). |
| `LinkListEditUsers` | SECC > CRM | Add/remove the people allowed to edit a given link-list content channel; manages a per-list security role group. |
| `CacheTagsCheckList` | SECC > CMS | Admin block: checkboxes of cache tags to clear/expire as a group. |
| `FontAwesomeSettings` | SECC > CMS | Admin block to configure Font Awesome. |

Blocks are configured through standard Rock block attributes. Notable settings:

- **ContactGroupLeaders** — `LeaderGroup` (default contact group), `DefaultRecipient`, `Subject`,
  `Message Body`/`Response Message` (Lava), `Enable SMS` + `SMS From Number` + `SMS Message Body`,
  `Save Communication History`, `Enabled Lava Commands`.
- **PublicProfileEdit** — `Show Family Members`, `Address Type`, `Phone Numbers`, family/adult/child
  `AttributeField`s, `Connection Status` for new people.
- **LoginStatus** — `Logged In Page List` (key/value, Lava links), `Enable Notifications`,
  `ConnectionRequestMaxDays`, `WorkflowActivityMaxDays`.
- **TimedContentChannel** — `Content Channel`, `Timer Attribute Key`, `Maximum Cache` (seconds),
  `Lava`, `Enabled Lava Commands`.
- **QRCodeGenerator** — `SiteField` (for the shortlink token), minimum-token-length `IntegerField`,
  a `FileField`, and a URL `TextField`.
- **CacheTagsCheckList** — `Cache Tags` (`DefinedValueField` on the Cache Tags defined type, multi).

### Controls

| Type | Purpose |
|------|---------|
| `SectionZone` (`: Rock.Web.UI.Controls.Zone`) | A Rock Zone that renders its block content inside a `<section>` whose CSS class comes from the contained **Section Header** block's `SectionType` defined value (`ClassName` attribute). |

## Dependencies & Integrations

- **Rock:** `RockBlock` / `RockBlockCustomSettings`, the Rock Zone/UI framework, `RockContext` and
  Rock services (`PageShortLinkService`, `GroupService`, `AuthService`, communication services),
  defined types/values, attribute framework, `Rock.Plugin` migrations.
- **Third-party APIs:** Twitter (`api.twitter.com` — app-only OAuth2 bearer token via the
  `/oauth2/token` endpoint, then `1.1/statuses/user_timeline.json`) in `TwitterLava`.
- **Other:** Newtonsoft.Json, EntityFramework 6, DotLiquid (Lava). QR rendering is delegated to
  Rock's `GetQRCode.ashx` handler, not generated in-plugin.
- **Cross-plugin:** none required at build time. See [org.secc.QRManager](../org.secc.QRManager/README.md)
  for the other (person-centric) QR path.

## Migrations

Ships a single Rock plugin migration under `/Migrations/`:

- `001_Init` (`MigrationNumber 1`, `1.13.0`) — adds a nullable `Name NVARCHAR(100)` column to the
  core `HtmlContent` table (idempotent; guarded by an `INFORMATION_SCHEMA` check).

## Observations

*Noticed while documenting — not a full audit.*

- **Security (review):** `ContactGroupLeaders` and `PublicProfileEdit` are **public-facing** blocks
  that send communications / edit person + family data. Confirm anti-abuse expectations (who can
  email a group's leaders, rate limiting) and that profile edits are scoped to the current user's
  own family. `LinkListEditUsers` creates/edits **security role groups** and `Auth` rows — make sure
  that block is restricted to trusted staff.
- **Improvement:** `TwitterLava` reaches an external API (`api.twitter.com` v1.1) from inside a CMS
  block. The v1.1 `statuses/user_timeline` endpoint has been deprecated/retired by X/Twitter, so this
  block may no longer return data — worth confirming it still works or retiring it.
- **Improvement:** `SectionZone.RenderControl` walks the control tree matching block type names with
  `GetType().ToString().Contains("org_secc_cms_sectionheader")` (string matching on the runtime-compiled
  type name). This is fragile to renames/namespace changes; a type/interface check would be sturdier.
- **Improvement:** `AssemblyInfo.cs` still carries the scaffolded `AssemblyProduct "Cms"` and
  `Copyright © 2016` with an empty `AssemblyCompany` rather than Southeast Christian Church.

## Making Changes

- To change a block's behavior or markup, edit the matching `*.ascx` / `*.ascx.cs` in
  `org_secc/Cms/`; these are runtime-compiled by RockWeb (no rebuild of the plugin assembly needed),
  but the `PostBuildEvent` `xcopy` is what stages them into `RockWeb/Plugins/org_secc/`.
- Section styling is split between `SectionHeader` (sets the `SectionType` defined value) and
  `SectionZone.cs` (reads it to pick the `<section>` CSS class) — change both together. `SectionZone`
  is in the compiled assembly, so editing it requires a rebuild.
- New schema/data belongs in a new numbered migration under `/Migrations/` — don't hand-edit
  `001_Init`, which has already run.
- Related: [org.secc.QRManager](../org.secc.QRManager/README.md) (person QR/Fast-Pass filters and
  endpoint).
