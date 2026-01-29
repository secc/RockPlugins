param(
    [string]$RockSolutionPath,
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

if (-not $RockSolutionPath)
{
    $RockSolutionPath = Prompt-ForPath "Enter path to Rock solution file (e.g. C:\Rock\Rock.sln)"
}
if (-not $PluginsRepoPath)
{
    $PluginsRepoPath = Prompt-ForPath "Enter path to Plugins repo root (e.g. C:\Users\you\source\repos\RockPlugins-16)"
}

# Validate solution file exists
if (-not (Test-Path $RockSolutionPath))
{
    Write-Error "Solution file not found at $RockSolutionPath"
    exit 1
}

$pluginsRoot = Join-Path $PluginsRepoPath "Plugins"

# Validate plugins root exists
if (-not (Test-Path $pluginsRoot))
{
    Write-Error "Plugins folder not found at $pluginsRoot"
    exit 1
}

# Find all .csproj files in plugin directories
$csprojFiles = Get-ChildItem -Path $pluginsRoot -Recurse -Filter "*.csproj"

if ($csprojFiles.Count -eq 0)
{
    Write-Host "No .csproj files found in $pluginsRoot"
    exit 0
}

Write-Host "Found $($csprojFiles.Count) project(s) to add."

# Add each project to the solution using dotnet sln command
foreach ($csproj in $csprojFiles)
{
    $csprojPath = $csproj.FullName
    Write-Host "Adding: $csprojPath"
    
    & dotnet sln $RockSolutionPath add $csprojPath
    
    if ($LASTEXITCODE -ne 0)
    {
        Write-Warning "Failed to add project: $csprojPath"
    }
    else
    {
        Write-Host "Successfully added: $($csproj.Name)"
    }
}

Write-Host "Done."