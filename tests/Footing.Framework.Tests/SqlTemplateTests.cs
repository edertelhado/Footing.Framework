using System.Collections;
using System.Collections.Concurrent;
using System.Dynamic;
using Footing.Framework.Data;

namespace Footing.Framework.Tests;

public class SqlTemplateTests
{
    private static string Render(string sql, object? p) => SqlTemplate.Parse(sql).Render(p).Sql;

    // 1. IF null/ausente/"" não renderiza vs "john" renderiza
    [Fact]
    public void IF_null_nao_renderiza()
    {
        var sql = "SELECT * FROM Users {IF:Name}WHERE Name=@Name{END}";
        Assert.DoesNotContain("WHERE", Render(sql, new { Name = (string?)null }));
        Assert.DoesNotContain("WHERE", Render(sql, new { }));
        Assert.DoesNotContain("WHERE", Render(sql, new { Name = "" }));
    }

    [Fact]
    public void IF_john_renderiza()
    {
        var sql = "SELECT * FROM Users {IF:Name}WHERE Name=@Name{END}";
        Assert.Contains("WHERE", Render(sql, new { Name = "john" }));
        // case-insensitive key
        Assert.Contains("WHERE", Render(sql, new Dictionary<string, object?> { ["name"] = "john" }));
    }

    // 2. IF != null (v3 — substitui IFNOTNULL) null não vs "" renderiza
    [Fact]
    public void IFNOTNULL_null_nao_vs_vazio_renderiza()
    {
        var sql = "SELECT * FROM Users {IF:Name != null}WHERE Name=@Name{END}";
        Assert.DoesNotContain("WHERE", Render(sql, new { Name = (string?)null }));
        Assert.DoesNotContain("WHERE", Render(sql, new { }));
        Assert.Contains("WHERE", Render(sql, new { Name = "" }));
        Assert.Contains("WHERE", Render(sql, new { Name = "abc" }));
    }

    // 3. IN string "admin" não vs string[] sim vs int[0] não vs List<object>{null,null} não
    [Fact]
    public void IN_string_admin_nao_renderiza()
    {
        var sql = "SELECT * FROM Users {IN:Roles}WHERE Role IN @Roles{END}";
        Assert.DoesNotContain("WHERE", Render(sql, new { Roles = "admin" }));
    }

    [Fact]
    public void IN_string_array_admin_renderiza()
    {
        var sql = "SELECT * FROM Users {IN:Roles}WHERE Role IN @Roles{END}";
        Assert.Contains("WHERE", Render(sql, new { Roles = new[] { "admin" } }));
        Assert.Contains("WHERE", Render(sql, new { Roles = new List<string> { "admin", "user" } }));
    }

    [Fact]
    public void IN_int0_nao_e_list_nulls_nao()
    {
        var sql = "SELECT * FROM Users {IN:Roles}WHERE Role IN @Roles{END}";
        Assert.DoesNotContain("WHERE", Render(sql, new { Roles = Array.Empty<int>() }));
        Assert.DoesNotContain("WHERE", Render(sql, new { Roles = new List<object> { null!, null! } }));
        Assert.DoesNotContain("WHERE", Render(sql, new { Roles = new int[0] }));
    }

    // 4. IF != '' e shorthand (v3 — substitui IFNOTEMPTY)
    [Fact]
    public void IFNOTEMPTY_vazio_nao_vs_abc_sim_vs_colecao()
    {
        var sql = "SELECT * FROM Users {IF:Name != ''}WHERE Name=@Name{END}";
        Assert.DoesNotContain("WHERE", Render(sql, new { Name = "" }));
        Assert.Contains("WHERE", Render(sql, new { Name = "abc" }));
        // coleção vazia não, com item sim — via IN em v3 (IFNOTEMPTY para coleção removido, usar IN)
        var sql2 = "SELECT * FROM Users {IN:Roles}WHERE x {END}";
        Assert.DoesNotContain("WHERE", Render(sql2, new { Roles = new int[0] }));
        Assert.Contains("WHERE", Render(sql2, new { Roles = new[] { 1 } }));
        Assert.Contains("WHERE", Render(sql2, new { Roles = new List<int> { 1, 2 } }));
        // shorthand também cobre ""/null
        var sql3 = "SELECT * FROM Users {IF:Name}WHERE Name=@Name{END}";
        Assert.DoesNotContain("WHERE", Render(sql3, new { Name = "" }));
        Assert.Contains("WHERE", Render(sql3, new { Name = "abc" }));
    }

