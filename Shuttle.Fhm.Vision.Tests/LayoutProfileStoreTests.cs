using Shuttle.Fhm.Vision.Layout;

namespace Shuttle.Fhm.Vision.Tests;

public sealed class LayoutProfileStoreTests {
    [Fact]
    public void Serialize_then_deserialize_round_trips() {
        var profile = new LayoutProfile {
            Name = "fhm-player-screen",
            Anchors = [
                new AnchorMarker { Bounds = new RatioRect(0.0, 0.0, 0.2, 0.05), ExpectedText = "Attributes" },
            ],
            Regions = [
                new FieldRegion {
                    Key = "name", Group = FieldGroup.Identity, Kind = FieldKind.Text,
                    Bounds = new RatioRect(0.1, 0.1, 0.3, 0.05),
                },
                new FieldRegion {
                    Key = "skating", Group = FieldGroup.Attribute, Kind = FieldKind.Integer,
                    Bounds = new RatioRect(0.5, 0.2, 0.05, 0.03),
                },
                new FieldRegion {
                    Key = "playmaker", Group = FieldGroup.Role, Kind = FieldKind.Integer,
                    Bounds = new RatioRect(0.7, 0.3, 0.05, 0.03),
                },
            ],
        };

        var json = LayoutProfileStore.Serialize(profile);
        var restored = LayoutProfileStore.Deserialize(json);

        Assert.Equal(profile.Name, restored.Name);
        Assert.Equal(profile.Anchors, restored.Anchors);
        Assert.Equal(profile.Regions, restored.Regions);
    }

    [Fact]
    public void Serialize_writes_enum_names_as_camel_case_strings() {
        var profile = new LayoutProfile {
            Name = "p",
            Regions = [
                new FieldRegion {
                    Key = "k", Group = FieldGroup.Attribute, Kind = FieldKind.Integer,
                    Bounds = new RatioRect(0, 0, 0.1, 0.1),
                },
            ],
        };

        var json = LayoutProfileStore.Serialize(profile);

        Assert.Contains("\"attribute\"", json);
        Assert.Contains("\"integer\"", json);
    }
}
