using System.Text.Json;
using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.Tests.Serialization.Portal;

public class HeightTests {
    
    [Theory]
    [InlineData("6'2\"", "6ft 2in", 188)]
    [InlineData("5'11\"", "5ft 11in", 180)]
    [InlineData("6'0\"", "6ft 0in", 183)]
    [InlineData("6’4”", "6ft 4in", 193)]
    [InlineData("4ft. 20in.", "5ft 8in", 173)]
    public void DeserializeHeight(string heightString, string altFormat, int expectedCm) {
        var jsonString = JsonSerializer.Serialize(heightString);
        var altJsonString = JsonSerializer.Serialize(altFormat);
        var height = JsonSerializer.Deserialize<Height>(jsonString);
        var altHeight = JsonSerializer.Deserialize<Height>(altJsonString);
        Assert.NotNull(height);
        Assert.NotNull(altHeight);
        Assert.Equal(height, altHeight);
        Assert.Equal(expectedCm, height.TotalCentimeters);
        Assert.Equal(expectedCm, altHeight.TotalCentimeters);
    }
}
