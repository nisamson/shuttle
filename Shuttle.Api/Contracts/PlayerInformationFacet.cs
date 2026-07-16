using Facet;
using Shuttle.EFCore.Entities.Portal;
using Shuttle.Models.Players;
using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.Api.Contracts;

/// <summary>
/// Facet-generated projection of the <see cref="PlayerInformation"/> entity. Faceting from the
/// entity gives us a generated constructor (entity -> facet) and LINQ projection with zero hand
/// mapping. The <see cref="PlayerInformation.User"/> navigation and the <c>IndexRecords</c>
/// collection are excluded so the facet stays flat and cheap to materialize.
/// </summary>
[Facet(
    typeof(PlayerInformation),
    exclude: [nameof(PlayerInformation.User), nameof(PlayerInformation.IndexRecords)])]
public partial class PlayerInformationFacet;

/// <summary>
/// A materialized <see cref="PlayerInformation"/> entity paired with its computed
/// <see cref="Recreate"/> flag, produced by <see cref="PlayerCardQueryExtensions.SelectCardRows"/>
/// so the (SQL-computed) recreate value travels with the row into the in-memory card mapping.
/// </summary>
public sealed class PlayerCardRow {
    public required PlayerInformation Player { get; init; }
    public required bool Recreate { get; init; }
}

public static class PlayerCardQueryExtensions {
    /// <summary>
    /// Projects each player alongside a computed "recreate" flag: <see langword="true"/> when the
    /// same user created an earlier player. This is the mirror of the most-recent anti-join in
    /// <c>UpdateMostRecentPlayers</c> (comparing "earlier" instead of "later"), evaluated as a
    /// correlated <c>EXISTS</c> against <paramref name="allPlayers"/>.
    /// </summary>
    public static IQueryable<PlayerCardRow> SelectCardRows(
        this IQueryable<PlayerInformation> source,
        IQueryable<PlayerInformation> allPlayers) =>
        source.Select(p => new PlayerCardRow {
            Player = p,
            Recreate = allPlayers.Any(o =>
                o.UserId == p.UserId
                && (o.CreationTime < p.CreationTime
                    || (o.CreationTime == p.CreationTime && o.PlayerId < p.PlayerId))),
        });

    /// <summary>Maps a single <see cref="PlayerCardRow"/> onto a <see cref="PlayerCard"/>.</summary>
    public static PlayerCard ToPlayerCard(this PlayerCardRow row) =>
        new PlayerInformationFacet(row.Player).ToPlayerCard() with { Recreate = row.Recreate };

    /// <summary>Maps a sequence of <see cref="PlayerCardRow"/>s onto a list of cards.</summary>
    public static List<PlayerCard> ToPlayerCards(this IEnumerable<PlayerCardRow> rows) =>
        rows.Select(row => row.ToPlayerCard()).ToList();
}

public static class PlayerInformationFacetExtensions {
    /// <summary>
    /// Maps the internal facet onto the lean, shared <see cref="PlayerCard"/> wire contract,
    /// collapsing the mutually-exclusive skater/goaltender attribute properties into the single
    /// polymorphic <see cref="PlayerCard.Attributes"/> value.
    /// </summary>
    public static PlayerCard ToPlayerCard(this PlayerInformationFacet f) =>
        new() {
            PlayerId = f.PlayerId,
            UserId = f.UserId,
            Username = f.Username,
            Name = f.Name,
            Status = f.Status,
            Position = f.Position,
            Handedness = f.Handedness,
            CreationDate = f.CreationTime,
            RetirementDate = f.RetirementDate,
            JerseyNumber = f.JerseyNumber,
            Height = f.Height,
            Weight = f.Weight,
            Birthplace = f.Birthplace,
            IihfNation = f.IihfNation,
            DraftSeason = f.DraftSeason,
            IsSuspended = f.IsSuspended,
            Inactive = f.Inactive,
            TotalTpe = f.TotalTpe,
            AppliedTpe = f.AppliedTpe,
            BankedTpe = f.BankedTpe,
            BankBalance = f.BankBalance,
            CurrentLeague = f.CurrentLeague,
            CurrentTeamId = f.CurrentTeamId,
            ShlRightsTeamId = f.ShlRightsTeamId,
            SmjhlRightsTeamId = f.SmjhlRightsTeamId,
            Attributes = (PlayerAttributes?)f.SkaterAttributes ?? f.GoaltenderAttributes,
        };
}
