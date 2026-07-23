using Shuttle.WebClient.Shared.Blogs;

namespace Shuttle.Api.Meta;

/// <summary>DI wiring for the SSR <c>/meta</c> endpoint.</summary>
public static class MetaServiceCollectionExtensions {
    public static IServiceCollection AddMetaEndpoint(
        this IServiceCollection services,
        IConfiguration configuration) {
        services.Configure<MetaOptions>(configuration.GetSection(MetaOptions.SectionName));
        services.AddSingleton<IBlogService, BlogService>();
        services.AddScoped<IMetaResolver, MetaResolver>();
        services.AddScoped<MetaHtmlRenderer>();
        return services;
    }
}
