using Shuttle.EFCore.Recruitment;

namespace Shuttle.Tests.Analysis;

/// <summary>
/// Unit tests for <see cref="RecruitmentAnalyzer.BuildLineageTree"/> and
/// <see cref="RecruitmentAnalyzer.BuildChildLookup"/> — the nested downstream lineage tree that backs
/// the recruitment lineage API endpoint.
/// </summary>
public class RecruitmentLineageTreeTests {

    private static RecruitedPlayer Player(int userId, string username, int playerId, string? recruiter, long tpe) =>
        new(userId, username, playerId, $"Player {playerId}", new DateTime(2020, 1, 1).AddDays(playerId), recruiter, tpe);

    // Chain: Google -> Root; Root -> Mid; Mid -> Leaf. Members: Root, Mid, Leaf.
    private static RecruitmentAnalysis ChainAnalysis() => RecruitmentAnalyzer.Aggregate(
        [
            Player(1, "Root", 10, "Google", 100),
            Player(2, "Mid", 20, "Root", 200),
            Player(3, "Leaf", 30, "Mid", 400),
        ],
        ["Root", "Mid", "Leaf"]);

    private static RecruiterTally Tally(RecruitmentAnalysis analysis, string recruiter) =>
        Assert.Single(analysis.Tallies, t => t.Recruiter == recruiter);

    [Fact]
    public void BuildChildLookup_ContainsOnlyPlayerRecruiters() {
        var analysis = ChainAnalysis();

        var lookup = RecruitmentAnalyzer.BuildChildLookup(analysis);

        // Root and Mid recruited members; Leaf recruited nobody; Google is External (not a member).
        Assert.True(lookup.ContainsKey("Root"));
        Assert.True(lookup.ContainsKey("Mid"));
        Assert.False(lookup.ContainsKey("Leaf"));
        Assert.False(lookup.ContainsKey("Google"));
    }

    [Fact]
    public void BuildLineageTree_ProducesNestedTreeWithSubtreeRollups() {
        var analysis = ChainAnalysis();
        var lookup = RecruitmentAnalyzer.BuildChildLookup(analysis);
        var root = Tally(analysis, "Root");

        var tree = RecruitmentAnalyzer.BuildLineageTree(root, lookup);

        // Root node represents the recruiter: no user id, no own career TPE.
        Assert.Null(tree.UserId);
        Assert.Equal("Root", tree.Name);
        Assert.Equal(RecruiterCategory.Player, tree.Category);
        Assert.Equal(0, tree.CareerTpe);
        // Root subtree rolls up Mid + Leaf and equals the tally's lineage totals.
        Assert.Equal(root.LineageUsers, tree.SubtreeUsers);
        Assert.Equal(root.LineageCareerTpe, tree.SubtreeCareerTpe);
        Assert.Equal(2, tree.SubtreeUsers);
        Assert.Equal(600, tree.SubtreeCareerTpe);

        var mid = Assert.Single(tree.Recruited);
        Assert.Equal(2, mid.UserId);
        Assert.Equal("Mid", mid.Name);
        Assert.Equal(200, mid.CareerTpe);
        Assert.Equal(1, mid.SubtreeUsers);
        Assert.Equal(400, mid.SubtreeCareerTpe);

        var leaf = Assert.Single(mid.Recruited);
        Assert.Equal(3, leaf.UserId);
        Assert.Equal(400, leaf.CareerTpe);
        Assert.Equal(0, leaf.SubtreeUsers);
        Assert.Empty(leaf.Recruited);
    }

    [Fact]
    public void BuildLineageTree_ExternalRootSpansWholeDownstreamChain() {
        var analysis = ChainAnalysis();
        var lookup = RecruitmentAnalyzer.BuildChildLookup(analysis);
        var google = Assert.Single(analysis.Tallies, t => t.Category == RecruiterCategory.External);

        var tree = RecruitmentAnalyzer.BuildLineageTree(google, lookup);

        Assert.Equal(RecruiterCategory.External, tree.Category);
        Assert.Equal(3, tree.SubtreeUsers);
        Assert.Equal(700, tree.SubtreeCareerTpe);
        // Google -> Root (a member with its own TPE) -> Mid -> Leaf.
        var rootNode = Assert.Single(tree.Recruited);
        Assert.Equal(1, rootNode.UserId);
        Assert.Equal(100, rootNode.CareerTpe);
    }

    [Fact]
    public void BuildLineageTree_MaxDepthCapsTraversalAndRollups() {
        var analysis = ChainAnalysis();
        var lookup = RecruitmentAnalyzer.BuildChildLookup(analysis);
        var root = Tally(analysis, "Root");

        var tree = RecruitmentAnalyzer.BuildLineageTree(root, lookup, maxDepth: 1);

        // Only direct recruits (Mid); Leaf is beyond the cap.
        var mid = Assert.Single(tree.Recruited);
        Assert.Empty(mid.Recruited);
        Assert.Equal(0, mid.SubtreeUsers);
        Assert.Equal(1, tree.SubtreeUsers);
        Assert.Equal(200, tree.SubtreeCareerTpe);
    }

    [Fact]
    public void BuildLineageTree_RejectsNonPositiveMaxDepth() {
        var analysis = ChainAnalysis();
        var lookup = RecruitmentAnalyzer.BuildChildLookup(analysis);
        var root = Tally(analysis, "Root");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RecruitmentAnalyzer.BuildLineageTree(root, lookup, maxDepth: 0));
    }

    [Fact]
    public void BuildLineageTree_GuardsAgainstCycles() {
        // A recruited B; B recruited A. Both are members -> a 2-cycle in the recruiter graph.
        var analysis = RecruitmentAnalyzer.Aggregate(
            [
                Player(1, "A", 10, "B", 100),
                Player(2, "B", 20, "A", 200),
            ],
            ["A", "B"]);
        var lookup = RecruitmentAnalyzer.BuildChildLookup(analysis);
        var a = Tally(analysis, "A");

        var tree = RecruitmentAnalyzer.BuildLineageTree(a, lookup);

        // Terminates: A(root) -> B -> A(member, leaf because A already visited on the way down).
        var b = Assert.Single(tree.Recruited);
        Assert.Equal("B", b.Name);
        var aMember = Assert.Single(b.Recruited);
        Assert.Equal("A", aMember.Name);
        Assert.Empty(aMember.Recruited);
        Assert.Equal(2, tree.SubtreeUsers);
    }
}
