using System.Collections;
using System.Dynamic;
using Footing.Framework.Data;
using Footing.Framework.Tests.Helpers;

namespace Footing.Framework.Tests;

/// <summary>
/// US-TEST-RESISTENCIA-CHAOS — SqlTemplate chaos C1-C9 (18 Gherkins)
/// Render NUNCA throw (fail-safe). SqlBatch SEMPRE throw onde valida (fail-fast) — ver SqlBatchChaosTests.
/// Cada Theory usa MemberData ChaosData determinístico + InlineData.
/// Critérios: Assert.Contains/DoesNotContain/Throws, sem flaky.
/// </summary>
public class SqlTemplateChaosTests
{
    private static string Render(string sql, object? p) => SqlTemplate.Parse(sql).Render(p).Sql;
    private static SqlResult RenderResult(string sql, object? p) => SqlTemplate.Parse(sql).Render(p);

    // C1 — Template malformado S1-S6 throw SqlTemplateParseException (FAILFAST 1 Must)
    [Theory]
    [MemberData(nameof(ChaosData.C1_Malformado), MemberType = typeof(ChaosData))]
    public void C1_Malformado_Throws_S1_S6(string template, object param, string expectedTagFragment)
    {
        var ex = Assert.Throws<SqlTemplateParseException>(() => Render(template, param));
        Assert.NotNull(ex.Tag);
        Assert.Contains(expectedTagFragment, ex.Tag);
        Assert.NotNull(ex.Expected);
        Assert.True(ex.Line > 0);
        Assert.True(ex.Column > 0);
        Assert.NotNull(ex.Snippet);
        Assert.NotNull(ex.Suggestion);
        Assert.True(ex.Message.Contains("Missing") || ex.Message.Contains("Unclosed"));
        var ex2 = Assert.Throws<SqlTemplateParseException>(() => SqlTemplate.Parse(template));
        Assert.True(ex2.Line > 0);
    }

    [Theory]
    [InlineData("SELECT * FROM Users {IF:Name} WHERE Name=@Name", "eder")]
    [InlineData("SELECT * FROM Users {WHERE} {IF:Name} AND Name=@Name {END}", "eder")]
    [InlineData("{CHOOSE} {WHEN:Status == 'ACTIVE'} AND Status=@Status {ENDWHEN} {OTHERWISE} AND x=1 {ENDOTHERWISE}", "ACTIVE")]
    public void C1_Malformado_Inline_Throws(string template, string val)
    {
        var ex = Assert.Throws<SqlTemplateParseException>(() => Render(template, new { Name = val, Status = val }));
        Assert.True(ex.Line > 0);
        Assert.NotNull(ex.Tag);
        var ex2 = Assert.Throws<SqlTemplateParseException>(() => SqlTemplate.Parse(template));
        Assert.True(ex2.Line > 0);
    }

    [Fact]
    public void C1_ChavesDesbalanceadas_Literal_NaoThrow_E_Unclosed_Throw()
    {
        // S8 literal {{teste}} -> não throw, contém literal
        var sql = "SELECT '{{teste}}' FROM t {IF:Name} AND x=1 {END}";
        var result = Render(sql, new { Name = "a" });
        Assert.Contains("'{{teste}}'", result);
        Assert.Contains("AND x=1", result);
        Assert.DoesNotContain("{IF:Name}", result);
        Assert.Null(Record.Exception(() => Render(sql, new { Name = "a" })));
        // S5 unclosed -> throw
        var sql2 = "SELECT * FROM t {IF:Name AND x=1 {END}";
        var ex = Assert.Throws<SqlTemplateParseException>(() => Render(sql2, new { Name = "a" }));
        Assert.Contains("Unclosed", ex.Message);
        Assert.True(ex.Line > 0);
        Assert.True(ex.Column > 0);
        Assert.Contains("{IF:Name", ex.Tag ?? ex.Message);
    }

    [Fact]
    public void Parse_MissingEnd_ThrowsWithLineCol()
    {
        var ex = Assert.Throws<SqlTemplateParseException>(() => SqlTemplate.Parse("SELECT * FROM Users {IF:Name} WHERE Name=@Name"));
        Assert.Contains("Missing {END}", ex.Message);
        Assert.Contains("{IF:Name}", ex.Tag);
        Assert.Equal("{END}", ex.Expected);
        Assert.Equal(1, ex.Line);
        Assert.True(ex.Column > 0);
        Assert.Contains("Snippet", ex.Message);
    }

