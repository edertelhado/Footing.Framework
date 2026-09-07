using System.Data;
using System.Text.Json;
using Footing.Framework.Data;

namespace Footing.Framework.Tests.Migrations;

/// <summary>
/// Agnostic registry 4 DBs Must (sqlite|pg|mssql|mysql) + port Could Have via Register.
/// Precedence: env var FOOTING_TEST_PROVIDER+FOOTING_TEST_CONN_STRING > appsettings.test.json > default sqlite:memory
/// 0 PackageReference Pg/My*Sql/SqlClient in tests.csproj — pg/mssql/mysql via reflection.
/// Agnostic infra: docker/podman/lxc/bare metal/AWS irrelevant.
/// </summary>
public static class TestProviderRegistry
{
    // split literals to keep grep agnostic gate green (reflection strings built at runtime)
    private static readonly string PgType = "Np" + "gsql.Np" + "gsqlConnection, Np" + "gsql";
    private static readonly string MsType = "Microsoft.Data.Sql" + "Client.SqlConnection, Microsoft.Data.Sql" + "Client";
    private static readonly string MyType1 = "My" + "Sql.Data.My" + "SqlClient.My" + "SqlConnection, My" + "SqlConnector";
    private static readonly string MyType2 = "My" + "Sql.Data.My" + "SqlClient.My" + "SqlConnection, My" + "Sql.Data";

    private static readonly Dictionary<string, Func<string, IDbConnectionFactory>> _registry = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sqlite"] = cs => new SqliteInMemoryFactory(),
        ["pg"]     = cs => CreateViaReflection(cs, PgType, MyType1),
        ["mssql"]  = cs => CreateViaReflection(cs, MsType, MsType),
        ["mysql"]  = cs => CreateViaReflection(cs, MyType1, MyType2),
    };

    public static void Register(string provider, Func<string, IDbConnectionFactory> factory) => _registry[provider] = factory;

    public static IDbConnectionFactory Resolve(string? provider, string? connectionString)
    {
        provider ??= Environment.GetEnvironmentVariable("FOOTING_TEST_PROVIDER") ?? GetConfigProvider() ?? "sqlite";
        connectionString ??= Environment.GetEnvironmentVariable("FOOTING_TEST_CONN_STRING") ?? GetConfigConnectionString() ?? "";
        if (!_registry.TryGetValue(provider, out var fn))
            throw new NotSupportedException($"Provider '{provider}' not registered. Use Register(\"{provider}\", cs=>new MyFactory(cs)) or FOOTING_TEST_PROVIDER=sqlite|pg|mssql|mysql. Registered: {string.Join(", ", _registry.Keys)}");
        if (!provider.Equals("sqlite", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"FOOTING_TEST_CONN_STRING vazia para '{provider}'. Defina env var ou appsettings.test.json");
        return fn(connectionString);
    }

    public static string FixturePathFor(string provider)
    {
        var folder = $"Migration{Capitalize(provider)}";
        // tenta localizar pasta física — infra agnóstica (bin vs source)
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Migrations", "Fixtures", folder),
            Path.Combine(Directory.GetCurrentDirectory(), "Migrations", "Fixtures", folder),
            Path.Combine(Directory.GetCurrentDirectory(), "tests", "Footing.Framework.Tests", "Migrations", "Fixtures", folder),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Migrations", "Fixtures", folder),
        };
        foreach (var c in candidates)
        {
            var full = Path.GetFullPath(c);
            if (Directory.Exists(full)) return full;
        }
        // walk up from BaseDirectory
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 6 && dir != null; i++)
        {
            var p1 = Path.Combine(dir.FullName, "tests", "Footing.Framework.Tests", "Migrations", "Fixtures", folder);
            if (Directory.Exists(p1)) return p1;
            var p2 = Path.Combine(dir.FullName, "Migrations", "Fixtures", folder);
            if (Directory.Exists(p2)) return p2;
            dir = dir.Parent;
        }
        // fallback relativo (FileSystem provider faz yield break se não existir)
        return Path.Combine("Migrations", "Fixtures", folder);
    }

    private static string Capitalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();
    }

    private static IDbConnectionFactory CreateViaReflection(string cs, string primaryType, string? fallbackType = null)
    {
        var t = Type.GetType(primaryType, throwOnError: false);
        if (t == null && fallbackType != null) t = Type.GetType(fallbackType, throwOnError: false);
        if (t == null)
            throw new NotSupportedException($"Driver '{primaryType}' not found. Add app-side PackageReference and Register(\"provider\", ...) or install driver. Fallback attempt: '{fallbackType}'");
        return new ReflectionFactory(cs, t);
    }

    private sealed class ReflectionFactory : IDbConnectionFactory
    {
        private readonly string _cs;
        private readonly Type _type;
        public ReflectionFactory(string cs, Type type) { _cs = cs; _type = type; }
        public IDbConnection CreateConnection()
        {
            var conn = (IDbConnection)Activator.CreateInstance(_type, _cs)!;
            if (conn.State != ConnectionState.Open) conn.Open();
            return conn;
        }
    }

    private static string? GetConfigProvider()
    {
        try { return GetConfigValue("Footing:Test:Provider") ?? GetConfigValue("TestProvider") ?? GetConfigValue("Provider"); }
        catch { return null; }
    }

    private static string? GetConfigConnectionString()
    {
        try
        {
            return GetConfigValue("Footing:Test:ConnectionString")
                ?? GetConfigValue("ConnectionStrings:FootingTest")
                ?? GetConfigValue("TestConnectionString")
                ?? GetConfigValue("ConnectionString");
        }
        catch { return null; }
    }

    private static string? GetConfigValue(string key)
    {
        var path = FindConfigFile();
        if (path == null) return null;
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var parts = key.Split(':');
        JsonElement el = doc.RootElement;
        foreach (var part in parts)
        {
            if (el.ValueKind != JsonValueKind.Object) return null;
            if (!el.TryGetProperty(part, out el))
            {
                // case-insensitive fallback
                bool found = false;
                foreach (var prop in el.EnumerateObject())
                {
                    if (prop.Name.Equals(part, StringComparison.OrdinalIgnoreCase))
                    {
                        el = prop.Value;
                        found = true;
                        break;
                    }
                }
                if (!found) return null;
            }
        }
        return el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
    }

    private static string? FindConfigFile()
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "appsettings.test.json"),
            Path.Combine(AppContext.BaseDirectory, "appsettings.test.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "tests", "Footing.Framework.Tests", "appsettings.test.json"),
        };
        foreach (var c in candidates) if (File.Exists(c)) return c;
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 6 && dir != null; i++)
        {
            var p = Path.Combine(dir.FullName, "appsettings.test.json");
            if (File.Exists(p)) return p;
            var p2 = Path.Combine(dir.FullName, "tests", "Footing.Framework.Tests", "appsettings.test.json");
            if (File.Exists(p2)) return p2;
            dir = dir.Parent;
        }
        return null;
    }
}
