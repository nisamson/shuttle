using Aspire.Hosting.Azure;
using Azure.Provisioning.AppService;
using Azure.Provisioning.Resources;
using Azure.Provisioning.Storage;
using Azure.ResourceManager.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var shuttleRg = builder.AddParameter("shuttleRg")
    .WithDescription("The name of the resource group to deploy to");
var sqlitePath = builder.Configuration.GetValue<string>("SqlitePath") ?? "shuttle.db";
var appInsightsName = builder.AddParameter("appInsightsName")
    .WithDescription("The name of the Application Insights resource to create or use");
var devAppInsightsName = builder.AddParameter("devAppInsightsName", "shlanalyticsdevinsights")
    .WithDescription("The name of the Application Insights resource to create or use for the development environment");
var umiName = builder.AddParameter("umiName")
    .WithDescription("The name of the User Managed Identity to create or use");

var umi = builder.AddAzureUserAssignedIdentity("shuttle-umi")
    .PublishAsExisting(umiName, shuttleRg);

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
    });

#pragma warning disable ASPIREPROBES001
var api = builder.AddProject<Shuttle_Api>("shuttle-api")
    .WithAzureUserAssignedIdentity(umi)
    .WithEnvironment("SHUTTLE_SQLITE_PATH", sqlitePath)
    .WithUrlForEndpoint("https",
        c => {
            c.DisplayText = "OpenAPI Spec";
            c.Url = "/openapi.json";
        })
    .WithExternalHttpEndpoints()
    .WithHttpProbe(ProbeType.Liveness, "/alive", initialDelaySeconds: 5)
    .PublishAsAzureAppServiceWebsite((infra, site) => {
        site.IsHttpsOnly = true;
        site.SiteConfig.IsAlwaysOn = true;
        site.SiteConfig.NumberOfWorkers = 1;
    });

var jobs = builder.AddProject<Shuttle_Api_Jobs>("shuttle-api-jobs")
    .WithAzureUserAssignedIdentity(umi)
    .WithEnvironment("SHUTTLE_SQLITE_PATH", sqlitePath)
    .WithExternalHttpEndpoints()
    .WithHttpProbe(ProbeType.Liveness, "/alive", initialDelaySeconds: 5)
    .WithUrlForEndpoint("https",
        c => {
            c.DisplayText = "Job Dashboard";
            c.Url = "/quartz";
        })
    .PublishAsAzureAppServiceWebsite((infra, site) => {
        site.IsHttpsOnly = true;
        site.SiteConfig.IsAlwaysOn = true;
        site.SiteConfig.NumberOfWorkers = 1;
    });
#pragma warning restore ASPIREPROBES001

builder.Build().Run();
