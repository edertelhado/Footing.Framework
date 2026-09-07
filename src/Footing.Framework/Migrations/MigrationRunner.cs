using System.Diagnostics;
using System.Data;
using Footing.Framework.Data;
using Microsoft.Extensions.Logging;

namespace Footing.Framework.Migrations;

/// <summary>
/// SQL courier — agnóstico. Entrega SQL puro via IDbCommand (sem parse).
/// Y/N CHAR(1) for success, VARCHAR(4000) for error_message.
/// Checksum SHA256, bloqueia se success='N' pendente.
/// SQL puro: schema vem da connection string ou direto no .sql, sem Replace.
/// </summary>
public sealed class MigrationRunner : IMigrationRunner
{
    private readonly IMigrationJournal _journal;
    private readonly IMigrationScriptProvider _scripts;
    private readonly IDbConnectionFactory _factory;
    private readonly MigrationOptions _options;
    private readonly ILogger<MigrationRunner>? _log;

    public MigrationRunner(IMigrationJournal journal, IMigrationScriptProvider scripts, IDbConnectionFactory factory, MigrationOptions? options = null, ILogger<MigrationRunner>? log = null)
    {
        _journal = journal; _scripts = scripts; _factory = factory; _options = options ?? new(); _log = log;
        if (_options.UseTransaction) throw new NotSupportedException("DDL transactions are not supported. Use BEGIN/COMMIT inside the .sql script if your database supports transactional DDL.");
    }

    public async Task<MigrationResult> MigrateAsync(CancellationToken ct = default)
    {
        if (_options.UseTransaction) throw new NotSupportedException("DDL transactions are not supported. Use BEGIN/COMMIT inside the .sql script if your database supports transactional DDL.");
        await _journal.EnsureHistoryTableAsync(ct);
        var failed = await _journal.GetFailedVersionsAsync(ct);
        if (failed.Count > 0) throw new MigrationException($"Pending failed migrations: {string.Join(", ", failed)} — run repair");
        var applied = await _journal.GetAppliedVersionsAsync(ct);
        var list = new List<MigrationInfo>();
        await foreach (var s in _scripts.GetScriptsAsync(ct)) list.Add(s);
        list.Sort((a, b) => StringComparer.Ordinal.Compare(a.Version, b.Version));
        var appliedNow = new List<MigrationInfo>(); var skipped = new List<MigrationInfo>(); var sw = Stopwatch.StartNew();
        foreach (var script in list)
        {
            ct.ThrowIfCancellationRequested();
            if (script.Type == "REPEATABLE")
            {
                var existing = await _journal.GetChecksumAsync(script.Version, ct);
                if (existing != null && existing == script.Checksum) { skipped.Add(script); continue; }
                var sql = script.Sql;
                var checksum = MigrationChecksum.Compute(sql);
                var eSw = Stopwatch.StartNew();
                try { ExecuteRawSql(sql); eSw.Stop(); await _journal.MarkAppliedAsync(script, checksum, eSw.ElapsedMilliseconds, "Y", null, ct); appliedNow.Add(script with { Checksum = checksum, Sql = sql }); }
                catch (Exception ex) { eSw.Stop(); await _journal.MarkAppliedAsync(script, checksum, eSw.ElapsedMilliseconds, "N", ex.Message, ct); throw new MigrationException($"Migration failed at {script.Version}: {ex.Message}", ex); }
                continue;
            }
            if (applied.Contains(script.Version)) { skipped.Add(script); continue; }
            var sql2 = script.Sql;
            var cs2 = MigrationChecksum.Compute(sql2);
            var sw2 = Stopwatch.StartNew();
            try { ExecuteRawSql(sql2); sw2.Stop(); await _journal.MarkAppliedAsync(script, cs2, sw2.ElapsedMilliseconds, "Y", null, ct); appliedNow.Add(script with { Checksum = cs2, Sql = sql2 }); }
            catch (Exception ex) { sw2.Stop(); await _journal.MarkAppliedAsync(script, cs2, sw2.ElapsedMilliseconds, "N", ex.Message, ct); throw new MigrationException($"Migration failed at {script.Version}: {ex.Message}", ex); }
        }
        sw.Stop(); return new MigrationResult(appliedNow, skipped, sw.Elapsed);
    }

    public async Task ValidateAsync(CancellationToken ct = default)
    {
        var list = new List<MigrationInfo>();
        await foreach (var s in _scripts.GetScriptsAsync(ct)) list.Add(s);
        foreach (var s in list.Where(x => x.Type == "VERSIONED"))
        {
            var stored = await _journal.GetChecksumAsync(s.Version, ct);
            if (stored != null && stored != s.Checksum) throw new MigrationException($"Checksum drift detected at {s.Version}: {stored} != {s.Checksum}");
        }
    }

    public Task<IReadOnlyList<MigrationStatus>> InfoAsync(CancellationToken ct = default) => _journal.GetInfoAsync(ct);

    public Task RepairAsync(CancellationToken ct = default) => _journal.RepairAsync(ct);

    public Task BaselineAsync(string version = "0", CancellationToken ct = default) => _journal.BaselineAsync(version, ct);

    public async Task<string> GenerateScriptAsync(CancellationToken ct = default)
    {
        var applied = await _journal.GetAppliedVersionsAsync(ct);
        var sb = new System.Text.StringBuilder();
        await foreach (var s in _scripts.GetScriptsAsync(ct))
        {
            if (s.Type == "REPEATABLE")
            {
                var existing = await _journal.GetChecksumAsync(s.Version, ct);
                if (existing != null && existing == s.Checksum) continue;
            }
            else if (applied.Contains(s.Version)) continue;
            sb.AppendLine($"-- {s.ScriptName} [{s.Version}]");
            sb.AppendLine(s.Sql);
            sb.AppendLine(";");
        }
        return sb.ToString();
    }

    private void ExecuteRawSql(string sql)
    {
        using var conn = _factory.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
