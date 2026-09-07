using System.Collections;
using System.Collections.Generic;
using Footing.Framework.Data;

namespace Footing.Framework.Tests;

/// <summary>
/// Gilson Chaos Exploratory — promoção dos 80 caos de /tmp/gilson_caos/Program.cs (113L) para xUnit permanente.
/// C1 malformado MustThrow line:col, C2 fail-safe MustNotThrow, C3 tipos, C4 enum case-sensitive, C5 IN, C6 null Age0,
/// C7 injeção placeholder, C8 SqlBatch guard, C9 length/WHERE/CHOOSE, AND/Won't, BIND/INCLUDE/TRIM/SET/IFDEFINED/unicode.
/// Total 80 Facts determinísticos, sem Thread.Sleep, sem flaky. Cobertura: 393→473 no CI, TWE true, 11→11 regex, pack 78K.
/// </summary>
public class GilsonChaosExploratoryTests
{
    private static string R(string sql, object? p = null) => SqlTemplate.Parse(sql).Render(p).Sql;
    private static SqlResult RR(string sql, object? p = null) => SqlTemplate.Parse(sql).Render(p);

    // ReSharper disable InconsistentNaming
    private enum Status2 { Active, Inactive }
    private class Foo2 { public string? Name { get; set; } public int Age { get; set; } public Status2 Status { get; set; } public List<int> Roles { get; set; } = new(); public Guid Id { get; set; } = Guid.NewGuid(); public decimal Preco { get; set; } }
    private class Ten2 { public int C1 { get; set; } public int C2 { get; set; } public int C3 { get; set; } public int C4 { get; set; } public int C5 { get; set; } public int C6 { get; set; } public int C7 { get; set; } public int C8 { get; set; } public int C9 { get; set; } public int C10 { get; set; } }

    // ───────────────── C1 — Template malformado MustThrow com line:col ─────────────────

    [Fact] public void C1_001_Missing_END_Throws() {
        var ex = Assert.Throws<SqlTemplateParseException>(() => SqlTemplate.Parse("SELECT * FROM t {IF:Name} WHERE x=1"));
        Assert.Contains("{END}", ex.Expected ?? ex.Message); Assert.True(ex.Line > 0); Assert.True(ex.Column > 0); Assert.Contains("Missing", ex.Message);
    }
    [Fact] public void C1_002_Missing_ENDWHERE_Throws() {
        var ex = Assert.Throws<SqlTemplateParseException>(() => SqlTemplate.Parse("SELECT * FROM t {WHERE} {IF:Name} AND x=1 {END}"));
        Assert.Contains("{ENDWHERE}", ex.Expected ?? ex.Message); Assert.True(ex.Line > 0);
    }
    [Fact] public void C1_003_CHOOSE_Sem_ENDCHOOSE_Throws() {
        var ex = Assert.Throws<SqlTemplateParseException>(() => SqlTemplate.Parse("{CHOOSE} {WHEN:Status=='A'} AND x=1 {ENDWHEN}"));
        Assert.Contains("{ENDCHOOSE}", ex.Expected ?? ex.Message); Assert.True(ex.Line > 0);
    }
    [Fact] public void C1_004_WHEN_Sem_ENDWHEN_Throws() {
        var ex = Assert.Throws<SqlTemplateParseException>(() => SqlTemplate.Parse("{CHOOSE} {WHEN:Status=='A'} AND x=1 {OTHERWISE} y {ENDOTHERWISE} {ENDCHOOSE}"));
        Assert.Contains("{ENDWHEN}", ex.Expected ?? ex.Message); Assert.True(ex.Line > 0);
    }
    [Fact] public void C1_005_Unclosed_IF_MissingBrace_Throws() {
        var ex = Assert.Throws<SqlTemplateParseException>(() => SqlTemplate.Parse("SELECT * FROM t {IF:Name AND x=1 {END}"));
        Assert.Contains("Unclosed", ex.Message); Assert.True(ex.Line > 0); Assert.True(ex.Column > 0);
    }
    [Fact] public void C1_006_BETWEEN_Sem_END_Throws() {
        var ex = Assert.Throws<SqlTemplateParseException>(() => SqlTemplate.Parse("{BETWEEN:Price} AND x=1"));
        Assert.Contains("{END}", ex.Expected ?? ex.Message); Assert.True(ex.Line > 0);
    }
    [Fact] public void C1_007_Literal_Chaves_Duplas_NaoThrow() {
        var ex = Record.Exception(() => R("SELECT '{{literal}}' FROM t", new { }));
        Assert.Null(ex);
        var r = R("SELECT '{{literal}}' FROM t", new { });
        Assert.True(r.Contains("{{literal}}") || r.Contains("literal"));
    }
    [Fact] public void C1_008_Missing_END_LineCol_Snippet_Suggestion() {
        var ex = Assert.Throws<SqlTemplateParseException>(() => SqlTemplate.Parse("SELECT * FROM Users {IF:Name} WHERE Name=@Name"));
        Assert.Contains("Missing {END}", ex.Message); Assert.Equal("{END}", ex.Expected); Assert.Equal(1, ex.Line); Assert.NotNull(ex.Tag); Assert.NotNull(ex.Snippet); Assert.NotNull(ex.Suggestion);
    }
    [Fact] public void C1_009_Mismatched_ENDWHERE_vs_END_Throws() {
        var ex = Assert.Throws<SqlTemplateParseException>(() => SqlTemplate.Parse("SELECT * FROM t {WHERE} AND x=1 {END}"));
        Assert.Contains("{ENDWHERE}", ex.Message); Assert.True(ex.Line > 0);
    }