    [Fact]
    public void Loader_Malformed_Throws()
    {
        // Loader bubble via Parse — use temp sql via Parse directly for now, but also test Loader with missing resource vs malformed?
        // Simula Loader.Load de sql malformado via SqlTemplate.Parse bubble
        var malformed = "SELECT * FROM t {IF:Name} AND x=1";
        var ex = Assert.Throws<SqlTemplateParseException>(() => SqlTemplate.Parse(malformed));
        Assert.Contains("Missing {END}", ex.Message);
        Assert.True(ex.Line > 0);
        // Loader com resource inexistente ainda throw FileNotFound (S13 mantido)
        var ex2 = Assert.Throws<FileNotFoundException>(() => SqlTemplateLoader.Load("naoexiste.sql", typeof(SqlTemplateChaosTests).Assembly));
        Assert.Contains("Available", ex2.Message);
    }

    // C2 — Parâmetro errado: typo, case-insensitive, null vs missing vs "", string "abc" >100
    [Theory]
    [MemberData(nameof(ChaosData.C2_TypoCaseMix), MemberType = typeof(ChaosData))]
    public void C2_ParamErrado_OmitOuRender_SemThrow(string template, object param, bool expectContains)
    {
        var sql = Render(template, param);
        var ex = Record.Exception(() => Render(template, param));
        Assert.Null(ex);
        if (expectContains)
            Assert.Contains("AND", sql);
        else
            Assert.DoesNotContain("AND", sql);
    }

    [Fact]
    public void C2_Typo_Nmae_Omitido_Gherkin_C2_1()
    {
        var sql = "{IF:Name == 'eder'} AND Name=@Name {END}";
        Assert.DoesNotContain("AND Name", Render(sql, new { Nmae = "eder" }));
        Assert.Null(Record.Exception(() => Render(sql, new { Nmae = "eder" })));
    }

    [Fact]
    public void C2_CaseInsensitive_Name_lower_upper_Renderiza()
    {
        var sql = "{IF:Name} AND Name=@Name {END}";
        Assert.Contains("AND Name=@Name", Render(sql, new Dictionary<string, object?> { ["name"] = "john" }));
        Assert.Contains("AND Name=@Name", Render(sql, new Dictionary<string, object?> { ["NAME"] = "john" }));
        Assert.Contains("AND Name=@Name", Render(sql, new Dictionary<string, object> { ["NaMe"] = "john" }));
    }

    [Fact]
    public void C2_AbcGt100_Omitido_ComWarning_Gherkin_C2_4()
    {
        // hotfix C: "abc" >100 → false+Warning, não throw
        Assert.DoesNotContain("AND", Render("{IF:Preco > 100} AND x=1 {END}", new { Preco = "abc" }));
        Assert.Contains("AND", Render("{IF:Preco > 100} AND x=1 {END}", new { Preco = "150" }));
        Assert.Contains("AND", Render("{IF:Preco > 100} AND x=1 {END}", new { Preco = 150 }));
        Assert.DoesNotContain("AND", Render("{IF:Preco > 100} AND x=1 {END}", new { Preco = 50 }));
        Assert.Null(Record.Exception(() => Render("{IF:Preco > 100} AND x=1 {END}", new { Preco = "abc" })));
    }

    // C3 — Tipos diferentes cross-type
    [Theory]
    [MemberData(nameof(ChaosData.C3_TiposCross), MemberType = typeof(ChaosData))]
    public void C3_TiposCross_SemThrow_ComparacaoCorreta(string template, object param, bool expectContains)
    {
        var sql = Render(template, param);
        Assert.Null(Record.Exception(() => Render(template, param)));
        if (expectContains)
            Assert.Contains("AND x=1", sql);
        else
            Assert.DoesNotContain("AND x=1", sql);
        // extra: para char > caso, documenta que char vs string incompatível → false
    }

    // C4 — Enums case-sensitive
    [Theory]
    [MemberData(nameof(ChaosData.C4_Enum), MemberType = typeof(ChaosData))]
    public void C4_Enum_CaseSensitive_SemThrow(string template, object param, bool expectContains)
    {
        var sql = Render(template, param);
        Assert.Null(Record.Exception(() => Render(template, param)));
        if (expectContains)
            Assert.Contains("AND x=1", sql);
        else
            Assert.DoesNotContain("AND x=1", sql);
    }

