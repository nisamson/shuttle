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

var outputOption = new Option<FileInfo>("--output", "-o") {
    Description = "Path of the JSON file to write the player information to.",
    DefaultValueFactory = _ => new FileInfo("player-information.json"),
};

var databaseOption = new Option<string?>("--database", "-d") {
    Description = "Overrides the SHUTTLESQLSERVER_DATABASE database (catalog) name to read from.",
};

var prettyOption = new Option<bool>("--pretty") {
    Description = "Write indented, human-readable JSON (default: true).",
    DefaultValueFactory = _ => true,
};

var downloadCommand = new Command(
    "download-players",
    "Download the current PlayerInformation table into a local JSON file."
) {
    outputOption,
    databaseOption,
    prettyOption,
};
downloadCommand.Aliases.Add("download-player-information");

downloadCommand.SetAction((parseResult, cancellationToken) => {
    var output = parseResult.GetValue(outputOption)!;
    var database = parseResult.GetValue(databaseOption);
    var pretty = parseResult.GetValue(prettyOption);
    return PlayerInformationExporter.RunAsync(output, database, pretty, cancellationToken);
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
