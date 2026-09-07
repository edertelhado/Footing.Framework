using Footing.Framework.Data;

namespace Footing.Framework.Tests;

/// <summary>
/// V3 breaking clean Gherkin 9 cenários + 2 remoção.
/// Cobre IF expr unificado {IF: Param Op Operand} e WHEN expr.
/// </summary>
public class SqlTemplateV3Tests
{
    private static string Render(string sql, object? p) => SqlTemplate.Parse(sql).Render(p).Sql;

    // Cenário 1 — IF:Name == 'eder' literal string
    [Fact]
    public void V3_IF_eq_literal_eder_single_and_double_quotes()
    {
        var sql = "{IF:Name == 'eder'} AND Name=@Name {END}";
        Assert.Contains("AND Name=@Name", Render(sql, new { Name = "eder" }));
        Assert.DoesNotContain("AND Name", Render(sql, new { Name = "joao" }));
        Assert.DoesNotContain("AND Name", Render(sql, new { Name = (string?)null }));
        Assert.DoesNotContain("AND Name", Render(sql, new { }));
        Assert.DoesNotContain("AND Name", Render(sql, new { Name = "" }));
        // double quotes
        var sql2 = "{IF:Name == \"eder\"} AND Name=@Name {END}";
        Assert.Contains("AND Name=@Name", Render(sql2, new { Name = "eder" }));
        Assert.DoesNotContain("AND Name", Render(sql2, new { Name = "joao" }));
    }

    // Cenário 2 — IF:Name != 'eder'
    [Fact]
    public void V3_IF_ne_literal_eder()
    {
        var sql = "{IF:Name != 'eder'} AND Name=@Name {END}";
        Assert.Contains("AND Name=@Name", Render(sql, new { Name = "joao" }));
        Assert.DoesNotContain("AND Name", Render(sql, new { Name = "eder" }));
        // "" != 'eder' => true
        Assert.Contains("AND Name=@Name", Render(sql, new { Name = "" }));
        // also test alias <> → !=
        var sql2 = "{IF:Name <> 'eder'} AND Name=@Name {END}";
        Assert.Contains("AND Name=@Name", Render(sql2, new { Name = "joao" }));
        Assert.DoesNotContain("AND Name", Render(sql2, new { Name = "eder" }));
        // alias = → ==
        var sql3 = "{IF:Name = 'eder'} AND Name=@Name {END}";
        Assert.Contains("AND Name=@Name", Render(sql3, new { Name = "eder" }));
        Assert.DoesNotContain("AND Name", Render(sql3, new { Name = "joao" }));
    }

    // Cenário 3 — IF:Preco >100 / >= / < / <= + textual aliases
    [Fact]
    public void V3_IF_gt_lt_gte_lte_numeric()
    {
        var sqlGt = "{IF:Preco > 100} AND Preco > @Preco {END}";
        Assert.Contains("Preco > @Preco", Render(sqlGt, new { Preco = 150 }));
        Assert.DoesNotContain("Preco", Render(sqlGt, new { Preco = 100 }));
        Assert.DoesNotContain("Preco", Render(sqlGt, new { Preco = 50 }));
        Assert.DoesNotContain("Preco", Render(sqlGt, new { Preco = (int?)null }));

        var sqlGte = "{IF:Preco >= 100} AND Preco >= @Preco {END}";
        Assert.Contains("Preco", Render(sqlGte, new { Preco = 100 }));
        Assert.Contains("Preco", Render(sqlGte, new { Preco = 150 }));
        Assert.DoesNotContain("Preco", Render(sqlGte, new { Preco = 99 }));

        // textual alias gte/ge
        Assert.Contains("Preco", Render("{IF:Preco gte 100} AND Preco >= @Preco {END}", new { Preco = 100 }));
        Assert.Contains("Preco", Render("{IF:Preco ge 100} AND Preco >= @Preco {END}", new { Preco = 100 }));
        Assert.Contains("Preco", Render("{IF:Preco GT 100} AND Preco > @Preco {END}", new { Preco = 150 }));
        Assert.DoesNotContain("Preco", Render("{IF:Preco gt 100} AND Preco > @Preco {END}", new { Preco = 50 }));

        var sqlLt = "{IF:Idade < 18} AND Idade < @Idade {END}";
        Assert.Contains("Idade", Render(sqlLt, new { Idade = 10 }));
        Assert.DoesNotContain("Idade", Render(sqlLt, new { Idade = 18 }));
        Assert.Contains("Idade", Render("{IF:Idade lt 18} AND Idade < @Idade {END}", new { Idade = 10 }));
        Assert.DoesNotContain("Idade", Render("{IF:Idade lt 18} AND Idade < @Idade {END}", new { Idade = 18 }));

        var sqlLte = "{IF:Preco <= 50} AND Preco <= @Preco {END}";
        Assert.Contains("Preco", Render(sqlLte, new { Preco = 50 }));
        Assert.DoesNotContain("Preco", Render(sqlLte, new { Preco = 51 }));
        Assert.Contains("Preco", Render("{IF:Preco lte 50} AND Preco <= @Preco {END}", new { Preco = 50 }));
        Assert.Contains("Preco", Render("{IF:Preco le 50} AND Preco <= @Preco {END}", new { Preco = 50 }));
    }

