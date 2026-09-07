using System.Collections;
using System.Dynamic;
using Footing.Framework.Data;

namespace Footing.Framework.Tests;

/// <summary>
/// Migrated from v1.1.0-alpha EQ/NE/etc (existência) to v3 IF expr.
/// Cenários preservados, agora com comparação real via {IF: Param Op Operand}.
/// </summary>
public class SqlTemplateOpsTests
{
    private static string Render(string sql, object? p) => SqlTemplate.Parse(sql).Render(p).Sql;

    // Cenário 1: IF existência renderiza quando presente, omitido quando null/empty
    [Fact]
    public void EQ_renderiza_vs_null_empty_ausente()
    {
        var sql = "SELECT * FROM t {WHERE} {IF:Name} AND Name=@Name {END} {ENDWHERE}";
        Assert.Contains("WHERE Name=@Name", Render(sql, new { Name = "john" }));
        Assert.DoesNotContain("WHERE", Render(sql, new { Name = (string?)null }));
        Assert.DoesNotContain("WHERE", Render(sql, new { }));
        Assert.DoesNotContain("WHERE", Render(sql, new { Name = "" }));
        // null vs "" herdando IF: IF:Name com "" → omitido, IF:Name != null com "" → renderiza
        Assert.DoesNotContain("Name=@Name", Render("{IF:Name} AND Name=@Name {END}", new { Name = "" }));
        Assert.Contains("Name=@Name", Render("{IF:Name != null} AND Name=@Name {END}", new { Name = "" }));
    }

    [Fact]
    public void EQ_zero_false_renderiza()
    {
        // RN: 0, false, MinValue renderizam via shorthand (existência)
        Assert.Contains("WHERE", Render("SELECT * FROM t {WHERE} {IF:Age} AND Age=@Age {END} {ENDWHERE}", new { Age = 0 }));
        Assert.Contains("WHERE", Render("SELECT * FROM t {WHERE} {IF:Flag} AND Flag=@Flag {END} {ENDWHERE}", new { Flag = false }));
        Assert.Contains("WHERE", Render("SELECT * FROM t {WHERE} {IF:Dt} AND Dt=@Dt {END} {ENDWHERE}", new { Dt = DateTime.MinValue }));
        // também via == 0
        Assert.Contains("WHERE", Render("SELECT * FROM t {WHERE} {IF:Age == 0} AND Age=@Age {END} {ENDWHERE}", new { Age = 0 }));
        Assert.Contains("WHERE", Render("SELECT * FROM t {WHERE} {IF:Flag == false} AND Flag=@Flag {END} {ENDWHERE}", new { Flag = false }));
    }

    // Cenário 2: NE/LT/GT/GTE/LTE com comparação real
    [Fact]
    public void NE_renderiza_vs_null()
    {
        var sql = "SELECT * FROM t {WHERE} {IF:Status != 'CANCELLED'} AND Status <> @Status {END} {ENDWHERE}";
        Assert.Contains("Status <>", Render(sql, new { Status = "ACTIVE" }));
        Assert.DoesNotContain("WHERE", Render(sql, new { Status = "CANCELLED" }));
        // null → left null != 'CANCELLED' → true? Actually null != 'CANCELLED' should be true? But our null handling: left null != literal → true (since not null). However template should omit when Status missing? For != 'CANCELLED', null param would render (since null != 'CANCELLED'). To test existence style, use != null check
        // For this test, we want != 'CANCELLED' with null should render (since null != 'CANCELLED')
        Assert.Contains("WHERE", Render(sql, new { Status = (string?)null })); // null != 'CANCELLED' → true
        // Use shorthand existence for null check
        Assert.DoesNotContain("WHERE", Render("SELECT * FROM t {WHERE} {IF:Status} AND Status <> @Status {END} {ENDWHERE}", new { Status = (string?)null }));
        Assert.DoesNotContain("WHERE", Render("SELECT * FROM t {WHERE} {IF:Status} AND Status <> @Status {END} {ENDWHERE}", new { Status = "" }));
    }

