using Shuttle.Api.Client;
using Shuttle.Models.Recruitment;

namespace Shuttle.WebClient.Testing;

/// <summary>
/// In-memory <see cref="IShuttleRecruitmentClient"/> that serves the deterministic recruitment graph
/// from <see cref="SeedData.RecruitmentEdges"/> without any HTTP, backend, or Azure dependency. It
/// mirrors the server's recruitment analysis semantics (per-category summary, filter/sort/limit on
/// the recruiter list, direct-recruit detail, and the transitive downstream lineage tree with
/// per-subtree roll-ups) closely enough that the WebClient behaves identically against it. Only
/// <see cref="RecruiterCategory.Player"/> and <see cref="RecruiterCategory.External"/> recruiters are
/// addressable by key; unknown keys return <see langword="null"/> (the server's 404).
/// </summary>
public sealed class InMemoryShuttleRecruitmentClient : IShuttleRecruitmentClient {
    /// <summary>Upper bound on the recruiter list limit (mirrors the server's clamp).</summary>
    private const int MaxLimit = 500;

    /// <summary>Upper bound on the lineage depth (mirrors the server's clamp).</summary>
    private const int MaxDepth = 32;

    private readonly IReadOnlyList<TallyModel> tallies;

    // Player-recruiter username (case-insensitive) -> the members they directly recruited. Only
    // Player recruiters can have descendants, so external/self/none recruiters are absent.
    private readonly IReadOnlyDictionary<string, IReadOnlyList<Edge>> childrenByUsername;

    /// <summary>Creates a client backed by the default seed graph.</summary>
    public InMemoryShuttleRecruitmentClient()
        : this(SeedData.RecruitmentEdges(), ResolveUsernames(), SeedData.UserCareerTpe()) {
    }

    /// <summary>
    /// Creates a client backed by a caller-supplied recruitment graph (useful for focused tests).
    /// </summary>
    /// <param name="edges">The recruiter → recruited-member edges.</param>
    /// <param name="usernamesByUserId">Maps each user id to their username.</param>
    /// <param name="careerTpeByUserId">Maps each user id to their full-career TPE.</param>
    public InMemoryShuttleRecruitmentClient(
        IReadOnlyList<SeedRecruitmentEdge> edges,
        IReadOnlyDictionary<int, string> usernamesByUserId,
        IReadOnlyDictionary<int, long> careerTpeByUserId) {
        (tallies, childrenByUsername) = Analyze(edges, usernamesByUserId, careerTpeByUserId);
    }

    public Task<IReadOnlyList<RecruiterCategorySummary>> GetSummary(CancellationToken token = default) {
        IReadOnlyList<RecruiterCategorySummary> summary =
        [
            .. tallies
                .GroupBy(t => t.Category)
                .Select(g => new RecruiterCategorySummary {
                    Category = g.Key,
                    DistinctRecruiters = g.Count(),
                    RecruitedUsers = g.Sum(t => t.RecruitedUsers),
                    TotalCareerTpe = g.Sum(t => t.TotalCareerTpe),
                })
                .OrderBy(c => c.Category),
        ];

        return Task.FromResult(summary);
    }

    public Task<IReadOnlyList<RecruiterTally>> GetRecruiters(
        RecruiterCategory? category = null,
        RecruiterSortField? sort = null,
        bool descending = true,
        int? limit = null,
        CancellationToken token = default) {
        IEnumerable<TallyModel> filtered = tallies;
        if (category is { } cat) {
            filtered = filtered.Where(t => t.Category == cat);
        }

        Func<TallyModel, long> key = (sort ?? RecruiterSortField.Recruits) switch {
            RecruiterSortField.CareerTpe => t => t.TotalCareerTpe,
            RecruiterSortField.LineageUsers => t => t.LineageUsers,
            RecruiterSortField.LineageTpe => t => t.LineageCareerTpe,
            _ => t => t.RecruitedUsers,
        };

        var ordered = (descending ? filtered.OrderByDescending(key) : filtered.OrderBy(key))
            .ThenBy(t => t.Recruiter, StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => t.Category);

        IEnumerable<TallyModel> selected = limit is { } n ? ordered.Take(Math.Clamp(n, 1, MaxLimit)) : ordered;

        IReadOnlyList<RecruiterTally> result = [.. selected.Select(ToTallyDto)];
        return Task.FromResult(result);
    }

