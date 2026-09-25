# ROCK-9086 Phase 2 — database-hosted Lava, Fluid compatibility

Targeted `UPDATE` scripts that make the Lava stored in the Rock database run under the Fluid
engine (Rock v13+, mandatory at v17) while staying valid under DotLiquid. Companion to the
repo-side changes in PR #304.

## What is changed

54 live rows, 56 statements, all listed with exact before/after text in [`rows.md`](rows.md):

| Rule | Rows | Change |
|---|---|---|
| `&&` / `\|\|` inside `{% if %}` | 19 | → `and` / `or` (GroupFinderMap `MapInfo` block settings, workflow form headers, HTML blocks, defined-value templates) |
| two-arg `Sort:'Order','Asc'` / `Sort:'X','desc'` | 9 | → `OrderBy:'Order'` / `OrderBy:'X desc'` (silent no-op under Fluid) |
| `Sort:''` on split strings | 3 | → bare `Sort` (silent no-op under Fluid) |
| inline `\d` / `\w` regex | 13 | → `[0-9]` / `[A-Za-z0-9_]` character classes (Fluid parse error otherwise) |
| variable `6MthsAgo` | 13 | → `sixMthsAgo`, every usage |
| nested `{% comment %}` | 1 | strip inner comment tags, re-wrap once (guarded on the include anchor and on 2+ openers, so drift is skipped and re-runs are no-ops) |
| `{% else- %}` in the parallax shortcode | 1 | → `{% else -%}` (Fluid fails the whole shortcode, so every `{[ parallax ]}` page renders truncated) |
| SEOnlineMap feed guard `lng != '' and lng != ''` | 1 | → `lat != '' and lng != ''` (same behaviour fix as the theme copy in PR #304) |

Every statement is a `REPLACE()` of one exact Lava tag, guarded by `CHARINDEX(old) > 0` and,
for HtmlContent, `Version = MAX(Version)` for that block. Rows edited in Rock after the export
are therefore skipped, never clobbered, and show up as `Rows = 0` in the log.

Two `Rows = 0` are expected and benign:

- **Attribute 12442** (GroupFinderMap `MapInfo` default) — Rock rewrites block-attribute defaults
  from the plugin source on startup, so the PR #304 code deploy already made this change.
- **HtmlContent 3386** — the export listed version 47 of block 5114; the current version (68) has
  neither the old `Sort` nor `OrderBy`, so the `MAX(Version)` guard skips it.

## Pass 2 — rows found by the Fluid-verification run (`005_apply_fluidpass.sql`)

With `002` applied on dev (sedev, 2026-09-23) and the engine set to *DotLiquid (with Fluid
verification)*, a click-through of the pages that own the touched rows surfaced five more rows in
classes the scanner does not cover. Same script shape as `002` (guarded `REPLACE`, `@DryRun = 1`
default, backups to `dbo._ROCK9086_LavaBackup`, verify SELECT inside the transaction):

| Row | Pattern | Fluid today | Change |
|---|---|---|---|
| WorkflowActionForm 531 Header | filter inside `{% if %}`: `{% if url \| RegExMatch:'^[/~].*$' %}` | parse error, header blank | hoist to `{% assign urlIsRelative = … %}{% if urlIsRelative %}` |
| AttributeValue 62012675 (SE Online events, page 2070) | filter inside `{% if %}`: `{% if campus \| Attribute:'Slug' != 'Yes' %}` | parse error, block blank | hoist to `{% assign campusSlug = … %}{% if campusSlug != 'Yes' %}` |
| HtmlContent 6548 (SE Online Digital Resources, page 2858) | `{% assign collapsed = true \| AsBoolean %}` … `{% if collapsed == 'false' %}` | **`bool == 'false'` is TRUE on Fluid** (string coerces truthy) → first accordion panel open | `{% if collapsed == false %}` (×4) |
| HtmlContent 1661 (Contact page 1777, campus list) | `{% for number in campusNumbers %}` over an empty-string attribute | **Fluid iterates the string once** → stray `<br><p><a href="tel:"></a></p>` per campus | wrap the loop body in `{% if number != '' %}` |
| AttributeValue 16588591 (Badge 38 *Volunteer Pie Chart*) | `{% assign blank = '#e3ded7' %}` — `blank` is a reserved keyword | broken on **both** engines (DotLiquid writes `Liquid syntax error: … DotLiquid.Util.Symbol` into the CSS gradient) | rename to `blankColor` (1 assign + 5 reads) |

Behaviour change on DotLiquid: **531 only**. The old condition tested the truthiness of `url`, so
`InternalApplicationRoot` was prepended to every URL, absolute ones included; after the change
only `/` and `~` paths get the prefix.

Rows 1661 and 62012675 were already backed up by `002`, so `004_rollback.sql` reverts both passes
for those two rows.

Not every *Render output mismatch* is a regression. Two on dev were DotLiquid being wrong and
Fluid right: the badge `blank` row above, and page 2070's `Append:eventItem.Id` (DotLiquid emits
`Liquid error: Object reference not set…` and a bare `/page/1683` link; Fluid renders
`/page/1683?EventItemId=662`). Diff `Expected` (DotLiquid) against `Actual` (Fluid) before fixing —
the child `ExceptionLog` row is formatted
`[Expected=<DotLiquid>,\nActual=<Fluid>] [Template="…", Engine=Fluid]`.