    [Fact]
    public void LT_GT_renderizam()
    {
        Assert.Contains("Age < @Age", Render("SELECT * FROM t {WHERE} {IF:Age < 18} AND Age < @Age {END} {ENDWHERE}", new { Age = 10 }));
        Assert.DoesNotContain("WHERE", Render("SELECT * FROM t {WHERE} {IF:Age < 18} AND Age < @Age {END} {ENDWHERE}", new { Age = 20 }));
        Assert.DoesNotContain("WHERE", Render("SELECT * FROM t {WHERE} {IF:Age < 18} AND Age < @Age {END} {ENDWHERE}", new { Age = (int?)null }));
        Assert.Contains("Salary > @Salary", Render("SELECT * FROM t {WHERE} {IF:Salary > 100} AND Salary > @Salary {END} {ENDWHERE}", new { Salary = 5000 }));
        Assert.DoesNotContain("WHERE", Render("SELECT * FROM t {WHERE} {IF:Salary > 100} AND Salary > @Salary {END} {ENDWHERE}", new { Salary = 50 }));
        Assert.DoesNotContain("WHERE", Render("SELECT * FROM t {WHERE} {IF:Salary > 100} AND Salary > @Salary {END} {ENDWHERE}", new { }));
    }

    [Fact]
    public void GTE_LTE_e_aliases_GE_LE()
    {
        var sqlGte = "SELECT * FROM t {WHERE} {IF:Age >= 18} AND Age >= @Age {END} {ENDWHERE}";
        Assert.Contains("Age >=", Render(sqlGte, new { Age = 18 }));
        Assert.Contains("Age >=", Render(sqlGte, new { Age = 20 }));
        Assert.DoesNotContain("WHERE", Render(sqlGte, new { Age = 10 }));
        Assert.DoesNotContain("WHERE", Render(sqlGte, new { Age = (int?)null }));
        // alias GE / GTE textual
        var sqlGe = "SELECT * FROM t {WHERE} {IF:Age ge 18} AND Age >= @Age {END} {ENDWHERE}";
        Assert.Contains("Age >=", Render(sqlGe, new { Age = 18 }));
        Assert.DoesNotContain("WHERE", Render(sqlGe, new { Age = 10 }));
        var sqlGte2 = "SELECT * FROM t {WHERE} {IF:Age gte 18} AND Age >= @Age {END} {ENDWHERE}";
        Assert.Contains("Age >=", Render(sqlGte2, new { Age = 18 }));

        var sqlLte = "SELECT * FROM t {WHERE} {IF:Price <= 100} AND Price <= @Price {END} {ENDWHERE}";
        Assert.Contains("Price <=", Render(sqlLte, new { Price = 100.0m }));
        Assert.Contains("Price <=", Render(sqlLte, new { Price = 50 }));
        Assert.DoesNotContain("WHERE", Render(sqlLte, new { Price = 200 }));
        Assert.DoesNotContain("WHERE", Render(sqlLte, new { Price = (decimal?)null }));
        var sqlLe = "SELECT * FROM t {WHERE} {IF:Price le 100} AND Price <= @Price {END} {ENDWHERE}";
        Assert.Contains("Price <=", Render(sqlLe, new { Price = 100.0m }));
        Assert.DoesNotContain("WHERE", Render(sqlLe, new { }));
        var sqlLteAlias = "SELECT * FROM t {WHERE} {IF:Price lte 100} AND Price <= @Price {END} {ENDWHERE}";
        Assert.Contains("Price <=", Render(sqlLteAlias, new { Price = 100.0m }));
    }

    // Cenário 3: BETWEEN com Min/Max flat
    [Fact]
    public void BETWEEN_min_max_ok_vs_falta_um()
    {
        var sql = "SELECT * FROM t {WHERE} {BETWEEN:CreatedAt} AND CreatedAt BETWEEN @CreatedAtMin AND @CreatedAtMax {END} {ENDWHERE}";
        var d1 = new DateTime(2024, 01, 01);
        var d2 = new DateTime(2024, 12, 31);
        Assert.Contains("BETWEEN", Render(sql, new { CreatedAtMin = d1, CreatedAtMax = d2 }));
        Assert.Contains("WHERE CreatedAt", Render(sql, new { CreatedAtMin = d1, CreatedAtMax = d2 }));
        // falta um → omitido
        Assert.DoesNotContain("BETWEEN", Render(sql, new { CreatedAtMin = d1, CreatedAtMax = (DateTime?)null }));
        Assert.DoesNotContain("WHERE", Render(sql, new { CreatedAtMin = d1 }));
        Assert.DoesNotContain("BETWEEN", Render(sql, new { CreatedAtMin = (DateTime?)null, CreatedAtMax = d2 }));
        Assert.DoesNotContain("WHERE", Render(sql, new { }));
        // string empty também omitido
        Assert.DoesNotContain("BETWEEN", Render("SELECT * FROM t {WHERE} {BETWEEN:Price} AND Price BETWEEN @PriceMin AND @PriceMax {END} {ENDWHERE}", new { PriceMin = "", PriceMax = "50" }));
    }

