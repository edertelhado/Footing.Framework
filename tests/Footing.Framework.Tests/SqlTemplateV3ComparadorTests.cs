using Footing.Framework.Data;

namespace Footing.Framework.Tests;

/// <summary>
/// Hotfix v2.0.0-alpha.1 — Opção C print-08 §6.
/// Cobre Comparador > < >= <= com não-numérico: falso+Warning, não D size.
/// Total 68 -> 71 (3 novos testes).
/// </summary>
public class SqlTemplateV3ComparadorTests
{
    private static string Render(string sql, object? p) => SqlTemplate.Parse(sql).Render(p).Sql;

    // 1) string vs número → falso, mantém numérico e string numérica
    [Fact]
    public void V3_Comparador_StringVsNumero_RetornaFalse_CorrigeBugAbcGt100()
    {
        // Bug antes: "abc" >100 → true (Ordinal a97 > 149). C: false+Warning
        Assert.DoesNotContain("AND", Render("{IF:Nome > 100} AND x=1 {END}", new { Nome = "abc" }));
        Assert.DoesNotContain("AND", Render("{IF:Nome >= 100} AND x=1 {END}", new { Nome = "abc" }));
        Assert.DoesNotContain("AND", Render("{IF:Nome < 100} AND x=1 {END}", new { Nome = "abc" }));
        Assert.DoesNotContain("AND", Render("{IF:Nome <= 100} AND x=1 {END}", new { Nome = "abc" }));
        // via @Param
        Assert.DoesNotContain("AND", Render("{IF:Nome > @Limite} AND x=1 {END}", new { Nome = "abc", Limite = 100 }));
        Assert.DoesNotContain("AND", Render("{IF:Nome > @Limite} AND x=1 {END}", new { Nome = "abc", Limite = 2 }));
        // D-size NÃO implementado: "abc"(3) >2 deveria ser false em C, true em D inventado
        Assert.DoesNotContain("AND", Render("{IF:Nome > 2} AND x=1 {END}", new { Nome = "abc" }));
        Assert.DoesNotContain("AND", Render("{IF:Preco > 'abc'} AND x=1 {END}", new { Preco = 10 }));
        // número > string também false
        Assert.DoesNotContain("AND", Render("{IF:Preco > 'abc'} AND x=1 {END}", new { Preco = 2 }));
        // mas numérico válido mantém true
        Assert.Contains("AND", Render("{IF:Preco > 100} AND x=1 {END}", new { Preco = 150 }));
        Assert.Contains("AND", Render("{IF:Preco > 100} AND x=1 {END}", new { Preco = "150" })); // string numérica
        Assert.DoesNotContain("AND", Render("{IF:Preco > 100} AND x=1 {END}", new { Preco = "50" }));
        // == != mantém Ordinal/númerico (não afetado por C)
        Assert.DoesNotContain("AND", Render("{IF:Nome == 100} AND x=1 {END}", new { Nome = "abc" }));
        Assert.Contains("AND", Render("{IF:Nome != 100} AND x=1 {END}", new { Nome = "abc" }));
    }

    // 2) string vs string mantém Ordinal
    [Fact]
    public void V3_Comparador_StringVsString_MantemOrdinal()
    {
        Assert.Contains("AND", Render("{IF:Name > 'adam'} AND x=1 {END}", new { Name = "eder" }));
        Assert.DoesNotContain("AND", Render("{IF:Name > 'eder'} AND x=1 {END}", new { Name = "adam" }));
        Assert.Contains("AND", Render("{IF:Name >= 'adam'} AND x=1 {END}", new { Name = "eder" }));
        Assert.Contains("AND", Render("{IF:Name <= 'adam'} AND x=1 {END}", new { Name = "adam" }));
        Assert.DoesNotContain("AND", Render("{IF:Name < 'adam'} AND x=1 {END}", new { Name = "eder" }));
        // via @Param string vs string
        Assert.Contains("AND", Render("{IF:Name > @Outro} AND x=1 {END}", new { Name = "eder", Outro = "adam" }));
        Assert.DoesNotContain("AND", Render("{IF:Name > @Outro} AND x=1 {END}", new { Name = "adam", Outro = "eder" }));
        // acento ordinal vs cultura: 'é' (233) > 'e' (101) Ordinal true
        Assert.Contains("AND", Render("{IF:Name > 'e'} AND x=1 {END}", new { Name = "é" }));
        // igualdade string
        Assert.Contains("AND", Render("{IF:Name == 'eder'} AND x=1 {END}", new { Name = "eder" }));
    }

