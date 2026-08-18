using BinarySerialization;

namespace Shuttle.BinarySerde.Common.QFormat;

public sealed record QDate {
    [FieldOrder(0)]
    public int Year { get; set; }

    [FieldOrder(1)]
    public int Month { get; set; }

    [FieldOrder(2)]
    public int Day { get; set; }

    [Ignore]
    public DateOnly Date => new(Year, Month, Day);

    public override string ToString() {
        return Date.ToString("yyyy-MM-dd");
    }
}
