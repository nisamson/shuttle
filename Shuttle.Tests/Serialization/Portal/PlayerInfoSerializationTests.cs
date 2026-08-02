using System.Text.Json;
using System.Text.Json.Serialization;
using Shuttle.Shl.Api.Models.Portal.V1;
using Shuttle.Tests.Utilites;
using TaskStatus = Shuttle.Shl.Api.Models.Portal.V1.TaskStatus;

namespace Shuttle.Tests.Serialization.Portal;

public class PlayerInfoSerializationTests {
    public const string FileName = "portalPlayers.json";
    
    private static string LoadTestFileContents() => 
        EmbeddedResourceHelper.LoadEmbeddedResourceContents(typeof(PlayerInfoSerializationTests), FileName);

    // private static PlayerInfo FirstPlayerInfo = new(
    //     UserId: 23596,
    //     Username: "st14browny",
    //     PlayerId: 2604,
    //     CreationDate: DateTime.Parse("2026-03-06T19:13:33.000Z"),
    //     Status: PlayerStatus.Active,
    //     Name: "Amon-Ra St Brown",
    //     Position: PlayerPosition.Center,
    //     Handedness: PlayerHandedness.Right,
    //     Recruiter: "",
    //     Render: "male",
    //     JerseyNumber: 14,
    //     Height: new Height(5, 11),
    //     Weight: 200,
    //     Birthplace: "London, Canada",
    //     TotalTpe: 170,
    //     AppliedTpe: 155,
    //     BankedTpe: 15,
    //     DraftSeason: 88,
    //     CurrentLeague: KnownLeague.Smjhl,
    //     CurrentTeamId: null,
    //     ShlRightsTeamId: null,
    //     SmjhlRightsTeamId: null,
    //     IihfNation: "Canada",
    //     
    // );

    private readonly JsonDocument testDocument;

    public PlayerInfoSerializationTests() {
        var json = LoadTestFileContents();
        testDocument = JsonDocument.Parse(json);
    }

    [Fact]
    public void DeserializePlayerInfoList() {
        var serializerOptions = new JsonSerializerOptions() {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            PropertyNameCaseInsensitive = true,
        };
        var originalElements = testDocument.RootElement.EnumerateArray().ToList();
        foreach (var element in originalElements) {
            var deserialized = JsonSerializer.Deserialize<PlayerInfo>(element.GetRawText(), serializerOptions);
            Assert.NotNull(deserialized);
        }
        
        var playerInfoList = testDocument.Deserialize<List<PlayerInfo>>(serializerOptions);
        Assert.NotNull(playerInfoList);
        Assert.NotEmpty(playerInfoList);
        var reserialized = JsonSerializer.SerializeToDocument(playerInfoList, serializerOptions);
        
        var reserializedElements = reserialized.RootElement.EnumerateArray().ToList();
        Assert.Equal(originalElements.Count, reserializedElements.Count);
        foreach (var (originalElement, reserializedElement) in originalElements.Zip(reserializedElements)) {
            Assert.Equivalent(originalElement, reserializedElement);
        }
        var deserializedAgain = reserialized.Deserialize<List<PlayerInfo>>(serializerOptions);
        Assert.NotNull(deserializedAgain);
        foreach (var (original, deserialized) in playerInfoList.Zip(deserializedAgain)) {
            Assert.Equivalent(original, deserialized);
        }
        Assert.NotNull(deserializedAgain);
        Assert.Equivalent(playerInfoList, deserializedAgain);
    }

    [Theory]
    [InlineData("Draftee Free Agent", TaskStatus.DrafteeFreeAgent)]
    [InlineData("SMJHL Rookie", TaskStatus.SmjhlRookie)]
    [InlineData("SHL/Send-down", TaskStatus.ShlOrSendDown)]
    [InlineData("Retired", TaskStatus.Retired)]
    [InlineData("Pending Approval", TaskStatus.PendingApproval)]
    public void DeserializeTaskStatusValues(string wireValue, TaskStatus expected) {
        var json = $"\"{wireValue}\"";
        var deserialized = JsonSerializer.Deserialize<TaskStatus>(json);
        Assert.Equal(expected, deserialized);
        Assert.Equal(json, JsonSerializer.Serialize(deserialized));
    }


}
