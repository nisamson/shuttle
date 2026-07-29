<#
.SYNOPSIS
    Publishes Shuttle.WebClient (Blazor WebAssembly) and deploys it to an Azure Static Web App
    using the Azure Static Web Apps CLI (SWA).

.DESCRIPTION
    Runs `dotnet publish` for the Shuttle.WebClient project in Release configuration, then deploys
    the produced static assets (publish/wwwroot) to Azure Static Web Apps via the SWA CLI.

    Authentication uses a Static Web Apps deployment token. The token is resolved in this order:
        1. -DeploymentToken parameter
        2. $env:SWA_CLI_DEPLOYMENT_TOKEN
        3. Retrieved automatically via the Azure CLI when -AppName is supplied (requires 'az login'):
               az staticwebapp secrets list --name <app-name> [--resource-group <rg>] `
                   --query "properties.apiKey" -o tsv
    You can also get the token from the Azure Portal
    (Static Web App resource -> Overview -> "Manage deployment token").

    The SWA CLI is pinned via the repo-root package.json / package-lock.json. In CI the workflow
    runs `npm ci`, and this script invokes the locally-installed binary (node_modules/.bin/swa).
    When no local install is present (e.g. an ad-hoc local run), it falls back to a version-pinned
    `npx @azure/static-web-apps-cli@<pinned>` (Node.js/npx must be available on PATH).

.PARAMETER DeploymentToken
    The Static Web Apps deployment token. Falls back to $env:SWA_CLI_DEPLOYMENT_TOKEN, then to the
    Azure CLI (when -AppName is provided).

.PARAMETER AppName
    Name of the Azure Static Web App. When provided and no explicit token is available, the script
    fetches the deployment token via the Azure CLI ('az login' required).

.PARAMETER ResourceGroup
    Resource group of the Static Web App. Optional; only needed to disambiguate when the app name is
    not unique in the subscription. Used together with -AppName for the Azure CLI token lookup.

.PARAMETER Environment
    The Static Web Apps environment to deploy to. Use "production" for the production environment,
    or any other name to create/update a named preview environment. Defaults to "production".

.PARAMETER Configuration
    The build configuration passed to `dotnet publish`. Defaults to "Release".

.PARAMETER SkipPublish
    Skip `dotnet publish` and deploy whatever is already in the publish output directory.

.PARAMETER SkipWorkloadRestore
    Skip the `dotnet workload restore` step that ensures the `wasm-tools` workload is installed.
    Without that workload, `dotnet publish` silently falls back to the prebuilt runtime (no native
    relinking), producing a larger WASM payload. Use this only when you know the workload is already
    present (e.g. to save time locally) or are deploying pre-built artifacts with -SkipPublish.

.PARAMETER DryRun
    Validate only: publish the app (unless -SkipPublish) but skip deployment token resolution and
    the SWA deploy call. Touches no Azure resources and needs no token or 'az login'. Useful for a
    build/packaging sanity check in CI.

.EXAMPLE
    ./Deploy-WebClientSwa.ps1 -DeploymentToken $token

.EXAMPLE
    # Fetch the token automatically via the Azure CLI (requires 'az login').
    ./Deploy-WebClientSwa.ps1 -AppName shuttle-webclient -ResourceGroup shuttle-rg

.EXAMPLE
    $env:SWA_CLI_DEPLOYMENT_TOKEN = $token
    ./Deploy-WebClientSwa.ps1 -Environment preview

.EXAMPLE
    # Validate the publish output without deploying (no Azure/token needed).
    ./Deploy-WebClientSwa.ps1 -DryRun
#>
[CmdletBinding()]
param(
    [string]$DeploymentToken = $env:SWA_CLI_DEPLOYMENT_TOKEN,
    [string]$AppName,
    [string]$ResourceGroup,
    [string]$Environment = "production",
    [string]$Configuration = "Release",
    [switch]$SkipPublish,
    [switch]$SkipWorkloadRestore,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "Shuttle.WebClient\Shuttle.WebClient.csproj"
$publishRoot = Join-Path $repoRoot "Shuttle.WebClient\bin\$Configuration\net10.0\publish"
$appArtifacts = Join-Path $publishRoot "wwwroot"

if (-not (Test-Path $project)) {
    throw "Could not find Shuttle.WebClient project at '$project'."
}

if (-not $SkipPublish) {
    if (-not $SkipWorkloadRestore) {
        # Ensure the wasm-tools workload is present so `dotnet publish` performs native runtime
        # relinking (emcc -Oz). Without it, publish falls back to the prebuilt dotnet.native.wasm
        # (~2x larger) and only IL-trims, inflating the download. `workload restore` is idempotent
        # and a no-op once the workload is installed.
        Write-Host "Restoring .NET workloads (wasm-tools) for native relinking..." -ForegroundColor Cyan
        dotnet workload restore $project
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet workload restore failed with exit code $LASTEXITCODE. " +
                "Install manually with 'dotnet workload install wasm-tools' or pass -SkipWorkloadRestore."
        }
    }

    Write-Host "Publishing Shuttle.WebClient ($Configuration)..." -ForegroundColor Cyan
    dotnet publish $project -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path $appArtifacts)) {
    throw "Publish output not found at '$appArtifacts'. Run without -SkipPublish, or publish first."
}

if ($DryRun) {
    Write-Host "Dry run: publish output validated at '$appArtifacts'." -ForegroundColor Green
    Write-Host "Dry run: skipping deployment-token resolution and the Azure Static Web Apps deploy." -ForegroundColor Yellow
    return
}

# Resolve the deployment token via the Azure CLI when none was supplied explicitly or via env var.
if ([string]::IsNullOrWhiteSpace($DeploymentToken) -and -not [string]::IsNullOrWhiteSpace($AppName)) {
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        throw "-AppName was provided but the Azure CLI ('az') was not found on PATH. Install it or pass -DeploymentToken."
    }

    Write-Host "Retrieving deployment token for Static Web App '$AppName' via Azure CLI..." -ForegroundColor Cyan
    $azArgs = @("staticwebapp", "secrets", "list", "--name", $AppName, "--query", "properties.apiKey", "-o", "tsv")
    if (-not [string]::IsNullOrWhiteSpace($ResourceGroup)) {
        $azArgs += @("--resource-group", $ResourceGroup)
    }

    $DeploymentToken = (& az @azArgs 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($DeploymentToken)) {
        throw "Failed to retrieve the deployment token via Azure CLI (exit code $LASTEXITCODE). Ensure you are logged in ('az login') and the app name/resource group are correct.`n$DeploymentToken"
    }
}