    // 3) Data cronológica + DateTime via @Param + null handling
    [Fact]
    public void V3_Comparador_DataString_E_DateTime_Cronologico_ENull()
    {
        // string data vs string data cronológico (TryToDateTime)
        Assert.Contains("AND", Render("{IF:Data > '2024-01-01'} AND x=1 {END}", new { Data = "2024-12-31" }));
        Assert.DoesNotContain("AND", Render("{IF:Data > '2024-12-31'} AND x=1 {END}", new { Data = "2024-01-01" }));
        Assert.Contains("AND", Render("{IF:Data >= '2024-01-01'} AND x=1 {END}", new { Data = "2024-01-01" }));
        Assert.Contains("AND", Render("{IF:Data < '2024-12-31'} AND x=1 {END}", new { Data = "2024-01-01" }));
        // DateTime via @Param
        Assert.Contains("AND", Render("{IF:Data > @Outro} AND x=1 {END}", new { Data = new DateTime(2024, 12, 31), Outro = new DateTime(2024, 1, 1) }));
        Assert.DoesNotContain("AND", Render("{IF:Data > @Outro} AND x=1 {END}", new { Data = new DateTime(2024, 1, 1), Outro = new DateTime(2024, 12, 31) }));
        // DateTime vs string literal (misto) cronológico
        Assert.Contains("AND", Render("{IF:Data > '2024-01-01'} AND x=1 {END}", new { Data = new DateTime(2024, 12, 31) }));
        Assert.DoesNotContain("AND", Render("{IF:Data > '2024-12-31'} AND x=1 {END}", new { Data = new DateTime(2024, 1, 1) }));
        // string não-data vs número continua false (não vira DateTime)
        Assert.DoesNotContain("AND", Render("{IF:Nome > 100} AND x=1 {END}", new { Nome = "2024-not-a-date" }));
        // null handling: null > X false, null >= null false só == true
        Assert.DoesNotContain("AND", Render("{IF:Nome > 100} AND x=1 {END}", new { Nome = (string?)null }));
        Assert.DoesNotContain("AND", Render("{IF:Nome >= 0} AND x=1 {END}", new { Nome = (string?)null }));
        // via dict para ambos null
        var dictBothNull = new Dictionary<string, object?> { ["A"] = null, ["B"] = null };
        Assert.DoesNotContain("AND", SqlTemplate.Parse("{IF:A >= B} AND x=1 {END}").Render(dictBothNull).Sql);
        Assert.DoesNotContain("AND", SqlTemplate.Parse("{IF:A > B} AND x=1 {END}").Render(dictBothNull).Sql);
        Assert.DoesNotContain("AND", SqlTemplate.Parse("{IF:A <= B} AND x=1 {END}").Render(dictBothNull).Sql);
        Assert.Contains("AND", SqlTemplate.Parse("{IF:A == B} AND x=1 {END}").Render(dictBothNull).Sql);
        Assert.DoesNotContain("AND", SqlTemplate.Parse("{IF:A != B} AND x=1 {END}").Render(dictBothNull).Sql);
        // BETWEEN mantém flat Min/Max intacto
        Assert.Contains("BETWEEN", Render("{BETWEEN:Price} AND Price BETWEEN @PriceMin AND @PriceMax {END}", new { PriceMin = 10, PriceMax = 20 }));
        Assert.DoesNotContain("BETWEEN", Render("{BETWEEN:Price} AND Price BETWEEN @PriceMin AND @PriceMax {END}", new { PriceMin = 10 }));
    }
}
