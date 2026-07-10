using System.Text.Json;
using System.Text.Json.Serialization;
using Shuttle.Shl.Api.Client;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;
using Shuttle.Tests.Utilites;
using TaskStatus = Shuttle.Shl.Api.Models.Portal.V1.TaskStatus;

namespace Shuttle.Tests.Serialization.Portal;

/// <summary>
/// Regression tests guarding against Refit's default <see cref="JsonSerializerOptions"/> hijacking the
/// model enums. Refit registers a global <see cref="JsonStringEnumConverter"/> in the converters
/// collection, which takes precedence over the type-level <c>[JsonConverter]</c> attributes and cannot
/// parse enum string values that contain spaces/slashes (e.g. "Right Defense", "SMJHL Rookie",
/// "SHL/Send-down"). The SHL API clients therefore use plain <see cref="JsonSerializerDefaults.Web"/>
/// options via <see cref="ShlConstants.JsonSerializerOptions"/>.
/// </summary>
public class PlayerInfoRefitOptionsTests {
    private static string LoadFixture() =>
        EmbeddedResourceHelper.LoadEmbeddedResourceContents(
            typeof(PlayerInfoSerializationTests),
            PlayerInfoSerializationTests.FileName);

    [Fact]
    public void ClientOptionsDeserializeWholePlayerList() {
        var players = JsonSerializer.Deserialize<List<PlayerInfo>>(LoadFixture(), ShlConstants.JsonSerializerOptions);

        Assert.NotNull(players);
        Assert.NotEmpty(players);
        // The fixture includes defensemen and SMJHL rookies whose enum string values contain spaces/slashes.
        Assert.Contains(players, p => p.Position == PlayerPosition.RightDefense);
        Assert.Contains(players, p => p.TaskStatus == TaskStatus.SmjhlRookie);
        Assert.Contains(players, p => p.TaskStatus == TaskStatus.ShlOrSendDown);
    }

    [Theory]
    [InlineData("\"Right Defense\"", PlayerPosition.RightDefense)]
    [InlineData("\"Left Defense\"", PlayerPosition.LeftDefense)]
    [InlineData("\"Center\"", PlayerPosition.Center)]
    [InlineData("\"Goalie\"", PlayerPosition.Goalie)]
    public void ClientOptionsParsePositionsWithSpaces(string json, PlayerPosition expected) {
        var position = JsonSerializer.Deserialize<PlayerPosition>(json, ShlConstants.JsonSerializerOptions);
        Assert.Equal(expected, position);
    }

    [Theory]
    [InlineData("\"SMJHL Rookie\"", TaskStatus.SmjhlRookie)]
    [InlineData("\"SHL/Send-down\"", TaskStatus.ShlOrSendDown)]
    [InlineData("\"Retired\"", TaskStatus.Retired)]
    public void ClientOptionsParseTaskStatusesWithSpaces(string json, TaskStatus expected) {
        var status = JsonSerializer.Deserialize<TaskStatus>(json, ShlConstants.JsonSerializerOptions);
        Assert.Equal(expected, status);
    }

    /// <summary>
    /// Characterizes the underlying bug: a globally-registered <see cref="JsonStringEnumConverter"/>
    /// (as added by Refit's default options) overrides the type-level <c>PositionConverter</c> and fails
    /// on values containing spaces. This is exactly why we do not use Refit's default serializer options.
    /// </summary>
    [Fact]
    public void GlobalStringEnumConverterBreaksSpacedEnumValues() {
        var refitLikeOptions = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        refitLikeOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<PlayerPosition>("\"Right Defense\"", refitLikeOptions));
    }
}
