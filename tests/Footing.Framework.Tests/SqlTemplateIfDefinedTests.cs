using System.Collections.Generic;
using Footing.Framework.Data;

namespace Footing.Framework.Tests;

/// <summary>
/// US-TEMPLATE-IFDEFINED-001 Must 1 SP — 7 testes Gherkin ContainsKey vs missing vs null
/// </summary>
public class SqlTemplateIfDefinedTests
{
    private static string Render(string sql, object? p) => SqlTemplate.Parse(sql).Render(p).Sql;

    [Fact]
    public void IfDefined_MissingFalse()
    {
        var sql = "{IFDEFINED:Ativo} AND c.Ativo IS NULL {END}";
        var result = Render(sql, new { });
        Assert.Equal("", result.Trim());
        Assert.DoesNotContain("IS NULL", result);
    }

    [Fact]
    public void IfDefined_NullTrue()
    {
        var sql = "{IFDEFINED:Ativo} AND c.Ativo IS NULL {END}";
        var result = Render(sql, new { Ativo = (string?)null });
        Assert.Contains("AND c.Ativo IS NULL", result);
    }

    [Fact]
    public void IfDefined_EmptyTrue_DistinctFrom_If()
    {
        var sql = "{IFDEFINED:Ativo} AND x {END}";
        var resultDefined = Render(sql, new { Ativo = "" });
        Assert.Contains("AND x", resultDefined);
        // {IF:Ativo} would be false for empty
        var sqlIf = "{IF:Ativo} AND x {END}";
        var resultIf = Render(sqlIf, new { Ativo = "" });
        Assert.DoesNotContain("AND x", resultIf);
    }

    [Fact]
    public void IfDefined_YTrue()
    {
        var sql = "{IFDEFINED:Ativo} AND c.Ativo IS NULL {END}";
        var result = Render(sql, new { Ativo = "Y" });
        Assert.Contains("AND c.Ativo IS NULL", result);
    }

    [Fact]
    public void IfDefined_DictMissingFalse()
    {
        var sql = "{IFDEFINED:Ativo} AND c.Ativo IS NULL {END}";
        var dict = new Dictionary<string, object?> { };
        var result = Render(sql, dict);
        Assert.Equal("", result.Trim());
    }

    [Fact]
    public void IfDefined_DictNullTrue()
    {
        var sql = "{IFDEFINED:Ativo} AND c.Ativo IS NULL {END}";
        var dict = new Dictionary<string, object?> { ["Ativo"] = null };
        var result = Render(sql, dict);
        Assert.Contains("AND c.Ativo IS NULL", result);
    }

    [Fact]
    public void IfDefined_AndWithDefined()
    {
        var sql = "{IF:defined(Ativo) and Ativo == 'Y'} AND x {END}";
        Assert.Contains("AND x", Render(sql, new { Ativo = "Y" }));
        Assert.DoesNotContain("AND x", Render(sql, new { Ativo = "N" }));
        Assert.DoesNotContain("AND x", Render(sql, new { }));
        Assert.DoesNotContain("AND x", Render(sql, new { Ativo = (string?)null }));
    }

    [Fact]
    public void IfNotDefined_MissingTrue_NullFalse()
    {
        var sql = "{IFNOTDEFINED:Ativo} AND y {END}";
        Assert.Contains("AND y", Render(sql, new { }));
        Assert.DoesNotContain("AND y", Render(sql, new { Ativo = (string?)null }));
        Assert.DoesNotContain("AND y", Render(sql, new { Ativo = "Y" }));
        Assert.DoesNotContain("AND y", Render(sql, new { Ativo = "" }));
    }

    [Fact]
    public void IfDefined_Choose_WhenDefined()
    {
        var sql = "{CHOOSE} {WHEN:defined(Ativo)} AND x {ENDWHEN} {OTHERWISE} AND y {ENDOTHERWISE} {ENDCHOOSE}";
        Assert.Contains("AND x", Render(sql, new { Ativo = "a" }));
        Assert.Contains("AND y", Render(sql, new { }));
        Assert.Contains("AND x", Render(sql, new { Ativo = (string?)null }));
        Assert.Contains("AND x", Render(sql, new { Ativo = "" }));
    }

    [Fact]
    public void IfDefined_WhereIntegration()
    {
        var sql = "SELECT * FROM t {WHERE} {IFDEFINED:Ativo} AND c.Ativo IS NULL {END} {IF:Ativo} AND c.Ativo=@Ativo {END} {ENDWHERE}";
        // missing -> empty WHERE
        Assert.DoesNotContain("WHERE", Render(sql, new { }));
        // null explicit -> IS NULL via IFDEFINED
        Assert.Contains("WHERE", Render(sql, new { Ativo = (string?)null }));
        Assert.Contains("IS NULL", Render(sql, new { Ativo = (string?)null }));
        // Y -> via IF:Ativo
        Assert.Contains("WHERE", Render(sql, new { Ativo = "Y" }));
        Assert.Contains("c.Ativo=@Ativo", Render(sql, new { Ativo = "Y" }));
    }
}
