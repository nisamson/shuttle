using System.Diagnostics;
using System.Numerics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Shuttle.EFCore.Resilience;

public class ShlDbConnectionResilienceService : IDbConnectionResilienceService<ShlDbContext> {
    private static readonly TimeSpan Delay = TimeSpan.FromSeconds(1);
    private const int MaxRetryAttempts = 5;
    private readonly string connectionString;
    private readonly ILogger<ShlDbConnectionResilienceService> logger;

    private readonly ResiliencePipeline resiliencePipeline;
    
    public ShlDbConnectionResilienceService(IConnectionStringProvider<ShlDbContext> connectionStringProvider, ILogger<ShlDbConnectionResilienceService> logger) {
        connectionString = connectionStringProvider.ConnectionString;
        this.logger = logger;
        resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(
                new() {
                    Name = $"{nameof(ShlDbConnectionResilienceService)}_Retry",
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = Delay,
                    MaxRetryAttempts = MaxRetryAttempts,
                    ShouldHandle = new PredicateBuilder()
                        .Handle<SqlException>(IsRetryableSqlException),
                    OnRetry = args => {
                        logger.LogWarning(
                            args.Outcome.Exception,
                            "Transient SQL exception detected. Retrying... Attempt {RetryAttempt} of {MaxRetryAttempts}",
                            args.AttemptNumber + 1,
                            MaxRetryAttempts
                        );
                        return ValueTask.CompletedTask;
                    }
                }
            )
            .Build();
    }

    private const int TimeoutHResult = unchecked((int)0x80131904);

    private bool IsRetryableSqlException(SqlException ex) {
        if (ex.IsTransient) {
            logger.LogDebug(ex, "Transient SQL exception detected");
            return true;
        }
        
        if (ex.HResult == TimeoutHResult) {
            logger.LogDebug(ex, "SQL timeout exception detected");
            return true;
        }

        return false;
    }

    public async Task EnsureDbConnectivity(CancellationToken cancellationToken = default) {
        using var activity = ActivitySources.ShuttleEfCore.StartActivity();
        using var _ = logger.BeginScope("Ensuring database connectivity for {DbContext}", nameof(ShlDbContext));
        var retryCount = 1;
        try {
            await resiliencePipeline.ExecuteAsync(AttemptWithRetry, cancellationToken);
        } catch (Exception ex) {
            logger.LogError(
                ex,
                "Failed to establish SQL connection after {MaxRetryAttempts} attempts.",
                MaxRetryAttempts
            );
            activity?.SetStatus(ActivityStatusCode.Error, "Failed to establish SQL connection");
            throw;
        }

        logger.LogInformation(
            "Successfully established SQL connection after {RetryCount} attempts.",
            retryCount
        );
        activity?.SetStatus(ActivityStatusCode.Ok);
        return;

        async ValueTask AttemptWithRetry(CancellationToken token = default) {
            using var attemptActivity = ActivitySources.ShuttleEfCore.StartActivity("Attempting SQL Connection");
            attemptActivity?.SetTag("RetryAttempt", retryCount);
            logger.LogInformation("Attempting to open SQL connection... Attempt {RetryAttempt}", ++retryCount);
            await AttemptDbConnection(token);
        }
    }

    private async ValueTask AttemptDbConnection(CancellationToken token = default) {
        await using var connection = new SqlConnection(connectionString);;
        await connection.OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        await command.ExecuteScalarAsync(token);
        
    }
}
