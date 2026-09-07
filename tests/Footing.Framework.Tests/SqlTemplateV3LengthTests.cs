using Footing.Framework.Data;

namespace Footing.Framework.Tests;

public class SqlTemplateV3LengthTests
{
    [Fact]
    public void Length_Name_gt_18_true_when_30_chars()
    {
        var tpl = SqlTemplate.Parse("SELECT * FROM Users {WHERE} {IF:length(Name) > 18} AND Name=@Name {END} {ENDWHERE}");
        var result = tpl.Render(new { Name = "eder almeida telhado mais dezoito" }); // >18 => 33
        Assert.Contains("Name=@Name", result.Sql);
        Assert.Contains("WHERE", result.Sql);
        // also direct true case length 20 >18
        var tpl2 = SqlTemplate.Parse("{IF:length(Name) > 18} AND x=1 {END}");
        var r2 = tpl2.Render(new { Name = new string('a', 20) });
        Assert.Contains("AND x=1", r2.Sql);
    }

    [Fact]
    public void Length_Name_gt_18_false_when_short()
    {
        var tpl = SqlTemplate.Parse("SELECT * FROM Users {WHERE} {IF:length(Name) > 18} AND Name=@Name {END} {ENDWHERE}");
        var result = tpl.Render(new { Name = "eder" });
        Assert.DoesNotContain("AND Name=@Name", result.Sql);

        var tpl2 = SqlTemplate.Parse("{IF:length(Name) > 18} AND x=1 {END}");
        var r2 = tpl2.Render(new { Name = "abc" });
        Assert.DoesNotContain("AND x=1", r2.Sql);
    }

    [Fact]
    public void Length_empty_and_null_handling()
    {
        var tplEq0 = SqlTemplate.Parse("{IF:length(Name) == 0} AND x=1 {END}");
        Assert.Contains("AND x=1", tplEq0.Render(new { Name = "" }).Sql);
        Assert.DoesNotContain("AND x=1", tplEq0.Render(new { Name = "a" }).Sql);

        var tplGt0 = SqlTemplate.Parse("{IF:length(Name) > 0} AND x=1 {END}");
        Assert.DoesNotContain("AND x=1", tplGt0.Render(new { Name = "" }).Sql);
        Assert.Contains("AND x=1", tplGt0.Render(new { Name = "a" }).Sql);
        Assert.DoesNotContain("AND x=1", tplGt0.Render(new { Name = (string?)null }).Sql);
        Assert.DoesNotContain("AND x=1", tplGt0.Render(new { }).Sql); // missing -> 0

        // length(null) ==0 true
        Assert.Contains("AND x=1", tplEq0.Render(new { Name = (string?)null }).Sql);
    }

    [Fact]
    public void Length_numeric_and_vs_param()
    {
        // length(123) == 3 -> "123".Length =3
        var tplNum = SqlTemplate.Parse("{IF:length(Code) == 3} AND x=1 {END}");
        Assert.Contains("AND x=1", tplNum.Render(new { Code = 123 }).Sql);
        Assert.DoesNotContain("AND x=1", tplNum.Render(new { Code = 12 }).Sql);
        Assert.DoesNotContain("AND x=1", tplNum.Render(new { Code = 12345 }).Sql); // 5 !=3

        var tplNum3 = SqlTemplate.Parse("{IF:length(Code) == 5} AND x=1 {END}");
        Assert.Contains("AND x=1", tplNum3.Render(new { Code = 12345 }).Sql);

        // length vs @Param
        var tplParam = SqlTemplate.Parse("{IF:length(Name) > @Min} AND x=1 {END}");
        Assert.Contains("AND x=1", tplParam.Render(new { Name = "eder", Min = 2 }).Sql); // 4>2 true
        Assert.DoesNotContain("AND x=1", tplParam.Render(new { Name = "eder", Min = 10 }).Sql); // 4>10 false
        Assert.Contains("AND x=1", tplParam.Render(new { Name = new string('a', 20), Min = 18 }).Sql);
    }

    [Fact]
    public void Length_vs_bare_Name_gt2_proves_explicit_difference()
    {
        // C maintained: Name >2 false (string vs number incompatible) -> false+Warning
        var tplBare = SqlTemplate.Parse("{IF:Name > 2} AND x=1 {END}");
        Assert.DoesNotContain("AND x=1", tplBare.Render(new { Name = "abc" }).Sql);

        // explicit length true: 3>2 true
        var tplLen = SqlTemplate.Parse("{IF:length(Name) > 2} AND x=1 {END}");
        Assert.Contains("AND x=1", tplLen.Render(new { Name = "abc" }).Sql);

        // length with gte/lte and different ops
        var tplGte = SqlTemplate.Parse("{IF:length(Name) >= 3} AND x=1 {END}");
        Assert.Contains("AND x=1", tplGte.Render(new { Name = "abc" }).Sql);
        Assert.DoesNotContain("AND x=1", tplGte.Render(new { Name = "ab" }).Sql);

        var tplLt = SqlTemplate.Parse("{IF:length(Name) < 5} AND x=1 {END}");
        Assert.Contains("AND x=1", tplLt.Render(new { Name = "abc" }).Sql);
        Assert.DoesNotContain("AND x=1", tplLt.Render(new { Name = "abcdef" }).Sql);
    }

    [Fact]
    public void Length_in_choose_when()
    {
        var tpl = SqlTemplate.Parse("{CHOOSE} {WHEN:length(Status) > 5} AND Status=@Status {ENDWHEN} {OTHERWISE} AND Status='A' {ENDOTHERWISE} {ENDCHOOSE}");
        var rTrue = tpl.Render(new { Status = "ACTIVE" }); // 6>5
        Assert.Contains("AND Status=@Status", rTrue.Sql);
        Assert.DoesNotContain("AND Status='A'", rTrue.Sql);

        var rFalse = tpl.Render(new { Status = "ab" }); //2>5 false
        Assert.Contains("AND Status='A'", rFalse.Sql);
        Assert.DoesNotContain("AND Status=@Status", rFalse.Sql);
    }

    [Fact]
    public void Length_case_insensitive_and_spaces()
    {
        var tpl = SqlTemplate.Parse("{IF:LENGTH(Name) > 2} AND x=1 {END}");
        Assert.Contains("AND x=1", tpl.Render(new { Name = "abc" }).Sql);

        var tpl2 = SqlTemplate.Parse("{IF:length ( Name ) > 2} AND x=1 {END}");
        Assert.Contains("AND x=1", tpl2.Render(new { Name = "abc" }).Sql);

        var tpl3 = SqlTemplate.Parse("{IF:length(Name) gte 3} AND x=1 {END}");
        Assert.Contains("AND x=1", tpl3.Render(new { Name = "abc" }).Sql);
    }
}
