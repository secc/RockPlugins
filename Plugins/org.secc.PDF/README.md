# org.secc.PDF

> Workflow actions that generate PDFs — render Lava/HTML to PDF, fill a PDF form template, and combine two PDFs — outputting the result to a binary-file workflow attribute.

## Overview

PDF adds a set of Rock workflow action components (category **PDF**) for producing documents inside
a workflow. It can render an HTML/Lava template to a PDF (with header/footer), merge workflow data
into a fillable PDF form template, and concatenate two existing PDFs into one. Each action writes its
output to a Rock binary file and stores the file Guid on a workflow/activity attribute. Two example
RockWeb blocks demonstrate driving these actions from a page.

## Project Info

- **Project file:** `org.secc.PDF.csproj`
- **Root namespace:** `org.secc.PDF`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` only — the post-build `xcopy` copies `org.secc.PDF.dll`,
  `NReco.PdfGenerator.*`, and `iText*.*`. The example blocks under `org_secc/PDFExamples/` are **not**
  referenced by the `.csproj` (neither compiled nor deployed); they exist as standalone source only.

## Project Layout

```
/Workflows/    Workflow action components — LavaPDF, PDFFormMerge, PDFCombine, InsertXHTMLLava
/Helpers/      PDFWorkflowObject  — entity passed to a workflow carrying Lava input + merge objects
/Utilities/    Utility            — HtmlToPdf (NReco) and workflow-attribute helpers
/org_secc/PDFExamples/   Two example RockWeb blocks (PDF Lava Example, PDF Form Example)
```

## Components

### Workflow Actions

Category in Rock: **PDF**. All write output to a `FileFieldType` workflow attribute.

| Action (ComponentName) | Class | Purpose |
|------------------------|-------|---------|
| Lava PDF | `LavaPDF` | Resolves a Lava/HTML template (plus optional header/footer) and renders it to a PDF via NReco. |
| PDF Form Merge | `PDFFormMerge` | Fills the form fields of a PDF template (iText AcroForm); supports Lava in unmatched field values and optional flattening. |
| PDF Combine | `PDFCombine` | Merges two PDF binary files into one (iText `PdfMerger`); validates both are `application/pdf`. |
| Insert XHTML and Lava | `InsertXTMLLava` | Stores the configured XHTML/Lava into the workflow `XHTML` attribute (ensures helper attributes exist first). |

#### Lava PDF — settings

| Setting | Type | Notes |
|---------|------|-------|
| **Lava** | CodeEditor (Lava) | The Lava/HTML body to render. |
| **Header** | CodeEditor (Lava) | Optional page-header HTML/Lava. |
| **Footer** | CodeEditor (Lava) | Optional page-footer HTML/Lava. |
| **PDF** | Workflow attribute (`FileFieldType`) | Destination binary-file attribute. |
| **DocumentName** | Text (Lava) | Output filename; defaults to `LavaDocument.pdf`. |

#### PDF Form Merge — settings

| Setting | Type | Notes |
|---------|------|-------|
| **PDFTemplate** | BinaryFile | The fillable PDF template to merge into. |
| **PDFOutput** | Workflow attribute | Destination for the rendered PDF (skipped when run with a `PDFWorkflowObject` entity). |
| **Flatten** | Boolean | If true, flattens the form fields (locks them) after merge. |

Merge keys come from `PDFWorkflowObject.MergeObjects`; when an entity isn't supplied, the action
builds merge objects from the workflow/activity attribute values (`new PDFWorkflowObject(action, rockContext)`).

#### PDF Combine — settings

| Setting | Type | Notes |
|---------|------|-------|
| **PDFFirstFile** | Workflow attribute (`FileFieldType`) | First PDF to combine. |
| **PDFSecondFile** | Workflow attribute (`FileFieldType`) | Second PDF, appended after the first. |
| **PDFOutputFile** | Workflow attribute (`FileFieldType`) | Destination for the combined PDF. |

### Blocks (examples)

Category in Rock: **CMS**. Demonstration `RockBlock`s under `org_secc/PDFExamples/`. Note these are
**not included in the `.csproj`**, so they are not compiled into the shipped assembly — treat them as
reference source for how to drive the actions, not as deployed blocks.

| Block | Purpose | Key settings |
|-------|---------|--------------|
| PDF Lava Example | Edits a Lava/HTML body, activates a workflow to render it, redirects to the generated PDF. | Workflow Type, Workflow Activity |
| PDF Form Example | Uploads/selects a merge-template PDF, supplies key/value merge fields, runs a workflow to fill it. | Workflow Type, Workflow Activity |

### Helpers / Models

| Type | Purpose |
|------|---------|
| `PDFWorkflowObject` | Plain object passed as the workflow entity: carries `PDF` (BinaryFile), `LavaInput`, and `MergeObjects`. Resolves attribute values to entities when the field type is an `IEntityFieldType`. |
| `Utility.HtmlToPdf` | Wraps NReco `HtmlToPdfConverter` (Letter size, optional header/footer/margins/orientation) and returns a `BinaryFile`. |
| `Utility.EnsureAttributes` | Creates the `PersonId`, `RegistrationRegistrantId`, `GroupMemberId`, `PDFGuid`, and `XHTML` workflow attributes if absent. |

## Dependencies & Integrations

- **Rock:** `RockContext`, `BinaryFileService` / `BinaryFileTypeService`, the workflow engine
  (`ActionComponent`), `AttributeCache`, Lava (`ResolveMergeFields`), `RockBlock` (examples).
- **Third-party:** `NReco.PdfGenerator` 1.1.15 (HTML-to-PDF, wkhtmltopdf-based), `itext7` 7.0.1
  (form merge + PDF combine), EntityFramework 6.1.3.

## Observations

*Noticed while documenting — not a full audit.*

- **Improvement:** `Utility.EnsureAttributes` / `CreateAttribute` create workflow attributes at
  **runtime** via `SaveChanges()` and `AttributeCache.Clear()` during action execution. Mutating the
  attribute schema and clearing the cache on every run is a side effect worth confirming, and is only
  invoked by `InsertXTMLLava`.
- **Improvement:** The `InsertXHTMLLava` class is named `InsertXTMLLava` (typo) and its
  `ComponentName` is "Insert XHTML and Lava" — the action stores XHTML to a workflow attribute but
  does no PDF rendering itself, so it must be paired with another action. Worth confirming it's still
  used.
- **Improvement:** NReco's `HtmlToPdfConverter` shells out to a bundled wkhtmltopdf binary; the
  `Lava PDF` action renders user/admin-authored Lava+HTML. Since templates are authored by trusted
  staff this is low-risk, but be aware external HTML/remote resources in a template are fetched by the
  renderer. Review template sources if untrusted input ever reaches them.
- **Improvement:** `LavaPDF` does not null-check the result of `BinaryFileTypeService.Get(...)` when a
  `binaryFileType` qualifier is present, so a misconfigured destination attribute could throw.

## Making Changes

- To change PDF generation behavior, edit the matching action under `/Workflows/`; HTML-to-PDF
  conversion settings (page size, margins, orientation) live in `Utility.HtmlToPdf`.
- New actions are discovered via MEF — add a class deriving from `ActionComponent` with
  `[ActionCategory("PDF")]` / `[Export(typeof(Rock.Workflow.ActionComponent))]` /
  `[ExportMetadata("ComponentName", ...)]` and configure settings with Rock attribute decorators.
- The example blocks show two driving patterns: passing a `PDFWorkflowObject` entity into
  `WorkflowService.Process` vs. letting an action read/write workflow attributes directly.
- Related: [org.secc.Workflow](../org.secc.Workflow/README.md) for other SECC workflow actions.
