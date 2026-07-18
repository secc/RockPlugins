# org.secc.Imaging

> Renders HTML/Lava into PNG images over REST, and bulk-updates person photos by AI face-cropping existing binary files via Azure Cognitive Services Face.

## Overview

Imaging bundles two unrelated image features. The first is a REST endpoint that converts an
HTML string to a PNG (via the wkhtmltoimage-backed `NReco.ImageGenerator`), used to produce
images from Lava/HTML templates. The second is a pair of admin blocks that batch-crop person
profile photos: they pull existing binary files, send each to Azure Face to detect and
straighten the face, crop to a 500x500 square, and save the result (as a quality-95 JPEG) back
as the person's photo.

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
/org_secc/Imaging/  UpdatePersonImage(.ascx), UpdatePersonImageFromRegistration(.ascx) — bulk crop blocks
/Migrations/   Rock plugin migrations (Html To Image defined type)
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
| Update Person Image | Runs an admin-supplied SQL query returning `PersonId`/`BinaryFileId` pairs, then face-crops each binary file into the person's photo. | Execution Delay |
| Update Person Image From Registration | Picks one or more registration templates, finds registrant attribute values with key containing "photo", and face-crops those binary files into the registrant's photo. | Execution Delay |

Both blocks share the `ExecutionDelay` (`IntegerField`) attribute — milliseconds to `Thread.Sleep`
between calls to stay under the Azure Face transactions-per-second limit. The registration block
adds an "Overwrite photos?" checkbox: when checked it updates non-`secc.org` profiles even if they
already have a photo; otherwise only registrants with no existing `PhotoId`.

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
- **Cross-plugin:** [org.secc.DevLib](../org.secc.DevLib/README.md) (`SettingsComponent` base for `MicrosoftFaceSettings`).

## Migrations

- `001_ImageGeneratorDefinedType` — adds the **Html To Image** defined type and its entry attributes
  (Template, Enabled Lava Commands, Image Type, Width, Height).
- `002_ImageGeneratorDefinedTypeResponseHeaders` — adds a Response Headers key-value attribute.
- `003_ImageGeneratorCleanup` — drops the Image Type attribute; renames Width/Height to Canvas Width /
  Canvas Height with help text.

## Observations

*Noticed while documenting — not a full audit.*

- **Security (medium):** The "Update Person Image" block executes **arbitrary admin-entered SQL**
  (`rockContext.Database.SqlQuery<ResponseData>(sql)`) with no validation. Powerful by design, but
  ensure the page/block is locked to trusted admins.
- **Security (low):** The Azure Face **Subscription Key** is stored in a plain `TextField` settings
  attribute (not an encrypted/`EncryptedTextField`), so it is readable in cleartext by anyone with
  access to the settings component. Treat it as a secret and restrict access accordingly.
- **Security (low):** The Azure Face `DetectWithUrlAsync` call builds the image URL from
  `PublicApplicationRoot` + `GetImage.ashx?Guid=…`, so the binary file must be publicly fetchable by
  Azure for detection to work. Worth confirming those `GetImage` URLs aren't otherwise sensitive.
- **Improvement:** Both blocks fire `Task.Run(async () => await face.UpdatePhoto(...))` in a
  fire-and-forget loop on a web request — the task is never awaited and exceptions are swallowed, so
  the page returns before/independent of the crops finishing and failures are invisible. A scheduled
  job or transaction queue would be more robust for a multi-thousand-record batch.
- **Improvement:** Both `.ascx.cs` files declare the same class `UpdatePersonImage` in the same
  `RockWeb.Plugins.org_secc.Imaging` namespace (the registration file's class name was not renamed).
  Worth confirming this doesn't cause a type collision at compile/load.
- **Improvement:** The `generateimage` endpoint takes raw `Content` and never reads the **Html To Image**
  defined type that the migrations install, so the template/canvas-size attributes appear unused by the
  current controller. Confirm whether a consumer applies them or whether they're vestigial.

## Making Changes

- To change HTML->image rendering, edit `HtmlToImage.cs` (NReco options) or the `GenerateImage`
  action in `Rest/ImagingController.Partial.cs`.
- To change face-crop logic (detection model, crop padding, output size/quality), edit `AI/FaceCrop.cs`.
- Azure Face credentials are stored in the **Microsoft Face** settings component
  (`Components/MicrosoftFaceSettings.cs`), edited through Rock, not in code.
- To change which photos a batch targets, edit the matching `*.ascx.cs` in `org_secc/Imaging/`.
- New defined-type attributes belong in a new numbered migration under `/Migrations/`.
