namespace Shuttle.Fhm.SaveData.Model;

/// <summary>Serialized FHM identity and sentinel values. These values are references, not database keys.</summary>
public static class FhmNullConstants {
    /// <summary>General null indicator</summary>
    public const int Null = -1;

    /// <summary>Unset FHM nation-index sentinel (and some other use cases).</summary>
    public const ushort NullUnsignedShort = 9999;

    /// <summary>Unlimited or disabled open-roster-slots sentinel.</summary>
    public const int UnlimitedOpenRosterSlots = 999;
}
