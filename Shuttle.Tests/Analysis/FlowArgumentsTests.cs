using Shuttle.Analysis.Flows;

namespace Shuttle.Tests.Analysis;

public class FlowArgumentsTests {

    [Fact]
    public void Parse_ReturnsEmptyForNull() {
        Assert.Empty(FlowArguments.Parse(null));
    }

    [Fact]
    public void Parse_ReadsKeyValueTokens() {
        var args = FlowArguments.Parse(["k=3", "seed=42"]);

        Assert.Equal("3", args["k"]);
        Assert.Equal("42", args["seed"]);
    }

    [Fact]
    public void Parse_IsCaseInsensitiveOnKeys() {
        var args = FlowArguments.Parse(["K=3"]);

        Assert.True(args.ContainsKey("k"));
        Assert.Equal("3", args["k"]);
    }

    [Fact]
    public void Parse_KeepsEqualsInValue() {
        var args = FlowArguments.Parse(["expr=a=b"]);

        Assert.Equal("a=b", args["expr"]);
    }

    [Fact]
    public void Parse_TrimsKeyWhitespace() {
        var args = FlowArguments.Parse([" k =3"]);

        Assert.Equal("3", args["k"]);
    }

    [Fact]
    public void Parse_ThrowsOnMissingSeparator() {
        Assert.Throws<FormatException>(() => FlowArguments.Parse(["k"]));
    }

    [Fact]
    public void Parse_ThrowsOnEmptyKey() {
        Assert.Throws<FormatException>(() => FlowArguments.Parse(["=3"]));
    }

    [Fact]
    public void Parse_ThrowsOnDuplicateKey() {
        Assert.Throws<FormatException>(() => FlowArguments.Parse(["k=3", "K=4"]));
    }

    [Fact]
    public void GetRequiredInt_ReturnsValue() {
        var args = FlowArguments.Parse(["k=5"]);

        Assert.Equal(5, FlowArguments.GetRequiredInt(args, "k"));
    }

    [Fact]
    public void GetRequiredInt_ThrowsWhenMissing() {
        Assert.Throws<ArgumentException>(() => FlowArguments.GetRequiredInt(FlowArguments.Empty, "k"));
    }

    [Fact]
    public void GetRequiredInt_ThrowsWhenNotInteger() {
        var args = FlowArguments.Parse(["k=abc"]);

        Assert.Throws<ArgumentException>(() => FlowArguments.GetRequiredInt(args, "k"));
    }

    [Fact]
    public void GetOptionalInt_ReturnsDefaultWhenMissing() {
        Assert.Equal(7, FlowArguments.GetOptionalInt(FlowArguments.Empty, "seed", 7));
    }
}
