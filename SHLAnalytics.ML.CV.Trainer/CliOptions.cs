using System.CommandLine;
using System.CommandLine.Parsing;
using SHLAnalytics.ML.CV.Trainer.Services.Dedup;

namespace SHLAnalytics.ML.CV.Trainer;

public abstract record CommonCliOptions {
    private const string LoggingPathArg = "--logging-path";
    private const string OutputDirArg = "--output-dir";
    private const string MaxProcessorsArg = "--max-processors";
    private const string OverwriteArg = "--overwrite";

    public FileInfo? LoggingPath { get; set; }
    public DirectoryInfo OutputDir { get; set; } = new ("./output");
    
    public bool Overwrite { get; set; }
    
    public int MaxProcessors { get; set; } = Environment.ProcessorCount;

    private static RootCommand CreateRoot() {
        var loggingPathOption = new Option<DirectoryInfo?>(LoggingPathArg, "-l") {
            Description =
                "The directory where logs will be stored. If not specified, logs will be written to the console.",
            Recursive = true,
        }.AcceptLegalFilePathsOnly();
        var outputDirOption = new Option<DirectoryInfo?>(OutputDirArg, "-o") {
            Description =
                "The directory where output files will be stored. If not specified, output will be written to the current directory.",
            Recursive = true,
        }.AcceptLegalFilePathsOnly();
        var maxProcessorsOption = new Option<int>(MaxProcessorsArg, "-m") {
            Description =
                "The maximum number of processors to use. Defaults to the number of processors on the machine. If set to 0, all processors will be used.",
            DefaultValueFactory = _ => Environment.ProcessorCount,
            Recursive = true,
        };
        var overwriteOption = new Option<bool>(OverwriteArg, "-D") {
            Description = "Whether to overwrite existing files in the output directory.",
            DefaultValueFactory = _ => false,
            Recursive = true,
        };
        maxProcessorsOption.Validators.Add(or => {
            var val = or.GetRequiredValue(maxProcessorsOption);
            if (val < 0) {
                or.AddError("Max processors must be non-negative.");
            }
        });

        var rootCommand = new RootCommand("SHLAnalytics ML CV Trainer") {
            loggingPathOption,
            outputDirOption,
            maxProcessorsOption,
            overwriteOption
        };
        return rootCommand;
    }

    public static CommonCliOptions FromResult(ParseResult parseResult) {
        var commandName = parseResult.CommandResult.Command.Name;
        var result = commandName switch {
            DedupOptions.DedupCommandName => DedupOptions.FromResult(parseResult.CommandResult) as CommonCliOptions,
            _ => throw new ArgumentException($"Unknown command: {parseResult.CommandResult.Command.Name}")
        } ;
        result.LoggingPath = parseResult.GetValue<FileInfo>(LoggingPathArg);
        result.OutputDir = parseResult.GetRequiredValue<DirectoryInfo>(OutputDirArg);
        result.MaxProcessors = parseResult.GetRequiredValue<int>(MaxProcessorsArg);
        if (result.MaxProcessors == 0) {
            result.MaxProcessors = Environment.ProcessorCount;
        }
        result.Overwrite = parseResult.GetValue<bool>(OverwriteArg);
        return result;
    }

    public static RootCommand CreateRootWithSubcommands() {
        var root = CreateRoot();
        DedupOptions.AddToRoot(root);
        return root;
    }
}

public record DedupOptions : CommonCliOptions, IImageHashConfiguration {
    public const string DedupCommandName = "dedup";
    private const string HashAlgorithmArg = "--hash-algorithm";
    private const string SimilarityThresholdArg = "--similarity-threshold";
    private const string HashStorageStrategyArg = "--hash-storage-strategy";

    public static void AddToRoot(RootCommand root) {
        var command = new Command(
            DedupCommandName,
            "Identifies duplicate frames in a directory of images and removes them."
        );
        var hashAlgorithmOption = new Option<HashAlgorithm>(
            HashAlgorithmArg,
            "-h"
        ) {
            Description = "The hash algorithm to use for frame comparison.",
        };
        var similarityThresholdOption = new Option<int>(
            SimilarityThresholdArg,
            "-s"
        ) {
            Description =
                "The similarity threshold for frame comparison. Frames with a hash distance below this threshold will be considered similar.",
            DefaultValueFactory = _ => 5,
        };
        var hashStorageStrategyOption = new Option<HashStorageStrategy>( 
            HashStorageStrategyArg,
            "-S"
        ) {
            Description = "The strategy to use for storing image hashes.",
            DefaultValueFactory = _ => HashStorageStrategy.InMemoryCache,
        };
        command.Options.Add(hashAlgorithmOption);
        command.Options.Add(hashAlgorithmOption);
        command.Options.Add(similarityThresholdOption);
    }


    public static DedupOptions FromResult(CommandResult commandResult) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(commandResult.Command.Name, DedupCommandName);
        var options = new DedupOptions {
            HashAlgorithm = commandResult.GetValue<HashAlgorithm>(HashAlgorithmArg),
            SimilarityThreshold = commandResult.GetValue<int>(SimilarityThresholdArg),
            HashStorageStrategy = commandResult.GetValue<HashStorageStrategy>(HashStorageStrategyArg),
        };
        return options;
    }

    public HashAlgorithm HashAlgorithm { get; set; }
    public HashStorageStrategy HashStorageStrategy { get; set; } = HashStorageStrategy.InMemoryCache;
    public int SimilarityThreshold { get; set; }
}
