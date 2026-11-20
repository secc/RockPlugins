<#
.SYNOPSIS
    Builds every deployable org.secc plugin into RockWeb for the Azure DevOps "Rock v16"
    pipeline (build definition 39, "Build Plugins" step).

.DESCRIPTION
    The pipeline step only calls this script, so how plugins are found and built lives in the
    repo and changes through pull requests like any other code.

    Run it after Rock has been built and RockPlugins has been cloned into
    $(Build.SourcesDirectory)\RockPlugins. It:

      1. Finds every plugin C# project under Plugins\, at any depth, so both layouts work:
           Plugins\org.secc.Foo\org.secc.Foo.csproj                    (legacy)
           Plugins\org.secc.Foo\org.secc.Foo\org.secc.Foo.csproj       (SDK-style, nested)
         Skipped: *.Tests projects (only org.secc.Test is run by the pipeline's "Test Plugins"
         step) and anything under bin\, obj\ or node_modules\.
      2. Builds them all from one generated .sln (one NuGet restore, one parallel msbuild).
         Legacy projects copy into RockWeb with their PostBuildEvent xcopy lines. SDK-style
         projects (IsRockPlugin) copy with the CopyRockPluginToRockWeb target in
         Directory.Build.targets.
      3. Builds every Obsidian (Vue/TypeScript) front end, found by its obsidian.config.json:
         npm ci, npm run build, then copies dist\ to the RockWeb folder named by the config's
         "destination" (the part after "RockWeb/").
      4. Copies the org_secc\ blocks and Themes\ of every top-level plugin folder into RockWeb,
         for block-only plugins that have no project.
      5. Fails the build if an SDK-style plugin's DLL did not land in RockWeb\Bin.

    Per-plugin BuildPlugin.ps1 scripts are NOT run. They only package .plugin zips, and
    anything a plugin needs in RockWeb belongs in its project (see Plugins\README-SdkStyle.md).

.PARAMETER SourcesDirectory
    The Rock checkout with RockWeb and RockPlugins inside it. Defaults to
    $env:BUILD_SOURCESDIRECTORY.

.PARAMETER Configuration
    MSBuild configuration. Defaults to $env:BUILDCONFIGURATION, then Debug.
#>
[CmdletBinding()]
param(
    [string] $SourcesDirectory = $env:BUILD_SOURCESDIRECTORY,
    [string] $Configuration = $(if ($env:BUILDCONFIGURATION) { $env:BUILDCONFIGURATION } else { 'Debug' })
)

$ErrorActionPreference = 'Stop'
$started = Get-Date

if (-not $SourcesDirectory) { throw "SourcesDirectory not set (pass -SourcesDirectory or set BUILD_SOURCESDIRECTORY)." }
$pluginsRoot = Join-Path $SourcesDirectory 'RockPlugins\Plugins'
$rockWeb = Join-Path $SourcesDirectory 'RockWeb'
if (-not (Test-Path $pluginsRoot)) { throw "Plugins folder not found: $pluginsRoot" }
if (-not (Test-Path (Join-Path $rockWeb 'Bin\Rock.dll'))) { throw "RockWeb\Bin\Rock.dll not found under $rockWeb. Build Rock first." }

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw "MSBuild.exe not found" }

# Matches paths inside build output and npm folders, which never hold plugin sources.
$ignoredPath = '\\(bin|obj|node_modules)\\'

function Get-ProjectInfo {
    param([IO.FileInfo] $File)
    $xml = [xml](Get-Content $File.FullName -Raw)
    $node = { param($name) $xml.SelectSingleNode("//*[local-name()='$name']") }
    $guidNode = & $node 'ProjectGuid'
    $guid = if ($guidNode) { $guidNode.InnerText.Trim() } else { '{' + [guid]::NewGuid().ToString() + '}' }
    if (-not $guid.StartsWith('{')) { $guid = "{$guid}" }
    $asmNode = & $node 'AssemblyName'
    $isPlugin = & $node 'IsRockPlugin'
    [pscustomobject]@{
        Name         = $File.BaseName
        Path         = $File.FullName
        Guid         = $guid.ToUpper()
        IsSdk        = [bool]$xml.Project.Sdk
        IsRockPlugin = [bool]($isPlugin -and $isPlugin.InnerText.Trim() -eq 'true')
        AssemblyName = if ($asmNode) { $asmNode.InnerText.Trim() } else { $File.BaseName }
    }
}

