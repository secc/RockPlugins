# org.secc.Trak1

> A Rock background-check provider that submits applicant data to the Trak-1 web service and polls back report status/results through a workflow action.

## Overview

Trak1 integrates Southeast's background-check process with the [Trak-1](https://www.trak-1.com/)
screening service. It ships a Rock `BackgroundCheckComponent` that builds an applicant payload
(name, SSN, DOB, home address) and POSTs it to Trak-1's `RunPackage` endpoint, a workflow
`ActionComponent` that polls `GetReportStatus`/`GetReportUrl` and writes the result back onto the
workflow and the Rock `BackgroundCheck` record, and an admin block that lists the screening
packages available from Trak-1. It is driven entirely by Rock's stock background-check workflow
plumbing — staff pick "Trak-1" as the provider on a background-check workflow.

## Project Info

- **Project file:** `org.secc.Trak1.csproj`
- **Root namespace:** `org.secc.Trak1`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly + `.pdb`) and `RockWeb/Plugins/org_secc/` (block `.ascx` markup), via the PostBuildEvent `xcopy`

## Project Layout

```
/BackgroundCheck/   Trak1.cs       — BackgroundCheckComponent (builds + sends the request)
/Workflow/          Trak1Status.cs — ActionComponent that polls status/report and writes results back
/Helpers/           Trak1Client (HTTP + response validation), Trak1Exception, Trak1StatusUpdate
                    (status/report polling decisions), and the DTOs (request/response):
                    Trak1Authentication, Trak1Applicant, Trak1Request, Trak1Response, Trak1Package,
                    Trak1Component, Trak1RequiredField, Trak1Report, Trak1ReportStatus, ErrorInformation
/org_secc/Trak1/    Trak1Settings block (.ascx + .ascx.cs) — lists available Trak-1 packages
/Properties/        AssemblyInfo.cs
/org.secc.Trak1.Tests/  xunit tests for the Rock-free helpers (no Rock database or Trak-1 calls)
```

## Components

### Background Check Provider

