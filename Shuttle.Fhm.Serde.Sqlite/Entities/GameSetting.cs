using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shuttle.Fhm.Serde.Sqlite.Entities;

/// <summary>One fixed-order game setting.</summary>
public abstract class GameSetting
{
    /// <summary>Gets or sets the documented setting ordinal.</summary>
    public int SettingOrdinal { get; set; }
}

/// <summary>A byte-valued game setting.</summary>
public sealed class ByteGameSetting : GameSetting { public byte Value { get; set; } }
/// <summary>An unsigned 16-bit game setting.</summary>
public sealed class UInt16GameSetting : GameSetting { public ushort Value { get; set; } }
/// <summary>A signed 32-bit game setting.</summary>
public sealed class Int32GameSetting : GameSetting { public int Value { get; set; } }
/// <summary>A real-valued game setting.</summary>
public sealed class DoubleGameSetting : GameSetting { public double Value { get; set; } }
/// <summary>A string-valued game setting.</summary>
public sealed class QStringGameSetting : GameSetting { public string? Value { get; set; } }

public sealed class GameSettingConfiguration : IEntityTypeConfiguration<GameSetting>
{
    public void Configure(EntityTypeBuilder<GameSetting> builder)
    {
        builder.ToTable("GameSettings");
        builder.HasKey(value => value.SettingOrdinal);
        builder.Property(value => value.SettingOrdinal).ValueGeneratedNever();
        builder.HasDiscriminator<string>("SettingType")
            .HasValue<ByteGameSetting>("Byte")
            .HasValue<UInt16GameSetting>("UInt16")
            .HasValue<Int32GameSetting>("Int32")
            .HasValue<DoubleGameSetting>("Double")
            .HasValue<QStringGameSetting>("QString");
    }
}

public sealed class ByteGameSettingConfiguration : IEntityTypeConfiguration<ByteGameSetting>
{
    public void Configure(EntityTypeBuilder<ByteGameSetting> builder) => builder.Property(value => value.Value).HasColumnName("ByteValue");
}

public sealed class UInt16GameSettingConfiguration : IEntityTypeConfiguration<UInt16GameSetting>
{
    public void Configure(EntityTypeBuilder<UInt16GameSetting> builder) => builder.Property(value => value.Value).HasColumnName("UInt16Value");
}

public sealed class Int32GameSettingConfiguration : IEntityTypeConfiguration<Int32GameSetting>
{
    public void Configure(EntityTypeBuilder<Int32GameSetting> builder) => builder.Property(value => value.Value).HasColumnName("Int32Value");
}

public sealed class DoubleGameSettingConfiguration : IEntityTypeConfiguration<DoubleGameSetting>
{
    public void Configure(EntityTypeBuilder<DoubleGameSetting> builder) => builder.Property(value => value.Value).HasColumnName("DoubleValue");
}

public sealed class QStringGameSettingConfiguration : IEntityTypeConfiguration<QStringGameSetting>
{
    public void Configure(EntityTypeBuilder<QStringGameSetting> builder) => builder.Property(value => value.Value).HasColumnName("StringValue");
}
