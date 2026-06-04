# org.secc.PastoralCare

> Block-based lists that let Pastoral Care staff track hospital, homebound, nursing-home, and communion visits.

## Overview

PastoralCare provides the Rock UI for Southeast's Pastoral Care ministry. Each block summarizes a
category of people who have been reported to Pastoral Care — hospital admissions, homebound
residents, nursing-home residents, and communion requests — driven by workflows and defined types
that the plugin's migrations install. Staff use these lists to see who needs a visit and to launch
the associated visit workflow.

## Project Info

- **Project file:** `org.secc.PastoralCare.csproj`
- **Root namespace:** `org.secc.PastoralCare`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly) and `RockWeb/Plugins/org_secc/` (block `.ascx` markup)

## Project Layout

```
/org_secc/PastoralCare/   UI blocks (.ascx + .ascx.cs)
/Migrations/              Rock plugin migrations (pages, defined types, workflows)
```

## Components

### Blocks

Category in Rock: **SECC > Pastoral Care**.

| Block | Purpose | Key settings |
|-------|---------|--------------|
| Hospital List | Summary of current hospitalizations reported to Pastoral Care. | Hospital Admission Workflow, Hospital List defined type, Volunteer Group, Display Pastoral Minister |
| Homebound List | Summary of current homebound residents. | Homebound Person Workflow |
| Nursing Home List | Summary of current nursing-home residents. | Nursing Home Resident Workflow, Nursing Home List defined type |
| Communion List | List of pastoral-care patients/residents who have requested communion. | (driven by the lists above) |

Blocks are configured via Rock block settings — chiefly `WorkflowTypeField` (which workflow a
list launches/closes), `DefinedTypeField` (the list of hospitals / nursing homes), a `GroupField`
for the volunteer group, and a `BooleanField` to show the assigned pastoral minister.

## Dependencies & Integrations

- **Rock:** `RockContext`, Rock workflow engine, defined types, the Rock block/UI framework.
- No third-party APIs.

## Migrations

Ships Rock plugin migrations that stand up the plugin's data and pages:

- `001_Pastoral_DefinedTypes` — hospital / nursing-home defined types.
- `002_Pastoral_WorkflowData` — the pastoral visit workflows.
- `003_Pastoral_Pages` — Rock pages/blocks for the lists.
- `004`–`006` — fixes and additions (nursing-home fix, visit-cancel, legacy-Lava fix,
  show-name-in-visit).

## Observations

*Noticed while documenting — not a full audit.*

- **Security (medium):** These blocks surface sensitive health/PII — hospitalizations, homebound
  status, nursing-home residency. Make sure the Rock **page/block security** for these is locked to
  Pastoral Care staff roles and not inadvertently viewable by broader admin/staff groups.

## Making Changes

- To change what a list shows, edit the matching `*.ascx.cs` in `org_secc/PastoralCare/`; the
  block's behavior keys off its configured workflow/defined-type settings.
- New pages, workflows, or defined-type values belong in a new numbered migration under
  `/Migrations/` (don't hand-edit existing migrations that have already run).
- Related: [org.secc.Workflow](../org.secc.Workflow/README.md) (the `DeleteVisitActivity` action and
  pastoral workflow helpers) and the `CloseDeseasedPastoralWorkflows` job in
  [org.secc.Jobs](../org.secc.Jobs/README.md).
