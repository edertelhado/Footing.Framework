using Footing.Framework.Data;

namespace Footing.Framework.Tests;

public class SqlTemplateBindTests
{
    private static string Render(string sql, object? p = null) => SqlTemplate.Parse(sql).Render(p).Sql;
    private static SqlResult RenderResult(string sql, object? p = null) => SqlTemplate.Parse(sql).Render(p);

    [Fact]
    public void Bind_LikePattern_Concat_Literal_Param()
    {
        var sql = "{BIND:LikePattern, value='%' + Name + '%'} SELECT * FROM T {WHERE} {IF:LikePattern != null} AND Name LIKE @LikePattern {END} {ENDWHERE}";
        var result = RenderResult(sql, new { Name = "eder" });
        Assert.Contains("Name LIKE @LikePattern", result.Sql);
        Assert.Contains("WHERE", result.Sql);
        var dict = (Dictionary<string, object?>)result.Parameters!;
        Assert.Equal("%eder%", dict["LikePattern"]?.ToString());
        var result2 = RenderResult(sql, new { Name = "" });
        var dict2 = (Dictionary<string, object?>)result2.Parameters!;
        Assert.Equal("%%", dict2["LikePattern"]?.ToString());
        var resultNull = RenderResult(sql, new { Name = (string?)null });
        Assert.Contains("Name LIKE @LikePattern", resultNull.Sql);
        var dictNull = (Dictionary<string, object?>)resultNull.Parameters!;
        Assert.Equal("%%", dictNull["LikePattern"]?.ToString());
    }

    [Fact]
    public void Bind_ValueInjection_Rejected_FailSafe()
    {
        var sql = "{BIND:Var, value='%' + Name + '; DROP' } SELECT * FROM T {WHERE} {IF:Var != null} AND x=@Var {END} {ENDWHERE}";
        var result = Render(sql, new { Name = "eder" });
        // value contains ';' => BIND should be rejected, Var not injected, IF:Var != null should be false (missing) => no AND
        Assert.DoesNotContain("AND x=@Var", result);
        // Ensure no throw
        var ex = Record.Exception(() => Render(sql, new { Name = "eder" }));
        Assert.Null(ex);
        var dict = (Dictionary<string, object?>)RenderResult(sql, new { Name = "eder" }).Parameters!;
        Assert.False(dict.ContainsKey("Var"));
    }

    [Fact]
    public void Bind_Valid_StoresInParamDict_UsedInWhere()
    {
        // Test that bind variable can be used in subsequent IF
        var sql = "{BIND:Pattern, value='%' + Search + '%'} SELECT * FROM T {WHERE} {IF:Pattern} AND col LIKE @Pattern {END} {ENDWHERE}";
        var r1 = Render(sql, new { Search = "abc" });
        Assert.Contains("col LIKE @Pattern", r1);
        var d1 = (Dictionary<string, object?>)RenderResult(sql, new { Search = "abc" }).Parameters!;
        Assert.Equal("%abc%", d1["Pattern"]?.ToString());
        var r2 = Render(sql, new { Search = "" });
        Assert.Contains("col LIKE @Pattern", r2);
        var d2 = (Dictionary<string, object?>)RenderResult(sql, new { Search = "" }).Parameters!;
        Assert.Equal("%%", d2["Pattern"]?.ToString());
        var r3 = Render(sql, new { Search = (string?)null });
        var d3 = (Dictionary<string, object?>)RenderResult(sql, new { Search = (string?)null }).Parameters!;
        Assert.Equal("%%", d3["Pattern"]?.ToString());
        var r4 = Render(sql, new { });
        var d4 = (Dictionary<string, object?>)RenderResult(sql, new { }).Parameters!;
        Assert.Equal("%%", d4["Pattern"]?.ToString());
    }
}