    public Task<RecruiterDetail?> GetRecruiter(string recruiter, CancellationToken token = default) {
        var tally = FindAddressable(recruiter);
        if (tally is null) {
            return Task.FromResult<RecruiterDetail?>(null);
        }

        var detail = new RecruiterDetail {
            Tally = ToTallyDto(tally),
            RecruitedMembers = [.. tally.Edges.Select(ToMemberDto)],
        };

        return Task.FromResult<RecruiterDetail?>(detail);
    }

    public Task<RecruitmentTreeNode?> GetRecruiterLineage(
        string recruiter,
        int? maxDepth = null,
        CancellationToken token = default) {
        var tally = FindAddressable(recruiter);
        if (tally is null) {
            return Task.FromResult<RecruitmentTreeNode?>(null);
        }

        var depthCap = maxDepth is { } d ? Math.Clamp(d, 1, MaxDepth) : (int?)null;
        var tree = BuildLineageTree(tally, depthCap);
        return Task.FromResult<RecruitmentTreeNode?>(tree);
    }

    /// <summary>
    /// Finds the addressable recruiter (Player/External only) whose key matches
    /// <paramref name="recruiter"/> case-insensitively, or <c>null</c>.
    /// </summary>
    private TallyModel? FindAddressable(string recruiter) =>
        tallies.FirstOrDefault(t =>
            (t.Category == RecruiterCategory.Player || t.Category == RecruiterCategory.External)
            && string.Equals(t.Recruiter, recruiter, StringComparison.OrdinalIgnoreCase));

    private static RecruiterTally ToTallyDto(TallyModel tally) => new() {
        Recruiter = tally.Recruiter,
        Category = tally.Category,
        RecruitedUsers = tally.RecruitedUsers,
        TotalCareerTpe = tally.TotalCareerTpe,
        LineageUsers = tally.LineageUsers,
        LineageCareerTpe = tally.LineageCareerTpe,
    };

    private static RecruitedMember ToMemberDto(Edge edge) => new() {
        UserId = edge.UserId,
        Username = edge.Username,
        CareerTpe = edge.CareerTpe,
    };

    /// <summary>Maps the seeded users to a user-id → username lookup.</summary>
    private static IReadOnlyDictionary<int, string> ResolveUsernames() =>
        SeedData.Users().ToDictionary(u => u.UserId, u => u.Username);

    /// <summary>
    /// Builds the per-recruiter tallies and the Player child-adjacency lookup, mirroring the server's
    /// <c>RecruitmentAnalyzer.AggregateUsers</c>.
    /// </summary>
    private static (IReadOnlyList<TallyModel> Tallies, IReadOnlyDictionary<string, IReadOnlyList<Edge>> Children) Analyze(
        IReadOnlyList<SeedRecruitmentEdge> seedEdges,
        IReadOnlyDictionary<int, string> usernamesByUserId,
        IReadOnlyDictionary<int, long> careerTpeByUserId) {
        var groups = new Dictionary<(RecruiterCategory Category, string Key), List<Edge>>();
        foreach (var seed in seedEdges) {
            var username = usernamesByUserId.GetValueOrDefault(seed.RecruitedUserId, $"user{seed.RecruitedUserId}");
            var careerTpe = careerTpeByUserId.GetValueOrDefault(seed.RecruitedUserId);
            var edge = new Edge(seed.Recruiter, seed.Category, seed.RecruitedUserId, username, careerTpe);
            if (!groups.TryGetValue((seed.Category, seed.Recruiter), out var edges)) {
                edges = [];
                groups[(seed.Category, seed.Recruiter)] = edges;
            }

            edges.Add(edge);
        }

        // Only Player recruiters (keyed by member username) can have downstream recruits.
        var children = new Dictionary<string, IReadOnlyList<Edge>>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups) {
            if (group.Key.Category == RecruiterCategory.Player) {
                children[group.Key.Key] = group.Value;
            }
        }

