using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Shuttle.Api.Services;

/// <summary>
/// Helpers for returning read responses whose body only changes when the backing database is
/// refreshed by the <see cref="Jobs.DbUpdateJob"/>. Each response is stamped with a
/// <c>Cache-Control</c>, a <c>Last-Modified</c>, and a strong <c>ETag</c> derived from the database
/// freshness signal (see <see cref="IDatabaseFreshnessProvider"/>), and requests carrying a matching
/// <c>If-None-Match</c> short-circuit to <c>304 Not Modified</c> so unchanged data is not re-sent.
/// </summary>
public static class DatabaseVersionedResults {
    /// <summary>
    /// Sets the freshness-derived cache headers on the response and returns <c>200</c> with
    /// <paramref name="body"/>, or <c>304 Not Modified</c> when the caller's <c>If-None-Match</c>
    /// already matches the computed validator. When <paramref name="lastUpdated"/> is <c>null</c>
    /// (no completed update recorded yet) no validator is emitted and a plain <c>200</c> is returned.
    /// </summary>
    /// <param name="controller">The controller producing the response.</param>
    /// <param name="body">The response body to return on a <c>200</c>.</param>
    /// <param name="lastUpdated">The database freshness signal used as the ETag/Last-Modified version.</param>
    /// <param name="maxAge">How long clients/shared caches may reuse the response before revalidating.</param>
    public static ActionResult DbVersionedOk<T>(
        this ControllerBase controller,
        T body,
        DateTimeOffset? lastUpdated,
        TimeSpan maxAge) {
        var headers = controller.Response.GetTypedHeaders();
        headers.CacheControl = new CacheControlHeaderValue {
            Public = true,
            MaxAge = maxAge,
        };

        if (lastUpdated is { } lu) {
            headers.LastModified = lu;
            var etag = ComputeETag(controller.Request, lu);
            headers.ETag = etag;

            var ifNoneMatch = controller.Request.GetTypedHeaders().IfNoneMatch;
            if (ifNoneMatch is { Count: > 0 }
                && ifNoneMatch.Any(t => t.Compare(etag, useStrongComparison: false))) {
                return controller.StatusCode(StatusCodes.Status304NotModified);
            }
        }

        return controller.Ok(body);
    }

    /// <summary>
    /// Builds a strong ETag from the freshness signal and the exact resource (path + query), so
    /// distinct endpoints/queries never share a validator.
    /// </summary>
    private static EntityTagHeaderValue ComputeETag(HttpRequest request, DateTimeOffset lastUpdated) {
        var raw = $"{lastUpdated:o}|{request.Path}{request.QueryString}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return new EntityTagHeaderValue($"\"{Convert.ToHexString(hash, 0, 8).ToLowerInvariant()}\"");
    }
}
