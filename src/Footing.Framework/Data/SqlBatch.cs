using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using Dapper;

namespace Footing.Framework.Data;

/// <summary>
/// Dapper-friendly helper for chunked INSERT batch.
/// Generates INSERT INTO t (cols) VALUES (@p0_Col,...),(@p1_Col,...) with DynamicParameters.
/// Chunk default 500, with auto-calc to respect SQL Server 2100 params limit (effective = min(batchSize, 2100/colCount)).
/// DB-agnostic (PG/MySQL/SQLite/SQLServer), parameterized, without DataTable.
/// </summary>
public static class SqlBatch
{
    /// <summary>Global: quando true, todo Batch usa snake_case sem precisar passar useSnakeCase por chamada. Ativa Dapper MatchNamesWithUnderscores no startup.</summary>
    public static bool GlobalUseSnakeCase { get; set; }

    /// <summary>Ativa globalmente: Batch gera snake_case + Dapper mapeia user_name → UserName sem aspas no PG.</summary>
    public static void EnableGlobalSnakeCase() { GlobalUseSnakeCase = true; Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true; }

    /// <summary>
    /// Generates SQL + DynamicParameters without executing (testable without DB).
    /// useSnakeCase: PascalCase → snake_case para colunas (PostgreSQL sem aspas). Também ativa Dapper DefaultTypeMap.MatchNamesWithUnderscores.
    /// </summary>
    public static (string Sql, DynamicParameters Parameters) BuildBatchInsert<T>(
        string tableName,
        IEnumerable<T> items,
        string? columnsOverride = null,
        bool useSnakeCase = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ValidateTableName(tableName);
        ArgumentNullException.ThrowIfNull(items);
        var snake = useSnakeCase || GlobalUseSnakeCase;
        if (snake) EnableSnakeCaseMapping();

        var list = items as IReadOnlyList<T> ?? items.ToList();
        if (list.Count == 0) return ("", new DynamicParameters());

        var props = GetProperties<T>(columnsOverride);
        if (props.Length == 0) return ("", new DynamicParameters());

        var cols = string.Join(", ", props.Select(p => snake ? ToSnakeCase(p.Name) : p.Name));
        var sql = $"INSERT INTO {tableName} ({cols}) VALUES ";

        var parms = new DynamicParameters();
        var rows = new List<string>(list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            var rowParams = props.Select(p => $"@{ParamName(i, p.Name)}");
            rows.Add($"({string.Join(", ", rowParams)})");
            foreach (var p in props)
                parms.Add(ParamName(i, p.Name), p.GetValue(list[i]));
        }
        sql += string.Join(", ", rows);
        return (sql, parms);
    }