    // ───────────────── C2 — Fail-safe Render nunca throw (typo/case/null/string>100) ─────────────────

    [Fact] public void C2_001_Typo_Nmae_Omitido_SemThrow() {
        var ex = Record.Exception(() => R("{IF:Name} AND Name=@Name {END}", new { Nmae = "eder" }));
        Assert.Null(ex); Assert.DoesNotContain("Name=@Name", R("{IF:Name} AND Name=@Name {END}", new { Nmae = "eder" }));
    }
    [Fact] public void C2_002_Case_NAME_Renderiza_SemThrow() {
        var ex = Record.Exception(() => R("{IF:Name} AND Name=@Name {END}", new Dictionary<string, object?> { ["NAME"] = "eder" }));
        Assert.Null(ex); Assert.Contains("Name=@Name", R("{IF:Name} AND Name=@Name {END}", new Dictionary<string, object?> { ["NAME"] = "eder" }));
    }
    [Fact] public void C2_003_Case_name_Lower_Renderiza() {
        Assert.Contains("Name=@Name", R("{IF:Name} AND Name=@Name {END}", new Dictionary<string, object?> { ["name"] = "eder" }));
        Assert.Null(Record.Exception(() => R("{IF:Name} AND Name=@Name {END}", new Dictionary<string, object?> { ["name"] = "eder" })));
    }
    [Fact] public void C2_004_Null_Param_Omitido_SemThrow() {
        Assert.DoesNotContain("Name", R("{IF:Name} AND Name=@Name {END}", null!));
        Assert.Null(Record.Exception(() => R("{IF:Name} AND Name=@Name {END}", null!)));
        var r = RR("{IF:Name} AND Name=@Name {END}", null);
        Assert.DoesNotContain("Name=@Name", r.Sql);
    }
    [Fact] public void C2_005_String_Abc_Gt100_False_SemThrow() {
        Assert.DoesNotContain("x=1", R("{IF:Preco > 100} AND x=1 {END}", new { Preco = "abc" }));
        Assert.Null(Record.Exception(() => R("{IF:Preco > 100} AND x=1 {END}", new { Preco = "abc" })));
    }
    [Fact] public void C2_006_String_150_Gt100_True() {
        Assert.Contains("x=1", R("{IF:Preco > 100} AND x=1 {END}", new { Preco = "150" }));
        Assert.Contains("x=1", R("{IF:Preco > 100} AND x=1 {END}", new { Preco = 150 }));
    }
    [Fact] public void C2_007_Missing_Param_Omitido() {
        Assert.DoesNotContain("Name", R("{IF:Name} AND Name=@Name {END}", new { }));
        Assert.Null(Record.Exception(() => R("{IF:Name} AND Name=@Name {END}", new { })));
    }
    [Fact] public void C2_008_EmptyString_Omitido() {
        Assert.DoesNotContain("Name", R("{IF:Name} AND Name=@Name {END}", new { Name = "" }));
        Assert.Contains("Name", R("{IF:Name != null} AND Name=@Name {END}", new { Name = "" }));
    }

    // ───────────────── C3 — Tipos int/long/decimal/double/bool/Guid/DateTime/enum ─────────────────

