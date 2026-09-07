namespace Footing.Framework.Migrations;

public sealed class FileSystemMigrationScriptProvider : IMigrationScriptProvider
{
    private readonly string _folder;
    public FileSystemMigrationScriptProvider(string folder) => _folder = folder;

    public async IAsyncEnumerable<MigrationInfo> GetScriptsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!Directory.Exists(_folder)) yield break;
        foreach (var file in Directory.GetFiles(_folder, "*.sql", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            var sql = await File.ReadAllTextAsync(file, ct);
            var folderHint = Path.GetDirectoryName(file);
            if (MigrationVersionParser.TryParse(file, folderHint, sql, out var info)) yield return info;
        }
    }
}

public sealed class EmbeddedResourceMigrationScriptProvider : IMigrationScriptProvider
{
    private readonly System.Reflection.Assembly _asm;
    private readonly string? _prefix;
    public EmbeddedResourceMigrationScriptProvider(System.Reflection.Assembly asm, string? prefix = null) { _asm = asm; _prefix = prefix; }

    public async IAsyncEnumerable<MigrationInfo> GetScriptsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var names = _asm.GetManifestResourceNames().Where(n => n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)).OrderBy(n => n, StringComparer.Ordinal);
        foreach (var name in names)
        {
            ct.ThrowIfCancellationRequested();
            if (_prefix != null && !name.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase)) continue;
            using var s = _asm.GetManifestResourceStream(name);
            if (s == null) continue;
            using var r = new StreamReader(s);
            var sql = await r.ReadToEndAsync(ct);
            var fileName = ExtractFileNameFromResourceName(name);
            if (MigrationVersionParser.TryParse(fileName, null, sql, out var info)) yield return info;
        }
    }

    private static string ExtractFileNameFromResourceName(string resourceName)
    {
        var lastDot = resourceName.LastIndexOf('.');
        if (lastDot < 0) return resourceName;
        var ext = resourceName[lastDot..];
        var nameWithoutExt = resourceName[..lastDot];
        var lastUnderscoreDouble = nameWithoutExt.LastIndexOf("__");
        if (lastUnderscoreDouble < 0) return resourceName;
        var description = nameWithoutExt[(lastUnderscoreDouble + 2)..];
        var prefix = nameWithoutExt[..lastUnderscoreDouble];
        var lastDotInPrefix = prefix.LastIndexOf('.');
        var versionPrefix = lastDotInPrefix >= 0 ? prefix[(lastDotInPrefix + 1)..] : prefix;
        return $"{versionPrefix}__{description}{ext}";
    }
}
