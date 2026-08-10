using System.Diagnostics;
using Shuttle.Fhm.SaveData.Binary;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>The fixed-order, no-header <c>game_settings.dat</c> payload.</summary>
public sealed class FhmGameSettingsFile : IFhmSaveFile
{
    private static readonly FhmGameSettingValueKind[] valueKinds =
    [
        FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte,
        FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.UInt16, FhmGameSettingValueKind.Byte,
        FhmGameSettingValueKind.UInt16, FhmGameSettingValueKind.UInt16, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte,
        FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Double, FhmGameSettingValueKind.Double, FhmGameSettingValueKind.Byte,
        FhmGameSettingValueKind.UInt16, FhmGameSettingValueKind.UInt16, FhmGameSettingValueKind.UInt16, FhmGameSettingValueKind.Byte,
        FhmGameSettingValueKind.UInt16, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte,
        FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.UInt16, FhmGameSettingValueKind.Byte,
        FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Int32, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte,
        FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Int32,
        FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.QString, FhmGameSettingValueKind.Byte,
        FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Double, FhmGameSettingValueKind.QString,
        FhmGameSettingValueKind.UInt16, FhmGameSettingValueKind.UInt16, FhmGameSettingValueKind.UInt16, FhmGameSettingValueKind.UInt16,
        FhmGameSettingValueKind.Int32, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte,
        FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte,
        FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte,
        FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte,
        FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte,
        FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.UInt16,
        FhmGameSettingValueKind.UInt16, FhmGameSettingValueKind.Byte,
        FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte,
        FhmGameSettingValueKind.Byte, FhmGameSettingValueKind.Byte,
    ];

    private readonly object?[] values = new object?[valueKinds.Length];

    /// <inheritdoc />
    public string RelativePath => "game_settings.dat";

    /// <summary>Gets the 80 setting values in documented wire order.</summary>
    public IList<object?> Values => values;

    /// <summary>Gets or sets the referenced object id, where <c>-1</c> is the documented sentinel.</summary>
    public int ReferencedObjectId
    {
        get => Get<int>(FhmGameSetting.ReferencedObjectId);
        set => Set(FhmGameSetting.ReferencedObjectId, value);
    }

    /// <summary>Gets or sets a value using its documented field identity.</summary>
    public void Set(FhmGameSetting setting, object? value)
    {
        var index = (int)setting;
        ValidateValue(index, value);
        values[index] = value;
    }

    /// <summary>Gets a setting value using its documented field identity.</summary>
    public T Get<T>(FhmGameSetting setting)
    {
        var index = (int)setting;
        var value = values[index] ?? DefaultValue(valueKinds[index]);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"Game setting {setting} cannot be returned as {typeof(T).Name}.");
    }

    internal static FhmGameSettingsFile Read(FhmBinaryReader reader)
    {
        var result = new FhmGameSettingsFile();
        for (var index = 0; index < valueKinds.Length; index++)
        {
            result.values[index] = ReadValue(reader, valueKinds[index]);
        }

        reader.EnsureEof("game_settings.dat");
        return result;
    }

    /// <inheritdoc />
    public void WriteTo(FhmBinaryWriter writer)
    {
        for (var index = 0; index < valueKinds.Length; index++)
        {
            WriteValue(writer, valueKinds[index], values[index] ?? DefaultValue(valueKinds[index]));
        }
    }

    private static object? ReadValue(FhmBinaryReader reader, FhmGameSettingValueKind kind) => kind switch
    {
        FhmGameSettingValueKind.Byte => reader.ReadByte(),
        FhmGameSettingValueKind.UInt16 => reader.ReadUInt16(),
        FhmGameSettingValueKind.Int32 => reader.ReadInt32(),
        FhmGameSettingValueKind.Double => reader.ReadDouble(),
        FhmGameSettingValueKind.QString => reader.ReadQString(),
        _ => throw new UnreachableException(),
    };

    private static void WriteValue(FhmBinaryWriter writer, FhmGameSettingValueKind kind, object? value)
    {
        switch (kind)
        {
            case FhmGameSettingValueKind.Byte:
                writer.WriteByte((byte)value!);
                break;
            case FhmGameSettingValueKind.UInt16:
                writer.WriteUInt16((ushort)value!);
                break;
            case FhmGameSettingValueKind.Int32:
                writer.WriteInt32((int)value!);
                break;
            case FhmGameSettingValueKind.Double:
                writer.WriteDouble((double)value!);
                break;
            case FhmGameSettingValueKind.QString:
                writer.WriteQString((string?)value);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private static void ValidateValue(int index, object? value)
    {
        var expected = valueKinds[index] switch
        {
            FhmGameSettingValueKind.Byte => typeof(byte),
            FhmGameSettingValueKind.UInt16 => typeof(ushort),
            FhmGameSettingValueKind.Int32 => typeof(int),
            FhmGameSettingValueKind.Double => typeof(double),
            FhmGameSettingValueKind.QString => typeof(string),
            _ => throw new UnreachableException(),
        };

        if (value is null && valueKinds[index] == FhmGameSettingValueKind.QString)
        {
            return;
        }

        if (value?.GetType() != expected)
        {
            throw new ArgumentException($"Game setting {(FhmGameSetting)index} requires a {expected.Name} value.", nameof(value));
        }
    }

    private static object? DefaultValue(FhmGameSettingValueKind kind) => kind switch
    {
        FhmGameSettingValueKind.Byte => (byte)0,
        FhmGameSettingValueKind.UInt16 => (ushort)0,
        FhmGameSettingValueKind.Int32 => 0,
        FhmGameSettingValueKind.Double => 0d,
        FhmGameSettingValueKind.QString => null,
        _ => throw new UnreachableException(),
    };
}

/// <summary>Documented fixed-order fields in <c>game_settings.dat</c>.</summary>
public enum FhmGameSetting
{
    Setting001, Setting002, Setting003, Setting004, Setting005, Setting006, Setting007, Setting008, Setting009, Setting010,
    Setting011, Setting012, DeprecatedFlag013, Setting014, Setting015, Setting016, Setting017, Setting018, Setting019, Setting020,
    Setting021, Setting022, Setting023, Setting024, Setting025, Setting026, Setting027, Setting028, Setting029, ReferencedObjectId,
    Setting031, Setting032, Setting033, Setting034, Setting035, Setting036, Setting037, Setting038, Setting039, Setting040,
    Setting041, Setting042, Setting043, Setting044, Setting045, Setting046, Setting047, Setting048, Setting049, Setting050,
    Setting051, Setting052, Setting053, Setting054, Setting055, Setting056, Setting057, Setting058, Setting059, Setting060,
    Setting061, Setting062, DeprecatedFlag063, Setting064, Setting065, Setting066, Setting067, Setting068, Setting069, Setting070,
    Setting071, Setting072, Setting073, Setting074, Setting075, Setting076, Setting077, Setting078, Setting079, Setting080,
}

internal enum FhmGameSettingValueKind
{
    Byte,
    UInt16,
    Int32,
    Double,
    QString,
}
