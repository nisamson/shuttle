using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shuttle.Api.Services;

namespace Shuttle.Tests.Api;

/// <summary>
/// Tests for <see cref="DatabaseVersionedResults.DbVersionedOk{T}"/>: the shared helper that stamps
/// DB-freshness-derived cache headers on read responses and short-circuits to <c>304 Not Modified</c>
/// when the caller's <c>If-None-Match</c> already matches.
/// </summary>
public class DatabaseVersionedResultsTests {
    private static readonly DateTimeOffset LastUpdated =
        new(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private sealed class TestController : ControllerBase;

    private static TestController Controller(HttpContext? httpContext = null) =>
        new() {
            ControllerContext = new ControllerContext {
                HttpContext = httpContext ?? new DefaultHttpContext(),
            },
        };

    private static HttpContext RequestFor(string path, string queryString = "", string? ifNoneMatch = null) {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(queryString);
        if (ifNoneMatch is not null) {
            context.Request.Headers.IfNoneMatch = ifNoneMatch;
        }

        return context;
    }

    [Fact]
    public void SetsPublicCacheEtagAndLastModified_WhenLastUpdatedKnown() {
        var controller = Controller(RequestFor("/players"));

        var result = controller.DbVersionedOk("body", LastUpdated, TimeSpan.FromMinutes(5));

        Assert.IsType<OkObjectResult>(result);
        var headers = controller.Response.GetTypedHeaders();
        Assert.Contains("public", controller.Response.Headers.CacheControl.ToString());
        Assert.Contains("max-age=300", controller.Response.Headers.CacheControl.ToString());
        Assert.Equal(LastUpdated, headers.LastModified);
        Assert.False(string.IsNullOrEmpty(controller.Response.Headers.ETag.ToString()));
    }

    [Fact]
    public void OmitsValidator_WhenLastUpdatedUnknown() {
        var controller = Controller(RequestFor("/players"));

        var result = controller.DbVersionedOk("body", lastUpdated: null, TimeSpan.FromMinutes(5));

        Assert.IsType<OkObjectResult>(result);
        Assert.True(string.IsNullOrEmpty(controller.Response.Headers.ETag.ToString()));
        Assert.Contains("max-age", controller.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public void Returns304_WhenIfNoneMatchMatches() {
        // First request captures the emitted ETag.
        var first = Controller(RequestFor("/players"));
        _ = first.DbVersionedOk("body", LastUpdated, TimeSpan.FromMinutes(5));
        var etag = first.Response.Headers.ETag.ToString();

        // Second, conditional request with the matching validator short-circuits.
        var second = Controller(RequestFor("/players", ifNoneMatch: etag));

        var result = second.DbVersionedOk("body", LastUpdated, TimeSpan.FromMinutes(5));

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status304NotModified, status.StatusCode);
    }

    [Fact]
    public void Returns200_WhenIfNoneMatchDoesNotMatch() {
        var controller = Controller(RequestFor("/players", ifNoneMatch: "\"deadbeef\""));

        var result = controller.DbVersionedOk("body", LastUpdated, TimeSpan.FromMinutes(5));

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void DistinctResources_ProduceDistinctEtags() {
        var players = Controller(RequestFor("/players"));
        _ = players.DbVersionedOk("body", LastUpdated, TimeSpan.FromMinutes(5));

        var search = Controller(RequestFor("/players/search", "?text=abc"));
        _ = search.DbVersionedOk("body", LastUpdated, TimeSpan.FromMinutes(5));

        Assert.NotEqual(
            players.Response.Headers.ETag.ToString(),
            search.Response.Headers.ETag.ToString());
    }

    [Fact]
    public void SameResourceEarlierFreshness_ProducesDifferentEtag() {
        var older = Controller(RequestFor("/players"));
        _ = older.DbVersionedOk("body", LastUpdated, TimeSpan.FromMinutes(5));

        var newer = Controller(RequestFor("/players"));
        _ = newer.DbVersionedOk("body", LastUpdated.AddHours(6), TimeSpan.FromMinutes(5));

        Assert.NotEqual(
            older.Response.Headers.ETag.ToString(),
            newer.Response.Headers.ETag.ToString());
    }
}
