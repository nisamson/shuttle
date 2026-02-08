using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace SHLAnalytics.Math;

[method: SetsRequiredMembers]
public abstract record BaseRating<TSelf, TValue>(TValue Value) : IComparable<BaseRating<TSelf, TValue>>
    where TSelf : BaseRating<TSelf, TValue>
    where TValue : INumber<TValue> {

    public required TValue Value { get; init; } = Value;
    public override string ToString() => Value.ToString() ?? throw new InvalidOperationException("Value.ToString() returned null");
    
    public int CompareTo(BaseRating<TSelf, TValue>? other)
    {
        if (other is null) return 1;
        return Value.CompareTo(other.Value);
    }
}

[method: SetsRequiredMembers]
public record Rating(int Value) : BaseRating<Rating, int>(Value);