    // Cenário 4 — IF:Name != null e == null
    [Fact]
    public void V3_IF_null_notnull()
    {
        var sqlNotNull = "{IF:Name != null} AND Name=@Name {END}";
        Assert.Contains("Name=@Name", Render(sqlNotNull, new { Name = "eder" }));
        Assert.Contains("Name=@Name", Render(sqlNotNull, new { Name = "" })); // "" != null → true
        Assert.DoesNotContain("Name=", Render(sqlNotNull, new { Name = (string?)null }));
        Assert.DoesNotContain("Name=", Render(sqlNotNull, new { }));
        // case-insensitive NULL
        Assert.Contains("Name=@Name", Render("{IF:Name != NULL} AND Name=@Name {END}", new { Name = "x" }));
        Assert.DoesNotContain("Name=", Render("{IF:Name != Null} AND Name=@Name {END}", new { Name = (string?)null }));

        var sqlIsNull = "{IF:Name == null} AND Name IS NULL {END}";
        Assert.Contains("IS NULL", Render(sqlIsNull, new { Name = (string?)null }));
        Assert.Contains("IS NULL", Render(sqlIsNull, new { }));
        Assert.DoesNotContain("IS NULL", Render(sqlIsNull, new { Name = "eder" }));
        Assert.DoesNotContain("IS NULL", Render(sqlIsNull, new { Name = "" }));
        Assert.Contains("IS NULL", Render("{IF:Name == NULL} AND Name IS NULL {END}", new { Name = (string?)null }));
    }

    // Cenário 5 — IF:Preco == @OutroParam / bare
    [Fact]
    public void V3_IF_param_vs_param_at_and_bare()
    {
        var sql = "{IF:Status == @Expected} AND Status=@Status {END}";
        Assert.Contains("Status=@Status", Render(sql, new { Status = "ACTIVE", Expected = "ACTIVE" }));
        Assert.DoesNotContain("Status=", Render(sql, new { Status = "ACTIVE", Expected = "INACTIVE" }));
        Assert.DoesNotContain("Status=", Render(sql, new { Status = "ACTIVE", Expected = (string?)null }));
        Assert.DoesNotContain("Status=", Render(sql, new { Status = "ACTIVE" })); // Expected ausente → null

        // bare without @
        var sqlBare = "{IF:Status == Expected} AND Status=@Status {END}";
        Assert.Contains("Status=@Status", Render(sqlBare, new { Status = "ACTIVE", Expected = "ACTIVE" }));
        Assert.DoesNotContain("Status=", Render(sqlBare, new { Status = "ACTIVE", Expected = "INACTIVE" }));
        Assert.DoesNotContain("Status=", Render(sqlBare, new { Status = "ACTIVE" }));
    }

    // Cenário 6 — Shorthand {IF:Status} existência
    [Fact]
    public void V3_IF_shorthand_existencia()
    {
        var sql = "{IF:Status} AND Status=@Status {END}";
        Assert.Contains("Status=@Status", Render(sql, new { Status = "ACTIVE" }));
        Assert.DoesNotContain("Status=", Render(sql, new { Status = "" }));
        Assert.DoesNotContain("Status=", Render(sql, new { Status = (string?)null }));
        Assert.DoesNotContain("Status=", Render(sql, new { }));
        // 0 e false são valores válidos → renderiza
        Assert.Contains("Age", Render("{IF:Age} AND Age=@Age {END}", new { Age = 0 }));
        Assert.Contains("Flag", Render("{IF:Flag} AND Flag=@Flag {END}", new { Flag = false }));
    }

