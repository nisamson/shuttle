using System.CommandLine;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Shuttle.Fhm.Serde.Domain.SaveFolder;
using Shuttle.Fhm.Serde.Sqlite;

var importSourceOption = new Option<DirectoryInfo>("--source", "-s")
{
    Description = "Path to the FHM save folder to import.",
    Required = true,
};

var importOutputOption = new Option<FileInfo>("--output", "-o")
{
    Description = "Path of the SQLite database to create.",
    Required = true,
};

var importOverwriteOption = new Option<bool>("--overwrite")
{
    Description = "Replace an existing SQLite adapter database at --output.",
};

var importTimingsOption = new Option<bool>("--timings")
{
    Description = "Print elapsed time for each import phase.",
};

var importProgressOption = new Option<bool>("--progress")
{
    Description = "Print each source file as it is read and decoded.",
};

var importCommand = new Command("import", "Create a SQLite database from an FHM 10 save folder.")
{
    importSourceOption,
    importOutputOption,
    importOverwriteOption,
    importTimingsOption,
    importProgressOption,
};

importCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var source = parseResult.GetValue(importSourceOption)!;
    var output = parseResult.GetValue(importOutputOption)!;
    var overwrite = parseResult.GetValue(importOverwriteOption);
    var timings = parseResult.GetValue(importTimingsOption);
    var progress = parseResult.GetValue(importProgressOption);

    if (!source.Exists)
    {
        Console.Error.WriteLine($"FHM save folder not found: {source.FullName}");
        return 1;
    }

    try
    {
        await new FhmSaveSqliteWriter().WriteFromDirectoryAsync(
            source.FullName,
            output.FullName,
            new FhmSaveSqliteWriteOptions
            {
                Overwrite = overwrite,
                Progress = timings
                    ? new Progress<FhmSaveSqliteWriteProgress>(progress =>
                        Console.WriteLine($"{progress.Phase}: {progress.Elapsed}"))
                    : null,
                SourceProgress = progress
                    ? new Progress<FhmSaveReadProgress>(update =>
                        Console.WriteLine($"{update.Phase}: {update.RelativePath}"))
                    : null,
            },
            cancellationToken);

        Console.WriteLine($"Created SQLite save database: {output.FullName}");
        return 0;
    }
    catch (Exception ex) when (ex is IOException or InvalidDataException or FormatException or SqliteException)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
});

var exportSourceOption = new Option<FileInfo>("--source", "-s")
{
    Description = "Path to the SQLite adapter database to export.",
    Required = true,
};

var exportOutputOption = new Option<DirectoryInfo>("--output", "-o")
{
    Description = "New or empty FHM save folder to create.",
    Required = true,
};

var exportTimingsOption = new Option<bool>("--timings")
{
    Description = "Print elapsed time for direct SQLite export phases.",
};

var exportCommand = new Command("export", "Create an FHM 10 save folder from a SQLite adapter database.")
{
    exportSourceOption,
    exportOutputOption,
    exportTimingsOption,
};

exportCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var source = parseResult.GetValue(exportSourceOption)!;
    var output = parseResult.GetValue(exportOutputOption)!;
    var timings = parseResult.GetValue(exportTimingsOption);

    if (!source.Exists)
    {
        Console.Error.WriteLine($"SQLite save database not found: {source.FullName}");
        return 1;
    }

    try
    {
        await new FhmSaveSqliteReader().ExportAsync(
            source.FullName,
            output.FullName,
            new FhmSaveSqliteExportOptions
            {
                Progress = timings
                    ? new Progress<FhmSaveSqliteExportProgress>(progress =>
                        Console.WriteLine($"{progress.Phase}: {progress.Elapsed}"))
                    : null,
            },
            cancellationToken);

        Console.WriteLine($"Created FHM save folder: {output.FullName}");
        return 0;
    }
    catch (Exception ex) when (ex is IOException or InvalidDataException or FormatException or SqliteException)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
});

var teamReportSourceOption = new Option<FileInfo>("--source", "-s")
{
    Description = "Path to the SQLite adapter database to report on.",
    Required = true,
};

var teamReportTeamOption = new Option<string>("--team", "-t")
{
    Description = "Exact displayed team name, such as 'Original City Originals'.",
};

var teamReportAllOption = new Option<bool>("--all")
{
    Description = "Write a report for every team.",
};

var teamReportOutputOption = new Option<DirectoryInfo>("--output", "-o")
{
    Description = "Directory that receives one JSON report per team when using --all.",
};

var teamReportOverwriteOption = new Option<bool>("--overwrite")
{
    Description = "Replace existing report files when using --all.",
};

var teamReportCommand = new Command("team-report", "Write a human-readable JSON report for one team.")
{
    teamReportSourceOption,
    teamReportTeamOption,
    teamReportAllOption,
    teamReportOutputOption,
    teamReportOverwriteOption,
};

teamReportCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var source = parseResult.GetValue(teamReportSourceOption)!;
    var teamName = parseResult.GetValue(teamReportTeamOption);
    var allTeams = parseResult.GetValue(teamReportAllOption);
    var output = parseResult.GetValue(teamReportOutputOption);
    var overwrite = parseResult.GetValue(teamReportOverwriteOption);
    if (!source.Exists)
    {
        Console.Error.WriteLine($"SQLite save database not found: {source.FullName}");
        return 1;
    }

    if (allTeams == !string.IsNullOrWhiteSpace(teamName))
    {
        Console.Error.WriteLine("Specify exactly one of --team or --all.");
        return 1;
    }

    if (allTeams && output is null)
    {
        Console.Error.WriteLine("--output is required with --all.");
        return 1;
    }

    if (!allTeams && output is not null)
    {
        Console.Error.WriteLine("--output can only be used with --all.");
        return 1;
    }

    try
    {
        var reportService = new TeamReportService();
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

        if (allTeams)
        {
            Directory.CreateDirectory(output!.FullName);
            var reports = await reportService.GetAllAsync(source.FullName, cancellationToken);
            foreach (var report in reports)
            {
                var destination = Path.Combine(output.FullName, $"{CreateReportFileName(report.Team.Name)}.json");
                if (!overwrite && File.Exists(destination))
                {
                    throw new IOException($"Report file '{destination}' already exists. Set --overwrite to replace it.");
                }

                await using var stream = new FileStream(
                    destination,
                    overwrite ? FileMode.Create : FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);
                await JsonSerializer.SerializeAsync(stream, report, serializerOptions, cancellationToken);
            }

            Console.WriteLine($"Exported {reports.Count} team reports to {output.FullName}");
            return 0;
        }

        var teamReport = await reportService.GetAsync(source.FullName, teamName!, cancellationToken);
        await JsonSerializer.SerializeAsync(Console.OpenStandardOutput(), teamReport, serializerOptions, cancellationToken);
        await Console.Out.WriteLineAsync();
        return 0;
    }
    catch (Exception ex) when (ex is IOException or InvalidDataException or FormatException or SqliteException)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
});

static string CreateReportFileName(string teamName) =>
    string.Concat(
        teamName.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));

var rootCommand = new RootCommand("Import, export, and report on FHM 10 save folders as SQLite adapter databases.")
{
    importCommand,
    exportCommand,
    teamReportCommand,
};

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

return await rootCommand.Parse(args).InvokeAsync(cancellationToken: cts.Token);
