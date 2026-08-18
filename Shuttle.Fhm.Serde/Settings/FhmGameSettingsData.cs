using System.Reflection;
using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;

namespace Shuttle.Fhm.Serde.Settings;

/// <summary>The fixed-order, no-header <c>game_settings.dat</c> wire contract.</summary>
public sealed class FhmGameSettingsData
{
    private static readonly PropertyInfo[] valueProperties = typeof(FhmGameSettingsData)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(property => property.Name.StartsWith("Value", StringComparison.Ordinal))
        .OrderBy(property => property.Name, StringComparer.Ordinal)
        .ToArray();

    [FieldOrder(0)]
    public byte Value001 { get; set; }
    [FieldOrder(1)]
    public byte Value002 { get; set; }
    [FieldOrder(2)]
    public byte Value003 { get; set; }
    [FieldOrder(3)]
    public byte Value004 { get; set; }
    [FieldOrder(4)]
    public byte Value005 { get; set; }
    [FieldOrder(5)]
    public byte Value006 { get; set; }
    [FieldOrder(6)]
    public ushort Value007 { get; set; }
    [FieldOrder(7)]
    public byte Value008 { get; set; }
    [FieldOrder(8)]
    public ushort Value009 { get; set; }
    [FieldOrder(9)]
    public ushort Value010 { get; set; }
    [FieldOrder(10)]
    public byte Value011 { get; set; }
    [FieldOrder(11)]
    public byte Value012 { get; set; }
    [FieldOrder(12)]
    public byte Value013 { get; set; }
    [FieldOrder(13)]
    public double Value014 { get; set; }
    [FieldOrder(14)]
    public double Value015 { get; set; }
    [FieldOrder(15)]
    public byte Value016 { get; set; }
    [FieldOrder(16)]
    public ushort Value017 { get; set; }
    [FieldOrder(17)]
    public ushort Value018 { get; set; }
    [FieldOrder(18)]
    public ushort Value019 { get; set; }
    [FieldOrder(19)]
    public byte Value020 { get; set; }
    [FieldOrder(20)]
    public ushort Value021 { get; set; }
    [FieldOrder(21)]
    public byte Value022 { get; set; }
    [FieldOrder(22)]
    public byte Value023 { get; set; }
    [FieldOrder(23)]
    public byte Value024 { get; set; }
    [FieldOrder(24)]
    public byte Value025 { get; set; }
    [FieldOrder(25)]
    public byte Value026 { get; set; }
    [FieldOrder(26)]
    public ushort Value027 { get; set; }
    [FieldOrder(27)]
    public byte Value028 { get; set; }
    [FieldOrder(28)]
    public byte Value029 { get; set; }
    [FieldOrder(29)]
    public int Value030 { get; set; }
    [FieldOrder(30)]
    public byte Value031 { get; set; }
    [FieldOrder(31)]
    public byte Value032 { get; set; }
    [FieldOrder(32)]
    public byte Value033 { get; set; }
    [FieldOrder(33)]
    public byte Value034 { get; set; }
    [FieldOrder(34)]
    public byte Value035 { get; set; }
    [FieldOrder(35)]
    public int Value036 { get; set; }
    [FieldOrder(36)]
    public byte Value037 { get; set; }
    [FieldOrder(37)]
    public byte Value038 { get; set; }
    [FieldOrder(38)]
    public QString Value039 { get; set; } = new();
    [FieldOrder(39)]
    public byte Value040 { get; set; }
    [FieldOrder(40)]
    public byte Value041 { get; set; }
    [FieldOrder(41)]
    public byte Value042 { get; set; }
    [FieldOrder(42)]
    public double Value043 { get; set; }
    [FieldOrder(43)]
    public QString Value044 { get; set; } = new();
    [FieldOrder(44)]
    public ushort Value045 { get; set; }
    [FieldOrder(45)]
    public ushort Value046 { get; set; }
    [FieldOrder(46)]
    public ushort Value047 { get; set; }
    [FieldOrder(47)]
    public ushort Value048 { get; set; }
    [FieldOrder(48)]
    public int Value049 { get; set; }
    [FieldOrder(49)]
    public byte Value050 { get; set; }
    [FieldOrder(50)]
    public byte Value051 { get; set; }
    [FieldOrder(51)]
    public byte Value052 { get; set; }
    [FieldOrder(52)]
    public byte Value053 { get; set; }
    [FieldOrder(53)]
    public byte Value054 { get; set; }
    [FieldOrder(54)]
    public byte Value055 { get; set; }
    [FieldOrder(55)]
    public byte Value056 { get; set; }
    [FieldOrder(56)]
    public byte Value057 { get; set; }
    [FieldOrder(57)]
    public byte Value058 { get; set; }
    [FieldOrder(58)]
    public byte Value059 { get; set; }
    [FieldOrder(59)]
    public byte Value060 { get; set; }
    [FieldOrder(60)]
    public byte Value061 { get; set; }
    [FieldOrder(61)]
    public byte Value062 { get; set; }
    [FieldOrder(62)]
    public byte Value063 { get; set; }
    [FieldOrder(63)]
    public byte Value064 { get; set; }
    [FieldOrder(64)]
    public byte Value065 { get; set; }
    [FieldOrder(65)]
    public byte Value066 { get; set; }
    [FieldOrder(66)]
    public byte Value067 { get; set; }
    [FieldOrder(67)]
    public byte Value068 { get; set; }
    [FieldOrder(68)]
    public byte Value069 { get; set; }
    [FieldOrder(69)]
    public byte Value070 { get; set; }
    [FieldOrder(70)]
    public byte Value071 { get; set; }
    [FieldOrder(71)]
    public ushort Value072 { get; set; }
    [FieldOrder(72)]
    public ushort Value073 { get; set; }
    [FieldOrder(73)]
    public byte Value074 { get; set; }
    [FieldOrder(74)]
    public byte Value075 { get; set; }
    [FieldOrder(75)]
    public byte Value076 { get; set; }
    [FieldOrder(76)]
    public byte Value077 { get; set; }
    [FieldOrder(77)]
    public byte Value078 { get; set; }
    [FieldOrder(78)]
    public byte Value079 { get; set; }
    [FieldOrder(79)]
    public byte Value080 { get; set; }

    /// <summary>Gets a value by its documented zero-based wire ordinal.</summary>
    public object? GetValue(int index)
    {
        var value = valueProperties[index].GetValue(this);
        return value is QString text ? text.Value : value;
    }

    /// <summary>Sets a value by its documented zero-based wire ordinal.</summary>
    public void SetValue(int index, object? value)
    {
        var property = valueProperties[index];
        property.SetValue(this, property.PropertyType == typeof(QString)
            ? new QString { Value = (string?)value }
            : value);
    }
}

/// <summary>Serializes the <c>game_settings.dat</c> wire contract.</summary>
public static class FhmGameSettingsSerializer
{
    /// <summary>Deserializes a complete <c>game_settings.dat</c> stream.</summary>
    public static FhmGameSettingsData Deserialize(Stream stream) =>
        QSerializerFactory.Deserialize<FhmGameSettingsData>(stream, "game_settings.dat");

    /// <summary>Serializes a complete <c>game_settings.dat</c> stream.</summary>
    public static void Serialize(Stream stream, FhmGameSettingsData value) =>
        QSerializerFactory.Serialize(stream, value);
}