`BackgroundCheck/Trak1.cs` — a `BackgroundCheckComponent` (MEF `[Export]`, `ComponentName "Trak-1"`).
`SendRequest` resolves the workflow's person, gathers a package's required fields, builds a
`Trak1Request`, POSTs it to the Request URL, and creates/updates a Rock `BackgroundCheck` row keyed
to the workflow. The request fails (an error message and `false`, nothing saved) when the Request
Type attribute is missing or does not resolve to a package, on a non-2xx response (the message
includes Trak-1's `Error.Message` and incident id when the body has one), on an empty response, on
a vendor error (including one nested at `Error.Error.Message`), and when Trak-1 returns no
`TransactionId`. Configuration is per-provider via Rock component attributes:

| Setting | Type | Notes |
|---------|------|-------|
| **UserName** | text, required | Trak-1 user name. |
| **SubscriberCode** | encrypted text, required | Subscriber code from Trak-1 (decrypted at call time). |
| **CompanyCode** | encrypted text, required | Company code from Trak-1 (decrypted at call time). |
| **PackageURL** | URL, required | `GetAvailablePackages` endpoint (default points at `stgapi.trak-1.com`). |
| **RequestURL** | URL, required | `RunPackage` endpoint (default `stgapi`). |
| **StatusURL** | URL, required | `GetReportStatus` endpoint (default `stgapi`). |
| **ReportURL** | URL, required | `GetReportUrl` endpoint (default `stgapi`). |

`GetReportUrl(reportKey)` returns a `~/GetFile.ashx?guid=` link, gated on the current person being
authorized to `VIEW` the component (otherwise returns `"Unauthorized"`).

### Workflow Actions

`Workflow/Trak1Status.cs` — `ActionComponent`, category **Background Check**, `ComponentName
"Trak-1 Status"`. Looks up the `BackgroundCheck` row by workflow Id, gets the status from the
provider's Status URL and, only once the report is complete, the report from the Report URL, and
writes the results back. A report is complete when `ReportStatus` is one of
`Trak1StatusUpdate.CompleteStatuses` (trimmed, case-insensitive). In production (checked 2026-10)
the only complete value is `Complete`, which is also what the Trak-1 background check workflow
stops polling on. The pending values are `Assigned`, `Pending Verification` and `Needs Attention`,
and completed checks have a `HitColor` of `Green`, `Red` or `Orange`.

| Setting | Type | Notes |
|---------|------|-------|
| **Provider** | component (`Rock.Security.BackgroundCheckContainer`) | The background-check provider to query. |
| **ReportStatus** | workflow attribute (text) | Receives the Trak-1 `ReportStatus` on every poll (`BackgroundCheck.Status` gets the first 25 characters). |
| **HitColor** | workflow attribute (text) | Receives the Trak-1 `HitColor` on every poll. Once the report is complete it also sets `BackgroundCheck.RecordFound` (true = a record/hit was found): empty/missing → `null`, `"Green"` (case-insensitive) → `false`, any other color → `true`. While the report is pending `RecordFound` is not changed. |
| **ReportUrl** | workflow attribute (text) | Receives the report URL once the report is complete. `BackgroundCheck.ResponseData` gets the same URL, and `ResponseDate` is set when the URL is first received (or changes). |

If no `BackgroundCheck` row exists for the workflow, or the row has no Trak-1 transaction id (the
request was never accepted), the action logs and sets ReportStatus to `"Error"` (returns `true` so
the workflow can handle it gracefully). All Trak-1 calls are made before anything is written back.
If a call fails (non-2xx HTTP status, network error, an empty or unreadable body, a status without
a `ReportStatus`, or a completed report without a `ReportUrl`; Trak-1's `RequestValidation` text is
included in the message) the action writes the failure to the Exception Log, adds the underlying
error message, and returns `false` so the workflow records the error instead of faulting. Nothing is
written to the workflow or the `BackgroundCheck` row in the failure case.

### Blocks

Category in Rock: **SECC > Background Check**.

| Block | Purpose | Key settings |
|-------|---------|--------------|
| Trak1 Packages | Read-only admin view listing each Trak-1 package (price, description, components, required fields) returned by `GetAvailablePackages`. | (none — resolves the Trak-1 provider via `EntityTypeService`) |

### Models / DTOs

Plain POCOs under `/Helpers/` used to serialize requests to and deserialize responses from the
Trak-1 JSON API — not Rock entities. Key ones: `Trak1Request` (`Authentication` + `Applicant` +
`PackageName`), `Trak1Applicant` (name/SSN/DOB/address + `RequiredFields` dictionary),
`Trak1Response` (`TransactionId`, optional `Error`), `Trak1ReportStatus` (`ReportStatus`,
`HitColor`, `Recommendation`), `Trak1Report` (`ReportUrl`, `HitColor`), and `Trak1Package` /
`Trak1Component` / `Trak1RequiredField` describing the package catalog.

## Dependencies & Integrations

- **Rock:** `BackgroundCheckComponent` / `BackgroundCheckContainer`, `BackgroundCheckService` and
  the `BackgroundCheck` model, the workflow engine (`ActionComponent`), `RockContext`,
  `Encryption` (encrypted attributes), `SSNFieldType.UnencryptAndClean`, attribute framework, and
  the Rock block/UI framework.
- **Third-party:** Trak-1 REST web service (`api.trak-1.com` / `stgapi.trak-1.com`), `HttpClient`,
  Newtonsoft.Json.
- **Cross-plugin:** none.

## Observations

*Noticed while documenting — not a full audit.*

- **Note:** All Trak-1 calls go through one shared `Trak1Client` (a single `HttpClient` with cookies
  disabled, so no cookie jar is replayed across requests). It checks the HTTP status code and rejects
  an empty, `null` or unreadable body with a `Trak1Exception` (the response body, truncated to 200
  characters, is kept on the exception). `GetPackageList` throws on failure, while a valid `[]` is an
  empty list. Rock's `BackgroundCheckComponent` API is synchronous, so the calls remain synchronous
  (`GetAwaiter().GetResult()`).

## Making Changes

- To change the request payload or which person/address/SSN fields are sent, edit
  `BackgroundCheck/Trak1.cs` (`SendRequest`); the wire shape lives in the `/Helpers/` DTOs.
- To change how status/results are written back to the workflow, edit `Workflow/Trak1Status.cs`
  (and keep its `ReportStatus`/`HitColor`/`ReportUrl` workflow-attribute keys in sync).
- Trak-1 endpoint URLs and credentials are Rock component attributes on the provider — change them
  in the admin UI, not in code; only the attribute *definitions* (and defaults) live in `Trak1.cs`.
- The package list is fetched live from Trak-1; the `Trak1 Packages` block (`org_secc/Trak1/`) is
  display-only.
- HTTP handling, response validation and the polling decisions live in the Rock-free helpers
  (`Trak1Client`, `Trak1StatusUpdate`) so they can be unit tested; keep Rock types out of them.

## Tests

- `org.secc.Trak1.Tests`: xunit over the Rock-free helpers, with a fake `HttpMessageHandler` (no
  Rock database, no Trak-1 calls). It covers the `RecordFound` mapping, the complete-status check,
  the guarantee that nothing is returned to write when a status or report request fails, the report
  being requested only once complete, and `Trak1Client` response validation (packages, vendor errors,
  missing transaction id, truncated bodies). Run with
  `dotnet test org.secc.Trak1.Tests -p:SolutionDir=<path to the Rock solution folder>\`.

Last updated: 2026-10-12
