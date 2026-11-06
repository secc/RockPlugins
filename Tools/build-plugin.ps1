<#
.SYNOPSIS
    Build one or more org.secc plugins against the prebuilt Rock in ..\Rock\RockWeb\Bin,
    without rebuilding the Rock solution.

.DESCRIPTION
    Plugins reference Rock as a DLL ($(SolutionDir)RockWeb\Bin\Rock.dll), so they only need
    Rock to have been built once. LinkList still gets Rock from the RockRMS.* NuGet packages
    until PR #348 lands, so the stale-Rock warning doesn't apply to it. This script:
      1. Warns when a Rock assembly in RockWeb\Bin is older than its source (stale Rock build).
      2. Builds the requested plugin csproj files with MSBuild, SolutionDir pointed at ..\Rock.
      3. Unless -NoCopy: copies the plugin's org.secc.* DLL/PDB into RockWeb\Bin and runs the
         plugin's own pre/post-build copy steps, and those of the org.secc projects it
         references, the same as a Visual Studio build.

    The csproj PreBuildEvent and PostBuildEvent are turned off during the MSBuild call and
    run afterwards by this script, minus two kinds of line:
      - copies into RockWeb\Plugins\org_secc. Those folders are symlinks into the main
        checkout, so from a worktree the copy would write the worktree's block files into
        the main checkout.
      - copies into RockWeb\Themes. Themes are managed by hand.
    Everything else runs as written: third-party DLLs into Bin, and files outside Bin such as
    RockWeb\Scripts\CheckinClient (FamilyCheckin), RockWeb\GetStatement.ashx (Finance),
    RockWeb\Assets and RockWeb\Styles (PayPalExpress, CustomIcons) and RockWeb\LibSass (Sass).
    Like a Visual Studio build, that can leave changes in the ..\Rock working copy.

    Plugins with no csproj (Event, Connection, ...) are compiled at runtime by RockWeb; naming
    one prints a skip message. Test projects (*.Tests) are not built.

    Copy mode needs MSBuild 17.8 or later (for -getProperty). -NoCopy works with older MSBuild.

.PARAMETER Plugin
    Plugin names or csproj paths. Names may omit the "org.secc." prefix and match
    case-insensitively (e.g. Workflow, org.secc.Workflow, FamilyCheckin).
    When omitted, builds every plugin with changes vs origin/master (falls back to master;
    committed, staged, unstaged, or untracked).

.PARAMETER All
    Build every plugin csproj under Plugins\.

.PARAMETER Configuration
    Debug (default, matches Visual Studio) or Release.

.PARAMETER RockPath
    Rock checkout. Defaults to the Rock folder next to this repo.

.PARAMETER MSBuildPath
    MSBuild.exe to use. Defaults to the newest Visual Studio or Build Tools install.

.PARAMETER SkipStaleCheck
    Skip the Rock-source-vs-Bin timestamp check.

.PARAMETER NoCopy
    Build only (compile check): copy nothing into RockWeb and run no pre/post-build steps.

.EXAMPLE
    .\Tools\build-plugin.ps1 Workflow
.EXAMPLE
    .\Tools\build-plugin.ps1 Workflow,Event -NoCopy
.EXAMPLE
    .\Tools\build-plugin.ps1            # whatever changed on this branch
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string[]] $Plugin,
    [switch] $All,
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',
    [string] $RockPath,
    [string] $MSBuildPath,
    [switch] $SkipStaleCheck,
    [switch] $NoCopy
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $RockPath) {
    # Rock sits next to the main checkout. From a worktree (.claude\worktrees\x), find the
    # main checkout through the shared git dir.
    $mainRoot = $repoRoot
    $commonDir = git -C $repoRoot rev-parse --path-format=absolute --git-common-dir 2>$null
    if ($commonDir) { $mainRoot = Split-Path -Parent $commonDir }
    $RockPath = Join-Path (Split-Path -Parent $mainRoot) 'Rock'
}
if (-not (Test-Path $RockPath)) { throw "Rock checkout not found at $RockPath. Pass -RockPath." }
$RockPath = (Resolve-Path $RockPath).Path.TrimEnd('\')
$rockBin = Join-Path $RockPath 'RockWeb\Bin'
$pluginsDir = Join-Path $repoRoot 'Plugins'

if (-not (Test-Path (Join-Path $rockBin 'Rock.dll'))) {
    throw "No Rock.dll in $rockBin. Build the Rock solution once first."
}

# --- MSBuild ---------------------------------------------------------------
# Copy mode reads project properties with -getProperty, which MSBuild added in 17.8.
$minMSBuild = [version]'17.8'
$msbuild = $null
if ($MSBuildPath) {
    if (-not (Test-Path $MSBuildPath -PathType Leaf)) { throw "MSBuild not found at $MSBuildPath." }
    $msbuild = (Resolve-Path $MSBuildPath).Path
} else {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path $vswhere) {
        # -products * includes Build Tools installs. Prefer a new enough instance.
        $msbuild = & $vswhere -latest -prerelease -products * -version "[$minMSBuild," `
            -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' |
            Select-Object -First 1
        if (-not $msbuild) {
            $msbuild = & $vswhere -latest -prerelease -products * `
                -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' |
                Select-Object -First 1
        }
    }
    if (-not $msbuild) { $msbuild = (Get-Command msbuild.exe -ErrorAction SilentlyContinue).Source }
    if (-not $msbuild) { throw 'MSBuild.exe not found (install Visual Studio or Build Tools).' }
}
$msbuildVersion = [version]((& $msbuild -version -nologo | Select-Object -Last 1).Trim())
if (-not $NoCopy -and $msbuildVersion -lt $minMSBuild) {
    throw "MSBuild $minMSBuild or later is required to copy outputs (found $msbuildVersion at $msbuild). " +
        'Install Visual Studio 2022 17.8+ or Build Tools, pass -MSBuildPath, or use -NoCopy for a compile check.'
}

