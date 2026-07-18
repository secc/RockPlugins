# org.secc.ConnectionsDigest

> A single scheduled job that emails each connector a periodic digest of their connection requests (new, active, idle, critical) for the selected opportunities.

## Overview

ConnectionsDigest ships one Rock scheduled job (`ConnectionsDigest`) that scans connection
requests across a configured set of connection opportunities, groups them by the assigned
connector, and sends each connector a summary email. The email rolls up counts of new, active,
idle, and critical requests since the job's last run. It's used to keep connectors aware of
requests assigned to them without their having to poll the Rock connection board.

## Project Info

- **Project file:** `org.secc.ConnectionsDigest.csproj`
- **Root namespace:** `org.secc.ConnectionsDigest` (note: the `ConnectionsDigest` class itself is
  declared in namespace `org.secc.Jobs`)
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly, via a PostBuildEvent `xcopy` that copies
  `bin\Debug\org.secc.ConnectionsDigest.*` — note it copies the Debug output unconditionally,
  regardless of build configuration)

## Project Layout

```
ConnectionsDigest.cs                         The scheduled job (Quartz IJob)
/Migrations/20180629120000_InitialCreate.cs  Installs the "Connection Digest Email" SystemEmail
/Properties/AssemblyInfo.cs                  Assembly metadata
org.secc.ConnectionsDigest.csproj            Project file
packages.config                              NuGet packages (EntityFramework 6.1.3)
App.config                                   App config
BuildPlugin.ps1                              Plugin packaging script
```

## Components

### Jobs

A Quartz `IJob` decorated `[DisallowConcurrentExecution]`; wired up as a Rock Job (Admin > System
Settings > Jobs) and configured through the job attributes below (read from the `JobDataMap`).

| Job (class) | Purpose | Key settings |
|-------------|---------|--------------|
| `ConnectionsDigest` | Group active/future-follow-up connection requests by connector and email each connector a digest. | Connection Opportunities, Email, Save Communication History |

Configuration attributes:

| Setting | Type | Notes |
|---------|------|-------|
| **ConnectionOpportunities** | custom checkbox list | Opportunities to include; values are opportunity Guids (sourced via SQL on `ConnectionOpportunity WHERE IsActive = 1`). |
| **Email** | `SystemCommunicationField` (required) | The communication template sent to connectors. (See Observations — the seeded template ships as a legacy `SystemEmail` row, not a `SystemCommunication`.) |
| **SaveCommunicationHistory** | bool (default false) | If true, persists a communication record to each recipient's profile. |

What the job selects and the merge fields it exposes to the email template:

- Considers requests in `ConnectionState.Active`, plus `FutureFollowUp` requests whose
  `FollowupDate` is before midnight today, that have a `ConnectorPersonAliasId` set.
- Groups by connector person; for each, computes **new** (created since `job.LastRunDateTime`),
  **idle**, and **critical** (`ConnectionStatus.IsCritical`) request sets.
- A request is **idle** when its newest `ConnectionRequestActivity.CreatedDateTime` is older than
  `ConnectionType.DaysUntilRequestIdle` days ago — or, if it has no activities at all, when its own
  `CreatedDateTime` is older than that threshold. Note the threshold lives on the
  `ConnectionType` (`cr.ConnectionOpportunity.ConnectionType.DaysUntilRequestIdle`), not on the
  opportunity.
- Merge fields passed to the template: `ConnectionOpportunities`, `ConnectionRequests`,
  `NewConnectionRequests`, `IdleConnectionRequestIds`, `CriticalConnectionRequestIds`, `Person`,
  `LastRunDate`.

## Dependencies & Integrations

- **Rock:** Quartz job framework (`IJob`, `JobDataMap`), `RockContext`, `ConnectionRequestService`,
  `PersonService`, `ServiceJobService`, Rock communication (`RockEmailMessage`,
  `RockEmailMessageRecipient`), Lava merge fields (`Rock.Lava.LavaHelper`), plugin migrations
  (`Rock.Plugin`). Project references `Rock`, `Rock.Common`, and `DotLiquid` (the Lava engine).
- **NuGet:** EntityFramework 6.1.3.
- No third-party APIs.

## Migrations

Ships one Rock plugin migration under `/Migrations/`:

- `20180629120000_InitialCreate` (`MigrationNumber 1`, `1.7.0`) — inserts the **"Connection Digest
  Email"** `SystemEmail` (Guid `10059716-8B49-46EA-BEDF-AE388DE9F7FF`) with the full digest Lava
  template under the `Plugins` category. `Down()` deletes that row by Guid.

## Observations

*Noticed while documenting — not a full audit.*

- **Improvement:** The job reads its Rock Job Id from `context.JobDetail.Description`
  (`Convert.ToInt16( context.JobDetail.Description )`) to look up `LastRunDateTime`. This is fragile
  — it assumes the job's Description field is set to the numeric Job Id, and overloads a free-text
  field for an identifier. Worth confirming this convention is documented wherever the job is
  configured.
- **Inconsistency:** The `Email` job attribute is a `[SystemCommunicationField]` (picks a
  `SystemCommunication`), but the migration seeds a row in the legacy **`SystemEmail`** table and
  the job sends via `new RockEmailMessage( systemEmail.Value )`. So the seeded template the picker
  is presumably meant to select is in the wrong entity table — an admin would have to recreate it
  as a SystemCommunication for the picker to find it.
- **Improvement:** The migration's inserted Lava template hardcodes production URLs
  (`https://rock.secc.org/page/407`, `/page/408`) and page Ids, so the template is
  environment-specific and won't link correctly in non-prod or if those pages change.
- **Improvement:** `AssemblyInfo.cs` still carries the default scaffolded company/copyright
  (`"Microsoft"`, `"Copyright © Microsoft 2017"`) rather than Southeast Christian Church, despite
  the source-file copyright header naming SECC.

## Making Changes

- To change which requests are summarized or how new/idle/critical sets are computed, edit the
  LINQ in `ConnectionsDigest.cs`; the merge fields it adds drive what the email template can show.
- To change the email's appearance, edit the **"Connection Digest Email"** SystemEmail in Rock
  (or ship a new numbered migration that updates the template — don't hand-edit the existing one
  that has already run).
- This job lives in namespace `org.secc.Jobs`; sibling/related scheduled jobs are in
  [org.secc.Jobs](../org.secc.Jobs/README.md).
