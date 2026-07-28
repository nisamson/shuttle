using System.CommandLine;
using System.Drawing;
using System.Runtime.Versioning;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Shuttle.Fhm.Vision.Calibration;
using Shuttle.Fhm.Vision.Capture;
using Shuttle.Fhm.Vision.Extraction;
using Shuttle.Fhm.Vision.Layout;
using Shuttle.Fhm.Vision.Monitor;
using Shuttle.Fhm.Vision.Ocr;
using Shuttle.Fhm.Vision.Storage;

namespace Shuttle.Fhm.Vision.Cli;

/// <summary>Builds the tool's command tree.</summary>
[SupportedOSPlatform("windows10.0.19041.0")]
public static class VisionCommands {
    private const string DefaultProcessFragment = "FHM";
    private const string DefaultDatabase = "fhm-captures.db";

    public static RootCommand BuildRoot() {
        var root = new RootCommand(
            "FHM player-screen capture and OCR data-collection tool (Windows only).");
        root.Subcommands.Add(BuildListWindows());
        root.Subcommands.Add(BuildCalibrate());
        root.Subcommands.Add(BuildMonitor());
        root.Subcommands.Add(BuildIngestImage());
        root.Subcommands.Add(BuildInspect());
        return root;
    }

    private static Command BuildListWindows() {
        var processOption = new Option<string?>("--process", "-n") {
            Description = "Filter to windows whose process name contains this text (case-insensitive).",
        };

        var command = new Command("list-windows", "List visible top-level windows (to find FHM's PID).") {
            processOption,
        };

        command.SetAction(parseResult => {
            var filter = parseResult.GetValue(processOption);
            var locator = new WindowLocator();
            var windows = filter is null
                ? locator.EnumerateWindows()
                : locator.FindByProcessName(filter);

            if (windows.Count == 0) {
                Console.WriteLine("No matching windows found.");
                return 0;
            }

            Console.WriteLine($"{"PID",-8} {"Process",-24} Title");
            foreach (var window in windows.OrderBy(w => w.ProcessName)) {
                Console.WriteLine($"{window.ProcessId,-8} {Truncate(window.ProcessName, 24),-24} {window.Title}");
            }

            return 0;
        });

        return command;
    }

    private static Command BuildCalibrate() {
        var pidOption = PidOption();
        var processOption = ProcessOption();
        var imageOption = new Option<FileInfo?>("--image", "-i") {
            Description = "Calibrate against an existing screenshot PNG instead of capturing a live window.",
        };
        var profileOption = new Option<FileInfo>("--profile", "-p") {
            Description = "Layout profile JSON to create or edit.",
            Required = true,
        };

        var command = new Command("calibrate", "Open the interactive editor to author/edit a layout profile.") {
            pidOption, processOption, imageOption, profileOption,
        };

        command.SetAction(async (parseResult, cancellationToken) => {
            var profileFile = parseResult.GetValue(profileOption)!;
            var imageFile = parseResult.GetValue(imageOption);

            byte[] pngBytes;
            if (imageFile is not null) {
                if (!imageFile.Exists) {
                    Console.Error.WriteLine($"Image not found: {imageFile.FullName}");
                    return 1;
                }

                pngBytes = await File.ReadAllBytesAsync(imageFile.FullName, cancellationToken);
            } else {
                var handle = ResolveHandle(parseResult.GetValue(pidOption), parseResult.GetValue(processOption));
                if (handle is null) {
                    return 1;
                }

                using var frame = new GdiWindowCapture().Capture(handle.Value);
                pngBytes = await ToPngAsync(frame, cancellationToken);
            }

            var existing = profileFile.Exists
                ? await LayoutProfileStore.LoadAsync(profileFile, cancellationToken)
                : null;

            using var bitmap = ToBitmap(pngBytes);
            var result = CalibrationLauncher.Run(bitmap, existing, profileFile);
            if (result is null) {
                Console.WriteLine("Calibration closed without saving; profile unchanged.");
                return 0;
            }

            Console.WriteLine(
                $"Saved profile '{result.Name}' with {result.Regions.Count} region(s) and "
                + $"{result.Anchors.Count} anchor(s) to {profileFile.FullName}.");
            return 0;
        });

        return command;
    }

