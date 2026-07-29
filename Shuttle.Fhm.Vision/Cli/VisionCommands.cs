using System.CommandLine;
using System.Drawing;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Shuttle.Fhm.Vision.Calibration;
using Shuttle.Fhm.Vision.Capture;
using Shuttle.Fhm.Vision.Extraction;
using Shuttle.Fhm.Vision.Layout;
using Shuttle.Fhm.Vision.Monitor;
using Shuttle.Fhm.Vision.Ocr;
using Shuttle.Fhm.Vision.Recognition;
using Shuttle.Fhm.Vision.Storage;
using Shuttle.Fhm.Vision.Training;

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
        root.Subcommands.Add(BuildTrainDigits());
        root.Subcommands.Add(BuildTrainDigitsGui());
        root.Subcommands.Add(BuildEvalDigits());
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
        var templatesOption = TemplatesOption();
        var verboseOption = VerboseOption();
        var intervalOption = new Option<int>("--interval") {
            Description = "Polling interval in milliseconds.",
            DefaultValueFactory = _ => 750,
        };

        var command = new Command("monitor", "Watch the FHM window and store unique player-info screens.") {
            pidOption, processOption, profileOption, dbOption, imagesOption, templatesOption, verboseOption, intervalOption,
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
            var recognizer = await TryLoadRecognizerAsync(parseResult.GetValue(templatesOption), cancellationToken);
            var level = parseResult.GetValue(verboseOption) ? LogLevel.Debug : LogLevel.Information;
            var extractor = new RegionExtractor(
                engine, new ConsoleLogger<RegionExtractor>(level), digitRecognizer: recognizer);
            var options = new MonitorOptions {
                PollInterval = TimeSpan.FromMilliseconds(parseResult.GetValue(intervalOption)),
            };
            var monitor = new PlayerScreenMonitor(
                new GdiWindowCapture(), extractor, profiles, store, options, new ConsoleLogger<PlayerScreenMonitor>(level));

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
        var templatesOption = TemplatesOption();
        var verboseOption = VerboseOption();

        var command = new Command("ingest-image", "Parse a single saved screenshot and store the capture (offline).") {
            imageOption, profileOption, dbOption, imagesOption, templatesOption, verboseOption,
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
            var recognizer = await TryLoadRecognizerAsync(parseResult.GetValue(templatesOption), cancellationToken);
            var level = parseResult.GetValue(verboseOption) ? LogLevel.Debug : LogLevel.Information;
            var extractor = new RegionExtractor(
                engine, new ConsoleLogger<RegionExtractor>(level), digitRecognizer: recognizer);

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
                    var (text, pixels, png) = await ReadRegionAsync(image, engine, anchor.Bounds, false, cancellationToken);
                    var matched = text.IndexOf(anchor.ExpectedText, StringComparison.OrdinalIgnoreCase) >= 0;
                    Console.WriteLine(
                        $"  anchor[{i}] {(matched ? "MATCH   " : "NO MATCH")} "
                        + $"expected='{anchor.ExpectedText}' got='{Flatten(text)}' {DescribeRect(pixels)}");
                    await DumpCropAsync(png, outDir, $"{Sanitize(profile.Name)}_anchor{i}", cancellationToken);
                }

                foreach (var region in profile.Regions) {
                    var baseName = $"{Sanitize(profile.Name)}_{Sanitize(region.Key)}";
                    var isolate = RegionExtractor.ShouldIsolateWhiteText(region.Kind);

                    // "before": the raw (upscaled, un-binarised) crop.
                    var (rawText, pixels, rawPng) =
                        await ReadRegionAsync(image, engine, region.Bounds, false, cancellationToken);
                    await DumpCropAsync(rawPng, outDir, baseName, cancellationToken);

                    if (!isolate) {
                        Console.WriteLine(
                            $"  region {EmptyFlag(rawText)} {region.Group}/{region.Kind} '{region.Key}' "
                            + $"got='{Flatten(rawText)}' {DescribeRect(pixels)} -> {baseName}.png");
                        continue;
                    }

                    // "after": the white-text-isolated (black-on-white) crop actually fed to OCR.
                    var (bwText, _, bwPng) =
                        await ReadRegionAsync(image, engine, region.Bounds, true, cancellationToken);
                    await DumpCropAsync(bwPng, outDir, $"{baseName}.bw", cancellationToken);

                    Console.WriteLine(
                        $"  region {region.Group}/{region.Kind} '{region.Key}' {DescribeRect(pixels)}");
                    Console.WriteLine(
                        $"      before (raw)      {EmptyFlag(rawText)} got='{Flatten(rawText)}' -> {baseName}.png");
                    Console.WriteLine(
                        $"      after  (b/w text) {EmptyFlag(bwText)} got='{Flatten(bwText)}' -> {baseName}.bw.png");
                }
            }

            Console.WriteLine();
            Console.WriteLine(
                "Note: crops are the exact (upscaled) images fed to OCR; numeric/bio regions also get a "
                + "'.bw.png' showing the white-text isolation actually used for OCR. If a crop shows the "
                + "wrong area, adjust the region bounds in 'calibrate'.");
            return 0;
        });

        return command;
    }

    private static Command BuildTrainDigits() {
        var imageOption = new Option<FileInfo>("--image", "-i") {
            Description = "Screenshot PNG containing numeric cells to label.",
            Required = true,
        };
        var profileOption = new Option<FileInfo[]>("--profile", "-p") {
            Description = "Layout profile JSON whose numeric regions to segment. Repeat to use several.",
            Required = true,
            AllowMultipleArgumentsPerToken = true,
        };
        var templatesOption = new Option<FileInfo>("--templates", "-t") {
            Description = "Digit template JSON to append to (created if it does not exist).",
            Required = true,
        };
        var outOption = new Option<DirectoryInfo?>("--out", "-o") {
            Description = "Directory to write labelled glyph preview PNGs into (default: none).",
        };

        var command = new Command(
            "train-digits",
            "Interactively label the FHM rating font: segment numeric cells into glyphs and record templates.") {
            imageOption, profileOption, templatesOption, outOption,
        };

        command.SetAction(async (parseResult, cancellationToken) => {
            var imageFile = parseResult.GetValue(imageOption)!;
            var profileFiles = parseResult.GetValue(profileOption) ?? [];
            var templatesFile = parseResult.GetValue(templatesOption)!;
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

            var set = templatesFile.Exists
                ? await DigitTemplateStore.LoadAsync(templatesFile, cancellationToken)
                : new DigitTemplateSet();
            Console.WriteLine(
                $"Templates: {templatesFile.FullName} ({set.Templates.Count} existing, glyph {set.Width}x{set.Height})");

            var extractor = new RegionExtractor(engine, new ConsoleLogger<RegionExtractor>());
            var outDir = parseResult.GetValue(outOption);
            outDir?.Create();

            // Best-guess helper built from the current templates; rebuilt whenever we add one so
            // guesses reflect everything labelled so far (the dataset is small, so this is cheap).
            var guesser = set.Templates.Count > 0 ? new TemplateDigitRecognizer(set) : null;

            using var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(imageFile.FullName, cancellationToken);
            Console.WriteLine($"Image: {imageFile.FullName} ({image.Width}x{image.Height})");
            Console.WriteLine(
                "For each glyph: press Enter to accept the guess (when shown), type a character to label it, "
                + "'s' to skip, or 'q' to save and quit.");

            var added = 0;
            var quit = false;
            foreach (var file in profileFiles) {
                if (quit) {
                    break;
                }

                var profile = await LayoutProfileStore.LoadAsync(file, cancellationToken);
                if (!await extractor.IsPlayerScreenAsync(image, profile, cancellationToken)) {
                    Console.WriteLine($"Profile '{profile.Name}' anchors did not match; skipping.");
                    continue;
                }

                Console.WriteLine($"=== Profile '{profile.Name}' ===");
                foreach (var region in profile.Regions) {
                    if (region.Kind is not (FieldKind.Integer or FieldKind.Float)) {
                        continue;
                    }

                    var pixels = region.Bounds.ToPixels(image.Width, image.Height);
                    var glyphs = DigitSegmenter.Segment(image, pixels, set.Width, set.Height);
                    Console.WriteLine($"  region '{region.Key}' -> {glyphs.Count} glyph(s)");

                    for (var i = 0; i < glyphs.Count; i++) {
                        if (outDir is not null) {
                            var png = await RegionImaging.CropToPngAsync(image, glyphs[i].Bounds, cancellationToken);
                            await File.WriteAllBytesAsync(
                                Path.Combine(outDir.FullName, $"{Sanitize(region.Key)}_{i}.png"), png, cancellationToken);
                        }

                        var guess = guesser?.Classify(glyphs[i].Glyph);
                        var guessHint = guess is { } g
                            ? $"guess='{g.Label}' ({(g.Confident ? "confident" : "low")} d={g.Score:0.###} m={g.Margin:0.###}) "
                            : string.Empty;
                        Console.Write($"    glyph[{i}] {DescribeRect(glyphs[i].Bounds)} {guessHint}label> ");

                        var input = Console.ReadLine()?.Trim() ?? string.Empty;
                        if (input.Equals("q", StringComparison.OrdinalIgnoreCase)) {
                            quit = true;
                            break;
                        }

                        if (input.Equals("s", StringComparison.OrdinalIgnoreCase)) {
                            continue;
                        }

                        string label;
                        if (input.Length == 0) {
                            // Enter accepts the current guess; with no guess, it skips.
                            if (guess is not { } accepted) {
                                continue;
                            }

                            label = accepted.Label;
                        } else {
                            label = input;
                        }

                        if (!set.TryAdd(label, glyphs[i].Glyph)) {
                            Console.WriteLine($"    (duplicate '{label}' glyph; skipped)");
                            continue;
                        }

                        added++;
                        guesser = new TemplateDigitRecognizer(set);
                    }

                    if (quit) {
                        break;
                    }
                }
            }

            await DigitTemplateStore.SaveAsync(templatesFile, set, cancellationToken);
            Console.WriteLine($"Added {added} template(s); saved {set.Templates.Count} total to {templatesFile.FullName}");
            return 0;
        });

        return command;
    }

    private static Command BuildTrainDigitsGui() {
        var templatesOption = new Option<FileInfo>("--templates", "-t") {
            Description = "Digit template JSON to append to (created if it does not exist).",
            Required = true,
        };
        var profileOption = new Option<FileInfo[]>("--profile", "-p") {
            Description = "Layout profile JSON whose numeric regions to segment. Repeat to use several.",
            Required = true,
            AllowMultipleArgumentsPerToken = true,
        };
        var imageOption = new Option<FileInfo[]>("--image", "-i") {
            Description = "Screenshot PNG(s) to preload. Optional — more can be added from within the GUI.",
            AllowMultipleArgumentsPerToken = true,
        };

        var command = new Command(
            "train-digits-gui",
            "Interactive GUI trainer: label FHM rating glyphs with original/normalized previews and "
            + "live confidence, pulling in multiple screenshots.") {
            templatesOption, profileOption, imageOption,
        };

        command.SetAction(async (parseResult, cancellationToken) => {
            var templatesFile = parseResult.GetValue(templatesOption)!;
            var profileFiles = parseResult.GetValue(profileOption) ?? [];
            var imageFiles = parseResult.GetValue(imageOption) ?? [];

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

            var set = templatesFile.Exists
                ? await DigitTemplateStore.LoadAsync(templatesFile, cancellationToken)
                : new DigitTemplateSet();
            Console.WriteLine(
                $"Templates: {templatesFile.FullName} ({set.Templates.Count} existing, glyph {set.Width}x{set.Height})");

            var profiles = new List<LayoutProfile>();
            foreach (var file in profileFiles) {
                profiles.Add(await LayoutProfileStore.LoadAsync(file, cancellationToken));
            }

            var extractor = new RegionExtractor(engine, new ConsoleLogger<RegionExtractor>());
            var initial = await PendingGlyphBuilder.BuildAsync(
                imageFiles, profiles, set.Width, set.Height, extractor, cancellationToken);
            Console.WriteLine($"Queued {initial.Count} glyph(s) from {imageFiles.Length} preloaded image(s).");

            // Segment images added from within the GUI on a threadpool thread so the (async, WinRT) OCR
            // anchor check never runs on the WinForms STA thread.
            IReadOnlyList<PendingGlyph> AddImages(IReadOnlyList<FileInfo> files) =>
                Task.Run(() => PendingGlyphBuilder.BuildAsync(
                    files, profiles, set.Width, set.Height, extractor, cancellationToken)).GetAwaiter().GetResult();

            var total = DigitTrainerLauncher.Run(initial, set, templatesFile, AddImages);
            Console.WriteLine($"Saved {total} template(s) to {templatesFile.FullName}.");
            return 0;
        });

        return command;
    }

    private static Command BuildEvalDigits() {
        var templatesOption = new Option<FileInfo>("--templates", "-t") {
            Description = "Digit template JSON (from 'train-digits') to score.",
            Required = true,
        };
        var thresholdOption = new Option<double>("--threshold") {
            Description = "Confidence threshold used at recognition time, for context in the margin report.",
            DefaultValueFactory = _ => 0.18,
        };

        var command = new Command(
            "eval-digits",
            "Score a digit template set by leave-one-out cross-validation: per-label accuracy, "
            + "confusion pairs and confidence margins, so you can tell when it is solid.") {
            templatesOption, thresholdOption,
        };

        command.SetAction(async (parseResult, cancellationToken) => {
            var templatesFile = parseResult.GetValue(templatesOption)!;
            if (!templatesFile.Exists) {
                Console.Error.WriteLine($"Templates file not found: {templatesFile.FullName}");
                return 1;
            }

            var threshold = parseResult.GetValue(thresholdOption);
            var set = await DigitTemplateStore.LoadAsync(templatesFile, cancellationToken);
            if (set.Templates.Count < 2) {
                Console.Error.WriteLine(
                    $"Only {set.Templates.Count} template(s); leave-one-out needs at least 2. Collect more with 'train-digits'.");
                return 2;
            }

            var eval = DigitTemplateEvaluator.Evaluate(set);
            Console.WriteLine($"Templates: {templatesFile.FullName}");
            Console.WriteLine(
                $"Glyph {set.Width}x{set.Height}; {eval.TemplateCount} template(s) across {eval.PerLabel.Count} label(s); "
                + $"recognition threshold {threshold:0.###}");
            Console.WriteLine(
                $"Leave-one-out accuracy: {eval.Correct}/{eval.TemplateCount} ({eval.Accuracy:P1})");
            Console.WriteLine();

            Console.WriteLine("  label  count  acc      same   other   margin");
            foreach (var s in eval.PerLabel) {
                var flag = LabelHealthFlag(s, threshold);
                Console.WriteLine(
                    $"  {s.Label,-5}  {s.Count,5}  {s.Accuracy,6:P0}  {s.MeanSameDistance,6:0.###} "
                    + $"{s.MeanNearestOtherDistance,6:0.###}  {s.Margin,6:0.###}  {flag}");
            }

            if (eval.Confusions.Count > 0) {
                Console.WriteLine();
                Console.WriteLine("Confusions (held-out actual -> predicted):");
                foreach (var c in eval.Confusions) {
                    Console.WriteLine($"  '{c.Actual}' misread as '{c.Predicted}'  x{c.Count}");
                }
            }

            if (eval.SingleSampleLabels.Count > 0) {
                Console.WriteLine();
                Console.WriteLine(
                    "Only one sample (cannot be validated; will always miss under leave-one-out): "
                    + string.Join(", ", eval.SingleSampleLabels.Select(l => $"'{l}'")));
            }

            Console.WriteLine();
            Console.WriteLine(Verdict(eval, threshold));
            return 0;
        });

        return command;
    }

    /// <summary>Flags a label that needs attention: misclassifications, thin coverage, or a slim margin.</summary>
    private static string LabelHealthFlag(LabelStats s, double threshold) {
        if (s.Accuracy < 1.0) {
            return "<- errors";
        }

        if (s.Count < 5) {
            return "<- few samples";
        }

        if (s.Margin < threshold) {
            return "<- slim margin";
        }

        return string.Empty;
    }

    private static string Verdict(DigitEvaluation eval, double threshold) {
        var thinLabels = eval.PerLabel.Count(s => s.Count < 5);
        var slimLabels = eval.PerLabel.Count(s => s.Accuracy >= 1.0 && s.Margin < threshold);

        if (eval.Accuracy >= 1.0 && thinLabels == 0 && slimLabels == 0) {
            return "Verdict: SOLID - 100% leave-one-out accuracy with healthy sample counts and margins.";
        }

        if (eval.Accuracy >= 0.98 && eval.Confusions.Count == 0) {
            return "Verdict: GOOD - collect a few more samples for the flagged labels to firm up margins.";
        }

        return "Verdict: NEEDS MORE DATA - address the confusions above and add samples for flagged labels, then re-run.";
    }

    /// <summary>Shared <c>--templates</c> option enabling the trained digit recognizer for numeric cells.</summary>
    private static Option<FileInfo?> TemplatesOption() =>
        new("--templates", "-t") {
            Description = "Digit template JSON (from 'train-digits'); when present, numeric cells use the "
                + "trained recognizer, falling back to OCR when it is not confident.",
        };

    /// <summary>Shared <c>--verbose</c> flag that lowers console logging to Debug (per-scan / per-region detail).</summary>
    private static Option<bool> VerboseOption() =>
        new("--verbose", "-v") {
            Description = "Log Debug-level detail: every scanned frame, per-region recognizer/OCR reads, "
                + "profile-match results and duplicate skips.",
        };

    /// <summary>
    /// Loads a <see cref="TemplateDigitRecognizer"/> from the given file when it exists and is non-empty;
    /// returns <c>null</c> (OCR-only) otherwise.
    /// </summary>
    private static async Task<IDigitRecognizer?> TryLoadRecognizerAsync(FileInfo? file, CancellationToken cancellationToken) {
        if (file is null) {
            return null;
        }

        if (!file.Exists) {
            Console.Error.WriteLine($"Templates file not found: {file.FullName}; using OCR only.");
            return null;
        }

        var set = await DigitTemplateStore.LoadAsync(file, cancellationToken);
        if (set.Templates.Count == 0) {
            Console.Error.WriteLine($"Templates file {file.FullName} is empty; using OCR only.");
            return null;
        }

        Console.WriteLine($"Digit recognizer: {set.Templates.Count} template(s) from {file.FullName}");
        return new TemplateDigitRecognizer(set);
    }

    private static async Task<(string Text, PixelRect Pixels, byte[] Png)> ReadRegionAsync(
        Image<Rgba32> image, IOcrEngine engine, RatioRect bounds, bool isolateWhiteText, CancellationToken cancellationToken) {
        var pixels = bounds.ToPixels(image.Width, image.Height);
        var png = await RegionImaging.CropForOcrAsync(
            image, pixels, cancellationToken, isolateWhiteText: isolateWhiteText);
        var text = await engine.RecognizeAsync(png, cancellationToken);
        return (text, pixels, png);
    }

    private static async Task DumpCropAsync(
        byte[] png, DirectoryInfo outDir, string name, CancellationToken cancellationToken) {
        await File.WriteAllBytesAsync(Path.Combine(outDir.FullName, $"{name}.png"), png, cancellationToken);
    }

    private static string EmptyFlag(string text) => string.IsNullOrWhiteSpace(text) ? "EMPTY " : "      ";

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
