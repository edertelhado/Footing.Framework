namespace Footing.Framework.Migrations;

/// <summary>
/// Agnostic SPI provider — FileSystem or EmbeddedResource.
/// Returns IAsyncEnumerable for lazy streaming.
/// </summary>
public interface IMigrationScriptProvider
{
    IAsyncEnumerable<MigrationInfo> GetScriptsAsync(CancellationToken ct = default);
}
