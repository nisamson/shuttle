using Shuttle.Models.Players;
using Shuttle.Models.Users;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;

namespace Shuttle.WebClient.Testing;

/// <summary>
/// Deterministic, Azure-free player data used by both the bUnit tests and the WebClient's
/// fake-backend run mode. The same seed produces the same players on every run, so tests and
/// Playwright snapshots stay stable. Attributes are intentionally left <see langword="null"/>
/// (a valid <see cref="PlayerCard"/> state); the profile page renders a "no attributes ingested"
/// message for those, which keeps the seed small and offline.
/// </summary>
public static class SeedData {
    /// <summary>
    /// The canonical set of seeded players, ordered by <see cref="PlayerCard.Name"/> (matching the
    /// server's default ordering). Regenerated fresh on each call so callers can't mutate the seed.
    /// </summary>
    public static IReadOnlyList<PlayerCard> Players() => BuildPlayers()
        .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    /// <summary>The slim autocomplete directory derived from <see cref="Players"/>.</summary>
    public static IReadOnlyList<PlayerSuggestion> Suggestions() => Players()
        .Select(p => new PlayerSuggestion {
            PlayerId = p.PlayerId,
            Name = p.Name,
            Username = p.Username,
            Status = p.Status,
            Position = p.Position,
        })
        .ToList();

