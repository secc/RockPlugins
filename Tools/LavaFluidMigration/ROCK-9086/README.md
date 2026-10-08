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

Forms verified with LavaProbe (R2/R4/R6). Applied to production 2026-09-25.

`008_apply_viewcase.sql` — the *View Case* page (HtmlContent 10557, block 6995) logged "(Block: workflow) A value was expected". The
`{% workflow %}` tag is fine; line 51 `{% assign wStatus = {{Workflow.Status}} %}` has a `{{ }}` inside an `assign`, which Fluid's
expression parser rejects and Rock attributes to the enclosing block. Now the bare `Workflow.Status` (LavaProbe V1/V2). Applied to
production and dev 2026-09-25. Rule of thumb: when Fluid blames a block tag, read the block body first.

Still open from the same log: ~~the *View Case*
HtmlContent rows~~ (fixed by 008), one Communication body with a parse error (page 1123, a single email, not a template), and
~~*Render output mismatch* warnings on the group pages~~ (diffed; see below).

**Group-page render mismatches (theme files, not SQL).** Diffing Expected/Actual on the group pages gave two results:

- `PublishGroupDetail.lava` — whitespace only (DotLiquid keeps a newline between `</script>` and the next `<div>`). Benign.
- The share buttons — `shareMessage('{{ group.Name | Replace:"'","\'" }}', …)` escapes apostrophes for the JavaScript string
  on DotLiquid (`Men\'s`) but is a no-op on Fluid (`Men's`), which ends the `onclick` string early for any group name with an
  apostrophe. Same mechanism as the json-escape class in pass 3: Fluid decodes `"\'"` to a bare `'`. Fixed in the six SECC2024
  `Assets/Lava/Groups/` files that use it (ConsolidatedGroups, ConsolidatedPublishGroupDetail, GroupFinder, HomeGroups,
  OnlineGroups, PublishGroupDetail) with `{%- capture bsapos -%}\'{%- endcapture -%}` at the top and `Replace:"'",bsapos`
  (LavaProbe Q2/S1 identical on both engines). These are theme files, so copy them to both shares and clear the cache.

## Pass 6 — the next rows production reported (`009_apply_prodpass2.sql` + theme files)

With the share-button fix live, the remaining prod rows were:

| Where | Pattern | Fix |
|---|---|---|
| AttributeValue 359962155 — `/groups/homegroups` Redirect block `Url` | `{% if ((…) or (…)) %}` (Fluid rejects parens) and `Replace:' ':''` | precomputed `hasFilter`, one plain `if` per clause; comma between args |
| AttributeValue 449779791, 449129643 — *RSVP Weekend Follow-up* (Detect tapback, Build Note) | `\s` / `\S` inside a quoted regex argument — Fluid "End of tag" | pattern moved into a `capture` (the Build Note capture has no right trim so `' to '` keeps its leading space) |
| 7 connection-card workflow `Phone` / *Get Phone Number* values | `Slice: 3,15` — Fluid reads the 2nd argument as an end index and drops the last two digits | `Slice: 3,100` |
| `HomeGroups.lava` (SECC2024, SECC2019, Child_Invert) and `OpportunitySearch.lava` (SECC2024, SECC2019) | `{%- when '19' and meetingType == '' -%}` — Fluid parse error; DotLiquid ignores everything after the first value | `{%- when '19' -%}` / `{%- when null -%}` (what DotLiquid already did) |

**Behaviour change on the Home Groups redirect.** DotLiquid never evaluated the parenthesised condition as written: `?type=`,
`?meet=`, `?handicap=` or `?otc=` on their own fell through to the campus-only branch (LavaProbe P4–P7). The rewrite does
what the template says, so those filters now carry through to `/allgroups`.

The RSVP workflow was built on production after dev was last copied, so on dev its two rows log `-1` (absent) instead of
failing the run.

## Pass 7 — the hour after 009 (`010_apply_prodpass3.sql` + theme/Content files)

