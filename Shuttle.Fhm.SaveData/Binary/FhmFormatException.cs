namespace Shuttle.Fhm.SaveData.Binary;

/// <summary>Indicates malformed or unsupported FHM save data.</summary>
public sealed class FhmFormatException : IOException
{
    /// <summary>Initializes a new instance of the <see cref="FhmFormatException"/> class.</summary>
    public FhmFormatException(string message)
        : base(message)
    {
    }
}