    // Cenário 7 — WHERE + expr
    [Fact]
    public void V3_WHERE_mixed_expr()
    {
        var sql = "{WHERE} {IF:Status == 'ACTIVE'} AND Status=@Status {END} {IF:Age >= 18} AND Age >= @Age {END} {ENDWHERE}";
        var r1 = Render(sql, new { Status = "ACTIVE", Age = 20 });
        Assert.Contains("WHERE", r1);
        Assert.Contains("Status=@Status", r1);
        Assert.Contains("Age >= @Age", r1);
        Assert.DoesNotContain("WHERE AND", r1);
        var r2 = Render(sql, new { Status = "INACTIVE", Age = 20 });
        Assert.Contains("WHERE", r2);
        Assert.Contains("Age >= @Age", r2);
        Assert.DoesNotContain("Status=@Status", r2);
        Assert.DoesNotContain("WHERE AND", r2);
        Assert.DoesNotContain("WHERE", Render(sql, new { Status = "INACTIVE", Age = 10 }));
        // WHERE some se vazio → ""
        Assert.Equal("", Render(sql, new { Status = (string?)null, Age = (int?)null }).Trim());
    }

    // Cenário 8 — CHOOSE/WHEN com expr
    [Fact]
    public void V3_CHOOSE_WHEN_expr()
    {
        var sql = "{CHOOSE} {WHEN:Status == 'ACTIVE'} AND Status=@Status {ENDWHEN} {WHEN:Status == 'INACTIVE'} AND Status=@Status {ENDWHEN} {OTHERWISE} AND Status IS NULL {ENDOTHERWISE} {ENDCHOOSE}";
        Assert.Contains("Status=@Status", Render(sql, new { Status = "ACTIVE" }));
        Assert.DoesNotContain("IS NULL", Render(sql, new { Status = "ACTIVE" }));
        Assert.Contains("Status=@Status", Render(sql, new { Status = "INACTIVE" }));
        Assert.Contains("IS NULL", Render(sql, new { Status = "OTHER" }));
        Assert.Contains("IS NULL", Render(sql, new { Status = (string?)null }));
        // WHEN shorthand existência
        var sql2 = "{CHOOSE} {WHEN:Status} AND Status=@Status {ENDWHEN} {OTHERWISE} AND Status IS NULL {ENDOTHERWISE} {ENDCHOOSE}";
        Assert.Contains("Status=@Status", Render(sql2, new { Status = "x" }));
        Assert.Contains("IS NULL", Render(sql2, new { Status = "" }));
        Assert.Contains("IS NULL", Render(sql2, new { Status = (string?)null }));
    }

    // Cenário 9 — numérico vs tipos
    [Fact]
    public void V3_numeric_types_int_decimal_double_string()
    {
        // int vs decimal literal
        Assert.Contains("AND", Render("{IF:Preco > 100} AND Preco > @Preco {END}", new { Preco = 150 }));
        Assert.Contains("AND", Render("{IF:Preco > 100} AND Preco > @Preco {END}", new { Preco = 150.5m }));
        Assert.Contains("AND", Render("{IF:Preco > 100} AND Preco > @Preco {END}", new { Preco = 150.5d }));
        // string numeric
        Assert.Contains("AND", Render("{IF:Preco > 100} AND Preco > @Preco {END}", new { Preco = "150" }));
        Assert.DoesNotContain("AND", Render("{IF:Preco > 100} AND Preco > @Preco {END}", new { Preco = "50" }));
        // param null/ausente → omitido
        Assert.DoesNotContain("AND", Render("{IF:Preco > 100} AND Preco > @Preco {END}", new { Preco = (int?)null }));
        Assert.DoesNotContain("AND", Render("{IF:Preco > 100} AND Preco > @Preco {END}", new { }));
        // 0 literal
        Assert.Contains("AND", Render("{IF:Preco == 0} AND Preco=@Preco {END}", new { Preco = 0 }));
        Assert.DoesNotContain("AND", Render("{IF:Preco == 0} AND Preco=@Preco {END}", new { Preco = 1 }));
        // bool
        Assert.Contains("AND", Render("{IF:Flag == true} AND Flag=@Flag {END}", new { Flag = true }));
        Assert.DoesNotContain("AND", Render("{IF:Flag == true} AND Flag=@Flag {END}", new { Flag = false }));
        Assert.Contains("AND", Render("{IF:Flag != false} AND Flag=@Flag {END}", new { Flag = true }));
    }

