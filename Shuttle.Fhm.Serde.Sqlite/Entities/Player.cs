using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Common.Mixins;
using Shuttle.Fhm.Serde.Domain.Model;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Shuttle.Fhm.Serde.Sqlite.Entities;

/// <summary>One editable players.dat profile record.</summary>
public sealed class Player
{
    /// <summary>Gets or sets the serialized record ordinal.</summary>
    public int RecordOrdinal { get; set; }
    /// <summary>Gets or sets the persisted player internal identity.</summary>
    public int InternalId { get; set; }
    /// <summary>Gets or sets the exported player identity.</summary>
    public int ExternalId { get; set; }
    /// <summary>Gets or sets the first-name reference.</summary>
    public int? FirstNameId { get; set; }
    /// <summary>Gets or sets the surname reference.</summary>
    public int? SurnameId { get; set; }
    /// <summary>Gets or sets the common-name reference.</summary>
    public int? CommonNameId { get; set; }
    /// <summary>Gets or sets the player's first-name entity.</summary>
    public Name? FirstName { get; set; }
    /// <summary>Gets or sets the player's surname entity.</summary>
    public Name? Surname { get; set; }
    /// <summary>Gets or sets the player's common-name entity.</summary>
    public Name? CommonName { get; set; }
    /// <summary>Gets the player's first-name text.</summary>
    [NotNullIfNotNull(nameof(FirstName))]
    public string? FirstNameText => FirstName?.Text;
    /// <summary>Gets the player's surname text.</summary>
    [NotNullIfNotNull(nameof(Surname))]
    public string? SurnameText => Surname?.Text;
    /// <summary>Gets the player's common-name text.</summary>
    [NotNullIfNotNull(nameof(CommonName))]
    public string? CommonNameText => CommonName?.Text;
    /// <summary>Gets or sets the player's birth date.</summary>
    public DateOnly BirthDate { get; set; }
    /// <summary>Gets or sets the team reference.</summary>
    public int? TeamRecordOrdinal { get; set; }
    /// <summary>Gets or sets the player's current team.</summary>
    public Team? Team { get; set; }
    /// <summary>Gets or sets the franchise reference.</summary>
    public int? FranchiseId { get; set; }
    /// <summary>Gets or sets the player's position affinities.</summary>
    public PlayerPositionAffinity PositionAffinity { get; set; } = new();
    /// <summary>Gets the player's highest-rated position.</summary>
    public PlayerPosition PrimaryPosition => PositionAffinity.PrimaryPosition;
    /// <summary>Gets or sets the player's rating attributes.</summary>
    public PlayerAttributes Attributes { get; set; } = null!;
    /// <summary>Gets or sets the player's primary contract playing role.</summary>
    public FhmPlayingRole PrimaryContractRole { get; set; }
    /// <summary>Gets or sets the player's supplementary contract squad status.</summary>
    public FhmSquadStatus SupplementaryContractRole { get; set; }
    /// <summary>Gets the player's ordered contracts.</summary>
    public ICollection<PlayerContract> Contracts { get; } = [];
    /// <summary>Gets the player's primary and supplementary tactical role assignments.</summary>
    public ICollection<PlayerRoleAssignment> TacticalRoleAssignments { get; } = [];
    /// <summary>Gets or sets the opaque original player record framing.</summary>
    public byte[] SerializedRecord { get; set; } = [];

}

/// <summary>The player's ratings for each playable position.</summary>
public sealed class PlayerPositionAffinity
{
    private static readonly PlayerPosition[] OrderedPositions =
    [
        PlayerPosition.Goalie,
        PlayerPosition.LeftDefense,
        PlayerPosition.RightDefense,
        PlayerPosition.LeftWing,
        PlayerPosition.Center,
        PlayerPosition.RightWing,
    ];

    [Range(0, 20)]
    public int Goalie
    {
        get;
        set => field = ValidateRating(value, nameof(Goalie));
    }

    [Range(0, 20)]
    public int LeftDefense
    {
        get;
        set => field = ValidateRating(value, nameof(LeftDefense));
    }

    [Range(0, 20)]
    public int RightDefense
    {
        get;
        set => field = ValidateRating(value, nameof(RightDefense));
    }

