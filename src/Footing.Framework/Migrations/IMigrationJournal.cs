namespace Footing.Framework.Migrations;

/// <summary>
/// Agnostic SPI journal — app provides implementation via IDbConnectionFactory.
/// success is CHAR(1) Y/N — 100% agnostic.
/// </summary>
public interface IMigrationJournal
{
    Task EnsureHistoryTableAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetAppliedVersionsAsync(CancellationToken ct = default);
    Task<bool> HasAppliedAsync(string version, CancellationToken ct = default);
    Task<string?> GetChecksumAsync(string version, CancellationToken ct = default);
    Task MarkAppliedAsync(MigrationInfo info, string checksum, long executionTimeMs, string successYN, string? errorMessage, CancellationToken ct = default);
    Task UpdateChecksumAsync(string version, string newChecksum, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetFailedVersionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MigrationStatus>> GetInfoAsync(CancellationToken ct = default);
    Task RepairAsync(CancellationToken ct = default);
    Task BaselineAsync(string version, CancellationToken ct = default);
}
