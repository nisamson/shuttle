<#
.SYNOPSIS
    Clean-builds and deploys the Shuttle backend (Shuttle.Api) via the Aspire AppHost.

.DESCRIPTION
    Runs a mandatory clean step before `aspire deploy`. The Aspire App Service deployment builds a
    container image from each project's publish output; if bin/obj/publish directories are left dirty,
    stale or renamed assemblies (e.g. an older Azure.Identity that no longer satisfies a bumped minimum
    version, or leftover *.dll from a previous assembly name) get baked into the image and break
    assembly resolution at runtime. Deleting bin/obj first guarantees every deploy ships only the
    current, consistent set of assemblies.

    Any extra arguments are forwarded to `aspire deploy` (e.g. --debug).

.PARAMETER SkipClean
    Skip deleting bin/obj (not recommended; defeats the purpose of this script).

.EXAMPLE
    ./Deploy-BackendAspire.ps1

.EXAMPLE
    ./Deploy-BackendAspire.ps1 --debug
#>
[CmdletBinding()]
param(
    [switch]$SkipClean,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$AspireArgs
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$appHostDir = Join-Path $repoRoot "Shuttle.Backend.Aspire"

if (-not (Test-Path (Join-Path $appHostDir "Shuttle.Backend.Aspire.csproj"))) {
    throw "Could not find the Aspire AppHost at '$appHostDir'."
}

if (-not (Get-Command aspire -ErrorAction SilentlyContinue)) {
    throw "The 'aspire' CLI was not found on PATH. Install it with: dotnet tool install -g aspire.cli (or see https://aka.ms/aspire/cli)."
}

if (-not $SkipClean) {
    Write-Host "Cleaning bin/obj across the solution..." -ForegroundColor Cyan
    Get-ChildItem -Path $repoRoot -Recurse -Directory -Include bin, obj -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '\\node_modules\\' } |
        ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue }
    Write-Host "Clean complete." -ForegroundColor Green
}

Write-Host "Running 'aspire deploy'..." -ForegroundColor Cyan
Push-Location $appHostDir
try {
    aspire deploy @AspireArgs
    $exit = $LASTEXITCODE
}
finally {
    Pop-Location
}

if ($exit -ne 0) {
    throw "aspire deploy failed with exit code $exit."
}

Write-Host "Deployment complete." -ForegroundColor Green
