# Builds the LinkList plugin: an SDK-style C# project (Rock assemblies come
# from RockWeb\Bin via the repo-root Directory.Build.props/.targets, not NuGet),
# plus an Obsidian (Vue/TypeScript) frontend project compiled by
# @sparkdevnetwork/obsidian-build-tools.
#
# The pipeline invokes this via the existing BuildPlugin.ps1 hook, so no pipeline
# changes are required — but the build agent must have Node.js/npm available.
#
# Locally, pass -RockWebPath to build into a specific RockWeb (for example a
# private copy, so a running site's Bin isn't touched).

# -RockWebPath binds by name only. Any positional arguments the pipeline hook
# passes land in $Remaining and are ignored, as they were before this script
# took parameters.
[CmdletBinding(PositionalBinding = $false)]
param(
    [string]$RockWebPath,

    [Parameter(ValueFromRemainingArguments = $true)]
    $Remaining
)

$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath

# Locate RockWeb. An explicit -RockWebPath wins, resolved against the caller's
# directory (before Push-Location below). Otherwise the first candidate
# matches the Azure DevOps pipeline layout (RockWeb at the root of
# Build.SourcesDirectory, plugins repo as a sibling) and the second matches a
# local rock-dev-tool environment layout.
$rockWeb = $null
if ($RockWebPath) {
    if (-not (Test-Path $RockWebPath)) {
        throw "LinkList: RockWebPath '$RockWebPath' does not exist!"
    }
    $rockWeb = (Resolve-Path $RockWebPath).Path
}

Push-Location $dir

if (-not $rockWeb) {
    ForEach ($candidate in @("..\..\..\RockWeb", "..\Rock\RockWeb")) {
        if (Test-Path $candidate) {
            $rockWeb = (Resolve-Path $candidate).Path
            break
        }
    }
}
if ($null -eq $rockWeb) {
    Pop-Location
    throw "LinkList: could not locate RockWeb directory!"
}
Write-Host "LinkList: using RockWeb at $rockWeb"

# Locate MSBuild. vswhere ships with every VS >= 15.2 (including Build Tools)
# at a fixed path, so this works regardless of edition/version on the agent.
# Fall back to msbuild on PATH (e.g. a Developer PowerShell or explicit setup).
$msbuild = $null
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vswhere) {
    $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
}
if (-not $msbuild) {
    $msbuildCmd = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($msbuildCmd) { $msbuild = $msbuildCmd.Source }
}
if (-not $msbuild) {
    Pop-Location
    throw "LinkList: could not locate MSBuild (vswhere found nothing and msbuild is not on PATH)!"
}
Write-Host "LinkList: using MSBuild at $msbuild"

# --- 1. C# project ---
# -restore handles NuGet (PackageReference; there is no packages.config).
# RockWebPath feeds the Rock assembly HintPaths (RockWeb\Bin) and the shared
# CopyRockPluginToRockWeb target, which copies the compiled DLL into RockWeb\Bin.
& $msbuild -m -restore "org.secc.LinkList\org.secc.LinkList.csproj" /p:Configuration=Release /p:RockWebPath="$rockWeb"
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    throw "org.secc.LinkList C# build failed!"
}

# --- 2. Obsidian (Vue) frontend ---
# Same steps as `npm run build` (vue-tsc typecheck + obsidian-build + web
# component bundle), except obsidian-build runs with its copy phase off.
# obsidian.config.json copies to $(Environment)/Rock/RockWeb whenever a
# rock-dev-tool environment.json sits above this folder, which would deploy
# the frontend to that site instead of (or as well as) $rockWeb. Step 3 copies
# dist to $rockWeb itself. The no-copy config is written next to
# obsidian.config.json because obsidian-build resolves paths from the
# config file's folder.
Push-Location "org.secc.LinkList.Obsidian"
& npm ci --no-audit --no-fund
if ($LASTEXITCODE -ne 0) {
    Pop-Location; Pop-Location
    throw "org.secc.LinkList: npm ci failed!"
}
$noCopyConfig = "obsidian.nocopy.config.json"
$buildExit = 1
try {
    $obsidianConfig = Get-Content "obsidian.config.json" -Raw | ConvertFrom-Json
    foreach ($entry in @($obsidianConfig)) { $entry | Add-Member -NotePropertyName copy -NotePropertyValue $false -Force }
    ConvertTo-Json -InputObject @($obsidianConfig) -Depth 10 | Set-Content $noCopyConfig -Encoding Ascii

    & npm run typecheck
    if ($LASTEXITCODE -eq 0) { & npx --no-install obsidian-build --config $noCopyConfig }
    if ($LASTEXITCODE -eq 0) { & npm run build:webcomponent }
    $buildExit = $LASTEXITCODE
}
finally {
    Remove-Item $noCopyConfig -ErrorAction SilentlyContinue
}
if ($buildExit -ne 0) {
    Pop-Location; Pop-Location
    throw "org.secc.LinkList: Obsidian build failed!"
}
Pop-Location

# --- 3. Copy compiled Obsidian output into RockWeb ---
# Rock resolves ObsidianFileUrl "~/Plugins/org_secc/LinkList/<block>.obs" to
# "<block>.obs.js" in this directory at runtime.
xcopy /Y /R /E /I "org.secc.LinkList.Obsidian\dist" "$rockWeb\Plugins\org_secc\LinkList"
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    throw "org.secc.LinkList: copying Obsidian dist to RockWeb failed!"
}

Pop-Location
