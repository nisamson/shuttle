using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;
using Refit;

namespace Shuttle.Shl.Api.Client;

public static class ShlConstants {
    public const string UserAgent = "Shuttle.Shl.Api.Client/1.0";

    /// <summary>
    /// System.Text.Json options used for all SHL API clients.
    /// <para>
    /// Uses <see cref="JsonSerializerDefaults.Web"/> (camelCase, case-insensitive) and relies on the
    /// per-type <c>[JsonConverter]</c> attributes declared on the model enums. We deliberately do NOT
    /// use Refit's default options: those register a global <see cref="System.Text.Json.Serialization.JsonStringEnumConverter"/>
    /// in the converters collection, which takes precedence over type-level <c>[JsonConverter]</c> attributes
    /// and therefore hijacks enums such as <c>PlayerPosition</c> ("Right Defense") and <c>TaskStatus</c>
    /// ("SMJHL Rookie"), whose string values it cannot parse.
    /// </para>
    /// </summary>
    public static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private static RefitSettings CreateRefitSettings() =>
        new() {
            ContentSerializer = new SystemTextJsonContentSerializer(JsonSerializerOptions)
        };

    private static ResiliencePipeline<HttpResponseMessage> CreateResiliencePipeline() {
        var retryStrategy = new RetryStrategyOptions<HttpResponseMessage>() {
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromSeconds(1),
        };
        var rateLimiterOptions = new TokenBucketRateLimiterOptions() {
            AutoReplenishment = true,
            TokenLimit = 100,
            TokensPerPeriod = 100,
            ReplenishmentPeriod = TimeSpan.FromSeconds(10)
        };

        var rateLimiter = new TokenBucketRateLimiter(rateLimiterOptions);

        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(retryStrategy)
            .AddRateLimiter(rateLimiter)
            .Build();
    }

    private static IAsyncPolicy<HttpResponseMessage> CreatePolicy() {
        var pipeline = CreateResiliencePipeline();
        return pipeline.AsAsyncPolicy();
    }

    public static IServiceCollection AddShlApiClients(this IServiceCollection services) {
        var policy = CreatePolicy();
        services.AddRefitClient<IShlIndexV1Client>(CreateRefitSettings())
            .AddPolicyHandler(policy)
            .ConfigureHttpClient(c => {
                    c.BaseAddress = new Uri(IShlIndexV1Client.BaseUrl);
                    c.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                }
            );
        
        var portalPolicy = CreatePolicy();
        services.AddRefitClient<IShlPortalV1Client>(CreateRefitSettings())
            .AddPolicyHandler(portalPolicy)
            .ConfigureHttpClient(c => {
                    c.BaseAddress = new Uri(IShlPortalV1Client.BaseUrl);
                    c.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                }
            );

        return services;
    }
}