function New-PluginSolution {
    param([object[]] $Projects, [string] $Path)
    # Minimal .sln so one restore and one parallel msbuild cover every plugin. Absolute
    # project paths are valid in the .sln format. Inter-plugin dependencies are all
    # ProjectReferences, so MSBuild orders them itself.
    $csharpType = '{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}'
    $sb = [Text.StringBuilder]::new()
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('Microsoft Visual Studio Solution File, Format Version 12.00')
    [void]$sb.AppendLine('# Visual Studio Version 17')
    [void]$sb.AppendLine('VisualStudioVersion = 17.0.31903.59')
    [void]$sb.AppendLine('MinimumVisualStudioVersion = 10.0.40219.1')
    foreach ($p in $Projects) {
        [void]$sb.AppendLine("Project(""$csharpType"") = ""$($p.Name)"", ""$($p.Path)"", ""$($p.Guid)""")
        [void]$sb.AppendLine('EndProject')
    }
    [void]$sb.AppendLine('Global')
    [void]$sb.AppendLine("`tGlobalSection(SolutionConfigurationPlatforms) = preSolution")
    [void]$sb.AppendLine("`t`tDebug|Any CPU = Debug|Any CPU")
    [void]$sb.AppendLine("`t`tRelease|Any CPU = Release|Any CPU")
    [void]$sb.AppendLine("`tEndGlobalSection")
    [void]$sb.AppendLine("`tGlobalSection(ProjectConfigurationPlatforms) = postSolution")
    foreach ($p in $Projects) {
        foreach ($c in 'Debug', 'Release') {
            [void]$sb.AppendLine("`t`t$($p.Guid).$c|Any CPU.ActiveCfg = $c|AnyCPU")
            [void]$sb.AppendLine("`t`t$($p.Guid).$c|Any CPU.Build.0 = $c|AnyCPU")
        }
    }
    [void]$sb.AppendLine("`tEndGlobalSection")
    [void]$sb.AppendLine('EndGlobal')
    [IO.File]::WriteAllText($Path, $sb.ToString(), [Text.UTF8Encoding]::new($true))
}

# --- 1. Find plugin projects -------------------------------------------------------------
$projects = @(
    Get-ChildItem $pluginsRoot -Recurse -File -Filter '*.csproj' |
        Where-Object { $_.FullName -notmatch $ignoredPath -and $_.BaseName -notmatch '\.Tests$' } |
        Sort-Object FullName |
        ForEach-Object { Get-ProjectInfo $_ }
)
$dupes = $projects | Group-Object Name | Where-Object Count -gt 1
if ($dupes) { throw "Duplicate plugin project names: $(($dupes | ForEach-Object Name) -join ', ')" }

Write-Host "Plugin projects ($($projects.Count)):"
foreach ($p in $projects) {
    $kind = if ($p.IsRockPlugin) { 'SDK, IsRockPlugin' } elseif ($p.IsSdk) { 'SDK' } else { 'legacy' }
    Write-Host ("  {0} ({1})" -f $p.Path.Substring($pluginsRoot.Length + 1), $kind)
}

# --- 2. Build them -----------------------------------------------------------------------
# The .sln sits at the sources root so $(SolutionDir) is the Rock checkout, which legacy
# plugins' HintPaths and PostBuildEvent xcopy lines expect.
$sln = Join-Path $SourcesDirectory 'Plugins.generated.sln'
New-PluginSolution -Projects $projects -Path $sln
try {
    Write-Host "Restoring plugin packages . . ."
    & nuget.exe restore $sln -SolutionDirectory $SourcesDirectory -NonInteractive
    if ($LASTEXITCODE -ne 0) { throw "nuget restore failed with exit code $LASTEXITCODE" }

    Write-Host "Building plugins ($Configuration) . . ."
    & $msbuild $sln /t:Build "/p:Configuration=$Configuration" "/p:Platform=Any CPU" "/p:RockWebPath=$rockWeb" /m /nr:false /v:m /clp:Summary /nologo
    if ($LASTEXITCODE -ne 0) { throw "Plugin build failed with exit code $LASTEXITCODE" }
}
finally {
    Remove-Item $sln -ErrorAction SilentlyContinue
}

