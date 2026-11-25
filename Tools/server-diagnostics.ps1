<#
.SYNOPSIS
    Prints how a Rock web server serves its site: IIS sites, applications and virtual
    directories, what is in the local Themes and Styles folders, and the compiled theme CSS.

.DESCRIPTION
    Read-only. It changes nothing on the server: it lists IIS configuration with appcmd, lists
    folders, and hashes .css files. It never prints virtual directory credentials or web.config.

    Meant to run on every node of a deployment group from an Azure DevOps release (paste it into
    an inline PowerShell step, or run it from a checkout), so the output of all nodes lands in
    the release logs. It also runs by hand in an elevated PowerShell on a server.

    For each folder IIS serves from somewhere other than the site root (a share), it reports the
    local folder of the same name next to it, because that local folder is what the site would
    serve if the virtual directory were removed.

    Windows PowerShell 5.1 or later. Needs administrator rights (appcmd).

.PARAMETER Site
    IIS site to report on. Defaults to "Default Web Site".

.PARAMETER HashFolders
    Folders under the site root whose compiled CSS is listed with a hash, both the local copy
    and the copy the virtual directory points to. Defaults to Themes.

.PARAMETER SkipThemes
    Theme folder names to leave out of the CSS listing (large retired themes).

.PARAMETER AppCmd
    Path of appcmd.exe. Defaults to the one in %windir%\System32\inetsrv.

.EXAMPLE
    Tools\server-diagnostics.ps1
#>
[CmdletBinding()]
param(
    [string] $Site = 'Default Web Site',
    [string[]] $HashFolders = @('Themes'),
    [string[]] $SkipThemes = @('KyleIdlman17'),
    [string] $AppCmd = (Join-Path $env:windir 'System32\inetsrv\appcmd.exe')
)

$ErrorActionPreference = 'Stop'

function Write-Section([string] $title) {
    Write-Host ''
    Write-Host "=== $title ==="
}

# Summary of a folder: whether it can be read, how many entries it has and the newest one.
function Get-FolderSummary([string] $path) {
    try {
        if (-not (Test-Path -LiteralPath $path)) { return 'missing' }
        $children = @(Get-ChildItem -LiteralPath $path -Force)
        if (-not $children) { return 'empty' }
        $newest = $children | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        return ('{0} entries, newest {1:yyyy-MM-dd HH:mm} ({2})' -f $children.Count, $newest.LastWriteTime, $newest.Name)
    }
    catch {
        return "cannot read: $($_.Exception.Message)"
    }
}

# Every .css under <root>\<theme>\Styles, as "relative path | bytes | modified | sha256".
function Write-CssListing([string] $root) {
    try {
        if (-not (Test-Path -LiteralPath $root)) { Write-Host "  missing: $root"; return }
        $count = 0
        foreach ($theme in Get-ChildItem -LiteralPath $root -Directory | Sort-Object Name) {
            if ($SkipThemes -contains $theme.Name) { continue }
            $styles = Join-Path $theme.FullName 'Styles'
            if (-not (Test-Path -LiteralPath $styles)) { continue }
            foreach ($file in Get-ChildItem -LiteralPath $styles -Recurse -File -Filter '*.css' | Sort-Object FullName) {
                $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.Substring(0, 16)
                Write-Host ('  {0} | {1} | {2:yyyy-MM-dd HH:mm} | {3}' -f $file.FullName.Substring($root.Length).TrimStart('\'), $file.Length, $file.LastWriteTime, $hash)
                $count++
            }
        }
        Write-Host "  $count file(s)"
    }
    catch {
        Write-Host "  cannot read ${root}: $($_.Exception.Message)"
    }
}

Write-Section 'Server'
Write-Host "Computer: $env:COMPUTERNAME"
Write-Host "Time:     $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')"
Write-Host "Runs as:  $([Security.Principal.WindowsIdentity]::GetCurrent().Name)"
Write-Host "PS:       $($PSVersionTable.PSVersion)"

if (-not (Test-Path $AppCmd)) { throw "appcmd not found: $AppCmd (is IIS installed?)" }

# The default appcmd output is one line per object with its path. Never add /config or
# /text:* here: those print virtual directory user names and passwords.
foreach ($object in 'site', 'apppool', 'app', 'vdir') {
    Write-Section "IIS $object"
    & $AppCmd list $object | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) { throw "appcmd list $object failed (exit code $LASTEXITCODE). Run this as administrator." }
}

# name -> physical path of the site's virtual directories ("/" is the site root).
$vdirs = [ordered]@{}
foreach ($line in & $appcmd list vdir) {
    if ($line -match '^VDIR "(?<name>[^"]+)" \(physicalPath:(?<path>.*)\)$' -and $Matches.name.StartsWith("$Site/")) {
        $vdirs[$Matches.name.Substring($Site.Length)] = [Environment]::ExpandEnvironmentVariables($Matches.path)
    }
}
$siteRoot = $vdirs['/']
if (-not $siteRoot) { throw "Site '$Site' has no root virtual directory." }

Write-Section "Site root $siteRoot"
$drive = Get-PSDrive -Name $siteRoot.Substring(0, 1) -ErrorAction SilentlyContinue
if ($drive) { Write-Host ('Free space: {0:N1} GB of {1:N1} GB' -f ($drive.Free / 1GB), (($drive.Free + $drive.Used) / 1GB)) }
$links = @(Get-ChildItem -LiteralPath $siteRoot -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint })
if ($links) { $links | ForEach-Object { Write-Host "Link: $($_.Name) -> $($_.Target)" } } else { Write-Host 'Links (junctions/symlinks): none' }
Get-ChildItem -LiteralPath $siteRoot -Directory -Force | Sort-Object Name | ForEach-Object {
    Write-Host ('  {0,-20} {1:yyyy-MM-dd HH:mm}' -f $_.Name, $_.LastWriteTime)
}

Write-Section 'Folders served from outside the site root'
foreach ($name in $vdirs.Keys | Where-Object { $_ -ne '/' }) {
    $local = Join-Path $siteRoot $name.TrimStart('/')
    Write-Host $name
    Write-Host "  served from: $($vdirs[$name])"
    Write-Host "               $(Get-FolderSummary $vdirs[$name])"
    Write-Host "  local:       $local"
    Write-Host "               $(Get-FolderSummary $local)"
}

foreach ($folder in $HashFolders) {
    $local = Join-Path $siteRoot $folder
    Write-Section "Compiled CSS, local $local"
    Write-CssListing $local
    $served = $vdirs["/$folder"]
    if ($served) {
        Write-Section "Compiled CSS, served $served"
        Write-CssListing $served
    }
}

Write-Section 'org.secc.Sass plugin files'
foreach ($relative in 'Bin\org.secc.Sass.dll', 'Bin\SharpScss.dll', 'LibSass', 'Plugins\org_secc\Sass') {
    $path = Join-Path $siteRoot $relative
    $state = if (Test-Path -LiteralPath $path) { 'present, {0:yyyy-MM-dd HH:mm}' -f (Get-Item -LiteralPath $path).LastWriteTime } else { 'absent' }
    Write-Host ('  {0,-28} {1}' -f $relative, $state)
}