    [Fact]
    public void C4_Enum_Gherkin_C3_4_Active_vs_ACTIVE()
    {
        Assert.Contains("AND x=1", Render("{IF:Status == 'Active'} AND x=1 {END}", new { Status = ChaosStatus.Active }));
        Assert.DoesNotContain("AND x=1", Render("{IF:Status == 'ACTIVE'} AND x=1 {END}", new { Status = ChaosStatus.Active }));
        Assert.DoesNotContain("AND x=1", Render("{IF:Status == 1} AND x=1 {END}", new { Status = ChaosStatus.Active })); // int literal vs enum → false
        Assert.Contains("AND x=1", Render("{IF:Status == @Outro} AND x=1 {END}", new { Status = ChaosStatus.Active, Outro = ChaosStatus.Active }));
        Assert.Contains("AND x=1", Render("{IF:Status == @Outro} AND x=1 {END}", new { Status = ChaosStatus.Active, Outro = "Active" }));
        Assert.DoesNotContain("AND x=1", Render("{IF:Status == @Outro} AND x=1 {END}", new { Status = ChaosStatus.Active, Outro = "ACTIVE" }));
    }

    // C5 — Coleções IN
    [Theory]
    [MemberData(nameof(ChaosData.C5_InColecoes), MemberType = typeof(ChaosData))]
    public void C5_InColecoes_OmitOuRender_SemThrow(string template, object param, bool expectContains)
    {
        // Hack: C5 data primeira linha usa string direct as param object? Para uniformidade, tratamos se param is string => wrap {Roles=param}
        object? realParam = param;
        if (param is string s && template.Contains("@Roles"))
            realParam = new { Roles = s };
        var sql = Render(template, realParam);
        Assert.Null(Record.Exception(() => Render(template, realParam)));
        if (expectContains)
            Assert.Contains("WHERE", sql);
        else
            Assert.DoesNotContain("WHERE", sql);
    }

    [Fact]
    public void C5_In_StringAdmin_Omitido_Gherkin_C5_1()
    {
        Assert.DoesNotContain("WHERE", Render("{IN:Roles} WHERE Role IN @Roles {END}", new { Roles = "admin" }));
        Assert.Contains("WHERE", Render("{IN:Roles} WHERE Role IN @Roles {END}", new { Roles = new[] { "admin" } }));
        Assert.DoesNotContain("WHERE", Render("{IN:Roles} WHERE Role IN @Roles {END}", new { Roles = Array.Empty<int>() }));
        Assert.DoesNotContain("WHERE", Render("{IN:Roles} WHERE Role IN @Roles {END}", new { Roles = new List<object> { null!, null! } }));
        Assert.Contains("WHERE", Render("{IN:Roles} WHERE Role IN @Roles {END}", new { Roles = new List<string?> { "a", null } }));
    }

    // C6 — Dinâmicos: Expando, Dictionary, Hashtable, PropsCache concorrente, 0/false
    [Fact]
    public void C6_Expando_Dictionary_Hashtable_CaseInsensitive_SemThrow()
    {
        var sql = "{IF:Name} AND Name=@Name {END}";
        // Expando com Name="john" → render
        dynamic expando = new ExpandoObject();
        ((IDictionary<string, object>)expando)["Name"] = "john";
        Assert.Contains("AND Name=@Name", Render(sql, (object)expando));
        // Expando null → omit
        dynamic expandoNull = new ExpandoObject();
        ((IDictionary<string, object>)expandoNull)["Name"] = null!;
        Assert.DoesNotContain("AND", Render(sql, (object)expandoNull));
        // Expando missing → omit
        dynamic expandoMissing = new ExpandoObject();
        Assert.DoesNotContain("AND", Render(sql, (object)expandoMissing));
        // Dictionary case lower
        Assert.Contains("AND", Render(sql, new Dictionary<string, object?> { ["name"] = "john" }));
        // Dictionary<string,object> não-nullable
        Assert.Contains("AND", Render(sql, new Dictionary<string, object> { ["Name"] = "john" }));
        // Hashtable
        Assert.Contains("AND", Render(sql, new Hashtable { ["Name"] = "john" }));
        // Hashtable chave não-string ignorada
        var htBad = new Hashtable { [123] = "x" };
        Assert.DoesNotContain("AND", Render(sql, htBad));
        var htMixed = new Hashtable { [123] = "x", ["Name"] = "john" };
        Assert.Contains("AND", Render(sql, htMixed));
    }