    [Fact] public void C3_001_Int_Gt100_True() => Assert.Contains("x=1", R("{IF:Preco > 100} AND x=1 {END}", new { Preco = 150 }));
    [Fact] public void C3_002_Long_Gt100_True() => Assert.Contains("x=1", R("{IF:Preco > 100} AND x=1 {END}", new { Preco = 150L }));
    [Fact] public void C3_003_Decimal_Gt100_True() => Assert.Contains("x=1", R("{IF:Preco > 100} AND x=1 {END}", new { Preco = 150.5m }));
    [Fact] public void C3_004_Double_Gt100_True() => Assert.Contains("x=1", R("{IF:Preco > 100} AND x=1 {END}", new { Preco = 150.5d }));
    [Fact] public void C3_005_Bool_True_Render() => Assert.Contains("x=1", R("{IF:Flag == true} AND x=1 {END}", new { Flag = true }));
    [Fact] public void C3_006_Bool_False_NotEqual_True() => Assert.DoesNotContain("x=1", R("{IF:Flag == true} AND x=1 {END}", new { Flag = false }));
    [Fact] public void C3_007_Guid_NotEqual_False() {
        var gid = Guid.NewGuid();
        Assert.DoesNotContain("x=1", R("{IF:Id == '00000000-0000-0000-0000-000000000000'} AND x=1 {END}", new { Id = gid }));
        Assert.Null(Record.Exception(() => R("{IF:Id == '00000000-0000-0000-0000-000000000000'} AND x=1 {END}", new { Id = gid })));
    }
    [Fact] public void C3_008_Guid_Equal_True() {
        var gid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        Assert.Contains("x=1", R("{IF:Id == '550e8400-e29b-41d4-a716-446655440000'} AND x=1 {END}", new { Id = gid }));
    }
    [Fact] public void C3_009_DateTime_Gt_True() => Assert.Contains("x=1", R("{IF:Data > '2024-01-01'} AND x=1 {END}", new { Data = DateTime.Parse("2024-12-31") }));
    [Fact] public void C3_010_Enum_Active_True() => Assert.Contains("x=1", R("{IF:Status == 'Active'} AND x=1 {END}", new { Status = Status2.Active }));

    // ───────────────── C4 — Enum case-sensitive ─────────────────

    [Fact] public void C4_001_Enum_Active_CaseSensitive_True() => Assert.Contains("x=1", R("{IF:Status == 'Active'} AND x=1 {END}", new { Status = Status2.Active }));
    [Fact] public void C4_002_Enum_ACTIVE_False_CaseSensitive() => Assert.DoesNotContain("x=1", R("{IF:Status == 'ACTIVE'} AND x=1 {END}", new { Status = Status2.Active }));
    [Fact] public void C4_003_Enum_Param_Same_True() => Assert.Contains("x=1", R("{IF:Status == @Outro} AND x=1 {END}", new { Status = Status2.Active, Outro = Status2.Active }));
    [Fact] public void C4_004_Enum_Param_ACTIVE_False_CaseSensitive() => Assert.DoesNotContain("x=1", R("{IF:Status == @Outro} AND x=1 {END}", new { Status = Status2.Active, Outro = "ACTIVE" }));
    [Fact] public void C4_005_Enum_Inactive_True() => Assert.Contains("x=1", R("{IF:Status == 'Inactive'} AND x=1 {END}", new { Status = Status2.Inactive }));

    // ───────────────── C5 — IN vazio/string/nulls ─────────────────

    [Fact] public void C5_001_IN_Vazio_Omitido() => Assert.DoesNotContain("Role", R("{IN:Roles} WHERE Role IN @Roles {END}", new { Roles = new List<int>() }));
    [Fact] public void C5_002_IN_ComItens_Render() => Assert.Contains("Role", R("{IN:Roles} WHERE Role IN @Roles {END}", new { Roles = new List<int> { 1, 2 } }));
    [Fact] public void C5_003_IN_String_Omitido() => Assert.DoesNotContain("Role", R("{IN:Roles} WHERE Role IN @Roles {END}", new { Roles = "admin" }));
    [Fact] public void C5_004_IN_Nulls_Omitido() => Assert.DoesNotContain("Role", R("{IN:Roles} WHERE Role IN @Roles {END}", new { Roles = new List<int?> { null, null } }));
    [Fact] public void C5_005_IN_Array_ComUm_Render() => Assert.Contains("Role", R("{IN:Roles} WHERE Role IN @Roles {END}", new { Roles = new[] { 1 } }));
    [Fact] public void C5_006_IN_ComValidoENull_AindaTrue() => Assert.Contains("Role", R("{IN:Roles} WHERE Role IN @Roles {END}", new { Roles = new List<string?> { "a", null } }));

