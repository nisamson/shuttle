using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.Primitives;
using Azure.Provisioning.Storage;
using Microsoft.Extensions.DependencyInjection;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var resourceGroup = builder.AddParameter("resourceGroup")
    .WithDescription("The name of the resource group to deploy to. The resource group must already exist.");
var appServicePlan = builder.AddParameter("appServicePlan")
    .WithDescription("The name of an existing App Service Plan to use for hosting the web API.");
var blobStorageAccount = builder.AddParameter("blobStorageAccount")
    .WithDescription("The name of an existing Azure Storage Account to use for blob storage.");
var umiName = builder.AddParameter("umiName")
    .WithDescription("The name of the User Managed Identity to use for the web API.");

var umi = builder.AddAzureUserAssignedIdentity("umi")
    .PublishAsExisting(umiName, resourceGroup);

var appServiceEnv = builder.AddAzureAppServiceEnvironment("env")
    .WithAzureApplicationInsights()
    .AsExisting(appServicePlan, resourceGroup);

var blobs = builder.AddAzureStorage("storage")
    .RunAsEmulator(c => c.WithDataVolume())
    .PublishAsExisting(blobStorageAccount, resourceGroup)
    .AddBlobs("blobs");

var webApi = builder.AddProject<SHLAnalytics_WebApi>("webapi")
    .WithHttpHealthCheck("/health")
    .WithReferenceRelationship(appServicePlan)
    .WithReferenceRelationship(umi)
    .WithReference(blobs)
    .WithExternalHttpEndpoints()
    .WithAzureUserAssignedIdentity(umi)
    .PublishAsAzureAppServiceWebsite((infra, website) => { })
    .WaitFor(blobs);

builder.Build().Run();
