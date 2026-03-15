using Aspire.Hosting.Azure;
using Azure.Provisioning.AppService;
using Azure.Provisioning.Resources;
using Azure.Provisioning.Storage;
using Azure.ResourceManager.Models;
using Microsoft.Extensions.Configuration;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var shuttleRg = builder.AddParameter("shuttleRg")
    .WithDescription("The name of the resource group to deploy to");
var databaseServerName = builder.AddParameter("databaseServerName", secret: true)
    .WithDescription("The server name of the Azure SQL Database to use");
var databaseName = builder.Configuration.GetValue<string>("DatabaseName") ?? throw new InvalidOperationException("DatabaseName must be configured.");
var appInsightsName = builder.AddParameter("appInsightsName")
    .WithDescription("The name of the Application Insights resource to create or use");
var devAppInsightsName = builder.AddParameter("devAppInsightsName", "shlanalyticsdevinsights")
    .WithDescription("The name of the Application Insights resource to create or use for the development environment");
var storageAccountName = builder.AddParameter("storageAccountName", secret: true)
    .WithDescription("The name of the Storage Account to create or use");
var umiName = builder.AddParameter("umiName")
    .WithDescription("The name of the User Managed Identity to create or use");

var umi = builder.AddAzureUserAssignedIdentity("shuttle-umi")
    .PublishAsExisting(umiName, shuttleRg);

var sqlServer = builder.AddAzureSqlServer("shuttleSqlServer")
    .WithRelationship(umi.Resource, "DbAccess")
    .AsExisting(databaseServerName, shuttleRg);

var insights = builder.AddAzureApplicationInsights("shuttle-app-insights")
    .PublishAsExisting(appInsightsName, shuttleRg)
    .RunAsExisting(devAppInsightsName, shuttleRg);

var appServicePlan = builder.AddAzureAppServiceEnvironment("shuttle-app-service-plan")
    .WithAzureApplicationInsights(insights);

var storage = builder.AddAzureStorage("shuttle-storage-account")
    .PublishAsExisting(storageAccountName, shuttleRg)
    .RunAsEmulator();

#pragma warning disable ASPIREPROBES001
var api = builder.AddProject<Shuttle_Api>("shuttle-api")
    .WithReference(sqlServer)
    .WaitFor(sqlServer)
    .WithAzureUserAssignedIdentity(umi)
    .WithEnvironment("DATABASE_NAME", databaseName)
    .WithUrlForEndpoint("openapi",
        c => {
            c.DisplayText = "OpenAPI Spec";
            c.Url = "/openapi.json";
        })
    .WithExternalHttpEndpoints()
    .WithHttpProbe(ProbeType.Liveness, "/health", initialDelaySeconds: 5)
    .PublishAsAzureAppServiceWebsite((infra, site) => {
        site.Kind = "app,linux";
        site.IsHttpsOnly = true;
        site.SiteConfig.IsAlwaysOn = true;
    });
#pragma warning restore ASPIREPROBES001

var jobs = builder.AddAzureFunctionsProject<Shuttle_Jobs>("shuttle-jobs")
    .WithReference(sqlServer)
    .WaitFor(sqlServer)
    .WithEnvironment("DATABASE_NAME", databaseName)
    .WithAzureUserAssignedIdentity(umi)
    .WithHostStorage(storage)
    .PublishAsAzureAppServiceWebsite((infra, site) => {
        site.Kind = "functionapp,linux";
        site.SiteConfig.IsAlwaysOn = true;
    })
    .WithExternalHttpEndpoints();

builder.Build().Run();
