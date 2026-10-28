# org.secc.Imaging

> Renders HTML/Lava into PNG images over REST, and bulk-updates person photos by AI face-cropping existing binary files via Azure Cognitive Services Face.

## Overview

Imaging bundles two unrelated image features. The first is a REST endpoint that converts an
HTML string to a PNG (via the wkhtmltoimage-backed `NReco.ImageGenerator`), used to produce
images from Lava/HTML templates. The second is an admin block that batch-crops registrants'
profile photos: it pulls the photos uploaded on registrations, sends each to Azure Face to detect
and straighten the face, crops to a 500x500 square, and saves the result (as a quality-95 JPEG)
back as the person's photo.

## Project Info

- **Project file:** `org.secc.Imaging.csproj`
- **Root namespace:** `org.secc.Imaging`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly + `NReco.ImageGenerator.dll`; the Microsoft Azure
  Face/Rest client DLLs (`Microsoft.Azure.CognitiveServices.Vision.Face`,
  `Microsoft.IdentityModel.Clients.ActiveDirectory`, `Microsoft.Rest.ClientRuntime[.Azure]`);
  and `ImageResizer.dll` + `ImageResizer.Plugins.SimpleFilters.dll`) and
  `RockWeb/Plugins/org_secc/` (block `.ascx` markup)

## Project Layout

```
/Rest/         ImagingController  — REST endpoints (test, generateimage)
/AI/           FaceCrop           — Azure Face detect + rotate + crop person photos
/Components/   MicrosoftFaceSettings — DevLib SettingsComponent holding Face endpoint + key
/org_secc/Imaging/  UpdatePersonImageFromRegistration(.ascx) — bulk crop block
/Migrations/   Rock plugin migrations (Html To Image defined type; Update Person Image removal)
HtmlToImage.cs Constants.cs       — NReco HTML->image wrapper; defined-type Guid constant
```

## Components

### REST Endpoints

`ImagingController` derives from `ApiControllerBase`.

| Route | Method | Purpose |
|-------|--------|---------|
| `api/imaging/test` | GET | Returns the literal string `"test"`. |
| `api/imaging/generateimage` | POST | Renders posted `Content` (HTML/Lava string) to a PNG and returns it as a `image.png` attachment. Optional `Width` (default 1920). Non-ASCII chars are HTML-entity encoded before rendering. |

### Blocks

Category in Rock: **SECC > Imaging**.

| Block | Purpose | Key settings |
|-------|---------|--------------|
| Update Person Image From Registration | Picks one or more registration templates, finds registrant attribute values with key containing "photo", and face-crops those binary files into the registrant's photo. | Execution Delay |

`ExecutionDelay` (`IntegerField`) is the number of milliseconds to `Thread.Sleep` between calls to
stay under the Azure Face transactions-per-second limit. The "Overwrite photos?" checkbox, when
checked, updates non-`secc.org` profiles even if they already have a photo; otherwise only
registrants with no existing `PhotoId` are updated.

### Settings Component

| Component | Type | Settings |
|-----------|------|----------|
| Microsoft Face | DevLib `SettingsComponent` (MEF `Export`) | **Endpoint** (`TextField`), **Subscription Key** (`TextField`, key `SubscriptionKey`) — Azure Cognitive Services Face credentials read by `FaceCrop`. |

### Defined Type

`001` installs the **Html To Image** defined type (Guid `8CC5A105-…`, mirrored in `Constants.cs`)
with entry attributes for Template (Lava/HTML), Enabled Lava Commands, Canvas Width, Canvas Height,
and Response Headers — intended as reusable templates for the image generator.

## Dependencies & Integrations

- **Rock:** `RockContext`, `PersonService`, `BinaryFileService`, `RegistrationRegistrantService`,
  `AttributeValueService`, `GlobalAttributesCache`, Rock REST (`ApiControllerBase`), the block/UI
  framework, defined types, `BinaryFiletype.PERSON_IMAGE`.
- **Third-party:** `NReco.ImageGenerator` (wkhtmltoimage HTML->image), `Microsoft.Azure.CognitiveServices.Vision.Face`
  (face detection), `System.Drawing` (rotate/crop/encode), `ImageResizer`.
- **Cross-plugin:** [org.secc.DevLib](../org.secc.DevLib/README.md) (`SettingsComponent` base for `MicrosoftFaceSettings`;
  `ReadContentBytes`, which `FaceCrop` uses to read the source photo through whichever storage provider
  holds it and which throws a clear `InvalidOperationException` when the file is missing or empty).

## Migrations

- `001_ImageGeneratorDefinedType` — adds the **Html To Image** defined type and its entry attributes
  (Template, Enabled Lava Commands, Image Type, Width, Height).
- `002_ImageGeneratorDefinedTypeResponseHeaders` — adds a Response Headers key-value attribute.
- `003_ImageGeneratorCleanup` — drops the Image Type attribute; renames Width/Height to Canvas Width /
  Canvas Height with help text.
- `004_RemoveUpdatePersonImageBlock` — deletes the removed Update Person Image block type (by path), its
  blocks, their security rules and its Execution Delay attribute, then the production page
  "Binary File To Person Photo" if nothing else is on or under it.

## Observations

*Noticed while documenting — not a full audit.*

- **Improvement:** The registration block fires `Task.Run(async () => await face.UpdatePhoto(...))` in a
  fire-and-forget loop on a web request, so the page returns before/independent of the crops finishing
  and failures are swallowed. A scheduled job or transaction queue would be more robust for a
  multi-thousand-record batch.
- **Improvement:** `UpdatePersonImageFromRegistration.ascx.cs` still declares its class as
  `UpdatePersonImage` (copied from the removed block and never renamed).
- **Improvement:** The `generateimage` endpoint takes raw `Content` and never reads the **Html To Image**
  defined type that the migrations install, so the template/canvas-size attributes appear unused by the
  current controller. Confirm whether a consumer applies them or whether they're vestigial.

## Making Changes

- To change HTML->image rendering, edit `HtmlToImage.cs` (NReco options) or the `GenerateImage`
  action in `Rest/ImagingController.Partial.cs`.
- To change face-crop logic (detection model, crop padding, output size/quality), edit `AI/FaceCrop.cs`.
- Azure Face credentials are stored in the **Microsoft Face** settings component
  (`Components/MicrosoftFaceSettings.cs`), edited through Rock, not in code.
- To change which photos a batch targets, edit `org_secc/Imaging/UpdatePersonImageFromRegistration.ascx.cs`.
- New defined-type attributes belong in a new numbered migration under `/Migrations/`.
- `FaceCrop.CropDetectedFace` disposes the source, rotated and target bitmaps; keep any new GDI+ objects in
  `using` blocks, since the registration block runs it for every matching registrant.

Last updated: 2026-10-13
