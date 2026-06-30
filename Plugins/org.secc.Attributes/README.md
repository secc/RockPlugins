# org.secc.Attributes

> Three custom Rock attribute field types — a cascading (dependent) dropdown, a typed phone-number picker, and a multi-file uploader — with their edit controls and `[FieldAttribute]` decorators.

## Overview

This plugin adds custom **field types** to Rock's attribute framework. Each field type plugs into the
standard Rock attribute editor (configure once, then attach to any entity) and provides its own edit
control and value-formatting logic. The three types are a **cascading dropdown** (chained dropdowns
filtered by the prior selection, driven by a key^value matrix or a SQL query), a **dynamic phone
number** (phone-type defined value + number), and a **multi-file upload** (multiple `BinaryFile`s
stored behind a single attribute via an `AttributeMatrix`). Two of them ship a strongly-typed
`FieldAttribute` subclass so they can be applied to blocks/components in code.

## Project Info

- **Project file:** `org.secc.Attributes.csproj`
- **Root namespace:** `org.secc.Attributes`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly + `.pdb`, via the PostBuildEvent `xcopy`)

## Project Layout

```
/FieldTypes/   The three Rock FieldType implementations (config / format / edit-value)
/Controls/     The IRockControl edit controls each field type renders
/Attributes/   FieldAttribute subclasses for applying the field types in code
/Helpers/      KeyValueList / KeyValueMatrix — parsing + filtering for the cascading dropdown
```

## Components

### Field Types

Each extends `Rock.Field.FieldType` and is registered with Rock by class name (no startup hook).

| Field type (class) | Stored value | Config keys | Edit control |
|--------------------|--------------|-------------|--------------|
| `CascadingDropDownFieldType` | `\|`-joined selected keys (one per level) | `configuration` (the data matrix) | `CascadingDropDownList` |
| `DynamicPhoneNumberFieldType` | `{phoneTypeDefinedValueId}\|{number}` | (none) | `DynamicPhoneNumberPicker` |
| `MultiFileFieldType` | an `AttributeMatrix` Guid (rows reference `BinaryFile`s) | `binaryfiletype`, `maxfiles`, `allowedextensions` | `MultiFileUpload` |

**`CascadingDropDownFieldType`** — one config textarea (**`configuration`**, the "Data Matrix")
defines the option tree. `KeyValueMatrix` parses it three ways (tried in order): JSON, a SQL `SELECT`
(detected by containing both `SELECT` and `FROM`; each row column is a `key^value` pair, run via
`Rock.Data.DbService.GetDataTable`), or a plain matrix where each **line** is a row and the row's
levels are **comma-separated** `key^value` cells. The control renders one dropdown per matrix column;
selecting a value filters the next dropdown's options to descendants of that key.

**`DynamicPhoneNumberFieldType`** — no configuration. The picker pairs a phone-type dropdown (sourced
from the `PERSON_PHONE_TYPE` defined type) with a `PhoneNumberBox`. `FormatValue` resolves the stored
defined-value Id back to its label (e.g. `Mobile: 555-1234`).

**`MultiFileFieldType`** — the most involved. Uploaded files become `BinaryFile`s; the attribute value
is the Guid of an `AttributeMatrix` whose items each hold one file in a `File` column. On first use it
auto-provisions a single shared `AttributeMatrixTemplate` (well-known Guid `D95683B6-…-F48A79340175`,
cached in a static field). `GetEditValue` reconciles rows against the picker's current file Ids
(adds new, deletes removed). Display is split across two overrides so rich markup only reaches HTML
surfaces: `FormatValue` is **link-only** (a list of `GetFile.ashx` download links, or an `"N files"`
count when condensed) — this feeds CSV/grid exports and Lava merge fields, which must not receive
embedded media. `FormatValueAsHtml` does the inline embedding for on-page HTML display: video files
embed as a native `<video>` player and images as an `<img>` thumbnail, each with its download link
directly beneath; non-media files render as a plain link. SVG is **never** embedded (it can carry
inline script) and falls back to a link. Condensed/grid contexts still show an `"N files"` count
rather than embedded media. Config attributes:

| Setting | Type | Notes |
|---------|------|-------|
| **`binaryfiletype`** | Binary File Type (Guid) | Required. Which `BinaryFileType` uploads are stored under. |
| **`maxfiles`** | int | `0` = unlimited. Enforced client-side before upload. |
| **`allowedextensions`** | csv | Comma-separated extension whitelist (e.g. `pdf,docx,jpg`); blank = any. Enforced client-side. |

### Edit Controls

