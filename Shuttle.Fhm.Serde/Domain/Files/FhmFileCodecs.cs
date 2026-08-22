using Shuttle.Fhm.Serde.Domain.Binary;

namespace Shuttle.Fhm.Serde.Domain.Files;

internal static class FhmFileCodecs
{
    internal static IFhmSaveFile? TryRead(string relativePath, byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var normalized = relativePath.Replace('\\', '/');
        if (string.Equals(normalized, "info.dat", StringComparison.OrdinalIgnoreCase))
        {
            using var infoStream = new MemoryStream(content, writable: false);
            return FhmInfoFile.Read(infoStream);
        }

        if (normalized is "shot_type_mod.dat" or "tactical_settings_mod.dat")
        {
            using var catalogueStream = new MemoryStream(content, writable: false);
            return FhmLengthPrefixedCatalogueFile.Read(normalized, hasCount: false, catalogueStream);
        }

        if (string.Equals(normalized, "tactic_templates.dat", StringComparison.OrdinalIgnoreCase))
        {
            using var templatesStream = new MemoryStream(content, writable: false);
            return FhmTacticTemplatesFile.Read(templatesStream);
        }

        if (string.Equals(normalized, "team_tactics.dat", StringComparison.OrdinalIgnoreCase))
        {
            using var teamTacticsStream = new MemoryStream(content, writable: false);
            return FhmTeamTacticsFile.Read(teamTacticsStream);
        }

        if (string.Equals(normalized, "leagues.dat", StringComparison.OrdinalIgnoreCase))
        {
            using var leaguesStream = new MemoryStream(content, writable: false);
            return FhmLeaguesFile.Read(leaguesStream);
        }

        if (string.Equals(normalized, "tactics.dat", StringComparison.OrdinalIgnoreCase))
        {
            using var tacticsStream = new MemoryStream(content, writable: false);
            return FhmTacticsFile.Read(tacticsStream);
        }

        if (string.Equals(normalized, "zone_event_mod.dat", StringComparison.OrdinalIgnoreCase))
        {
            using var zoneEventModifiersStream = new MemoryStream(content, writable: false);
            return FhmZoneEventModifiersFile.Read(zoneEventModifiersStream);
        }

        if (string.Equals(normalized, "game_settings.dat", StringComparison.OrdinalIgnoreCase))
        {
            using var gameSettingsStream = new MemoryStream(content, writable: false);
            return FhmGameSettingsFile.Read(gameSettingsStream);
        }

        if (string.Equals(normalized, "names.dat", StringComparison.OrdinalIgnoreCase))
        {
            using var namesStream = new MemoryStream(content, writable: false);
            return FhmNamesFile.Read(namesStream);
        }

        if (string.Equals(normalized, "player_roles.dat", StringComparison.OrdinalIgnoreCase))
        {
            using var playerRolesStream = new MemoryStream(content, writable: false);
            return FhmPlayerRolesFile.Read(playerRolesStream);
        }

        if (string.Equals(normalized, "players.dat", StringComparison.OrdinalIgnoreCase))
        {
            using var playersStream = new MemoryStream(content, writable: false);
            return FhmPlayersFile.Read(playersStream);
        }

        if (string.Equals(normalized, "personal.dat", StringComparison.OrdinalIgnoreCase))
        {
            using var personnelStream = new MemoryStream(content, writable: false);
            return FhmPersonnelFile.Read(personnelStream);
        }

        if (string.Equals(normalized, "teams.dat", StringComparison.OrdinalIgnoreCase))
        {
            using var teamsStream = new MemoryStream(content, writable: false);
            return FhmTeamsFile.Read(teamsStream);
        }

        if (string.Equals(normalized, "stored_lines.dat", StringComparison.OrdinalIgnoreCase))
        {
            using var storedLinesStream = new MemoryStream(content, writable: false);
            return FhmStoredLinesFile.Read(storedLinesStream);
        }

        if (string.Equals(normalized, "trade.dat", StringComparison.OrdinalIgnoreCase))
        {
            using var tradeStream = new MemoryStream(content, writable: false);
            return FhmTradeFile.Read(tradeStream);
        }

        if (string.Equals(normalized, "trade_history.dat", StringComparison.OrdinalIgnoreCase))
        {
            using var tradeHistoryStream = new MemoryStream(content, writable: false);
            return FhmTradeHistoryFile.Read(tradeHistoryStream);
        }

        if (normalized.StartsWith("set_play_", StringComparison.OrdinalIgnoreCase) &&
            normalized.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
        {
            using var setPlayStream = new MemoryStream(content, writable: false);
            return FhmSetPlayFile.Read(normalized, setPlayStream);
        }

        return null;
    }
}
