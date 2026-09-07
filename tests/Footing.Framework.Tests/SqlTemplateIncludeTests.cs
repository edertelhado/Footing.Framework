using Footing.Framework.Data;

namespace Footing.Framework.Tests;

public class SqlTemplateIncludeTests
{
    private static string Render(string sql, object? p = null) => SqlTemplate.Parse(sql).Render(p).Sql;

    [Fact]
    public void Include_Expande_Fragmento_Registrado()
    {
        SqlTemplate.ClearFragments();
        SqlTemplate.RegisterFragment("BaseColumns", "u.Id, u.Name");
        var sql = "SELECT {INCLUDE:BaseColumns} FROM Users";
        var result = Render(sql);
        Assert.Contains("u.Id, u.Name", result);
        Assert.DoesNotContain("{INCLUDE:BaseColumns}", result);
        Assert.Contains("SELECT u.Id, u.Name FROM Users", result);
    }

    [Fact]
    public void Include_Missing_FailSafe_Literal_SemThrow()
    {
        SqlTemplate.ClearFragments();
        var sql = "SELECT {INCLUDE:MissingFragment} FROM T";
        var ex = Record.Exception(() => Render(sql));
        Assert.Null(ex);
        var result = Render(sql);
        Assert.Contains("{INCLUDE:MissingFragment}", result);
    }

    [Fact]
    public void Include_Recursivo_DepthGuard_NaoStackOverflow()
    {
        SqlTemplate.ClearFragments();
        SqlTemplate.RegisterFragment("A", "cols A {INCLUDE:B}");
        SqlTemplate.RegisterFragment("B", "cols B {INCLUDE:A}");
        var sql = "SELECT {INCLUDE:A} FROM T";
        var ex = Record.Exception(() => Render(sql));
        Assert.Null(ex);
        var result = Render(sql);
        // deve conter literal de loop ou profundidade guard sem throw
        Assert.NotNull(result);
        SqlTemplate.ClearFragments();
    }
}
