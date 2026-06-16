# org.secc.Finance

> Generates and serves annual giving (contribution) statements — a workflow action and merge-field builder that assemble the statement, blocks to launch/list/print them, a download handler, and a job to drive the generation workflows at scale; plus a bulk registration-refund block.

## Overview

Finance is Southeast's contribution-statement plugin. It builds a person/family giving statement
(transactions, fund summary, pledges) as Lava-rendered HTML, then drives the at-scale generation of
those statements through a Rock workflow that a job processes in batches. Staff use the blocks under
**SECC > Finance** to kick off a statement run, review and list the resulting PDF/binary files, and
print an on-screen Lava statement. The plugin also ships a download handler that access-checks
statement files and a separate bulk-refund block for registrations.

## Project Info

- **Project file:** `org.secc.Finance.csproj`
- **Root namespace:** `org.secc.Finance`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly + iText7 / Newtonsoft), `RockWeb/Plugins/org_secc/`
  (block `.ascx` markup + Lava), and `RockWeb/GetStatement.ashx` (download handler)

## Project Layout

```
/Workflow/      GenerateStatement   — workflow action that renders one statement to an attribute
/Utility/       Statement           — builds the statement merge fields (transactions, accounts, pledges)
/Jobs/          ProcessGivingStatements — batch-processes pending statement-generator workflows
/Rest/Controllers/ FinancialStatementsController — DELETE endpoint for a statement binary file
/Handlers/      GetStatement.ashx   — access-checked download handler for statement files
/org_secc/Finance/  UI blocks (.ascx + .ascx.cs) and the ContributionStatement.lava template
/Migrations/    Rock plugin migrations (giving-analytics stored proc update)
```

## Components

### Workflow Actions

Discovered by Rock via MEF (`[Export(typeof(ActionComponent))]`). Category: **SECC > Finance**.

| Action | Purpose | Key config attributes |
|--------|---------|-----------------------|
| Generate Giving Statement | Builds one person's/family's statement and stores the rendered HTML to a target workflow attribute. | Person, Start Date, End Date, Lava Template, Excluded Currency Types, Accounts, Statement HTML (target) |

Statement content is assembled by `Utility/Statement.AddMergeFields`, which pulls all transactions
for the person's `GivingId` (whole giving family), groups them into account/fund summaries, and adds
pledge progress. With no Accounts selected it defaults to tax-deductible accounts.

### Jobs

| Job | Purpose | Key config attributes |
|-----|---------|-----------------------|
| ProcessGivingStatements | Finds active, **Pending** workflows of the configured statement-generator type and activates the named PDF-generation activity on each (new `RockContext` per workflow). | Statement Generator Workflow, Workflow Activity Name |

### Blocks

Category in Rock: **SECC > Finance**.

| Block | Purpose | Key settings |
|-------|---------|--------------|
| Contribution Statement Generator | Kicks off the statement-generation run (launches the generator workflow over a dataview). | Statement Generator Workflow, Default Review DataView, Suppress Statement Dataview, Command Timeout, Concurrent Workers |
| Contribution Statement List | Lists generated statement files; supports merge/print and delete. | Detail Page, Binary File Type, Document Type, Print & Mail Dataviews, Page Load Timeout |
| Contribution Statement Lava | Renders an on-screen, printable Lava statement for a person. | Accounts, Display Pledges, Lava Template (ships a default template) |
| Bulk Refund | Issues bulk refunds for registrations (SignalR progress). | (none declared) |

### REST Endpoints

| Route | Method | Purpose |
|-------|--------|---------|
| `api/FinancialStatements/{binaryFileId}` | DELETE (`[Authenticate, Secured]`) | Deletes a contribution-statement binary file and its `Document` row. |

### HTTP Handlers

| Handler | Purpose |
|---------|---------|
| `GetStatement.ashx` (`?id=` or `?guid=`) | Streams a binary file (resumable/range support). When the file type requires view security, authorizes the caller via Rock authorization *or* by matching the file's `GivingId` / `PersonIds` attributes against the current person. |

## Dependencies & Integrations

- **Rock:** `RockContext`, `FinancialTransactionDetailService`, `PersonAliasService`,
  `WorkflowService` / workflow engine, `BinaryFileService`, Rock REST (`ApiControllerBase`),
  the Rock block/UI framework, SignalR (`RockMessageHub`).
- **Third-party:** iText7 (PDF merge in the statement list), DotLiquid / Lava, Quartz (job),
  Newtonsoft.Json.
- **Cross-plugin:** none in code; relies on the configured Rock workflow type for statement
  generation.

## Migrations

Ships Rock plugin migrations (SQL only — `Down()` is intentionally empty):

- `001_v12_AddFamilyIdToGivingAnalytics` — adds `PrimaryFamilyId` to
  `spFinance_GivingAnalyticsQuery_PersonSummary` (Rock 1.12).
- `002_v13_AddFamilyIdToGivingAnalytics` — re-applies the same change for the Rock 1.13 stored proc.

## Observations

*Noticed while documenting — not a full audit.*

- **Security (medium):** Giving statements are sensitive financial PII. `GetStatement.ashx` only
  enforces authorization when the binary file's *type* has `RequiresViewSecurity` set — otherwise
  any file id/guid streams without a person check. The custom fallback (matching `GivingId` or
  `PersonIds` file attributes) is reasonable, but worth confirming the statement binary-file type is
  actually flagged `RequiresViewSecurity` and that those attributes are populated on every generated
  file; otherwise statements could be retrieved by guessing/iterating ids.
- **Improvement:** The DELETE endpoint runs a raw `DELETE FROM dbo.[Document]` via
  `ExecuteSqlCommand` (parameterized, so not injection-prone) alongside the EF `Delete` of the
  binary file. Review that this leaves no orphaned references and consider doing the cleanup through
  the service layer for consistency.
- **Improvement:** `Statement.AddMergeFields` constructs its own `RockContext` independent of the
  caller's, so the workflow action effectively uses two contexts per statement. Fine for one-off
  generation, but the per-person job loop plus per-statement context allocation is worth watching at
  full-membership scale.

## Making Changes

- To change what a statement contains (transactions, fund summary, pledges), edit
  `Utility/Statement.cs`; to change the action's inputs/output, edit `Workflow/GenerateStatement.cs`.
- The default on-screen statement markup lives in the **Lava Template** block setting of
  Contribution Statement Lava (and in `org_secc/Finance/Lava/ContributionStatement.lava`); the
  generation run is configured on the Contribution Statement Generator block.
- The batch driver is `Jobs/ProcessGivingStatements.cs` — it keys off the configured generator
  workflow type and activity name, so adding a new generation step is a workflow change, not a code
  change.
- File access/serving rules live in `Handlers/GetStatement.ashx.cs`; deletion in
  `Rest/Controllers/FinancialStatementsController.cs`.