    // ───────────────── C6 — null Age 0 ─────────────────

    [Fact] public void C6_001_ObjComNull_NameNull_Omitido() => Assert.DoesNotContain("Name=@Name", R("{IF:Name} AND Name=@Name {END}", new Foo2 { Name = null, Age = 0 }));
    [Fact] public void C6_002_Age0_Shorthand_True() => Assert.Contains("Age", R("{IF:Age} AND Age=@Age {END}", new { Age = 0 }));
    [Fact] public void C6_003_FlagFalse_Shorthand_True() => Assert.Contains("Flag", R("{IF:Flag} AND Flag=@Flag {END}", new { Flag = false }));

    // ───────────────── C7 — Injeção placeholder ─────────────────

    [Fact] public void C7_001_Placeholder_DROP_NaoInterpolado() {
        var r = RR("SELECT * FROM Users {WHERE} {IF:Name} AND Name=@Name {END} {ENDWHERE}", new { Name = "'; DROP TABLE Users; --" });
        Assert.Contains("Name=@Name", r.Sql); Assert.DoesNotContain("DROP TABLE", r.Sql); Assert.Null(Record.Exception(() => RR("SELECT * FROM Users {WHERE} {IF:Name} AND Name=@Name {END} {ENDWHERE}", new { Name = "'; DROP TABLE Users; --" })));
    }
    [Fact] public void C7_002_Placeholder_END_NaoViraTag() {
        var r = RR("{IF:Name} AND Name=@Name {END}", new { Name = "{{END}}" });
        Assert.Contains("Name=@Name", r.Sql); Assert.Null(Record.Exception(() => RR("{IF:Name} AND Name=@Name {END}", new { Name = "{{END}}" })));
    }
    [Fact] public void C7_003_Placeholder_OR_Injection_Placeholder() {
        var r = RR("{IF:Name} AND Name=@Name {END}", new { Name = "' OR '1'='1" });
        Assert.Contains("Name=@Name", r.Sql); Assert.DoesNotContain("' OR '1'='1", r.Sql);
    }
    [Fact] public void C7_004_Placeholder_IF_Injection_NaoViraTag() {
        var r = RR("{IF:Name} AND Name=@Name {END}", new { Name = "{{IF:Name}}" });
        Assert.Contains("Name=@Name", r.Sql);
    }

    // ───────────────── C8 — SqlBatch guard Users; DROP ─────────────────

    [Fact] public void C8_001_BadTable_SemicolonDrop_Throws() {
        var ex = Assert.Throws<ArgumentException>(() => SqlBatch.BuildBatchInsert("Users; DROP TABLE Users; --", new[] { new { Id = 1 } }));
        Assert.Equal("tableName", ex.ParamName); Assert.Contains("invalid characters", ex.Message);
    }
    [Fact] public void C8_002_BadTable_SpacePrefix_Throws() {
        var ex = Assert.Throws<ArgumentException>(() => SqlBatch.BuildBatchInsert(" Users", new[] { new { Id = 1 } }));
        Assert.Equal("tableName", ex.ParamName);
    }
    [Fact] public void C8_003_BadTable_DigitPrefix_Throws() {
        var ex = Assert.Throws<ArgumentException>(() => SqlBatch.BuildBatchInsert("1Users", new[] { new { Id = 1 } }));
        Assert.Equal("tableName", ex.ParamName);
    }
    [Fact] public void C8_004_Valid_Schema_Users_NaoThrow() {
        var ex = Record.Exception(() => SqlBatch.BuildBatchInsert("schema.Users", new[] { new { Id = 1, Name = "a" } }));
        Assert.Null(ex); var (sql, _) = SqlBatch.BuildBatchInsert("schema.Users", new[] { new { Id = 1, Name = "a" } }); Assert.Contains("schema.Users", sql);
    }
    [Fact] public void C8_005_ColumnsOverride_Typo_Total_NoOp() {
        var (sql, p) = SqlBatch.BuildBatchInsert("Users", new[] { new { Id = 1, Name = "a" } }, "TYPO");
        Assert.Equal("", sql); Assert.Empty(p.ParameterNames); Assert.Null(Record.Exception(() => SqlBatch.BuildBatchInsert("Users", new[] { new { Id = 1, Name = "a" } }, "TYPO")));
    }
    [Fact] public void C8_006_EffectiveBatch_2100_10cols_Chunk210_NaoThrow() {
        var items = Enumerable.Range(1, 500).Select(i => new Ten2 { C1 = i, C2 = i, C3 = i, C4 = i, C5 = i, C6 = i, C7 = i, C8 = i, C9 = i, C10 = i });
        var ex = Record.Exception(() => SqlBatch.BuildBatchInsert("Users", items));
        Assert.Null(ex); var (sql, _) = SqlBatch.BuildBatchInsert("Users", items); Assert.True(sql.Length > 0);
    }
    [Fact] public void C8_007_BuildBatchInsert_SemValoresLiterais_SoPlaceholder() {
        var (sql, _) = SqlBatch.BuildBatchInsert("Users", new[] { new { Name = "eder; DROP" } });
        Assert.DoesNotContain("eder", sql); Assert.Contains("@p0_Name", sql);
    }
    [Fact] public void C8_008_BadTable_DashDash_Throws() {
        var ex = Assert.Throws<ArgumentException>(() => SqlBatch.BuildBatchInsert("Users--", new[] { new { Id = 1 } }));
        Assert.Equal("tableName", ex.ParamName);
    }

