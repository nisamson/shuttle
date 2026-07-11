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

var outputOption = new Option<FileInfo?>("--output", "-o") {
    Description = "Path of the file to write. Defaults to player-information.<json|csv> based on the format.",
};

var databaseOption = new Option<string?>("--database", "-d") {
    Description = "Overrides the SHUTTLESQLSERVER_DATABASE database (catalog) name to read from.",
};

var formatOption = new Option<ExportFormat?>("--format", "-f") {
    Description = "Output format (json or csv). Defaults to the --output extension (.csv => csv), otherwise json.",
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
    prettyOption,
};
downloadCommand.Aliases.Add("download-player-information");

downloadCommand.SetAction((parseResult, cancellationToken) => {
    var output = parseResult.GetValue(outputOption);
    var explicitFormat = parseResult.GetValue(formatOption);
    var database = parseResult.GetValue(databaseOption);
    var pretty = parseResult.GetValue(prettyOption);

    var format = explicitFormat
        ?? (output is not null && string.Equals(output.Extension, ".csv", StringComparison.OrdinalIgnoreCase)
            ? ExportFormat.Csv
            : ExportFormat.Json);
    output ??= new FileInfo(format == ExportFormat.Csv ? "player-information.csv" : "player-information.json");

    return PlayerInformationExporter.RunAsync(output, database, format, pretty, cancellationToken);
});

var rootCommand = new RootCommand("Shuttle data-analysis command-line tools.") {
    downloadCommand,
};

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => {
    e.Cancel = true;
    cts.Cancel();
};

return await rootCommand.Parse(args).InvokeAsync(cancellationToken: cts.Token);
