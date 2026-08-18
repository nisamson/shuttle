using System.Drawing;
using BinarySerialization;
using JetBrains.Annotations;

namespace Shuttle.Fhm.Serde.General;

/// <summary>
/// Represents a QColor RGBA64
/// </summary>
public sealed record FhmColor {
    /// <summary>
    /// FHM colors are always RGBA64, but the wire format has a spec byte we need to handle.
    /// </summary>
    [FieldOrder(0)]
    [UsedImplicitly]
    public byte UnusedSpec { get; init; }

    [FieldOrder(2)]
    public ushort Red { get; init; }

    [FieldOrder(3)]
    public ushort Green { get; init; }

    [FieldOrder(4)]
    public ushort Blue { get; init; }

    [FieldOrder(1)]
    public ushort Alpha { get; init; }

    [Ignore]
    public Color Color => Color.FromArgb(Alpha >> 8, Red >> 8, Green >> 8, Blue >> 8);
};
