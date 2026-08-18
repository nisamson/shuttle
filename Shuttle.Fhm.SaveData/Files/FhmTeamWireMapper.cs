using System.Collections;
using System.Reflection;
using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;
using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.SaveData.Model;
using Shuttle.Fhm.Serde.Teams;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>Maps the stable team object model to the BinarySerializer wire contract.</summary>
internal static class FhmTeamWireMapper
{
    internal const int MaximumCollectionCount = 10_000_000;

    internal static FhmTeamsFileData ToWire(FhmTeamsFile value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var result = new FhmTeamsFileData();
        CopyObject(value, result);
        NormalizeForWrite(result);
        ValidateWire(result);
        return result;
    }

    internal static FhmTeamsFile FromWire(FhmTeamsFileData value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ValidateWire(value);
        var result = new FhmTeamsFile();
        CopyObject(value, result);
        return result;
    }

    internal static FhmTeamTacticsSettingsData ToWire(FhmTeamTacticsSettings value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var result = new FhmTeamTacticsSettingsData();
        CopyObject(value, result);
        NormalizeLists(result);
        ValidateTactics(result);
        return result;
    }

    internal static FhmTeamTacticsSettings FromWire(FhmTeamTacticsSettingsData value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ValidateObject(value);
        ValidateTactics(value);
        var result = new FhmTeamTacticsSettings();
        CopyObject(value, result);
        return result;
    }

    internal static void ValidateWire(FhmTeamsFileData value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ValidateObject(value);
        if (value.TeamCount < 0 || value.TeamCount > MaximumCollectionCount || value.Teams.Count != value.TeamCount)
        {
            throw new FhmFormatException($"Invalid teams count {value.TeamCount}.");
        }

        foreach (var team in value.Teams)
        {
            ValidateTeam(team);
        }
    }

    private static void CopyObject(object source, object destination)
    {
        var sourceProperties = source.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead)
            .ToDictionary(property => property.Name, StringComparer.Ordinal);

        foreach (var destinationProperty in destination.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!destinationProperty.CanRead || !sourceProperties.TryGetValue(destinationProperty.Name, out var sourceProperty))
            {
                continue;
            }

            var sourceValue = sourceProperty.GetValue(source);
            if (sourceValue is null && destinationProperty.PropertyType.IsValueType)
            {
                continue;
            }

