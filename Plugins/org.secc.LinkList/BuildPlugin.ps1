# Builds the LinkList plugin. Unlike our legacy plugins, this one uses the new
# Rock plugin format (rock-dev-tool style): an SDK-style C# project that restores
# via NuGet PackageReference, plus an Obsidian (Vue/TypeScript) frontend project
# compiled by @sparkdevnetwork/obsidian-build-tools.
#
# The pipeline invokes this via the existing BuildPlugin.ps1 hook, so no pipeline
# changes are required — but the build agent must have Node.js/npm available.

$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath
Push-Location $dir

# Locate RockWeb. First candidate matches the Azure DevOps pipeline layout
# (RockWeb at the root of Build.SourcesDirectory, plugins repo as a sibling).
# Second candidate matches a local rock-dev-tool environment layout.
$rockWeb = $null
ForEach ($candidate in @("..\..\..\RockWeb", "..\Rock\RockWeb")) {
    if (Test-Path $candidate) {
        $rockWeb = (Resolve-Path $candidate).Path
        break
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
# RockWebPath feeds the DotLiquid/HtmlAgilityPack HintPaths and the
# CopyToRockWeb step (SparkDevNetwork.Rock.Build.Tasks copies the compiled
# DLL into RockWeb\Bin).
& $msbuild -m -restore "org.secc.LinkList\org.secc.LinkList.csproj" /p:Configuration=Release /p:RockWebPath="$rockWeb"
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    throw "org.secc.LinkList C# build failed!"
}

# --- 2. Obsidian (Vue) frontend ---
# npm run build = vue-tsc typecheck + obsidian-build + web component bundle.
# obsidian-build's own copy phase is skipped automatically in CI (no local
# dev environment is present), so we copy dist to RockWeb ourselves below.
Push-Location "org.secc.LinkList.Obsidian"
& npm ci --no-audit --no-fund
if ($LASTEXITCODE -ne 0) {
    Pop-Location; Pop-Location
    throw "org.secc.LinkList: npm ci failed!"
}
& npm run build
if ($LASTEXITCODE -ne 0) {
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
