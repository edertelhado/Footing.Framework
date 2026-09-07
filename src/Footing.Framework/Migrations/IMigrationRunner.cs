namespace Footing.Framework.Migrations;

/// <summary>
/// SPI runner — orquestra journal + provider via IDbConnectionFactory.
/// </summary>
public interface IMigrationRunner
{
    Task<MigrationResult> MigrateAsync(CancellationToken ct = default);
    Task ValidateAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MigrationStatus>> InfoAsync(CancellationToken ct = default);
    Task RepairAsync(CancellationToken ct = default);
    Task BaselineAsync(string version = "0", CancellationToken ct = default);
    Task<string> GenerateScriptAsync(CancellationToken ct = default);
}
