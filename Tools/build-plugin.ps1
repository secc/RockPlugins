<#
.SYNOPSIS
    Build one or more org.secc plugins against the prebuilt Rock in ..\Rock\RockWeb\Bin,
    without rebuilding the Rock solution.

.DESCRIPTION
    Plugins reference Rock as a DLL ($(SolutionDir)RockWeb\Bin\Rock.dll), so they only need
    Rock to have been built once. This script:
      1. Warns when a Rock assembly in RockWeb\Bin is older than its source (stale Rock build).
      2. Builds the requested plugin csproj files with MSBuild, SolutionDir pointed at ..\Rock.
      3. Copies the plugin DLL/PDB (and any org.secc.* project references) into RockWeb\Bin.

    The csproj PostBuildEvent is suppressed. It xcopies org_secc\ into RockWeb\Plugins, which
    is unnecessary here: those folders are symlinked back into this repo.

.PARAMETER Plugin
    Plugin names or csproj paths. Names may omit the "org.secc." prefix and match
    case-insensitively (e.g. Workflow, org.secc.Workflow, RegistrationCart).
    When omitted, builds every plugin with changes vs origin/master (falls back to master; committed, staged, unstaged,
    or untracked).

.PARAMETER All
    Build every plugin csproj under Plugins\.

.PARAMETER Configuration
    Debug (default, matches Visual Studio) or Release.

.PARAMETER RockPath
    Rock checkout. Defaults to the Rock folder next to this repo.

.PARAMETER SkipStaleCheck
    Skip the Rock-source-vs-Bin timestamp check.

.PARAMETER NoCopy
    Build only; do not copy outputs into RockWeb\Bin (compile check).

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
$msbuild = $null
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (Test-Path $vswhere) {
    $msbuild = & $vswhere -latest -prerelease -requires Microsoft.Component.MSBuild `
        -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
}
if (-not $msbuild) { $msbuild = (Get-Command msbuild.exe -ErrorAction SilentlyContinue).Source }
if (-not $msbuild) { throw 'MSBuild.exe not found (install Visual Studio or Build Tools).' }

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

function Resolve-Plugin([string] $name) {
    if (Test-Path $name -PathType Leaf) { return Get-Item $name }
    $n = $name.Trim()
    $hit = $allProjects | Where-Object {
        $_.BaseName -ieq $n -or $_.BaseName -ieq "org.secc.$n"
    }
    if (-not $hit) {
        $hit = $allProjects | Where-Object { $_.BaseName -ilike "*$n*" }
    }
    if (@($hit).Count -ne 1) {
        $list = if ($hit) { ($hit.BaseName -join ', ') } else { 'none' }
        throw "Plugin '$name' matched $(@($hit).Count) projects ($list). Be more specific."
    }
    return $hit
}

if ($All) {
    $targets = $allProjects
} elseif ($Plugin) {
    $targets = $Plugin | ForEach-Object { $_ -split ',' } | Where-Object { $_ } |
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
$targets = @($targets | Sort-Object FullName -Unique)

# --- Build -----------------------------------------------------------------
$solutionDir = "$RockPath\"
$failed = @()
$sw = [Diagnostics.Stopwatch]::StartNew()

foreach ($csproj in $targets) {
    Write-Host "`n=== $($csproj.BaseName) ($Configuration) ===" -ForegroundColor Cyan
    $msArgs = @(
        $csproj.FullName
        '-restore'   # SDK-style projects (LinkList) need project.assets.json in a fresh worktree
        "/p:Configuration=$Configuration"
        "/p:SolutionDir=$solutionDir\"
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

    $targetPath = (& $msbuild $csproj.FullName "/p:Configuration=$Configuration" `
            "/p:SolutionDir=$solutionDir\" -getProperty:TargetPath -nologo).Trim()
    $outDir = Split-Path -Parent $targetPath
    # Plugin DLL plus any org.secc.* project references that were copied local.
    Get-ChildItem $outDir -File |
        Where-Object { $_.Name -like 'org.secc.*' -and $_.Extension -in '.dll', '.pdb' } |
        ForEach-Object {
            Copy-Item $_.FullName $rockBin -Force
            Write-Host "  -> RockWeb\Bin\$($_.Name)"
        }
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
