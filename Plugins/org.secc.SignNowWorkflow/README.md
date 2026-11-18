# org.secc.SignNowWorkflow

> Two Rock workflow actions that push a PDF into SignNow for e-signature and pull the signed copy back when it's done.

## Overview

SignNowWorkflow ships a pair of `ActionComponent`s (under the **SECC > Sign Now** category) used by
Southeast's volunteer-application / background-check flow. `SignNow Create` uploads a rendered PDF to
SignNow, creates a guest signer, and produces a signing invite link; `SignNow Download` polls the
document and, once a signature is present, downloads the signed PDF back into a Rock binary-file
attribute. Both lean on Rock's built-in `Rock.SignNow` integration for account access tokens and on
the `SignNowSDK` project for the actual API calls.

## Project Info

- **Project file:** `org.secc.SignNowWorkflow.csproj`
- **Root namespace:** `org.secc.SignNowWorkflow`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly + `.pdb`, via the PostBuildEvent `xcopy`)

## Project Layout

```
/Workflows/    SignNowCreate.cs    — upload PDF, create signer, build invite link
               SignNowDownload.cs  — check for a signature, download the signed PDF
/Properties/   AssemblyInfo.cs
/org.secc.SignNowWorkflow.Tests/  xunit tests for SignNowCreate's temp file path and SignNowDownload's
                                  document id check and error messages (not deployed to RockWeb)
```

## Components

### Workflow Actions

Both are `ActionComponent`s exported via MEF (`[Export(typeof(ActionComponent))]`) under
`ActionCategory("SECC > Sign Now")`.