        // Sum the whole downstream subtree reachable from a recruiter's direct edges, counting each
        // member once (the visited set de-duplicates and guards against cycles).
        (int Users, long CareerTpe) ComputeLineage(IReadOnlyList<Edge> directEdges) {
            var visited = new HashSet<int>();
            var stack = new Stack<Edge>(directEdges);
            long tpe = 0;
            while (stack.Count > 0) {
                var edge = stack.Pop();
                if (!visited.Add(edge.UserId)) {
                    continue;
                }

                tpe += edge.CareerTpe;
                if (children.TryGetValue(edge.Username, out var kids)) {
                    foreach (var kid in kids) {
                        stack.Push(kid);
                    }
                }
            }

            return (visited.Count, tpe);
        }

        var tallies = groups
            .Select(g => {
                var (lineageUsers, lineageTpe) = ComputeLineage(g.Value);
                IReadOnlyList<Edge> orderedEdges =
                [
                    .. g.Value
                        .OrderByDescending(e => e.CareerTpe)
                        .ThenBy(e => e.Username, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(e => e.UserId),
                ];
                return new TallyModel(
                    g.Key.Key,
                    g.Key.Category,
                    g.Value.Count,
                    g.Value.Sum(e => e.CareerTpe),
                    lineageUsers,
                    lineageTpe,
                    orderedEdges);
            })
            .OrderByDescending(t => t.RecruitedUsers)
            .ThenByDescending(t => t.TotalCareerTpe)
            .ThenBy(t => t.Recruiter, StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => t.Category)
            .ToList();

        return (tallies, children);
    }

    /// <summary>
    /// Builds the nested downstream lineage tree for a recruiter, mirroring the server's
    /// <c>RecruitmentAnalyzer.BuildLineageTree</c>. The root node represents the recruiter itself
    /// (<see cref="RecruitmentTreeNode.UserId"/> <c>null</c>, <see cref="RecruitmentTreeNode.CareerTpe"/>
    /// <c>0</c>); per-subtree roll-ups count each member once and exclude the node itself.
    /// </summary>
    private RecruitmentTreeNode BuildLineageTree(TallyModel root, int? maxDepth) {
        var visited = new HashSet<int>();

        RecruitmentTreeNode? BuildMember(Edge edge, int depth) {
            if (!visited.Add(edge.UserId)) {
                return null;
            }

            var kids = childrenByUsername.TryGetValue(edge.Username, out var e) ? e : [];
            var (nodeChildren, subtreeUsers, subtreeTpe) = ExpandEdges(kids, depth);
            return new RecruitmentTreeNode {
                UserId = edge.UserId,
                Name = edge.Username,
                Category = edge.Category,
                CareerTpe = edge.CareerTpe,
                SubtreeUsers = subtreeUsers,
                SubtreeCareerTpe = subtreeTpe,
                Recruited = nodeChildren,
            };
        }

        (IReadOnlyList<RecruitmentTreeNode> Children, int SubtreeUsers, long SubtreeTpe) ExpandEdges(
            IReadOnlyList<Edge> edges,
            int parentDepth) {
            if (maxDepth is { } cap && parentDepth >= cap) {
                return ([], 0, 0);
            }

            var children = new List<RecruitmentTreeNode>();
            var subtreeUsers = 0;
            long subtreeTpe = 0;
            foreach (var kid in edges) {
                var child = BuildMember(kid, parentDepth + 1);
                if (child is null) {
                    continue;
                }

                children.Add(child);
                subtreeUsers += 1 + child.SubtreeUsers;
                subtreeTpe += child.CareerTpe + child.SubtreeCareerTpe;
            }

            return (children, subtreeUsers, subtreeTpe);
        }

        var (rootChildren, rootUsers, rootTpe) = ExpandEdges(root.Edges, parentDepth: 0);
        return new RecruitmentTreeNode {
            UserId = null,
            Name = root.Recruiter,
            Category = root.Category,
            CareerTpe = 0,
            SubtreeUsers = rootUsers,
            SubtreeCareerTpe = rootTpe,
            Recruited = rootChildren,
        };
    }

    /// <summary>A recruiter → recruited-member edge (one row per recruited user).</summary>
    private sealed record Edge(
        string Recruiter,
        RecruiterCategory Category,
        int UserId,
        string Username,
        long CareerTpe);

    /// <summary>A recruiter's aggregated tally plus its ordered direct-recruit edges.</summary>
    private sealed record TallyModel(
        string Recruiter,
        RecruiterCategory Category,
        int RecruitedUsers,
        long TotalCareerTpe,
        int LineageUsers,
        long LineageCareerTpe,
        IReadOnlyList<Edge> Edges);
}
