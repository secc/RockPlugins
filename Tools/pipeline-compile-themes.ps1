<#
.SYNOPSIS
    Compiles the SCSS of every theme in RockWeb\Themes to CSS for the Azure DevOps "Rock v16 YAML"
    pipeline (azure-pipelines.yml at the repo root, "Compile theme SCSS" step).

.DESCRIPTION
    Does at build time what the org.secc.Sass plugin does on every app start, with the same
    compiler (SharpScss 1.3.8 / LibSass), so the CSS in the build is the CSS the servers serve
    today. For each theme folder with a Styles\ folder and no Styles\.nocompile file, every
    .scss file under Styles\ at any depth whose name doesn't start with "_" is compiled to a
    .css file next to it.

    Run it after "Build Plugins" has copied every plugin's Themes\ into RockWeb\Themes. It has to
    run on that merged tree, not on the plugin folders, because themes import from each other and
    from RockWeb\Styles by relative path (SEKids2020 imports ..\..\SECC2019\Styles\mixins and
    ..\..\..\Styles\FontAwesome\Scss\fontawesome).

    The Font Awesome Pro SCSS that the SEKids2020 themes import is licensed, so it is in neither
    repo. Pass it as a zip with -FontAwesomeScssZip (the pipeline downloads it as a secure file)
    and it is extracted to RockWeb\Styles\FontAwesome\Scss before compiling.

    Any file that fails to compile fails the build. LESS is not compiled here: Rock compiles LESS
    itself on app start.

    Windows PowerShell 5.1 or PowerShell 7 on 64-bit Windows.

.PARAMETER RockWeb
    The RockWeb folder to compile in. Defaults to $env:BUILD_SOURCESDIRECTORY\RockWeb.

.PARAMETER FontAwesomeScssZip
    Zip of the Font Awesome Pro .scss files (fontawesome.scss, _variables.scss, ... at the zip
    root or in one top-level folder).

.PARAMETER SharpScssPackage
    An extracted SharpScss 1.3.8 package folder (with lib\ and runtimes\) to use instead of
    downloading it from nuget.org, e.g. ..\Rock\packages\SharpScss.1.3.8 on a dev machine.

.EXAMPLE
    # Dev machine: compile a scratch copy of the merged themes.
    Tools\pipeline-compile-themes.ps1 -RockWeb C:\scratch\RockWeb -SharpScssPackage ..\Rock\packages\SharpScss.1.3.8
#>
[CmdletBinding()]
param(
    [string] $RockWeb = $(if ($env:BUILD_SOURCESDIRECTORY) { Join-Path $env:BUILD_SOURCESDIRECTORY 'RockWeb' }),
    [string] $FontAwesomeScssZip,
    [string] $SharpScssPackage
)

$ErrorActionPreference = 'Stop'
$started = Get-Date

# Same package and version the org.secc.Sass plugin shipped. Changing either changes the CSS.
$sharpScssVersion = '1.3.8'
$sharpScssSha256 = '5FDC443878F668BC620BE7498E1BD3259630F18C1CADFEC34A912A491B2F4A1E'

if (-not $RockWeb) { throw "RockWeb not set (pass -RockWeb or set BUILD_SOURCESDIRECTORY)." }
$RockWeb = [IO.Path]::GetFullPath($RockWeb)
$themesRoot = Join-Path $RockWeb 'Themes'
if (-not (Test-Path $themesRoot)) { throw "Themes folder not found: $themesRoot" }
if (-not [Environment]::Is64BitProcess) { throw "Run this in a 64-bit PowerShell (the win-x64 LibSass is loaded)." }

$tempRoot = if ($env:AGENT_TEMPDIRECTORY) { $env:AGENT_TEMPDIRECTORY } else { [IO.Path]::GetTempPath() }

