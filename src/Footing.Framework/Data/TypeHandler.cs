using System.Data;
namespace Footing.Framework.Data;

public interface ITypeHandler
{
    object? Parse(IDataReader reader, int ordinal, Type targetType);
    object? Serialize(object? value);
}

public interface ITypeHandler<T> : ITypeHandler
{
    T Parse(IDataReader reader, int ordinal);
    object? Serialize(T value);

    object? ITypeHandler.Parse(IDataReader reader, int ordinal, Type targetType)
        => Parse(reader, ordinal);

    object? ITypeHandler.Serialize(object? value)
        => value is T t ? Serialize(t) : null;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false)]
public class TypeHandlerAttribute : Attribute
{
    public Type HandlerType { get; }
    public TypeHandlerAttribute(Type handlerType) => HandlerType = handlerType;
}

public class JsonTypeHandler : ITypeHandler<object>
{
    public object Parse(IDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return null!;
        return reader.GetValue(ordinal);
    }

    public object? Serialize(object? value) => value;
}

public class EnumTypeHandler : ITypeHandler<Enum>
{
    public Enum Parse(IDataReader reader, int ordinal)
    {
        var raw = reader.GetValue(ordinal);
        return (raw as Enum)!;
    }

    public object? Serialize(Enum value) => value.ToString("D");

    object? ITypeHandler.Parse(IDataReader reader, int ordinal, Type targetType)
    {
        if (reader.IsDBNull(ordinal)) return null;
        var raw = reader.GetValue(ordinal);

        if (raw is string str && targetType.IsEnum)
            return Enum.Parse(targetType, str, ignoreCase: true);

        if (raw is int or long or short or byte && targetType.IsEnum)
            return Enum.ToObject(targetType, raw);

        return raw;
    }

    object? ITypeHandler.Serialize(object? value)
        => value is Enum e ? e.ToString("D") : value;
}