    [Fact]
    public void BETWEEN_com_outro_filtro_WHERE_normaliza()
    {
        var sql = "SELECT * FROM t {WHERE} {IF:Status} AND Status = @Status {END} {BETWEEN:CreatedAt} AND CreatedAt BETWEEN @CreatedAtMin AND @CreatedAtMax {END} {ENDWHERE}";
        var d1 = new DateTime(2024, 01, 01);
        var d2 = new DateTime(2024, 12, 31);
        var result = Render(sql, new { Status = "ACTIVE", CreatedAtMin = d1, CreatedAtMax = d2 });
        Assert.Contains("WHERE", result);
        Assert.Contains("Status = @Status", result);
        Assert.Contains("CreatedAt BETWEEN", result);
        Assert.DoesNotContain("WHERE AND", result);
    }

    // Cenário 4: Interação WHERE com múltiplos operadores mistos
    [Fact]
    public void WHERE_multiplos_operadores_mistos()
    {
        var sql = "SELECT * FROM t {WHERE} {IF:Status} AND Status = @Status {END} {IF:Age >= 18} AND Age >= @Age {END} {BETWEEN:Price} AND Price BETWEEN @PriceMin AND @PriceMax {END} {ENDWHERE}";
        // Status e Price presentes, Age 10 → GTE 18 omitido
        var result = Render(sql, new { Status = "A", Age = 10, PriceMin = 10, PriceMax = 50 });
        Assert.Contains("Status = @Status", result);
        Assert.Contains("Price BETWEEN", result);
        Assert.DoesNotContain("Age >=", result);
        Assert.DoesNotContain("WHERE AND", result);
        Assert.Contains("WHERE", result);

        // Age 20 → todos
        var result2 = Render(sql, new { Status = "A", Age = 20, PriceMin = 10, PriceMax = 50 });
        Assert.Contains("Age >=", result2);

        // todos null → WHERE some
        Assert.DoesNotContain("WHERE", Render(sql, new { Status = (string?)null, Age = (int?)null, PriceMin = (int?)null, PriceMax = (int?)null }));
    }

    // Cenário 5: Case-insensitive e IDictionary/Expando compatível
    [Fact]
    public void OPS_case_insensitive_e_dictionary()
    {
        var sql = "SELECT * FROM t {WHERE} {IF:Name} AND Name=@Name {END} {ENDWHERE}";
        Assert.Contains("WHERE", Render(sql, new Dictionary<string, object?> { ["name"] = "john" }));
        Assert.Contains("WHERE", Render(sql, new Dictionary<string, object?> { ["NAME"] = "john" }));
        Assert.Contains("WHERE", Render(sql, new Dictionary<string, object> { ["Name"] = "john" }));
        // Hashtable
        var ht = new Hashtable { ["name"] = "john" };
        Assert.Contains("WHERE", Render(sql, ht));
        // Expando
        dynamic expando = new ExpandoObject();
        ((IDictionary<string, object>)expando)["Name"] = "john";
        Assert.Contains("WHERE", Render(sql, (object)expando));
        // GTE case-insensitive via IF expr
        Assert.Contains("Age >=", Render("SELECT * FROM t {WHERE} {IF:Age >= 18} AND Age >= @Age {END} {ENDWHERE}", new Dictionary<string, object?> { ["age"] = 18 }));
        Assert.Contains("Age >=", Render("SELECT * FROM t {WHERE} {IF:Age gte 18} AND Age >= @Age {END} {ENDWHERE}", new Dictionary<string, object?> { ["AGE"] = 18 }));
        // BETWEEN case-insensitive Min/Max
        var d1 = new DateTime(2024, 01, 01);
        var d2 = new DateTime(2024, 12, 31);
        Assert.Contains("BETWEEN", Render("SELECT * FROM t {WHERE} {BETWEEN:CreatedAt} AND CreatedAt BETWEEN @CreatedAtMin AND @CreatedAtMax {END} {ENDWHERE}", new Dictionary<string, object?> { ["createdAtMin"] = d1, ["CREATEDATMAX"] = d2 }));
    }