    // 5. WHERE todos IFs falhando some vs 1 IF ok sem AND líder
    [Fact]
    public void WHERE_todos_IFs_falhando_some()
    {
        var sql = "SELECT * FROM Users {WHERE} {IF:Name} AND Name=@Name {END} {IF:Email} AND Email=@Email {END} {ENDWHERE}";
        var result = Render(sql, new { Name = (string?)null, Email = (string?)null });
        Assert.DoesNotContain("WHERE", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WHERE_um_IF_ok_sem_AND_lider()
    {
        var sql = "SELECT * FROM Users {WHERE} {IF:Name} AND Name=@Name {END} {IF:Email} AND Email=@Email {END} {ENDWHERE}";
        var result = Render(sql, new { Name = "john", Email = (string?)null });
        Assert.Contains("WHERE", result);
        // normalize removes leading AND
        Assert.DoesNotContain("WHERE AND", result);
        Assert.Contains("WHERE Name", result);
    }

    [Fact]
    public void WHERE_normaliza_AND_OR_lider_com_multiplos()
    {
        var sql = "SELECT * FROM Users {WHERE} {IF:A} AND A=@A {END} {IF:B} OR B=@B {END} {ENDWHERE}";
        var result = Render(sql, new { A = "x", B = "y" });
        Assert.Contains("WHERE", result);
        // should be WHERE A=@A AND B=@B (OR stripped, joined with AND)
        Assert.DoesNotContain("WHERE AND", result);
        Assert.DoesNotContain("WHERE OR", result);
    }

    // 6. Dictionary<string,object> com {IF:Name} renderiza
    [Fact]
    public void Dictionary_string_object_renderiza()
    {
        var sql = "SELECT * FROM Users {IF:Name}WHERE Name=@Name{END}";
        var dict = new Dictionary<string, object> { ["Name"] = "john" };
        Assert.Contains("WHERE", Render(sql, dict));
        // case-insensitive
        var dict2 = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["name"] = "john" };
        // pass as Dictionary<string,object> -> ToDictionary path d2
        Assert.Contains("WHERE", Render(sql, dict2));
        // null dict value -> not render
        var dict3 = new Dictionary<string, object?> { ["Name"] = null };
        Assert.DoesNotContain("WHERE", Render(sql, dict3));
    }

    [Fact]
    public void Dictionary_string_object_nullable_renderiza()
    {
        var sql = "SELECT * FROM Users {IF:Name}WHERE Name=@Name{END}";
        var dict = new Dictionary<string, object?> { ["Name"] = "john" };
        Assert.Contains("WHERE", Render(sql, dict));
    }

    // 7. ExpandoObject renderiza
    [Fact]
    public void ExpandoObject_renderiza()
    {
        var sql = "SELECT * FROM Users {IF:Name}WHERE Name=@Name{END}";
        dynamic expando = new ExpandoObject();
        ((IDictionary<string, object>)expando)["Name"] = "john";
        Assert.Contains("WHERE", Render(sql, (object)expando));

        dynamic expando2 = new ExpandoObject();
        ((IDictionary<string, object>)expando2)["Name"] = null!;
        // null -> not render (Expando stores object, null maps)
        // Note: Expando as IDictionary<string,object> will have null value
        // Our ToDictionary d2 copies value null, IF checks null -> no render
        Assert.DoesNotContain("WHERE", Render(sql, (object)expando2));
    }

    // 8. Hashtable / IDictionary não-genérico + cache thread-safe
    [Fact]
    public void Hashtable_nao_generico_renderiza()
    {
        var sql = "SELECT * FROM Users {IF:Name}WHERE Name=@Name{END}";
        var ht = new Hashtable { ["Name"] = "john" };
        Assert.Contains("WHERE", Render(sql, ht));
        var ht2 = new Hashtable { ["Name"] = null };
        Assert.DoesNotContain("WHERE", Render(sql, ht2));
        // non-string key ignored
        var ht3 = new Hashtable { [123] = "john", ["Name"] = "john" };
        Assert.Contains("WHERE", Render(sql, ht3));
    }

    [Fact]
    public void ToDictionary_null_retorna_vazio()
    {
        var sql = "SELECT * FROM Users {IF:Name}WHERE Name=@Name{END}";
        Assert.DoesNotContain("WHERE", Render(sql, null));
    }

    [Fact]
    public void Cache_thread_safe_10_threads_sem_race()
    {
        var sql = "SELECT * FROM Users {IF:Name}WHERE Name=@Name{END}";
        var errors = new ConcurrentBag<Exception>();
        Parallel.For(0, 100, i =>
        {
            try
            {
                var obj = new { Name = "user" + i, Age = i };
                var dict = SqlTemplate.Parse(sql).Render(obj);
                if (!dict.Sql.Contains("WHERE")) throw new Exception("should render");
                // also test with different types
                var dict2 = SqlTemplate.Parse(sql).Render(new { Name = "x" });
                if (!dict2.Sql.Contains("WHERE")) throw new Exception("should render 2");
            }
            catch (Exception ex) { errors.Add(ex); }
        });
        Assert.Empty(errors);
    }

    [Fact]
    public void Cache_reflection_uma_vez_por_type_via_concurrent()
    {
        // Indirect test: Parallel.For many times same type must not throw and must render correctly
        var sql = "SELECT * FROM Users {IF:Name}WHERE Name=@Name{END}";
        Parallel.For(0, 50, _ =>
        {
            var r = Render(sql, new { Name = "john" });
            Assert.Contains("WHERE", r);
        });
        // Verify PropsCache shared exists via ReflectionCache / PropsCacheHelper (via reflection to allow internal)
        var asm = typeof(SqlTemplate).Assembly;
        var rcType = asm.GetType("Footing.Framework.Data.ReflectionCache");
        var phType = asm.GetType("Footing.Framework.Data.PropsCacheHelper");
        System.Reflection.FieldInfo? field = null;
        if (rcType != null) field = rcType.GetField("PropsCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        if (field == null && phType != null) field = phType.GetField("Cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        if (field == null && rcType != null) field = rcType.GetField("PropsCache", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        // fallback: any ConcurrentDictionary field in ReflectionCache
        if (field == null && rcType != null)
        {
            field = rcType.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                .FirstOrDefault(f => f.FieldType.Name.Contains("ConcurrentDictionary"));
        }
        Assert.NotNull(field);
        Assert.True(field!.FieldType.Name.Contains("ConcurrentDictionary"));
        // Verify shared: cache instance exists
        var cache = field.GetValue(null);
        Assert.NotNull(cache);
    }

    [Fact]
    public void StringComparer_OrdinalIgnoreCase()
    {
        var sql = "SELECT * FROM Users {IF:Name}WHERE Name=@Name{END}";
        var dictLower = new Dictionary<string, object?> { ["name"] = "john" };
        Assert.Contains("WHERE", Render(sql, dictLower));
        var dictUpper = new Dictionary<string, object?> { ["NAME"] = "john" };
        Assert.Contains("WHERE", Render(sql, dictUpper));
    }
}
