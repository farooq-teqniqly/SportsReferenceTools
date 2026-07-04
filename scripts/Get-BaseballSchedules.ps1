#Requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [int]$StartYear,

    [Parameter(Mandatory = $true)]
    [int]$EndYear,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    [int]$MinDelaySeconds = 5,

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
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$cliProject = Join-Path $scriptRoot '..\Utilities\CLI\Teqniqly.SportsReferenceClient.Cli\Teqniqly.SportsReferenceClient.Cli.csproj'
$cliProject = (Resolve-Path $cliProject).Path

if (-not (Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}
$OutputDirectory = (Resolve-Path $OutputDirectory).Path

$failedYears = @()

for ($year = $StartYear; $year -le $EndYear; $year++) {
    $fileName = "baseball-schedule-$year.shtml"
    $filePath = Join-Path $OutputDirectory $fileName

    if (Test-Path $filePath) {
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
