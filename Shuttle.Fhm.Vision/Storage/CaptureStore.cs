using Microsoft.EntityFrameworkCore;
using Shuttle.Fhm.Vision.Model;

namespace Shuttle.Fhm.Vision.Storage;

/// <summary>Outcome of attempting to store a capture.</summary>
public enum CaptureStoreOutcome {
    /// <summary>The capture was new and was persisted.</summary>
    Stored,

    /// <summary>An identical capture (same content hash) already existed; nothing was written.</summary>
    Duplicate,
}

/// <summary>Result of a store attempt.</summary>
public readonly record struct CaptureStoreResult(CaptureStoreOutcome Outcome, int RecordId, string? ImageFileName);

/// <summary>
/// Persists <see cref="FhmPlayerCapture"/> records to the SQLite database and saves each source
/// screenshot to an <c>images/</c> subfolder, de-duplicating by content hash.
/// </summary>
public sealed class CaptureStore {
    private readonly DbContextOptions<CaptureDbContext> options;
    private readonly string imagesDirectory;

    private CaptureStore(DbContextOptions<CaptureDbContext> options, string imagesDirectory) {
        this.options = options;
        this.imagesDirectory = imagesDirectory;
    }

    /// <summary>Opens (creating if needed) the database at <paramref name="databasePath"/>.</summary>
    /// <param name="databasePath">Path to the SQLite database file.</param>
    /// <param name="imagesDirectory">
    /// Directory for saved screenshots. Defaults to an <c>images</c> folder beside the database.
    /// </param>
    public static async Task<CaptureStore> OpenAsync(
        string databasePath,
        string? imagesDirectory,
        CancellationToken cancellationToken
    ) {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var fullDbPath = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullDbPath)!);

        var images = imagesDirectory is null
            ? Path.Combine(Path.GetDirectoryName(fullDbPath)!, "images")
            : Path.GetFullPath(imagesDirectory);
        Directory.CreateDirectory(images);

        var options = new DbContextOptionsBuilder<CaptureDbContext>()
            .UseSqlite($"Data Source={fullDbPath}")
            .Options;

        await using var context = new CaptureDbContext(options);
        await context.Database.EnsureCreatedAsync(cancellationToken);

        return new CaptureStore(options, images);
    }

    /// <summary>The directory screenshots are written to.</summary>
    public string ImagesDirectory => imagesDirectory;

    /// <summary>
    /// Stores <paramref name="capture"/> and its source screenshot, unless a capture with the same
    /// content hash already exists (in which case nothing is written).
    /// </summary>
    public async Task<CaptureStoreResult> TryStoreAsync(
        FhmPlayerCapture capture,
        ReadOnlyMemory<byte> sourceImagePng,
        CancellationToken cancellationToken
    ) {
        ArgumentNullException.ThrowIfNull(capture);

        var hash = capture.ContentHash;

        await using var context = new CaptureDbContext(options);
        var existing = await context.Captures
            .Where(c => c.ContentHash == hash)
            .Select(c => new { c.Id, c.ImageFileName })
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null) {
            return new CaptureStoreResult(CaptureStoreOutcome.Duplicate, existing.Id, existing.ImageFileName);
        }

        var imageFileName = BuildImageFileName(capture, hash);
        if (!sourceImagePng.IsEmpty) {
            var imagePath = Path.Combine(imagesDirectory, imageFileName);
            await File.WriteAllBytesAsync(imagePath, sourceImagePng, cancellationToken);
        }

        var entity = new CaptureRecordEntity {
            CapturedAtUtc = capture.CapturedAtUtc,
            Name = capture.Name,
            JerseyNumber = capture.JerseyNumber,
            Position = capture.Position,
            Handedness = capture.Handedness,
            ContentHash = hash,
            ImageFileName = sourceImagePng.IsEmpty ? null : imageFileName,
            Attributes = [.. capture.Attributes.Select(p => new AttributeValueEntity { Key = p.Key, Value = p.Value })],
            RoleRatings = [.. capture.RoleRatings.Select(p => new RoleValueEntity { Key = p.Key, Value = p.Value })],
            Numbers = [.. capture.Numbers.Select(p => new NumericValueEntity { Key = p.Key, Value = p.Value })],
            TextFields = [.. capture.TextFields.Select(p => new TextValueEntity { Key = p.Key, Value = p.Value })],
        };

        context.Captures.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return new CaptureStoreResult(CaptureStoreOutcome.Stored, entity.Id, entity.ImageFileName);
    }

    private static string BuildImageFileName(FhmPlayerCapture capture, string hash) {
        var stamp = capture.CapturedAtUtc.ToUniversalTime().ToString("yyyyMMdd-HHmmss");
        var shortHash = hash.Length >= 12 ? hash[..12] : hash;
        return $"{stamp}-{shortHash}.png";
    }
}
