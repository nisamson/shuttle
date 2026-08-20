using System.Collections;
using System.Reflection;
using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;
using Shuttle.Fhm.Serde.Domain.Binary;
using Shuttle.Fhm.Serde.Wire.Players;

namespace Shuttle.Fhm.Serde.Domain.Files;

/// <summary>Maps the stable player object model to the BinarySerializer wire contract.</summary>
internal static class FhmPlayerWireMapper
{
    internal const int MaximumCollectionCount = 10_000_000;

    internal static FhmPlayerRecordData ToWire(FhmPlayerRecord value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var result = new FhmPlayerRecordData();
        CopyObject(value, result);
        ValidateWire(result);
        return result;
    }

    internal static FhmPlayerRecord FromWire(FhmPlayerRecordData value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ValidateWire(value);
        var result = new FhmPlayerRecord();
        CopyObject(value, result);
        return result;
    }

    internal static void ValidateWire(FhmPlayersFileData value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ValidateObject(value);
    }

    internal static void ValidateWire(FhmPlayerRecordData value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ValidateObject(value);
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

        if (targetType == typeof(QDate))
        {
            var date = (FhmDate)value!;
            return new QDate { Year = date.Year, Month = date.Month, Day = date.Day };
        }

        if (targetType == typeof(FhmDate))
        {
            var date = (QDate)value!;
            return new FhmDate(date.Year, date.Month, date.Day);
        }

        if (targetType == typeof(FhmOpaqueBytes))
        {
            return new FhmOpaqueBytes(((byte[])value!).ToArray());
        }

        if (targetType == typeof(byte[]))
        {
            return ((FhmOpaqueBytes)value!).Value.ToArray();
        }

        if (targetType == typeof(FhmOptionalPlayerRoleInstanceData))
        {
            return new FhmOptionalPlayerRoleInstanceData
            {
                Value = value is null ? null : (FhmPlayerRoleInstanceData)ConvertValue(value, typeof(FhmPlayerRoleInstanceData))!,
            };
        }

        if (targetType == typeof(FhmPlayerRoleInstance) && value is FhmOptionalPlayerRoleInstanceData optionalRole)
        {
            return optionalRole.Value is null
                ? null
                : ConvertValue(optionalRole.Value, typeof(FhmPlayerRoleInstance));
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
            ?? throw new InvalidOperationException($"Cannot create player wire type '{targetType.FullName}'.");
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
                throw new FhmFormatException($"Fixed player vector contains {items.Count} values; expected {destination.Count}.");
            }

            for (var index = 0; index < items.Count; index++)
            {
                destination[index] = ConvertValue(items[index], elementType);
            }

            return;
        }

        destination.Clear();
        foreach (var item in items)
        {
            destination.Add(ConvertValue(item, elementType));
        }
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

        return ((IEnumerable)source).Cast<object?>();
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
                     propertyValue is not QString &&
                     propertyValue is not QDate)
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
            : (int)(parent.GetType().GetProperty(attribute.Path)?.GetValue(parent)
                ?? throw new FhmFormatException($"Missing count field '{attribute.Path}'."));
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
