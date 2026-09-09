using Shuttle.Fhm.Serde.Domain.Binary;
using System.Buffers.Binary;

namespace Shuttle.Fhm.Serde.Domain.Files;

internal static class FhmFileCodecs
{
    internal static bool IsDocumentedPath(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/');
        return normalized.Equals("info.dat", StringComparison.OrdinalIgnoreCase) ||
            normalized is "shot_type_mod.dat" or "tactical_settings_mod.dat" ||
            normalized.Equals("tactic_templates.dat", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("team_tactics.dat", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("leagues.dat", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("tactics.dat", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("zone_event_mod.dat", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("game_settings.dat", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("names.dat", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("player_roles.dat", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("players.dat", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("personal.dat", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("teams.dat", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("stored_lines.dat", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("trade.dat", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("trade_history.dat", StringComparison.OrdinalIgnoreCase) ||
            (normalized.StartsWith("set_play_", StringComparison.OrdinalIgnoreCase) &&
             normalized.EndsWith(".dat", StringComparison.OrdinalIgnoreCase));
    }

    internal static IFhmSaveFile? TryRead(string relativePath, Stream content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var normalized = relativePath.Replace('\\', '/');
        if (string.Equals(normalized, "info.dat", StringComparison.OrdinalIgnoreCase))
        {
            return FhmInfoFile.Read(content);
        }

        if (normalized is "shot_type_mod.dat" or "tactical_settings_mod.dat")
        {
            return FhmLengthPrefixedCatalogueFile.Read(normalized, hasCount: false, content);
        }

        if (string.Equals(normalized, "tactic_templates.dat", StringComparison.OrdinalIgnoreCase))
        {
            return FhmTacticTemplatesFile.Read(content);
        }

        if (string.Equals(normalized, "team_tactics.dat", StringComparison.OrdinalIgnoreCase))
        {
            return FhmTeamTacticsFile.Read(content);
        }

        if (string.Equals(normalized, "leagues.dat", StringComparison.OrdinalIgnoreCase))
        {
            using var leaguesStream = new MemoryStream();
            content.CopyTo(leaguesStream);
            var leaguesContent = leaguesStream.ToArray();
            if (FhmLeaguesFile.HasMultipleRecords(leaguesContent))
            {
                return new FhmOpaqueDocumentedFile(
                    normalized,
                    BinaryPrimitives.ReadInt32BigEndian(leaguesContent),
                    leaguesContent[sizeof(int)..]);
            }

            using var bufferedLeaguesStream = new MemoryStream(leaguesContent, writable: false);
            return FhmLeaguesFile.Read(bufferedLeaguesStream);
        }

        if (string.Equals(normalized, "tactics.dat", StringComparison.OrdinalIgnoreCase))
        {
            return FhmTacticsFile.Read(content);
        }

        if (string.Equals(normalized, "zone_event_mod.dat", StringComparison.OrdinalIgnoreCase))
        {
            return FhmZoneEventModifiersFile.Read(content);
        }

        if (string.Equals(normalized, "game_settings.dat", StringComparison.OrdinalIgnoreCase))
        {
            return FhmGameSettingsFile.Read(content);
        }

        if (string.Equals(normalized, "names.dat", StringComparison.OrdinalIgnoreCase))
        {
            return FhmNamesFile.Read(content);
        }

        if (string.Equals(normalized, "player_roles.dat", StringComparison.OrdinalIgnoreCase))
        {
            return FhmPlayerRolesFile.Read(content);
        }

        if (string.Equals(normalized, "players.dat", StringComparison.OrdinalIgnoreCase))
        {
            return FhmPlayersFile.Read(content);
        }

        if (string.Equals(normalized, "personal.dat", StringComparison.OrdinalIgnoreCase))
        {
            return FhmPersonnelFile.Read(content);
        }

        if (string.Equals(normalized, "teams.dat", StringComparison.OrdinalIgnoreCase))
        {
            return FhmTeamsFile.Read(content);
        }

        if (string.Equals(normalized, "stored_lines.dat", StringComparison.OrdinalIgnoreCase))
        {
            return FhmStoredLinesFile.Read(content);
        }

        if (string.Equals(normalized, "trade.dat", StringComparison.OrdinalIgnoreCase))
        {
            return FhmTradeFile.Read(content);
        }

        if (string.Equals(normalized, "trade_history.dat", StringComparison.OrdinalIgnoreCase))
        {
            using var tradeHistoryStream = new MemoryStream();
            content.CopyTo(tradeHistoryStream);
            var tradeHistoryContent = tradeHistoryStream.ToArray();
            using var bufferedTradeHistoryStream = new MemoryStream(tradeHistoryContent, writable: false);
            try
            {
                return FhmTradeHistoryFile.Read(bufferedTradeHistoryStream);
            }
            catch (FhmUnsupportedFormatException)
            {
                return new FhmOpaqueDocumentedFile(
                    normalized,
                    BinaryPrimitives.ReadInt32BigEndian(tradeHistoryContent),
                    tradeHistoryContent[sizeof(int)..]);
            }
        }

        if (normalized.StartsWith("set_play_", StringComparison.OrdinalIgnoreCase) &&
            normalized.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
        {
            return FhmSetPlayFile.Read(normalized, content);
        }

        return null;
    }
}
