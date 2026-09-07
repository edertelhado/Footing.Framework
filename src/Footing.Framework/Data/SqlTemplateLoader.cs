using System.Collections.Concurrent;
using System.Reflection;
namespace Footing.Framework.Data;

public static class SqlTemplateLoader
{
    private static readonly ConcurrentDictionary<string, SqlTemplate> Cache = new();

    public static SqlTemplate Load(string resourceName, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(resourceName);
        ArgumentNullException.ThrowIfNull(assembly);

        return Cache.GetOrAdd(resourceName, name =>
        {
            var stream = assembly.GetManifestResourceStream(name)
                ?? throw new FileNotFoundException(
                    $"Embedded resource '{name}' not found in assembly '{assembly.GetName().Name}'. " +
                    $"Available resources: {string.Join(", ", GetSqlResources(assembly))}");

            using (stream)
            using (var reader = new StreamReader(stream))
            {
                return SqlTemplate.Parse(reader.ReadToEnd());
            }
        });
    }

    public static SqlTemplate For<T>(string relativePath)
        => ForAssembly(typeof(T).Assembly, relativePath);

    public static SqlTemplate ForAssembly(Assembly assembly, string relativePath)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(relativePath);

        var path = Normalize(relativePath);
        var candidates = assembly.GetManifestResourceNames()
            .Where(r => r.EndsWith(path, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0)
        {
            var all = GetSqlResources(assembly);
            var suggestion = FindBestMatch(path, all);

            throw new FileNotFoundException(
                $"No embedded resource ending with '{path}' found in '{assembly.GetName().Name}'. " +
                (suggestion != null
                    ? $"Did you mean '{suggestion}'?"
                    : $"Available .sql resources: {string.Join(", ", all)}"));
        }

        if (candidates.Count > 1)
            throw new AmbiguousMatchException(
                $"Multiple resources match '{path}': {string.Join(", ", candidates)}. " +
                "Use Load() with the full resource name instead.");

        return Load(candidates[0], assembly);
    }

    public static IReadOnlyDictionary<string, SqlTemplate> LoadAll(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var resources = GetSqlResources(assembly);
        var result = new Dictionary<string, SqlTemplate>(StringComparer.OrdinalIgnoreCase);

        foreach (var resource in resources)
            result[resource] = Load(resource, assembly);

        return result;
    }

    public static void Invalidate(string resourceName)
        => Cache.TryRemove(resourceName, out _);

    public static void ClearCache()
        => Cache.Clear();

    private static string Normalize(string relativePath)
    {
        var path = relativePath
            .Replace('/', '.')
            .Replace('\\', '.');

        if (!path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            path += ".sql";

        return path;
    }

    private static string[] GetSqlResources(Assembly assembly)
        => assembly.GetManifestResourceNames()
            .Where(r => r.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r)
            .ToArray();

    private static string? FindBestMatch(string path, string[] candidates)
    {
        var score = candidates
            .Select(c => new { Resource = c, Score = ComputeSimilarity(path, c) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        return score?.Resource;
    }

    private static int ComputeSimilarity(string a, string b)
    {
        var aParts = a.ToLowerInvariant().Split('.', StringSplitOptions.RemoveEmptyEntries);
        var bParts = b.ToLowerInvariant().Split('.', StringSplitOptions.RemoveEmptyEntries);

        var aSqlFiles = new List<string>(aParts).Where(p => p != "sql").ToList();
        var bSqlFiles = new List<string>(bParts).Where(p => p != "sql").ToList();

        var matches = aSqlFiles.Intersect(bSqlFiles).Count();
        var total = Math.Max(aSqlFiles.Count, bSqlFiles.Count);
        return total > 0 ? matches * 100 / total : 0;
    }
}
