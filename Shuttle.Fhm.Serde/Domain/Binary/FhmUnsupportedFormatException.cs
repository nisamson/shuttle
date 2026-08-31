namespace Shuttle.Fhm.Serde.Domain.Binary;

/// <summary>Indicates valid FHM save data whose layout is not fully modeled.</summary>
public sealed class FhmUnsupportedFormatException : IOException
{
    /// <summary>Initializes a new instance of the <see cref="FhmUnsupportedFormatException"/> class.</summary>
    public FhmUnsupportedFormatException(string message)
        : base(message)
    {
    }
}
