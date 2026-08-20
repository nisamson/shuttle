namespace Shuttle.Fhm.Serde.Domain.Binary;

/// <summary>A QDate encoded as three signed 32-bit values.</summary>
public readonly record struct FhmDate(int Year, int Month, int Day);
