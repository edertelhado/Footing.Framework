using System.Data;
using Dapper;
namespace Footing.Framework.Data.TypeHandlers;

public class StringEnumTypeHandler : SqlMapper.ITypeHandler, ITypeHandler
{
    public void SetValue(IDbDataParameter parameter, object value)
    {
        parameter.Value = value?.ToString();
        parameter.DbType = DbType.String;
    }

    public object Parse(Type destinationType, object value)
    {
        if (value is string str)
        {
            if (destinationType.IsEnum)
                return Enum.Parse(destinationType, str, ignoreCase: true);

            var nullableType = Nullable.GetUnderlyingType(destinationType);
            if (nullableType?.IsEnum == true)
                return Enum.Parse(nullableType, str, ignoreCase: true);
        }

        if (value is int or long or short or byte)
        {
            if (destinationType.IsEnum)
                return Enum.ToObject(destinationType, value);

            var nullableType = Nullable.GetUnderlyingType(destinationType);
            if (nullableType?.IsEnum == true)
                return Enum.ToObject(nullableType, value);
        }

        return value;
    }

    object? ITypeHandler.Parse(IDataReader reader, int ordinal, Type targetType)
    {
        if (reader.IsDBNull(ordinal)) return null;
        var raw = reader.GetString(ordinal);

        if (targetType.IsEnum)
            return Enum.Parse(targetType, raw, ignoreCase: true);

        var nullableType = Nullable.GetUnderlyingType(targetType);
        if (nullableType?.IsEnum == true)
            return Enum.Parse(nullableType, raw, ignoreCase: true);

        return raw;
    }

    object? ITypeHandler.Serialize(object? value)
        => value?.ToString();
}