if ([string]::IsNullOrWhiteSpace($DeploymentToken)) {
    throw "No deployment token available. Pass -DeploymentToken, set `$env:SWA_CLI_DEPLOYMENT_TOKEN, or provide -AppName (with -ResourceGroup if needed) to fetch it via the Azure CLI."
}

# When running under GitHub Actions, register the token as a masked value so it can never be
# printed in the (public) build log. Guarded so a local run never echoes the token to the console.
if ($env:GITHUB_ACTIONS -eq 'true') {
    Write-Host "::add-mask::$DeploymentToken"
}

# Resolve the SWA CLI: prefer the version pinned in the repo's package-lock.json (installed via
# `npm ci` into node_modules/.bin), and fall back to a version-pinned npx for ad-hoc local runs.
$SwaVersion = "2.0.10"
$onWindows = ($null -eq $IsWindows) -or $IsWindows
$localSwa = Join-Path $repoRoot ("node_modules/.bin/" + ($(if ($onWindows) { "swa.cmd" } else { "swa" })))

if (Test-Path $localSwa) {
    $swaCommand = { param($deployArgs) & $localSwa @deployArgs }
}
else {
    if (-not (Get-Command npx -ErrorAction SilentlyContinue)) {
        throw "The pinned SWA CLI is not installed (run 'npm ci') and npx (Node.js) was not found on PATH."
    }
    $swaCommand = { param($deployArgs) npx --yes "@azure/static-web-apps-cli@$SwaVersion" @deployArgs }
}

Write-Host "Deploying '$appArtifacts' to Azure Static Web Apps (environment: $Environment)..." -ForegroundColor Cyan

# Deploy pre-built static assets. --deployment-token authenticates; --env selects the target environment.
& $swaCommand @("deploy", $appArtifacts, "--deployment-token", $DeploymentToken, "--env", $Environment)

if ($LASTEXITCODE -ne 0) {
    throw "SWA deploy failed with exit code $LASTEXITCODE."
}

Write-Host "Deployment complete." -ForegroundColor Green
