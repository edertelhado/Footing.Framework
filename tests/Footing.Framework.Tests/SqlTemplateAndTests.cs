using Footing.Framework.Data;

namespace Footing.Framework.Tests;

/// <summary>
/// US-TEMPLATE-AND-001 — and simples Should Have 2.3 (0,5 SP)
/// Só and, 2 condições, sem or/()/!, com length(), split fora de ''/"", short-circuit, case-insensitive, mantém 7 regex.
/// </summary>
public class SqlTemplateAndTests
{
    private static string Render(string sql, object? p) => SqlTemplate.Parse(sql).Render(p).Sql;

    // 1) busca != null and busca != '' — padrão dominante 27/71
    [Fact]
    public void And_Busca_NotNullAndNotEmpty()
    {
        var sql = "SELECT * FROM t {WHERE} {IF:Busca != null and Busca != ''} AND nome=@Busca {END} {ENDWHERE}";
        Assert.Contains("nome=@Busca", Render(sql, new { Busca = "eder" }));
        Assert.DoesNotContain("nome=@Busca", Render(sql, new { Busca = "" }));
        Assert.DoesNotContain("nome=@Busca", Render(sql, new { Busca = (string?)null }));
        Assert.DoesNotContain("WHERE", Render(sql, new { Busca = (string?)null }));
        Assert.DoesNotContain("WHERE", Render(sql, new { Busca = "" }));
        // dict missing => null
        Assert.DoesNotContain("WHERE", Render(sql, new { }));
    }

    // 2) status != null and status != '' — 18/71, case-insensitive AND
    [Fact]
    public void And_Status_NotNullAndNotEmpty_CaseInsensitive()
    {
        var sql = "SELECT * FROM t {IF:Status != null AND Status != ''} AND status=@Status {END}";
        Assert.Contains("status=@Status", Render(sql, new { Status = "ATIVO" }));
        Assert.DoesNotContain("status=@Status", Render(sql, new { Status = "" }));
        Assert.DoesNotContain("status=@Status", Render(sql, new { Status = (string?)null }));

        var sqlLower = "SELECT * FROM t {IF:Status != null and Status != ''} AND status=@Status {END}";
        Assert.Contains("status=@Status", Render(sqlLower, new { Status = "ATIVO" }));
        var sqlMixed = "SELECT * FROM t {IF:Status != null AnD Status != ''} AND status=@Status {END}";
        Assert.Contains("status=@Status", Render(sqlMixed, new { Status = "ATIVO" }));
    }

    // 3) length(Name) >2 and Name != '' — branch length() + and
    [Fact]
    public void And_Length_Gt2_And_NotEmpty()
    {
        var sql = "SELECT * FROM t {IF:length(Name) > 2 and Name != ''} AND x=1 {END}";
        Assert.Contains("AND x=1", Render(sql, new { Name = "abc" })); // 3>2 and !='' => true
        Assert.DoesNotContain("AND x=1", Render(sql, new { Name = "ab" })); // 2>2 false
        Assert.DoesNotContain("AND x=1", Render(sql, new { Name = "" })); // length 0>2 false + !='' false
        Assert.DoesNotContain("AND x=1", Render(sql, new { Name = (string?)null })); // 0>2 false

        var sql2 = "SELECT * FROM t {IF:length(Name) >2 and Name != ''} AND x=1 {END}";
        Assert.Contains("AND x=1", Render(sql2, new { Name = "eder" }));
    }

    // 4) Age >18 and Age <65 — range numérico 20/10/70
    [Fact]
    public void And_Age_Range_18_65()
    {
        var sql = "SELECT * FROM t {IF:Age > 18 and Age < 65} AND x=1 {END}";
        Assert.Contains("AND x=1", Render(sql, new { Age = 20 }));
        Assert.Contains("AND x=1", Render(sql, new { Age = 30 }));
        Assert.DoesNotContain("AND x=1", Render(sql, new { Age = 10 })); // >18 false
        Assert.DoesNotContain("AND x=1", Render(sql, new { Age = 70 })); // <65 false
        Assert.DoesNotContain("AND x=1", Render(sql, new { Age = 18 })); // >18 false (boundary)
        Assert.DoesNotContain("AND x=1", Render(sql, new { Age = 65 })); // <65 false
        Assert.DoesNotContain("AND x=1", Render(sql, new { Age = (int?)null }));
    }

