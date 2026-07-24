using Shuttle.Analysis.Flows.Recruitment;
using Shuttle.EFCore.Recruitment;

namespace Shuttle.Tests.Analysis;

public class RecruitmentDotWriterTests {

    private static RecruitedPlayer Player(int userId, string username, int playerId, string? recruiter, long tpe) =>
        new(userId, username, playerId, $"Player {playerId}", new DateTime(2020, 1, 1).AddDays(playerId), recruiter, tpe);

    [Fact]
    public void BuildFullGraph_EmitsDigraphWithRecruiterAndMemberNodes() {
        var analysis = RecruitmentAnalyzer.Aggregate(
            [Player(1, "Rookie", 10, "Gretzky", 500)],
            ["Gretzky"]);

        var dot = RecruitmentDotWriter.BuildFullGraph(analysis);

        Assert.StartsWith("digraph recruitment {", dot);
        Assert.Contains("\"Gretzky\"", dot);
        Assert.Contains("\"Rookie\"", dot);
        Assert.Contains("-> u1", dot);
        Assert.Contains("}", dot);
    }

    [Fact]
    public void BuildFullGraph_EscapesQuotesInNames() {
        var analysis = RecruitmentAnalyzer.Aggregate(
            [Player(1, "Wa\"yne", 10, "Go\"ogle", 100)],
            []);

        var dot = RecruitmentDotWriter.BuildFullGraph(analysis);

        Assert.Contains("Go\\\"ogle", dot);
        Assert.Contains("Wa\\\"yne", dot);
    }

    [Fact]
    public void BuildPlayerRecruiterNetwork_OnlyIncludesMemberRecruiters() {
        var analysis = RecruitmentAnalyzer.Aggregate(
            [
                Player(1, "Rookie", 10, "Gretzky", 500),   // member recruiter
                Player(2, "Other", 20, "Google", 200),      // external recruiter
            ],
            ["Gretzky"]);

        var dot = RecruitmentDotWriter.BuildPlayerRecruiterNetwork(analysis);

        Assert.StartsWith("digraph player_recruiter_network {", dot);
        Assert.Contains("\"Gretzky\" -> \"Rookie\"", dot);
        Assert.DoesNotContain("Google", dot);   // external recruiters are excluded
        Assert.DoesNotContain("Other", dot);
    }

    [Fact]
    public void BuildPlayerRecruiterNetwork_LabelsEdgesWithCareerTpe() {
        var analysis = RecruitmentAnalyzer.Aggregate(
            [Player(1, "Rookie", 10, "Gretzky", 750)],
            ["Gretzky"]);

        var dot = RecruitmentDotWriter.BuildPlayerRecruiterNetwork(analysis);

        Assert.Contains("label=\"750\"", dot);
    }
}
