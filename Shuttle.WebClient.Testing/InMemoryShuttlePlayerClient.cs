using Shuttle.Api.Client;
using Shuttle.Models.Players;
using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.WebClient.Testing;

/// <summary>
/// In-memory <see cref="IShuttlePlayerClient"/> that serves <see cref="SeedData"/> without any HTTP,
/// backend, or Azure dependency. The filtering, sorting, and paging mirror the semantics of the
/// server's <c>GET /players/search</c> (see <c>PlayerController</c>) closely enough that the
/// WebClient behaves identically against it. It is a faithful simplification, not a byte-for-byte
/// reproduction of the SQL backend.
/// </summary>
public sealed class InMemoryShuttlePlayerClient : IShuttlePlayerClient {
    private const int MaxPageSize = 100;

    private readonly IReadOnlyList<PlayerCard> players;
    private readonly IReadOnlyList<PlayerSuggestion> suggestions;

    /// <summary>Creates a client backed by the default <see cref="SeedData"/>.</summary>
    public InMemoryShuttlePlayerClient()
        : this(SeedData.Players()) {
    }

    /// <summary>Creates a client backed by a caller-supplied player set (useful for focused tests).</summary>
    public InMemoryShuttlePlayerClient(IReadOnlyList<PlayerCard> players) {
        this.players = players;
        suggestions = players
            .Select(p => new PlayerSuggestion {
                PlayerId = p.PlayerId,
                Name = p.Name,
                Username = p.Username,
                Status = p.Status,
                Position = p.Position,
            })
            .ToList();
    }

    public Task<IReadOnlyList<PlayerCard>> GetPlayers(CancellationToken token = default) =>
        Task.FromResult<IReadOnlyList<PlayerCard>>(
            players.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList());

    public Task<IReadOnlyList<PlayerSuggestion>> GetPlayerSuggestions(CancellationToken token = default) =>
        Task.FromResult<IReadOnlyList<PlayerSuggestion>>(suggestions);

    public Task<PlayerCard?> GetPlayer(int playerId, CancellationToken token = default) =>
        Task.FromResult(players.FirstOrDefault(p => p.PlayerId == playerId));

    public Task<IReadOnlyList<TpeTimelinePoint>?> GetPlayerTpeTimeline(int playerId, CancellationToken token = default) {
        var player = players.FirstOrDefault(p => p.PlayerId == playerId);
        return Task.FromResult(player is null ? null : BuildTimeline(player));
    }

    // Mirrors the server's QUERY /players/resolve semantics against the seed set: case-insensitive
    // name matching (ambiguous names rejected), unknown ids/names reported, resolved players
    // de-duplicated preserving order (requested ids first, then name-resolved).
    public Task<ResolvePlayersResult> ResolvePlayers(ResolvePlayersRequest request, CancellationToken token = default) {
        var requestedIds = (request.PlayerIds ?? []).ToList();
        var requestedNames = (request.Names ?? [])
            .Select(n => n?.Trim() ?? string.Empty)
            .Where(n => n.Length > 0)
            .ToList();

        var notFound = new List<string>();

        var byLoweredName = players
            .GroupBy(p => p.Name.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Select(p => p.PlayerId).Distinct().ToList());

        var ambiguous = new List<string>();
        var resolvedFromNames = new List<int>();
        var seenLoweredNames = new HashSet<string>();
        foreach (var name in requestedNames) {
            var lowered = name.ToLowerInvariant();
            if (!seenLoweredNames.Add(lowered)) {
                continue;
            }

            if (!byLoweredName.TryGetValue(lowered, out var ids) || ids.Count == 0) {
                notFound.Add(name);
            } else if (ids.Count > 1) {
                ambiguous.Add(name);
            } else {
                resolvedFromNames.Add(ids[0]);
            }
        }

        var byId = players.ToDictionary(p => p.PlayerId);

        var resolved = new List<ResolvedPlayer>();
        var seenIds = new HashSet<int>();
        foreach (var id in requestedIds) {
            if (!seenIds.Add(id)) {
                continue;
            }

            if (byId.TryGetValue(id, out var card)) {
                resolved.Add(ToResolved(card));
            } else {
                notFound.Add(id.ToString());
            }
        }

        foreach (var id in resolvedFromNames) {
            if (seenIds.Add(id) && byId.TryGetValue(id, out var card)) {
                resolved.Add(ToResolved(card));
            }
        }

        return Task.FromResult(new ResolvePlayersResult {
            Resolved = resolved,
            NotFound = notFound,
            Ambiguous = ambiguous,
        });
    }

    private static ResolvedPlayer ToResolved(PlayerCard card) => new() {
        PlayerId = card.PlayerId,
        Name = card.Name,
        Username = card.Username,
        Status = card.Status,
        Position = card.Position,
        DraftSeason = card.DraftSeason,
        TotalTpe = card.TotalTpe,
    };