    // 5) and case-insensitive + short-circuit (left false não avalia right)
    [Fact]
    public void And_CaseInsensitive_AND_ShortCircuit()
    {
        var sql = "SELECT * FROM t {IF:Name != null AND Name != ''} AND x=1 {END}";
        Assert.Contains("AND x=1", Render(sql, new { Name = "eder" }));
        // left false => short-circuit, right não avaliado (mesmo que right fosse inválido, não throw)
        Assert.DoesNotContain("AND x=1", Render(sql, new { Name = (string?)null }));
    }

    // 6) or não suportado → false+Warning (mantém Won't)
    [Fact]
    public void Or_NaoSuportado_RetornaFalse()
    {
        var sql = "SELECT * FROM t {IF:Name == 'eder' or Name == 'joao'} AND x=1 {END}";
        Assert.DoesNotContain("AND x=1", Render(sql, new { Name = "eder" }));
        Assert.DoesNotContain("AND x=1", Render(sql, new { Name = "joao" }));
        Assert.DoesNotContain("AND x=1", Render(sql, new { Name = "other" }));

        var sqlUpper = "SELECT * FROM t {IF:Name == 'eder' OR Name == 'joao'} AND x=1 {END}";
        Assert.DoesNotContain("AND x=1", Render(sqlUpper, new { Name = "eder" }));

        // garante não throw
        Assert.Null(Record.Exception(() => Render(sql, new { Name = "eder" })));
    }

    // 7) and com 3 condições não suportado → false (só 1 and, 2 fatores)
    [Fact]
    public void And_TresCondicoes_NaoSuportado_RetornaFalse()
    {
        var sql = "SELECT * FROM t {IF:A != null and B != null and C != null} AND x=1 {END}";
        Assert.DoesNotContain("AND x=1", Render(sql, new { A = "a", B = "b", C = "c" }));
        Assert.DoesNotContain("AND x=1", Render(sql, new { A = "a", B = "b", C = (string?)null }));
        Assert.Null(Record.Exception(() => Render(sql, new { A = "a", B = "b", C = "c" })));
    }

    // 8) and dentro de '' não quebra split ("a and b" literal)
    [Fact]
    public void And_DentroDeAspas_NaoQuebraSplit()
    {
        var sql = "SELECT * FROM t {IF:Status == 'a and b' and Busca != ''} AND x=1 {END}";
        Assert.Contains("AND x=1", Render(sql, new { Status = "a and b", Busca = "x" }));
        Assert.DoesNotContain("AND x=1", Render(sql, new { Status = "a and b", Busca = "" }));
        Assert.DoesNotContain("AND x=1", Render(sql, new { Status = "other", Busca = "x" }));
        // sem aspas duplas também
        var sql2 = "SELECT * FROM t {IF:Status == \"a and b\" and Busca != ''} AND x=1 {END}";
        Assert.Contains("AND x=1", Render(sql2, new { Status = "a and b", Busca = "x" }));
    }

    // 9) WHEN com and (RenderChooseBlocks usa EvaluateCondition)
    [Fact]
    public void And_In_When_Choose()
    {
        var sql = "{CHOOSE} {WHEN:Status != null and Status != ''} AND Status=@Status {ENDWHEN} {OTHERWISE} AND Status IS NULL {ENDOTHERWISE} {ENDCHOOSE}";
        Assert.Contains("Status=@Status", Render(sql, new { Status = "ATIVO" }));
        Assert.Contains("IS NULL", Render(sql, new { Status = "" }));
        Assert.Contains("IS NULL", Render(sql, new { Status = (string?)null }));
    }

    // 10) parênteses não suportado → false
    [Fact]
    public void Parenteses_NaoSuportado_RetornaFalse()
    {
        var sql = "SELECT * FROM t {IF:(Name != null) and (Name != '')} AND x=1 {END}";
        Assert.DoesNotContain("AND x=1", Render(sql, new { Name = "eder" }));
        Assert.Null(Record.Exception(() => Render(sql, new { Name = "eder" })));
    }

    // 11) ! isolado não suportado → false
    [Fact]
    public void Exclamacao_Isolada_NaoSuportada_RetornaFalse()
    {
        var sql = "SELECT * FROM t {IF:!Flag} AND x=1 {END}";
        Assert.DoesNotContain("AND x=1", Render(sql, new { Flag = true }));
        // != deve continuar funcionando (não é ! isolado)
        var sql2 = "SELECT * FROM t {IF:Flag != null and Flag != ''} AND x=1 {END}";
        Assert.Contains("AND x=1", Render(sql2, new { Flag = "x" }));
    }
}
