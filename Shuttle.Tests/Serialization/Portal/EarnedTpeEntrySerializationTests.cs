using System.Text.Json;
using Shuttle.Shl.Api.Client;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;

namespace Shuttle.Tests.Serialization.Portal;

/// <summary>
/// Deserialization tests for <see cref="EarnedTpeEntry"/> against the shape returned by the portal
/// <c>GET /analytics/earned-tpe</c> endpoint, using the client's
/// <see cref="ShlConstants.JsonSerializerOptions"/>. Guards long-form <c>position</c> parsing,
/// <c>currentLeague</c>/<c>status</c> mapping, the case-insensitive <c>earnedTPE</c> mapping, and the
/// nullable TPE-breakdown fields (which the upstream API returns as <c>null</c> when not applicable).
/// </summary>
public class EarnedTpeEntrySerializationTests {
    private const string Json = """
        [
            {"playerUpdateID":2377,"season":89,"name":"Booksy Jefferson","position":"Right Wing","currentLeague":"SMJHL","currentTeamID":5,"shlRightsTeamID":5,"status":"active","draftSeason":87,"userID":21792,"username":"BigHops","earnedTPE":118,"rank":10,"regression":null,"activitycheck":10,"training":25,"trainingcamp":12,"coaching":28,"pt":39,"fantasy":4,"recruitment":null,"correction":null,"other":null},
            {"playerUpdateID":1700,"season":89,"name":"Shiny Rainbow","position":"Right Wing","currentLeague":"SHL","currentTeamID":5,"shlRightsTeamID":5,"status":"active","draftSeason":80,"userID":12707,"username":"Marshi","earnedTPE":116,"rank":12,"regression":-187,"activitycheck":10,"training":25,"trainingcamp":6,"coaching":28,"pt":39,"fantasy":8,"recruitment":null,"correction":null,"other":null},
            {"playerUpdateID":2349,"season":89,"name":"Matthew Fox","position":"Right Defense","currentLeague":"SHL","currentTeamID":5,"shlRightsTeamID":null,"status":"active","draftSeason":86,"userID":2227,"username":"NYRangers","earnedTPE":114,"rank":14,"regression":null,"activitycheck":10,"training":25,"trainingcamp":12,"coaching":28,"pt":39,"fantasy":null,"recruitment":null,"correction":null,"other":null},
            {"playerUpdateID":1218,"season":75,"name":"Troy McClure IIII","position":"Right Defense","currentLeague":"","currentTeamID":null,"shlRightsTeamID":null,"status":"retired","draftSeason":null,"userID":2969,"username":"Troy_McClure03","earnedTPE":40,"rank":100,"regression":null,"activitycheck":null,"training":null,"trainingcamp":null,"coaching":null,"pt":null,"fantasy":null,"recruitment":null,"correction":null,"other":null},
            {"playerUpdateID":999,"season":74,"name":"Old Timer","position":"Goalie","currentLeague":null,"currentTeamID":null,"shlRightsTeamID":null,"status":"retired","draftSeason":null,"userID":111,"username":"oldtimer","earnedTPE":12,"rank":200,"regression":null,"activitycheck":null,"training":null,"trainingcamp":null,"coaching":null,"pt":null,"fantasy":null,"recruitment":null,"correction":null,"other":null}
        ]
        """;

    [Fact]
    public void DeserializesEarnedTpeEntries() {
        var entries = JsonSerializer.Deserialize<List<EarnedTpeEntry>>(Json, ShlConstants.JsonSerializerOptions);

        Assert.NotNull(entries);
        Assert.Equal(5, entries!.Count);

        var first = entries[0];
        Assert.Equal(2377, first.PlayerUpdateId);
        Assert.Equal(89, first.Season);
        Assert.Equal("Booksy Jefferson", first.Name);
        Assert.Equal(PlayerPosition.RightWing, first.Position);
        Assert.Equal(KnownLeague.Smjhl, first.CurrentLeague);
        Assert.Equal(5, first.CurrentTeamId);
        Assert.Equal(5, first.ShlRightsTeamId);
        Assert.Equal(PlayerStatus.Active, first.Status);
        Assert.Equal(87, first.DraftSeason);
        Assert.Equal(21792, first.UserId);
        Assert.Equal("BigHops", first.Username);
        Assert.Equal(118, first.EarnedTpe);
        Assert.Equal(10, first.Rank);
        // Breakdown: present and absent values.
        Assert.Null(first.Regression);
        Assert.Equal(10, first.ActivityCheck);
        Assert.Equal(25, first.Training);
        Assert.Equal(12, first.TrainingCamp);
        Assert.Equal(28, first.Coaching);
        Assert.Equal(39, first.Pt);
        Assert.Equal(4, first.Fantasy);
        Assert.Null(first.Recruitment);
        Assert.Null(first.Correction);
        Assert.Null(first.Other);

        // Negative regression is preserved.
        Assert.Equal(-187, entries[1].Regression);

        // Nullable rights-team id is honoured.
        Assert.Null(entries[2].ShlRightsTeamId);
        Assert.Equal(PlayerPosition.RightDefense, entries[2].Position);
        Assert.Equal(KnownLeague.Shl, entries[2].CurrentLeague);

        // A long-retired/undrafted player: current-state fields are null/empty in the full dataset.
        // The analytics endpoint sends currentLeague as an empty string (not null) for these rows.
        var retired = entries[3];
        Assert.Null(retired.CurrentLeague);
        Assert.Null(retired.CurrentTeamId);
        Assert.Null(retired.DraftSeason);
        Assert.Equal(PlayerStatus.Retired, retired.Status);
        Assert.Equal(40, retired.EarnedTpe);

        // A JSON null currentLeague also maps to null.
        Assert.Null(entries[4].CurrentLeague);
    }
}
