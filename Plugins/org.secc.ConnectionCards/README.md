# org.secc.ConnectionCards

> A Rock block that ingests a scanned PDF sheet of physical connection cards, slices it into a grid of per-card images, and launches a workflow for each one.

## Overview

ConnectionCards digitizes the paper "connection card" sheets that ministries hand out and collect.
Staff scan a full sheet to PDF and upload it to the block; the block rasterizes the PDF to a PNG,
lets the user rotate and set a row/column grid, then chops the image into one binary file per card
and launches a configured workflow for each card (so they can be reviewed/keyed individually).

## Project Info

- **Project file:** `org.secc.ConnectionCards.csproj`
- **Root namespace:** `org.secc.ConnectionCards`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly + `Ghostscript.NET.dll`) and `RockWeb/Plugins/org_secc/` (block `.ascx` markup)

## Project Layout

```
/org_secc/ConnectionCards/   ConnectionCardEntry  — upload / rotate / crop UI block (.ascx + .ascx.cs)
/Utilities/                  ConnectionCardsUtilties — PDF→image, rotate, grid-chop, whitespace-crop helpers
```

## Components

### Blocks

Category in Rock: **SECC > Connection Cards**.

| Block | Purpose |
|-------|---------|
| Connection Card Entry | Upload a scanned PDF sheet, preview/rotate/crop it, chop it into a grid, and launch a workflow per card. |

Block settings:

| Setting | Type | Notes |
|---------|------|-------|
| **WorkflowType** | WorkflowTypeField | Workflow launched once per chopped card (named "New Connection Card Workflow"); passes `Initiator` = current person alias Guid. |
| **BinaryFileType** | BinaryFileTypeField | Binary file type assigned to the rasterized sheet/cards. |
| **cols** | IntegerField ("Number of Columns") | Default column count for the chop grid (pre-fills the Columns control). |
| **rows** | IntegerField ("Number of Rows") | Default row count for the chop grid (pre-fills the Rows control). |

### Utilities

`ConnectionCardsUtilties` (static helpers used by the block):

| Method | Purpose |
|--------|---------|
| `ConvertPDFToImage` | Rasterizes page 1 of the uploaded PDF to a 96-dpi PNG `BinaryFile` via Ghostscript. |
| `RotateImage` | Rotates the stored image 90/270 and saves it back. |
| `ChopImage` | Splits the image into `cols` x `rows` cells, whitespace-crops each, and persists one `BinaryFile` per cell. |
| `Crop` | Trims surrounding white border from a single card bitmap (scans rows/columns, averaging each pixel's red channel and treating an average > 235 as "white"). |

## Dependencies & Integrations

- **Rock:** `RockContext`, `BinaryFileService` / `BinaryFileTypeService`, `BinaryFile`, the Rock block/UI framework (`RockBlock`, `FileUploader`), and the workflow engine (`LaunchWorkflow`).
- **Third-party:** `Ghostscript.NET` (PDF rasterization — requires the native Ghostscript runtime on the server), `System.Drawing` (image rotate/crop/PNG output).

## Observations

*Noticed while documenting — not a full audit.*

- **Improvement:** `ConvertPDFToImage` only ever rasterizes **page 1** (`rasterizer.GetPage( ..., 1 )`).
  A multi-page scanned PDF silently drops every page after the first — worth confirming sheets are
  always single-page.
- **Improvement:** `fMainSheet_FileUploaded` guards with `mainSheetId != null || mainSheetId != 0`,
  which is always true (an `||` that can never be false). The intended check was likely
  `!= null && != 0`; as written it relies on the later `binaryFileService.Get(...) != null` to bail.
- **Improvement:** The chop loop in `ChopImage` clones `new Rectangle( x + 4, y + 4, elementWidth - 4, elementHeight - 4 )`
  and `Crop` calls `GetPixel` per pixel across the whole cell. For large/high-dpi sheets this is slow
  and the `+4`/`-4` insets can throw if a cell is near the image edge; review bounds for non-evenly-divisible sizes.
- **Improvement:** `Ghostscript.NET` depends on a native Ghostscript install being present on the
  server; failures there surface only at upload time. Worth documenting the server prerequisite.
- **Improvement:** `RotateImage` writes the rotated PNG into a `MemoryStream` declared in a `using`
  block, assigns it to `inputFile.ContentStream`, then calls `SaveChanges()` — the stream is
  disposed when the `using` exits, so whether the rotated bytes persist depends on Rock reading the
  stream before disposal. Worth confirming rotation actually survives a save.

## Making Changes

- The upload → rotate → crop → chop flow lives in `org_secc/ConnectionCards/ConnectionCardEntry.ascx.cs`;
  the image math (rasterize, rotate, chop, whitespace-crop) lives in `Utilities/ConnectionCardsUtilties.cs`.
- The per-card workflow is configured via the block's **WorkflowType** setting and launched in
  `btnCrop_Click`; the downstream review/keying logic lives in that workflow, not in this plugin.
  See [org.secc.Workflow](../org.secc.Workflow/README.md) for related workflow actions.
