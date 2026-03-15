using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shuttle.ServiceDefaults;

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.ConfigureFunctionsWebApplication();

var app = builder.Build();
app.Run();