| Action (`ComponentName`) | Purpose |
|--------------------------|---------|
| `SignNow Create` | Reads the `Document` attribute's binary file, writes it to a temp path, uploads it to SignNow (`Document.Create`), creates a throwaway guest signer + OAuth token, sends an invite (`Document.Invite`, email disabled), and stores the document id and a `dispatch` invite link on workflow attributes. Returns `false` (no-op) when there is no `HttpContext` (i.e. not browser-initiated). |
| `SignNow Download` | Looks up the SignNow document by id (`Document.Get`); if it has any `signatures`, downloads the file (`Document.Download`) into a per-call temp directory and stores it into the `Document` binary-file attribute (creating the `BinaryFile` and honoring the attribute's `binaryFileType` qualifier, or replacing the content of the file already there), then sets `PDF Signed` = `True`. Otherwise sets `PDF Signed` = `False`. |

#### `SignNow Create` — attributes

| Setting | Type | Notes |
|---------|------|-------|
| **SignNow Invite Link** | Workflow attribute (Text) | Output — the generated `signnow.com/dispatch?route=fieldinvite…` URL. |
| **SignNow Document Id** | Workflow attribute (Text) | Output — the SignNow document id. |
| **Document** | Workflow attribute (BinaryFile/File) | Input — the PDF to upload. |
| **Redirect Uri** | Text (Lava-enabled) | Optional. Page to return to after signing; `document_id` is appended and the URI is URL-encoded onto the invite link. |
| **Signer Role** | Text | Role the signature is assigned to (default `Applicant`). |

#### `SignNow Download` — attributes

| Setting | Type | Notes |
|---------|------|-------|
| **SignNow Document Id** | Workflow attribute (Text) | Input — the SignNow document id to check (required). Surrounding whitespace is ignored. The action errors without calling SignNow if the value is blank or is not 40 ASCII letters, digits or underscores (the SignNow document id format). |
| **Document** | Workflow attribute (BinaryFile/File) | Output — where the signed PDF is stored. |
| **PDF Signed** | Workflow attribute (Boolean) | Output — `True` once a signature is detected, else `False`. |

## Dependencies & Integrations

- **Rock:** workflow engine (`ActionComponent`, `WorkflowAttribute`, `WorkflowAction`), `RockContext`,
  `BinaryFileService` / `BinaryFileTypeService`, `AttributeCache`, MEF (`System.ComponentModel.Composition`).
- **Rock.SignNow:** `Rock.SignNow.SignNow` — supplies the account-level OAuth access token
  (`GetAccessToken`) from Rock's SignNow configuration.
- **SignNowSDK:** `Document.Create` / `Invite` / `Get` / `Download`, `User.Create`, `OAuth2.RequestToken`.
- **Third-party:** Newtonsoft.Json 11.0.2 (`JObject`/`JArray` response parsing), SignNow REST API.
- **Cross-plugin:** [org.secc.DevLib](../org.secc.DevLib/README.md) — `ReadContentBytes` extension used by
  `SignNow Create` to read the document through its storage provider.
- **Other project refs:** `Rock.Common`, and `DotLiquid` (Lava `ResolveMergeFields` on the **Redirect Uri**).
- **Other:** EntityFramework 6.1.3 (referenced; no plugin migrations shipped).

## Observations

*Noticed while documenting — not a full audit.*

- **Temp-file handling (ROCK-9041):** `SignNow Download` has `Document.Download` write the signed PDF into
  a new directory unique to each call (`Path.GetTempPath()` + `Path.GetRandomFileName()`). It checks the
  SDK result for the saved `file` path (a SignNow error adds an error message instead of throwing), reads
  the bytes into memory, and deletes the directory in a `finally` before any database work; a failed
  delete is logged, not thrown. The bytes go to `signedPDF.ContentStream` as a `MemoryStream` and
  `SaveChanges()` runs in both the new-`BinaryFile` and existing-`BinaryFile` branches. New files use the
  file type named by the attribute's `binaryFileType` qualifier and fall back to the Default file type
  only when the qualifier is blank (otherwise Rock silently drops the content); a qualifier that names a
  missing file type is an error, not a fallback. The destination attribute and file type are resolved
  before the download, so a misconfigured action fails without calling SignNow. The stored file name gets
  a `.pdf` extension, and the `%PDF-` header is accepted anywhere in the first 1024 bytes.
  `SignNow Create` writes the upload into a per-call temp directory under the document's file name, with
  each run of characters that are invalid in file names replaced by `_` and commas and semicolons removed
  (Rock's `MakeValidFileName`), or as `document.pdf` when no usable name remains.
  `SignNow Create` also removes its upload temp directory in a `finally`, including on error returns.
- **SDK error handling:** SignNowSDK uses RestSharp 105, which never throws on transport failures. The SDK
  then returns `null`, or the raw error body (object, array, or non-JSON), instead of the expected object.
  The `Document.Get`, `Download`, and `Create` calls treat the result as untyped, describe `null` as
  "No response from SignNow.", and catch `JsonException` for non-JSON bodies. `SignNow Download`
  shows a fixed message ("SignNow Document Error: the document could not be retrieved from
  SignNow." or "SignNow Download Error: the signed document could not be downloaded from SignNow.") and
  writes the detail to Rock's exception log (Exception List): the entry starts with the same message,
  then names the action type id and, once the workflow is saved, the workflow id, followed by the SignNow
  response (or the first 500 bytes of a download that isn't a PDF); a caught exception is logged as its
  inner exception. `Document.Download` sends the request
  twice and saves the second body without a status check, so `SignNow Download` only accepts the file if
  it has the `%PDF-` header; otherwise it errors and leaves `PDF Signed` unchanged so the workflow keeps
  polling. The existing-file branch also resets `MimeType` to `application/pdf`.
- **Improvement:** The "is it signed?" check is a simple `signatures.Count > 0` poll with no timeout or
  expiration handling — the workflow must re-run `SignNow Download` itself (e.g. on a delay/loop) until a
  signature appears. There's no detection of a declined/expired invite.
- **Improvement:** `SignNow Download` hardcodes `MimeType = "application/pdf"` (flagged with a `// TODO` in
  the source) and assumes the downloaded file is a PDF.

## Making Changes

- Both actions live in `/Workflows/`; to change inputs/outputs, edit the `[WorkflowAttribute]` /
  `[TextField]` declarations at the top of `SignNowCreate.cs` / `SignNowDownload.cs` and keep the
  `GetActionAttributeValue(action, "…")` keys in sync.
- The actual SignNow API surface is **not** in this plugin — it's in the solution's `SignNowSDK` and
  `Rock.SignNow` projects; account credentials/config come from Rock's built-in SignNow integration.
- New actions: add another `ActionComponent` in `/Workflows/`, decorate it with `[ActionCategory]`,
  `[Description]`, `[Export(typeof(ActionComponent))]`, and `[ExportMetadata("ComponentName", …)]`, then
  add it to the `<Compile>` list in the `.csproj`.

Last updated: 2026-10-16
