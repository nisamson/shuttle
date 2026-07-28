using Shuttle.Fhm.Vision.Extraction;

namespace Shuttle.Fhm.Vision.Tests;

public sealed class FieldTextParserTests {
    [Theory]
    [InlineData("  hello   world  ", "hello world")]
    [InlineData("Line\nBreak", "Line Break")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void NormalizeText_collapses_whitespace(string? input, string expected) {
        Assert.Equal(expected, FieldTextParser.NormalizeText(input));
    }

    [Theory]
    [InlineData("15", 15)]
    [InlineData(" 7 ", 7)]
    [InlineData("rating: 12", 12)]
    [InlineData("-3", -3)]
    [InlineData("1o2", 12)] // stray lowercase non-digit dropped
    public void ParseInteger_extracts_digits(string input, int expected) {
        Assert.Equal(expected, FieldTextParser.ParseInteger(input));
    }

    [Theory]
    [InlineData("l4", 14)]  // leading '1' misread as lowercase L
    [InlineData("I4", 14)]  // leading '1' misread as uppercase i
    [InlineData("|4", 14)]  // leading '1' misread as pipe
    [InlineData("S", 5)]    // single digit '5' misread as S
    [InlineData("B", 8)]    // single digit '8' misread as B
    [InlineData("Z", 2)]    // single digit '2' misread as Z
    [InlineData("O", 0)]    // single digit '0' misread as O
    public void ParseInteger_recovers_confusable_digits(string input, int expected) {
        Assert.Equal(expected, FieldTextParser.ParseInteger(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("no digits here")]
    public void ParseInteger_returns_null_when_no_digits(string? input) {
        Assert.Null(FieldTextParser.ParseInteger(input));
    }

    [Theory]
    [InlineData("3.5", 3.5)]
    [InlineData(" 12 ", 12.0)]
    [InlineData("$775,000", 775000.0)]
    [InlineData("243 LBS", 243.0)]
    [InlineData("-1.25", -1.25)]
    [InlineData("rating 2.75/5", 2.75)]
    public void ParseDecimal_extracts_number(string input, double expected) {
        Assert.Equal(expected, FieldTextParser.ParseDecimal(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("no digits here")]
    public void ParseDecimal_returns_null_when_no_digits(string? input) {
        Assert.Null(FieldTextParser.ParseDecimal(input));
    }
}
