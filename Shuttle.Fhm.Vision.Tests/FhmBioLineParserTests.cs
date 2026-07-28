using Shuttle.Fhm.Vision.Extraction;

namespace Shuttle.Fhm.Vision.Tests;

public sealed class FhmBioLineParserTests {
    private const string Sample =
        "LD/RD | SACRAMENTO EXPRESS | SHOOTS: LEFT | AGE: 23 | 6'5\" - 243 LBS | SALARY: $775,000 (1)";

    [Fact]
    public void Parse_extracts_position_height_and_weight_from_sample() {
        var bio = FhmBioLineParser.Parse(Sample);

        Assert.Equal("LD/RD", bio.Position);
        Assert.Equal("6'5\"", bio.Height);
        Assert.Equal((6 * 12) + 5, bio.HeightInches);
        Assert.Equal(243, bio.Weight);
    }

    [Fact]
    public void Parse_tolerates_missing_inch_mark_and_extra_whitespace() {
        var bio = FhmBioLineParser.Parse("C  |  TEAM |  SHOOTS: RIGHT | AGE: 19 |  5' 11  -  190  LBS  | SALARY: $1");

        Assert.Equal("C", bio.Position);
        Assert.Equal("5'11\"", bio.Height);
        Assert.Equal((5 * 12) + 11, bio.HeightInches);
        Assert.Equal(190, bio.Weight);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Parse_returns_default_for_empty_input(string? input) {
        var bio = FhmBioLineParser.Parse(input);

        Assert.Null(bio.Position);
        Assert.Null(bio.Height);
        Assert.Null(bio.HeightInches);
        Assert.Null(bio.Weight);
    }
}
