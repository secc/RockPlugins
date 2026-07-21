# org.secc.AI

> Empty scaffold project — no functionality yet; reserved as the home for future AI-related Rock code.

## Overview

This is a freshly-scaffolded, **empty** plugin. It contains a single placeholder type
(`Class1`, with no members) and the default project metadata — nothing is wired into Rock.
It exists as a reserved namespace/assembly (`org.secc.AI`) for future AI-related work, but as
shipped it has no REST endpoints, Lava filters, blocks, jobs, field types, models, or migrations.

## Project Info

- **Project file:** `org.secc.AI.csproj`
- **Root namespace:** `org.secc.AI`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** nothing — no `PostBuildEvent` is defined, so the build output is not copied to `RockWeb/bin/`

## Project Layout

```
Class1.cs               Empty placeholder class (no members)
/Properties/            AssemblyInfo.cs — default scaffolded assembly metadata
org.secc.AI.csproj      Library project; references Rock.dll / Rock.Rest.dll but uses none of them
```

## Components

None. The only compiled type is an empty `Class1`; there is no Rock-facing functionality to document.

The `.csproj` already references the usual Rock plugin assemblies (`Rock`, `Rock.Rest`, `DotLiquid`,
`EntityFramework.SqlServer`, `Newtonsoft.Json`), so the project is set up to grow into a real plugin
without further reference wiring.

## Dependencies & Integrations

- **Rock:** References `Rock.dll` and `Rock.Rest.dll` but does not call into them yet.
- **Third-party:** References DotLiquid, EntityFramework, and Newtonsoft.Json (unused as shipped).

## Observations

*Noticed while documenting — not a full audit.*

- **Improvement:** The plugin is an empty scaffold. `Class1.cs` should be replaced with real code or
  the project removed/renamed before relying on the `org.secc.AI` assembly in production.
- **Improvement:** No `PostBuildEvent` `xcopy` to `RockWeb/bin/` is defined (unlike sibling plugins
  such as [org.secc.Jobs](../org.secc.Jobs/README.md)), so even if code were added, the build output
  would not be deployed until that step is added.
- **Improvement:** `AssemblyInfo.cs` carries empty `AssemblyCompany`/`AssemblyDescription` and a
  generic `"Copyright ©  2024"` rather than Southeast Christian Church attribution.

## Making Changes

- This is the place to add Southeast's AI integrations. Add new `.cs` files and reference them in the
  `<Compile>` list of `org.secc.AI.csproj` (or replace the placeholder `Class1.cs`).
- Before the assembly can load in Rock, add a `PostBuildEvent` `xcopy` of the output to `RockWeb/bin/`
  — copy the pattern from [org.secc.Jobs](../org.secc.Jobs/README.md) or another deploying sibling.
- For component patterns (REST controllers, Lava filters, jobs), follow established examples such as
  [org.secc.QRManager](../org.secc.QRManager/README.md) and [org.secc.Jobs](../org.secc.Jobs/README.md).
