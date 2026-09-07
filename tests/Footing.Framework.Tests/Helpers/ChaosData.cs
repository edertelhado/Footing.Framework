using System.Collections;
using System.Dynamic;

namespace Footing.Framework.Tests.Helpers;

/// <summary>
/// ChaosData — MemberData determinístico para US-TEST-RESISTENCIA-CHAOS (C1-C9).
/// Determinístico, sem random, seed fixo. Cada yield é copiar-colar para Theory.
/// Cobre 9 categorias: C1 malformado, C2 param errado, C3 tipos, C4 enums, C5 coleções, C6 dinâmicos, C7 injeção, C8 batch, C9 edge.
/// TWE compatível, 0 dependência extra.
/// </summary>
public enum ChaosStatus
{
    Active = 1,
    Inactive = 2
}

public static class ChaosData
{
    // C1 — Template malformado (esqueceu END) — FAILFAST S1-S6 throw SqlTemplateParseException
    public static IEnumerable<object[]> C1_Malformado => new List<object[]>
    {
        // S1 sem {END} — throw Missing {END} for {IF:Name}
        new object[] { "SELECT * FROM Users {IF:Name} WHERE Name=@Name", new { Name = "eder" }, "{IF:Name}" },
        // S2 sem {ENDWHERE}
        new object[] { "SELECT * FROM Users {WHERE} {IF:Name} AND Name=@Name {END}", new { Name = "eder" }, "{WHERE}" },
        // S3 sem {ENDCHOOSE}
        new object[] { "{CHOOSE} {WHEN:Status == 'ACTIVE'} AND Status=@Status {ENDWHEN} {OTHERWISE} AND x=1 {ENDOTHERWISE}", new { Status = "ACTIVE" }, "{CHOOSE}" },
        // S4 sem {ENDWHEN}
        new object[] { "{CHOOSE} {WHEN:Status == 'ACTIVE'} AND x=1 {OTHERWISE} AND y=1 {ENDOTHERWISE} {ENDCHOOSE}", new { Status = "ACTIVE" }, "{WHEN:Status" },
        // S5 chaves desbalanceadas {IF:Name sem } — Unclosed tag
        new object[] { "SELECT * FROM t {IF:Name AND x=1 {END}", new { Name = "a" }, "{IF:Name" },
        // S6 sem {END} para BETWEEN
        new object[] { "SELECT * FROM t {BETWEEN:Price} AND Price BETWEEN @PriceMin AND @PriceMax", new { PriceMin = 10, PriceMax = 20 }, "{BETWEEN:Price}" },
    };

    // C2 — Parâmetro errado: typo, case, missing vs null vs "", string "abc" >100
    public static IEnumerable<object[]> C2_TypoCaseMix => new List<object[]>
    {
        // typo Nmae → omitido
        new object[] { "{IF:Name == 'eder'} AND Name=@Name {END}", new { Nmae = "eder" }, false },
        // missing vs null vs "" shorthand
        new object[] { "{IF:Name} AND Name=@Name {END}", new { Name = (string?)null }, false },
        new object[] { "{IF:Name} AND Name=@Name {END}", new { }, false },
        new object[] { "{IF:Name} AND Name=@Name {END}", new { Name = "" }, false },
        // != null com "" → true (armadilha: vaza "" != null)
        new object[] { "{IF:Name != null} AND Name=@Name {END}", new { Name = "" }, true },
        // case-insensitive lower → true
        new object[] { "{IF:Name} AND Name=@Name {END}", new Dictionary<string, object?> { ["name"] = "john" }, true },
        new object[] { "{IF:Name} AND Name=@Name {END}", new Dictionary<string, object?> { ["NAME"] = "john" }, true },
        // string "abc" >100 → false+Warning (hotfix C)
        new object[] { "{IF:Preco > 100} AND x=1 {END}", new { Preco = "abc" }, false },
        new object[] { "{IF:Preco > 100} AND x=1 {END}", new { Preco = "150" }, true },
        new object[] { "{IF:Preco > 100} AND x=1 {END}", new { Preco = 150 }, true },
        new object[] { "{IF:Preco > 100} AND x=1 {END}", new { Preco = 50 }, false },
        new object[] { "{IF:Preco > 100} AND x=1 {END}", new { Preco = (int?)null }, false },
    };