    // Cenário 6: Compatibilidade 1.0.0 — templates sem novas tags idênticos
    [Fact]
    public void Compatibilidade_1_0_0_templates_existentes()
    {
        var sqlIf = "SELECT * FROM Users {WHERE} {IF:Name} AND Name=@Name {END} {ENDWHERE}";
        Assert.Contains("WHERE Name=@Name", Render(sqlIf, new { Name = "john" }));
        Assert.DoesNotContain("WHERE", Render(sqlIf, new { Name = (string?)null }));

        var sqlWhere = "SELECT * FROM Users {WHERE} {IF:Name} AND Name=@Name {END} {IF:Email} AND Email=@Email {END} {ENDWHERE}";
        var r1 = Render(sqlWhere, new { Name = "john", Email = (string?)null });
        Assert.Contains("WHERE Name", r1);
        Assert.DoesNotContain("WHERE AND", r1);

        var sqlIn = "SELECT * FROM Users {IN:Roles}WHERE Role IN @Roles{END}";
        Assert.Contains("WHERE", Render(sqlIn, new { Roles = new[] { 1, 2 } }));
        Assert.DoesNotContain("WHERE", Render(sqlIn, new { Roles = Array.Empty<int>() }));

        var sqlChoose = "SELECT * FROM t {CHOOSE} {WHEN:Status} AND A=@A {ENDWHEN} {OTHERWISE} AND B=1 {ENDOTHERWISE} {ENDCHOOSE}";
        Assert.Contains("A=@A", Render(sqlChoose, new { Status = "x" }));
        // WHEN with expr
        var sqlChoose2 = "SELECT * FROM t {CHOOSE} {WHEN:Status == 'ACTIVE'} AND A=@A {ENDWHEN} {OTHERWISE} AND B=1 {ENDOTHERWISE} {ENDCHOOSE}";
        Assert.Contains("A=@A", Render(sqlChoose2, new { Status = "ACTIVE" }));
        Assert.Contains("B=1", Render(sqlChoose2, new { Status = "INACTIVE" }));
        Assert.Contains("B=1", Render(sqlChoose, new { Status = (string?)null }));
    }

    // Cenário 7: CHOOSE/WITHIN WHERE com novos operadores
    [Fact]
    public void CHOOSE_within_WHERE_com_GTE()
    {
        var sql = "SELECT * FROM t {WHERE} {CHOOSE} {WHEN:Status == 'X'} AND Status=@Status {ENDWHEN} {OTHERWISE} AND Status='ACTIVE' {ENDOTHERWISE} {ENDCHOOSE} {IF:Age >= 18} AND Age>=@Age {END} {ENDWHERE}";
        var r1 = Render(sql, new { Status = "X", Age = 18 });
        Assert.Contains("Status=@Status", r1);
        Assert.Contains("Age>=@Age", r1);
        Assert.Contains("WHERE", r1);
        Assert.DoesNotContain("WHERE AND", r1);
        // Status null → OTHERWISE + GTE
        var r2 = Render(sql, new { Status = (string?)null, Age = 18 });
        Assert.Contains("Status='ACTIVE'", r2);
        Assert.Contains("Age>=@Age", r2);
        // Age 10 → só CHOOSE
        var r3 = Render(sql, new { Status = "X", Age = 10 });
        Assert.Contains("Status=@Status", r3);
        Assert.DoesNotContain("Age>=", r3);
    }

    [Fact]
    public void Todos_operadores_juntos_sem_WHERE()
    {
        // Testa fora de WHERE também (pipeline top-level) com IF expr
        var sql = "{IF:A == 'a'} A=@A {END} {IF:B != 'x'} B<>@B {END} {IF:C < 10} C<@C {END} {IF:D > 1} D>@D {END} {IF:E >= 3} E>=@E {END} {IF:F <= 4} F<=@F {END} {BETWEEN:G} G BETWEEN @GMin AND @GMax {END}";
        var result = Render(sql, new { A = "a", B = "b", C = 1, D = 2, E = 3, F = 4, GMin = 10, GMax = 20 });
        Assert.Contains("A=@A", result);
        Assert.Contains("B<>@B", result);
        Assert.Contains("C<@C", result);
        Assert.Contains("D>@D", result);
        Assert.Contains("E>=@E", result);
        Assert.Contains("F<=@F", result);
        Assert.Contains("G BETWEEN", result);

        var empty = Render(sql, new { A = "other", B = "x", C = 20, D = 0, E = 0, F = 10, GMin = (int?)null, GMax = (int?)null });
        Assert.DoesNotContain("A=@A", empty);
        Assert.DoesNotContain("B<>@B", empty);
        Assert.DoesNotContain("C<@C", empty);
        Assert.DoesNotContain("D>@D", empty);
        Assert.DoesNotContain("E>=@E", empty);
        Assert.DoesNotContain("F<=@F", empty);
        Assert.DoesNotContain("G BETWEEN", empty);
        Assert.Equal("", empty.Trim());
    }
}
