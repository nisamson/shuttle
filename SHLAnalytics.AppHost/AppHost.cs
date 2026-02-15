using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.Primitives;
using Azure.Provisioning.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var resourceGroup = builder.AddParameter("resourceGroup")
    .WithDescription("The name of the resource group to deploy to. The resource group must already exist.");
var appServicePlan = builder.AddParameter("appServicePlan")
    .WithDescription("The name of an existing App Service Plan to use for hosting the web API.");
var umiName = builder.AddParameter("umiName")
    .WithDescription("The name of the User Managed Identity to use for the web API.");
var sqlServerName = builder.AddParameter("sqlServerName")
    .WithDescription("The name of an existing SQL Server to use for the web API's database.");

var umi = builder.AddAzureUserAssignedIdentity("umi")
    .PublishAsExisting(umiName, resourceGroup);

var appServiceEnv = builder.AddAzureAppServiceEnvironment("env")
    .WithAzureApplicationInsights()
    .AsExisting(appServicePlan, resourceGroup);

var databaseName = "SHLAnalyticsTest";
if (builder.Environment.IsDevelopment()) {
    databaseName = "SHLAnalytics";
}

var sqlServerEndpoint = builder.AddAzureSqlServer("shlAnalyticsSqlServer")
    .PublishAsExisting(sqlServerName, resourceGroup)
    .RunAsExisting(sqlServerName, resourceGroup)
    .AddDatabase("analyticsDb", databaseName);

var webApi = builder.AddProject<SHLAnalytics_WebApp>("webapp")
    .WithReference(sqlServerEndpoint)
    .PublishAsAzureAppServiceWebsite()
    .WaitFor(sqlServerEndpoint);

builder.Build().Run();
