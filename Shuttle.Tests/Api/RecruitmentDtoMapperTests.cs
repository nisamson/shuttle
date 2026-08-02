using Shuttle.Api.Services.Recruitment;
using EfCore = Shuttle.EFCore.Recruitment;
using Dto = Shuttle.Models.Recruitment;

namespace Shuttle.Tests.Api;

/// <summary>
/// Unit tests for <see cref="RecruitmentDtoMapper"/>: the EFCore recruitment records to public
/// <see cref="Shuttle.Models.Recruitment"/> DTO projections.
/// </summary>
public class RecruitmentDtoMapperTests {

    [Theory]
    [InlineData(EfCore.RecruiterCategory.Player, Dto.RecruiterCategory.Player)]
    [InlineData(EfCore.RecruiterCategory.External, Dto.RecruiterCategory.External)]
    [InlineData(EfCore.RecruiterCategory.Self, Dto.RecruiterCategory.Self)]
    [InlineData(EfCore.RecruiterCategory.None, Dto.RecruiterCategory.None)]
    public void ToDto_MapsEveryCategory(EfCore.RecruiterCategory source, Dto.RecruiterCategory expected) {
        Assert.Equal(expected, source.ToDto());
    }

    [Fact]
    public void ToSummaryDto_MapsAllFields() {
        var count = new EfCore.RecruiterCategoryCount(EfCore.RecruiterCategory.External, 3, 7, 1234);

        var dto = count.ToSummaryDto();

        Assert.Equal(Dto.RecruiterCategory.External, dto.Category);
        Assert.Equal(3, dto.DistinctRecruiters);
        Assert.Equal(7, dto.RecruitedUsers);
        Assert.Equal(1234, dto.TotalCareerTpe);
    }

    private static EfCore.RecruiterTally SampleTally() => new(
        "Gretzky",
        EfCore.RecruiterCategory.Player,
        RecruitedUsers: 2,
        TotalCareerTpe: 700,
        LineageUsers: 3,
        LineageCareerTpe: 900,
        Edges: [
            new EfCore.RecruitmentEdge("Gretzky", EfCore.RecruiterCategory.Player, 2, "Rookie", 500),
            new EfCore.RecruitmentEdge("Gretzky", EfCore.RecruiterCategory.Player, 3, "Sophomore", 200),
        ]);

    [Fact]
    public void ToTallyDto_MapsAllFields() {
        var dto = SampleTally().ToTallyDto();

        Assert.Equal("Gretzky", dto.Recruiter);
        Assert.Equal(Dto.RecruiterCategory.Player, dto.Category);
        Assert.Equal(2, dto.RecruitedUsers);
        Assert.Equal(700, dto.TotalCareerTpe);
        Assert.Equal(3, dto.LineageUsers);
        Assert.Equal(900, dto.LineageCareerTpe);
    }

    [Fact]
    public void ToDetailDto_ExpandsEdgesPreservingOrder() {
        var dto = SampleTally().ToDetailDto();

        Assert.Equal("Gretzky", dto.Tally.Recruiter);
        Assert.Collection(
            dto.RecruitedMembers,
            m => {
                Assert.Equal(2, m.UserId);
                Assert.Equal("Rookie", m.Username);
                Assert.Equal(500, m.CareerTpe);
            },
            m => {
                Assert.Equal(3, m.UserId);
                Assert.Equal("Sophomore", m.Username);
                Assert.Equal(200, m.CareerTpe);
            });
    }

    [Fact]
    public void ToTreeDto_MapsNodeAndDescendantsRecursively() {
        var node = new EfCore.RecruitmentLineageNode(
            UserId: null,
            "Gretzky",
            EfCore.RecruiterCategory.Player,
            CareerTpe: 0,
            SubtreeUsers: 1,
            SubtreeCareerTpe: 500,
            Recruited: [
                new EfCore.RecruitmentLineageNode(2, "Rookie", EfCore.RecruiterCategory.Player, 500, 0, 0, []),
            ]);

        var dto = node.ToTreeDto();

        Assert.Null(dto.UserId);
        Assert.Equal("Gretzky", dto.Name);
        Assert.Equal(Dto.RecruiterCategory.Player, dto.Category);
        Assert.Equal(0, dto.CareerTpe);
        Assert.Equal(1, dto.SubtreeUsers);
        Assert.Equal(500, dto.SubtreeCareerTpe);

        var child = Assert.Single(dto.Recruited);
        Assert.Equal(2, child.UserId);
        Assert.Equal("Rookie", child.Name);
        Assert.Equal(500, child.CareerTpe);
        Assert.Empty(child.Recruited);
    }
}