    // C3 — Tipos diferentes: int/long/decimal/double/bool/Guid/DateTime/DateTimeOffset/char/null
    public static IEnumerable<object[]> C3_TiposCross => new List<object[]>
    {
        new object[] { "{IF:Preco > 100} AND x=1 {END}", new { Preco = 150 }, true },
        new object[] { "{IF:Preco > 100} AND x=1 {END}", new { Preco = 150L }, true },
        new object[] { "{IF:Preco > 100} AND x=1 {END}", new { Preco = 150.5m }, true },
        new object[] { "{IF:Preco > 100} AND x=1 {END}", new { Preco = 150.5d }, true },
        new object[] { "{IF:Preco > 100} AND x=1 {END}", new { Preco = "150.5" }, true },
        // bool
        new object[] { "{IF:Flag == true} AND x=1 {END}", new { Flag = true }, true },
        new object[] { "{IF:Flag == true} AND x=1 {END}", new { Flag = false }, false },
        new object[] { "{IF:Flag} AND x=1 {END}", new { Flag = false }, true }, // shorthand false != null → true (RN)
        new object[] { "{IF:Flag} AND x=1 {END}", new { Flag = (bool?)null }, false },
        new object[] { "{IF:Flag} AND x=1 {END}", new { Flag = (bool?)false }, true },
        // Guid vs string literal quoted ordinal
        new object[] { "{IF:Id == '550e8400-e29b-41d4-a716-446655440000'} AND x=1 {END}", new { Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000") }, true },
        new object[] { "{IF:Id == '550e8400-e29b-41d4-a716-446655440001'} AND x=1 {END}", new { Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000") }, false },
        // char == via ToString ordinal true; > com char vs string → IsOrderingComparable false → false (documenta armadilha)
        new object[] { "{IF:Letra == 'A'} AND x=1 {END}", new { Letra = 'A' }, true },
        new object[] { "{IF:Letra > 'B'} AND x=1 {END}", new { Letra = 'A' }, false },
        new object[] { "{IF:Letra > 'A'} AND x=1 {END}", new { Letra = 'B' }, false }, // char vs string incompatível → false (use string Letra="B" para true)
        // DateTime vs string
        new object[] { "{IF:Data > '2024-01-01'} AND x=1 {END}", new { Data = new DateTime(2024, 12, 31) }, true },
        new object[] { "{IF:Data > '2024-12-31'} AND x=1 {END}", new { Data = new DateTime(2024, 1, 1) }, false },
        new object[] { "{IF:Data > '2024-01-01'} AND x=1 {END}", new { Data = "2024-12-31" }, true },
        // DateTimeOffset
        new object[] { "{IF:Data > '2024-01-01'} AND x=1 {END}", new { Data = new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero) }, true },
        // null shorthand vs == null
        new object[] { "{IF:Preco == null} AND x=1 {END}", new { Preco = (int?)null }, true },
        new object[] { "{IF:Preco == null} AND x=1 {END}", new { Preco = 0 }, false },
    };

    // C4 — Enums: case-sensitive, int vs enum, @Param same type
    public static IEnumerable<object[]> C4_Enum => new List<object[]>
    {
        // enum ToString case-sensitive
        new object[] { "{IF:Status == 'Active'} AND x=1 {END}", new { Status = ChaosStatus.Active }, true },
        new object[] { "{IF:Status == 'ACTIVE'} AND x=1 {END}", new { Status = ChaosStatus.Active }, false },
        new object[] { "{IF:Status == 'Inactive'} AND x=1 {END}", new { Status = ChaosStatus.Inactive }, true },
        // enum vs int literal 1 → fallback ordinal "Active" vs "1" → false (armadilha)
        new object[] { "{IF:Status == 1} AND x=1 {END}", new { Status = ChaosStatus.Active }, false },
        // enum via @Param same enum → true
        new object[] { "{IF:Status == @Outro} AND x=1 {END}", new { Status = ChaosStatus.Active, Outro = ChaosStatus.Active }, true },
        // enum vs string "Active" via @Param → fallback ordinal true
        new object[] { "{IF:Status == @Outro} AND x=1 {END}", new { Status = ChaosStatus.Active, Outro = "Active" }, true },
        // enum vs string "ACTIVE" case-sensitive via @Param → false
        new object[] { "{IF:Status == @Outro} AND x=1 {END}", new { Status = ChaosStatus.Active, Outro = "ACTIVE" }, false },
        // Dict int 0 vs enum? missing typing → false
        new object[] { "{IF:Status == 'Active'} AND x=1 {END}", new Dictionary<string, object?> { ["Status"] = 0 }, false },
    };

    // C5 — Coleções IN: string "admin", vazio, nulls, etc
    public static IEnumerable<object[]> C5_InColecoes => new List<object[]>
    {
        new object[] { "{IN:Roles} WHERE Role IN @Roles {END}", "admin", false }, // string → omitido (hack: pass as object with Roles="admin" via wrapper)
        new object[] { "{IN:Roles} WHERE Role IN @Roles {END}", new { Roles = "admin" }, false },
        new object[] { "{IN:Roles} WHERE Role IN @Roles {END}", new { Roles = Array.Empty<int>() }, false },
        new object[] { "{IN:Roles} WHERE Role IN @Roles {END}", new { Roles = new List<object> { null!, null! } }, false },
        new object[] { "{IN:Roles} WHERE Role IN @Roles {END}", new { Roles = new[] { 1, 2 } }, true },
        new object[] { "{IN:Roles} WHERE Role IN @Roles {END}", new { Roles = new List<int> { 1 } }, true },
        // 1 válido + 1 null → ainda true (hasItems true)
        new object[] { "{IN:Roles} WHERE Role IN @Roles {END}", new { Roles = new List<string?> { "a", null } }, true },
        // string char trap: List<char> com 'a','d','m','i','n' seria IEnumerable mas val já é string check antes → omitido? Se passar char[] não é string → renderizaria (array char[] não é string) → true. Documentar.
        new object[] { "{IN:Roles} WHERE Role IN @Roles {END}", new { Roles = new[] { 'a', 'b' } }, true },
    };

    // C7 — Injeção (valor com chaves/SQL) — testado via Fact, mas MemberData para placeholder
    public static IEnumerable<object[]> C7_Injecao => new List<object[]>
    {
        new object[] { "SELECT * FROM Users {WHERE} {IF:Name} AND Name=@Name {END} {ENDWHERE}", new { Name = "'; DROP TABLE Users; --" }, true },
        new object[] { "SELECT * FROM Users {WHERE} {IF:Name} AND Name=@Name {END} {ENDWHERE}", new { Name = "' OR '1'='1" }, true },
        new object[] { "{IF:Name} AND Name=@Name {END}", new { Name = "{{END}}" }, true },
        new object[] { "{IF:Name} AND Name=@Name {END}", new { Name = "{{IF:Name}}" }, true },
        new object[] { "SELECT '{{teste}}' FROM t {IF:Name} AND x=1 {END}", new { Name = "a" }, true }, // literal {{teste}} permanece, x=1 renderiza
    };

    // C8 — SqlBatch chaos: tableName invalid (should throw), batchSize invalid, columnsOverride typo, etc
    public static IEnumerable<object[]> C8_InvalidTableNames => new List<object[]>
    {
        new object[] { "Users; DROP TABLE Users; --" },
        new object[] { "Users; DROP" },
        new object[] { "bad; DROP" },
        new object[] { "Users--" },
        new object[] { " Users" }, // leading space
        new object[] { "1Users" }, // starts with digit
        new object[] { "Users; SELECT" },
    };

    public static IEnumerable<object[]> C8_ValidTableNames => new List<object[]>
    {
        new object[] { "Users" },
        new object[] { "schema.Users" },
        new object[] { "Users_123" },
        new object[] { "my_schema.my_table" },
        new object[] { "_hidden" },
    };

    public static IEnumerable<object[]> C8_BatchSizeInvalid => new List<object[]>
    {
        new object[] { 0 },
        new object[] { -1 },
        new object[] { -500 },
    };

    public static IEnumerable<object[]> C8_ColumnsOverride => new List<object[]>
    {
        // typo total → 0 cols → noop ""
        new object[] { "TYPO_INEXISTENTE", 0, false },
        // typo parcial → 1 col
        new object[] { "FirstName,TYPO", 1, true },
        new object[] { "firstname,lastname", 2, true }, // case-insensitive
    };

    // C9 — length/WHERE/CHOOSE edge + loader
    public static IEnumerable<object[]> C9_LengthWhereChoose => new List<object[]>
    {
        new object[] { "{IF:length(Name) == 0} AND x=1 {END}", new { Name = (string?)null }, true },
        new object[] { "{IF:length(Name) == 0} AND x=1 {END}", new { Name = "" }, true },
        new object[] { "{IF:length(Name) == 0} AND x=1 {END}", new { Name = "a" }, false },
        new object[] { "{IF:length(Code) == 3} AND x=1 {END}", new { Code = 123 }, true },
        new object[] { "{IF:length(Code) == 3} AND x=1 {END}", new { Code = 12 }, false },
        // WHERE vazio → some (não contém WHERE)
        new object[] { "SELECT * FROM t {WHERE} {IF:Name} AND Name=@Name {END} {ENDWHERE}", new { Name = (string?)null }, false },
        // CHOOSE sem OTHERWISE e nenhum WHEN true → ""
        new object[] { "{CHOOSE} {WHEN:Status=='A'} AND x=1 {ENDWHEN} {ENDCHOOSE}", new { Status = "B" }, false },
        new object[] { "{CHOOSE} {WHEN:Status=='A'} AND x=1 {ENDWHEN} {ENDCHOOSE}", new { Status = "A" }, true },
        // Guid length 36
        new object[] { "{IF:length(Id) == 36} AND x=1 {END}", new { Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000") }, true },
    };

    // Infra fuzz determinístico: 200 combos compactos — para garantir Render nunca throw (C geral)
    public static IEnumerable<object[]> FuzzDeterministico => GenerateFuzz();

    private static IEnumerable<object[]> GenerateFuzz()
    {
        // Seed fixo: combina templates malformados + typos + tipos + INs
        var templates = new[]
        {
            "{IF:Name} AND Name=@Name {END}",
            "{IF:Preco > 100} AND x=1 {END}",
            "{IN:Roles} WHERE Role IN @Roles {END}",
            "{IF:length(Name) == 0} AND x=1 {END}",
            "SELECT * FROM Users {WHERE} {IF:Name} AND Name=@Name {END} {ENDWHERE}",
            "{CHOOSE} {WHEN:Status=='A'} AND x=1 {ENDWHEN} {OTHERWISE} AND y=1 {ENDOTHERWISE} {ENDCHOOSE}",
            "SELECT '{{teste}}' FROM t {IF:Name} AND x=1 {END}",
        };
        var paramCases = new object?[]
        {
            new { Name = "eder" },
            new { Name = (string?)null },
            new { },
            new { Nmae = "eder" },
            new { Preco = "abc" },
            new { Preco = 150 },
            new { Preco = "150" },
            new { Roles = "admin" },
            new { Roles = Array.Empty<int>() },
            new { Roles = new[] { 1, 2 } },
            new { Status = ChaosStatus.Active },
            new { Status = "ACTIVE" },
            new { Name = "'; DROP TABLE Users; --" },
            new { Name = "{{END}}" },
            null,
        };
        int count = 0;
        foreach (var tpl in templates)
        {
            foreach (var p in paramCases)
            {
                if (count++ >= 200) yield break;
                yield return new object[] { tpl, p! };
            }
        }
    }
}
