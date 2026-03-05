using FFMediaToolkit.Decoding;
using FFMediaToolkit.Graphics;
using Microsoft.Extensions.Logging;

namespace SHLAnalytics.ML.CV.Trainer.Commands;

public class SplitCommand : ICommand {
    private readonly ILogger<SplitCommand> logger;
    private readonly SplitterOptions options;

    public SplitCommand(SplitterOptions options, ILogger<SplitCommand> logger) {
        this.options = options;
        this.logger = logger;
        
        logger.LogInformation("Initialized SplitCommand with options: {Options}", options);
    }

    public string Name => SplitterOptions.SplitCommandName;

    public Task Execute(CancellationToken token = default) {
        logger.LogInformation("Starting video file split with options: {Options}", options);
        
        var videoFileName = options.VideoPath;
        if (!videoFileName.Exists) {
            logger.LogError("Video file {VideoFileName} does not exist.", videoFileName.FullName);
            throw new FileNotFoundException($"Video file {videoFileName.FullName} does not exist.", videoFileName.FullName);
        }

        var dirName = videoFileName.Name[..^videoFileName.Extension.Length];
        var outputDir = options.OutputDir.CreateSubdirectory(dirName).CreateSubdirectory("split");
        
        if (!outputDir.Exists) {
            logger.LogError("Failed to create output directory {OutputDir}.", outputDir.FullName);
            throw new IOException($"Failed to create output directory {outputDir.FullName}.");
        }

        MediaFile vidFile;
        try {
            vidFile = MediaFile.Open(
                videoFileName.FullName,
                new() {
                    StreamsToLoad = MediaMode.Video,
                    VideoPixelFormat = ImagePixelFormat.Rgba32,
                    DecoderThreads = options.MaxProcessors,
                }
            );
        } catch (Exception ex) {
            throw new IOException("Failed to open video file.", ex);
        }

        if (vidFile is null) {
            logger.LogError("Failed to open video file {VideoFileName}.", videoFileName.FullName);
            throw new IOException($"Failed to open video file {videoFileName.FullName}.");
        }

        using var video = vidFile;
    }
}
