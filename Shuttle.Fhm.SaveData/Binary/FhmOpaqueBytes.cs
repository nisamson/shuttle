namespace Shuttle.Fhm.SaveData.Binary;

/// <summary>Bytes whose FHM wire boundary is known but whose semantics are not.</summary>
public sealed class FhmOpaqueBytes
{
    /// <summary>Initializes an opaque byte block.</summary>
    public FhmOpaqueBytes(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    /// <summary>Gets the exact bytes in wire order.</summary>
    public byte[] Value { get; }
}