    [Range(0, 20)]
    public int LeftWing
    {
        get;
        set => field = ValidateRating(value, nameof(LeftWing));
    }

    [Range(0, 20)]
    public int Center
    {
        get;
        set => field = ValidateRating(value, nameof(Center));
    }

    [Range(0, 20)]
    public int RightWing
    {
        get;
        set => field = ValidateRating(value, nameof(RightWing));
    }

    public PlayerPosition PrimaryPosition => OrderedPositions.MaxBy(GetRating);

    public int GetRating(PlayerPosition position) => position switch
    {
        PlayerPosition.Goalie => Goalie,
        PlayerPosition.Center => Center,
        PlayerPosition.LeftWing => LeftWing,
        PlayerPosition.RightWing => RightWing,
        PlayerPosition.LeftDefense => LeftDefense,
        PlayerPosition.RightDefense => RightDefense,
        _ => throw new ArgumentOutOfRangeException(nameof(position), position, "Must specify a single unambiguous position."),
    };

    public void SetPrimaryPosition(PlayerPosition position)
    {
        _ = GetRating(position);
        Goalie = position == PlayerPosition.Goalie ? 20 : 0;
        LeftDefense = position == PlayerPosition.LeftDefense ? 20 : 0;
        RightDefense = position == PlayerPosition.RightDefense ? 20 : 0;
        LeftWing = position == PlayerPosition.LeftWing ? 20 : 0;
        Center = position == PlayerPosition.Center ? 20 : 0;
        RightWing = position == PlayerPosition.RightWing ? 20 : 0;
    }

    private static int ValidateRating(int value, string parameterName) =>
        value is >= 0 and <= 20
            ? value
            : throw new ArgumentOutOfRangeException(parameterName, value, "Position ratings must be between 0 and 20.");
}

/// <summary>The editable 58-value player-rating vector.</summary>
public sealed class PlayerAttributes : IGoaltenderRatings, IHiddenAttributes, ISkaterRatings
{
    /// <summary>Gets or sets the owning player record ordinal.</summary>
    public int PlayerId { get; set; }
    public int BigGames { get; set; }
    public int Consistency { get; set; }
    public int Greed { get; set; }
    public int Adaptability { get; set; }
    public int Loyalty { get; set; }
    public int Coachability { get; set; }
    public int Aging { get; set; }
    public int Sportsmanship { get; set; }
    public int PassShootTendency { get; set; }
    public int Controversy { get; set; }
    public int HandleCritics { get; set; }
    public int HandleFailure { get; set; }
    public int HandleSuccess { get; set; }
    public int Intelligence { get; set; }
    public int Mood { get; set; }
    public int DevelopmentRate { get; set; }
    public int Aggression { get; set; }
    public int Bravery { get; set; }
    public int Determination { get; set; }
    public int TeamPlayer { get; set; }
    public int Leadership { get; set; }
    public int Temperament { get; set; }
    public int Professionalism { get; set; }
    public int Ambition { get; set; }
    public int Acceleration { get; set; }
    public int Agility { get; set; }
    public int Balance { get; set; }
    public int Speed { get; set; }
    public int Stamina { get; set; }
    public int Strength { get; set; }
    public int Fighting { get; set; }
    public int Reflexes { get; set; }
    public int GoalieStamina { get; set; }
    public int Screening { get; set; }
    public int GettingOpen { get; set; }
    public int Passing { get; set; }
    public int Puckhandling { get; set; }
    public int ShootingAccuracy { get; set; }
    public int ShootingRange { get; set; }
    public int OffensiveRead { get; set; }
    public int Checking { get; set; }
    public int Faceoffs { get; set; }
    public int Hitting { get; set; }
    public int Positioning { get; set; }
    public int ShotBlocking { get; set; }
    public int Stickchecking { get; set; }
    public int DefensiveRead { get; set; }
    public int GoaliePositioning { get; set; }
    public int GoaliePassing { get; set; }
    public int GoaliePokeCheck { get; set; }
    public int GoalieBlocker { get; set; }
    public int GoalieGlove { get; set; }
    public int GoalieRebound { get; set; }
    public int GoalieRecovery { get; set; }
    public int GoaliePuckhandling { get; set; }
    public int GoalieLowShots { get; set; }
    public int MentalToughness { get; set; }
    public int Skating { get; set; }