Repo copies of the pass-2 classes were checked and are safe: `FAQ_Accordion.lava` and
`MyOnlineGroups.lava` compare string to string (no `AsBoolean`), `PageListAsBlocks.lava` compares
Rock's string-typed menu properties, and the `altLinks` loops are guarded with `!= empty`.

After `005` and a cache clear, re-hitting the five pages produced no new Lava rows. Pages **not**
exercised on dev: workflow action-type Lava (the `6MthsAgo` and regex rows fire only when those
workflows run), DefinedValue templates (types 48/236/245), `{[ parallax ]}` pages, the three
GroupFinderMap popups, Registration page 413.

## Pass 3 — webhook templates (`006_apply_webhooks.sql`)

The Subsplash mobile-app and TV-app JSON is served by `api/subsplash/webhook/{path}` (com.subsplash controller);
`{path}` resolves against DefinedType 277 *Subsplash Webhook*, whose `Template` attribute usually just
`{% include %}`s a `Content/Lava/SEMobileApp/...` file. DefinedType 236 *Lava Webhook* is the website
equivalent. Neither column was in the Phase 2 export. Crawling every route on dev with the engine on
FluidVerification (seed every parameterless route, follow the `api/subsplash/webhook` links in the JSON,
rewrite `app.secc.org` to the dev host) found 21 rows, 25 statements:

