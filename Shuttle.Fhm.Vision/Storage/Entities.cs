namespace Shuttle.Fhm.Vision.Storage;

/// <summary>A stored player-screen capture (one row per unique <c>ContentHash</c>).</summary>
public sealed class CaptureRecordEntity {
    public int Id { get; set; }
    public DateTimeOffset CapturedAtUtc { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? JerseyNumber { get; set; }
    public string? Position { get; set; }
    public string? Handedness { get; set; }

    /// <summary>Stable fingerprint of the normalized capture; unique across the database.</summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>File name (relative to the images subfolder) of the saved source screenshot.</summary>
    public string? ImageFileName { get; set; }

    public List<AttributeValueEntity> Attributes { get; set; } = [];
    public List<RoleValueEntity> RoleRatings { get; set; } = [];
    public List<NumericValueEntity> Numbers { get; set; } = [];
    public List<TextValueEntity> TextFields { get; set; } = [];
}

/// <summary>One raw attribute rating for a capture.</summary>
public sealed class AttributeValueEntity {
    public int Id { get; set; }
    public int CaptureRecordId { get; set; }
    public string Key { get; set; } = string.Empty;
    public int Value { get; set; }
}

/// <summary>One decimal-valued field for a capture (e.g. a fractional rating, weight or salary).</summary>
public sealed class NumericValueEntity {
    public int Id { get; set; }
    public int CaptureRecordId { get; set; }
    public string Key { get; set; } = string.Empty;
    public double Value { get; set; }
}

/// <summary>One derived per-role rating for a capture.</summary>
public sealed class RoleValueEntity {
    public int Id { get; set; }
    public int CaptureRecordId { get; set; }
    public string Key { get; set; } = string.Empty;
    public int Value { get; set; }
}

/// <summary>One free-text field captured for a record.</summary>
public sealed class TextValueEntity {
    public int Id { get; set; }
    public int CaptureRecordId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
