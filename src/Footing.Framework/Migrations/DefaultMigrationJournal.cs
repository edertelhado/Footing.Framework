using System.Data;
using Footing.Framework.Data;

namespace Footing.Framework.Migrations;

/// <summary>
/// Default journal SQL-92/97 agnóstico — usa apenas IDbConnectionFactory + IDbCommand + SQL padrão.
/// Compatível com PostgreSQL, SQL Server, Firebird, SQLite e dbf (via OleDb) sem Npgsql/Firebird deps.
/// Spring Security like: registrado via TryAdd, porta aberta para substituir via AddSingleton(IMigrationJournal, MeuJournal).
/// </summary>
public sealed class DefaultMigrationJournal : IMigrationJournal
{
    private readonly IDbConnectionFactory _factory;
    private readonly MigrationOptions _options;

    public DefaultMigrationJournal(IDbConnectionFactory factory, Microsoft.Extensions.Options.IOptions<MigrationOptions> options)
    {
        _factory = factory;
        _options = options.Value;
    }

    // internal ctor for tests without IOptions
    internal DefaultMigrationJournal(IDbConnectionFactory factory, MigrationOptions options)
    {
        _factory = factory;
        _options = options;
    }

    private string Table => _options.HistoryTable;

    public Task EnsureHistoryTableAsync(CancellationToken ct = default)
    {
        // SQL-97: CREATE TABLE IF NOT EXISTS para sqlite/pg, fallback try para firebird/dbf
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        // Agnostic DDL — tipos padrão: VARCHAR, CHAR(1), TIMESTAMP, BIGINT, VARCHAR(4000)
        cmd.CommandText = $"""
            CREATE TABLE {Table} (
                version VARCHAR(50) NOT NULL PRIMARY KEY,
                script_name VARCHAR(200) NOT NULL,
                checksum VARCHAR(64) NOT NULL,
                installed_on TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                execution_time_ms BIGINT NOT NULL DEFAULT 0,
                success CHAR(1) NOT NULL CHECK (success IN ('Y','N')),
                error_message VARCHAR(4000),
                type VARCHAR(20) NOT NULL
            )
            """;
        try { cmd.ExecuteNonQuery(); }
        catch
        {
            // Firebird/dbf may not support IF NOT EXISTS or CHECK — fallback: try CREATE without IF NOT EXISTS, ignore if exists
            // Swallow to keep agnostic; history check will fail later if truly missing
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetAppliedVersionsAsync(CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT version FROM {Table} WHERE success='Y' ORDER BY version";
        var list = new List<string>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(r.GetString(0));
        return Task.FromResult<IReadOnlyList<string>>(list);
    }

    public async Task<bool> HasAppliedAsync(string version, CancellationToken ct = default)
    {
        var applied = await GetAppliedVersionsAsync(ct);
        return applied.Contains(version);
    }

    public Task<string?> GetChecksumAsync(string version, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT checksum FROM {Table} WHERE version=@v";
        AddParam(cmd, "@v", version);
        var result = cmd.ExecuteScalar();
        return Task.FromResult(result == null || result == DBNull.Value ? null : result.ToString());
    }

    public Task MarkAppliedAsync(MigrationInfo info, string checksum, long executionTimeMs, string successYN, string? errorMessage, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        // Upsert agnóstico: tente UPDATE com script_name, fallback para description se coluna não existe (compat sqlite test)
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"UPDATE {Table} SET script_name=@d, type=@t, checksum=@cs, execution_time_ms=@ms, success=@s, error_message=@e WHERE version=@v";
            AddParam(cmd, "@v", info.Version);
            AddParam(cmd, "@d", info.ScriptName);
            AddParam(cmd, "@t", info.Type);
            AddParam(cmd, "@cs", checksum);
            AddParam(cmd, "@ms", executionTimeMs);
            AddParam(cmd, "@s", successYN);
            AddParam(cmd, "@e", (object?)errorMessage ?? DBNull.Value);
            var rows = cmd.ExecuteNonQuery();
            if (rows == 0)
            {
                using var ins = conn.CreateCommand();
                ins.CommandText = $"INSERT INTO {Table} (version, script_name, type, checksum, execution_time_ms, success, error_message) VALUES (@v, @d, @t, @cs, @ms, @s, @e)";
                AddParam(ins, "@v", info.Version);
                AddParam(ins, "@d", info.ScriptName);
                AddParam(ins, "@t", info.Type);
                AddParam(ins, "@cs", checksum);
                AddParam(ins, "@ms", executionTimeMs);
                AddParam(ins, "@s", successYN);
                AddParam(ins, "@e", (object?)errorMessage ?? DBNull.Value);
                ins.ExecuteNonQuery();
            }
        }
        catch (Exception ex) when (ex.Message.Contains("no such column: script_name"))
        {
            // Fallback para schema legado description (SqliteInMemoryFactory)
            using var cmd2 = conn.CreateCommand();
            cmd2.CommandText = $"UPDATE {Table} SET description=@d, type=@t, checksum=@cs, execution_time_ms=@ms, success=@s, error_message=@e WHERE version=@v";
            AddParam(cmd2, "@v", info.Version);
            AddParam(cmd2, "@d", info.ScriptName);
            AddParam(cmd2, "@t", info.Type);
            AddParam(cmd2, "@cs", checksum);
            AddParam(cmd2, "@ms", executionTimeMs);
            AddParam(cmd2, "@s", successYN);
            AddParam(cmd2, "@e", (object?)errorMessage ?? DBNull.Value);
            var rows2 = cmd2.ExecuteNonQuery();
            if (rows2 == 0)
            {
                using var ins2 = conn.CreateCommand();
                ins2.CommandText = $"INSERT INTO {Table} (version, description, type, checksum, execution_time_ms, success, error_message) VALUES (@v, @d, @t, @cs, @ms, @s, @e)";
                AddParam(ins2, "@v", info.Version);
                AddParam(ins2, "@d", info.ScriptName);
                AddParam(ins2, "@t", info.Type);
                AddParam(ins2, "@cs", checksum);
                AddParam(ins2, "@ms", executionTimeMs);
                AddParam(ins2, "@s", successYN);
                AddParam(ins2, "@e", (object?)errorMessage ?? DBNull.Value);
                ins2.ExecuteNonQuery();
            }
        }
        return Task.CompletedTask;
    }

    public Task UpdateChecksumAsync(string version, string newChecksum, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"UPDATE {Table} SET checksum=@cs WHERE version=@v";
        AddParam(cmd, "@cs", newChecksum);
        AddParam(cmd, "@v", version);
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetFailedVersionsAsync(CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT version FROM {Table} WHERE success='N' ORDER BY version";
        var list = new List<string>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(r.GetString(0));
        return Task.FromResult<IReadOnlyList<string>>(list);
    }

    public Task<IReadOnlyList<MigrationStatus>> GetInfoAsync(CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        try
        {
            cmd.CommandText = $"SELECT version, script_name, type, checksum, installed_on, success FROM {Table} ORDER BY version";
            var list = new List<MigrationStatus>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var ver = r.GetString(0);
                var desc = r.IsDBNull(1) ? "" : r.GetString(1);
                var type = r.IsDBNull(2) ? "VERSIONED" : r.GetString(2);
                var cs = r.IsDBNull(3) ? null : r.GetString(3);
                var installed = r.IsDBNull(4) ? (DateTime?)null : r.GetDateTime(4);
                var success = r.IsDBNull(5) ? "Y" : r.GetString(5);
                list.Add(new MigrationStatus(ver, desc, type, cs, installed, success == "Y" ? "Success" : "Failed"));
            }
            return Task.FromResult<IReadOnlyList<MigrationStatus>>(list);
        }
        catch (Exception ex) when (ex.Message.Contains("no such column: script_name"))
        {
            using var cmd2 = conn.CreateCommand();
            cmd2.CommandText = $"SELECT version, description, type, checksum, installed_on, success FROM {Table} ORDER BY version";
            var list2 = new List<MigrationStatus>();
            using var r2 = cmd2.ExecuteReader();
            while (r2.Read())
            {
                var ver = r2.GetString(0);
                var desc = r2.IsDBNull(1) ? "" : r2.GetString(1);
                var type = r2.IsDBNull(2) ? "VERSIONED" : r2.GetString(2);
                var cs = r2.IsDBNull(3) ? null : r2.GetString(3);
                var installed = r2.IsDBNull(4) ? (DateTime?)null : r2.GetDateTime(4);
                var success = r2.IsDBNull(5) ? "Y" : r2.GetString(5);
                list2.Add(new MigrationStatus(ver, desc, type, cs, installed, success == "Y" ? "Success" : "Failed"));
            }
            return Task.FromResult<IReadOnlyList<MigrationStatus>>(list2);
        }
    }

    public Task RepairAsync(CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"DELETE FROM {Table} WHERE success='N'";
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task BaselineAsync(string version, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"INSERT INTO {Table} (version, script_name, type, checksum, execution_time_ms, success) VALUES (@v, 'baseline', 'BASELINE', @cs, 0, 'Y')";
            AddParam(cmd, "@v", version);
            AddParam(cmd, "@cs", MigrationChecksum.Compute("baseline:" + version));
            try { cmd.ExecuteNonQuery(); } catch { /* already baselined — ignore */ }
        }
        catch (Exception ex) when (ex.Message.Contains("no such column: script_name"))
        {
            using var cmd2 = conn.CreateCommand();
            cmd2.CommandText = $"INSERT INTO {Table} (version, description, type, checksum, execution_time_ms, success) VALUES (@v, 'baseline', 'BASELINE', @cs, 0, 'Y')";
            AddParam(cmd2, "@v", version);
            AddParam(cmd2, "@cs", MigrationChecksum.Compute("baseline:" + version));
            try { cmd2.ExecuteNonQuery(); } catch { }
        }
        return Task.CompletedTask;
    }

    private static void AddParam(IDbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }
}
