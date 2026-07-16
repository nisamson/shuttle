using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Shuttle.Api.Client;

/// <summary>
/// Registration helpers and shared serialization settings for the Shuttle backend API Refit
/// clients. Usable from the Blazor WebClient and any other consumer of <c>Shuttle.Api</c>.
/// </summary>
public static class ShuttleApiClientExtensions {
    /// <summary>
    /// System.Text.Json options used for all Shuttle API clients.
    /// <para>
    /// Uses <see cref="JsonSerializerDefaults.Web"/> (camelCase, case-insensitive) and relies on the
    /// per-type <c>[JsonConverter]</c> attributes declared on the shared models (e.g.
    /// <c>PlayerPosition</c>, <c>PlayerStatus</c>, <c>PlayerAttributes</c>). We deliberately do NOT
    /// use Refit's default options: those register a global
    /// <see cref="System.Text.Json.Serialization.JsonStringEnumConverter"/> which takes precedence
    /// over type-level <c>[JsonConverter]</c> attributes and hijacks enums whose spaced string values
    /// it cannot parse (e.g. "Right Defense").
    /// </para>
    /// </summary>
    public static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private static RefitSettings CreateRefitSettings() =>
        new() {
            ContentSerializer = new SystemTextJsonContentSerializer(JsonSerializerOptions),
            // Multiselect query collections (e.g. PlayerSearchQuery.Statuses) serialize as repeated
            // keys: ?statuses=Active&statuses=Retired.
            CollectionFormat = CollectionFormat.Multi,
        };

    /// <summary>
    /// Registers the Shuttle backend API Refit clients pointed at <paramref name="baseAddress"/>
    /// (e.g. the value of <c>Api:BaseUrl</c>). Returns the <see cref="IHttpClientBuilder"/> for the
    /// player client so callers can chain additional configuration such as auth message handlers.
    /// </summary>
    public static IHttpClientBuilder AddShuttleApiClient(this IServiceCollection services, Uri baseAddress) {
        return services.AddRefitClient<IShuttlePlayerClient>(CreateRefitSettings())
            .ConfigureHttpClient(c => c.BaseAddress = baseAddress);
    }
}
