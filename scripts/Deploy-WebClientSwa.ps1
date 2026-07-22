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

    The SWA CLI is invoked through `npx @azure/static-web-apps-cli`, so no global install is required
    (Node.js/npx must be available on PATH).

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

.EXAMPLE
    ./Deploy-WebClientSwa.ps1 -DeploymentToken $token

.EXAMPLE
    # Fetch the token automatically via the Azure CLI (requires 'az login').
    ./Deploy-WebClientSwa.ps1 -AppName shuttle-webclient -ResourceGroup shuttle-rg

.EXAMPLE
    $env:SWA_CLI_DEPLOYMENT_TOKEN = $token
    ./Deploy-WebClientSwa.ps1 -Environment preview
#>
[CmdletBinding()]
param(
    [string]$DeploymentToken = $env:SWA_CLI_DEPLOYMENT_TOKEN,
    [string]$AppName,
    [string]$ResourceGroup,
    [string]$Environment = "production",
    [string]$Configuration = "Release",
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "Shuttle.WebClient\Shuttle.WebClient.csproj"
$publishRoot = Join-Path $repoRoot "Shuttle.WebClient\bin\$Configuration\net10.0\publish"
$appArtifacts = Join-Path $publishRoot "wwwroot"

if (-not (Test-Path $project)) {
    throw "Could not find Shuttle.WebClient project at '$project'."
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

if (-not (Get-Command npx -ErrorAction SilentlyContinue)) {
    throw "npx (Node.js) was not found on PATH. Install Node.js so the SWA CLI can run via 'npx @azure/static-web-apps-cli'."
}

if (-not $SkipPublish) {
    Write-Host "Publishing Shuttle.WebClient ($Configuration)..." -ForegroundColor Cyan
    dotnet publish $project -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path $appArtifacts)) {
    throw "Publish output not found at '$appArtifacts'. Run without -SkipPublish, or publish first."
}

Write-Host "Deploying '$appArtifacts' to Azure Static Web Apps (environment: $Environment)..." -ForegroundColor Cyan

# Deploy pre-built static assets. --deployment-token authenticates; --env selects the target environment.
npx --yes @azure/static-web-apps-cli deploy $appArtifacts `
    --deployment-token $DeploymentToken `
    --env $Environment

if ($LASTEXITCODE -ne 0) {
    throw "SWA deploy failed with exit code $LASTEXITCODE."
}

Write-Host "Deployment complete." -ForegroundColor Green
