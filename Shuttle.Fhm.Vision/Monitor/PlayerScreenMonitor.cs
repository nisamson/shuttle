using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Shuttle.Fhm.Vision.Capture;
using Shuttle.Fhm.Vision.Extraction;
using Shuttle.Fhm.Vision.Layout;
using Shuttle.Fhm.Vision.Storage;

namespace Shuttle.Fhm.Vision.Monitor;

/// <summary>Tuning knobs for <see cref="PlayerScreenMonitor"/>.</summary>
public sealed record MonitorOptions {
    /// <summary>Delay between window captures.</summary>
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(750);

    /// <summary>
    /// Perceptual-hash similarity (0..100) at or above which two frames are treated as the same
    /// screen — used both to detect a settled screen and to avoid reprocessing an unchanged one.
    /// </summary>
    public double StabilitySimilarity { get; init; } = 98.0;
}

/// <summary>
/// Watches a window and, whenever it settles on a new, previously-unseen player-info screen, OCRs it
/// and stores the unique capture. A fast perceptual frame hash avoids OCR'ing transient/unchanged
/// frames; content-hash dedup in the store avoids persisting the same player state twice.
/// </summary>
public sealed class PlayerScreenMonitor {
    private readonly IFrameCapture capture;
    private readonly RegionExtractor extractor;
    private readonly IReadOnlyList<LayoutProfile> profiles;
    private readonly CaptureStore store;
    private readonly MonitorOptions options;
    private readonly ILogger logger;
    private readonly PerceptualHash hasher = new();

    public PlayerScreenMonitor(
        IFrameCapture capture,
        RegionExtractor extractor,
        IReadOnlyList<LayoutProfile> profiles,
        CaptureStore store,
        MonitorOptions? options = null,
        ILogger<PlayerScreenMonitor>? logger = null
    ) {
        ArgumentNullException.ThrowIfNull(capture);
        ArgumentNullException.ThrowIfNull(extractor);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(store);
        if (profiles.Count == 0) {
            throw new ArgumentException("At least one layout profile is required.", nameof(profiles));
        }

        this.capture = capture;
        this.extractor = extractor;
        this.profiles = profiles;
        this.store = store;
        this.options = options ?? new MonitorOptions();
        this.logger = logger ?? NullLogger<PlayerScreenMonitor>.Instance;
    }

    /// <summary>Runs the capture loop until <paramref name="cancellationToken"/> is cancelled.</summary>
    public async Task RunAsync(IntPtr handle, CancellationToken cancellationToken) {
        logger.LogInformation("Monitoring window 0x{Handle:X}; press Ctrl+C to stop.", handle);

        ulong? lastProcessed = null;
        ulong? candidate = null;
        var confirmations = 0;

        while (!cancellationToken.IsCancellationRequested) {
            Image<Rgba32>? frame = null;
            try {
                frame = capture.Capture(handle);
                // PerceptualHash.Hash mutates the image (resizes it to 64x64), so hash a
                // throwaway clone and keep the full-resolution frame for OCR extraction.
                ulong hash;
                using (var hashFrame = frame.Clone()) {
                    hash = hasher.Hash(hashFrame);
                }

                if (lastProcessed is { } processed && IsSameScreen(hash, processed)) {
                    // Screen has not changed since we last stored it; keep waiting.
                    logger.LogDebug("Scanned frame hash=0x{Hash:X16}: unchanged since last stored screen.", hash);
                } else if (candidate is { } pending && IsSameScreen(hash, pending)) {
                    confirmations++;
                    logger.LogDebug("Scanned frame hash=0x{Hash:X16}: settled, processing.", hash);
                    if (confirmations >= 1) {
                        await ProcessFrameAsync(frame, cancellationToken);
                        lastProcessed = hash;
                        candidate = null;
                        confirmations = 0;
                    }
                } else {
                    candidate = hash;
                    confirmations = 0;
                    logger.LogDebug("Scanned frame hash=0x{Hash:X16}: new candidate screen.", hash);
                }
            } catch (OperationCanceledException) {
                break;
            } catch (Exception ex) {
                logger.LogWarning(ex, "Frame capture/processing failed; retrying.");
            } finally {
                frame?.Dispose();
            }

            try {
                await Task.Delay(options.PollInterval, cancellationToken);
            } catch (OperationCanceledException) {
                break;
            }
        }

        logger.LogInformation("Monitoring stopped.");
    }

    private bool IsSameScreen(ulong a, ulong b) =>
        CompareHash.Similarity(a, b) >= options.StabilitySimilarity;

    private async Task ProcessFrameAsync(Image<Rgba32> frame, CancellationToken cancellationToken) {
        LayoutProfile? matched = null;
        foreach (var candidate in profiles) {
            if (await extractor.IsPlayerScreenAsync(frame, candidate, cancellationToken)) {
                matched = candidate;
                break;
            }
        }

        if (matched is null) {
            logger.LogDebug("Settled frame did not match any of the {Count} profile(s); skipping.", profiles.Count);
            return;
        }

        var record = await extractor.ExtractAsync(frame, matched, DateTimeOffset.UtcNow, cancellationToken);

        using var pngStream = new MemoryStream();
        await frame.SaveAsPngAsync(pngStream, cancellationToken);

        var result = await store.TryStoreAsync(record, pngStream.ToArray(), cancellationToken);
        if (result.Outcome == CaptureStoreOutcome.Stored) {
            logger.LogInformation(
                "Stored capture #{Id} for '{Name}' (#{Number}) via profile '{Profile}' -> {Image}",
                result.RecordId, record.Name, record.JerseyNumber, matched.Name, result.ImageFileName);
        } else {
            logger.LogDebug(
                "Duplicate capture for '{Name}' via profile '{Profile}' (hash already stored).",
                record.Name, matched.Name);
        }
    }
}