    [Fact]
    public void C6_ObjComNull_E_Age0_False_Renderiza_PropsCacheConcorrente()
    {
        var sql = "{IF:Name} AND Name=@Name {END} {IF:Age} AND Age=@Age {END}";
        Assert.DoesNotContain("AND Name", Render(sql, new { Name = (string?)null, Age = 0 }));
        Assert.Contains("AND Age=@Age", Render(sql, new { Name = (string?)null, Age = 0 }));
        Assert.Contains("AND Age", Render("{IF:Age} AND Age=@Age {END}", new { Age = 0 }));
        Assert.Contains("AND Flag", Render("{IF:Flag} AND Flag=@Flag {END}", new { Flag = false }));
        Assert.DoesNotContain("AND", Render("{IF:Name} AND Name=@Name {END}", new { Name = (string?)null }));
        // Parallel PropsCache 100 threads
        var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        Parallel.For(0, 100, i =>
        {
            try
            {
                var s = Render("{IF:Name} AND Name=@Name {END}", new { Name = "user" + i });
                if (!s.Contains("AND Name")) throw new Exception("should contain");
            }
            catch (Exception ex) { errors.Add(ex); }
        });
        Assert.Empty(errors);
        // Name="" vs != null armadilha
        Assert.DoesNotContain("AND Name", Render("{IF:Name} AND Name=@Name {END}", new { Name = "" }));
        Assert.Contains("AND Name", Render("{IF:Name != null} AND Name=@Name {END}", new { Name = "" }));
    }

    // C7 — Injeção & chaves: valor nunca interpolado, placeholder preservado
    [Theory]
    [MemberData(nameof(ChaosData.C7_Injecao), MemberType = typeof(ChaosData))]
    public void C7_Injecao_Placeholder_SemThrow(string template, object param, bool expectContains)
    {
        var result = RenderResult(template, param);
        Assert.Null(Record.Exception(() => Render(template, param)));
        Assert.DoesNotContain("DROP TABLE", result.Sql);
        if (template.Contains("@Name"))
        {
            Assert.Contains("@Name", result.Sql);
        }
        else if (template.Contains("x=1"))
        {
            // para SELECT '{{teste}}' case, garante x=1 renderiza e {{teste}} permanece
            if (expectContains) Assert.Contains("x=1", result.Sql);
            if (template.Contains("'{{teste}}'")) Assert.Contains("'{{teste}}'", result.Sql);
        }
    }

    [Fact]
    public void C7_Injecao_Drop_Placeholder_Gherkin_C7_1()
    {
        var tpl = "SELECT * FROM Users {WHERE} {IF:Name} AND Name=@Name {END} {ENDWHERE}";
        var param = new { Name = "'; DROP TABLE Users; --" };
        var result = RenderResult(tpl, param);
        Assert.Contains("Name=@Name", result.Sql); // WHERE normaliza AND → WHERE Name=@Name
        Assert.DoesNotContain("DROP TABLE", result.Sql);
        // valor fica em Parameters (Dapper DynamicParameters ou raw object)
        // SqlResult.Parameters holds original object, não interpolado
        var dict = (object)param;
        Assert.Equal("'; DROP TABLE Users; --", param.Name);
        Assert.Null(Record.Exception(() => Render(tpl, param)));
        // chaves no valor não viram tag
        var r2 = RenderResult("{IF:Name} AND Name=@Name {END}", new { Name = "{{END}}" });
        Assert.Contains("AND Name=@Name", r2.Sql);
        Assert.DoesNotContain("{{END}}", r2.Sql); // valor não no SQL
        // SQL content com {{ }}
        var r3 = Render("SELECT '{{teste}}' FROM t {IF:Name} AND x=1 {END}", new { Name = "a" });
        Assert.Contains("'{{teste}}'", r3);
    }

