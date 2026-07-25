namespace Shuttle.EFCore.Recruitment;

/// <summary>
/// Pure (database-free) consolidation, classification, and aggregation of player recruitment data.
/// Reusable by any caller that already has a set of member names and player projections (the CLI
/// analysis flow and, later, the API server).
/// </summary>
/// <remarks>
/// Recruitment is consolidated by <em>member</em>: a member is recruited once, recorded on their
/// earliest-created player, and their contribution is their full-career TPE (summed across all of
/// their players' latest timeline totals).
/// </remarks>
public static class RecruitmentAnalyzer {

    /// <summary>The recruiter value that denotes a player recruiting themselves.</summary>
    public const string SelfRecruiter = "Myself";

    /// <summary>
    /// Classifies a raw recruiter value against the known set of member names.
    /// </summary>
    /// <param name="recruiter">The raw recruiter value (may be <c>null</c>/blank).</param>
    /// <param name="memberNames">
    /// A case-insensitive lookup of member names to their canonical casing (see
    /// <see cref="BuildMemberLookup"/>).
    /// </param>
    /// <returns>
    /// The category and the normalized recruiter key: canonical member casing for a
    /// <see cref="RecruiterCategory.Player"/>, the trimmed value for <see cref="RecruiterCategory.External"/>,
    /// <see cref="SelfRecruiter"/> for <see cref="RecruiterCategory.Self"/>, and an empty string for
    /// <see cref="RecruiterCategory.None"/>.
    /// </returns>
    public static (RecruiterCategory Category, string Key) Classify(
        string? recruiter,
        IReadOnlyDictionary<string, string> memberNames
    ) {
        ArgumentNullException.ThrowIfNull(memberNames);

        if (string.IsNullOrWhiteSpace(recruiter)) {
            return (RecruiterCategory.None, string.Empty);
        }

        var trimmed = recruiter.Trim();

        if (memberNames.TryGetValue(trimmed, out var canonical)) {
            return (RecruiterCategory.Player, canonical);
        }

        if (string.Equals(trimmed, SelfRecruiter, StringComparison.OrdinalIgnoreCase)) {
            return (RecruiterCategory.Self, SelfRecruiter);
        }

        return (RecruiterCategory.External, trimmed);
    }

    /// <summary>
    /// Builds a case-insensitive member-name lookup mapping each name to its canonical casing. When the
    /// same name appears more than once (differing only by case) the first occurrence wins.
    /// </summary>
    public static IReadOnlyDictionary<string, string> BuildMemberLookup(IEnumerable<string> memberNames) {
        ArgumentNullException.ThrowIfNull(memberNames);

        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in memberNames) {
            if (string.IsNullOrWhiteSpace(name)) {
                continue;
            }

            var trimmed = name.Trim();
            lookup.TryAdd(trimmed, trimmed);
        }

        return lookup;
    }

    /// <summary>
    /// Consolidates per-player rows into per-member <see cref="RecruitedUser"/> records: the member's
    /// recruiter is taken from their earliest-created player (tie-broken by player id), and their
    /// <see cref="RecruitedUser.CareerTpe"/> is the sum of every one of their players'
    /// <see cref="RecruitedPlayer.LatestTimelineTpe"/>.
    /// </summary>
    public static IReadOnlyList<RecruitedUser> ConsolidateByUser(IEnumerable<RecruitedPlayer> players) {
        ArgumentNullException.ThrowIfNull(players);

        return players
            .GroupBy(p => p.UserId)
            .Select(g => {
                var firstPlayer = g
                    .OrderBy(p => p.CreationTime)
                    .ThenBy(p => p.PlayerId)
                    .First();
                return new RecruitedUser(
                    g.Key,
                    firstPlayer.OwnerUsername,
                    firstPlayer.Recruiter,
                    g.Sum(p => p.LatestTimelineTpe));
            })
            .ToList();
    }

    /// <summary>
    /// Consolidates the players by member and aggregates the members into per-recruiter tallies and a
    /// per-category summary.
    /// </summary>
    /// <param name="players">The per-player rows to analyze.</param>
    /// <param name="memberNames">The known member names (any enumerable; matched case-insensitively).</param>
    public static RecruitmentAnalysis Aggregate(
        IEnumerable<RecruitedPlayer> players,
        IEnumerable<string> memberNames
    ) {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(memberNames);

        return AggregateUsers(ConsolidateByUser(players), memberNames);
    }

    /// <summary>
    /// Aggregates already-consolidated members into per-recruiter tallies and a per-category summary.
    /// </summary>
    /// <param name="users">The recruited members (one per user).</param>
    /// <param name="memberNames">The known member names (any enumerable; matched case-insensitively).</param>
    public static RecruitmentAnalysis AggregateUsers(
        IEnumerable<RecruitedUser> users,
        IEnumerable<string> memberNames
    ) {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(memberNames);

        var lookup = BuildMemberLookup(memberNames);

        // Group edges by (category, normalized key). Category is part of the key so the pseudo-recruiters
        // (Self/None) never collide with a same-named member/external value.
        var groups = new Dictionary<(RecruiterCategory Category, string Key), List<RecruitmentEdge>>();

        foreach (var user in users) {
            var (category, key) = Classify(user.Recruiter, lookup);
            var edge = new RecruitmentEdge(key, category, user.UserId, user.Username, user.CareerTpe);
            if (!groups.TryGetValue((category, key), out var edges)) {
                edges = [];
                groups[(category, key)] = edges;
            }

            edges.Add(edge);
        }

        // Build the child adjacency for lineage traversal. A Player recruiter's key is a member
        // username, so when a recruited member is themselves a Player recruiter their own recruits
        // form the next generation. Only Player-category groups can have descendants.
        var childrenByUsername = new Dictionary<string, List<RecruitmentEdge>>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups) {
            if (group.Key.Category == RecruiterCategory.Player) {
                childrenByUsername[group.Key.Key] = group.Value;
            }
        }

        // Sum the whole downstream subtree reachable from a recruiter's direct edges, counting each
        // member once (a visited set both de-duplicates and guards against any cycles in the data).
        (int Users, long CareerTpe) ComputeLineage(IReadOnlyList<RecruitmentEdge> directEdges) {
            var visited = new HashSet<int>();
            var stack = new Stack<RecruitmentEdge>(directEdges);
            long tpe = 0;
            while (stack.Count > 0) {
                var edge = stack.Pop();
                if (!visited.Add(edge.UserId)) {
                    continue;
                }

                tpe += edge.CareerTpe;
                if (childrenByUsername.TryGetValue(edge.Username, out var kids)) {
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
                return new RecruiterTally(
                    g.Key.Key,
                    g.Key.Category,
                    g.Value.Count,
                    g.Value.Sum(e => e.CareerTpe),
                    lineageUsers,
                    lineageTpe,
                    [.. g.Value
                        .OrderByDescending(e => e.CareerTpe)
                        .ThenBy(e => e.Username, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(e => e.UserId)]);
            })
            .OrderByDescending(t => t.RecruitedUsers)
            .ThenByDescending(t => t.TotalCareerTpe)
            .ThenBy(t => t.Recruiter, StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => t.Category)
            .ToList();

        var summary = tallies
            .GroupBy(t => t.Category)
            .Select(g => new RecruiterCategoryCount(
                g.Key,
                g.Count(),
                g.Sum(t => t.RecruitedUsers),
                g.Sum(t => t.TotalCareerTpe)))
            .OrderBy(c => c.Category)
            .ToList();

        var allEdges = tallies.SelectMany(t => t.Edges).ToList();

        return new RecruitmentAnalysis(tallies, summary, allEdges);
    }
}
