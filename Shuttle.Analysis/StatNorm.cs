namespace Shuttle.Analysis;

/// <summary>
/// Selects whether player stat attribute values are exported raw or replaced in place with a
/// normalized form of the per-player stat vector.
/// </summary>
public enum StatNorm {

    /// <summary>Export the raw attribute values.</summary>
    None,

    /// <summary>Replace each attribute with its value divided by the vector's L1 norm.</summary>
    L1,

    /// <summary>Replace each attribute with its value divided by the vector's L2 norm (unit vector).</summary>
    L2,
}