    private static Command BuildMonitor() {
        var pidOption = PidOption();
        var processOption = ProcessOption();
        var profileOption = new Option<FileInfo[]>("--profile", "-p") {
            Description = "Layout profile JSON describing a player-info screen. "
                + "Repeat to supply several; each frame is matched against them in order.",
            Required = true,
            AllowMultipleArgumentsPerToken = true,
        };
        var dbOption = DatabaseOption();
        var imagesOption = ImagesOption();
        var intervalOption = new Option<int>("--interval") {
            Description = "Polling interval in milliseconds.",
            DefaultValueFactory = _ => 750,
        };

        var command = new Command("monitor", "Watch the FHM window and store unique player-info screens.") {
            pidOption, processOption, profileOption, dbOption, imagesOption, intervalOption,
        };

        command.SetAction(async (parseResult, cancellationToken) => {
            var profileFiles = parseResult.GetValue(profileOption) ?? [];
            var missing = profileFiles.Where(f => !f.Exists).ToList();
            if (missing.Count > 0) {
                foreach (var file in missing) {
                    Console.Error.WriteLine($"Profile not found: {file.FullName}. Run 'calibrate' first.");
                }

                return 1;
            }

            var handle = ResolveHandle(parseResult.GetValue(pidOption), parseResult.GetValue(processOption));
            if (handle is null) {
                return 1;
            }

            var engine = TryCreateOcrEngine();
            if (engine is null) {
                return 1;
            }

            var profiles = new List<LayoutProfile>(profileFiles.Length);
            foreach (var file in profileFiles) {
                profiles.Add(await LayoutProfileStore.LoadAsync(file, cancellationToken));
            }

            var store = await CaptureStore.OpenAsync(
                parseResult.GetValue(dbOption)!, parseResult.GetValue(imagesOption), cancellationToken);
            var extractor = new RegionExtractor(engine, new ConsoleLogger<RegionExtractor>());
            var options = new MonitorOptions {
                PollInterval = TimeSpan.FromMilliseconds(parseResult.GetValue(intervalOption)),
            };
            var monitor = new PlayerScreenMonitor(
                new GdiWindowCapture(), extractor, profiles, store, options, new ConsoleLogger<PlayerScreenMonitor>());

            Console.WriteLine($"Matching against {profiles.Count} profile(s): {string.Join(", ", profiles.Select(p => p.Name))}");
            Console.WriteLine($"Storing captures in {Path.GetFullPath(parseResult.GetValue(dbOption)!)}");
            Console.WriteLine($"Images -> {store.ImagesDirectory}");
            await monitor.RunAsync(handle.Value, cancellationToken);
            return 0;
        });

        return command;
    }

    private static Command BuildIngestImage() {
        var imageOption = new Option<FileInfo>("--image", "-i") {
            Description = "Screenshot PNG to parse.",
            Required = true,
        };
        var profileOption = new Option<FileInfo[]>("--profile", "-p") {
            Description = "Layout profile JSON to apply. "
                + "Repeat to supply several; the image is matched against them in order.",
            Required = true,
            AllowMultipleArgumentsPerToken = true,
        };
        var dbOption = DatabaseOption();
        var imagesOption = ImagesOption();

        var command = new Command("ingest-image", "Parse a single saved screenshot and store the capture (offline).") {
            imageOption, profileOption, dbOption, imagesOption,
        };

        command.SetAction(async (parseResult, cancellationToken) => {
            var imageFile = parseResult.GetValue(imageOption)!;
            var profileFiles = parseResult.GetValue(profileOption) ?? [];
            if (!imageFile.Exists) {
                Console.Error.WriteLine($"Image not found: {imageFile.FullName}");
                return 1;
            }

            var missing = profileFiles.Where(f => !f.Exists).ToList();
            if (missing.Count > 0) {
                foreach (var file in missing) {
                    Console.Error.WriteLine($"Profile not found: {file.FullName}");
                }

                return 1;
            }

            var engine = TryCreateOcrEngine();
            if (engine is null) {
                return 1;
            }

            var profiles = new List<LayoutProfile>(profileFiles.Length);
            foreach (var file in profileFiles) {
                profiles.Add(await LayoutProfileStore.LoadAsync(file, cancellationToken));
            }

            using var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(imageFile.FullName, cancellationToken);
            var extractor = new RegionExtractor(engine, new ConsoleLogger<RegionExtractor>());

            LayoutProfile? matched = null;
            foreach (var candidate in profiles) {
                if (await extractor.IsPlayerScreenAsync(image, candidate, cancellationToken)) {
                    matched = candidate;
                    break;
                }
            }

            if (matched is null) {
                Console.Error.WriteLine(
                    $"The image did not match any of {profiles.Count} profile(s)' anchors (not a player-info screen).");
                return 2;
            }

            var record = await extractor.ExtractAsync(image, matched, DateTimeOffset.UtcNow, cancellationToken);
            var store = await CaptureStore.OpenAsync(
                parseResult.GetValue(dbOption)!, parseResult.GetValue(imagesOption), cancellationToken);
            var png = await ToPngAsync(image, cancellationToken);
            var result = await store.TryStoreAsync(record, png, cancellationToken);

            Console.WriteLine(
                $"{result.Outcome} [{matched.Name}]: '{record.Name}' (#{record.JerseyNumber}) "
                + $"attributes={record.Attributes.Count} roles={record.RoleRatings.Count} "
                + $"-> record #{result.RecordId} {result.ImageFileName}");
            return 0;
        });

        return command;
    }

