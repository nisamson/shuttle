using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Shuttle.Core;

namespace Shuttle.ServiceDefaults;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions {
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";
    
    private static readonly Once HealthChecksDoneOnlyOnce = new();

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http => {
                // Turn on resilience by default
                http.AddStandardResilienceHandler();

                // Turn on service discovery by default
                http.AddServiceDiscovery();
            }
        );

        builder.Services.AddRequestTimeouts();
        builder.Services.AddOutputCache();

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder {
        if (builder.Environment.IsDevelopment()) {
            builder.Logging.SetMinimumLevel(LogLevel.Trace)
                .AddEventSourceLogger();
        }

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => {
                    metrics.AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation();
                }
            )
            .WithTracing(tracing => {
                    tracing.AddSource(builder.Environment.ApplicationName)
                        .AddAspNetCoreInstrumentation(ancTracing =>

                            // Exclude health check requests from tracing
                            ancTracing.Filter = context =>
                                !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                                && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                        )

                        // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                        //.AddGrpcClientInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddConsoleExporter();
                }
            )
            .WithLogging(
                configureBuilder: null,
                configureOptions: logging => {
                    logging.IncludeFormattedMessage = true;
                    logging.IncludeScopes = true;
                }
            );

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter) {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        {
            builder.Services.AddOpenTelemetry()
               .UseAzureMonitor();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder {
        HealthChecksDoneOnlyOnce.Ensure();
        
        builder.Services.AddRequestTimeouts(
            configure: static timeouts =>
                timeouts.AddPolicy("HealthChecks", TimeSpan.FromSeconds(5)));

        builder.Services.AddOutputCache(
            configureOptions: static caching =>
                caching.AddPolicy("HealthChecks",
                    build: static policy => policy.Expire(TimeSpan.FromSeconds(10))));
        
        builder.Services.AddHealthChecks()

            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app) {

        var healthChecks = app.MapGroup("");
        
        healthChecks.MapHealthChecks(HealthEndpointPath)
            .CacheOutput("HealthChecks")
            .WithRequestTimeout("HealthChecks")
            .AllowAnonymous();

        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        healthChecks.MapHealthChecks(
            AlivenessEndpointPath,
            new HealthCheckOptions {
                Predicate = r => r.Tags.Contains("live")
            }
        )
        .CacheOutput("HealthChecks")
        .WithRequestTimeout("HealthChecks")
        .AllowAnonymous();

        return app;
    }
    
    public static IHostApplicationBuilder AddTelemetryService(this IHostApplicationBuilder builder, ActivitySource activitySource) {
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddSource(activitySource.Name));
        return builder;
    }
}