# --- Stale Rock check ------------------------------------------------------
if (-not $SkipStaleCheck) {
    $stale = @()
    foreach ($proj in Get-ChildItem $RockPath -Directory -Filter 'Rock*' |
            Where-Object { $_.Name -notmatch '^Rock\.Tests|^RockWeb$|^Rock\.CodeGeneration$' } |
            ForEach-Object { Get-ChildItem $_.FullName -Filter *.csproj -File | Select-Object -First 1 }) {
        $asm = ([xml](Get-Content $proj.FullName -Raw)).Project.PropertyGroup.AssemblyName |
            Where-Object { $_ } | Select-Object -First 1
        if (-not $asm) { $asm = $proj.Directory.Name }
        $dll = Join-Path $rockBin "$asm.dll"
        if (-not (Test-Path $dll)) { continue }
        $dllTime = (Get-Item $dll).LastWriteTime
        $newer = Get-ChildItem $proj.DirectoryName -Recurse -File -Include *.cs, *.csproj |
            Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.LastWriteTime -gt $dllTime } |
            Sort-Object LastWriteTime -Descending
        if ($newer) {
            $stale += [pscustomobject]@{
                Assembly = "$asm.dll"
                Built    = $dllTime.ToString('yyyy-MM-dd HH:mm')
                Newest   = $newer[0].LastWriteTime.ToString('yyyy-MM-dd HH:mm')
                Files    = @($newer).Count
                Example  = $newer[0].FullName.Substring($RockPath.Length + 1)
            }
        }
    }
    if ($stale) {
        $branch = git -C $RockPath branch --show-current 2>$null
        Write-Warning "Rock source ($branch) has changes newer than these assemblies in RockWeb\Bin:"
        $stale | Format-Table -AutoSize | Out-String | Write-Host -ForegroundColor Yellow
        Write-Warning 'Plugins will compile against the OLD Rock. Rebuild Rock (Visual Studio: Build > Build Solution) if those changes matter, or pass -SkipStaleCheck.'
    }
}

# --- Resolve plugin list ---------------------------------------------------
# Top-level csproj per plugin, or <Plugin>\<Plugin>\<Plugin>.csproj (LinkList layout).
# Test projects are skipped. Plugins with no csproj compile at runtime in RockWeb.
$allProjects = Get-ChildItem $pluginsDir -Directory | ForEach-Object {
    $top = Get-ChildItem $_.FullName -Filter *.csproj -File
    if ($top) { $top }
    else {
        $nested = Join-Path $_.FullName "$($_.Name)\$($_.Name).csproj"
        if (Test-Path $nested) { Get-Item $nested }
    }
} | Where-Object { $_ -and $_.BaseName -notlike '*.Tests' }

