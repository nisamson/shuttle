using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Shl.Api.Models.Portal.V1;

namespace Shuttle.EFCore.Entities.Portal;

/// <summary>
/// A player's TPE-earning summary for a single season, sourced from the portal
/// <c>GET /analytics/earned-tpe</c> endpoint. Records the total TPE the player earned in
/// <see cref="Season"/> along with a breakdown by source. Identity and current-state fields returned
/// by the endpoint (name, username, position, current league/team, rights team, status, draft season)
/// are intentionally omitted here since they are already tracked on <see cref="PlayerInformation"/>.
/// The endpoint's per-season <c>rank</c> is also omitted as it is derivable from <see cref="EarnedTpe"/>.
/// </summary>
[EntityTypeConfiguration(typeof(PlayerEarnedTpeConfiguration))]
public class PlayerEarnedTpe {
    /// <summary>The id of the player this summary belongs to.</summary>
    public required int PlayerId { get; set; }

    /// <summary>The season the TPE was earned in.</summary>
    public required int Season { get; set; }

    /// <summary>The total TPE the player earned in <see cref="Season"/>.</summary>
    public required int EarnedTpe { get; set; }

    /// <summary>TPE lost to regression this season (0 when none).</summary>
    public required int Regression { get; set; }

    /// <summary>TPE earned from activity checks this season (0 when none).</summary>
    public required int ActivityCheck { get; set; }

    /// <summary>TPE earned from training this season (0 when none).</summary>
    public required int Training { get; set; }

    /// <summary>TPE earned from training camp this season (0 when none).</summary>
    public required int TrainingCamp { get; set; }

    /// <summary>TPE earned from coaching this season (0 when none).</summary>
    public required int Coaching { get; set; }

    /// <summary>TPE earned from point tasks (PTs) this season (0 when none).</summary>
    public required int Pt { get; set; }

    /// <summary>TPE earned from fantasy this season (0 when none).</summary>
    public required int Fantasy { get; set; }

    /// <summary>TPE earned from recruitment this season (0 when none).</summary>
    public required int Recruitment { get; set; }

    /// <summary>TPE adjustments from corrections this season (0 when none).</summary>
    public required int Correction { get; set; }

    /// <summary>TPE earned from other sources this season (0 when none).</summary>
    public required int Other { get; set; }

    /// <summary>The player this summary belongs to.</summary>
    public PlayerInformation Player { get; set; } = null!;

    /// <summary>
    /// Builds a <see cref="PlayerEarnedTpe"/> from a portal earned-TPE entry. The entry's identity and
    /// current-state fields are discarded (tracked on <see cref="PlayerInformation"/>), and each
    /// nullable breakdown value is coalesced to <c>0</c> ("none earned from this source").
    /// </summary>
    public static PlayerEarnedTpe FromShlApi(EarnedTpeEntry entry) {
        return new() {
            PlayerId = entry.PlayerUpdateId,
            Season = entry.Season,
            EarnedTpe = entry.EarnedTpe,
            Regression = entry.Regression ?? 0,
            ActivityCheck = entry.ActivityCheck ?? 0,
            Training = entry.Training ?? 0,
            TrainingCamp = entry.TrainingCamp ?? 0,
            Coaching = entry.Coaching ?? 0,
            Pt = entry.Pt ?? 0,
            Fantasy = entry.Fantasy ?? 0,
            Recruitment = entry.Recruitment ?? 0,
            Correction = entry.Correction ?? 0,
            Other = entry.Other ?? 0,
        };
    }
}

public class PlayerEarnedTpeConfiguration : IEntityTypeConfiguration<PlayerEarnedTpe> {
    public void Configure(EntityTypeBuilder<PlayerEarnedTpe> builder) {
        builder.HasKey(e => new { e.PlayerId, e.Season });
        builder.HasOne(e => e.Player)
            .WithMany(p => p.EarnedTpe)
            .HasForeignKey(e => e.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