    // Cenário extra — boolean false literal + case-insensitive operator
    [Fact]
    public void V3_IF_bool_and_operator_case_insensitive()
    {
        Assert.Contains("AND", Render("{IF:Flag == True} AND Flag=@Flag {END}", new { Flag = true }));
        Assert.Contains("AND", Render("{IF:Flag == TRUE} AND Flag=@Flag {END}", new { Flag = true }));
        Assert.DoesNotContain("AND", Render("{IF:Flag == true} AND Flag=@Flag {END}", new { Flag = false }));
        // > vs GT case-insensitive
        Assert.Contains("AND", Render("{IF:Age GT 18} AND Age=@Age {END}", new { Age = 20 }));
        Assert.DoesNotContain("AND", Render("{IF:Age Gt 18} AND Age=@Age {END}", new { Age = 10 }));
    }

    // Cenário — todos juntos sem WHERE (pipeline top-level)
    [Fact]
    public void V3_todos_juntos_sem_WHERE()
    {
        var sql = "{IF:Status == 'ACTIVE'} AND Status=@Status {END} {IF:Age >= 18} AND Age>=@Age {END} {IN:Roles} AND Role IN @Roles {END} {BETWEEN:Price} AND Price BETWEEN @PriceMin AND @PriceMax {END}";
        var result = Render(sql, new { Status = "ACTIVE", Age = 20, Roles = new[] { 1, 2 }, PriceMin = 10, PriceMax = 50 });
        Assert.Contains("Status=@Status", result);
        Assert.Contains("Age>=@Age", result);
        Assert.Contains("Role IN @Roles", result);
        Assert.Contains("Price BETWEEN", result);

        var empty = Render(sql, new { Status = "INACTIVE", Age = 10, Roles = Array.Empty<int>(), PriceMin = (int?)null, PriceMax = (int?)null });
        Assert.DoesNotContain("Status=@Status", empty);
        Assert.DoesNotContain("Age>=", empty);
        Assert.DoesNotContain("Role IN", empty);
        Assert.DoesNotContain("Price BETWEEN", empty);
    }

    // Remoção — EQ não interpretado (permanece literal)
    [Fact]
    public void V3_REMOVED_Eq_tag_not_interpreted()
    {
        var sql = "SELECT * FROM t {EQ:Status} AND Status=@Status {END}";
        var result = Render(sql, new { Status = "ACTIVE" });
        // regex removido → template permanece literal
        Assert.Contains("{EQ:Status}", result);
        Assert.Contains("{END}", result);
        Assert.Contains("AND Status=@Status", result); // conteúdo permanece literal (não interpretado)
        Assert.Contains("{EQ:", result);
    }

    [Fact]
    public void V3_REMOVED_IfNotNull_tag_not_interpreted()
    {
        var sql = "SELECT * FROM t {IFNOTNULL:Name} AND Name=@Name {END}";
        var result = Render(sql, new { Name = "eder" });
        Assert.Contains("{IFNOTNULL:Name}", result);
        Assert.Contains("{END}", result); // remains literal
    }

    [Fact]
    public void V3_REMOVED_IfNotEmpty_tag_not_interpreted()
    {
        var sql = "SELECT * FROM t {IFNOTEMPTY:Name} AND Name=@Name {END}";
        var result = Render(sql, new { Name = "eder" });
        Assert.Contains("{IFNOTEMPTY:Name}", result);
    }

    [Fact]
    public void V3_REMOVED_Ne_Lt_Gt_Gte_Ge_Lte_Le_not_interpreted()
    {
        foreach (var tag in new[] { "{NE:Status}", "{LT:Age}", "{GT:Age}", "{GTE:Age}", "{GE:Age}", "{LTE:Age}", "{LE:Age}" })
        {
            var sql = $"SELECT * FROM t {tag} AND x=1 {{END}}";
            var result = Render(sql, new { Status = "x", Age = 1 });
            Assert.Contains(tag, result);
        }
    }
}
