namespace Footing.Framework.Data;

public class SqlResult
{
    public string Sql { get; }
    public object? Parameters { get; }

    public SqlResult(string sql, object? parameters = null)
    {
        Sql = sql;
        Parameters = parameters;
    }
}