# Returns the csproj to build, or nothing for a runtime-compiled plugin. Throws for unknown
# names, ambiguous names and test projects.
function Resolve-Plugin([string] $name) {
    $n = $name.Trim()
    if (Test-Path $n -PathType Leaf) { return Get-Item $n }
    $short = $n -replace '^org\.secc\.', ''
    if ($short -like '*.Tests') { throw "'$n' is a test project. This script does not build tests." }
    $hit = $allProjects | Where-Object { $_.BaseName -ieq "org.secc.$short" }
    if ($hit) { return $hit }
    # A plugin folder with no csproj at all holds runtime-compiled blocks. Checked before the
    # substring match so 'Connection' doesn't resolve to ConnectionCards/ConnectionsDigest.
    $dir = Join-Path $pluginsDir "org.secc.$short"
    if (Test-Path $dir -PathType Container) {
        if (-not (Get-ChildItem $dir -Filter *.csproj -File -Recurse -Depth 1)) {
            Write-Host "Skipping $((Get-Item $dir).Name): no csproj (compiled at runtime by RockWeb)."
            return
        }
    }
    $hit = $allProjects | Where-Object { $_.BaseName -ilike "*$short*" }
    if (@($hit).Count -ne 1) {
        $list = if ($hit) { ($hit.BaseName -join ', ') } else { 'none' }
        throw "Plugin '$name' matched $(@($hit).Count) projects ($list). Be more specific."
    }
    return $hit
}

if ($All) {
    $targets = $allProjects
} elseif ($Plugin) {
    $targets = $Plugin | ForEach-Object { $_ -split ',' } | Where-Object { $_.Trim() } |
        ForEach-Object { Resolve-Plugin $_ }
} else {
    $baseRef = 'origin/master'
    git -C $repoRoot rev-parse --verify --quiet $baseRef *> $null
    if ($LASTEXITCODE -ne 0) { $baseRef = 'master' }
    $base = git -C $repoRoot merge-base HEAD $baseRef 2>$null
    $changed = @(
        if ($base) { git -C $repoRoot diff --name-only $base HEAD -- Plugins }
        git -C $repoRoot diff --name-only HEAD -- Plugins
        git -C $repoRoot ls-files --others --exclude-standard -- Plugins
    )
    $dirs = $changed | Where-Object { $_ -match '^Plugins/([^/]+)/' } |
        ForEach-Object { $Matches[1] } | Sort-Object -Unique
    $targets = $allProjects | Where-Object {
        $rel = $_.FullName.Substring($pluginsDir.Length + 1)
        $dirs -contains ($rel -split '\\')[0]
    }
    if (-not $targets) {
        Write-Host 'No changed plugins vs master. Pass plugin names or -All.'
        return
    }
}
$targets = @($targets | Where-Object { $_ } | Sort-Object FullName -Unique)
if (-not $targets) {
    Write-Host 'Nothing to build.'
    return
}

# --- Build events ----------------------------------------------------------
$solutionDir = "$RockPath\"
# MSBuild wants SolutionDir to end in '\'. With a space in the path PowerShell quotes the
# argument, and on Windows PowerShell 5.1 (Legacy argument passing) the trailing '\"' then
# escapes the closing quote. PowerShell 7.3+ escapes it itself.
if ($RockPath -match '\s' -and
    (Get-Variable PSNativeCommandArgumentPassing -ValueOnly -ErrorAction SilentlyContinue) -in $null, 'Legacy') {
    $solutionDir += '\'
}
$skipEventLine = '\\RockWeb\\(Plugins\\org_secc|Themes)(\\|"|\s|$)'
$eventsRun = @{}   # csproj full path (lower case) -> $true, so each project's events run once

# org.secc csproj files referenced by a project, transitively, dependencies first.
function Get-ProjectClosure([string] $csprojPath, [hashtable] $seen = @{}) {
    $key = $csprojPath.ToLowerInvariant()
    if ($seen.ContainsKey($key)) { return }
    $seen[$key] = $true
    $dir = Split-Path -Parent $csprojPath
    $refs = ([xml](Get-Content $csprojPath -Raw)).SelectNodes("//*[local-name()='ProjectReference']")
    foreach ($ref in $refs) {
        $path = [IO.Path]::GetFullPath((Join-Path $dir $ref.GetAttribute('Include')))
        if ((Test-Path $path) -and [IO.Path]::GetFileNameWithoutExtension($path) -notlike '*.Tests') {
            Get-ProjectClosure $path $seen
        }
    }
    $csprojPath
}

# Evaluated (macro-expanded) properties of a project, as MSBuild sees them for this build.
function Get-ProjectProperties([string] $csprojPath) {
    $json = & $msbuild $csprojPath "/p:Configuration=$Configuration" "/p:SolutionDir=$solutionDir" `
        -getProperty:TargetDir -getProperty:PreBuildEvent -getProperty:PostBuildEvent -nologo |
        Out-String
    if ($LASTEXITCODE -ne 0) { throw "Could not read properties of ${csprojPath}:`n$json" }
    ($json | ConvertFrom-Json).Properties
}

