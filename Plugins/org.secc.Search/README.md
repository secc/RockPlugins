# org.secc.Search

> Adds custom Rock person-search components (address, DOB, envelope) plus admin blocks for person, bulk, and Apple Pass lookups.

## Overview

Search supplies Southeast's custom person-search extensions. The compiled assembly contributes
three Rock `SearchComponent`s — search people by home/previous **address**, **date of birth**, or
**giving envelope number** — that appear in Rock's universal search alongside the built-in ones. The
plugin also ships several RockWeb admin blocks (a richer person-results grid, a bulk email/phone
lookup, and an Apple-Pass generator/test pair) under `org_secc/Crm/`. Staff use these to find people
during data work and to issue event passes.

## Project Info

- **Project file:** `org.secc.Search.csproj`
- **Root namespace:** `org.secc.Search`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly) and `RockWeb/Plugins/org_secc/` (block `.ascx` markup, via the PostBuildEvent `xcopy`)
- **Note:** Only the three `Person/*.cs` search components are in the `.csproj` `<Compile>` list. The
  `org_secc/Crm/*.ascx.cs` blocks are **RockWeb-compiled** (compiled in place by RockWeb, not into this assembly).

## Project Layout

```
/Person/        SearchComponent exports — Address, DOB, Envelope (compiled into the assembly)
/org_secc/Crm/  RockWeb blocks (.ascx + .ascx.cs) — PersonSearch, BulkSearch, GenerateApplePass, ApplePassTest
```

## Components

### Search Components

MEF exports of Rock's `SearchComponent`, discovered at startup. Each appears in Rock's universal
search as a search type; `Search(searchterm)` returns a list of matching display strings.

| Component (ComponentName) | Search Label | Matches on |
|---------------------------|--------------|------------|
| `SECC Person Address` (`Address`) | Address | Family home/previous `GroupLocation` where Street1 or PostalCode contains the term; returns `Street1 - PostalCode`. |
| `SECC Person DOB` (`DOB`) | DOB | Distinct birth dates whose short/long string form contains the term (case-insensitive); falls back to a full long-date (`"D"`) match. |
| `SECC Person Envelope` (`Envelope`) | Envelope | Exact match of the person `GivingEnvelopeNumber` attribute value. |

### Blocks

RockWeb blocks under `org_secc/Crm/`. Categories below are taken from each block's `[Category]` attribute.

| Block | Category | Purpose | Key settings |
|-------|----------|---------|--------------|
| Person Search (`PersonSearch`) | SECC > CRM | Results grid for a `SearchType` + `SearchTerm` (name, phone, address, email, envelope, dob) passed as page parameters; redirects to the person if a single match. | Person Detail Page (linked page), Show Performance (bool) |
| Bulk Search (`BulkSearch`) | SECC > Search | Paste a newline-delimited list of emails and/or phone numbers into a grid of matching people. | (none) |
| My Account - Generate Apple Pass (`GenerateApplePass`) | SECC > CRM | Headless block (hidden field + hidden button, no person picker): a client-side `GeneratePass(personAliasGuid)` script stuffs the alias guid into `hfPassInfo` and clicks the hidden button; the block then returns the person's existing pass file, or runs the configured pass workflow if the person's pass attribute is empty, and redirects to `GetFile.ashx`. | Pass Template Id (`PassTemplateId`, int, optional), Apple Pass Workflow (`ApplePassWorkflow`, workflow type, required), Event Pass Person Attribute (`EventPassAttribute`, person attribute, required) |
| Apple Pass Test (`ApplePassTest`) | SECC > CRM | Test harness: pick a person, then it emits the `GeneratePass("<primaryAliasGuid>")` startup script (the same hook the `GenerateApplePass` block listens for). Has no `[Description]` attribute. | (none) |

## Dependencies & Integrations

- **Rock:** universal search (`SearchComponent`), `RockContext` and Rock services (`PersonService`,
  `GroupMemberService`, `PhoneNumberService`, `AttributeService`/`AttributeValueService`,
  `PersonAliasService`, `WorkflowService`), the Rock block/UI framework, workflow engine (Apple Pass),
  defined-value/cache helpers, EntityFramework 6.1.3.
- **Third-party:** none.
- The Apple-Pass blocks delegate file generation to a configured Rock **workflow type** (the block
  reads the resulting `PassFile` attribute), so the actual pass-building logic lives in that workflow,
  not in this plugin.

## Observations

*Noticed while documenting — not a full audit.*

- **Improvement:** The `DOB` search component and the `PersonSearch` `dob` branch pull **all** birth
  dates into memory (`Distinct().AsEnumerable()`) and format/compare each string client-side. On a
  large person table this is an unindexed full scan per search — review for performance. The same
  logic is duplicated between `Person/DOB.cs` and `PersonSearch.ascx.cs`.
- **Improvement:** `BulkSearch` lives in folder `org_secc/Crm/` but declares namespace
  `RockWeb.Plugins.org_secc.Search` and category `SECC > Search`, while its siblings use
  `RockWeb.Plugins.org_secc.Crm` / `SECC > CRM`. Its `[Description]` is also placeholder text
  ("Takes the ten two csv..."). Worth tidying so the block path/namespace/category line up.
- **Security (low):** These search/results blocks surface person PII (addresses, DOB, email, envelope
  numbers). Confirm the Rock **page/block security** on the pages hosting them is limited to staff who
  should see it; envelope search in particular ties a person to giving data.
- **Improvement:** `Properties/AssemblyInfo.cs` still carries a scaffolded `AssemblyCopyright`
  (`"Copyright ©  2016"`) with empty company/description rather than Southeast Christian Church.

## Making Changes

- To add a new search type, drop a `SearchComponent` subclass in `Person/`, decorate it with
  `[Export(typeof(SearchComponent))]` + `[ExportMetadata("ComponentName", "…")]` and a `SearchLabel`
  default, implement `Search(string)`, and add it to the `.csproj` `<Compile>` list. MEF discovers it
  at startup — no further registration needed. Follow `Person/Envelope.cs` as the simplest template.
- The results grid and its per-type query logic live in `org_secc/Crm/PersonSearch.ascx.cs`
  (`BindGrid` switch). Keep its branches in sync with the standalone search components if behavior
  should match.
- Apple-Pass generation is driven by the workflow configured on the `GenerateApplePass` block, not by
  code here — change the pass output by editing that workflow type.
