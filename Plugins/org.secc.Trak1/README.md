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
/Helpers/           DTOs (request/response): Trak1Authentication, Trak1Applicant, Trak1Request,
                    Trak1Response, Trak1Package, Trak1Component, Trak1RequiredField, Trak1Report,
                    Trak1ReportStatus, ErrorInformation
/org_secc/Trak1/    Trak1Settings block (.ascx + .ascx.cs) — lists available Trak-1 packages
/Properties/        AssemblyInfo.cs
```

## Components

### Background Check Provider

`BackgroundCheck/Trak1.cs` — a `BackgroundCheckComponent` (MEF `[Export]`, `ComponentName "Trak-1"`).
`SendRequest` resolves the workflow's person, gathers a package's required fields, builds a
`Trak1Request`, POSTs it to the Request URL, and creates/updates a Rock `BackgroundCheck` row keyed
to the workflow. Configuration is per-provider via Rock component attributes:

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
"Trak-1 Status"`. Looks up the `BackgroundCheck` row by workflow Id, calls the provider's Status and
Report URLs, and writes the results back.

| Setting | Type | Notes |
|---------|------|-------|
| **Provider** | component (`Rock.Security.BackgroundCheckContainer`) | The background-check provider to query. |
| **ReportStatus** | workflow attribute (text) | Receives the Trak-1 `ReportStatus`. |
| **HitColor** | workflow attribute (text) | Receives the Trak-1 `HitColor`; `RecordFound` is set true when `"Green"`. |
| **ReportUrl** | workflow attribute (text) | Receives the report URL. |

If no `BackgroundCheck` row exists for the workflow, the action logs and sets ReportStatus to
`"Error"` (returns `true` so the workflow can handle it gracefully).

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

- **Security (medium):** `SendRequest` transmits SSN, date of birth, and home address to Trak-1.
  The DTOs are POSTed over whatever scheme the configured URL uses; confirm the production Request
  URL is HTTPS (the shipped defaults are `https://stgapi.trak-1.com`, a **staging** host — verify
  production deploys repoint these to the live endpoint).
- **Security (low):** The subscriber/company codes are decrypted and embedded directly in the URL
  path for `GetPackageList` and `Trak1Status` (`/{subscriberCode}/{companyCode}/...`). URLs can be
  logged by proxies/servers; worth confirming Trak-1 expects credentials in the path and that
  request logging won't capture them.
- **Improvement:** Every Trak-1 call (the `RunPackage` POST, `GetAvailablePackages`, and both the
  status and report GETs in `Trak1Status`) uses `.Result` on async `HttpClient` calls
  (sync-over-async) and constructs a new `HttpClient` per call. Functional for a low-volume workflow
  step but can deadlock under some sync contexts and leaks sockets under load.
- **Improvement:** In `SendRequest`, `person.LoadAttributes(rockContext)` is called immediately
  after the `PersonAliasService` query without a null guard, so a missing/invalid person alias would
  NRE before the explicit `person == null` check below it.
- **Improvement:** Response handling assumes well-formed JSON — `response.Error?.Message` is guarded,
  but `response`, `status`, and the deserialized report are dereferenced without null checks, so a
  non-JSON or error HTTP body would throw rather than surface a clean error message.

## Making Changes

- To change the request payload or which person/address/SSN fields are sent, edit
  `BackgroundCheck/Trak1.cs` (`SendRequest`); the wire shape lives in the `/Helpers/` DTOs.
- To change how status/results are written back to the workflow, edit `Workflow/Trak1Status.cs`
  (and keep its `ReportStatus`/`HitColor`/`ReportUrl` workflow-attribute keys in sync).
- Trak-1 endpoint URLs and credentials are Rock component attributes on the provider — change them
  in the admin UI, not in code; only the attribute *definitions* (and defaults) live in `Trak1.cs`.
- The package list is fetched live from Trak-1; the `Trak1 Packages` block (`org_secc/Trak1/`) is
  display-only.
