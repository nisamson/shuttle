using EfCore = Shuttle.EFCore.Recruitment;
using Dto = Shuttle.Models.Recruitment;

namespace Shuttle.Api.Services.Recruitment;

/// <summary>
/// Maps the <see cref="Shuttle.EFCore.Recruitment"/> analysis records to the public
/// <see cref="Shuttle.Models.Recruitment"/> DTOs returned by <c>RecruitmentController</c>.
/// </summary>
public static class RecruitmentDtoMapper {
    /// <summary>Maps an EFCore recruiter category to its DTO counterpart.</summary>
    public static Dto.RecruiterCategory ToDto(this EfCore.RecruiterCategory category) => category switch {
        EfCore.RecruiterCategory.Player => Dto.RecruiterCategory.Player,
        EfCore.RecruiterCategory.External => Dto.RecruiterCategory.External,
        EfCore.RecruiterCategory.Self => Dto.RecruiterCategory.Self,
        EfCore.RecruiterCategory.None => Dto.RecruiterCategory.None,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown recruiter category."),
    };

    /// <summary>Maps a per-category count to its summary DTO.</summary>
    public static Dto.RecruiterCategorySummary ToSummaryDto(this EfCore.RecruiterCategoryCount count) => new() {
        Category = count.Category.ToDto(),
        DistinctRecruiters = count.DistinctRecruiters,
        RecruitedUsers = count.RecruitedUsers,
        TotalCareerTpe = count.TotalCareerTpe,
    };

    /// <summary>Maps a recruiter tally to its DTO (without the edge list).</summary>
    public static Dto.RecruiterTally ToTallyDto(this EfCore.RecruiterTally tally) => new() {
        Recruiter = tally.Recruiter,
        Category = tally.Category.ToDto(),
        RecruitedUsers = tally.RecruitedUsers,
        TotalCareerTpe = tally.TotalCareerTpe,
        LineageUsers = tally.LineageUsers,
        LineageCareerTpe = tally.LineageCareerTpe,
    };

    /// <summary>Maps a recruiter edge to a recruited-member DTO.</summary>
    public static Dto.RecruitedMember ToMemberDto(this EfCore.RecruitmentEdge edge) => new() {
        UserId = edge.UserId,
        Username = edge.Username,
        CareerTpe = edge.CareerTpe,
    };

    /// <summary>Maps a recruiter tally to a detail DTO, expanding its directly-recruited members.</summary>
    public static Dto.RecruiterDetail ToDetailDto(this EfCore.RecruiterTally tally) => new() {
        Tally = tally.ToTallyDto(),
        RecruitedMembers = [.. tally.Edges.Select(ToMemberDto)],
    };

    /// <summary>Maps a lineage node (and its subtree) to the tree-node DTO.</summary>
    public static Dto.RecruitmentTreeNode ToTreeDto(this EfCore.RecruitmentLineageNode node) => new() {
        UserId = node.UserId,
        Name = node.Name,
        Category = node.Category.ToDto(),
        CareerTpe = node.CareerTpe,
        SubtreeUsers = node.SubtreeUsers,
        SubtreeCareerTpe = node.SubtreeCareerTpe,
        Recruited = [.. node.Recruited.Select(ToTreeDto)],
    };
}