    private static Command BuildInspect() {
        var imageOption = new Option<FileInfo>("--image", "-i") {
            Description = "Screenshot PNG to inspect.",
            Required = true,
        };
        var profileOption = new Option<FileInfo[]>("--profile", "-p") {
            Description = "Layout profile JSON to inspect. Repeat to inspect several.",
            Required = true,
            AllowMultipleArgumentsPerToken = true,
        };
        var outOption = new Option<DirectoryInfo?>("--out", "-o") {
            Description = "Directory to write per-region crop PNGs into (default: 'inspect' beside the image).",
        };

        var command = new Command(
            "inspect",
            "Diagnose a profile against a screenshot: dump each anchor/region crop and its OCR text.") {
            imageOption, profileOption, outOption,
        };

        command.SetAction(async (parseResult, cancellationToken) => {
            var imageFile = parseResult.GetValue(imageOption)!;
            var profileFiles = parseResult.GetValue(profileOption) ?? [];
            if (!imageFile.Exists) {
                Console.Error.WriteLine($"Image not found: {imageFile.FullName}");
                return 1;
            }

            var missing = profileFiles.Where(f => !f.Exists).ToList();
            if (missing.Count > 0) {
                foreach (var file in missing) {
                    Console.Error.WriteLine($"Profile not found: {file.FullName}");
                }

                return 1;
            }

            var engine = TryCreateOcrEngine();
            if (engine is null) {
                return 1;
            }

            using var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(imageFile.FullName, cancellationToken);
            Console.WriteLine($"Image: {imageFile.FullName} ({image.Width}x{image.Height})");

            var outDir = parseResult.GetValue(outOption)
                ?? new DirectoryInfo(Path.Combine(imageFile.DirectoryName ?? ".", "inspect"));
            outDir.Create();
            Console.WriteLine($"Crops -> {outDir.FullName}");

            foreach (var file in profileFiles) {
                var profile = await LayoutProfileStore.LoadAsync(file, cancellationToken);
                Console.WriteLine();
                Console.WriteLine($"=== Profile '{profile.Name}' ({file.Name}) ===");

                for (var i = 0; i < profile.Anchors.Count; i++) {
                    var anchor = profile.Anchors[i];
                    var (text, pixels, png) = await ReadRegionAsync(image, engine, anchor.Bounds, cancellationToken);
                    var matched = text.IndexOf(anchor.ExpectedText, StringComparison.OrdinalIgnoreCase) >= 0;
                    Console.WriteLine(
                        $"  anchor[{i}] {(matched ? "MATCH   " : "NO MATCH")} "
                        + $"expected='{anchor.ExpectedText}' got='{Flatten(text)}' {DescribeRect(pixels)}");
                    await DumpCropAsync(png, outDir, $"{Sanitize(profile.Name)}_anchor{i}", cancellationToken);
                }

                foreach (var region in profile.Regions) {
                    var (text, pixels, png) = await ReadRegionAsync(image, engine, region.Bounds, cancellationToken);
                    var flag = string.IsNullOrWhiteSpace(text) ? "EMPTY " : "      ";
                    Console.WriteLine(
                        $"  region {flag} {region.Group}/{region.Kind} '{region.Key}' "
                        + $"got='{Flatten(text)}' {DescribeRect(pixels)}");
                    await DumpCropAsync(
                        png, outDir, $"{Sanitize(profile.Name)}_{Sanitize(region.Key)}", cancellationToken);
                }
            }

            Console.WriteLine();
            Console.WriteLine(
                "Note: crops are the exact (upscaled) images fed to OCR. Open the EMPTY regions' crop "
                + "PNGs \u2014 if the crop shows the wrong area, adjust the region bounds in 'calibrate'; "
                + "if it looks right but text is EMPTY, the region may still be too small/low-contrast for OCR.");
            return 0;
        });

        return command;
    }

