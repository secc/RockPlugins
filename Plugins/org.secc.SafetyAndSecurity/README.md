# org.secc.SafetyAndSecurity

> Campus lock-down SMS alerting plus workflow actions for volunteer background-check applications, incident-report PDFs, and MinistrySafe training.

## Overview

SafetyAndSecurity bundles Southeast's safety/security tooling: two Rock UI blocks that let
staff fire a campus lock-down SMS blast (and send follow-up / all-clear updates), a pair of
EF-backed models that record those alerts, and a set of workflow actions that drive the
volunteer-application background-check process — validating references, storing an encrypted SSN,
filling and flattening confidential PDF forms (volunteer application, minor application, medical
and digital incident reports, external church reference), and creating/updating users in the
external MinistrySafe training API. Used by Safety & Security and volunteer-screening staff.

## Project Info

- **Project file:** `org.secc.SafetyAndSecurity.csproj`
- **Root namespace:** `org.secc.SafetyAndSecurity`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` — the PostBuildEvent runs
  `xcopy ...bin\Debug\org.secc.SafetyAndSecurity.* ...RockWeb\bin` (assembly only).
  The block `.ascx`/`.ascx.cs` files under `org_secc/SafetyAndSecurity/` are **not** referenced
  by this `.csproj` and are **not** copied by the build event; they live in the RockWeb source
  tree (`RockWeb/Plugins/org_secc/SafetyAndSecurity/`) and are compiled there.

## Project Layout

```
/Data/         SafetyAndSecurityContext (EF DbContext) + SafetyAndSecurityService base
/Model/        AlertNotification / AlertMessage entities + their *Service classes
/Migrations/   EF plugin migration (001_Init) that stands up the two alert tables
/Workflows/    Workflow ActionComponents (PDF merges, validation, SSN, MinistrySafe)
/org_secc/SafetyAndSecurity/   UI blocks (.ascx + .ascx.cs): CampusLockDown, CampusLockDownAlerts
```

## Components

### Blocks

Category in Rock: **SECC > Safety And Security**.

| Block | Purpose | Key settings |
|-------|---------|--------------|
| Campus Lock Down | Pick a campus, then send a lock-down SMS to a defined-value audience (staff only or staff + volunteers); supports a custom-message modal. | **LockdownTitle**, **LockdownAlert**, **DefinedType** (audience defined type), **From** (SMS from number) |
| Campus Lock Down Active Alerts | Lists active alerts and their messages; lets staff send an update or an "All Clear" (which deactivates the alert). | **StandardAlert**, **Default Review DataView** |

Audience selection keys off the configured **DefinedType**: each defined value carries `Campus`
and `HasVolunteer` attributes, and the block matches the selected campus + volunteer flag to one
defined value, whose `DataView` attribute resolves the recipient query.

### Workflow Actions

All under action category **SECC > Safety and Security** (`ActionComponent` exports).

| Component (class) | Purpose | Key settings |
|-------------------|---------|--------------|
| Volunteer Application Merge (`VolunteerApplicationMerge`) | Fill + flatten the adult/minor confidential volunteer-application PDF from ~80 workflow attributes; saves as a background-check binary file. | Adult/Minor Application PDF (binary files), **IsMinorApplication** |
| Minor Volunteer Application Merge (`MinorVolunteerApplicationMerge`) | Minor-specific volunteer-application PDF merge. | Minor Application PDF |
| Medical Incident Report Merge (`MedicalIncidentReportMerge`) | Fill + flatten the medical incident report PDF. | Report PDF, **binaryFileType** |
| Digital Incident Report Merge (`DigitalIncidentReportMerge`) | Fill + flatten the digital incident report PDF. | Report PDF, **binaryFileType** |
| External Church Reference Merge (`ExternalChurchReferenceMerge`) | Fill + flatten the external-church reference PDF. | Reference PDF, **binaryFileType** |
| Volunteer Application Validation (`VolunteerApplicationValidation`) | Per-step server-side validation (personal info, each reference, special circumstances, statement of faith); branches to a success/fail activity. | **Action Step**, **Error Messages**, Success/Fail Activity, **IsMinorApplication** |
| Reference Validation (`ReferenceValidation`) | Validate one "Get References" entry (name/phone/email/address) and reject duplicates across references. | **Error Messages**, **IsMinorApplication**, **ReferenceCount**, **MaxReferenceLimit** |
| Volunteer Application SSN (`VolunteerApplicationSSN`) | Validate the SSN (regex), store it encrypted on the person's `SSN` attribute, then mask the workflow's SSN fields. | Success Activity |
| MinistrySafe Create (`MinistrySafeRequest`) | POST a person to the MinistrySafe API, then GET their `direct_login_url` back into a workflow attribute. | **Person**, **MinistrySafe URL**, **MinistrySafe Tags** |
| MinistrySafe Update (`MinistrySafeUpdate`) | GET a MinistrySafe user's score / completion date into workflow attributes. | **Person**, **Score**, **Completion Date** |

### Models

EF entities stored in the Rock database (Rock `IRockEntity` / `ISecured`).

| Model | Table | Notes |
|-------|-------|-------|
| `AlertNotification` | `_org_secc_SafetyAndSecurity_AlertNotification` | An alert: Title, IsActive, AudienceValue (DefinedValue), AlertNotificationTypeValue (DefinedValue), child AlertMessages. |
| `AlertMessage` | `_org_secc_SafetyAndSecurity_AlertMessage` | A message belonging to an alert; `SendCommunication(fromGuid)` resolves the audience's `DataView` to recipients and sends a `RockSMSMessage`. |

## Dependencies & Integrations

- **Rock:** `RockContext`, Rock workflow engine (`ActionComponent`), plugin migrations
  (`Rock.Plugin`), defined types/values, DataViews, `Encryption`, `RockSMSMessage`
  (Rock.Communication), binary-file services, the Rock block/UI framework, EF (`Rock.Data.DbContext`).
- **Third-party:** iText7 (`itext.*`) for PDF form fill/flatten, RestSharp for the MinistrySafe
  HTTP calls, Newtonsoft.Json. The `.csproj` also references `Rock.SignNow` / `SignNowSDK` /
  `CSLibrary` (CudaSign), though no code in this project currently calls them.
- **External APIs:** MinistrySafe (`MinistrySafeAPIURL` + `MinistrySafeAPIToken` Global Attributes).

## Migrations

Ships one EF plugin migration under `/Migrations/`:

- `001_Init` (`MigrationNumber 1`, `1.10.2`) — creates `_org_secc_SafetyAndSecurity_AlertNotification`
  and `_org_secc_SafetyAndSecurity_AlertMessage` with their FKs (DefinedValue, PersonAlias) and indexes.

## Observations

*Noticed while documenting — not a full audit.*

- **Security (medium):** This plugin handles highly sensitive data — Social Security Numbers,
  background-check applications, and medical/incident reports. `VolunteerApplicationSSN` and
  `VolunteerApplicationValidation` decrypt SSN parts and re-store the SSN encrypted, which is good;
  worth confirming the binary-file types the PDF merges write to (`5C701472-…`, `9510D232-…`,
  `E68932AD-…`, `A72B7149-…`) are locked down so the rendered confidential PDFs aren't broadly readable.
- **Security (low):** `CampusLockDown` hard-codes `int alertTypeId = 31480` (a DefinedValue Id) rather
  than resolving by Guid. Ids are environment-specific, so this will break or mis-tag alerts on any
  database where that value Id differs (e.g. a restored/rebuilt environment). Worth switching to a
  Guid-based lookup or a block setting.
- **Improvement:** The MinistrySafe API token is read from a Global Attribute and sent as
  `Authorization: Token token=…`. Confirm `MinistrySafeAPIToken` is stored as an encrypted/field-type
  global attribute and not broadly readable.
- **Improvement:** `MinistrySafeRequest` declares the **same `MinistrySafe URL` `WorkflowAttribute`
  twice** (duplicate attribute decoration). Harmless but should be deduped.
- **Improvement:** Several actions `new RockContext()` internally instead of using the passed-in
  `rockContext` (e.g. `MinistrySafe*` person-alias lookups, `ReferenceValidation` location lookup),
  which can split work across contexts. `AlertMessage.GetRecipients` also spins up its own context.
- **Improvement:** `AssemblyInfo.cs` still carries the scaffolded `AssemblyCompany("Microsoft")` /
  `Copyright © Microsoft 2016` despite the Southeast copyright header — worth correcting.

## Making Changes

- To change lock-down behavior or who receives an alert, edit
  `org_secc/SafetyAndSecurity/CampusLockDown.ascx.cs` (audience matching lives in `GetAudience`,
  send logic in `CreateAlert` → `AlertMessage.SendCommunication`); update/all-clear logic is in
  `CampusLockDownAlerts.ascx.cs`.
- To add or adjust a workflow action, drop/edit an `ActionComponent` under `/Workflows/`, decorate it
  with `[ActionCategory]` / `[ExportMetadata("ComponentName", …)]` and its `[WorkflowAttribute]` config,
  and follow a sibling as a template. PDF field mapping for the volunteer app lives in
  `VolunteerApplicationMerge.BuildFieldDictionary` (PDF form field name → workflow attribute).
- New supporting data or schema belongs in a new numbered migration under `/Migrations/` — don't
  hand-edit `001_Init` once it has run.
- Related: workflow-engine helpers and other custom actions live in
  [org.secc.Workflow](../org.secc.Workflow/README.md).
