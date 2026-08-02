using Aspire.Hosting.Azure;
using Azure.Provisioning.AppService;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.Resources;
using Azure.Provisioning.Storage;
using Azure.ResourceManager.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// The generated Azure infrastructure includes RBAC role assignments (ACR AcrPull, the Aspire
// dashboard identity's Contributor, the web app's Website Contributor). Creating those needs
// Microsoft.Authorization/roleAssignments/write, which the CI deploy principal intentionally does
// NOT have. Set FIRST_RUN=true for a one-time interactive bootstrap deploy (from an identity that
// CAN assign roles) to create them; every later deploy — including CI — leaves FIRST_RUN unset, so
// the role assignments are stripped from the template. Incremental deployments never delete the
// already-existing assignments, so the app keeps working. See docs/deployment-ci.md.
var isFirstRun = builder.Configuration.GetValue("FIRST_RUN", false);

var shuttleRg = builder.AddParameter("shuttleRg")
    .WithDescription("The name of the resource group to deploy to");
var databaseServerName = builder.Configuration.GetValue<string>("DatabaseServerName") ?? throw new InvalidOperationException("DatabaseServerName must be configured.");
var dbServerNameParam = builder.AddParameter("dbServerName", databaseServerName);
var databaseName = builder.Configuration.GetValue<string>("DatabaseName") ?? throw new InvalidOperationException("DatabaseName must be configured.");
var appInsightsName = builder.AddParameter("appInsightsName")
    .WithDescription("The name of the Application Insights resource to create or use");
var devAppInsightsName = builder.AddParameter("devAppInsightsName", "shlanalyticsdevinsights")
    .WithDescription("The name of the Application Insights resource to create or use for the development environment");
var umiName = builder.AddParameter("umiName", "shl-app-umi")
    .WithDescription("The name of the User Managed Identity to create or use");

var umi = builder.AddAzureUserAssignedIdentity("shuttle-umi")
    .PublishAsExisting(umiName, shuttleRg);

var sqlServer = builder.AddAzureSqlServer("shuttleSqlServer")
    .WithRelationship(umi.Resource, "DbAccess")
    .AsExisting(dbServerNameParam, shuttleRg);

var insights = builder.AddAzureApplicationInsights("shuttle-app-insights")
    .PublishAsExisting(appInsightsName, shuttleRg)
    .RunAsExisting(devAppInsightsName, shuttleRg);

var appServicePlan = builder.AddAzureAppServiceEnvironment("shuttle-app-service-plan")
    .WithAzureApplicationInsights(insights)
    .ConfigureInfrastructure(infra => {
        var appServicePlan = infra.GetProvisionableResources()
            .OfType<AppServicePlan>()
            .Single();
        appServicePlan.Sku = new() {
            Name = "B3",
            Tier = "Basic"
        };
        appServicePlan.IsElasticScaleEnabled = false;
        // Role assignments are created only on the FIRST_RUN bootstrap deploy (see note above);
        // otherwise they are stripped so the CI principal needs no roleAssignments/write.
        if (!isFirstRun) {
            RemoveRoleAssignments(infra);
        }
    });

#pragma warning disable ASPIREPROBES001
var api = builder.AddProject<Shuttle_Api>("shuttle-api")
    .WithReference(sqlServer)
    .WaitFor(sqlServer)
    .WithAzureUserAssignedIdentity(umi)
    .WithEnvironment("SHUTTLESQLSERVER_DATABASE", databaseName)
    .WithUrl("/quartz", "Job Dashboard")
    .WithExternalHttpEndpoints()
    .WithHttpProbe(ProbeType.Liveness, "/alive", initialDelaySeconds: 5)
    .PublishAsAzureAppServiceWebsite((infra, site) => {
        site.IsHttpsOnly = true;
        site.SiteConfig.IsAlwaysOn = true;
        site.SiteConfig.NumberOfWorkers = 1;
        // See note above: the web app's Website Contributor role assignment is created only on
        // the FIRST_RUN bootstrap deploy; otherwise it is stripped from the generated template.
        if (!isFirstRun) {
            RemoveRoleAssignments(infra);
        }
    });
#pragma warning restore ASPIREPROBES001

// The Blazor WebAssembly front end is only orchestrated for local development. It runs via
// the Blazor dev server and is excluded from publish so it does not affect the Azure App
// Service deployment of the API.
//
// Set LaunchWebClient=false (see the "https (debug frontend in Rider)" launch profile) to
// omit this tile so the WebClient can instead be run/debugged directly in Rider with full
// WASM debugging while this AppHost provides the backend. The WebClient reads its API URL
// from wwwroot/appsettings.Development.json, so it targets the same local API either way.
if (builder.ExecutionContext.IsRunMode
    && builder.Configuration.GetValue("LaunchWebClient", true)) {
    builder.AddProject<Shuttle_WebClient>("webclient")
        .WithReference(api)
        .WaitFor(api)
        .WithExternalHttpEndpoints();
}

builder.Build().Run();

// Removes every auto-generated Azure RBAC role assignment from a provisioned resource's
// infrastructure. Role assignments require Microsoft.Authorization/roleAssignments/write, which
// the CI deploy principal intentionally lacks; they are created once by a FIRST_RUN bootstrap
// deploy (see docs/deployment-ci.md) and left untouched by later incremental deployments.
static void RemoveRoleAssignments(AzureResourceInfrastructure infra) {
    foreach (var roleAssignment in infra.GetProvisionableResources().OfType<RoleAssignment>().ToList()) {
        infra.Remove(roleAssignment);
    }
}