    /// <summary>
    /// The canonical set of seeded users, derived from <see cref="Players"/> (grouped by user id),
    /// ordered by username. A user's <see cref="SeedUser.Username"/> is taken from their
    /// earliest-created player; Discord names are attached for a subset (see
    /// <see cref="DiscordNames"/>) so the WebClient's authenticated Discord gating can be exercised.
    /// </summary>
    public static IReadOnlyList<SeedUser> Users() {
        var discord = DiscordNames();

        return Players()
            .GroupBy(p => p.UserId)
            .Select(g => {
                var earliest = g.OrderBy(p => p.CreationDate).ThenBy(p => p.PlayerId).First();
                return new SeedUser(
                    g.Key,
                    earliest.Username,
                    discord.GetValueOrDefault(g.Key));
            })
            .OrderBy(u => u.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>The slim user autocomplete directory derived from <see cref="Users"/>.</summary>
    public static IReadOnlyList<UserSuggestion> UserSuggestions() => Users()
        .Select(u => new UserSuggestion { UserId = u.UserId, Username = u.Username })
        .ToList();

    // Discord names for a subset of users (keyed by user id), so authenticated-only Discord
    // behaviour has something to surface while other users deliberately have none.
    private static IReadOnlyDictionary<int, string> DiscordNames() => new Dictionary<int, string> {
        [5001] = "frostbite",
        [5002] = "bella.ridge",
        [5004] = "marshmallow",
        [5006] = "faye_nolan",
        [5009] = "ivan.pope",
    };

    private static IEnumerable<PlayerCard> BuildPlayers() {
        // (name, username, position, status, handedness, league, draftSeason, totalTpe, bank,
        //  nation, inactive, suspended, ownerUserId). Two owners (5001, 5002) intentionally have a
        // second, later-created player so the recreate flag has coverage in the seed.
        var specs = new (string Name, string User, PlayerPosition Pos, PlayerStatus Status,
            PlayerHandedness Hand, KnownLeague? League, int? Draft, int Tpe, int Bank, string? Nation,
            bool Inactive, bool Suspended, int Owner)[] {
            ("Aaron Frost", "frostbite", PlayerPosition.Center, PlayerStatus.Active,
                PlayerHandedness.Left, KnownLeague.Shl, 70, 1450, 12_500, "Canada", false, false, 5001),
            ("Bella Ridge", "bridge", PlayerPosition.LeftWing, PlayerStatus.Active,
                PlayerHandedness.Left, KnownLeague.Shl, 68, 1620, 3_200, "USA", false, false, 5002),
            ("Cole Vance", "cvance", PlayerPosition.RightWing, PlayerStatus.Active,
                PlayerHandedness.Right, KnownLeague.Shl, 69, 1380, 8_900, "Sweden", false, true, 5003),
            ("Dana Marsh", "dmarsh", PlayerPosition.LeftDefense, PlayerStatus.Active,
                PlayerHandedness.Left, KnownLeague.Shl, 67, 1710, 21_000, "Finland", false, false, 5004),
            ("Eli Barnes", "ebarnes", PlayerPosition.RightDefense, PlayerStatus.Active,
                PlayerHandedness.Right, KnownLeague.Shl, 66, 1555, 500, "Canada", true, false, 5005),
            ("Faye Nolan", "fnolan", PlayerPosition.Goalie, PlayerStatus.Active,
                PlayerHandedness.Left, KnownLeague.Shl, 65, 1490, 15_750, "USA", false, false, 5006),
            ("Gus Holt", "gholt", PlayerPosition.Center, PlayerStatus.Active,
                PlayerHandedness.Right, KnownLeague.Smjhl, 72, 640, 4_100, "Czechia", false, false, 5007),
            ("Hana Vega", "hvega", PlayerPosition.LeftWing, PlayerStatus.Active,
                PlayerHandedness.Left, KnownLeague.Smjhl, 72, 720, 2_050, "Germany", false, false, 5008),
            ("Ivan Pope", "ipope", PlayerPosition.RightWing, PlayerStatus.Active,
                PlayerHandedness.Right, KnownLeague.Smjhl, 71, 810, 9_300, "Russia", false, false, 5009),
            ("Jade Quinn", "jquinn", PlayerPosition.LeftDefense, PlayerStatus.Active,
                PlayerHandedness.Left, KnownLeague.Smjhl, 71, 590, 6_400, "Canada", false, false, 5010),
            ("Kirk Alder", "kalder", PlayerPosition.RightDefense, PlayerStatus.Active,
                PlayerHandedness.Right, KnownLeague.Smjhl, 70, 675, 1_200, "USA", true, false, 5011),
            ("Lena Frost", "lfrost", PlayerPosition.Goalie, PlayerStatus.Active,
                PlayerHandedness.Left, KnownLeague.Smjhl, 72, 700, 3_800, "Switzerland", false, false, 5001),
            ("Milo Reyes", "mreyes", PlayerPosition.Center, PlayerStatus.Retired,
                PlayerHandedness.Left, null, 55, 2_050, 44_000, "Canada", true, false, 5002),
            ("Nora Bly", "nbly", PlayerPosition.LeftWing, PlayerStatus.Retired,
                PlayerHandedness.Right, null, 52, 1_980, 38_500, "USA", true, false, 5012),
            ("Otis Crane", "ocrane", PlayerPosition.RightDefense, PlayerStatus.Retired,
                PlayerHandedness.Right, null, 50, 2_240, 51_200, "Sweden", true, false, 5013),
            ("Priya Sen", "psen", PlayerPosition.Center, PlayerStatus.Pending,
                PlayerHandedness.Right, null, 73, 90, 0, "India", false, false, 5014),
            ("Quinn Diaz", "qdiaz", PlayerPosition.LeftDefense, PlayerStatus.Pending,
                PlayerHandedness.Left, null, 73, 60, 0, "Mexico", false, false, 5015),
            ("Rex Fowler", "rfowler", PlayerPosition.Goalie, PlayerStatus.Denied,
                PlayerHandedness.Left, null, null, 0, 0, null, false, false, 5016),
        };

        var playerId = 1000;
        var created = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // A user's earliest-created player is first-gen; every later player of theirs is a recreate.
        // Creation time increases with array order, so the first spec seen for an owner is earliest.
        var earliestSeen = new HashSet<int>();
        var cards = new List<PlayerCard>();

        foreach (var s in specs) {
            playerId++;
            created = created.AddDays(11);

            var recreate = !earliestSeen.Add(s.Owner);

            cards.Add(new PlayerCard {
                PlayerId = playerId,
                UserId = s.Owner,
                Username = s.User,
                Name = s.Name,
                Status = s.Status,
                Position = s.Pos,
                Handedness = s.Hand,
                CreationDate = created,
                RetirementDate = s.Status == PlayerStatus.Retired ? created.AddYears(2) : null,
                JerseyNumber = (playerId % 98) + 1,
                Weight = 180 + (playerId % 40),
                Birthplace = s.Nation,
                IihfNation = s.Nation,
                DraftSeason = s.Draft,
                IsSuspended = s.Suspended,
                Inactive = s.Inactive,
                Recreate = recreate,
                TotalTpe = s.Tpe,
                AppliedTpe = s.Tpe,
                BankedTpe = 0,
                BankBalance = s.Bank,
                CurrentLeague = s.League,
                CurrentTeamId = s.League is null ? null : 10 + (playerId % 16),
                Attributes = null,
            });
        }

        return cards;
    }
}

/// <summary>A seeded SHL user: their id, username, and Discord name when one is on file.</summary>
public sealed record SeedUser(int UserId, string Username, string? DiscordName);