Generated by `gen_010.py` from the live prod values (every REPLACE checked against them first). Of the 15 mismatch
templates in the log, 8 were benign: encrypted per-render tokens (`rckipid`, unsubscribe and confirm-account links) and
debug text inside an HTML comment on two signature documents.

| Where | Pattern | Fix |
|---|---|---|
| 25 ReportFields — "Children:/Parents:" Lava in staff, elder and family directory reports | `{% if forloop.rindex > 1 %},` — Fluid 2.3.1 numbers `rindex` backwards, so the commas move | `forloop.last == false` |
| HtmlContent 12894 — `/links/{slug}` detail | `hideTitle == false` / `roundHeaderImage == true` against `'RawValue'` (a string) — Fluid hides every title | `== 'False'` / `== 'True'` (exactly what DotLiquid matched) |
| WorkflowType 47, 724, 732, 733, 734 `NoActionMessage` | `{% assign completed = false %}` then `== 'true'` / `== 'false'` — Fluid matches neither branch | `{% if completed %}` / `{% else %}` |
| 5 workflow 639 email bodies | `{% assign X = {{Workflow \| Attribute:…}} %}` | bare expression (DotLiquid resolves the no-space form, LavaProbe G1–G3) |
| `publishgroup/list`, `/v2`, `/aigroupfinder` webhooks | `"Name": "{{g.Name}}"` etc. unescaped — a `"` in a group name breaks `webrequest` on Fluid | `Replace:'"',bsq`, as in 006 |
| RSVP Weekend (FINAL) *Build Phone For Card* | `RegExMatch:'^([0-9])\1\1…'` — `\1` inside a quoted string | pattern in a `capture` |
| `PublishGroupFilters.lava` (SECC2024/SECC2019/Child_Invert), `APIPublishGroupFilters.lava` (SECC2019/Child_Invert), `Content/Lava/APIPublishGroups.lava` | `{% for item in results %}` over the single JSON object the webhook returns — Fluid iterates nothing, the Ministry filter comes up empty | `{% for data in results.AvailAudienceIds %}` |

**Not a template bug:** the *Starting:* dates on the group finder are one day earlier on DotLiquid than on Fluid. The
webhook stamps local dates with a `Z`, and DotLiquid's JSON parse converts them to local time (a day back); Fluid keeps
the date. Fluid is right, so nothing changes; prod shows the correct dates once it runs on Fluid.

## Pass 8 — `{{ }}` inside `assign`, everywhere (`011_apply_assign_mustache.sql`)

A wide `LIKE '%{%%assign % = {{%'` first suggested 273 workflow action values; it was matching an `assign` on one line
and a `= {{ … }}` in SQL text further down. `gen_011.py` scans the dumped values with a real tag regex and found 16 rows
(18 tags): 6 workflow action values (workflow 639 and siblings: `EventContact`, `StaffContact`, `MinistryArea`,
`SECCLocation`, `ChildContact`), 4 workflow form headers (`failflag = {{Workflow | Attribute:'RegExFailureFlag'}}`), and 6
HTML blocks — five more copies of the View Case `wStatus = {{Workflow.Status}}` line and one `connectStatus`. All use the
no-space form DotLiquid resolves, so the bare expression renders the same (LavaProbe G1–G3, V1/V2).