    // Synthesizes a deterministic, monotonically increasing TPE ramp from the player's creation date
    // up to their current TotalTpe, so the offline profile renders a real timeline chart. Players
    // with no TPE (e.g. pending/denied) yield an empty timeline, exercising the "no data" state.
    private static IReadOnlyList<TpeTimelinePoint> BuildTimeline(PlayerCard player) {
        const int steps = 6;
        if (player.TotalTpe <= 0) {
            return Array.Empty<TpeTimelinePoint>();
        }

        var points = new List<TpeTimelinePoint>(steps);
        for (var i = 1; i <= steps; i++) {
            points.Add(new TpeTimelinePoint {
                TaskDate = player.CreationDate.AddDays(30 * (i - 1)),
                TotalTpe = (int)Math.Round(player.TotalTpe * ((double)i / steps)),
            });
        }

        return points;
    }

    public Task<PagedResult<PlayerCard>> SearchPlayers(PlayerSearchQuery query, CancellationToken token = default) {
        var filtered = ApplyFilters(players, query).ToList();
        var totalCount = filtered.Count;

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var items = ApplySort(filtered, query)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new PagedResult<PlayerCard> {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        });
    }

    private static IEnumerable<PlayerCard> ApplyFilters(IEnumerable<PlayerCard> source, PlayerSearchQuery query) {
        if (!string.IsNullOrWhiteSpace(query.Text)) {
            var text = query.Text.Trim();
            source = source.Where(p =>
                p.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
                || p.Username.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        if (query.Positions is { Count: > 0 }) {
            var positions = query.Positions
                .Select(code => PlayerPosition.TryFromString(code, out var pos) ? pos : (PlayerPosition?)null)
                .Where(pos => pos is not null)
                .Select(pos => pos!.Value)
                .Distinct()
                .ToList();

            if (positions.Count > 0) {
                source = source.Where(p => positions.Contains(p.Position));
            }
        }

        if (query.Statuses is { Count: > 0 }) {
            var statuses = query.Statuses.Distinct().ToList();
            source = source.Where(p => statuses.Contains(p.Status));
        }

        if (query.Leagues is { Count: > 0 }) {
            var leagues = query.Leagues.Distinct().ToList();
            source = source.Where(p => p.CurrentLeague is not null && leagues.Contains(p.CurrentLeague.Value));
        }

        if (query.Handedness is { Count: > 0 }) {
            var handedness = query.Handedness.Distinct().ToList();
            source = source.Where(p => handedness.Contains(p.Handedness));
        }

        if (query.DraftSeason is { } draftSeason) {
            source = source.Where(p => p.DraftSeason == draftSeason);
        }

        if (!string.IsNullOrWhiteSpace(query.IihfNation)) {
            var nation = query.IihfNation.Trim();
            source = source.Where(p =>
                p.IihfNation is not null && p.IihfNation.Contains(nation, StringComparison.OrdinalIgnoreCase));
        }

        if (query.Inactive is { } inactive) {
            source = source.Where(p => p.Inactive == inactive);
        }

        if (query.Suspended is { } suspended) {
            source = source.Where(p => p.IsSuspended == suspended);
        }

        if (query.MinTotalTpe is { } minTpe) {
            source = source.Where(p => p.TotalTpe >= minTpe);
        }

        if (query.MaxTotalTpe is { } maxTpe) {
            source = source.Where(p => p.TotalTpe <= maxTpe);
        }

        if (query.MinBankBalance is { } minBank) {
            source = source.Where(p => p.BankBalance >= minBank);
        }

        if (query.MaxBankBalance is { } maxBank) {
            source = source.Where(p => p.BankBalance <= maxBank);
        }

        return source;
    }

    // PlayerId is a stable tiebreaker so paging is deterministic, matching the server.
    private static IEnumerable<PlayerCard> ApplySort(IEnumerable<PlayerCard> source, PlayerSearchQuery query) {
        var desc = query.SortDescending;

        return query.SortBy switch {
            PlayerSortField.TotalTpe => OrderBy(source, p => p.TotalTpe, desc),
            PlayerSortField.DraftSeason => OrderBy(source, p => p.DraftSeason, desc),
            PlayerSortField.Position => OrderBy(source, p => p.Position, desc),
            PlayerSortField.Status => OrderBy(source, p => p.Status, desc),
            PlayerSortField.League => OrderBy(source, p => p.CurrentLeague, desc),
            PlayerSortField.Username => OrderBy(source, p => p.Username, desc),
            PlayerSortField.Created => OrderBy(source, p => p.CreationDate, desc),
            _ => OrderBy(source, p => p.Name, desc),
        };
    }

    private static IEnumerable<PlayerCard> OrderBy<TKey>(
        IEnumerable<PlayerCard> source,
        Func<PlayerCard, TKey> keySelector,
        bool descending) =>
        (descending
            ? source.OrderByDescending(keySelector)
            : source.OrderBy(keySelector))
        .ThenBy(p => p.PlayerId);
}
