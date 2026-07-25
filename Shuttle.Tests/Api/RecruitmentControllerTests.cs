using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shuttle.Api.Controllers;
using Shuttle.Api.Services.Recruitment;
using Shuttle.Models.Recruitment;
using EfCore = Shuttle.EFCore.Recruitment;

namespace Shuttle.Tests.Api;

/// <summary>
/// Behavioural tests for <see cref="RecruitmentController"/>, driven by a fake
/// <see cref="IRecruitmentAnalysisCache"/> (no database): filtering/sorting/limit, recruiter
/// addressing + 404s, the lineage tree, and cache/ETag headers with conditional 304 handling.
/// </summary>
public class RecruitmentControllerTests {
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed class FakeCache(RecruitmentAnalysisSnapshot snapshot) : IRecruitmentAnalysisCache {
        public ValueTask<RecruitmentAnalysisSnapshot> GetAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(snapshot);
    }

    private static EfCore.RecruitedPlayer Player(int userId, string username, int playerId, string? recruiter, long tpe) =>
        new(userId, username, playerId, $"Player {playerId}", new DateTime(2020, 1, 1).AddDays(playerId), recruiter, tpe);

    // Gretzky (Player, 2 recruits) > Google (External) / Myself (Self) / blank (None), each 1 recruit.
    private static EfCore.RecruitmentAnalysis SampleAnalysis() => EfCore.RecruitmentAnalyzer.Aggregate(
        [
            Player(1, "Rookie", 10, "Gretzky", 500),
            Player(2, "Sophomore", 20, "Gretzky", 200),
            Player(3, "Third", 30, "Google", 150),
            Player(4, "Fourth", 40, "Myself", 300),
            Player(5, "Fifth", 50, null, 50),
        ],
        ["Gretzky", "Rookie", "Sophomore", "Third", "Fourth", "Fifth"]);

    private static RecruitmentController Controller(
        EfCore.RecruitmentAnalysis? analysis = null,
        DateTimeOffset? lastUpdated = null,
        HttpContext? httpContext = null) {
        var snapshot = new RecruitmentAnalysisSnapshot(analysis ?? SampleAnalysis(), lastUpdated);
        return new RecruitmentController(new FakeCache(snapshot)) {
            ControllerContext = new ControllerContext {
                HttpContext = httpContext ?? new DefaultHttpContext(),
            },
        };
    }

    private static T OkValue<T>(ActionResult<T> action) {
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        return Assert.IsAssignableFrom<T>(ok.Value);
    }

    [Fact]
    public async Task GetSummary_ReturnsPerCategoryTotals_AndSetsCacheHeader() {
        var controller = Controller();

        var result = await controller.GetSummary(Ct);

        var summary = OkValue(result);
        Assert.Equal(4, summary.Count);
        var external = Assert.Single(summary, s => s.Category == RecruiterCategory.External);
        Assert.Equal(1, external.DistinctRecruiters);
        Assert.Equal(150, external.TotalCareerTpe);
        Assert.Contains("max-age", controller.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task GetRecruiters_DefaultSort_RanksByRecruitCountDescending() {
        var controller = Controller();

        var result = await controller.GetRecruiters(
            category: null, sort: null, descending: true, limit: null, cancellationToken: Ct);

        var recruiters = OkValue(result);
        Assert.Equal("Gretzky", recruiters[0].Recruiter);
        Assert.Equal(2, recruiters[0].RecruitedUsers);
    }

    [Fact]
    public async Task GetRecruiters_FiltersByCategory() {
        var controller = Controller();

        var result = await controller.GetRecruiters(
            category: RecruiterCategory.External, sort: null, descending: true, limit: null, cancellationToken: Ct);

        var recruiters = OkValue(result);
        var only = Assert.Single(recruiters);
        Assert.Equal("Google", only.Recruiter);
        Assert.Equal(RecruiterCategory.External, only.Category);
    }

    [Fact]
    public async Task GetRecruiters_LimitReturnsTopN() {
        var controller = Controller();

        var result = await controller.GetRecruiters(
            category: null, sort: null, descending: true, limit: 1, cancellationToken: Ct);

        var recruiters = OkValue(result);
        Assert.Single(recruiters);
        Assert.Equal("Gretzky", recruiters[0].Recruiter);
    }

    [Fact]
    public async Task GetRecruiter_ReturnsDetail_CaseInsensitive() {
        var controller = Controller();

        var result = await controller.GetRecruiter("gRETZky", Ct);

        var detail = OkValue(result);
        Assert.Equal("Gretzky", detail.Tally.Recruiter);
        Assert.Equal(2, detail.RecruitedMembers.Count);
        Assert.Equal("Rookie", detail.RecruitedMembers[0].Username); // highest career TPE first
    }

    [Fact]
    public async Task GetRecruiter_UnknownRecruiter_Returns404() {
        var controller = Controller();

        var result = await controller.GetRecruiter("Nobody", Ct);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetRecruiter_SelfCategory_IsNotAddressable() {
        var controller = Controller();

        var result = await controller.GetRecruiter("Myself", Ct);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetRecruiterLineage_ReturnsNestedTree() {
        var controller = Controller();

        var result = await controller.GetRecruiterLineage("Gretzky", maxDepth: null, cancellationToken: Ct);

        var tree = OkValue(result);
        Assert.Null(tree.UserId);
        Assert.Equal("Gretzky", tree.Name);
        Assert.Equal(RecruiterCategory.Player, tree.Category);
        Assert.Equal(2, tree.SubtreeUsers);
        Assert.Equal(2, tree.Recruited.Count);
    }

    [Fact]
    public async Task GetRecruiterLineage_UnknownRecruiter_Returns404() {
        var controller = Controller();

        var result = await controller.GetRecruiterLineage("Nobody", maxDepth: null, cancellationToken: Ct);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Endpoints_EmitEtag_AndReturn304WhenIfNoneMatchMatches() {
        var lastUpdated = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

        // First request: capture the emitted ETag.
        var first = Controller(lastUpdated: lastUpdated);
        _ = await first.GetSummary(Ct);
        var etag = first.Response.Headers.ETag.ToString();
        Assert.False(string.IsNullOrEmpty(etag));

        // Second request with a matching If-None-Match must short-circuit to 304.
        var conditionalContext = new DefaultHttpContext();
        conditionalContext.Request.Headers.IfNoneMatch = etag;
        var second = Controller(lastUpdated: lastUpdated, httpContext: conditionalContext);

        var result = await second.GetSummary(Ct);

        var status = Assert.IsType<StatusCodeResult>(result.Result);
        Assert.Equal(StatusCodes.Status304NotModified, status.StatusCode);
    }
}