    int IGoaltendingMentalRatings.Stamina => GoalieStamina;
    int IGoaltendingMentalRatings.Aggression => Aggression;
    int IGoaltendingMentalRatings.Determination => Determination;
    int IGoaltendingMentalRatings.TeamPlayer => TeamPlayer;
    int IGoaltendingMentalRatings.Leadership => Leadership;
    int IGoaltendingMentalRatings.Professionalism => Professionalism;
    int IGoaltendingMentalRatings.MentalToughness => MentalToughness;
    int IGoaltendingTechniqueRatings.Positioning => GoaliePositioning;
    int IGoaltendingTechniqueRatings.Passing => GoaliePassing;
    int IGoaltendingTechniqueRatings.PokeCheck => GoaliePokeCheck;
    int IGoaltendingTechniqueRatings.Blocker => GoalieBlocker;
    int IGoaltendingTechniqueRatings.Glove => GoalieGlove;
    int IGoaltendingTechniqueRatings.Rebound => GoalieRebound;
    int IGoaltendingTechniqueRatings.Recovery => GoalieRecovery;
    int IGoaltendingTechniqueRatings.Puckhandling => GoaliePuckhandling;
    int IGoaltendingTechniqueRatings.LowShots => GoalieLowShots;
    int IGoaltendingTechniqueRatings.Skating => Skating;
    int IGoaltendingTechniqueRatings.Reflexes => Reflexes;

    /// <summary>Gets or sets the owning player profile.</summary>
    public Player Player { get; set; } = null!;
}

public sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Players");
        builder.HasKey(value => value.InternalId);
        builder.HasIndex(value => value.RecordOrdinal).IsUnique();
        builder.Property(value => value.InternalId).ValueGeneratedNever();
        builder.Property(value => value.RecordOrdinal).ValueGeneratedNever();
        builder.Property(value => value.SerializedRecord).IsRequired();
        builder.Ignore(value => value.FirstNameText);
        builder.Ignore(value => value.SurnameText);
        builder.Ignore(value => value.CommonNameText);
        builder.Ignore(value => value.PrimaryPosition);
        builder.OwnsOne(value => value.PositionAffinity, affinity =>
        {
            affinity.Ignore(value => value.PrimaryPosition);
            affinity.Property(value => value.Goalie).HasColumnName("Goalie");
            affinity.Property(value => value.LeftDefense).HasColumnName("LeftDefense");
            affinity.Property(value => value.RightDefense).HasColumnName("RightDefense");
            affinity.Property(value => value.LeftWing).HasColumnName("LeftWing");
            affinity.Property(value => value.Center).HasColumnName("Center");
            affinity.Property(value => value.RightWing).HasColumnName("RightWing");
        });
        builder.Property(value => value.PrimaryContractRole).HasConversion<int>();
        builder.Property(value => value.SupplementaryContractRole).HasConversion<int>();
        builder.HasOne(value => value.Attributes)
            .WithOne(value => value.Player)
            .HasForeignKey<PlayerAttributes>(value => value.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(value => value.FirstName)
            .WithMany()
            .HasForeignKey(value => value.FirstNameId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(value => value.Surname)
            .WithMany()
            .HasForeignKey(value => value.SurnameId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(value => value.CommonName)
            .WithMany()
            .HasForeignKey(value => value.CommonNameId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(value => value.Team)
            .WithMany(value => value.Players)
            .HasForeignKey(value => value.TeamRecordOrdinal)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Navigation(value => value.FirstName).AutoInclude();
        builder.Navigation(value => value.Surname).AutoInclude();
        builder.Navigation(value => value.CommonName).AutoInclude();
        builder.Navigation(value => value.Team).AutoInclude();
        builder.Navigation(value => value.Attributes).AutoInclude();
        builder.Navigation(value => value.Contracts).AutoInclude();
        builder.Navigation(value => value.TacticalRoleAssignments).AutoInclude();
    }
}

public sealed class PlayerAttributesConfiguration : IEntityTypeConfiguration<PlayerAttributes>
{
    public void Configure(EntityTypeBuilder<PlayerAttributes> builder)
    {
        builder.ToTable("PlayerAttributes");
        builder.HasKey(value => value.PlayerId);
        builder.Property(value => value.PlayerId).ValueGeneratedNever();
    }
}