| Class | Rows | Change |
|---|---|---|
| `Replace:'"','\"'` (json-escape) | 20 | → `Replace:'"',bsq` with `{%- capture bsq -%}\"{%- endcapture -%}` prepended (same fix as the repo files in PR #304) |
| `Replace:'','\'` | 4 | → `Replace:bs,bsbs` with two captures prepended — Fluid reads `'` as an escaped quote and never closes the string |
| `"{{colors:brand}}"` | 1 | → `""` (`/sermondiscussion`) |
| `Split:'\|'- %}` (space before `%}`) | 1 | → `-%}` (`/campusdata`). Fluid: "End of tag '%}' was expected"; the error names the enclosing block, not the bad tag |

Every rewrite was rendered on both engines with the LavaProbe harness (cases A4/B2/C2). Applied on dev
2026-09-24; the re-crawl of 400 URLs left only the two routes whose include files do not exist on any
share (`/weekend` → `Home/Weekend.lava`, `/home/tabs` → `Home/HomeTabs.lava`), which fail identically on
DotLiquid.

The same crawl found three more `Content/Lava` classes, fixed in PR #316: the `- %}` trim in 20 HtmlToImage
templates; `Home-2.lava` (the live `/home` template) comparing `item.ExpireDateTime` against
`'Now' | Date` — a string, which Fluid never orders against a DateTime, so **expired banners came back**
(fix: `'Now' | Date:'yyyy-MM-dd HH:mm:ss' | AsDateTime`; note `'Now' | AsDateTime` alone is null on both
engines); and `FamilyResources_Details.lava` parsing `'Now' | Date:'dd/MM/yy HH:mm' | AsDateTime` as
month/day → null → empty response on **both** engines since 2022.

Pre-existing, not Fluid: `/sekids/home/worship` (DV 50683) emits invalid JSON (missing comma between
blocks) on DotLiquid today.

## Pass 4 — rows production itself reported (`007_apply_prodpass.sql`)

Production's engine was set to *DotLiquid (with Fluid verification)* on 2026-09-25, so its own ExceptionLog became the
burn-down source. After `002`/`005`/`006` and the Content/Themes share copy, four database rows kept failing:

| Row | Page | Pattern | Change |
|---|---|---|---|
| AttributeValue 3241 | `/MyDashboard` (BEMA *My Workflows Lava*) | `>= aYearAgo  AND ... Status = 'Active'` — uppercase `AND`, single `=` | `and` / `==` |
| AttributeValue 54526705 | OnePoint `/documents` | `{% if documentCategory == {{ctgyName}} %}` | bare variable |
| AttributeValue 328839027, 361818241 | `/groups/oncampus` redirect blocks | `Replace:' ':''` — colon between the filter arguments | `Replace:' ',''` |

Forms verified with LavaProbe (R2/R4/R6). Applied to production 2026-09-25. Still open from the same log: the *View Case*
HtmlContent rows (`{% workflow where:'Guid == "{{workflowGuid}}"' limit:'1'%}` → "A value was expected"; entity tags cannot be
probed offline), one Communication body with a parse error (page 1123, a single email, not a template), and *Render output
mismatch* warnings on the group pages that still need Expected/Actual diffing.

**Production deploy notes (2026-09-25):** the three prod web nodes serve `/Content` and `/Themes` as IIS virtual directories
on \\seccrockprod.file.core.windows.net\iis\IIS_Rock16, so copying to that share is the deploy; clear the cache **after** the copy finishes (a clear during the copy re-caches old files, which is
templates per app lifetime — clear the cache **after** the copy finishes (a clear during the copy re-caches old files, which is
what happened on the first attempt). A tracked `Thumbs.db` under `my-secc` is locked on the share; skip it.

## Behaviour changes to test before applying

Rock's vendored DotLiquid evaluates only the **first clause** of `{% if a && b %}` (`If.cs` splits
conditions on `and`/`or` only, and the unanchored `Syntax` regex drops everything after `a`). After
the `&&` → `and` rewrite every clause counts, so some conditions flip on production **the day the
scripts run, with the engine setting untouched**. Confirmed by rendering old vs new on the same
engine:

| Row | Old (first clause only) | New (all clauses) |
|---|---|---|
| WorkflowActionForm 456 Header (`child.Age < 6 && grade == ''`) | shows | hidden when a grade is set |
| WorkflowActionForm 531 Header (`attribute.IsRequired && attribute.Value == Empty`) | shows | hidden when the value is filled |
| `PurchaseOrderPDF.lava:70` (repo copy, same class) | address shown | address suppressed when either part is empty |

These are almost certainly the intended behaviour, but before running `002_apply.sql` with
`@DryRun = 0`: open workflow forms 456 and 531 in staging with one record that **should** show the
block and one that **should not**, and confirm both after the change. The same class of change
applies to every `and-or` row in `rows.md` (19 rows).

## How to apply

Run in SSMS against the target database, in order:

1. `001_preflight.sql` — every line should report `Found = 1`. A `0` means the row changed
   since the 2026-09-05 export (or the tag spans a line break); fix that row by hand in Rock.
2. `002_apply.sql` — leave `@DryRun = 1` first and read the log: every statement should show
   `Rows = 1` (except 12442 and 3386, see above). Then set `@DryRun = 0` and run again. Originals
   are copied to `dbo._ROCK9086_LavaBackup` before any change.
3. `005_apply_fluidpass.sql`, `006_apply_webhooks.sql` and `007_apply_prodpass.sql` — same dry-run then live routine; expect `Rows = 1` on every line and
   `StillHasOld = 0`, `HasNew > 0` in its own verify block. Unlike `002`, these two have no expected zeros, so a
   live run (`@DryRun = 0`) that finds any `Rows <> 1` rolls back and throws — nothing is committed.
4. **Clear the Rock cache** (route `/cachemanager`; not under Admin Tools > System Settings on
   1.16). AttributeValue, HtmlContent, LavaShortcode and WorkflowActionForm are cached; nothing
   changes on screen until you do.
5. `003_verify.sql` — expect `StillHasOld = 0` and `HasNew > 0` on every line (covers `002` only).
6. If anything looks wrong: `004_rollback.sql` restores every touched row (both passes) from the
   backup table, then clear the cache again.

Running from a CLI instead of SSMS: the dev database is Azure SQL, so `sqlcmd -E`/`-G` do not
work; take an AAD token with `az account get-access-token --resource https://database.windows.net`
and set `SqlConnection.AccessToken` (pwsh 7 + `System.Data.SqlClient`). The scripts contain no
`GO`, so each file runs as one batch.

To switch the engine on 1.16 (no UI for it): set `AttributeValue` for global attribute
`core_LavaEngine_LiquidFramework` to `DotLiquid`, `Fluid` or `FluidVerification` and restart the
app. *FluidVerification* renders every template twice — turn it back to `DotLiquid` before any
performance-sensitive testing.

## Provenance / regenerating

Source export: `scan_db_lava.sql` (secc-claude-tools, `lava-fluid-migration` skill) run
against production on 2026-09-05 → 2593 flagged rows → `scan_lava.py` → 53 rows with
must-fix findings on their current version. Scripts generated by
`gen_db_lava_updates.py --tag ROCK9086 --extra extra.json`; `extra.json` holds the one
hand-curated change (the lat/lng guard). Re-export and regenerate rather than editing the
SQL by hand.

Verified: the simulated after-state re-scans with 0 must-fix findings; every rewrite pattern
was rendered through both engines with the skill's `LavaProbe` harness; all four scripts
parse and run against a Rock 1.16.12 schema.

Not covered here: check-in label ZPL (BinaryFileData), `~/Content/...` Lava files that live
only on the web server, and the 245 `{% include %}` tags with dynamic paths — those are for
the Fluid-verification pass in staging. Note that the repo's `Content/Lava/` changes in PR #304
(59 files: mobile-app JSON escaping, `{{colors:brand}}`, `OrderBy`, `| |`, badge `{{ }}`-in-`if`)
are **not** deployed by the pipeline — the `Content` folder is a network share and has to be
updated by hand, after diffing against what is on the share. The same is true of `Themes/`: the theme
folders are hand-copied to the share too, and on 2026-09-24 dev's theme folder still had the pre-#304 files
(`/events` failed on `&&`, `/page/1607` on `| |`). Both were re-baselined from production with production as
the source of truth and the Fluid fixes re-applied on top — Content in PR #316, Themes in PR #317 — after a
database liveness check of which themes are still used; deploy both to the prod and dev shares by hand after
merging (LF export, byte-compare copy, no deletes).

Known scan gaps (`scan_db_lava.sql` reads 15 table.column pairs): `ReportField.Selection`,
`WorkflowType.SummaryViewText`, `DefinedValue.Description`, `Block.PreHtml`, `Block.PostHtml`,
`Metric.SourceLava` and `Page.HeaderContent` also hold Lava and were never exported. Two rows in
those columns are already known to break under Fluid (WorkflowType 456 `SummaryViewText` still has
`&&`; report 2122 mismatches). Add the columns to the scanner in secc-claude-tools, re-export, and
regenerate before the staging pass. The scanner's rules also do not cover the classes fixed
repo-side in PR #304 (filters or `{{ }}` or parentheses inside `{% if %}`, `| |`, `{% else- %}`,
`Date:'yyMMddHHmm' | AsInteger`, `Replace:'"','\"'`), so after applying these scripts burn down
the DEV `ExceptionLog` signatures from a Fluid-verification run rather than adding regex rules:

```sql
SELECT COUNT(*) AS Cnt,
       LEFT(REPLACE(REPLACE(Description, CHAR(13), ' '), CHAR(10), ' '), 260) AS Sig
FROM ExceptionLog
WHERE CreatedDateTime >= '<run start>' AND CreatedDateTime < '<run end>'
  AND (Description LIKE 'Lava%' OR Source LIKE '%Fluid%')
GROUP BY LEFT(REPLACE(REPLACE(Description, CHAR(13), ' '), CHAR(10), ' '), 260)
ORDER BY Cnt DESC;
```

Last updated: 2026-09-25
