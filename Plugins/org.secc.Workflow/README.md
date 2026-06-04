# org.secc.Workflow

> A library of custom Rock workflow actions (plus two bulk-workflow admin blocks) covering people, communications, registrations, media, and workflow control.

## Overview

This is Southeast's catch-all workflow-extension plugin. It supplies ~29 custom workflow
**actions** that drop into Rock's workflow engine, organized by area (person matching, SMS,
connections, registrations/discount codes, attribute-matrix manipulation, media processing,
and workflow control). It also ships two admin blocks for selecting and updating workflows in
bulk.

## Project Info

- **Project file:** `org.secc.Workflow.csproj`
- **Root namespace:** `org.secc.Workflow`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly + `FFmpeg.NET.dll`, `Magick*`) and
  `RockWeb/Plugins/org_secc/` (block markup)

## Project Layout

Actions are grouped into one folder per area; each folder maps roughly to an action category in
the Rock workflow editor.

```
/Person/              person matching & history actions
/Communication/       SMS
/Connections/         connection-request actions
/Registrations/       discount codes & registrant field mapping
/WorkflowAttributes/  attribute & attribute-matrix manipulation
/WorkflowControl/     activate / reactivate / process / cache actions
/Schedule/            schedule helpers
/SignatureDocument/   signed-document storage
/Media/               FFmpeg / ImageMagick actions
/Twilio/              Twilio lookup
/CMS/                 cache-tag actions
/org_secc/Workflow/   admin blocks (.ascx)
```

## Components

### Blocks

Category in Rock: **SECC > Workflow**.

| Block | Purpose |
|-------|---------|
| Workflow Bulk Select | Tool for bulk-selecting workflows. |
| Workflow Bulk Update | Tool for updating workflows in bulk. |

### Workflow Actions

| Action | Area | Purpose |
|--------|------|---------|
| GetPersonFromFields | Person | Set an attribute to a person via SECC custom matching; optionally create a new person. |
| PersonAddHistory | Person | Add a history record to the selected person. |
| SendSms | Communication | Send an SMS to a person or a phone number in the `To` field. |
| SetConnectionRequestGroup | Connections | Set the group of a connection request. |
| SetConnectionAttributeValue | Connections | Set attributes of a connection request. |
| AutoApplyDiscountCode | Registrations | Apply a discount code to an unpersisted RegistrationState. |
| GenerateDiscountCode | Registrations | Generate a new discount code on a registration template. |
| UpdateDiscountCodeWithAttribute | Registrations | Update an existing discount code on a registration template. |
| SetAttributeFromRegistrantField | Registrations | Set an attribute from a registrant field by key + registrant index. |
| UpdateRegistrationGroupWithPlacementGroup | Registrations | Sync a registration group with its placement group. |
| SetAttributeValue | WorkflowAttributes | Set an attribute's value to the selected value. |
| CopyAttributesFromWorkflow | WorkflowAttributes | Copy attribute values from one workflow to another. |
| AttributeMatrixAddRow / UpdateRow / DeleteRow / Copy | WorkflowAttributes | Add / update / delete / clone attribute-matrix rows. |
| BinaryFileFromBase64String | WorkflowAttributes | Save a Base64 string as a Binary File. |
| ActivateWorkflowWithLava | WorkflowControl | Activate a new workflow with provided attribute values. |
| ProcessWorkflow | WorkflowControl | Process another workflow with provided attribute values. |
| ReActivateActivity | WorkflowControl | Reactivate an activity instance and its actions. |
| DeleteVisitActivity | WorkflowControl | Delete a visit activity instance and its actions. |
| ClearAuthCache | WorkflowControl | Clear the authorization cache. |
| ScheduleNextStartDate | Schedule | Get the next start date for a schedule. |
| StoreSignedDocument | SignatureDocument | Create a new signature document. |
| ClearCacheTags | CMS | Clear all cached items with the selected tag(s). |
| Lookup | Twilio | Make a Twilio Lookup API call. |
| FFmpeg | Media | Run FFmpeg commands. |
| ImageMontage | Media | Create JPG montages of image tiles. |
| BinaryFileRemove | Media | Remove a Binary File. |

## Dependencies & Integrations

- **Rock:** workflow engine (`ActionComponent`), `RockContext`, connections, registrations, CMS cache.
- **Cross-plugin:** references [org.secc.PersonMatch](../org.secc.PersonMatch/README.md) (used by `GetPersonFromFields`).
- **Third-party:** Twilio (lookup), FFmpeg.NET (video/audio), Magick.NET / ImageMagick (image montages).

## Observations

*Noticed while documenting — not a full audit; the `Media/FFmpeg` action stood out.*

- **Security (review):** `FFmpeg` resolves the admin-configured `Command` (Lava, including the
  `{{file}}` and `{{outputPath}}` merge fields) and passes it straight to the FFmpeg engine. If the
  `File` workflow attribute can be populated from untrusted input (e.g. a registrant upload/field),
  that string is interpolated into the command — an argument/command-injection vector. Treat this
  action as admin-trusted-only and validate/sanitize the `file` value. `OutputPath` is also used to
  `Directory.CreateDirectory` with no path-traversal guard, so it can write outside the intended root.
- **Improvement:** `Task.Run( … ).Wait()` is sync-over-async — it blocks the workflow thread on an
  external process and risks deadlock; there's also no timeout on the FFmpeg run. The default
  `FFmpegExecutable` is a hardcoded path into an ImageMagick install folder
  (`C:\Program Files\ImageMagick-…\ffmpeg.exe`), which is brittle and likely wrong on most servers.

## Making Changes

- To add an action, create a class in the appropriate area folder that subclasses Rock's
  `ActionComponent` and carries the standard `[Description]` / `[ActionCategory]` /
  `[Export]` attributes; follow an existing sibling as a template.
- Media actions require the `FFmpeg.NET` and `Magick*` assemblies to be present in `RockWeb/bin`
  (handled by the PostBuildEvent).
- `GetPersonFromFields` depends on [org.secc.PersonMatch](../org.secc.PersonMatch/README.md); person-matching changes may belong there.
