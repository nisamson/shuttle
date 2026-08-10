namespace Shuttle.Fhm.SaveData.Model;

/// <summary>Losslessly exposes a finite FHM enum without rejecting values added by later game versions.</summary>
public readonly record struct FhmEnumValue<TEnum>(ushort RawValue)
    where TEnum : struct, Enum
{
    /// <summary>Gets whether <see cref="RawValue"/> is currently known.</summary>
    public bool IsKnown => Enum.IsDefined((TEnum)Enum.ToObject(typeof(TEnum), RawValue));

    /// <summary>Gets the enum view of the raw wire value.</summary>
    public TEnum Value => (TEnum)Enum.ToObject(typeof(TEnum), RawValue);
}
