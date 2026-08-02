using System.CommandLine;
using Shuttle.EloCalc.Sinks;

namespace Shuttle.EloCalc;

public record CliOptions() {
    public SinkFormat SinkFormat { get; private set; } = SinkFormat.Csv;

    public string? OutputFile { get; private set; } = "";

    public List<int> Seasons { get; private set; } = [];

    public string League { get; private set; } = "SHL";

    public static CliOptions Parse(string[] args) {
        var options = new CliOptions();
        
        var formatOption = new Option<SinkFormat>(
            "--format"
        ) {
            DefaultValueFactory = _ => SinkFormat.Csv,
            Description = "The output format for the results sink (Csv or Sqlite).",
        };
        
        var outputFileOption = new Option<string?>(
            "--output-file"
        ) {
            Description = "The output file path for the results sink.",
            DefaultValueFactory = _ => null,
        };

        var seasonsValue = new Argument<List<int>>("seasons") {
            Arity = ArgumentArity.OneOrMore,
            Description = "The SHL/SMJHL seasons to process (e.g. 86).",
        };
        
        var leagueOption = new Option<string>(
            "--league"
        ) {
            Description = "The league to process (SHL or SMJHL).",
            DefaultValueFactory = _ => "SHL",
        };
        
        var rootCommand = new RootCommand("Elo Calculator for SHL/SMJHL games")
        {
            formatOption,
            outputFileOption,
            leagueOption,
            seasonsValue,
        };
        
        var parseResult = rootCommand.Parse(args);
        options.SinkFormat = parseResult.GetRequiredValue(formatOption);
        options.OutputFile = parseResult.GetRequiredValue(outputFileOption);
        options.League = parseResult.GetRequiredValue(leagueOption);
        options.Seasons = parseResult.GetRequiredValue(seasonsValue);
        return options;
    }
}
