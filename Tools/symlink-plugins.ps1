param(
    [string]$RockPluginsPath,
    [string]$PluginsRepoPath
)

function Prompt-ForPath([string]$message)
{
    do
    {
        $path = Read-Host $message
    } while (-not $path)
    $path
}

if (-not $RockPluginsPath)
{
    $RockPluginsPath = Prompt-ForPath "Enter path to RockWeb\Plugins (e.g. C:\Rock\RockWeb\Plugins)"
}
if (-not $PluginsRepoPath)
{
    $PluginsRepoPath = Prompt-ForPath "Enter path to Plugins repo root (e.g. C:\Users\you\source\repos\RockPlugins-16)"
}

$rockOrgSeccPath = Join-Path $RockPluginsPath "org_secc"
$pluginsRoot     = Join-Path $PluginsRepoPath "Plugins"

if (-not (Test-Path $pluginsRoot))
{
    Write-Error "Plugins folder not found at $pluginsRoot"
    exit 1
}

if (-not (Test-Path $rockOrgSeccPath))
{
    New-Item -ItemType Directory -Path $rockOrgSeccPath | Out-Null
    Write-Host "Created $rockOrgSeccPath"
}

Get-ChildItem -Path $pluginsRoot -Directory | ForEach-Object {
    $pluginFolder     = $_.FullName
    $pluginOrgSecc    = Join-Path $pluginFolder "org_secc"

    if (-not (Test-Path $pluginOrgSecc)) { return }

    Get-ChildItem -Path $pluginOrgSecc -Directory | ForEach-Object {
        $targetFolder = $_.FullName
        $linkName     = $_.Name
        $linkPath     = Join-Path $rockOrgSeccPath $linkName

        if (Test-Path $linkPath)
        {
            Write-Host "Skipping (exists): $linkPath"
            return
        }

        New-Item -ItemType SymbolicLink -Path $linkPath -Target $targetFolder | Out-Null
        Write-Host "Linked $linkPath -> $targetFolder"
    }
}

Write-Host "Done."