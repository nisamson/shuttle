using Shuttle.Fhm.SaveData.Binary;

namespace Shuttle.Fhm.SaveData.Files;

internal static class FhmFileCodecs
{
    internal static IFhmSaveFile? TryRead(string relativePath, byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var normalized = relativePath.Replace('\\', '/');
        using var stream = new MemoryStream(content, writable: false);
        using var reader = new FhmBinaryReader(stream, leaveOpen: true);
        if (normalized.StartsWith("set_play_", StringComparison.OrdinalIgnoreCase) &&
            normalized.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
        {
            return FhmSetPlayFile.Read(normalized, reader);
        }

        return normalized.ToLowerInvariant() switch
        {
            "info.dat" => FhmInfoFile.Read(reader),
            "names.dat" => FhmNamesFile.Read(reader),
            "player_roles.dat" => FhmPlayerRolesFile.Read(reader),
            "team_tactics.dat" => FhmTeamTacticsFile.Read(reader),
            "stored_lines.dat" => FhmStoredLinesFile.Read(reader),
            "leagues.dat" => FhmLeaguesFile.Read(reader),
            "tactic_templates.dat" => FhmTacticTemplatesFile.Read(reader),
            "shot_type_mod.dat" => FhmLengthPrefixedCatalogueFile.Read(normalized, hasCount: false, reader),
            "tactical_settings_mod.dat" => FhmLengthPrefixedCatalogueFile.Read(normalized, hasCount: false, reader),
            "players.dat" => FhmPlayersFile.Read(reader),
            "teams.dat" => FhmTeamsFile.Read(reader),
            "trade.dat" => FhmTradeFile.Read(reader),
            "trade_history.dat" => FhmTradeHistoryFile.Read(reader),
            "tactics.dat" => FhmTacticsFile.Read(reader),
            "zone_event_mod.dat" => FhmZoneEventModifiersFile.Read(reader),
            "game_settings.dat" => FhmGameSettingsFile.Read(reader),
            _ => null,
        };
    }

}
