using Shuttle.EFCore.Recruitment;

namespace Shuttle.Tests.Analysis;

public class RecruitmentAnalyzerTests {

    private static RecruitedPlayer Player(
        int userId,
        string username,
        int playerId,
        string? recruiter,
        long latestTpe,
        DateTime? created = null,
        string? name = null
    ) => new(
        userId,
        username,
        playerId,
        name ?? $"Player {playerId}",
        created ?? new DateTime(2020, 1, 1).AddDays(playerId),
        recruiter,
        latestTpe);

    [Fact]
    public void Classify_MatchesMemberCaseInsensitively_AndReturnsCanonicalCasing() {
        var lookup = RecruitmentAnalyzer.BuildMemberLookup(["Gretzky"]);

        var (category, key) = RecruitmentAnalyzer.Classify("gRETZky", lookup);

        Assert.Equal(RecruiterCategory.Player, category);
        Assert.Equal("Gretzky", key);
    }

    [Theory]
    [InlineData("Myself")]
    [InlineData("myself")]
    [InlineData("  MYSELF  ")]
    public void Classify_TreatsMyselfAsSelf(string recruiter) {
        var lookup = RecruitmentAnalyzer.BuildMemberLookup([]);

        var (category, key) = RecruitmentAnalyzer.Classify(recruiter, lookup);

        Assert.Equal(RecruiterCategory.Self, category);
        Assert.Equal("Myself", key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Classify_TreatsBlankAsNone(string? recruiter) {
        var lookup = RecruitmentAnalyzer.BuildMemberLookup(["Gretzky"]);

        var (category, key) = RecruitmentAnalyzer.Classify(recruiter, lookup);

        Assert.Equal(RecruiterCategory.None, category);
        Assert.Equal(string.Empty, key);
    }

    [Fact]
    public void Classify_TreatsUnknownAsExternal_Trimmed() {
        var lookup = RecruitmentAnalyzer.BuildMemberLookup(["Gretzky"]);

        var (category, key) = RecruitmentAnalyzer.Classify("  Google ", lookup);

        Assert.Equal(RecruiterCategory.External, category);
        Assert.Equal("Google", key);
    }

    [Fact]
    public void ConsolidateByUser_TakesRecruiterFromEarliestPlayer_AndSumsCareerTpe() {
        var players = new[] {
            Player(1, "Rookie", playerId: 20, recruiter: "Reddit", latestTpe: 300,
                created: new DateTime(2021, 5, 1)),
            Player(1, "Rookie", playerId: 10, recruiter: "Gretzky", latestTpe: 500,
                created: new DateTime(2020, 1, 1)),
        };

        var users = RecruitmentAnalyzer.ConsolidateByUser(players);

        var user = Assert.Single(users);
        Assert.Equal(1, user.UserId);
        Assert.Equal("Gretzky", user.Recruiter); // from the earliest-created player
        Assert.Equal(800, user.CareerTpe);        // 500 + 300
    }

    [Fact]
    public void Aggregate_ConsolidatesByUser_CountsMembersAndSumsCareerTpe() {
        var players = new[] {
            // User 1: recruited by Gretzky, two players (career TPE 500 + 300 = 800).
            Player(1, "Rookie", 10, "Gretzky", 500, new DateTime(2020, 1, 1)),
            Player(1, "Rookie", 11, "Gretzky", 300, new DateTime(2021, 1, 1)),
            // User 2: recruited by Gretzky, one player (career TPE 200).
            Player(2, "Sophomore", 20, "gretzky", 200, new DateTime(2020, 2, 1)),
            // User 3: external recruiter.
            Player(3, "Third", 30, "Google", 150, new DateTime(2020, 3, 1)),
        };

        var analysis = RecruitmentAnalyzer.Aggregate(players, ["Gretzky"]);

        var gretzky = Assert.Single(analysis.Tallies, t => t.Category == RecruiterCategory.Player);
        Assert.Equal("Gretzky", gretzky.Recruiter);
        Assert.Equal(2, gretzky.RecruitedUsers);      // two distinct members, not three players
        Assert.Equal(1000, gretzky.TotalCareerTpe);   // 800 + 200

        var google = Assert.Single(analysis.Tallies, t => t.Category == RecruiterCategory.External);
        Assert.Equal(1, google.RecruitedUsers);
        Assert.Equal(150, google.TotalCareerTpe);
    }

    [Fact]
    public void Aggregate_OrdersTalliesByUserCountThenTpe() {
        var players = new[] {
            Player(1, "A", 10, "Google", 100),
            Player(2, "B", 20, "Google", 100),
            Player(3, "C", 30, "Reddit", 5000),
        };

        var analysis = RecruitmentAnalyzer.Aggregate(players, []);

        // Google has 2 members (beats Reddit's 1) despite Reddit's larger TPE.
        Assert.Equal("Google", analysis.Tallies[0].Recruiter);
        Assert.Equal("Reddit", analysis.Tallies[1].Recruiter);
    }

    [Fact]
    public void Aggregate_ProducesPerCategorySummary() {
        var players = new[] {
            Player(1, "A", 10, "Gretzky", 100),
            Player(2, "B", 20, "Google", 200),
            Player(3, "C", 30, "Myself", 300),
            Player(4, "D", 40, null, 400),
        };

        var analysis = RecruitmentAnalyzer.Aggregate(players, ["Gretzky"]);

        Assert.Equal(4, analysis.CategorySummary.Count);
        var none = Assert.Single(analysis.CategorySummary, c => c.Category == RecruiterCategory.None);
        Assert.Equal(1, none.RecruitedUsers);
        Assert.Equal(400, none.TotalCareerTpe);
        // Summary is ordered by the RecruiterCategory enum order.
        Assert.Equal(
            [RecruiterCategory.Player, RecruiterCategory.External, RecruiterCategory.Self, RecruiterCategory.None],
            analysis.CategorySummary.Select(c => c.Category));
    }

    [Fact]
    public void Aggregate_KeepsSelfAndSameNamedMemberSeparate() {
        // A member literally named "Myself" must not merge with self-recruited players.
        var players = new[] {
            Player(1, "A", 10, "Myself", 100),   // Self
            Player(2, "B", 20, "Coach", 200),     // Player (Coach is a member)
        };

        var analysis = RecruitmentAnalyzer.Aggregate(players, ["Coach"]);

        Assert.Contains(analysis.Tallies, t => t.Category == RecruiterCategory.Self && t.RecruitedUsers == 1);
        Assert.Contains(analysis.Tallies, t => t.Category == RecruiterCategory.Player && t.Recruiter == "Coach");
    }

    [Fact]
    public void Aggregate_ComputesTransitiveLineageTpe() {
        // Chain: Google -> Root; Root -> Mid; Mid -> Leaf. Members: Root, Mid, Leaf.
        var players = new[] {
            Player(1, "Root", 10, "Google", 100),
            Player(2, "Mid", 20, "Root", 200),
            Player(3, "Leaf", 30, "Mid", 400),
        };

        var analysis = RecruitmentAnalyzer.Aggregate(players, ["Root", "Mid", "Leaf"]);

        var root = Assert.Single(analysis.Tallies, t => t.Recruiter == "Root");
        // Direct total counts only Mid; lineage rolls up Mid + Leaf.
        Assert.Equal(1, root.RecruitedUsers);
        Assert.Equal(200, root.TotalCareerTpe);
        Assert.Equal(2, root.LineageUsers);
        Assert.Equal(600, root.LineageCareerTpe);

        var mid = Assert.Single(analysis.Tallies, t => t.Recruiter == "Mid");
        Assert.Equal(1, mid.LineageUsers);
        Assert.Equal(400, mid.LineageCareerTpe);

        // External recruiter's lineage spans the whole downstream chain.
        var google = Assert.Single(analysis.Tallies, t => t.Category == RecruiterCategory.External);
        Assert.Equal(1, google.RecruitedUsers);
        Assert.Equal(100, google.TotalCareerTpe);
        Assert.Equal(3, google.LineageUsers);
        Assert.Equal(700, google.LineageCareerTpe);
    }

    [Fact]
    public void Aggregate_LineageMatchesDirectTotalForLeafRecruiter() {
        var players = new[] {
            Player(1, "Solo", 10, "Google", 100),
        };

        var analysis = RecruitmentAnalyzer.Aggregate(players, ["Solo"]);

        var google = Assert.Single(analysis.Tallies);
        Assert.Equal(google.TotalCareerTpe, google.LineageCareerTpe);
        Assert.Equal(google.RecruitedUsers, google.LineageUsers);
    }
}
