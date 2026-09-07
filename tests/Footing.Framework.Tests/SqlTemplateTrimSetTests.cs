using Footing.Framework.Data;

namespace Footing.Framework.Tests;

public class SqlTemplateTrimSetTests
{
    private static string Render(string sql, object? p = null) => SqlTemplate.Parse(sql).Render(p).Sql;

    [Fact]
    public void Trim_PrefixWhere_PrefixOverrides_StripsAndOr()
    {
        var sql = "{TRIM prefix=\"WHERE\" prefixOverrides=\"AND|OR \"} AND x=1 OR y=2 {ENDTRIM}";
        var result = Render(sql);
        Assert.Equal("WHERE x=1 OR y=2", result.Trim());
    }

    [Fact]
    public void Trim_PrefixWhere_WithInnerIf_RenderedAndTrimmed()
    {
        var sql = "{TRIM prefix=\"WHERE\" prefixOverrides=\"AND|OR \"} {IF:Name != null} AND Name=@Name {END} {IF:Age != null} AND Age=@Age {END} {ENDTRIM}";
        var r1 = Render(sql, new { Name = "eder", Age = (int?)null });
        Assert.Contains("WHERE Name=@Name", r1);
        Assert.DoesNotContain("AND Age", r1);
        var r2 = Render(sql, new { Name = (string?)null, Age = (int?)null });
        Assert.Equal("", r2.Trim());
    }

    [Fact]
    public void Set_RemoveTrailingComma_WithIfNull_OmitsComma()
    {
        var sql = "UPDATE T {SET} {IF:Name != null} Name=@Name, {END} UpdatedAt=NOW(), {ENDSET} WHERE Id=@Id";
        var r1 = Render(sql, new { Name = (string?)null, Id = 1 });
        // Name null -> SET UpdatedAt=NOW() sem vírgula trailing? Actually SET ... suffixOverrides "," should remove trailing comma from "UpdatedAt=NOW(),"
        Assert.Contains("SET UpdatedAt=NOW()", r1);
        Assert.DoesNotContain("SET ,", r1);
        // ensure no trailing comma before WHERE
        Assert.DoesNotContain("NOW(), WHERE", r1);
        Assert.Contains("WHERE Id=@Id", r1);
    }

    [Fact]
    public void Set_WithValue_KeepsCommaCorrectly()
    {
        var sql = "UPDATE T {SET} {IF:Name != null} Name=@Name, {END} UpdatedAt=NOW(), {ENDSET} WHERE Id=@Id";
        var r1 = Render(sql, new { Name = "eder", Id = 1 });
        Assert.Contains("SET Name=@Name, UpdatedAt=NOW()", r1);
        // comma between Name and UpdatedAt is correct, trailing comma removed
        Assert.DoesNotContain("NOW(), WHERE", r1);
        // Verify no double commas
        Assert.DoesNotContain(",,", r1);
    }

    [Fact]
    public void Set_EmptyContent_ReturnsEmpty()
    {
        var sql = "UPDATE T {SET} {IF:Name != null} Name=@Name, {END} {ENDSET} WHERE Id=@Id";
        var r1 = Render(sql, new { Name = (string?)null, Id = 1 });
        // SET block empty -> should return "" (no SET)
        Assert.DoesNotContain("SET", r1);
        Assert.Contains("WHERE Id=@Id", r1);
    }
}