            var destinationValue = destinationProperty.GetValue(destination);
            if (destinationProperty.CanWrite)
            {
                destinationProperty.SetValue(destination, ConvertValue(sourceValue, destinationProperty.PropertyType));
            }
            else if (destinationValue is IList destinationList)
            {
                CopyCollection(sourceValue, destinationList);
            }
            else if (sourceValue is not null && destinationValue is not null)
            {
                CopyObject(sourceValue, destinationValue);
            }
        }
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (targetType == typeof(QString))
        {
            return new QString { Value = value as string };
        }

        if (targetType == typeof(string))
        {
            return value is QString text ? text.Value : value;
        }

        if (targetType == typeof(FhmOpaqueBytes))
        {
            return value switch
            {
                byte[] bytes => new FhmOpaqueBytes(bytes.ToArray()),
                FhmFixed12BytesData fixed12 => new FhmOpaqueBytes(fixed12.Data.ToArray()),
                FhmFixed89BytesData fixed89 => new FhmOpaqueBytes(fixed89.Data.ToArray()),
                _ => throw new InvalidOperationException($"Cannot map '{value?.GetType().FullName}' to {nameof(FhmOpaqueBytes)}."),
            };
        }

        if (targetType == typeof(byte[]))
        {
            return value switch
            {
                FhmOpaqueBytes opaque => opaque.Value.ToArray(),
                IEnumerable<byte> values => values.ToArray(),
                _ => throw new InvalidOperationException($"Cannot map '{value?.GetType().FullName}' to a byte array."),
            };
        }

        if (targetType == typeof(FhmFixed12BytesData) && value is FhmOpaqueBytes opaque12)
        {
            return new FhmFixed12BytesData { Data = opaque12.Value.ToArray() };
        }

        if (targetType == typeof(FhmFixed89BytesData) && value is FhmOpaqueBytes opaque89)
        {
            return new FhmFixed89BytesData { Data = opaque89.Value.ToArray() };
        }

        if (targetType == typeof(ushort) && IsFhmEnumValue(value?.GetType()))
        {
            return value!.GetType().GetProperty(nameof(FhmEnumValue<FhmFanHappinessEvent>.RawValue))!.GetValue(value);
        }

        if (IsFhmEnumValue(targetType) && value is ushort rawValue)
        {
            return Activator.CreateInstance(targetType, rawValue);
        }

        if (Nullable.GetUnderlyingType(targetType) is { } nullableType &&
            IsFhmEnumValue(nullableType) &&
            value is ushort nullableRawValue)
        {
            return Activator.CreateInstance(nullableType, nullableRawValue);
        }

        if (targetType == typeof(FhmTaggedPlayerId) && value is FhmTaggedPlayerIdData taggedPlayerId)
        {
            return new FhmTaggedPlayerId(taggedPlayerId.PlayerReference, taggedPlayerId.Tag);
        }

        if (targetType == typeof(FhmZoneSelectorBlock) && value is FhmZoneSelectorBlockData selector)
        {
            var result = new FhmZoneSelectorBlock(selector.BlockIndex);
            CopyObject(selector, result);
            return result;
        }

        if (value is null)
        {
            return null;
        }

        if (IsQList(targetType, out var listElementType))
        {
            var items = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(listElementType))!;
            foreach (var item in GetItems(value))
            {
                items.Add(ConvertValue(item, listElementType));
            }

            var list = Activator.CreateInstance(targetType)!;
            targetType.GetProperty(nameof(QList<int>.Length))!.SetValue(list, items.Count);
            targetType.GetProperty(nameof(QList<int>.Items))!.SetValue(list, items);
            return list;
        }

        if (targetType.IsArray)
        {
            var elementType = targetType.GetElementType()!;
            var items = GetItems(value).ToArray();
            var result = Array.CreateInstance(elementType, items.Length);
            for (var index = 0; index < items.Length; index++)
            {
                result.SetValue(ConvertValue(items[index], elementType), index);
            }

            return result;
        }

        if (IsCollection(targetType, out listElementType))
        {
            var result = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(listElementType))!;
            foreach (var item in GetItems(value))
            {
                result.Add(ConvertValue(item, listElementType));
            }

            return result;
        }

        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        if (targetType.IsValueType)
        {
            return Convert.ChangeType(value, targetType, System.Globalization.CultureInfo.InvariantCulture);
        }

        var target = Activator.CreateInstance(targetType)
            ?? throw new InvalidOperationException($"Cannot create team wire type '{targetType.FullName}'.");
        CopyObject(value, target);
        return target;
    }

    private static void CopyCollection(object? source, IList destination)
    {
        var elementType = GetCollectionElementType(destination.GetType())
            ?? throw new InvalidOperationException($"Cannot determine collection element type for '{destination.GetType().FullName}'.");
        var items = GetItems(source).ToList();
        if (destination.IsFixedSize)
        {
            if (destination.Count != items.Count)
            {
                throw new FhmFormatException($"Fixed team vector contains {items.Count} values; expected {destination.Count}.");
            }

            for (var index = 0; index < items.Count; index++)
            {
                destination[index] = ConvertCollectionItem(items[index], elementType, index);
            }

            return;
        }

        destination.Clear();
        for (var index = 0; index < items.Count; index++)
        {
            destination.Add(ConvertCollectionItem(items[index], elementType, index));
        }
    }

    private static object? ConvertCollectionItem(object? value, Type targetType, int index)
    {
        if (targetType == typeof(FhmActiveLineSlotList) && value is QList<int> activeLine)
        {
            var result = new FhmActiveLineSlotList((FhmLineGroup)index);
            CopyCollection(activeLine, (IList)result.PlayerReferences);
            return result;
        }

        if (targetType == typeof(FhmActiveLineSlotLockList) && value is QList<byte> activeLineLock)
        {
            var result = new FhmActiveLineSlotLockList((FhmLineGroup)index);
            CopyCollection(activeLineLock, (IList)result.Values);
            return result;
        }

        if (targetType == typeof(FhmManagedDepthChart) && value is QList<int> managedDepthChart)
        {
            var result = new FhmManagedDepthChart((FhmManagedDepthChartRole)index);
            CopyCollection(managedDepthChart, (IList)result.PlayerReferences);
            return result;
        }

        return ConvertValue(value, targetType);
    }

    private static IEnumerable<object?> GetItems(object? source)
    {
        if (source is null)
        {
            return [];
        }

        if (IsQList(source.GetType(), out _))
        {
            return ((IEnumerable)source.GetType().GetProperty(nameof(QList<int>.Items))!.GetValue(source)!).Cast<object?>();
        }

        return source switch
        {
            FhmActiveLineSlotList activeLine => activeLine.PlayerReferences.Cast<object?>(),
            FhmActiveLineSlotLockList activeLineLock => activeLineLock.Values.Cast<object?>(),
            FhmManagedDepthChart managedDepthChart => managedDepthChart.PlayerReferences.Cast<object?>(),
            IEnumerable values => values.Cast<object?>(),
            _ => throw new InvalidOperationException($"'{source.GetType().FullName}' is not a team collection."),
        };
    }

    private static bool IsQList(Type type, out Type elementType)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(QList<>))
        {
            elementType = type.GetGenericArguments()[0];
            return true;
        }

        elementType = null!;
        return false;
    }

    private static bool IsCollection(Type type, out Type elementType)
    {
        elementType = GetCollectionElementType(type)!;
        return elementType is not null;
    }

    private static Type? GetCollectionElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        var genericCollection = type.GetInterfaces()
            .Append(type)
            .FirstOrDefault(candidate => candidate.IsGenericType &&
                                         candidate.GetGenericTypeDefinition() == typeof(ICollection<>));
        return genericCollection?.GetGenericArguments()[0];
    }

    private static bool IsFhmEnumValue(Type? type) =>
        type is { IsGenericType: true } && type.GetGenericTypeDefinition() == typeof(FhmEnumValue<>);

    private static void NormalizeForWrite(FhmTeamsFileData value)
    {
        value.TeamCount = value.Teams.Count;
        foreach (var team in value.Teams)
        {
            team.SeasonHistoryCount = team.SeasonHistory.Count;
            team.FranchiseHistory.SeasonStatisticCount = team.FranchiseHistory.SeasonStatistics.Count;
            team.FranchiseHistory.NameHistoryStringCount = team.FranchiseHistory.NameHistory.Count;
            team.FranchiseHistory.AbbreviationHistoryCount = team.FranchiseHistory.AbbreviationHistory.Count;
            team.Tail.UnknownMRecordCount = checked((ushort)team.Tail.UnknownMRecords.Count);
            team.Tail.UnknownMWordCount = checked(team.Tail.UnknownMRecords.Count * 3);
            team.Tail.RetiredNumberCount = team.Tail.RetiredNumbers.Count;
            foreach (var colour in team.PostBody.Colours)
            {
                colour.Reserved = [0, 0];
            }

            foreach (var unit in team.PostBody.Units)
            {
                unit.Marker1 = 0;
                unit.Marker2 = 10;
            }
        }

        NormalizeLists(value);
    }

    private static void NormalizeLists(object value)
    {
        if (value is null || value is string or QString || value.GetType().IsValueType)
        {
            return;
        }

        if (IsQList(value.GetType(), out _))
        {
            var items = (IList)value.GetType().GetProperty(nameof(QList<int>.Items))!.GetValue(value)!;
            value.GetType().GetProperty(nameof(QList<int>.Length))!.SetValue(value, items.Count);
            foreach (var item in items)
            {
                if (item is not null)
                {
                    NormalizeLists(item);
                }
            }

            return;
        }

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is not null)
                {
                    NormalizeLists(item);
                }
            }

            return;
        }

        foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var propertyValue = property.GetValue(value);
            if (propertyValue is not null)
            {
                NormalizeLists(propertyValue);
            }
        }
    }

    private static void ValidateTeam(FhmTeamRecordData team)
    {
        if (team.SeasonHistoryCount < 0 || team.SeasonHistoryCount > MaximumCollectionCount ||
            team.SeasonHistory.Count != team.SeasonHistoryCount)
        {
            throw new FhmFormatException($"Invalid team season history count {team.SeasonHistoryCount}.");
        }

        if (team.FranchiseHistory.NameHistoryCount < 0 ||
            team.Tail.UnknownMWordCount != team.Tail.UnknownMRecordCount * 3)
        {
            throw new FhmFormatException("Team tail contains invalid count fields.");
        }

        if (team.SeasonParticipation.Blocks.Count == 0)
        {
            throw new FhmFormatException("Season participation chain requires at least one block.");
        }

        if (team.Roster.Lists.Count == 0 || team.Roster.Lists[0].Items.Count != 0)
        {
            throw new FhmFormatException("Team roster chains must begin with the empty participation delimiter list.");
        }

        if (team.PostHead.Pre.Length != 7 ||
            team.PostHead.Pre[4] != 0 ||
            team.PostHead.Pre[5] != 100 ||
            team.PostHead.Pre[6] != 1)
        {
            throw new FhmFormatException("Team post-head prefix must retain the roster-chain termination signature.");
        }

        if (team.PostBody.Colours.Any(colour => colour.Reserved.Count != 2 || colour.Reserved.Any(value => value != 0)))
        {
            throw new FhmFormatException("Invalid QColor reserved bytes.");
        }

        if (team.PostBody.Units.Any(unit => unit.Marker1 != 0 || unit.Marker2 != 10))
        {
            throw new FhmFormatException("Invalid team post-unit marker.");
        }

        ValidateTactics(team.Tail.Tactics);
    }

    private static void ValidateTactics(FhmTeamTacticsSettingsData tactics)
    {
        if (tactics.TeamValues2To4.Count != 3 ||
            tactics.BaseSettings.Count != 59 ||
            tactics.Selectors.Count != 22 ||
            tactics.FinalUseOwnSettingsFlags.Count != 2 ||
            tactics.Tendencies.Count != 22)
        {
            throw new FhmFormatException("Team tactics requires its fixed selector and tendency blocks.");
        }

        for (var index = 0; index < tactics.Selectors.Count; index++)
        {
            var selector = tactics.Selectors[index];
            if (selector.BlockIndex != index ||
                selector.SystemIds.Count != 12 ||
                selector.DelayedUseOwnSettingsFlags.Count != GetDelayedFlagCount(index))
            {
                throw new FhmFormatException("Team tactic selector blocks must remain in their serialized order.");
            }
        }

        if (tactics.Tendencies.Any(tendency => tendency.Values.Count != 8 || tendency.Overrides.Count != 8))
        {
            throw new FhmFormatException("Team tactics requires exactly eight values and overrides per tendency block.");
        }
    }

    private static int GetDelayedFlagCount(int blockIndex) => blockIndex switch
    {
        4 => 4,
        13 => 3,
        6 or 8 or 10 or 15 or 17 or 19 => 2,
        _ => 0,
    };

    private static void ValidateObject(object value)
    {
        foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var propertyValue = property.GetValue(value);
            if (propertyValue is null)
            {
                continue;
            }

            if (IsQList(property.PropertyType, out _))
            {
                ValidateQList(propertyValue, property.Name);
            }

            if (property.GetCustomAttribute<FieldCountAttribute>() is { } countAttribute)
            {
                ValidateFieldCount(value, property, propertyValue, countAttribute);
            }

            if (property.GetCustomAttribute<FieldLengthAttribute>() is { } lengthAttribute)
            {
                ValidateFieldLength(property, propertyValue, lengthAttribute);
            }

            if (propertyValue is IEnumerable enumerable && propertyValue is not string)
            {
                foreach (var item in enumerable)
                {
                    if (item is not null && !item.GetType().IsValueType && item is not string)
                    {
                        ValidateObject(item);
                    }
                }
            }
            else if (!propertyValue.GetType().IsValueType &&
                     propertyValue is not string &&
                     propertyValue is not QString)
            {
                ValidateObject(propertyValue);
            }
        }
    }

    private static void ValidateQList(object value, string fieldName)
    {
        var type = value.GetType();
        var length = (int)type.GetProperty(nameof(QList<int>.Length))!.GetValue(value)!;
        var items = (ICollection)type.GetProperty(nameof(QList<int>.Items))!.GetValue(value)!;
        if (length < 0 || length > MaximumCollectionCount || items.Count != length)
        {
            throw new FhmFormatException($"Invalid {fieldName} count {length}.");
        }
    }

    private static void ValidateFieldCount(object parent, PropertyInfo property, object value, FieldCountAttribute attribute)
    {
        var expectedCount = attribute.Path is null
            ? checked((int)attribute.ConstCount)
            : Convert.ToInt32(parent.GetType().GetProperty(attribute.Path)?.GetValue(parent)
                ?? throw new FhmFormatException($"Missing count field '{attribute.Path}'."),
                System.Globalization.CultureInfo.InvariantCulture);
        var actualCount = value is ICollection collection
            ? collection.Count
            : throw new FhmFormatException($"{property.Name} is not a collection.");
        if (expectedCount < 0 || expectedCount > MaximumCollectionCount || actualCount != expectedCount)
        {
            throw new FhmFormatException($"Invalid {property.Name} count {expectedCount}.");
        }
    }

    private static void ValidateFieldLength(PropertyInfo property, object value, FieldLengthAttribute attribute)
    {
        var expectedLength = checked((int)attribute.ConstLength);
        if (value is not byte[] bytes || bytes.Length != expectedLength)
        {
            throw new FhmFormatException($"{property.Name} must contain exactly {expectedLength} bytes.");
        }
    }
}