| Control | Implements | Renders |
|---------|------------|---------|
| `CascadingDropDownList` | `CompositeControl`, `IRockControl` | N chained `RockDropDownList`s, re-filtered on `SelectedIndexChanged` postback. |
| `DynamicPhoneNumberPicker` | `CompositeControl`, `IRockControl` | Phone-type `RockDropDownList` + `PhoneNumberBox` in a Bootstrap row. |
| `MultiFileUpload` | `CompositeControl`, `IRockControl` | HTML5 multi-file `<input>` + a server-rendered removable file list; uploads each file to `/FileUploader.ashx` over XHR and tracks resulting Ids in a hidden field. |

### Field Attributes

`FieldAttribute` subclasses that let the field types be declared on a block/component in code.

| Attribute | Applies | Notes |
|-----------|---------|-------|
| `CascadingDropDownFieldAttribute` | `CascadingDropDownFieldType` | Takes a `matrix` ctor arg, stored as the `configuration` value. `AllowMultiple`, `Inherited`. |
| `DynamicPhoneNumber` | `DynamicPhoneNumberFieldType` | Thin wrapper; no extra config. |

(`MultiFileFieldType` has no companion `FieldAttribute`; it is applied through the Rock admin UI.)

## Dependencies & Integrations

- **Rock:** `Rock.Field.FieldType`, the attribute/qualifier framework, `IRockControl` + `RockControlHelper`,
  `RockDropDownList` / `PhoneNumberBox` / `NumberBox`, defined-type cache (`PERSON_PHONE_TYPE`),
  `BinaryFile` / `BinaryFileType`, `AttributeMatrix` / `AttributeMatrixTemplate` / `AttributeMatrixItem`,
  `FieldTypeCache` / `EntityTypeCache`, `Rock.Data.DbService` (raw SQL), and the `/FileUploader.ashx` /
  `/GetFile.ashx` handlers.
- **Third-party:** Newtonsoft.Json (matrix (de)serialization), EntityFramework (transitive via Rock).
- No REST endpoints, jobs, migrations, or Lava filters.

## Observations

*Noticed while documenting — not a full audit.*

- **Security (medium):** `CascadingDropDownFieldType`'s "Data Matrix" config can be a raw SQL `SELECT`
  executed via `DbService.GetDataTable( config, CommandType.Text, null )` (any string containing
  `SELECT` and `FROM`). This runs with the app's DB credentials. The config is only editable by users
  with attribute-edit rights, so the threat surface is limited to admins, but it is effectively
  arbitrary SQL stored in an attribute qualifier — worth confirming who can edit these attributes and
  that the SQL is trusted.
- **Security (low):** `MultiFileFieldType`/`MultiFileUpload` enforce the allowed-extension whitelist
  and max-file count **client-side only** (JS pre-checks before the XHR upload). The actual
  `/FileUploader.ashx` upload and `BinaryFileType` rules are the real gate; a crafted request could
  bypass the client checks. Confirm the chosen `BinaryFileType` has appropriate server-side
  restrictions if extension filtering matters for security (vs. UX).
- **Improvement:** `MultiFileFieldType.GetEditValue` performs several `SaveChanges()` calls and
  per-row `LoadAttributes()` inside a loop — workable for a handful of files, but chatty for large
  sets. The shared-template Id is cached statically (`_sharedTemplateIdCache`), which is fine, but the
  template is never re-validated if deleted out from under the cache.
- **Improvement:** Removing files from a `MultiFileUpload` deletes the `AttributeMatrixItem` rows but
  does not delete the orphaned `BinaryFile`s themselves; they remain in storage unless cleaned up
  elsewhere.

## Making Changes

- To add a new field type, drop a `FieldType` subclass in `/FieldTypes/`, give it an edit control in
  `/Controls/` implementing `IRockControl`, and (optionally) a `FieldAttribute` subclass in
  `/Attributes/`; register it as a Rock Field Type in the admin UI. Add all files to the `.csproj`
  `<Compile>` list. Follow an existing trio (e.g. `DynamicPhoneNumber*`) as a template.
- Cascading-dropdown parsing/filtering lives entirely in `Helpers/KeyValueMatrix.cs`
  (`BuildFromConfiguration` for parsing, `FormatValue` for display).
- The multi-file template provisioning and row reconciliation live in `MultiFileFieldType`
  (`EnsureSharedTemplate`, `GetEditValue`); the client-side upload/remove behavior is the inline JS in
  `Controls/MultiFileUpload.cs` (`RegisterClientScript`).

---

**Last updated:** 2026-06-29
</content>
</invoke>