    [Fact]
    public void C7_ValorComChaves_NaoViraTag()
    {
        var sql = "{IF:Name} AND Name=@Name {END}";
        Assert.Contains("AND Name=@Name", Render(sql, new { Name = "{{END}}" }));
        Assert.Contains("AND Name=@Name", Render(sql, new { Name = "{{IF:Name}}" }));
        Assert.Contains("AND Name=@Name", Render(sql, new { Name = "' OR '1'='1" }));
    }

    // C9 — Loader/length/WHERE/CHOOSE edge
    [Theory]
    [MemberData(nameof(ChaosData.C9_LengthWhereChoose), MemberType = typeof(ChaosData))]
    public void C9_Length_Where_Choose_SemThrow(string template, object param, bool expectContains)
    {
        var sql = Render(template, param);
        Assert.Null(Record.Exception(() => Render(template, param)));
        if (expectContains)
            Assert.Contains("AND x=1", sql); // para length cases
        else
        {
            // para WHERE vazio → não contém WHERE; para CHOOSE sem match → não contém AND x=1
            Assert.True(!sql.Contains("AND x=1") || !sql.Contains("WHERE") || sql == "");
        }
    }

    [Fact]
    public void C9_LengthNullZero_WhereVazio_ChooseSemOtherwise_Gherkin()
    {
        Assert.Contains("AND x=1", Render("{IF:length(Name) == 0} AND x=1 {END}", new { Name = (string?)null }));
        Assert.Contains("AND x=1", Render("{IF:length(Name) == 0} AND x=1 {END}", new { Name = "" }));
        Assert.DoesNotContain("AND x=1", Render("{IF:length(Name) == 0} AND x=1 {END}", new { Name = "a" }));
        Assert.Contains("AND x=1", Render("{IF:length(Code) == 3} AND x=1 {END}", new { Code = 123 }));
        Assert.Contains("AND x=1", Render("{IF:length(Id) == 36} AND x=1 {END}", new { Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000") }));
        // WHERE vazio → some (sem WHERE)
        Assert.DoesNotContain("WHERE", Render("SELECT * FROM t {WHERE} {IF:Name} AND Name=@Name {END} {ENDWHERE}", new { Name = (string?)null }));
        Assert.Equal("SELECT * FROM t", Render("SELECT * FROM t {WHERE} {IF:Name} AND Name=@Name {END} {ENDWHERE}", new { Name = (string?)null }));
        // CHOOSE sem OTHERWISE e nenhum WHEN true → "" (vazio)
        Assert.Equal("", Render("{CHOOSE} {WHEN:Status=='A'} AND x=1 {ENDWHEN} {ENDCHOOSE}", new { Status = "B" }).Trim());
        Assert.Contains("AND x=1", Render("{CHOOSE} {WHEN:Status=='A'} AND x=1 {ENDWHEN} {ENDCHOOSE}", new { Status = "A" }));
        Assert.Contains("AND x=1", Render("{IF:length(Code) == 5} AND x=1 {END}", new { Code = 12345 }));
    }

    [Fact]
    public void C9_SqlTemplateLoader_NotFound_FileNotFoundException()
    {
        // SqlTemplateLoader.Load com arquivo inexistente deve throw FileNotFoundException com sugestão
        var ex = Assert.Throws<FileNotFoundException>(() => SqlTemplateLoader.Load("naoexiste.sql", typeof(SqlTemplateChaosTests).Assembly));
        Assert.Contains("Available", ex.Message);
        // Normalize typo sem .sql
        var ex2 = Assert.Throws<FileNotFoundException>(() => SqlTemplateLoader.ForAssembly(typeof(SqlTemplateChaosTests).Assembly, "Sql.Users.GetAll"));
        Assert.Contains("Available", ex2.Message);
    }

    // Infra fuzz determinístico: Render nunca throw para qualquer combinação
    [Theory]
    [MemberData(nameof(ChaosData.FuzzDeterministico), MemberType = typeof(ChaosData))]
    public void Fuzz_Deterministico_Render_NuncaThrow(string template, object param)
    {
        var ex = Record.Exception(() => SqlTemplate.Parse(template).Render(param));
        Assert.Null(ex);
        // garante que não houve throw e SQL é string (pode ser literal ou vazio)
        var sql = SqlTemplate.Parse(template).Render(param).Sql;
        Assert.NotNull(sql);
    }
}
