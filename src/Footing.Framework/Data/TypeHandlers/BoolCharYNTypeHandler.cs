using System.Data;
using Dapper;

namespace Footing.Framework.Data.TypeHandlers;

/// <summary>
/// Y/N variant — bool ↔ CHAR(1) 'Y'/'N' agnostic 100% (CHAR(1) on all databases).
/// Reuses BoolCharTypeHandler S/N PT-BR logic with TrueChar='Y'.
/// </summary>
public sealed class BoolCharYNTypeHandler : SqlMapper.TypeHandler<bool>, ITypeHandler<bool>
{
    private const char TrueChar = 'Y';
    private const char FalseChar = 'N';

    public override void SetValue(IDbDataParameter parameter, bool value)
    {
        parameter.Value = value ? TrueChar.ToString() : FalseChar.ToString();
        parameter.DbType = DbType.AnsiStringFixedLength;
        parameter.Size = 1;
    }

    public override bool Parse(object value)
    {
        return value is string s
            ? s.Trim().ToUpperInvariant() == TrueChar.ToString()
            : value is char c && char.ToUpperInvariant(c) == TrueChar;
    }

    public bool Parse(IDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return false;
        var raw = reader.GetString(ordinal);
        return raw.Trim().ToUpperInvariant() == TrueChar.ToString();
    }

    public object? Serialize(bool value) => value ? TrueChar.ToString() : FalseChar.ToString();

    object? ITypeHandler.Parse(IDataReader reader, int ordinal, Type targetType)
    {
        if (reader.IsDBNull(ordinal)) return null;
        var raw = reader.GetString(ordinal);
        return raw.Trim().ToUpperInvariant() == TrueChar.ToString();
    }

    object? ITypeHandler.Serialize(object? value)
    {
        if (value is bool b) return b ? TrueChar.ToString() : FalseChar.ToString();
        return null;
    }
}
