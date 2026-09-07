using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Footing.Framework.Data;

namespace Footing.Framework.Migrations;

/// <summary>
/// Default journal SQL-92/97 agnóstico — usa apenas IDbConnectionFactory + IDbCommand + SQL padrão.
/// Compatível com PostgreSQL, SQL Server, Firebird, SQLite e dbf (via OleDb) sem Npgsql/Firebird deps.
/// Spring Security like: registrado via TryAdd, porta aberta para substituir via AddSingleton(IMigrationJournal, MeuJournal).
/// </summary>
public sealed class DefaultMigrationJournal : IMigrationJournal
{
    private static readonly Regex TableRx = new(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$", RegexOptions.Compiled);
    private static readonly Regex VerRx = new(@"^[A-Za-z0-9._\-]{1,50}$", RegexOptions.Compiled);
    private readonly IDbConnectionFactory _factory;
    private readonly MigrationOptions _options;
    private readonly ILogger<DefaultMigrationJournal>? _log;

    public DefaultMigrationJournal(IDbConnectionFactory factory, Microsoft.Extensions.Options.IOptions<MigrationOptions> options, ILogger<DefaultMigrationJournal>? log = null)
    {
        _factory = factory;
        _options = options.Value;
        _log = log;
    }

    // internal ctor for tests without IOptions
    internal DefaultMigrationJournal(IDbConnectionFactory factory, MigrationOptions options, ILogger<DefaultMigrationJournal>? log = null)
    {
        _factory = factory;
        _options = options;
        _log = log;
    }

    private string Table => _options.HistoryTable;

    private void ValidateTable()
    {
        if (string.IsNullOrWhiteSpace(Table) || !TableRx.IsMatch(Table))
            throw new ArgumentException($"HistoryTable inválido: '{Table}'", nameof(MigrationOptions.HistoryTable));
    }

    private static void ValidateVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version) || version.Length > 50 || !VerRx.IsMatch(version))
            throw new ArgumentException($"Version inválida: '{version}'", nameof(version));
        if (version.Any(c => char.IsControl(c) || c == '\u200B' || c == '\uFEFF'))
            throw new ArgumentException("Version contém caracteres de controle", nameof(version));
    }

    private static bool IsDuplicateKey(Exception ex)
    {
        var m = ex.Message ?? "";
        return m.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
            || m.Contains("PRIMARY", StringComparison.OrdinalIgnoreCase)
            || m.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
            || m.Contains("already exists", StringComparison.OrdinalIgnoreCase)
            || m.Contains("constraint", StringComparison.OrdinalIgnoreCase);
    }

    private bool HasColumn(IDbConnection conn, string col)
    {
        try
        {
            using var probe = conn.CreateCommand();
            probe.CommandText = $"SELECT {col} FROM {Table} WHERE 1=0";
            probe.ExecuteNonQuery();
            return true;
        }
        catch { return false; }
    }

    public Task EnsureHistoryTableAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ValidateTable();
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
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
        catch (Exception ex) when (IsDuplicateKey(ex)) { _log?.LogDebug(ex, "History table {Table} já existe", Table); }
        catch (Exception ex) { _log?.LogError(ex, "EnsureHistoryTable falhou {Table}", Table); throw; }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetAppliedVersionsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ValidateTable();
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
        ct.ThrowIfCancellationRequested();
        ValidateTable(); ValidateVersion(version);
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
        ct.ThrowIfCancellationRequested();
        ValidateTable(); ValidateVersion(info.Version);
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        bool hasScriptName = HasColumn(conn, "script_name");
        string col = hasScriptName ? "script_name" : "description";
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"UPDATE {Table} SET {col}=@d, type=@t, checksum=@cs, execution_time_ms=@ms, success=@s, error_message=@e WHERE version=@v";
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
            ins.CommandText = $"INSERT INTO {Table} (version, {col}, type, checksum, execution_time_ms, success, error_message) VALUES (@v, @d, @t, @cs, @ms, @s, @e)";
            AddParam(ins, "@v", info.Version);
            AddParam(ins, "@d", info.ScriptName);
            AddParam(ins, "@t", info.Type);
            AddParam(ins, "@cs", checksum);
            AddParam(ins, "@ms", executionTimeMs);
            AddParam(ins, "@s", successYN);
            AddParam(ins, "@e", (object?)errorMessage ?? DBNull.Value);
            try { ins.ExecuteNonQuery(); }
            catch (Exception ex) when (IsDuplicateKey(ex)) { _log?.LogDebug(ex, "MarkApplied duplicate {Version} — ignorado", info.Version); }
            catch (Exception ex) { _log?.LogError(ex, "MarkApplied falhou {Version}", info.Version); throw; }
        }
        return Task.CompletedTask;
    }

    public Task UpdateChecksumAsync(string version, string newChecksum, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ValidateTable(); ValidateVersion(version);
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
        ct.ThrowIfCancellationRequested();
        ValidateTable();
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
        ct.ThrowIfCancellationRequested();
        ValidateTable();
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        bool hasScriptName = HasColumn(conn, "script_name");
        string col = hasScriptName ? "script_name" : "description";
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT version, {col}, type, checksum, installed_on, success FROM {Table} ORDER BY version";
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

    public Task RepairAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ValidateTable();
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"DELETE FROM {Table} WHERE success='N'";
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task BaselineAsync(string version, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ValidateTable(); ValidateVersion(version);
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        bool hasScriptName = HasColumn(conn, "script_name");
        string col = hasScriptName ? "script_name" : "description";
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"INSERT INTO {Table} (version, {col}, type, checksum, execution_time_ms, success) VALUES (@v, 'baseline', 'BASELINE', @cs, 0, 'Y')";
        AddParam(cmd, "@v", version);
        AddParam(cmd, "@cs", MigrationChecksum.Compute("baseline:" + version));
        try { cmd.ExecuteNonQuery(); }
        catch (Exception ex) when (IsDuplicateKey(ex)) { _log?.LogDebug(ex, "Baseline {Version} já existe — ignorado", version); }
        catch (Exception ex) { _log?.LogError(ex, "Baseline falhou {Version}", version); throw; }
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
