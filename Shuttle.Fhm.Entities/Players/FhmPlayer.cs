using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shuttle.Fhm.Entities.Names;
using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.SaveData.Files;
using Shuttle.Fhm.SaveData.Model;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Index.V1;

namespace Shuttle.Fhm.Entities.Players;

public sealed class FhmPlayer {
    /// <summary>
    /// The internal FHM identity of the player, which is the primary key of the entity. This value
    /// is used for correlation with other FHM entities. It is expected to equal the serialized
    /// <c>players.dat</c> record ordinal; callers can inspect that invariant on <see cref="FhmPlayersFile"/>.
    /// </summary>
    public required int InternalId { get; init; }

    /// <summary>
    /// The external FHM identity of the player, which is exported to the
    /// </summary>
    public required int ExternalId { get; init; }

    public FhmName? First { get; set; }
    public FhmName? Last { get; set; }
    public FhmName? Nick { get; set; }

    [NotNullIfNotNull(nameof(First))] public string? FirstName => First?.Name;
    [NotNullIfNotNull(nameof(Last))] public string? LastName => Last?.Name;
    [NotNullIfNotNull(nameof(Nick))] public string? NickName => Nick?.Name;

    public DateOnly BirthDate { get; set; }
    public required FhmPositionAffinity PositionAffinity { get; set; }

    public PlayerPosition Position => PositionAffinity.PrimaryPosition;
    
    public FhmPlayerOpaqueData OpaqueData { get; set; } = null!;
    public FhmPlayerAttributes Attributes { get; set; } = null!;

    public static FhmPlayer FromSave(
        FhmPlayerRecord source,
        IReadOnlyDictionary<int, FhmName> names)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(names);

        if (source.InternalIdentity == FhmNullConstants.Null)
        {
            throw new ArgumentException("An empty player record cannot be mapped to an entity.", nameof(source));
        }

        var internalId = source.InternalIdentity;
        var attributes = FhmPlayerAttributes.FromSave(source.RatingAttributes);
        attributes.InternalId = internalId;

        return new FhmPlayer {
            ExternalId = source.ExportedPlayerId,
            InternalId = internalId,
            First = ResolveName(source.FirstNameId, names, nameof(source.FirstNameId)),
            Last = ResolveName(source.SurnameId, names, nameof(source.SurnameId)),
            Nick = ResolveName(source.CommonNameId, names, nameof(source.CommonNameId)),
            BirthDate = ToDateOnly(source.BirthDate, nameof(source.BirthDate)),
            PositionAffinity = FhmPositionAffinity.FromSave(source.PositionRatings),
            Attributes = attributes,
            OpaqueData = FhmPlayerOpaqueData.FromSave(source),
        };
    }

    public static FhmPlayer FromSave(FhmPlayerRecord source, FhmNamesFile names)
    {
        ArgumentNullException.ThrowIfNull(names);
        var lookup = names.MasterNames
            .Where(entry => entry.Text is not null)
            .ToDictionary(
                entry => entry.NameId,
                entry => new FhmName(
                    entry.NameId,
                    entry.Text!,
                    entry.GroupId,
                    unchecked((ushort)entry.CategoryWeight),
                    entry.FlagA != 0,
                    entry.FlagB != 0,
                    entry.FlagC != 0));
        return FromSave(source, lookup);
    }

    private static FhmName? ResolveName(
        int nameId,
        IReadOnlyDictionary<int, FhmName> names,
        string parameterName)
    {
        if (nameId == FhmNullConstants.Null)
        {
            return null;
        }

        return names.TryGetValue(nameId, out var name)
            ? name
            : throw new ArgumentException($"Name id {nameId} was not found in names.dat.", parameterName);
    }

    private static DateOnly ToDateOnly(FhmDate date, string parameterName)
    {
        try
        {
            return new DateOnly(date.Year, date.Month, date.Day);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new ArgumentException(
                $"FHM date {date.Year:D4}-{date.Month:D2}-{date.Day:D2} is invalid.",
                parameterName,
                exception);
        }
    }
}

public sealed class FhmPositionAffinity {
    private static readonly PlayerPosition[] OrderedPositions = [
        PlayerPosition.Goalie,
        PlayerPosition.LeftDefense,
        PlayerPosition.RightDefense,
        PlayerPosition.LeftWing,
        PlayerPosition.Center,
        PlayerPosition.RightWing,
    ];

    public PlayerPosition PrimaryPosition => OrderedPositions.MaxBy(PositionAffinity);

    public static FhmPositionAffinity FromSave(FhmPlayerPositionRatings source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.RawValues.Count != 12)
        {
            throw new ArgumentException(
                $"Expected 12 position-rating values, but found {source.RawValues.Count}.",
                nameof(source));
        }