    private static async Task<(string Text, PixelRect Pixels, byte[] Png)> ReadRegionAsync(
        Image<Rgba32> image, IOcrEngine engine, RatioRect bounds, CancellationToken cancellationToken) {
        var pixels = bounds.ToPixels(image.Width, image.Height);
        var png = await RegionImaging.CropForOcrAsync(image, pixels, cancellationToken);
        var text = await engine.RecognizeAsync(png, cancellationToken);
        return (text, pixels, png);
    }

    private static async Task DumpCropAsync(
        byte[] png, DirectoryInfo outDir, string name, CancellationToken cancellationToken) {
        await File.WriteAllBytesAsync(Path.Combine(outDir.FullName, $"{name}.png"), png, cancellationToken);
    }

    private static string DescribeRect(PixelRect r) => $"[x={r.X} y={r.Y} w={r.Width} h={r.Height}]";

    private static string Flatten(string text) => text.Replace('\r', ' ').Replace('\n', ' ').Trim();

    private static string Sanitize(string value) =>
        string.Concat(value.Select(c => Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 ? '_' : c));

    private static Option<int?> PidOption() =>
        new("--pid") { Description = "Process id of the FHM window to capture." };

    private static Option<string?> ProcessOption() =>
        new("--process", "-n") {
            Description = $"Process-name fragment to auto-detect the FHM window (default: '{DefaultProcessFragment}').",
        };

    private static Option<string> DatabaseOption() =>
        new("--db", "-d") {
            Description = $"SQLite database path (default: '{DefaultDatabase}').",
            DefaultValueFactory = _ => DefaultDatabase,
        };

    private static Option<string?> ImagesOption() =>
        new("--images") { Description = "Directory for saved screenshots (default: 'images' beside the database)." };

    private static IntPtr? ResolveHandle(int? pid, string? process) {
        var locator = new WindowLocator();

        if (pid is { } id) {
            var window = locator.FindByProcessId(id);
            if (window is null) {
                Console.Error.WriteLine($"No visible window found for PID {id}.");
                return null;
            }

            Console.WriteLine($"Using window '{window.Title}' (PID {window.ProcessId}).");
            return window.Handle;
        }

        var fragment = process ?? DefaultProcessFragment;
        var matches = locator.FindByProcessName(fragment);
        if (matches.Count == 0) {
            Console.Error.WriteLine(
                $"No window found for process containing '{fragment}'. "
                + "Pass --pid or --process, or run 'list-windows'.");
            return null;
        }

        var chosen = matches[0];
        Console.WriteLine($"Using window '{chosen.Title}' (process '{chosen.ProcessName}', PID {chosen.ProcessId}).");
        return chosen.Handle;
    }

    private static IOcrEngine? TryCreateOcrEngine() {
        try {
            return new WindowsMediaOcrEngine();
        } catch (Exception ex) {
            Console.Error.WriteLine($"Could not initialise Windows OCR: {ex.Message}");
            return null;
        }
    }

    private static async Task<byte[]> ToPngAsync(Image<Rgba32> image, CancellationToken cancellationToken) {
        using var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream, cancellationToken);
        return stream.ToArray();
    }

    private static Bitmap ToBitmap(byte[] png) {
        using var stream = new MemoryStream(png);
        using var loaded = new Bitmap(stream);
        return new Bitmap(loaded);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
