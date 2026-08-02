using System.Text.Json;
using System.Text.Json.Serialization;
using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.Shl.Api.Models.Index.V1;
using Positions = IDictionary<PlayerPosition, PlayerRef>;

public record Lineup();

public record EvenStrengthLines( 
    LineGroup FiveOnFive,
    LineGroup FourOnFour,
    LineGroup ThreeOnThree
    );

public record PowerPlayLines(
    LineGroup FiveOnFour,
    LineGroup FiveOnThree,
    LineGroup FourOnThree
    );

public record PenaltyKillLines(
    LineGroup FourOnFive,
    LineGroup ThreeOnFive,
    LineGroup ThreeOnFour
    );

public record Goalies(
    PlayerRef Starter,
    PlayerRef Backup
);

public record LineGroup(IList<Line> Lines);

[JsonConverter(typeof(LineConverter))]
public record Line(Positions Positions);

public class LineConverter : JsonConverter<Line> {
    public override Line? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var positions = JsonSerializer.Deserialize<Positions>(ref reader, options);
        return positions is null ? null : new Line(positions);
    }
    public override void Write(Utf8JsonWriter writer, Line value, JsonSerializerOptions options) {
        JsonSerializer.Serialize(writer, value.Positions, options);
    }
}

public class LineGroupConverter : JsonConverter<LineGroup> {
    public override LineGroup? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        throw new NotImplementedException();
    }
    public override void Write(Utf8JsonWriter writer, LineGroup value, JsonSerializerOptions options) {
        throw new NotImplementedException();
    }
}