        return new FhmPositionAffinity {
            Goalie = ToRating(source.Goalie, nameof(source.Goalie)),
            LeftDefense = ToRating(source.LeftDefenceman, nameof(source.LeftDefenceman)),
            RightDefense = ToRating(source.RightDefenceman, nameof(source.RightDefenceman)),
            LeftWing = ToRating(source.LeftWing, nameof(source.LeftWing)),
            Center = ToRating(source.Centre, nameof(source.Centre)),
            RightWing = ToRating(source.RightWing, nameof(source.RightWing)),
        };
    }

    private static int ToRating(ushort value, string parameterName)
    {
        return value <= 20
            ? value
            : throw new ArgumentOutOfRangeException(parameterName, value, "FHM position ratings must be between 0 and 20.");
    }

    public int PositionAffinity(PlayerPosition position) {
        switch (position) {
            case PlayerPosition.Goalie:
                return Goalie;
            case PlayerPosition.Center:
                return Center;
            case PlayerPosition.LeftWing:
                return LeftWing;
            case PlayerPosition.RightWing:
                return RightWing;
            case PlayerPosition.LeftDefense:
                return LeftDefense;
            case PlayerPosition.RightDefense:
                return RightDefense;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(position),
                    position,
                    "Must specify a single unambiguous position."
                );
        }
    }

    [Range(0, 20)]
    public int Goalie {
        get;
        set => field = ValidateRating(value, nameof(Goalie));
    }

    [Range(0, 20)]
    public int LeftWing {
        get;
        set => field = ValidateRating(value, nameof(LeftWing));
    }

    [Range(0, 20)]
    public int RightWing {
        get;
        set => field = ValidateRating(value, nameof(RightWing));
    }

    [Range(0, 20)]
    public int Center {
        get;
        set => field = ValidateRating(value, nameof(Center));
    }

    [Range(0, 20)]
    public int RightDefense {
        get;
        set => field = ValidateRating(value, nameof(RightDefense));
    }

    [Range(0, 20)]
    public int LeftDefense {
        get;
        set => field = ValidateRating(value, nameof(LeftDefense));
    }

    private static int ValidateRating(int value, string parameterName) =>
        value is >= 0 and <= 20
            ? value
            : throw new ArgumentOutOfRangeException(parameterName, value, "Position ratings must be between 0 and 20.");
}


public sealed class FhmPlayerOpaqueData {
    public int PlayerId { get; set; }
    public byte[] SerializedRecord { get; set; } = [];

    public static FhmPlayerOpaqueData FromSave(FhmPlayerRecord source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new FhmPlayerOpaqueData {
            PlayerId = source.InternalIdentity,
            SerializedRecord = source.ToBytes(),
        };
    }

    public FhmPlayerRecord ToSaveRecord()
    {
        if (SerializedRecord.Length == 0)
        {
            throw new InvalidOperationException("Opaque player data does not contain a serialized player record.");
        }

        return FhmPlayerRecord.FromBytes(SerializedRecord);
    }
}

public sealed class FhmPlayerOpaqueDataEntityConfiguration : IEntityTypeConfiguration<FhmPlayerOpaqueData>
{
    public void Configure(EntityTypeBuilder<FhmPlayerOpaqueData> builder)
    {
        builder.HasKey(data => data.PlayerId);
        builder.Property(data => data.PlayerId).ValueGeneratedNever();
        builder.Property(data => data.SerializedRecord).IsRequired();
    }
}

public sealed class FhmPlayerEntityConfiguration : IEntityTypeConfiguration<FhmPlayer> {

    public void Configure(EntityTypeBuilder<FhmPlayer> builder) {
        builder.Property(p => p.InternalId)
            .IsRequired()
            .ValueGeneratedNever();
        builder.HasKey(p => p.InternalId);
        builder.Ignore(p => p.Position);
        builder.Ignore(p => p.FirstName);
        builder.Ignore(p => p.LastName);
        builder.Ignore(p => p.NickName);
        
        builder.HasOne(p => p.First)
            .WithMany()
            .OnDelete(DeleteBehavior.SetNull);
        builder.Navigation(p => p.First)
            .AutoInclude();
        
        builder.HasOne(p => p.Last)
            .WithMany()
            .OnDelete(DeleteBehavior.SetNull);
        builder.Navigation(p => p.Last)
            .AutoInclude();
        
        builder.HasOne(p => p.Nick)
            .WithMany()
            .OnDelete(DeleteBehavior.SetNull);
        builder.Navigation(p => p.Nick)
            .AutoInclude();
        
        builder.HasOne(p => p.OpaqueData)
            .WithOne()
            .HasForeignKey<FhmPlayerOpaqueData>(data => data.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(p => p.Attributes)
            .WithOne()
            .HasForeignKey<FhmPlayerAttributes>(attributes => attributes.InternalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(p => p.PositionAffinity, affinity => {
            affinity.Ignore(value => value.PrimaryPosition);
            affinity.Property(value => value.Goalie).IsRequired();
            affinity.Property(value => value.LeftWing).IsRequired();
            affinity.Property(value => value.RightWing).IsRequired();
            affinity.Property(value => value.Center).IsRequired();
            affinity.Property(value => value.RightDefense).IsRequired();
            affinity.Property(value => value.LeftDefense).IsRequired();
        });
    }
}