# Runs the project's pre- and post-build events minus the org_secc and Themes copies.
# Like MSBuild's Exec, all lines go in one .cmd file and the exit code is the last line's.
# Returns $true on success.
function Invoke-BuildEvents([string] $csprojPath, $props) {
    $name = [IO.Path]::GetFileNameWithoutExtension($csprojPath)
    foreach ($event in 'PreBuildEvent', 'PostBuildEvent') {
        $lines = @("$($props.$event)" -split '\r?\n' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
        if (-not $lines) { continue }
        $keep = @($lines | Where-Object { $_ -notmatch $skipEventLine })
        $skipped = $lines.Count - $keep.Count
        $note = if ($skipped) { ", skipped $skipped org_secc/Themes line(s)" } else { '' }
        if (-not $keep) {
            Write-Host "  $name ${event}: nothing to run$note"
            continue
        }
        $cmdFile = Join-Path ([IO.Path]::GetTempPath()) "build-plugin-$([guid]::NewGuid().ToString('N')).cmd"
        Set-Content -Path $cmdFile -Value $keep -Encoding Ascii
        # On Windows PowerShell 5.1, 'Stop' turns any stderr line from 2>&1 into a terminating
        # error; the exit code below decides success instead.
        $eap = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $out = & cmd.exe /Q /D /C $cmdFile 2>&1 | ForEach-Object { "$_" } | Out-String
            $code = $LASTEXITCODE
        } finally {
            $ErrorActionPreference = $eap
            Remove-Item $cmdFile -ErrorAction SilentlyContinue
        }
        if ($code -ne 0) {
            Write-Host "  $name $event FAILED (exit $code):" -ForegroundColor Red
            $keep | ForEach-Object { Write-Host "    $_" }
            Write-Host $out.TrimEnd()
            return $false
        }
        Write-Host "  $name ${event}: ran $($keep.Count) line(s)$note"
    }
    return $true
}

# --- Build -----------------------------------------------------------------
$failed = @()
$sw = [Diagnostics.Stopwatch]::StartNew()

foreach ($csproj in $targets) {
    Write-Host "`n=== $($csproj.BaseName) ($Configuration) ===" -ForegroundColor Cyan
    $msArgs = @(
        $csproj.FullName
        '-restore'   # SDK-style projects (LinkList) need project.assets.json in a fresh worktree
        "/p:Configuration=$Configuration"
        "/p:SolutionDir=$solutionDir"
        '/p:PreBuildEvent='    # both events run below, filtered (see .DESCRIPTION)
        '/p:PostBuildEvent='
        '/v:m', '/nologo', '/clp:ErrorsOnly;Summary'
    )
    # SDK-style plugins (LinkList) copy to RockWeb\Bin in their own target; honor -NoCopy there too.
    if ($NoCopy) { $msArgs += '/p:CopyToRockWeb=false' }
    & $msbuild @msArgs
    if ($LASTEXITCODE -ne 0) {
        $failed += $csproj.BaseName
        continue
    }
    if ($NoCopy) { continue }

    $ok = $true
    foreach ($proj in Get-ProjectClosure $csproj.FullName) {
        $key = $proj.ToLowerInvariant()
        $isTarget = $proj -ieq $csproj.FullName
        if ($eventsRun.ContainsKey($key) -and -not $isTarget) { continue }
        $props = Get-ProjectProperties $proj
        if ($isTarget) {
            # Plugin DLL plus any org.secc.* project references that were copied local. Covers
            # projects whose events don't copy their own DLL (AI, CustomIcons, SportsAndFitness).
            Get-ChildItem $props.TargetDir -File |
                Where-Object { $_.Name -like 'org.secc.*' -and $_.Extension -in '.dll', '.pdb' } |
                ForEach-Object {
                    Copy-Item $_.FullName $rockBin -Force
                    Write-Host "  -> RockWeb\Bin\$($_.Name)"
                }
        }
        if (-not $eventsRun.ContainsKey($key)) {
            $eventsRun[$key] = $true
            if (-not (Invoke-BuildEvents $proj $props)) { $ok = $false }
        }
    }
    if (-not $ok) { $failed += "$($csproj.BaseName) (build events)" }
}

$sw.Stop()
Write-Host ''
if ($failed) {
    Write-Host "FAILED: $($failed -join ', ')  ($([int]$sw.Elapsed.TotalSeconds)s)" -ForegroundColor Red
    exit 1
}
$elapsed = [int]$sw.Elapsed.TotalSeconds
if ($NoCopy) {
    Write-Host "Built $($targets.Count) plugin(s) in ${elapsed}s (not copied)." -ForegroundColor Green
} else {
    Write-Host "Built and copied $($targets.Count) plugin(s) in ${elapsed}s. The site recycles on the Bin change; first request will be slow." -ForegroundColor Green
}
