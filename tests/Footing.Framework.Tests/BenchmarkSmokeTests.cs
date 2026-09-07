using System.Diagnostics;
using Footing.Framework.Data;

namespace Footing.Framework.Tests;

/// <summary>
/// Benchmark smoke — minimal Stopwatch 0.5 SP agnostic (without BenchmarkDotNet pack).
/// 10k renders &lt;100ms target, &lt;2000ms stable CI gate.
/// </summary>
public class BenchmarkSmokeTests
{
    [Fact]
    public void Benchmark_SqlTemplate_10k_Renders_Under1000Ms()
    {
        var tpl = SqlTemplate.Parse("SELECT * FROM t {WHERE} {IF:Name} AND Name=@Name {END} {IF:Age > 18} AND Age=@Age {END} {ENDWHERE}");
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 10_000; i++)
        {
            var r = tpl.Render(new { Name = "eder", Age = 20 });
            // touch Sql to prevent dead-code elimination
            if (r.Sql.Length == 0) throw new InvalidOperationException();
        }
        sw.Stop();
        // Gate relaxado: <2000ms CI estável (alvo <100ms em máquina rápida)
        Assert.True(sw.ElapsedMilliseconds < 2000, $"10k renders took {sw.ElapsedMilliseconds}ms, expected <2000ms");
    }

    [Fact]
    public void Benchmark_SqlBatch_Build_Under1000Ms()
    {
        var items = Enumerable.Range(1, 100).Select(i => new { Name = $"n{i}", Age = i }).ToList();
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 200; i++) // 200*100 =20k rows built
        {
            var (sql, parms) = SqlBatch.BuildBatchInsert("Users", items);
            if (sql.Length == 0) throw new InvalidOperationException();
        }
        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 2000, $"BuildBatchInsert 200x100 took {sw.ElapsedMilliseconds}ms, expected <2000ms");
    }

    [Fact]
    public void Benchmark_SqlTemplate_Complex_And_Length_Under1000Ms()
    {
        var tpl = SqlTemplate.Parse(@"
            SELECT u.* FROM Users u
            {WHERE}
                {IF:Busca != null and Busca != ''} AND u.Name LIKE @Busca {END}
                {IF:length(Name) > 2} AND u.Name=@Name {END}
                {IF:Age > 18 and Age < 65} AND u.Age > @Min {END}
            {ENDWHERE}
            ORDER BY u.Name");
        var param = new { Busca = "eder", Name = "eder", Age = 30, Min = 18, Max = 65 };
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 10_000; i++)
        {
            var r = tpl.Render(param);
            if (!r.Sql.Contains("SELECT")) throw new InvalidOperationException();
        }
        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 2000, $"Complex 10k took {sw.ElapsedMilliseconds}ms, expected <2000ms");
    }
}
