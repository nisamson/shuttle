// Shuttle.Fhm.Vision — Windows-only tool that captures Franchise Hockey Manager (FHM) player-info
// screens and extracts their attribute and role ratings via region-based OCR into a local SQLite
// database. See README.md for usage and the calibration workflow.

using Shuttle.Fhm.Vision.Cli;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => {
    e.Cancel = true;
    cts.Cancel();
};

var rootCommand = VisionCommands.BuildRoot();
return await rootCommand.Parse(args).InvokeAsync(cancellationToken: cts.Token);
