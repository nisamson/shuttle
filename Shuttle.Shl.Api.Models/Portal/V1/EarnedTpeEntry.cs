using System.Text.Json;
using System.Text.Json.Serialization;
using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.Shl.Api.Models.Portal.V1;

/// <summary>
/// A single per-player, per-season TPE-earning breakdown, as returned by the portal
/// <c>GET /analytics/earned-tpe</c> endpoint. Each entry reports how much TPE a player earned in a
/// given <see cref="Season"/>, both in total (<see cref="EarnedTpe"/>) and broken down by source
/// (activity check, training, PT, etc.). Breakdown fields and <see cref="ShlRightsTeamId"/> are
/// nullable because the upstream API omits/nulls them when they do not apply.
/// </summary>
/// <param name="PlayerUpdateId">The portal player update id (<c>playerUpdateID</c>).</param>
/// <param name="Season">The season the TPE was earned in.</param>
/// <param name="Name">The player's name.</param>
/// <param name="Position">The player's position.</param>
/// <param name="CurrentLeague">The league the player is currently in, if any.</param>
/// <param name="CurrentTeamId">The id of the team the player is currently on, if any.</param>
/// <param name="Status">The player's status (e.g. active, retired).</param>
/// <param name="DraftSeason">The season the player was drafted in, if any.</param>
/// <param name="UserId">The user id of the member behind the player.</param>
/// <param name="Username">The username of the member behind the player.</param>
/// <param name="EarnedTpe">The total TPE the player earned in <see cref="Season"/>.</param>
/// <param name="Rank">The player's rank by earned TPE within the queried set.</param>
/// <param name="ShlRightsTeamId">The id of the SHL team that holds the player's rights, if any.</param>
/// <param name="Regression">TPE lost to regression, if any.</param>
/// <param name="ActivityCheck">TPE earned from activity checks, if any.</param>
/// <param name="Training">TPE earned from training, if any.</param>
/// <param name="TrainingCamp">TPE earned from training camp, if any.</param>
/// <param name="Coaching">TPE earned from coaching, if any.</param>
/// <param name="Pt">TPE earned from point tasks (PTs), if any.</param>
/// <param name="Fantasy">TPE earned from fantasy, if any.</param>
/// <param name="Recruitment">TPE earned from recruitment, if any.</param>
/// <param name="Correction">TPE adjustments from corrections, if any.</param>
/// <param name="Other">TPE earned from other sources, if any.</param>
public record EarnedTpeEntry(
    [property: JsonPropertyName("playerUpdateID")]
    int PlayerUpdateId,
    int Season,
    string Name,
    PlayerPosition Position,
    [property: JsonConverter(typeof(NullableLeagueAbbreviationConverter))]
    KnownLeague? CurrentLeague,
    [property: JsonPropertyName("currentTeamID")]
    int? CurrentTeamId,
    PlayerStatus Status,
    int? DraftSeason,
    [property: JsonPropertyName("userID")]
    int UserId,
    string Username,
    [property: JsonPropertyName("earnedTPE")]
    int EarnedTpe,
    int Rank,
    [property: JsonPropertyName("shlRightsTeamID")]
    int? ShlRightsTeamId = null,
    int? Regression = null,
    [property: JsonPropertyName("activitycheck")]
    int? ActivityCheck = null,
    int? Training = null,
    [property: JsonPropertyName("trainingcamp")]
    int? TrainingCamp = null,
    int? Coaching = null,
    int? Pt = null,
    int? Fantasy = null,
    int? Recruitment = null,
    int? Correction = null,
    int? Other = null
);

/// <summary>
/// Deserializes the analytics endpoint's <c>currentLeague</c> field into a nullable
/// <see cref="KnownLeague"/>. The endpoint sends the league abbreviation (<c>"SHL"</c>/<c>"SMJHL"</c>),
/// JSON <c>null</c>, or — for players with no current league — an empty string; all of the latter two
/// (and whitespace) map to <see langword="null"/>.
/// </summary>
public class NullableLeagueAbbreviationConverter : JsonConverter<KnownLeague?> {
    public override KnownLeague? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.Null) {
            return null;
        }

        var text = reader.GetString();
        if (string.IsNullOrWhiteSpace(text)) {
            return null;
        }

        if (KnownLeague.TryFromAbbreviation(text.Trim(), out var league)) {
            return league;
        }

        throw new JsonException($"Unknown league abbreviation: '{text}'");
    }

    public override void Write(Utf8JsonWriter writer, KnownLeague? value, JsonSerializerOptions options) {
        if (value is { } league) {
            writer.WriteStringValue(league.Abbreviation);
        } else {
            writer.WriteNullValue();
        }
    }
}
