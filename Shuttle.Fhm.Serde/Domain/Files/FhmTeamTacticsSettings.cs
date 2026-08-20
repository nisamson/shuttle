using Shuttle.Fhm.Serde.Domain.Model;
using Shuttle.Fhm.Serde.Domain.Binary;
using Shuttle.Fhm.Serde.Wire.Teams;

namespace Shuttle.Fhm.Serde.Domain.Files;

/// <summary>The 1,303-byte team-owned tactical settings region within a team record.</summary>
public sealed class FhmTeamTacticsSettings
{
    private const int WireLength = 1303;

    /// <summary>Gets or sets the first team-owned tactical scalar.</summary>
    public ushort TeamValue1 { get; set; }
    /// <summary>Gets or sets the team-owned tactical flag.</summary>
    public byte TeamFlag { get; set; }
    /// <summary>Gets or sets the team-owned tactical rating.</summary>
    public byte TeamRating { get; set; }
    /// <summary>Gets or sets the three team-owned tactical scalars.</summary>
    public int[] TeamValues2To4 { get; } = new int[3];
    /// <summary>Gets or sets the nested tactics-object version.</summary>
    public int TacticsObjectVersion { get; set; }
    /// <summary>Gets 59 documented but not individually named base settings.</summary>
    public ushort[] BaseSettings { get; } = new ushort[59];
    /// <summary>Gets selector blocks: global at index zero followed by the 21 unit blocks.</summary>
    public IList<FhmZoneSelectorBlock> Selectors { get; } =
        Enumerable.Range(0, 22).Select(index => new FhmZoneSelectorBlock(index)).ToList();
    /// <summary>Gets or sets the final offensive orientation raw value.</summary>
    public FhmEnumValue<FhmOffensiveOrientation> FinalOffensiveOrientation { get; set; }
    /// <summary>Gets or sets the final physical orientation raw value.</summary>
    public FhmEnumValue<FhmPhysicalOrientation> FinalPhysicalOrientation { get; set; }
    /// <summary>Gets the two final use-own-settings flags for selector blocks 20 and 21.</summary>
    public byte[] FinalUseOwnSettingsFlags { get; } = new byte[2];
    /// <summary>Gets the 22 tendency blocks in selector order.</summary>
    public IList<FhmTendencyBlock> Tendencies { get; } = Enumerable.Range(0, 22).Select(_ => new FhmTendencyBlock()).ToList();

    /// <summary>Reads the exact team-tactics settings region.</summary>
    public static FhmTeamTacticsSettings ReadFrom(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        try
        {
            return FhmTeamWireMapper.FromWire(FhmTeamsFileSerializer.DeserializeSettings(stream));
        }
        catch (InvalidDataException exception)
        {
            throw new FhmFormatException(exception.Message);
        }
    }

    /// <summary>Writes the exact team-tactics settings region in FHM wire order.</summary>
    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var serialized = new MemoryStream();
        FhmTeamsFileSerializer.SerializeSettings(serialized, FhmTeamWireMapper.ToWire(this));
        if (serialized.Length != WireLength)
        {
            throw new FhmFormatException($"Team tactics must serialize to exactly {WireLength} bytes.");
        }

        serialized.Position = 0;
        serialized.CopyTo(stream);
    }
}

/// <summary>A per-unit zone-system selection block.</summary>
public sealed class FhmZoneSelectorBlock
{
    /// <summary>Initializes a selector block at its documented ordinal.</summary>
    public FhmZoneSelectorBlock(int blockIndex)
    {
        if (blockIndex is < 0 or > 21)
        {
            throw new ArgumentOutOfRangeException(nameof(blockIndex));
        }

        BlockIndex = blockIndex;
        DelayedUseOwnSettingsFlags = new byte[GetDelayedFlagCount(blockIndex)];
    }

    /// <summary>Gets the global-or-unit selector ordinal.</summary>
    public int BlockIndex { get; }
    /// <summary>Gets twelve selected team-tactics catalogue system ids.</summary>
    public ushort[] SystemIds { get; } = new ushort[12];
    /// <summary>Gets or sets the unit orientation, except in the final selector block.</summary>
    public FhmEnumValue<FhmOffensiveOrientation>? OffensiveOrientation { get; set; }
    /// <summary>Gets or sets the unit orientation, except in the final selector block.</summary>
    public FhmEnumValue<FhmPhysicalOrientation>? PhysicalOrientation { get; set; }
    /// <summary>Gets delayed use-own-settings flags emitted after specific selector groups.</summary>
    public byte[] DelayedUseOwnSettingsFlags { get; }

    internal static int GetDelayedFlagCount(int blockIndex) => blockIndex switch
    {
        4 => 4,
        13 => 3,
        6 or 8 or 10 or 15 or 17 or 19 => 2,
        _ => 0,
    };
}

/// <summary>Eight tactical tendency values and their per-unit override flags.</summary>
public sealed class FhmTendencyBlock
{
    /// <summary>Gets tendency values ordered Aggressiveness through Tempo.</summary>
    public ushort[] Values { get; } = new ushort[8];
    /// <summary>Gets matching override flags ordered Aggressiveness through Tempo.</summary>
    public byte[] Overrides { get; } = new byte[8];
}