# --- 3. Obsidian front ends --------------------------------------------------------------
$obsidianConfigs = @(
    Get-ChildItem $pluginsRoot -Recurse -File -Filter 'obsidian.config.json' |
        Where-Object { $_.FullName -notmatch $ignoredPath } |
        Sort-Object FullName
)
foreach ($configFile in $obsidianConfigs) {
    $dir = $configFile.DirectoryName
    $label = $dir.Substring($pluginsRoot.Length + 1)
    $config = Get-Content $configFile.FullName -Raw | ConvertFrom-Json
    $destination = [string]$config.destination
    $marker = $destination.IndexOf('RockWeb/', [StringComparison]::OrdinalIgnoreCase)
    if ($marker -lt 0) { throw "$label\obsidian.config.json: destination '$destination' has no 'RockWeb/' segment." }
    $target = Join-Path $rockWeb ($destination.Substring($marker + 'RockWeb/'.Length) -replace '/', '\')

    Write-Host "Building Obsidian front end $label -> $target"
    Push-Location $dir
    try {
        & npm ci --no-audit --no-fund
        if ($LASTEXITCODE -ne 0) { throw "$label`: npm ci failed with exit code $LASTEXITCODE" }
        # obsidian-build's own copy step needs a rock-dev-tool environment and skips itself
        # on the agent, so dist\ is copied below.
        & npm run build
        if ($LASTEXITCODE -ne 0) { throw "$label`: npm run build failed with exit code $LASTEXITCODE" }
    }
    finally {
        Pop-Location
    }

    $dist = Join-Path $dir 'dist'
    if (-not (Get-ChildItem $dist -Recurse -File -ErrorAction SilentlyContinue)) { throw "$label`: build produced nothing in $dist" }
    xcopy /Y /R /E /I /Q "$dist" "$target" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "$label`: copying dist to $target failed with exit code $LASTEXITCODE" }
}

# --- 4. Blocks and themes of top-level plugin folders -------------------------------------
# Block-only plugins (no project) ship .ascx files and themes that nothing else copies.
# A harmless repeat for plugins whose build already copied them.
foreach ($plugin in Get-ChildItem $pluginsRoot -Directory) {
    $blocks = Join-Path $plugin.FullName 'org_secc'
    if (Test-Path $blocks) { xcopy /Y /R /E /I /Q $blocks (Join-Path $rockWeb 'Plugins\org_secc') | Out-Null }
    $themes = Join-Path $plugin.FullName 'Themes'
    if (Test-Path $themes) { xcopy /Y /R /E /I /Q $themes (Join-Path $rockWeb 'Themes') | Out-Null }
}

# --- 5. Check that SDK-style plugins deployed --------------------------------------------
# An SDK-style plugin whose DLL never reaches RockWeb\Bin still "builds", and the release then
# ships the old DLL without any error. Legacy projects copy with hand-written xcopy lines and
# not all of them ship a DLL, so they are only listed.
$missing = @()
foreach ($p in $projects | Where-Object IsRockPlugin) {
    $dll = Join-Path $rockWeb "Bin\$($p.AssemblyName).dll"
    if (-not (Test-Path $dll) -or (Get-Item $dll).LastWriteTime -lt $started) { $missing += $p.Name }
}
if ($missing) { throw "These SDK-style plugins built but their DLL in RockWeb\Bin was not updated: $($missing -join ', ')" }

$stale = @(
    $projects | Where-Object { -not $_.IsRockPlugin } | Where-Object {
        $dll = Join-Path $rockWeb "Bin\$($_.AssemblyName).dll"
        -not (Test-Path $dll) -or (Get-Item $dll).LastWriteTime -lt $started
    } | ForEach-Object Name
)
if ($stale) { Write-Host "Note: no fresh RockWeb\Bin DLL for these legacy projects (expected for test or block-only projects): $($stale -join ', ')" }

Write-Host ("Plugins built in {0:N0}s: {1} projects, {2} Obsidian front ends." -f ((Get-Date) - $started).TotalSeconds, $projects.Count, $obsidianConfigs.Count)