# --- 1. SharpScss / LibSass --------------------------------------------------------------
if (-not $SharpScssPackage) {
    $SharpScssPackage = Join-Path $tempRoot "SharpScss.$sharpScssVersion"
    if (-not (Test-Path (Join-Path $SharpScssPackage 'lib'))) {
        $nupkg = Join-Path $tempRoot "sharpscss.$sharpScssVersion.zip"
        Write-Host "Downloading SharpScss $sharpScssVersion . . ."
        [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest "https://api.nuget.org/v3-flatcontainer/sharpscss/$sharpScssVersion/sharpscss.$sharpScssVersion.nupkg" -OutFile $nupkg -UseBasicParsing
        $hash = (Get-FileHash $nupkg -Algorithm SHA256).Hash
        if ($hash -ne $sharpScssSha256) { throw "SharpScss package hash is $hash, expected $sharpScssSha256." }
        Expand-Archive $nupkg -DestinationPath $SharpScssPackage -Force
    }
}
$libFolder = if ($PSVersionTable.PSEdition -eq 'Core') { 'netstandard1.3' } else { 'net40' }
$sharpScssDll = Join-Path $SharpScssPackage "lib\$libFolder\SharpScss.dll"
$libSassDir = Join-Path $SharpScssPackage 'runtimes\win-x64\native'
foreach ($path in $sharpScssDll, (Join-Path $libSassDir 'libsass.dll')) {
    if (-not (Test-Path $path)) { throw "Not found: $path" }
}
$env:PATH = "$libSassDir;$env:PATH"
Add-Type -Path $sharpScssDll

# --- 2. Font Awesome Pro SCSS ------------------------------------------------------------
if ($FontAwesomeScssZip) {
    if (-not (Test-Path $FontAwesomeScssZip)) { throw "Font Awesome SCSS zip not found: $FontAwesomeScssZip" }
    $extracted = Join-Path $tempRoot ('fa-scss-' + [guid]::NewGuid().ToString('N'))
    Expand-Archive $FontAwesomeScssZip -DestinationPath $extracted -Force
    $entry = Get-ChildItem $extracted -Recurse -File -Filter 'fontawesome.scss' | Select-Object -First 1
    if (-not $entry) { throw "No fontawesome.scss in $FontAwesomeScssZip" }
    $target = Join-Path $RockWeb 'Styles\FontAwesome\Scss'
    New-Item -ItemType Directory -Force $target | Out-Null
    Copy-Item (Join-Path $entry.DirectoryName '*.scss') $target -Force
    Write-Host "Font Awesome SCSS: $(@(Get-ChildItem $target -Filter '*.scss').Count) files in $target"
}

# --- 3. Compile --------------------------------------------------------------------------
$compiled = 0
$failures = @()
foreach ($theme in Get-ChildItem $themesRoot -Directory | Sort-Object Name) {
    $styles = Join-Path $theme.FullName 'Styles'
    if (-not (Test-Path $styles)) { continue }
    $sources = @(
        Get-ChildItem $styles -Recurse -File -Filter '*.scss' |
            Where-Object { -not $_.Name.StartsWith('_') } |
            Sort-Object FullName
    )
    if (-not $sources) { continue }
    if (Test-Path (Join-Path $styles '.nocompile')) {
        Write-Host "$($theme.Name): skipped (Styles\.nocompile)"
        continue
    }

    $themeStarted = Get-Date
    foreach ($source in $sources) {
        $label = $source.FullName.Substring($themesRoot.Length + 1)
        $cssName = $source.Name.Replace('.scss', '.css')
        $options = New-Object SharpScss.ScssOptions
        $options.InputFile = $source.FullName
        $options.OutputFile = $cssName   # only used in error messages; nothing is written by LibSass
        $options.GenerateSourceMap = $false
        try {
            $result = [SharpScss.Scss]::ConvertToCss([IO.File]::ReadAllText($source.FullName), $options)
            [IO.File]::WriteAllText((Join-Path $source.DirectoryName $cssName), $result.Css)
            $compiled++
        }
        catch {
            $inner = $_.Exception
            while ($inner.InnerException) { $inner = $inner.InnerException }
            $failures += "$label`: $($inner.Message.Trim())"
        }
    }
    Write-Host ("{0}: {1} file(s), {2:N1}s" -f $theme.Name, $sources.Count, ((Get-Date) - $themeStarted).TotalSeconds)
}

if ($failures) {
    $failures | ForEach-Object { Write-Host "##vso[task.logissue type=error]$_" }
    throw "$($failures.Count) SCSS file(s) failed to compile:`n$($failures -join "`n")"
}
if ($compiled -eq 0) { throw "No SCSS files were compiled under $themesRoot." }

Write-Host ("Theme SCSS compiled in {0:N0}s: {1} files." -f ((Get-Date) - $started).TotalSeconds, $compiled)
