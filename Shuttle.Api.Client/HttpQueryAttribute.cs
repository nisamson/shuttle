using Refit;

namespace Shuttle.Api.Client;

/// <summary>
/// Refit HTTP method attribute for the <c>QUERY</c> verb (RFC 9110-style safe, idempotent read that
/// carries a request body). Refit has no built-in <c>QUERY</c> attribute, but any custom verb can be
/// added by subclassing <see cref="HttpMethodAttribute"/>; the <c>[Body]</c> parameter is serialized
/// exactly as it would be for a POST.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class HttpQueryAttribute : HttpMethodAttribute {
    private static readonly HttpMethod QueryMethod = new("QUERY");

    public HttpQueryAttribute(string path) : base(path) { }

    public override HttpMethod Method => QueryMethod;
}
