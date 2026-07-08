using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;
using Refit;

namespace Shuttle.Shl.Api.Client;

public static class ShlConstants {
    public const string UserAgent = "Shuttle.Shl.Api.Client/1.0";
    
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
        services.AddRefitClient<IShlIndexV1Client>()
            .AddPolicyHandler(policy)
            .ConfigureHttpClient(c => {
                    c.BaseAddress = new Uri(IShlIndexV1Client.BaseUrl);
                    c.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                }
            );
        
        var portalPolicy = CreatePolicy();
        services.AddRefitClient<IShlPortalV1Client>()
            .AddPolicyHandler(portalPolicy)
            .ConfigureHttpClient(c => {
                    c.BaseAddress = new Uri(IShlPortalV1Client.BaseUrl);
                    c.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                }
            );

        return services;
    }
}
