namespace Shuttle.Models.Recruitment;

/// <summary>
/// A single node in a recruiter's downstream lineage tree, returned by
/// <c>GET /recruitment/recruiters/{recruiter}/lineage</c>. The root node represents the recruiter
/// itself (<see cref="UserId"/> <c>null</c>, <see cref="CareerTpe"/> <c>0</c>); every other node is
/// a recruited member.
/// </summary>
public record RecruitmentTreeNode {
    /// <summary>The member's user id, or <c>null</c> for the recruiter root.</summary>
    public required int? UserId { get; init; }

    /// <summary>The member's username, or the recruiter key for the root.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// For the root, the recruiter's own category; for a member node, the category of the recruiter
    /// relationship that brought them in.
    /// </summary>
    public required RecruiterCategory Category { get; init; }

    /// <summary>This member's own full-career TPE; <c>0</c> for the recruiter root.</summary>
    public required long CareerTpe { get; init; }

    /// <summary>
    /// The number of distinct members in this node's subtree, excluding the node itself. At the root
    /// this equals the tally's lineage user count when the tree is not depth-capped.
    /// </summary>
    public required int SubtreeUsers { get; init; }

    /// <summary>
    /// The combined full-career TPE of every member in this node's subtree, excluding the node
    /// itself. At the root this equals the tally's lineage career TPE when not depth-capped.
    /// </summary>
    public required long SubtreeCareerTpe { get; init; }

    /// <summary>
    /// This node's directly-recruited members, ordered by career TPE (desc) then username. Empty at a
    /// leaf or where a depth cap stops traversal.
    /// </summary>
    public required IReadOnlyList<RecruitmentTreeNode> Recruited { get; init; }
}