    /// <summary>
    /// Executes chunked INSERT batch. Each chunk is a VALUES statement with batchSize rows.
    /// Auto-calc effectiveBatch = min(batchSize, 2100/colCount) to avoid SQL Server overflow when colCount>0.
    /// useSnakeCase: PascalCase → snake_case para colunas (PostgreSQL sem aspas). Também ativa Dapper DefaultTypeMap.MatchNamesWithUnderscores.
    /// </summary>
    public static async Task<int> InsertBatchAsync<T>(
        IDbConnection connection,
        string tableName,
        IEnumerable<T> items,
        IDbTransaction? transaction = null,
        int batchSize = 500,
        bool useSnakeCase = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ValidateTableName(tableName);
        ArgumentNullException.ThrowIfNull(items);
        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize), "batchSize must be > 0");
        var snake1 = useSnakeCase || GlobalUseSnakeCase;
        if (snake1) EnableSnakeCaseMapping();

        var list = items as IReadOnlyList<T> ?? items.ToList();
        if (list.Count == 0) return 0;

        var props = GetProperties<T>(null); // for colCount calc (override not used here unless caller uses BuildBatchInsert directly)
        // If caller wants columnsOverride on execution, use overload with columnsOverride string? For v1.1.0 keep simple: caller should call BuildBatchInsert with override manually.
        // To support override on InsertBatchAsync, we add optional param via overload below; but keep main logic using props length for chunk calc inclusive of override case.
        // Compute effective batch size respecting 2100 params limit (SQL Server). For generic DB, this is safe conservative chunk.
        int effectiveBatch = batchSize;
        if (props.Length > 0)
        {
            int maxRows = 2100 / props.Length;
            if (maxRows < 1) maxRows = 1;
            if (maxRows < effectiveBatch) effectiveBatch = maxRows;
        }

        var total = 0;
        foreach (var chunk in list.Chunk(effectiveBatch))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (sql, parms) = BuildBatchInsert(tableName, chunk, null, snake1);
            if (string.IsNullOrEmpty(sql)) continue;
            total += await connection.ExecuteAsync(new CommandDefinition(sql, parms, transaction, cancellationToken: cancellationToken));
        }
        return total;
    }

    /// <summary>
    /// Overload with columnsOverride for execution. useSnakeCase: PascalCase → snake_case para colunas.
    /// </summary>
    public static async Task<int> InsertBatchAsync<T>(
        IDbConnection connection,
        string tableName,
        IEnumerable<T> items,
        string columnsOverride,
        IDbTransaction? transaction = null,
        int batchSize = 500,
        bool useSnakeCase = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ValidateTableName(tableName);
        ArgumentNullException.ThrowIfNull(items);
        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize), "batchSize must be > 0");
        var snake2 = useSnakeCase || GlobalUseSnakeCase;
        if (snake2) EnableSnakeCaseMapping();

        var list = items as IReadOnlyList<T> ?? items.ToList();
        if (list.Count == 0) return 0;

        var props = GetProperties<T>(columnsOverride);
        int effectiveBatch = batchSize;
        if (props.Length > 0)
        {
            int maxRows = 2100 / props.Length;
            if (maxRows < 1) maxRows = 1;
            if (maxRows < effectiveBatch) effectiveBatch = maxRows;
        }

        var total = 0;
        foreach (var chunk in list.Chunk(effectiveBatch))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (sql, parms) = BuildBatchInsert(tableName, chunk, columnsOverride, snake2);
            if (string.IsNullOrEmpty(sql)) continue;
            total += await connection.ExecuteAsync(new CommandDefinition(sql, parms, transaction, cancellationToken: cancellationToken));
        }
        return total;
    }

    public static Task<int> InsertBatchAsync<T>(
        UnitOfWork uow,
        string tableName,
        IEnumerable<T> items,
        int batchSize = 500,
        bool useSnakeCase = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uow);
        return InsertBatchAsync(uow.Connection, tableName, items, uow.Transaction, batchSize, useSnakeCase, cancellationToken);
    }

    public static Task<int> InsertBatchAsync<T>(
        UnitOfWork uow,
        string tableName,
        IEnumerable<T> items,
        string columnsOverride,
        int batchSize = 500,
        bool useSnakeCase = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uow);
        return InsertBatchAsync(uow.Connection, tableName, items, columnsOverride, uow.Transaction, batchSize, useSnakeCase, cancellationToken);
    }

    private static void ValidateTableName(string tableName)
    {
        if (!Regex.IsMatch(tableName, @"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$"))
            throw new ArgumentException("tableName contains invalid characters; allowed: A-Z, 0-9, _, .", nameof(tableName));
    }

    private static string ParamName(int idx, string col) => $"p{idx}_{col}";

    private static PropertyInfo[] GetProperties<T>(string? cols)
    {
        var all = PropsCacheHelper.Get(typeof(T));
        if (string.IsNullOrWhiteSpace(cols)) return all;
        var set = cols.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return all.Where(p => set.Contains(p.Name)).ToArray();
    }

    public static void EnableSnakeCaseMapping() => Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var sb = new System.Text.StringBuilder(name.Length + 5);
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else sb.Append(c);
        }
        return sb.ToString();
    }
}
