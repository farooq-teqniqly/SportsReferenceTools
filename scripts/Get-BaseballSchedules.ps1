#Requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [int]$StartYear,

    [Parameter(Mandatory = $true)]
    [int]$EndYear,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    [ValidateRange(0, 86400)]
    [int]$MinDelaySeconds = 5,

    [ValidateRange(0, 86400)]
    [int]$MaxDelaySeconds = 10
)

$ErrorActionPreference = 'Stop'

if ($EndYear -lt $StartYear) {
    throw "EndYear ($EndYear) must be greater than or equal to StartYear ($StartYear)."
}

if ($MaxDelaySeconds -lt $MinDelaySeconds) {
    throw "MaxDelaySeconds ($MaxDelaySeconds) must be greater than or equal to MinDelaySeconds ($MinDelaySeconds)."
}

# Resolve the CLI project relative to this script so the script works from any CWD.
$cliProject = Join-Path $PSScriptRoot '..\Utilities\CLI\Teqniqly.SportsReferenceClient.Cli\Teqniqly.SportsReferenceClient.Cli.csproj'
$cliProject = (Resolve-Path -LiteralPath $cliProject).Path

# Accept an existing directory or create one. Reject an existing non-directory path so we never
# build child file paths under a file. -LiteralPath keeps wildcard metacharacters (e.g. '[', ']')
# in the supplied path from being treated as patterns.
if (Test-Path -LiteralPath $OutputDirectory) {
    if (-not (Test-Path -LiteralPath $OutputDirectory -PathType Container)) {
        throw "OutputDirectory ($OutputDirectory) exists but is not a directory."
    }
}
else {
    New-Item -ItemType Directory -LiteralPath $OutputDirectory -Force | Out-Null
}
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path

$failedYears = @()

for ($year = $StartYear; $year -le $EndYear; $year++) {
    $fileName = "baseball-schedule-$year.shtml"
    $filePath = Join-Path $OutputDirectory $fileName

    if (Test-Path -LiteralPath $filePath -PathType Leaf) {
        Write-Host "Skipping $year, file already exists: $filePath"
        continue
    }

    Write-Host "Downloading $year schedule -> $filePath"

    & dotnet run --project $cliProject -- baseball schedule get --year $year --file $filePath
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "CLI exited with code $LASTEXITCODE for year $year."
        $failedYears += $year
    }

    # Delay only between invocations, not after the last one.
    if ($year -lt $EndYear) {
        $delay = Get-Random -Minimum $MinDelaySeconds -Maximum ($MaxDelaySeconds + 1)
        Write-Host "Waiting $delay second(s) before next request..."
        Start-Sleep -Seconds $delay
    }
}

if ($failedYears.Count -gt 0) {
    Write-Host ""
    Write-Warning "Failed years: $($failedYears -join ', ')"
    exit 1
}

Write-Host "Done."
