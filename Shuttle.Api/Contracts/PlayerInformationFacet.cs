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
