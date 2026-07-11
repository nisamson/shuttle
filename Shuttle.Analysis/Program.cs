// Shuttle.Analysis — command-line tools for offline analysis of the Shuttle database.
//
// Requires the same environment configuration as the server:
//   SHUTTLESQLSERVER_DATABASE  - the Azure SQL database (catalog) name
//   SHUTTLESQLSERVER_HOST      - the Azure SQL server host
// These may be supplied via the environment or the shared Shuttle.EFCore/.env file (loaded on
// demand by each command). Azure SQL uses ActiveDirectoryDefault auth, so a signed-in Azure
// identity (e.g. `az login`) that can access the database is also required.

using System.CommandLine;
using Shuttle.Analysis;
using Shuttle.Analysis.Flows;
using Shuttle.Shl.Api.Models.Common;

var outputOption = new Option<FileInfo?>("--output", "-o") {
    Description = "Path of the file to write. Defaults to player-information.<json|csv> based on the format.",
};

var databaseOption = new Option<string?>("--database", "-d") {
    Description = "Overrides the SHUTTLESQLSERVER_DATABASE database (catalog) name to read from.",
};

var formatOption = new Option<ExportFormat?>("--format", "-f") {
    Description = "Output format (json or csv). Defaults to the --output extension (.csv => csv), otherwise json.",
};

var normOption = new Option<StatNorm>("--norm", "-n") {
    Description = "Replace each player's stat attributes in place with a normalized form of their vector "
                  + "(none, l1, or l2). Default: none (raw values).",
    DefaultValueFactory = _ => StatNorm.None,
};

var positionsOption = new Option<IReadOnlySet<PlayerPosition>?>("--positions", "-p") {
    Description = "Filter to a comma-separated list of shorthand positions (G, C, LW, RW, LD, RD). "
                  + "Group aliases: F = forwards (C,LW,RW), D = defense (LD,RD). Case-insensitive; "
                  + "omit to export all players.",
    CustomParser = result => {
        var spec = string.Join(',', result.Tokens.Select(t => t.Value));
        if (PositionFilter.TryParse(spec, out var positions, out var error)) {
            return positions;
        }

        result.AddError(error!);
        return null;
    },
};

var prettyOption = new Option<bool>("--pretty") {
    Description = "Write indented, human-readable JSON (ignored for CSV; default: true).",
    DefaultValueFactory = _ => true,
};

var downloadCommand = new Command(
    "download-players",
    "Download the current PlayerInformation table into a local JSON or CSV file."
) {
    outputOption,
    databaseOption,
    formatOption,
    normOption,
    positionsOption,
    prettyOption,
};
downloadCommand.Aliases.Add("download-player-information");

downloadCommand.SetAction((parseResult, cancellationToken) => {
    var output = parseResult.GetValue(outputOption);
    var explicitFormat = parseResult.GetValue(formatOption);
    var database = parseResult.GetValue(databaseOption);
    var norm = parseResult.GetValue(normOption);
    var positions = parseResult.GetValue(positionsOption);
    var pretty = parseResult.GetValue(prettyOption);

    var format = explicitFormat
        ?? (output is not null && string.Equals(output.Extension, ".csv", StringComparison.OrdinalIgnoreCase)
            ? ExportFormat.Csv
            : ExportFormat.Json);
    output ??= new FileInfo(format == ExportFormat.Csv ? "player-information.csv" : "player-information.json");

    return PlayerInformationExporter.RunAsync(output, database, format, norm, positions, pretty, cancellationToken);
});

var rootCommand = new RootCommand("Shuttle data-analysis command-line tools.") {
    downloadCommand,
};

var analysisRegistry = AnalysisFlowRegistry.CreateDefault();

var flowOption = new Option<string?>("--flow", "-f") {
    Description = "Name of the analysis flow to run. Use --list to see the available flows.",
};

var flowInputOption = new Option<FileInfo?>("--input", "-i") {
    Description = "Path of the CSV data file (produced by download-players) to ingest and analyze.",
};

var flowOutputOption = new Option<DirectoryInfo?>("--output", "-o") {
    Description = "Directory for any artifacts the flow produces. Defaults to ./analysis-output.",
};

var listFlowsOption = new Option<bool>("--list") {
    Description = "List the available analysis flows and exit.",
};

var flowArgOption = new Option<string[]>("--arg", "-a") {
    Description = "Flow-specific argument as key=value. Repeat to pass multiple (e.g. --arg k=3 --arg seed=42).",
    Arity = ArgumentArity.ZeroOrMore,
    AllowMultipleArgumentsPerToken = true,
};

var analyzeCommand = new Command(
    "analyze",
    "Ingest a data file and run it through a named analysis flow."
) {
    flowOption,
    flowInputOption,
    flowOutputOption,
    listFlowsOption,
    flowArgOption,
};

analyzeCommand.SetAction((parseResult, cancellationToken) => {
    if (parseResult.GetValue(listFlowsOption)) {
        if (analysisRegistry.Flows.Count == 0) {
            Console.WriteLine("No analysis flows are registered.");
        } else {
            Console.WriteLine("Available analysis flows:");
            foreach (var registered in analysisRegistry.Flows) {
                Console.WriteLine($"  {registered.Name,-24} {registered.Description}");
            }
        }

        return Task.FromResult(0);
    }

    var flow = parseResult.GetValue(flowOption);
    if (string.IsNullOrWhiteSpace(flow)) {
        Console.Error.WriteLine("A flow name is required. Pass --flow <name> or --list to see the options.");
        return Task.FromResult(1);
    }

    var input = parseResult.GetValue(flowInputOption);
    if (input is null) {
        Console.Error.WriteLine("An input file is required. Pass --input <file>.");
        return Task.FromResult(1);
    }

    var output = parseResult.GetValue(flowOutputOption) ?? new DirectoryInfo("analysis-output");

    IReadOnlyDictionary<string, string> arguments;
    try {
        arguments = FlowArguments.Parse(parseResult.GetValue(flowArgOption));
    } catch (FormatException ex) {
        Console.Error.WriteLine(ex.Message);
        return Task.FromResult(1);
    }

    return AnalysisFlowRunner.RunAsync(flow, input, output, analysisRegistry, arguments, cancellationToken);
});

rootCommand.Subcommands.Add(analyzeCommand);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => {
    e.Cancel = true;
    cts.Cancel();
};

return await rootCommand.Parse(args).InvokeAsync(cancellationToken: cts.Token);