    // ───────────────── C9 — length null/WHERE vazio/CHOOSE otherwise ─────────────────

    [Fact] public void C9_001_Length_Null_0_True() => Assert.Contains("x=1", R("{IF:length(Name) == 0} AND x=1 {END}", new { Name = (string?)null }));
    [Fact] public void C9_002_Length_Empty_0_True() => Assert.Contains("x=1", R("{IF:length(Name) == 0} AND x=1 {END}", new { Name = "" }));
    [Fact] public void C9_003_Length_A_False() => Assert.DoesNotContain("x=1", R("{IF:length(Name) == 0} AND x=1 {END}", new { Name = "a" }));
    [Fact] public void C9_004_WHERE_Vazio_NaoContemWHERE() {
        var r = R("SELECT * FROM t {WHERE} {IF:Name} AND Name=@Name {END} {ENDWHERE}", new { Name = (string?)null });
        Assert.DoesNotContain("WHERE", r); Assert.Equal("SELECT * FROM t", r.Trim());
    }
    [Fact] public void C9_005_CHOOSE_Otherwise_Render() {
        var r = R("{CHOOSE} {WHEN:Status=='A'} AND x=1 {ENDWHEN} {OTHERWISE} AND y=1 {ENDOTHERWISE} {ENDCHOOSE}", new { Status = "B" });
        Assert.Contains("y=1", r); Assert.DoesNotContain("x=1", r);
    }
    [Fact] public void C9_006_CHOOSE_WhenTrue_Render() {
        var r = R("{CHOOSE} {WHEN:Status=='A'} AND x=1 {ENDWHEN} {OTHERWISE} AND y=1 {ENDOTHERWISE} {ENDCHOOSE}", new { Status = "A" });
        Assert.Contains("x=1", r);
    }

    // ───────────────── AND / Won't or / parens ─────────────────

    [Fact] public void AND_001_Ok_2Fatores_True() => Assert.Contains("x=1", R("{IF:Name and Age > 18} AND x=1 {END}", new { Name = "eder", Age = 20 }));
    [Fact] public void AND_002_3Fatores_False_Wont() => Assert.DoesNotContain("x=1", R("{IF:A and B and C} AND x=1 {END}", new { A = "a", B = "b", C = "c" }));
    [Fact] public void Wont_001_OR_NaoSuportado_False() => Assert.DoesNotContain("x=1", R("{IF:Name or Age > 18} AND x=1 {END}", new { Name = "eder", Age = 20 }));
    [Fact] public void Wont_002_Parens_NaoSuportado_False() => Assert.DoesNotContain("x=1", R("{IF:(Name)} AND x=1 {END}", new { Name = "eder" }));

    // ───────────────── BIND / INCLUDE / TRIM / SET / IFDEFINED / unicode ─────────────────

