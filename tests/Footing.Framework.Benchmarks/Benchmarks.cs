using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Dapper;
using Footing.Framework.Data;

/// <summary>
/// Formal benchmark suite — BenchmarkDotNet PrivateAssets + Stopwatch smoke gate &lt;2000ms 10k renders.
/// Smoke valida CI rápido (Stopwatch); BenchmarkDotNet mede throughput/latência para venda (artifacts/benchmark).
/// Run smoke: dotnet run -c Release --project tests/Footing.Framework.Benchmarks -- smoke
/// Run formal: dotnet run -c Release --project tests/Footing.Framework.Benchmarks
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class Benchmarks
{
    private readonly SqlTemplate _tplSimple = SqlTemplate.Parse("SELECT * FROM t {WHERE} {IF:Name} AND Name=@Name {END} {ENDWHERE}");
    private readonly SqlTemplate _tplComplex = SqlTemplate.Parse(@"
            SELECT u.* FROM Users u
            {WHERE}
                {IF:Busca != null and Busca != ''} AND u.Name LIKE @Busca {END}
                {IF:length(Name) > 2} AND u.Name=@Name {END}
                {IF:Age > 18 and Age < 65} AND u.Age > @Min {END}
            {ENDWHERE}
            ORDER BY u.Name");

    // --- BenchmarkDotNet formal (PrivateAssets) ---

    [Benchmark(Description = "Render Simple 1 param")]
    public SqlResult Render_Simple() => _tplSimple.Render(new { Name = "eder" });

    [Benchmark(Description = "Render Complex and/length")]
    public SqlResult Render_Complex() => _tplComplex.Render(new { Busca = "eder", Name = "eder", Age = 30, Min = 18, Max = 65 });

    [Benchmark(Description = "BuildBatchInsert 100 rows")]
    public (string Sql, DynamicParameters Parameters) BuildBatchInsert_100()
    {
        var items = Enumerable.Range(1, 100).Select(i => new { Name = $"n{i}", Age = i }).ToList();
        return SqlBatch.BuildBatchInsert("Users", items);
    }

    // --- Smoke gate 10k <2000ms (Stopwatch, sem BenchmarkDotNet) ---

    public TimeSpan Run_10k_Renders()
    {
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 10_000; i++) _ = _tplSimple.Render(new { Name = "eder" });
        sw.Stop();
        return sw.Elapsed;
    }

    public TimeSpan Run_10k_Complex()
    {
        var param = new { Busca = "eder", Name = "eder", Age = 30, Min = 18, Max = 65 };
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 10_000; i++) _ = _tplComplex.Render(param);
        sw.Stop();
        return sw.Elapsed;
    }

    public static int Main(string[] args)
    {
        // smoke mode: dotnet run ... -- smoke
        if (args.Contains("smoke", StringComparer.OrdinalIgnoreCase))
        {
            var b = new Benchmarks();
            var elapsed = b.Run_10k_Renders();
            var elapsed2 = b.Run_10k_Complex();
            Console.WriteLine($"Smoke Simple 10k renders: {elapsed.TotalMilliseconds:F1}ms (gate <2000ms)");
            Console.WriteLine($"Smoke Complex 10k renders: {elapsed2.TotalMilliseconds:F1}ms (gate <2000ms)");
            bool ok = elapsed.TotalMilliseconds < 2000 && elapsed2.TotalMilliseconds < 2000;
            Console.WriteLine(ok ? "PASS smoke <2000ms" : "FAIL smoke >=2000ms");
            return ok ? 0 : 1;
        }

        // smoke sempre antes do BenchmarkDotNet (fail-fast CI)
        var smoke = new Benchmarks();
        var sm = smoke.Run_10k_Renders();
        Console.WriteLine($"Pre-check Smoke 10k: {sm.TotalMilliseconds:F1}ms (gate <2000ms)");
        if (sm.TotalMilliseconds >= 2000)
        {
            Console.WriteLine("FAIL smoke >=2000ms — abort BenchmarkDotNet");
            return 1;
        }

        // formal BenchmarkDotNet
        var summary = BenchmarkRunner.Run<Benchmarks>();
        Console.WriteLine($"BenchmarkDotNet done — results in BenchmarkDotNet.Artifacts/");
        return 0;
    }
}
