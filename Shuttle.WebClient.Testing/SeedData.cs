using Shuttle.Models.Players;
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

    private static IEnumerable<PlayerCard> BuildPlayers() {
        // (name, username, position, status, handedness, league, draftSeason, totalTpe, bank,
        //  nation, inactive, suspended)
        var specs = new (string Name, string User, PlayerPosition Pos, PlayerStatus Status,
            PlayerHandedness Hand, KnownLeague? League, int? Draft, int Tpe, int Bank, string? Nation,
            bool Inactive, bool Suspended)[] {
            ("Aaron Frost", "frostbite", PlayerPosition.Center, PlayerStatus.Active,
                PlayerHandedness.Left, KnownLeague.Shl, 70, 1450, 12_500, "Canada", false, false),
            ("Bella Ridge", "bridge", PlayerPosition.LeftWing, PlayerStatus.Active,
                PlayerHandedness.Left, KnownLeague.Shl, 68, 1620, 3_200, "USA", false, false),
            ("Cole Vance", "cvance", PlayerPosition.RightWing, PlayerStatus.Active,
                PlayerHandedness.Right, KnownLeague.Shl, 69, 1380, 8_900, "Sweden", false, true),
            ("Dana Marsh", "dmarsh", PlayerPosition.LeftDefense, PlayerStatus.Active,
                PlayerHandedness.Left, KnownLeague.Shl, 67, 1710, 21_000, "Finland", false, false),
            ("Eli Barnes", "ebarnes", PlayerPosition.RightDefense, PlayerStatus.Active,
                PlayerHandedness.Right, KnownLeague.Shl, 66, 1555, 500, "Canada", true, false),
            ("Faye Nolan", "fnolan", PlayerPosition.Goalie, PlayerStatus.Active,
                PlayerHandedness.Left, KnownLeague.Shl, 65, 1490, 15_750, "USA", false, false),
            ("Gus Holt", "gholt", PlayerPosition.Center, PlayerStatus.Active,
                PlayerHandedness.Right, KnownLeague.Smjhl, 72, 640, 4_100, "Czechia", false, false),
            ("Hana Vega", "hvega", PlayerPosition.LeftWing, PlayerStatus.Active,
                PlayerHandedness.Left, KnownLeague.Smjhl, 72, 720, 2_050, "Germany", false, false),
            ("Ivan Pope", "ipope", PlayerPosition.RightWing, PlayerStatus.Active,
                PlayerHandedness.Right, KnownLeague.Smjhl, 71, 810, 9_300, "Russia", false, false),
            ("Jade Quinn", "jquinn", PlayerPosition.LeftDefense, PlayerStatus.Active,
                PlayerHandedness.Left, KnownLeague.Smjhl, 71, 590, 6_400, "Canada", false, false),
            ("Kirk Alder", "kalder", PlayerPosition.RightDefense, PlayerStatus.Active,
                PlayerHandedness.Right, KnownLeague.Smjhl, 70, 675, 1_200, "USA", true, false),
            ("Lena Frost", "lfrost", PlayerPosition.Goalie, PlayerStatus.Active,
                PlayerHandedness.Left, KnownLeague.Smjhl, 72, 700, 3_800, "Switzerland", false, false),
            ("Milo Reyes", "mreyes", PlayerPosition.Center, PlayerStatus.Retired,
                PlayerHandedness.Left, null, 55, 2_050, 44_000, "Canada", true, false),
            ("Nora Bly", "nbly", PlayerPosition.LeftWing, PlayerStatus.Retired,
                PlayerHandedness.Right, null, 52, 1_980, 38_500, "USA", true, false),
            ("Otis Crane", "ocrane", PlayerPosition.RightDefense, PlayerStatus.Retired,
                PlayerHandedness.Right, null, 50, 2_240, 51_200, "Sweden", true, false),
            ("Priya Sen", "psen", PlayerPosition.Center, PlayerStatus.Pending,
                PlayerHandedness.Right, null, 73, 90, 0, "India", false, false),
            ("Quinn Diaz", "qdiaz", PlayerPosition.LeftDefense, PlayerStatus.Pending,
                PlayerHandedness.Left, null, 73, 60, 0, "Mexico", false, false),
            ("Rex Fowler", "rfowler", PlayerPosition.Goalie, PlayerStatus.Denied,
                PlayerHandedness.Left, null, null, 0, 0, null, false, false),
        };

        var playerId = 1000;
        var userId = 5000;
        var created = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        foreach (var s in specs) {
            playerId++;
            userId++;
            created = created.AddDays(11);

            yield return new PlayerCard {
                PlayerId = playerId,
                UserId = userId,
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
                TotalTpe = s.Tpe,
                AppliedTpe = s.Tpe,
                BankedTpe = 0,
                BankBalance = s.Bank,
                CurrentLeague = s.League,
                CurrentTeamId = s.League is null ? null : 10 + (playerId % 16),
                Attributes = null,
            };
        }
    }
}
