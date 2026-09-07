namespace Footing.Framework.Migrations;

/// <summary>
/// Represents a migration script discovered on disk or embedded resource.
/// Version already normalized (V1_0_1 → 1.0.1), Description is suffix after "__".
/// Type: VERSIONED | REPEATABLE | BASELINE
/// </summary>
public sealed record MigrationInfo(
    string Version,
    string Description,
    string ScriptName,
    string Type,
    string Checksum,
    string Sql);

public sealed record MigrationStatus(
    string Version,
    string Description,
    string Type,
    string? Checksum,
    DateTime? InstalledOn,
    string State);

public sealed record MigrationResult(
    IReadOnlyList<MigrationInfo> Applied,
    IReadOnlyList<MigrationInfo> Skipped,
    TimeSpan Elapsed);
