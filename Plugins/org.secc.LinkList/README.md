# org.secc.LinkList

> A "link-in-bio" style Link List system on Rock content channels — Obsidian management/viewer blocks, a multi-slug URL scheme, per-list security-group edit access, an embeddable `<secc-link-list>` web component, and click/view analytics via Rock Interactions.

## Overview

LinkList lets staff build and theme shareable link pages (à la Linktree): each list is a
**ContentChannelItem** in a dedicated "Link Lists" content channel, its link rows live in an
**Attribute Matrix**, and its colors/header media are item attributes with reusable "design
preset" defined values. The plugin ships three **Obsidian** blocks (management grid, editor, and
slug-routed viewer), an anonymous **REST endpoint + web component** so lists can be embedded on
external sites (Webflow, se.church), and an **analytics** layer that records views and link
clicks as Rock Interactions with per-IP rate limiting.

Two subsystems deserve special mention:

- **Slug system** — a list is addressed by one or more URL slugs (`ContentChannelItemSlug`
  rows, one flagged primary). `LinkListService` is the single source of truth for slug rules:
  `NormalizeSlug` (read path), `CanonicalizeSlug` (write path — a deliberate *fixed point* of
  Rock's internal `MakeSlugValid`, so the text validated is exactly the text stored), and
  `IsValidSlug`. Saves run a full reconcile (`SlugReconciler`): explicit conflict checks
  (Rock's `SaveSlug` silently suffixes `-1` instead of erroring), deferred deletions (a slug's
  public URL is only removed after everything else succeeds), and concurrency handling so two
  editors can't resurrect or clobber each other's slugs (`loadedSlugIds`).
- **Manage Access model** — each list lazily gets its **own security role group** named
  `RSR - Link List - {title}` (`LinkListGuids.SecurityGroupNamePrefix`, clamped to the 100-char
  `Group.Name` limit). `EnsureSecurityGroup` creates the group on first save, adds the creator
  as a member, and grants the group **EDIT on the ContentChannelItem** via an `Auth` rule
  (`Authorization.AllowSecurityRole`). The editor's "Manage Access" panel adds/removes group
  members (`AddMember`/`RemoveMember` — the last active member can never be removed), and
  renames the group when the list's title changes. All block-action authorization then flows
  through Rock's standard `item.IsAuthorized( EDIT/ADMINISTRATE )`. **Creating** a list needs
  no channel permission (ROCK-9100): any signed-in person who can view the management/detail
  pages may create one and becomes its editor — control *who can create* with page/block
  security. Channel **EDIT** is not used as a gate because it cascades to every item (it
  would make every holder an editor of every list); channel **ADMINISTRATE** still gates the
  admin tabs (global header/footer, design presets, allowed origins).

## Project Info

- **Projects:** `org.secc.LinkList` (C# plugin), `org.secc.LinkList.Obsidian` (Vue/TypeScript
  frontend), `org.secc.LinkList.Tests` (xunit)
- **Root namespace:** `org.secc.LinkList`
- **Target framework:** .NET Framework 4.7.2 (SDK-style csproj; `RockRMS.Rock` /
  `Rock.Blocks` / `Rock.Rest` NuGet packages, `1.16.9-rc.1`)
- **Deploys to:** `RockWeb/Bin/` (assembly — a custom `CopyPluginToRockWebBin` MSBuild target,
  since the Spark build task no-ops without a rock-dev-tool layout) and
  `RockWeb/Plugins/org_secc/LinkList/` (built Obsidian JS — `npm run deploy` in the
  `.Obsidian` project; a solution rebuild does **not** copy the JS)
- **Packaging:** `plugin.json` + `BuildPlugin.ps1` bundle the Release DLL and the Obsidian
  `dist/**` output into a Rock plugin package.

## Project Layout

```
org.secc.LinkList/            C# plugin assembly
  Blocks/                     Three Obsidian block types (List, Detail, Viewer)
  Services/                   LinkListService (bags, slugs, security groups), LinkListInteractionService (analytics)
  Rest/Controllers/           Public anonymous REST endpoint (GET list + click beacon) with CORS allowlist
  Migrations/                 001-011 Rock plugin migrations (channel/type/matrix, design presets, analytics channel, ...)
  SystemGuids/                LinkListGuids — every well-known GUID and attribute key
  Utility/                    SlugReconciler, AnalyticsSeries, ClickPayload, ClientIpResolver, InteractionRateLimiter, LinkListHtmlSanitizer
  ViewModels/                 Bags/boxes shared with the Obsidian frontend (LinkListBag, GroupMemberBag, ...)
org.secc.LinkList.Obsidian/   Vue 3 / Obsidian frontend (npm build; jest tests in /tests)
  src/linkListDetail.obs      Editor block entry (hosts components/linkListEditor.obs)
  src/linkListList.obs        Management grid entry
  src/linkListViewer.obs      Viewer block entry (renders via the web component)
  src/webComponents/linkList.ts  <secc-link-list> shadow-DOM renderer (in-Rock + external embeds)
org.secc.LinkList.Tests/      xunit static-logic tests (no RockContext)
plugin.json / BuildPlugin.ps1 Rock plugin packaging
```

## Components

### Blocks (Obsidian)

Category in Rock: **SECC > Link Lists**.

| Block (class) | Obsidian file | Purpose |
|---------------|---------------|---------|
| `LinkListListBlock` | `linkListList.obs` | Management grid of the lists the person can edit (admins see all — `Admin Security Role(s)` block setting); create/edit/delete navigation, global settings (header/footer, design presets, CORS origins). |
| `LinkListDetailBlock` | `linkListDetail.obs` → `components/linkListEditor.obs` | Create/edit a single list. Panels: Details (title, slugs), Links, Footer content, Theme colors, Header & Media, Manage Access, Analytics. Block actions: `GetListDetail`, `SaveList`, `DeleteList`, `GetDesigns`, `GetAnalytics`, `AddMember`, `RemoveMember`. |
| `LinkListViewerBlock` | `linkListViewer.obs` | Public in-Rock display, slug-routed (route param `slug`, or a fixed `Manual Slug`); unknown slugs redirect to a configurable Not Found page (legacy default `/page/255`). |

### REST endpoints

`LinkListController`, route prefix `api/secc/linklist` — **anonymous by design**, gated by the
per-list `IsPublic` attribute (non-public lists return 404, not 403, to avoid enumeration).
CORS is origin-reflection against an admin-managed Defined Type unioned with a hardcoded
fallback list; JSON is forced camelCase to match the Obsidian bag contract.

| Route | Verb | Purpose |
|-------|------|---------|
| `api/secc/linklist/{idOrSlug}` | GET | Returns the full list bag for the web component; records a "View" interaction (rate-limited per IP). |
| `api/secc/linklist/{idOrSlug}/click` | POST | `navigator.sendBeacon` click target. Always answers 200 (except a 413 body-size cap); validates the matrix-row guid server-side and records a "Click" interaction, rate-limited per IP (accept-but-drop). |

### Services & utilities

| Type | Purpose |
|------|---------|
| `LinkListService` | The core service: channel/matrix lookups, `ResolveItem` (guid → slug → id, slug deliberately before id), bag building for viewer/editor/REST, matrix-row persistence, design presets, CORS origins, global header/footer, and the whole security-group model (`EnsureSecurityGroup`, `RenameSecurityGroup`, `GetPrimarySecurityGroup`, `GetMembers`, `AddMember`, `RemoveMember`, `GetOtherEditAccess`). |
| `LinkListInteractionService` | Records views/clicks as Rock Interactions on the "Link Lists" InteractionChannel (one component per list, queued via `InteractionTransaction`); charges the per-IP rate limit at this single choke point; fail-safe (analytics never break a page view). |
| `SlugReconciler` | Pure static slug logic: builds/validates submissions, diffs existing vs submitted rows into add/delete plans, and filters concurrent edits (`FilterConcurrentlyDeleted`, `FilterDeletableIds`). |
| `AnalyticsSeries` / `ClickPayload` / `ClientIpResolver` / `InteractionRateLimiter` | Day-series gap filling and range clamping; bounded text/plain beacon parsing; X-Forwarded-For-aware, DNS-free IP resolution; token-bucket rate limiting. |
| `LinkListHtmlSanitizer` | HtmlAgilityPack-based sanitizer for admin-entered header/footer HTML. |

### Web component

`<secc-link-list>` (`src/webComponents/linkList.ts`, bundled by rollup to
`linkList.webcomponent.js`) is the single renderer used both externally (fetches a list by
slug from the REST endpoint) and in-Rock (the Viewer block hands it a pre-fetched bag). The
entire design — global header → hero → featured button → intro → accordion sections → footer —
ships inside a shadow root with no host-CSS dependency; it also sends the click beacons.

### Migrations

Eleven Rock plugin migrations (`MigrationNumber` 1-11, `1.16.0`): 001 creates the content
channel type/channel/matrix template with the **production GUIDs** and legacy attributes
(idempotent upserts); 002-010 add matrix-row attributes (item type, indent, subtitle,
featured), design-preset Defined Type/Values, the CORS-origins Defined Type, header media, the
global header/footer channel attributes, and the title color; 011 creates the "Link Lists"
Interaction medium/channel for analytics.

## Dependencies & Integrations

- **Rock:** `RockBlockType` (Obsidian blocks), ContentChannel/Item/Slug services, Attribute
  Matrix, `Authorization`/`AuthService` (per-list `Auth` rules), Group/GroupMember services
  (security role groups), Interactions (`InteractionTransaction`, channel/component caches),
  Defined Types/Values, `Rock.Plugin` migrations, BinaryFile (header images).
- **NuGet:** `RockRMS.Rock` / `Rock.Blocks` / `Rock.Rest` `1.16.9-rc.1`,
  `Microsoft.AspNet.WebApi.Core`; `DotLiquid` and `HtmlAgilityPack` referenced from
  `RockWeb\Bin` with `Private=false` (compile against Rock's copy, never ship it).
- **Frontend:** `@rockrms/obsidian-framework`, Vue 3, chart.js (analytics panel), rollup (web
  component bundle), jest (frontend tests). External fonts: Google Fonts always; the Adobe
  IvyJournal kit only when the org-wide toggle is on (licensed domains).
- **Cross-plugin:** none at build time. The legacy WebForms `LinkListEditUsers` block lives in
  [org.secc.Cms](../org.secc.Cms/README.md) and manages the same per-list security groups.

## Tests

- `org.secc.LinkList.Tests` — xunit over the **static** logic only (no RockContext):
  `SlugValidationTests`, `SlugReconciliationTests`, `SecurityGroupNameTests`,
  `ClickPayloadTests`, `ClickBodyLimitTests`, `ClientIpResolverTests`,
  `InteractionRateLimiterTests`, `OriginValidationTests`, `AnalyticsSeriesTests`,
  `LinkListHtmlSanitizerTests`.
- `org.secc.LinkList.Obsidian/tests` — jest specs for the web component (rendering, clicks,
  behavior), slug canonicalizer parity, item grouping, and theming.

## Observations

*Noticed while documenting — not a full audit.*

- **Security (accepted design):** the REST endpoints are anonymous and the X-Forwarded-For
  value is trusted without a proxy allowlist, so the per-IP rate limit is client-spoofable —
  documented in-code as accepted residual risk (the limiter bounds its own keyspace and only
  gates analytics writes, never the page data).
- **Security (review):** `GET api/secc/linklist/{idOrSlug}` builds the full bag *before* the
  rate-limit check, so a flood still pays the database cost of bag building (noted in-code:
  the limit gates only the analytics write).
- **Coupling:** `GetPrimarySecurityGroup` identifies the auto-created group by the
  `RSR - Link List - ` **name prefix** (falling back to the first group with an EDIT rule).
  Renaming a group by hand in Rock's UI can therefore detach it from the list, and the shared
  prefix constant matters to both this plugin and the legacy Cms `LinkListEditUsers` block.
- **Frontend deploy gotcha:** rebuilding the solution copies only the DLL; the Obsidian JS
  reaches RockWeb only via `npm run deploy` (which `xcopy`s to a sibling `Rock` checkout — the
  relative path assumes the standard repo layout).
- **Slug canonicalization** intentionally *diverges* from Rock's `MakeSlugValid` order (dash
  collapse after charset strip) to be a fixed point — if Rock ever changes `MakeSlugValid`,
  `CanonicalizeSlug` and its tests must be re-verified.

## Making Changes

- **Backend:** edit the C# project and rebuild — the `CopyPluginToRockWebBin` target drops the
  DLL into `RockWeb\Bin`. New schema/attributes belong in a **new numbered migration**; never
  change the GUIDs in `LinkListGuids` (many match production data that predates the plugin).
- **Frontend:** edit `src/**` in `org.secc.LinkList.Obsidian`, then `npm run deploy` (builds
  via `obsidian-build` + rollup and copies `dist` into RockWeb; hard-refresh the browser).
  Run `npm test` for the jest suites. Regenerate viewmodels with `npm run viewmodels` after
  changing bag classes.
- **Slug rules** live only in `LinkListService` (canonicalize/normalize/validate) and
  `SlugReconciler` (diff/concurrency) — change them there and update
  `SlugValidationTests`/`SlugReconciliationTests` plus the frontend `slugCanonicalizer.ts`,
  which mirrors the server rules.
- **Access model:** anything touching edit permissions should go through
  `EnsureSecurityGroup`/`AddMember`/`RemoveMember` so the Auth rule, group naming, and cache
  flush behavior stay consistent; the Obsidian PersonPicker sends a **PersonAlias guid**, which
  `AddMember` resolves via `PersonAliasService.GetPerson` (with a `Person.Guid` fallback).
- **Packaging:** `BuildPlugin.ps1` + `plugin.json` produce the installable package (Release
  DLL + `dist/**`).
- Related: [org.secc.Cms](../org.secc.Cms/README.md) (legacy `LinkListEditUsers` block over the
  same security groups).

Last updated: 2026-09-02
