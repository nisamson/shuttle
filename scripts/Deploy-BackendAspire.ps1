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

.PARAMETER WhatIf
    Dry run: generate the deployment artifacts via `aspire publish` (into the AppHost's gitignored
    'aspire-output' folder) instead of deploying. Validates that the AppHost composes and produces a
    deployment manifest without mutating any Azure resources.

.EXAMPLE
    ./Deploy-BackendAspire.ps1

.EXAMPLE
    ./Deploy-BackendAspire.ps1 --debug

.EXAMPLE
    # Validate composition without deploying.
    ./Deploy-BackendAspire.ps1 -DryRun --non-interactive
#>
[CmdletBinding()]
param(
    [switch]$SkipClean,
    [switch]$DryRun,
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

$aspireVerb = if ($DryRun) { "publish" } else { "deploy" }
Write-Host "Running 'aspire $aspireVerb'..." -ForegroundColor Cyan
Push-Location $appHostDir
try {
    aspire $aspireVerb @AspireArgs
    $exit = $LASTEXITCODE
}
finally {
    Pop-Location
}

if ($exit -ne 0) {
    throw "aspire $aspireVerb failed with exit code $exit."
}

Write-Host "$(if ($DryRun) { 'Dry run (aspire publish) complete.' } else { 'Deployment complete.' })" -ForegroundColor Green