    [Fact] public void BIND_001_Concat_Full_LIKE_Param() {
        var r = RR("{BIND:Full, value=\"'%' + Name + '%'\"} SELECT * FROM t {WHERE} {IF:Full} AND Name LIKE @Full {END} {ENDWHERE}", new { Name = "eder" });
        var d = (IDictionary<string, object?>)r.Parameters!;
        Assert.Contains("LIKE", r.Sql); Assert.True(d.ContainsKey("Full")); Assert.Null(Record.Exception(() => RR("{BIND:Full, value=\"'%' + Name + '%'\"} SELECT * FROM t {WHERE} {IF:Full} AND Name LIKE @Full {END} {ENDWHERE}", new { Name = "eder" })));
    }
    [Fact] public void INCLUDE_001_Missing_Literal_Preservado() {
        var r = R("SELECT {INCLUDE:FragInexistente} FROM t", new { });
        Assert.Contains("{INCLUDE:FragInexistente}", r);
    }
    [Fact] public void TRIM_001_PrefixWhere_StripsAND() {
        var r = R("{TRIM prefix=\"WHERE\" prefixOverrides=\"AND|OR \"} AND Name=@Name {ENDTRIM}", new { Name = "eder" });
        Assert.Contains("WHERE Name", r);
    }
    [Fact] public void SET_001_RemoveTrailingComma() {
        var r = R("UPDATE t {SET} {IF:Name} Name=@Name, {END} {IF:Age} Age=@Age, {END} {ENDSET} WHERE Id=1", new { Name = "eder", Age = (int?)null });
        Assert.Contains("SET", r); Assert.Contains("Name", r); Assert.DoesNotContain("Age", r);
    }
    [Fact] public void IFDEFINED_001_Null_Defined_True() => Assert.Contains("x=1", R("{IFDEFINED:Name} AND x=1 {END}", new { Name = (string?)null }));
    [Fact] public void IFDEFINED_002_Missing_False() => Assert.DoesNotContain("x=1", R("{IFDEFINED:Name} AND x=1 {END}", new { }));
    [Fact] public void IFDEFINED_003_IFNOTDEFINED_Missing_True() => Assert.Contains("x=1", R("{IFNOTDEFINED:Name} AND x=1 {END}", new { }));
    [Fact] public void Unicode_001_10k_Rocket_NaoThrow() {
        var big = new string('A', 10000) + " 🚀💣\u200B\uFEFF";
        var ex = Record.Exception(() => R("{IF:Name} AND Name=@Name {END}", new { Name = big }));
        Assert.Null(ex); Assert.Contains("Name=@Name", R("{IF:Name} AND Name=@Name {END}", new { Name = big }));
    }
    [Fact] public void C9_007_Length_Guid_36_True() => Assert.Contains("x=1", R("{IF:length(Id) == 36} AND x=1 {END}", new { Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000") }));
    [Fact] public void AND_003_DentroAspas_NaoQuebra() => Assert.Contains("x=1", R("{IF:Status == 'a and b' and Busca != ''} AND x=1 {END}", new { Status = "a and b", Busca = "x" }));

    // ───────────────── Extra 7 para fechar 73→80 (473 total) ─────────────────
    [Fact] public void C1_010_Nested_IF_Missing_END_Throws() {
        var ex = Assert.Throws<SqlTemplateParseException>(() => SqlTemplate.Parse("SELECT * FROM t {IF:Name} AND x=1 {IF:Age} AND y=1 {END}"));
        Assert.Contains("{END}", ex.Expected ?? ex.Message); Assert.True(ex.Line > 0);
    }
    [Fact] public void C2_009_Preco_Null_Omitido_SemThrow() {
        Assert.DoesNotContain("x=1", R("{IF:Preco > 100} AND x=1 {END}", new { Preco = (int?)null }));
        Assert.Null(Record.Exception(() => R("{IF:Preco > 100} AND x=1 {END}", new { Preco = (int?)null })));
    }
    [Fact] public void C3_011_DateTimeOffset_Gt_True() => Assert.Contains("x=1", R("{IF:Data > '2024-01-01'} AND x=1 {END}", new { Data = new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero) }));
    [Fact] public void C5_007_IN_CharArray_Render_NaoString() => Assert.Contains("Role", R("{IN:Roles} WHERE Role IN @Roles {END}", new { Roles = new[] { 'a', 'b' } }));
    [Fact] public void C8_009_Valid_Underscore_Hidden_NaoThrow() {
        var ex = Record.Exception(() => SqlBatch.BuildBatchInsert("_hidden", new[] { new { Id = 1 } }));
        Assert.Null(ex); var (sql, _) = SqlBatch.BuildBatchInsert("_hidden", new[] { new { Id = 1 } }); Assert.Contains("_hidden", sql);
    }
    [Fact] public void C9_008_Length_Code_Int_3_True() => Assert.Contains("x=1", R("{IF:length(Code) == 3} AND x=1 {END}", new { Code = 123 }));
    [Fact] public void AND_004_Exclamation_Isolada_False() => Assert.DoesNotContain("x=1", R("{IF:!Flag} AND x=1 {END}", new { Flag = true }));
}
