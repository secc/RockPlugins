# SDK-style plugin projects

We're moving plugin `.csproj` files from the legacy format (`ToolsVersion="14.0"`,
`packages.config`, `xcopy` post-build events) to SDK-style projects, one plugin at a
time. Converted so far: **org.secc.LinkList**, **org.secc.Search**. Every other plugin
still builds exactly as before.

## How it works

Two files at the repo root do the shared work:

- **`Directory.Build.props`** finds RockWeb and sets `RockWebPath` / `RockBin`.
  First match wins:
  1. `/p:RockWebPath=...` passed explicitly (BuildPlugin.ps1, CLI, CI)
  2. `$(SolutionDir)RockWeb` when building from a solution in the Rock folder
  3. `..\RockWeb` (pipeline layout: RockPlugins checked out inside Rock)
  4. `..\Rock\RockWeb` (local layout: `Rock` and `RockPlugins` side by side)

  It also re-imports the parent `Directory.Build.props` (Rock's, which sets `PathMap`).
- **`Directory.Build.targets`** adds the Rock references, keeps `org_secc\**` out of
  compilation, and copies the plugin into RockWeb after each build.

Both files apply to every project under the repo, legacy ones included. So everything
that changes a build is gated on an SDK-style project opting in. Legacy projects
evaluate identically with or without these files.

### Rock comes from RockWeb\Bin, not NuGet

Plugins compile against the Rock DLLs we actually build and deploy (`RockWeb\Bin`, with
`Private=false`, so they are never copied or shipped). We don't use the `RockRMS.*`
NuGet packages. Spark doesn't publish one for every hotfix (there is none for 16.12),
and they would never include changes from our own Rock fork.

That means **secc/Rock must be built first**. If it isn't, the build stops with one
clear error telling you where it looked.

## Project settings

| Setting | Meaning |
|---|---|
| `<IsRockPlugin>true</IsRockPlugin>` | Plugin that deploys to RockWeb. Turns everything below on. |
| `<IsRockPluginTest>true</IsRockPluginTest>` | Test project: same Rock references, but copied locally (`Private=true`) so the test host can load them. Nothing deployed. |
| `<RockReference Include="Rock.Rest;Rock.Blocks" />` | Add any DLL from `RockWeb\Bin` on top of the defaults. |
| `<RockDefaultReferences>false</RockDefaultReferences>` | Skip the defaults and list every `RockReference` yourself. |
| `<RockWebContent Include="Themes\**" RockWebDir="Themes" />` | Extra files to copy into RockWeb (replaces the non-`org_secc` `xcopy` lines). |
| `<CopyToRockWeb>false</CopyToRockWeb>` | Build without deploying. |

Default references: `Rock`, `Rock.Common`, `Rock.Enums`, `Rock.Lava.Shared`,
`Rock.ViewModels`, `DotLiquid`, `EntityFramework`, `Newtonsoft.Json`.

On every build, the plugin DLL and PDB go to `RockWeb\Bin`, `org_secc\**` goes to
`RockWeb\Plugins\org_secc`, and `RockWebContent` items go to `RockWeb\<RockWebDir>`.

## Converting a plugin (checklist)

Start from `org.secc.Search/org.secc.Search.csproj`. Then:

1. **Compare the old `<Compile>` list to the `.cs` files on disk** (outside `org_secc`,
   `bin`, `obj`). SDK projects compile every `.cs` file. Anything on disk that the old
   project didn't list will start compiling: delete it, or add `<Compile Remove>`.
2. **`Properties\AssemblyInfo.cs`?** Set `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>`
   (otherwise you get duplicate attribute errors).
3. **Output folder.** If `BuildPlugin.ps1` or the pipeline picks up
   `bin\Debug\` / `bin\Release\`, set
   `<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>`.
4. **References.**
   - Anything Rock ships in `RockWeb\Bin` (Rock.*, DotLiquid, EF, Newtonsoft, Web API,
     HtmlAgilityPack, Quartz, …) → `RockReference`. That includes things the old project
     pulled from a `packages\` folder. They were often stale (EF 6.1.3 vs Rock's 6.4.4).
   - Only genuinely plugin-owned third-party libraries → `PackageReference`. Watch for
     packages that drag in an old `Newtonsoft.Json`. `Microsoft.AspNet.WebApi.Core`
     pulls 6.0.4 and wins over Rock's 13.0. Use `RockReference` for those instead.
   - Framework assemblies that aren't implicit in SDK net472 projects must be listed:
     `System.Web`, `System.Net.Http`, `System.ComponentModel.Composition`,
     `System.ComponentModel.DataAnnotations`, `Microsoft.CSharp`, …
   - Delete `packages.config`.
   - Don't add `SparkDevNetwork.Rock.Build.Tasks`. Its copy step no-ops outside a
     rock-dev-tool layout, and its props blank out `RockWebPath`. `SparkDevNetwork.Rock.Analyzers`
     is fine.
5. **Post-build events.** Delete the `PostBuildEvent`. The `org_secc` and DLL/PDB copies
   are automatic. Turn every other `xcopy` line into a `RockWebContent` item (or a small
   target for anything unusual, like Sass's LibSass runtimes).
6. **Verify.**
   - The build is clean and only the intended `.cs` files compile
     (`dotnet msbuild -getItem:Compile`).
   - The DLL's types, members and assembly references match the legacy build.
   - The right files land in RockWeb.
   - Tests still pass.
   - Smoke-test the plugin's blocks in Rock.

## Known gaps

- **Restore.** SDK projects must be restored before building (`msbuild -restore`,
  `dotnet build`, or `nuget restore`). Anything that builds plugin projects without a
  restore step fails with `NETSDK1004`. LinkList's `BuildPlugin.ps1` already passes
  `-restore`.
- **Mixed project references.** Not yet exercised: a legacy project referencing an
  SDK-style one, or the reverse (e.g. once `org.secc.DevLib` is converted).
- **Mac/Linux** can compile SDK-style plugins (the .NET SDK supplies the net472 reference
  assemblies), as long as a built RockWeb\Bin is available. Running the tests there needs
  Mono, and running Rock still needs Windows.
