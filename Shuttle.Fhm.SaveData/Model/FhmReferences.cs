namespace Shuttle.Fhm.SaveData.Model;

/// <summary>Serialized FHM identity and sentinel values. These values are references, not database keys.</summary>
public static class FhmReferences
{
    /// <summary>Empty player-line-slot sentinel.</summary>
    public const int EmptyPlayer = -1;

    /// <summary>Unset FHM nation-index sentinel.</summary>
    public const ushort UnsetNationIndex = 9999;

    /// <summary>Unlimited or disabled open-roster-slots sentinel.</summary>
    public const int UnlimitedOpenRosterSlots = 999;
}

/// <summary>A players.dat internal identity used by line slots and roster references.</summary>
public readonly record struct FhmPlayerInternalIdentity(int Value)
{
    /// <summary>Gets whether the identity is the empty-line-slot sentinel.</summary>
    public bool IsEmpty => Value == FhmReferences.EmptyPlayer;
}

/// <summary>A teams.dat record ordinal used by cross-file team references.</summary>
public readonly record struct FhmTeamRecordIndex(int Value);