Left alone: AttributeValue 121667807 (workflow 639, *Set Today's date*) puts a whole template inside one `assign` tag. It
fails to parse on DotLiquid too, so it has never worked; it needs its owner to say what it was meant to do.

## Pass 9 — the evening log (`012_apply_prodpass4.sql` + `HomeGroups.lava`)

Generated by `gen_012.py`. After 010/011 went live (16:26 / 16:43 EDT) the Ministry filter, `/links` and webhook-JSON rows
stopped. The next four hours of the log produced:

| Where | Pattern | Fix |
|---|---|---|
| HtmlContent 10753, `/ministries/college-age` | nested `{% comment %}` — Fluid ends at the first `endcomment` | inner comment tags dropped, outer kept; the whole value is set, guarded by the SHA-256 of its exact current text |
| HtmlContent 10536, `/communityengagement` service times | `{% if (forloop.rindex0 == 0 %}` — parens plus Fluid's reversed `rindex0`. DotLiquid only ever took the last-item branch | `{% if forloop.last %}` |
| SystemCommunication 9 *Workflow Form Notification* | the link was mangled by an HTML editor on 2020-11-20: `{{ ' global'="" \|="" attribute:…` | restored to the stock Rock line |
| AttributeValue 419773844, 25012462 — reference *Create the PDF* | `{% assign textSize = … 'AnythingElseText' %}{% if textSize > 0 %}` — text compared to 0 (DotLiquid true when non-empty, Fluid never) | `\| Size` |
| AttributeValue 199751449 — page 3040 *Baptisms By Campus, Date & Age* | uppercase `AND` (DotLiquid drops everything after it) and parens in `elseif` | `and`, no parens |
| `HomeGroups.lava` (SECC2024) | `{% else if dayofWeek != '' %}` — Fluid parse error once the `when … and` fix let it get that far; DotLiquid treats it as `else` | `{% else %}` |

**Behaviour changes on DotLiquid:** the form-notification "requires action" link works again (it has been broken since
2020), and the Baptisms report's age filter now filters as written — before, a blank `MaxAgeRange` emitted
`age <= CAST('' AS INT)` (age ≤ 0).

**Not fixed here:** the SE!Kids video pages (`/familyresources/{slug}`, HtmlContent 4109) throw "Value was either too large
or too small for an Int32" at parse time inside a two-pass `{% cache %}` block — needs a live repro on dev.
`api/Lava/RenderTemplate` received a template with `{% capture s.roundcorners %}` from a client; it is not stored anywhere in
Rock or on the shares. `/lavatester` rows are someone's work in progress. `?theme=SECC2019_Child_Invert` on `/connect`
asks for a `PrimaryHeaderNav.lava` that theme does not have. `events/bible-beach` mismatches because DotLiquid writes
`Liquid error: … Int32 … String` into the Ministry filter and Fluid does not — Fluid is right.

## Pass 10 — dates stored as big numbers (`013_apply_datenumber.sql` + 24 theme/Content files)

The SE!Kids video pages threw "Value was either too large or too small for an Int32". The stack trace ends in
`Rock.Lava.Fluid.FluidExtensions.ToRealObjectValue` (Rock 1.16.12, `FluidExtensions.cs` line 247), which returns `(int) d` for
every whole-number decimal. It runs whenever a shortcode collects merge fields (`DynamicShortcode.OnRender` →
`GetMergeFields`) and whenever a Rock filter such as `Plus` receives the value. Today as `yyMMddHHmm` is 2609251846 — over
Int32 — so every `Date:'yyMMddHHmm' | AsDouble` (and `yyyyMMddHHmm`, `yyyyMMddHHmmss`) blows up on Fluid once a shortcode or
filter touches it. Dates before 2021-06 fit, which is why it only started recently. DotLiquid never makes that cast.
Worth reporting upstream to Spark.

Fix: a decimal point after the day — `yyMMdd.HHmm`, `yyyyMMdd.HHmm`, `yyyyMMdd.HHmmss`. Ordering and equality are unchanged
(the time becomes the fraction), the whole part stays at 6–8 digits, and blank dates behave the same (LavaProbe U1–U4,
V1–V4). Hard-coded literals next to these get the same point (`'202601041115'` → `'20260104.1115'`, the "never expires"
`Default:'99999999999999'` → `'99999999.999999'`).

- 24 repo files under `Themes/` and `Content/Lava/` (`gen_013.py --apply-files`).
- 7 HTML blocks (`013_apply_datenumber.sql`): 469, 4109 (SE!Kids video), 4257, 4621, 6434, 6544, 10546.
- **Behaviour change:** HtmlContent 4257 (page 2429 *Resources*) used `AsInteger`, which overflows on DotLiquid too, so that
  block has failed since mid-2021; it now uses `AsDouble`.
- HtmlContent 10546 and `churchnews.lava` use a 12-hour `hhmmss`, so 1 PM sorts before 11 AM. Kept as is; changing it would
  change behaviour.

## Pass 11 — the weekend log (`014_apply_prodpass5.sql` + `HomeGroups.lava`, `LiveStreamRedirect.lava`)

Generated by `gen_014.py` from production 2026-09-25 through 2026-09-28. 013 went live 2026-09-28 10:42 EDT, so the
weekend's SE!Kids Int32 rows are from before it. Every rewrite keeps the DotLiquid output (LavaProbe cases 27-31):

| Where | Pattern | Fix |
|---|---|---|
| `HomeGroups.lava` (SECC2024) | pass 9's `else if` → `else` left **two** `{% else %}` in one `if`; Fluid: "'{% endif %}' was expected". DotLiquid renders the first | second `else` branch (never reached) removed |
| `Content/Lava/SETVApp/LiveStreamRedirect.lava`, `/seonline/livestream` | `PageRedirect` inside `{% definedvalue %}`: its interrupt reaches Fluid as an `AggregateException`, logged as "(Block: definedvalue) One or more errors occurred" | keep the first matching URL, redirect after the block |
| HtmlContent 4591, OnePoint internal jobs | `{{ job.['Job Serial'] }}` | `job['Job Serial']` |
| HtmlContent 5306 (Easter devotions), WorkflowActionForm 565, 566 (workflow 639) | `x \| \| Filter` | one pipe |
| WorkflowActionForm 289, 292, 339, 691 | `Address,'Home','…'` — DotLiquid reads the comma as the colon | `Address:'Home','…'` |
| AttributeValue 3889252 | `Attribute: 'Spouse':'NickName'` — DotLiquid reads the second colon as a comma | `'Spouse','NickName'` |
| WorkflowActionForm 36, hospital visits | `lastActivityDate >= '08/2/2019'` — DateTime vs string is false on Fluid, so the Actions column vanished | compare with `'2019-08-02' \| AsDateTime` |
| AttributeValue 418179915, 419773863, reference *Set NeedsReview* | `RawValue == true` / `== false` — Fluid: every non-empty string equals `true` | `== 'True'` / `== 'False'` |
| AttributeValue 119518117, 119518133, 181166313 (workflow 639 subjects) | `Attribute:'LocationShortCode''` | one quote |
| AttributeValue 119518134, 181933899 (workflow 639 bodies) | `(Registraion == 'None') AND (…)` | `and`, no parens (`Registraion` is misspelt, so it stays false on both engines) |
| AttributeValue 51107841 | `\| Size asInteger` | `\| Size` |
| AttributeValue 101737557, page 2829 weekly totals | `DateTime \| Replace:" 12:00:00 AM",""` — Fluid prints the offset | `\| Date:'M/d/yyyy'` |
| AttributeValue 211932365, page 3135 | `{% if {{row.Status}} … %}` | `{% if row.Status … %}` |
| AttributeValue 55093101, 58019746 (block LavaTemplates) | `when '19' and meetingType == ''` | `when '19'` |
| SystemCommunication 9 *Workflow Form Notification* | `{% if url \| RegExMatch:… %}` | assign first, as WorkflowActionForm 531 already does |
| AttributeValue 3752511/3752514, 71943274/71943275, 446668184/446668185 — MinistrySafe *Send the Email* body/subject (workflow types 108, 421 and a prod-only copy) | `Attribute:'IsRenewal'` is the formatted Boolean, `Yes`/`No`, compared to `True` | `Attribute:'IsRenewal','RawValue'` and `== 'True'` |
| HtmlContent 12330, `/MyVolunteerStatus` | `CurrentPerson \| PersonInDataView:'1678'` — not a Lava filter | `CurrentPerson.Id \| IsInDataView:'1678'` (data view *Ministry Safe Training is Expired*) |

**Behaviour changes on DotLiquid:**

- SystemCommunication 9 — DotLiquid ignored the `RegExMatch` and prefixed *every* attribute URL with
  `InternalApplicationRoot`; now only relative ones are.
- MinistrySafe emails — `Yes` never equalled `True` on DotLiquid, so every email used the first-time wording (Fluid would
  have used the renewal wording for everyone). Renewals now get the renewal subject and body.
- MyVolunteerStatus — the unknown filter meant *Expired* never showed on DotLiquid (and always would on Fluid). People in
  data view 1678 now see *Expired* and the *Renew MinistrySafe* button; the page runs that data view once per view.

AttributeValue rows are guarded on `AttributeId` and `EntityId` as well as `Id`: on RockDev, ids 446668184/446668185 are
unrelated rows (action 76744 exists only on prod), so they log `-1` there.

**Not fixed here (benign):** per-render tokens (ConfirmAccount, Unsubscribe, rckipid, shortlinks), the "Starting:" day shift
on the group finder (Fluid is right), DotLiquid-only `Liquid error:` text, `?theme=` requests for files the theme lacks.

**`015_apply_wftype456.sql`** (`gen_015.py`): WorkflowType 456 *The Unified Connection Card* `SummaryViewText` (shown on
`/Workflow/{id}`) has `{% if person != null && person != empty %}` — `&&` → `and`. `person` is `Attribute:'Person','Object'`
(a Person or null), so DotLiquid's first-clause-only reading gives the same result (LavaProbe case 32). `SummaryViewText` was
not in any earlier sweep; worth adding to the scanner's column list.

**Ignored:** sermon series pages stop rendering on Fluid at the YouTube id (`SermonSeriesDetail.lava`, the `youtube`
shortcode). The Rock sermon pages are retired — sermons live on Webflow and the page carries a Lava redirect there — so
treat these rows as known. The LWYA blog header (HtmlContent 284 and 10 copies, `/lwya/blog/post`) gets the page name
from `'Global' | Page:'Title'` on Fluid instead of the post title; it only feeds the breadcrumb and a commented-out
heading, and the LWYA site (Site 18) had ~4 human views in the 90 days to 2026-09-28 (109 of 113 were a Chrome 48 crawler),
so these rows are known too. Candidate for retiring or redirecting like the sermon pages.

**Open:** Bema pipeline `ActionLinks` render empty on Fluid (plugin object, not Lava — new plugin version requested from
BEMA).

## Pass 12 — two more days of the prod log (`016_apply_prodpass6.sql`)

Generated by `gen_016.py` from production 2026-09-28 16:30 through 2026-09-30 (843 Fluid rows; most are the benign
classes above). 19 rows:

| Rows | Where | Problem | Change |
|---|---|---|---|
| WorkflowActionForm 495–497, AttributeValue 3070786 / 80610647 / 80610655 / 80644504, WorkflowType 71 `SummaryViewText`, SystemCommunication 62 | *Event Requests – External* (type 71): forms, notification emails, summary | `{% if (Workflow \| Attribute:'WhatTypeOfVenueIsRequired') == 'Other' %}` | assign first, then compare |
| WorkflowActionForm 498–500 | *Event Request – Internal* (type 72) forms | same, `HasItOccurredBefore` | assign the **RawValue** (the attribute is a Boolean, formatted Yes/No) and compare to `'True'` |
| AttributeValue 248266514 | *Benevolence Request Connection Card Intake* (type 715) card, `/support` | filter inside `if`, compared to an unquoted GUID; `WhyUnemployed` filter inside `if` | assign first, quote the GUID, `and` instead of `or` |
| HtmlContent 7906 | Deacon/Elder application | `PageRedirect` inside `{% connectionrequest %}` (Fluid wraps the interrupt in an AggregateException) | keep the URL, redirect after the block |
| HtmlContent 10407, Block 6928 `PreHtml` | Sports & Fitness training video | parentheses in `if`; `{{ }}` inside `assign` (PreHtml) | removed |
| WorkflowActionForm 569 | *W-9 Form* | `isAutomated == true` (any non-empty string equals `true` on Fluid) | `== 'True'` |
| ReportField 64362 / 64364 | report 2069 *TEST \| PR Families* | `Where:'GroupRoleId', 10#` | `10` |

**Behaviour changes on DotLiquid:** a filter inside `if` is always true on DotLiquid (LavaProbe V1–V4, M1–M5), so the
"Other venue", "occurred before", spouse and "why unemployed" lines showed on every request; now they show only when they
apply. Report 2069's head-of-household column was always empty; now it fills. The other rows render the same on DotLiquid
(LavaProbe cases 33–36).

**Not fixed:** report 1522 *Cleared with Minors by Campus* (ReportField 48764, 48784, 48785): parentheses throughout, and
logic whose intent is unclear (`daysuntil >= 1 or daysuntil <= 60` is always true); left for the report owner. Email template 4
is in `017` below. The mobile-app card images (`churchseimaging`
ImageFromHtml builds `{% capture s.roundcorners %}` from the query string) are no longer used. Benign this time as well: the
SignNow signature templates differ only inside an HTML comment, Group Detail hides `Capacity 0` on Fluid, and
`{{FromPerson}}` prints the name on Fluid where DotLiquid printed nothing.

**`017_apply_commtemplate4.sql`** (`gen_017.py`): CommunicationTemplate 4 *Neighborhood Group – Recommit Final Reminder*
has `rckipid={{%20Person.UrlEncodedKey%20}}` (an editor URL-encoded the spaces). Fluid cannot parse the template, so the
whole email fails; DotLiquid renders the key empty. The link goes to Webflow (`www.southeastchristian.org` → `www.se.church`,
which returns 404), so a Rock login token (`PersonTokenCreate`) must not go there. The tag is removed, which is what
DotLiquid sends today. The link itself still needs pointing at the current sign-up page.

## Pass 13 — the next week (`018_apply_prodpass7.sql`)

Generated by `gen_018.py` from production 2026-09-30 through 2026-10-07 (300–650 Fluid rows a day, 40–65 signatures;
almost all the benign classes above — per-render tokens, the GroupFinder "Starting:" day, SignNow templates that differ
only inside an HTML comment, retired sermon pages, `?theme=` requests for files a theme lacks). 17 rows on production:

| Rows | Where | Problem | Change |
|---|---|---|---|
| WorkflowType 72 `SummaryViewText`, AttributeValue 3094280 / 80610811 / 80610821 / 82143392 | *Event Request – Internal* (type 72) summary and notification emails | `{% if (Workflow \| Attribute:'HasItOccurredBefore') == 'True' %}` — the copies 016 did not reach | assign the **RawValue**, compare to `'True'` (as in 016) |
| AttributeValue 206537195 | *Sports & Fitness training* workflow, Run Lava action (`/sftraining`) | parentheses in `if` | removed |
| AttributeValue 449129641 / 449130783 / 449130791 (production only) | *RSVP Weekend Prayer follow-up* emails (SMS pipeline 2) | `{% if Workflow \| Attribute:'CardReopened' == 'Yes' %}` | assign first, then compare |
| HtmlContent 546 / 547 / 548 / 550 / 554 / 557 | pages 1159–1164, 2018 MIX camp registration notes | the HTML editor stored `<=` as `&lt;=` in `{% if %}` | `<=` |
| ReportField 71196 | report 1532 *Computer Clearance by Campus*, Security Check column | `{% if (a == 'x') or (a == 'y') %}` (Fluid parse error) and a filter chain inside `if` (always true on DotLiquid) | parentheses removed; assign `DateDiff` first, then compare (the form the report owner used for the Badges column) |
| AttributeValue 418777152 (Block 9054 `FormattedOutput`, page 534) | *Pastoral Care* person tab, Nursing Home List | an integer **0 == ''** is TRUE on Fluid (LavaProbe P1/P3), so a 0 visit count went blank; `Communion` is a bit and **bool == 'false'** is TRUE on Fluid (Q6), so the check icon flipped | numbers compare to `null` only; `Communion` goes through `AsBoolean` and the branches swap so a NULL bit still shows the times icon (Q1–Q5, Q7) |

**Behaviour changes on DotLiquid:** the internal event request's "When/Where was it held" line, the RSVP emails'
"Their card had been closed" line and report 1532's "Attendance verified more than 6 months ago" line showed every time
(a filter inside `if` is always true on DotLiquid); now they show only when they apply. The other rows render the same on
DotLiquid (LavaProbe cases 38, P1–P10, Q1–Q17). 17 rows on production; the three RSVP emails do not exist on RockDev.
The report owner rewrote the other Lava columns of reports 1522 and 1528–1532 by hand on 2026-09-30; 71196 was the one
left over.

**Applied 2026-10-08** on RockDev (13 rows, 4 absent) and rockprod (17 rows), dry run then live, cache cleared through the
Cache Manager on both. Verified on sedev before prod: Pastoral tab for person 68058 shows `0` visits and check icons, MIX pages
1159/1162 render their date-gated text, type-72 workflow 00534 summary omits "When/Where was it held" (occurred = No), and a
`/sftraining` submit as a different participant took the PersonMismatch branch (workflow 01906, test person 787868 left on dev).
On prod after the clear: Pastoral 68058, workflow 00525 (occurred = Yes, line shown) and report 1532 rendered with zero new
Lava rows in the ExceptionLog.

**New divergence class:** Fluid's `NumberValue == StringValue` converts the string to a number, so `0 == ''` is true
(`5 == ''` is false, `0 == null` is false on both). Any `{% if n == '' %}` guard on a count or id is suspect; compare to
`null`, or `| ToString` first.

**Still open after this pass:** `/oldsite/connect` has `or or` in an `if` (6 rows, not in any database column — a theme
file of the old site); `api/Lava/RenderTemplate` still receives the `{% capture s.roundcorners %}` template nightly
(57 rows, client-sent); BEMA pipeline `ActionLinks` (waiting on BEMA). *MyVolunteerStatus* logs `(Block: sql) Conversion
failed … uniqueidentifier` — a bad AttributeValue joined to `PersonAlias.Guid`, not Lava.

**Production deploy notes (2026-09-25):** the three prod web nodes serve `/Content` and `/Themes` as IIS virtual directories
on `\\seccrockprod.file.core.windows.net\iis\IIS_Rock16`, so copying to that share is the deploy. Rock caches parsed
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
3. `005_apply_fluidpass.sql`, `006_apply_webhooks.sql`, `007_apply_prodpass.sql`, `008_apply_viewcase.sql`, `009_apply_prodpass2.sql`, `010_apply_prodpass3.sql`, `011_apply_assign_mustache.sql`, `012_apply_prodpass4.sql`, `013_apply_datenumber.sql`, `014_apply_prodpass5.sql`, `015_apply_wftype456.sql`, `016_apply_prodpass6.sql`, `017_apply_commtemplate4.sql` and `018_apply_prodpass7.sql` — run each dry-run then live; expect `Rows = 1` on every line (009-018 also allow `-1`, a row that does not exist in that database). The 005/006/008 verify blocks should show `StillHasOld = 0`, `HasNew > 0`; 007 and 009 use their `Old*`/`New*` columns, 010-018 their `StillOld` counts. A live run of any of them with an unexpected row count rolls back and throws — nothing is committed.
4. **Clear the Rock cache** (route `/cachemanager`; not under Admin Tools > System Settings on
   1.16). AttributeValue, HtmlContent, LavaShortcode and WorkflowActionForm are cached; nothing
   changes on screen until you do.
5. `003_verify.sql` — expect `StillHasOld = 0` and `HasNew > 0` on every line (covers `002` only).
6. If anything looks wrong: `004_rollback.sql` restores every touched row (all passes) from the
   backup table, then clear the cache again. A row can have more than one backup (the first live
   run of 014 backed up rows an earlier pass already had), so each row is restored from its
   earliest backup — the value before any ROCK9086 pass. The script throws if the backup table
   holds a table/column it does not handle.

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

`gen_010.py`–`gen_018.py` write their SQL next to themselves and read the dumped prod values
from `vals/` beside the script, or from the folder in `ROCK9086_VALS`. The dumps hold production
content and are not committed. `gen_012.py` writes `probe-cases22.txt` next to that folder.

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

Last updated: 2026-10-08
