using System.Data;
using Microsoft.Data.Sqlite;
using Footing.Framework.Data;
using Footing.Framework.Migrations;

namespace Footing.Framework.Tests.Migrations;

/// <summary>
/// Factory sqlite:memory com Cache=Shared isolada por Guid + keepAlive.
/// Provê DDL real para M2/M3/M5/M6. Data Source=file:memdb{Guid}?mode=memory&cache=shared
/// </summary>
public sealed class SqliteInMemoryFactory : IDbConnectionFactory, IDisposable
{
    private readonly SqliteConnection _keepAlive;
    private readonly string _cs;

    public SqliteInMemoryFactory()
    {
        _cs = $"Data Source=file:memdb{Guid.NewGuid():N}?mode=memory&cache=shared";
        _keepAlive = new SqliteConnection(_cs);
        _keepAlive.Open();
        // habilita FK para testes de FK violation
        using (var pragma = _keepAlive.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            pragma.ExecuteNonQuery();
        }
        using var cmd = _keepAlive.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS __migrations (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                version VARCHAR(50) NOT NULL UNIQUE,
                description VARCHAR(200),
                type VARCHAR(20),
                checksum VARCHAR(64),
                installed_on TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                execution_time_ms INTEGER,
                success CHAR(1) CHECK (success IN ('Y','N')),
                error_message VARCHAR(4000)
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public string ConnectionString => _cs;

    public IDbConnection CreateConnection()
    {
        var c = new SqliteConnection(_cs);
        c.Open();
        using (var pragma = c.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            pragma.ExecuteNonQuery();
        }
        return c;
    }

    public void Dispose() => _keepAlive.Dispose();
}

/// <summary>
/// Real journal over sqlite:memory via IDbConnectionFactory.
/// success CHAR(1) Y/N agnostic, proves Repair DELETE WHERE N, CHECK Y/N, etc.
/// </summary>
public sealed class SqliteJournal : IMigrationJournal
{
    private readonly IDbConnectionFactory _factory;
    private readonly string _table = "__migrations";

    public SqliteJournal(IDbConnectionFactory factory) => _factory = factory;

    public async Task EnsureHistoryTableAsync(CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            CREATE TABLE IF NOT EXISTS {_table} (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                version VARCHAR(50) NOT NULL UNIQUE,
                description VARCHAR(200),
                type VARCHAR(20),
                checksum VARCHAR(64),
                installed_on TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                execution_time_ms INTEGER,
                success CHAR(1) CHECK (success IN ('Y','N')),
                error_message VARCHAR(4000)
            );
            """;
        if (cmd is Microsoft.Data.Sqlite.SqliteCommand sc)
            await sc.ExecuteNonQueryAsync(ct);
        else
            cmd.ExecuteNonQuery();
    }

    public async Task<IReadOnlyList<string>> GetAppliedVersionsAsync(CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT version FROM {_table} WHERE success='Y' ORDER BY version";
        var list = new List<string>();
        if (cmd is Microsoft.Data.Sqlite.SqliteCommand sc)
        {
            using var r = await sc.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct)) list.Add(r.GetString(0));
        }
        else
        {
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(r.GetString(0));
        }
        return list;
    }

    public async Task<bool> HasAppliedAsync(string version, CancellationToken ct = default)
    {
        var applied = await GetAppliedVersionsAsync(ct);
        return applied.Contains(version);
    }

    public async Task<string?> GetChecksumAsync(string version, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT checksum FROM {_table} WHERE version=@v LIMIT 1";
        var p = cmd.CreateParameter(); p.ParameterName = "@v"; p.Value = version; cmd.Parameters.Add(p);
        object? result;
        if (cmd is Microsoft.Data.Sqlite.SqliteCommand sc)
            result = await sc.ExecuteScalarAsync(ct);
        else
            result = cmd.ExecuteScalar();
        return result == null || result == DBNull.Value ? null : result.ToString();
    }

    public async Task MarkAppliedAsync(MigrationInfo info, string checksum, long executionTimeMs, string successYN, string? errorMessage, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        // upsert to support REPEATABLE (INSERT OR REPLACE semantics via ON CONFLICT)
        cmd.CommandText = $"""
            INSERT INTO {_table} (version, description, type, checksum, execution_time_ms, success, error_message)
            VALUES (@v, @d, @t, @cs, @ms, @s, @e)
            ON CONFLICT(version) DO UPDATE SET
                description=excluded.description,
                type=excluded.type,
                checksum=excluded.checksum,
                execution_time_ms=excluded.execution_time_ms,
                success=excluded.success,
                error_message=excluded.error_message
            """;
        AddParam(cmd, "@v", info.Version);
        AddParam(cmd, "@d", info.Description);
        AddParam(cmd, "@t", info.Type);
        AddParam(cmd, "@cs", checksum);
        AddParam(cmd, "@ms", executionTimeMs);
        AddParam(cmd, "@s", successYN);
        AddParam(cmd, "@e", (object?)errorMessage ?? DBNull.Value);
        if (cmd is Microsoft.Data.Sqlite.SqliteCommand sc)
            await sc.ExecuteNonQueryAsync(ct);
        else
            cmd.ExecuteNonQuery();
    }

    public async Task UpdateChecksumAsync(string version, string newChecksum, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"UPDATE {_table} SET checksum=@cs WHERE version=@v";
        AddParam(cmd, "@cs", newChecksum);
        AddParam(cmd, "@v", version);
        if (cmd is Microsoft.Data.Sqlite.SqliteCommand sc) await sc.ExecuteNonQueryAsync(ct); else cmd.ExecuteNonQuery();
    }

    public async Task<IReadOnlyList<string>> GetFailedVersionsAsync(CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT version FROM {_table} WHERE success='N' ORDER BY version";
        var list = new List<string>();
        if (cmd is Microsoft.Data.Sqlite.SqliteCommand sc)
        {
            using var r = await sc.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct)) list.Add(r.GetString(0));
        }
        else
        {
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(r.GetString(0));
        }
        return list;
    }

    public async Task<IReadOnlyList<MigrationStatus>> GetInfoAsync(CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT version, description, type, checksum, installed_on FROM {_table} ORDER BY version";
        var list = new List<MigrationStatus>();
        if (cmd is Microsoft.Data.Sqlite.SqliteCommand sc)
        {
            using var r = await sc.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var ver = r.GetString(0);
                var desc = r.IsDBNull(1) ? "" : r.GetString(1);
                var type = r.IsDBNull(2) ? "VERSIONED" : r.GetString(2);
                var cs = r.IsDBNull(3) ? null : r.GetString(3);
                var installed = r.IsDBNull(4) ? (DateTime?)null : r.GetDateTime(4);
                // state derived from success
                string state = "Success";
                list.Add(new MigrationStatus(ver, desc, type, cs, installed, state));
            }
        }
        else
        {
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var ver = r.GetString(0);
                var desc = r.IsDBNull(1) ? "" : r.GetString(1);
                var type = r.IsDBNull(2) ? "VERSIONED" : r.GetString(2);
                var cs = r.IsDBNull(3) ? null : r.GetString(3);
                DateTime? installed = r.IsDBNull(4) ? null : r.GetDateTime(4);
                list.Add(new MigrationStatus(ver, desc, type, cs, installed, "Success"));
            }
        }
        return list;
    }

    public async Task RepairAsync(CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"DELETE FROM {_table} WHERE success='N'";
        if (cmd is Microsoft.Data.Sqlite.SqliteCommand sc) await sc.ExecuteNonQueryAsync(ct); else cmd.ExecuteNonQuery();
    }

    public async Task BaselineAsync(string version, CancellationToken ct = default)
    {
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"INSERT OR IGNORE INTO {_table} (version, description, type, checksum, execution_time_ms, success) VALUES (@v, 'baseline', 'BASELINE', @cs, 0, 'Y')";
        AddParam(cmd, "@v", version);
        AddParam(cmd, "@cs", MigrationChecksum.Compute("baseline:" + version));
        if (cmd is Microsoft.Data.Sqlite.SqliteCommand sc) await sc.ExecuteNonQueryAsync(ct); else cmd.ExecuteNonQuery();
    }

    private static void AddParam(IDbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }
}
