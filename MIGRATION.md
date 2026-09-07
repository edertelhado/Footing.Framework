# Guia de Migração — Footing.Framework

> **Versão:** `0.1.0-alpha` → `3.5.1` patch sem or (3.5.0 477t → 3.5.1 477t Fixed i18n + Benchmark formal, or Won't definitivo 0% 0/208 vs and 34.1% Must DONE)  
> **Data:** 2026-09-07  
> **Status:** `3.5.1` patch 0.35 SP sem or — `3.5.0` aa37af8 477t 11 regex 78K 2897 LOC → `3.5.1` 477t 11 regex 78K 2897 LOC <3.5K TWE true, tag `v3.5.1` pendente auditoria @aud_carlos ≥9.0, `GLOBAL_RULES 60L` agnóstico, 0 Npgsql/src  
> **Histórico:** `0.2.0-alpha` removeu `SqlBuilder`; `0.3.0-alpha` removeu `ColumnAttribute` + `TypeHandler`/`DependencyWalk` deprecated; `1.0.0` stable; `1.1.0-alpha` descartado bug `EQ*`; `2.0.0-alpha` breaking clean v3 (5 tags, EvaluateCondition 80L, 68 testes); `2.0.0-alpha.1` hotfix C `false+Warning` (71 testes); `2.0.0` stable promoção sem código; `2.1.0` minor Could Have fechado (Sprint 05 3.5 SP); `2.2.0-alpha` chaos 3.5 SP 303 testes fail-safe C1 literal (Sprint 06 3.5 SP Must); `2.2.0` stable promoção docs 0.75 SP Must Have; `2.3.0-alpha` and simples 0.5 SP (314 testes, 7 regex); `2.3.0` stable fechamento 3.5 SP restante (4.0 total); `2.3.1` patch docs 0.5 SP (59K+remote); `2.4.0` minor 2.75 SP IFDEFINED+PropsCache+FsCheck+trailing dot; `3.0.0` major breaking Obsolete; `3.1.0` veto-final 1.6 SP SPI; `3.2.0` sem-migrations 1.5 SP; `3.3.0` Migrations Y/N 0.7 SP; `3.4.0` resiliência 0.7 SP 389t sqlite:memory + `3.4.1` 4 DBs 0.5 SP 393t + `3.4.2` GilsonChaos 80 473t stable + `3.5.0` portal Antora 0.3 + UseTransaction Won't + i18n 0.15 477t 2897 LOC + `3.5.1` patch Fixed i18n + Benchmark formal 0.35 SP sem or  

## Sumário

- [1. Remoção do SqlBuilder (Breaking Change)](#1-remoção-do-sqlbuilder-breaking-change)
  - [1.1 Motivação](#11-motivação)
  - [1.2 O que foi removido](#12-o-que-foi-removido)
  - [1.3 Antes vs Depois — Comparativo C#](#13-antes-vs-depois--comparativo-c)
  - [1.4 Template .sql — EmbeddedResource](#14-template-sql--embeddedresource)
  - [1.5 Passos de Migração (< 15 min por query)](#15-passos-de-migração--15-min-por-query)
- [2. ColumnAttribute — Removido](#2-columnattribute--removido)
- [3. TypeHandlerRegistry — Deprecated (corrigido para thread-safe)](#3-typehandlerregistry--deprecated-corrigido-para-thread-safe)
- [4. AddDependencyWalkAuto — Deprecated](#4-adddependencywalkauto--deprecated)
  - [4.1 Nota 0.3.0-alpha → 1.0.0 (Stable)](#41-nota-030-alpha--100-stable--sem-breaking-apenas-fixes-sprint-03)
- [5. Checklist de Migração](#5-checklist-de-migração)
- [6. Referências](#6-referências)
- [7. Nota 1.0.0 → 1.1.0-alpha (sem breaking, OPS + BATCH)](#7-nota-100--110-alpha-sem-breaking-13-sp-ops-5--batch-8)
- [8. Nota 1.1.0-alpha → 2.0.0-alpha → 2.0.0](#8-nota-110-alpha-bug--200-alpha-breaking-clean-us-template-v3-5-sp)
  - [8.1 Nota 2.0.0-alpha → 2.0.0-alpha.1 hotfix](#81-nota-200-alpha--200-alpha1-hotfix-comparador-não-numérico-us-template-v3-comparador-05-sp)
  - [8.2 Nota 2.0.0-alpha.1 → 2.0.0 stable](#82-nota-200-alpha1--200-stable-promoção-sem-código-sprint-04-2-sp-must-have)
  - [8.3 Nota 2.0.0 → 2.1.0 minor additive](#83-nota-200--210-minor-additive-sem-breaking-sprint-05-could-have-35-sp)
  - [8.4 Nota 2.1.0 tri-state null / Y / N traz todos + IS NULL](#84-nota-210-tri-state-null--yn--traz-todos--is-null--docs-05-sp-sem-código-us-template-tri-state-null--backlog-andor-wont-30)
  - [8.5 Nota 2.2.0 stable — promoção docs sem código](#85-nota-220-stable-promoção-docs-sem-código-sprint-06-075-sp-must-have)
  - [8.6 Nota 2.3.0 stable fechamento — and + fail-fast + gap MyBatis viável](#86-nota-230-stable-fechamento--and--fail-fast--gap-mybatis-viável)
  - [8.7 Nota 2.3.1 patch docs sem código](#87-nota-231-patch-docs-sem-código-pack-59k--git-remote)
  - [8.8 Nota 2.4.0 minor — IFDEFINED + PropsCache + FsCheck + ValidateTableName trailing dot](#88-nota-240-minor--ifdefined--propscache--fscheck--validatetablename-trailing-dot)
  - [8.9 Nota 3.0.0 major breaking — Remoção Obsolete (TypeHandlerRegistry + AddDependencyWalkAuto)](#89-nota-300-major-breaking--remoção-obsolete-typehandlerregistry--adddependencywalkauto)
  - [8.10 Nota 3.1.0 minor — Must 3.1.0 4.5 SP additive sem breaking](#810-nota-310-minor--must-310-45-sp-additive-sem-breaking)
  - [8.11 Nota 3.2.0 minor — 1.5 SP sem migrations (DiSmoke 0.25 + Spec 0.5 + Benchmark 0.5 + Docs 0.25)](#811-nota-320-minor--15-sp-sem-migrations-dismoke-025--spec-05--benchmark-05--docs-025)
  - [8.12 Nota 3.3.0 minor — Migrations SPI carteiro Y/N (0.7 SP)](#812-nota-330-minor--migrations-spi-carteiro-yn-07-sp)
  - [8.13 Nota 3.4.2 stable — promoção docs/tag sem código sobre d61ef60 (0.2 SP)](#813-nota-342-stable--promoção-docstag-sem-código-sobre-d61ef60-02-sp)
  - [8.14 Nota 3.5.0 revisado 0.7 SP — UseTransaction Won't definitivo + docs Antora Must (0 LOC prod docs + throw agnóstico)](#814-nota-350-revisado-07-sp--usetransaction-wont-definitivo--docs-antora-must-0-loc-prod-docs--throw-agnóstico)
  - [8.15 Nota 3.5.1 patch — Fixed i18n varredura PT→EN + BenchmarkDotNet formal + and revalidação (0.35 SP sem or)](#815-nota-351-patch--fixed-i18n-varredura-pten--benchmarkdotnet-formal--and-revalidação-035-sp-sem-or)

---

## 1. Remoção do SqlBuilder (Breaking Change)

### 1.1 Motivação

`SqlBuilder` (`src/Footing.Framework/Data/SqlBuilder.cs`) era uma API fluente (`Select/From/Where/If/IfIn/Build`) que **duplicava 100%** da capacidade já existente em `SqlTemplate` + `SqlTemplateLoader` (`{IF}{WHERE}{IN}{CHOOSE}` com arquivos `.sql` como `EmbeddedResource`).

Auditoria de 2026-09-04 comprovou:

| Problema | SqlBuilder | SqlTemplate | Impacto |
|---|---|---|---|
| **B4 — `IF` com `null` renderiza indevidamente** | `IfCondition()` retorna `clause` mesmo quando `value == null` | `RenderIfBlocks()` idem | Filtro `AND Name=@Name` com `null` gera `WHERE Name = NULL` (nunca true) ou parâmetro fantasma |
| **B6 — `string` como `IEnumerable<char>`** | `InCondition()` trata `"abc"` como coleção não-vazia | `RenderInBlocks()` idem | `{IN:roles} AND Role IN @Roles` renderiza com `roles = "admin"` → SQL inválido |
| **B-perf — `ToDictionary` sem cache** | Reflection `GetProperties()` a cada `Build()` | `GetProperties()` a cada `Render()` | Hot path com custo O(n) por request |

> **Decisão PO/Stakeholder (US-SQL-000):** Remover `SqlBuilder` **diretamente em `0.2.0-alpha`**, sem fase `[Obsolete]`. SemVer `0.y.z` permite breaking changes; repo ainda em `alpha` sem consumidores estáveis em produção. Todos os fixes (B4/B6/perf) consolidados **exclusivamente** em `SqlTemplate`.

- ✅ Superfície de bug reduzida pela metade
- ✅ Correção única (não em dois lugares)
- ✅ SQL versionável em `.sql` (review/diff/lint) em vez de strings C#

### 1.2 O que foi removido

| Artefato | Status em `0.2.0-alpha` | Substituto |
|---|---|---|
| `src/Footing.Framework/Data/SqlBuilder.cs` — classe `SqlBuilder` | **Removido** | `SqlTemplate` + `SqlTemplateLoader` |
| Métodos `Select()`, `From()`, `Join()`, `Where()`, `If()`, `IfNotNull()`, `IfIn()`, `IfNotIn()`, `GroupBy()`, `Having()`, `OrderBy()`, `Build()` | **Removidos** | Tags `{IF:param}`, `{IFNOTNULL:param}`, `{IN:param}`, `{WHERE}{ENDWHERE}`, `{IFNOTEMPTY:param}`, `{CHOOSE}` |
| Referências em `Footing.Framework.csproj` (`Description`/`PackageTags` com `sqlbuilder`) | **Removidas** | `Description: Data Layer (SqlTemplate + Dapper)` |
| Seção `### SqlBuilder (Fluent API)` em `README.md` e `src/Footing.Framework/README.md` | **Removida** | Documentação apenas `SqlTemplateLoader`/`SqlTemplate` |

> `SqlResult` (`Sql` + `Parameters`) **permanece** — contrato compatível, apenas a forma de construí-lo muda.

### 1.3 Antes vs Depois — Comparativo C#

#### Antes — `SqlBuilder` (0.1.0-alpha) ❌ Removido

```csharp
using Footing.Framework.Data;
using Dapper;

public class UserRepository(IDbConnectionFactory factory)
{
    public async Task<IEnumerable<User>> SearchAsync(string? name, string? email, int[]? roles)
    {
        var result = SqlBuilder
            .Select("u.Id, u.Name, u.Email, u.CreatedAt")
            .From("Users u")
            .Join("Roles r ON r.Id = u.RoleId")
            .Where("u.Active = 1")
            .If("name", "AND u.Name LIKE @Name")
            .IfNotNull("email", "AND u.Email = @Email")
            .IfIn("roles", "AND u.RoleId IN @Roles")
            .GroupBy("u.Id")
            .OrderBy("u.Name", "u.CreatedAt DESC")
            .Build(new { name, email, roles });

        // result.Sql + result.Parameters (Dapper-ready)
        using var conn = factory.CreateConnection();
        return await conn.QueryAsync<User>(result.Sql, result.Parameters);
    }

    // Outro exemplo: Build com objeto anônimo direto
    public SqlResult BuildInline(object filters)
    {
        return new SqlBuilder()
            .Select("u.Id").From("Users u")
            .Where("1=1")
            .If("status", "AND u.Status = @Status")
            .Build(filters);
    }
}
```

**Problemas do padrão acima:** SQL em string C# (sem syntax highlight/lint), duplicação de lógica condicional, bugs B4/B6 herdados, sem cache de reflection.

#### Depois — `SqlTemplate` + `SqlTemplateLoader` (0.2.0-alpha) ✅ Recomendado

**Opção A — Arquivo `.sql` como `EmbeddedResource` (recomendado para queries reutilizáveis)**

```csharp
using Footing.Framework.Data;
using Dapper;

public class UserRepository(IDbConnectionFactory factory)
{
    public async Task<IEnumerable<User>> SearchAsync(string? name, string? email, int[]? roles)
    {
        // Carrega por convenção: procura resource que termina com "Sql.Users.Search.sql"
        // Assembly = typeof(UserRepository).Assembly
        var template = SqlTemplateLoader.For<UserRepository>("Sql.Users.Search");
        var result = template.Render(new { name, email, roles });

        using var conn = factory.CreateConnection();
        return await conn.QueryAsync<User>(result.Sql, result.Parameters);
    }
}
```

**Opção B — `SqlTemplate.Parse` inline (para queries ad-hoc/testes)**

```csharp
var template = SqlTemplate.Parse(@"
    SELECT u.Id, u.Name, u.Email, u.CreatedAt
    FROM Users u
    JOIN Roles r ON r.Id = u.RoleId
    {WHERE}
        u.Active = 1
        {IF:name} AND u.Name LIKE @Name {END}
        {IFNOTNULL:email} AND u.Email = @Email {END}
        {IN:roles} AND u.RoleId IN @Roles {END}
    {ENDWHERE}
    GROUP BY u.Id
    ORDER BY u.Name, u.CreatedAt DESC
");

var result = template.Render(new { name = "%john%", email = "a@b.com", roles = new[] { 1, 2, 3 } });
await conn.QueryAsync<User>(result.Sql, result.Parameters);
```

**Equivalente 1:1 da API fluente:**

| SqlBuilder | SqlTemplate |
|---|---|
| `.Select("u.Id, u.Name").From("Users u")` | `SELECT u.Id, u.Name FROM Users u` (SQL literal no `.sql`) |
| `.Where("u.Active = 1")` | `u.Active = 1` dentro de `{WHERE}...{ENDWHERE}` |
| `.If("name", "AND u.Name LIKE @Name")` | `{IF:name} AND u.Name LIKE @Name {END}` |
| `.IfNotNull("email", "AND u.Email=@Email")` | `{IFNOTNULL:email} AND u.Email=@Email {END}` |
| `.IfIn("roles", "AND u.RoleId IN @Roles")` | `{IN:roles} AND u.RoleId IN @Roles {END}` |
| `.IfNotIn("roles", "AND u.RoleId NOT IN @Roles")` | `{IN:roles} AND u.RoleId NOT IN @Roles {END}` *(mesma tag, negação no SQL)* |
| `.GroupBy("u.Id")` | `GROUP BY u.Id` literal |
| `.OrderBy("u.Name")` | `ORDER BY u.Name` literal |
| `.Build(obj)` | `.Render(obj)` → `SqlResult` idêntico |

### 1.4 Template .sql — EmbeddedResource

Crie o arquivo físico e marque como `EmbeddedResource`:

**Estrutura sugerida:**

```
src/MyApp.Data/
  Sql/
    Users/
      Search.sql
      GetById.sql
  MyApp.Data.csproj
```

**`Sql/Users/Search.sql`:**

```sql
SELECT u.Id, u.Name, u.Email, u.CreatedAt
FROM Users u
JOIN Roles r ON r.Id = u.RoleId
{WHERE}
    u.Active = 1
    {IF:name} AND u.Name LIKE @Name {END}
    {IFNOTNULL:email} AND u.Email = @Email {END}
    {IFNOTEMPTY:role} AND r.Name = @Role {END}
    {IN:roles} AND u.RoleId IN @Roles {END}
    {CHOOSE}
        {WHEN:status} AND u.Status = @Status {ENDWHEN}
        {OTHERWISE} AND u.Status = 'ACTIVE' {ENDOTHERWISE}
    {ENDCHOOSE}
{ENDWHERE}
GROUP BY u.Id
ORDER BY u.Name, u.CreatedAt DESC
```

**Tags suportadas em `0.2.0-alpha` (com fixes B4/B6):**

| Tag | Renderiza quando |
|---|---|
| `{IF:param} ... {END}` | `param` existe **e** `!= null` **e** não é `string.Empty` |
| `{IFNOTNULL:param} ... {END}` | `param` existe **e** `!= null` (string vazia **renderiza**) |
| `{IFNOTEMPTY:param} ... {END}` | `string` não-vazia **ou** `IEnumerable` (não-string) não-vazia **ou** scalar não-nulo |
| `{IN:param} ... {END}` | `IEnumerable` (não-`string`) com ≥1 item não-nulo |
| `{WHERE} ... {ENDWHERE}` | Só adiciona `WHERE` se conteúdo interno renderizar; remove `AND`/`OR` líder |
| `{CHOOSE}{WHEN:param}...{ENDWHEN}{OTHERWISE}...{ENDOTHERWISE}{ENDCHOOSE}` | Primeiro `WHEN` truthy, senão `OTHERWISE` |

> **Fix B6 em 0.2.0:** `string` **nunca** é tratada como `IEnumerable` em `{IN}`/`{IFNOTEMPTY}`. `Roles = "admin"` não renderiza `IN` — exige `new[] {"admin"}`.

**Marcar como EmbeddedResource (`MyApp.Data.csproj`):**

```xml
<ItemGroup>
  <EmbeddedResource Include="Sql\**\*.sql" />
</ItemGroup>
```

**Uso do Loader:**

```csharp
// Por convenção (recomendado) — busca resource terminado em "Sql.Users.Search.sql"
var t1 = SqlTemplateLoader.For<UserRepository>("Sql.Users.Search");
var t2 = SqlTemplateLoader.ForAssembly(typeof(UserRepository).Assembly, "Sql.Users.Search");

// Por nome completo (quando há ambiguidade)
var t3 = SqlTemplateLoader.Load("MyApp.Data.Sql.Users.Search.sql", typeof(UserRepository).Assembly);

// Cache em memória (ConcurrentDictionary) — invalidação opcional
SqlTemplateLoader.Invalidate("MyApp.Data.Sql.Users.Search.sql");
SqlTemplateLoader.ClearCache();

// Carregar todos os .sql do assembly
var all = SqlTemplateLoader.LoadAll(typeof(UserRepository).Assembly);
```

### 1.5 Passos de Migração — < 15 min por query

> **Tempo estimado:** 5–15 min por query `SqlBuilder` → `.sql` + `Loader`. Batch de 10 queries ≈ 1–2h.

**Passo 1 — Localize usos (1 min):**

```bash
grep -rn "SqlBuilder" --include="*.cs" src/
grep -rn "new SqlBuilder\|SqlBuilder.Select" --include="*.cs" .
```

**Passo 2 — Extraia o SQL para `.sql` (5 min):**

1. Crie `Sql/<Domínio>/<Query>.sql` no projeto que contém o Repository.
2. Copie `SELECT/FROM/JOIN/GROUP BY/ORDER BY` literais; envolva filtros em `{WHERE}`.
3. Converta cada `.If("p","AND ...")` → `{IF:p} AND ... {END}` (snake do nome: `paramName` case-insensitive).
4. Converta `.IfIn("roles","AND x IN @Roles")` → `{IN:roles} AND x IN @Roles {END}`.
5. Marque o arquivo como `EmbeddedResource` no `.csproj` (ver acima).

**Passo 3 — Troque a chamada C# (2 min):**

```diff
- var result = SqlBuilder.Select("u.Id, u.Name").From("Users u")
-     .Where("u.Active=1").If("name","AND u.Name LIKE @Name").Build(new { name });
+ var result = SqlTemplateLoader.For<UserRepository>("Sql.Users.Search").Render(new { name });
  await conn.QueryAsync<User>(result.Sql, result.Parameters);
```

**Passo 4 — Ajuste parâmetros `null`/`string` (2 min) — aproveite fix B4/B6:**

- Se antes você contornava `IF null` renderizando, remova workaround — `{IF:param}` agora **não** renderiza com `null`.
- Se passava `string` para `IfIn`, troque para `IEnumerable<string>` — `{IN:roles}` ignora `string` em `0.2.0`.

**Passo 5 — Valide (5 min):**

```bash
dotnet build -c Release
dotnet test  # se houver testes de repo
# Opcional: logue result.Sql em teste para conferir WHERE sem AND líder
```

**Erros comuns:**

| Erro | Causa | Solução |
|---|---|---|
| `FileNotFoundException: Embedded resource 'Sql.Users.Search.sql' not found` | `.sql` não marcado como `EmbeddedResource` ou nome errado | Confira `<EmbeddedResource Include="Sql\**\*.sql" />` e use `For<T>("Sql.Users.Search")` (sem `.sql`) |
| `AmbiguousMatchException: Multiple resources match` | Dois `.sql` com mesmo sufixo em assemblies diferentes | Use `Load("Full.Resource.Name.sql", assembly)` |
| `WHERE AND ...` | Faltou `{WHERE}{ENDWHERE}` | Envolva filtros em `{WHERE} ... {ENDWHERE}` — ele remove `AND`/`OR` líder |

---

## 2. ColumnAttribute — Removido

**Status:** `ColumnAttribute` (`src/Footing.Framework/Data/ColumnAttribute.cs`) — **removido em `0.3.0-alpha`** (US-CLEAN-001).

**Motivo:** Atributo de 8 linhas sem leitura em `UnitOfWork`/`SqlTemplate`/`TypeHandler`; `grep -rn ColumnAttribute src/` retornava zero usos além da definição. Dapper já resolve mapeamento via convenção.

**Antes:**

```csharp
public class User
{
    [Column("user_id")]
    public int Id { get; set; }
    [Column("full_name")]
    public string? Name { get; set; }
}
```

**Depois — use Dapper nativo:**

```csharp
// Opção 1 — Global (recomendado para snake_case → PascalCase)
DefaultTypeMap.MatchNamesWithUnderscores = true;

// Opção 2 — Por tipo
SqlMapper.SetTypeMap(typeof(User), new CustomPropertyTypeMap(typeof(User),
    (type, columnName) => type.GetProperties().FirstOrDefault(p =>
        p.GetCustomAttribute<ColumnAttribute>()?.Name == columnName // ← não use mais
        // substitua por mapeamento explícito:
        string.Equals(p.Name, columnName.Replace("_",""), StringComparison.OrdinalIgnoreCase)
    )!
));

// Opção 3 — Query com alias SQL (mais simples)
var sql = "SELECT user_id AS Id, full_name AS Name FROM users";
```

> `grep -rn ColumnAttribute` deve retornar zero após migração (exceto `CHANGELOG`/`MIGRATION.md`).

---

## 3. TypeHandlerRegistry — Deprecated (corrigido para thread-safe)

**Status:** `TypeHandlerRegistry` (`src/Footing.Framework/Data/TypeHandler.cs:29-50`) — **deprecated em `0.3.0-alpha`**, corrigido para `ConcurrentDictionary<Type, ITypeHandler>` e com aviso `[Obsolete]` (US-CLEAN-001).

**Motivo:** `Dictionary<Type, ITypeHandler>` não-concorrente causava race se `Register` chamado em múltiplas threads no startup; além disso duplicava `SqlMapper.AddTypeHandler` do Dapper.

**Antes:**

```csharp
TypeHandlerRegistry.Register<StringArrayHandler>();
TypeHandlerRegistry.Register(new BoolCharTypeHandler());
SqlMapper.AddTypeHandler(new BoolCharTypeHandler()); // duplicação
```

**Depois (0.3.0-alpha):**

```csharp
// Preferir Dapper nativo (recomendado)
SqlMapper.AddTypeHandler(new BoolCharTypeHandler());
SqlMapper.AddTypeHandler(typeof(MyEnum), new StringEnumTypeHandler());

// Se ainda usar registry, agora é thread-safe, mas será removido em 1.0:
#pragma warning disable CS0618
TypeHandlerRegistry.Register<BoolCharTypeHandler>(); // [Obsolete] — use SqlMapper.AddTypeHandler
#pragma warning restore CS0618
```

**Interfaces `ITypeHandler` / `ITypeHandler<T>` e handlers concretos (`JsonTypeHandler`, `EnumTypeHandler`, `BoolCharTypeHandler`, `StringEnumTypeHandler`) permanecem** — apenas o registry estático será removido em `1.0`.

---

## 4. AddDependencyWalkAuto — Deprecated

**Status:** `DependencyWalkExtension.AddDependencyWalkAuto(rootNamespace)` (`src/Footing.Framework/DI/DependencyWalkExtension.cs:10-17`) — **deprecated em `0.3.0-alpha`** (US-DI-001).

**Motivo:** Filtragem por `Assembly.FullName.StartsWith(rootNamespace)` falha quando `FullName` contém `Version/Culture/PublicKeyToken`; assemblies referenciados ainda não carregados não aparecem em `AppDomain.CurrentDomain.GetAssemblies()`; `DefinedTypes` sem `try/catch` para `ReflectionTypeLoadException`.

**Antes:**

```csharp
services.AddDependencyWalkAuto("MeuApp", configuration);
```

**Depois — use overload explícito (recomendado):**

```csharp
// Opção A — assemblies explícitos (determinístico, sem varredura global)
services.AddDependencyWalk("MeuApp", configuration, typeof(Program).Assembly, typeof(UserRepository).Assembly);

// Opção B — se mantiver Auto, agora filtra por Assembly.GetName().Name == rootNamespace || StartsWith(rootNamespace + ".")
// e trata ReflectionTypeLoadException com log warning
#pragma warning disable CS0618
services.AddDependencyWalkAuto("MeuApp", configuration); // [Obsolete] — prefira AddDependencyWalk com assemblies
#pragma warning restore CS0618
```

---

## 4.1 Nota `0.3.0-alpha → 1.0.0` (Stable — sem breaking, apenas fixes Sprint 03)

**`0.3.0-alpha → 1.0.0` sem breaking change.** Apenas fixes LOW da auditoria Sprint 02 (§4 DoD) + `Version 1.0.0` stable.

| Área | Antes (`0.3.0-alpha`) | Depois (`1.0.0`) | Issue |
|---|---|---|---|
| **Docs — README índice** | `README.md:27` listava `- [ColumnAttribute](#columnattribute)` como seção ativa (drift — atributo já removido) | Linha removida do índice; seção `### ColumnAttribute` mantida com `> Removido em 0.3.0-alpha` | US-DOC-001 |
| **Tests — portabilidade** | `File.ReadAllText("/home/etelhado/...")` hardcode em `EventBusChannelTests.cs:115-118` e `LifecycleDedupTests.cs:194` — falha em CI/outro path | `Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", ...))` relativo — `grep -rn "/home/etelhado" tests/` == 0 | US-TEST-001 |
| **DI — `StartsWith` Ordinal** | `DependencyWalkExtension.cs:15,24` `StartsWith(rootNamespace+".")` sem `StringComparison.Ordinal` (culture-sensitive) | `StartsWith(rootNamespace+".", StringComparison.Ordinal)` + `ResolverExtensions.cs:27` `StartsWith(rootNamespace, Ordinal)` | US-DI-002 |
| **Packaging** | `Version 0.3.0-alpha` (pré-release) | `Version 1.0.0` stable; `CHANGELOG [1.0.0]`, `PUBLISH.md 1.0.0`, `dotnet pack` estável | US-PACK-002 |

> `ColumnAttribute` já removido em `0.3.0-alpha` (ver §2); não há migração adicional para `1.0.0`. `TypeHandlerRegistry` mantém `[Obsolete]` + `ConcurrentDictionary` até adoção zero (remoção em `2.0.0`). Checklist §8 `PackageIcon`/`TreatWarningsAsErrors`/`Benchmark` = Won't Have `1.0.0` (planejado `1.1.0`, ver `CHANGELOG [1.0.0] Notas`).

---

## 5. Checklist de Migração

- [ ] `grep -rn SqlBuilder --include="*.cs"` retorna zero (fora de `MIGRATION.md`/`CHANGELOG.md`)
- [ ] `grep -rn ColumnAttribute --include="*.cs"` retorna zero
- [ ] Todos `.sql` criados em `Sql/` e marcados como `<EmbeddedResource>`
- [ ] Chamadas trocadas para `SqlTemplateLoader.For<T>("Sql....").Render(params)` ou `SqlTemplate.Parse(sql).Render(params)`
- [ ] `dotnet build -c Release` passa
- [ ] `dotnet pack` gera `1.0.0` (stable, antes `0.3.0-alpha`) sem `sqlbuilder` em `Description`/`PackageTags` — ver `CHANGELOG [1.0.0]`
- [ ] Testes de `IF null`, `IN` com `string`, `IN` com coleção vazia validados (ver US-SQL-001)
- [ ] `TypeHandlerRegistry` substituído por `SqlMapper.AddTypeHandler` onde possível
- [ ] `AddDependencyWalkAuto` substituído por `AddDependencyWalk` com assemblies explícitos (se aplicável)

---

## 6. Referências

| Issue | Título | Tipo |
|---|---|---|
| US-SQL-000 | Remover SqlBuilder — manter apenas SqlTemplate | Breaking Change |
| US-SQL-001 | Fix SqlTemplate — IF null, IN/IFNOTEMPTY com string (B4/B6) | Bugfix |
| US-SQL-002 | ToDictionary com cache e suporte IDictionary genérico | Performance |
| US-CLEAN-001 | Remover ColumnAttribute / limpar TypeHandlerRegistry | Breaking + Deprecated |
| US-DI-001 | Revisar AddDependencyWalkAuto — scanning seguro | Deprecated |
| US-EVB-001/002, US-PACK-001 | EventBus / Packaging | Não afetam migração 0.1→0.2 |
| US-DOC-001 | README índice ColumnAttribute removido | Docs fix (Sprint 03) |
| US-TEST-001 | Tests portabilidade AppContext.BaseDirectory | Tests fix |
| US-DI-002 | StartsWith StringComparison.Ordinal | DI fix |
| US-PACK-002 | Version 1.0.0 stable | Packaging stable |

## 7. Nota `1.0.0 → 1.1.0-alpha` (sem breaking, 13 SP: OPS 5 + BATCH 8)

**Status:** `1.0.0 stable` → `1.1.0-alpha` **sem breaking change** — apenas adições (Must Have v1.1.0, `print-04.md` roadmap). `Version 1.1.0-alpha` pre-release, sem tag ainda (auditoria @aud_carlos pendente).

| Área | Antes (`1.0.0`) | Depois (`1.1.0-alpha`) | Issue | Breaking? |
|---|---|---|---|---|
| **Data — `SqlTemplate` operadores** | 6 tags (`IF/IFNOTNULL/IFNOTEMPTY/IN/WHERE/CHOOSE`) | +7 tags bloco `{EQ:Param}`, `{NE:Param}`, `{LT:Param}`, `{GT:Param}`, `{GTE:Param}`/`{GE:Param}`, `{LTE:Param}`/`{LE:Param}`, `{BETWEEN:Param}` com `{END}` — semântica `null`/`""` omitido (igual `{IF}`), `BETWEEN` exige `ParamMin`+`ParamMax` flat (Dapper-friendly) | US-TEMPLATE-OPS | **Não** — regex disjuntas (`\{EQ:` etc.), templates 1.0.0 sem novas tags renderizam idêntico |
| **Data — Batch Insert** | `UnitOfWork` + Dapper, insert batch exigia `DataTable` na unha | Novo `SqlBatch` helper: `BuildBatchInsert<T>(table, items, cols?) → (Sql, DynamicParameters)` + `InsertBatchAsync<T>(conn/uow, table, items, batchSize=500, ct)` chunked `INSERT INTO t (cols) VALUES (@p0_Col,...)` com auto-calc `effective = min(batchSize,2100/colCount)` | US-DATA-BATCH | **Não** — arquivo novo, API additive |
| **Docs** | `README.md` tabela 6 tags | +7 linhas (`EQ/NE/LT/GT/GTE/LTE/BETWEEN`) + seção `Batch Insert` | — | Docs only |
| **Tests** | 29 tests | 53 tests (29 + 13 OPS + 11 BATCH) | — | — |
| **Packaging** | `Version 1.0.0` | `Version 1.1.0-alpha` | — | Pre-release |

**Migração:** nenhuma — templates 1.0.0 continuam válidos. Para usar novos operadores, adicione blocos `{EQ:Param} AND Col=@Param {END}` dentro de `{WHERE}` (ver `README.md` exemplo `Sql/Exemplos/FiltrosComparacao.sql`). Para batch, use `SqlBatch.InsertBatchAsync(uow, "Users", users)` (ver `README.md` Batch Insert).

**Checklist:**

- [ ] `grep -rn "EQ:\|BETWEEN:\|SqlBatch" src/` retorna novos artefatos
- [ ] `dotnet test -c Release` 53 passed
- [ ] `dotnet pack -c Release` gera `1.1.0-alpha.nupkg`

---

## 8. Nota `1.1.0-alpha` bug → `2.0.0-alpha` breaking clean (US-TEMPLATE-v3, 5 SP)

> **Status:** `1.1.0-alpha` (3955fde, 53 testes, pack 51K) **DESCARTADO — bug CRITICAL `EQ*` existência** → `2.0.0-alpha` **breaking clean** (sem compatibilidade) — `Version 2.0.0-alpha` pre-release, breaking livre autorizado stakeholder Sueli (planejamento). Ver `print-07.md` e `issues/US-TEMPLATE-v3-breaking-redesign.md`.

**Breaking Changes:**

| Tipo | Antes (v1.1.0-alpha 3955fde) | Depois v3 (v2.0.0-alpha) | Ação dev |
|------|------------------------------|--------------------------|----------|
| **Templates 1.0.0** (`{IF:Name}`, `{WHERE}`, `{IN}`, `{CHOOSE}`) | Funcionava | **Compat total** — `IfRegex([^}]+)` ainda casa `\w+` | Nenhuma |
| **Templates com `{EQ:Status}` como existência (uso real)** | `{EQ:Status} AND Status=@Status {END}` | **Removido** → usar `{IF:Status} AND Status=@Status {END}` | Find/replace `\{EQ:` → `{IF:` |
| **Templates com `{EQ}` esperando comparação (`Status=='ACTIVE'`)** | Bug: só existência, vazava filtro | **Fix** → `{IF:Status == 'ACTIVE'} AND Status=@Status {END}` | Migrar para expr com literal |
| **`{NE}/{LT}/{GT}/{GTE}/{GE}/{LTE}/{LE}`** | Bug existência | **Removido** → `{IF:Age > 18}`, `{IF:Price <= 100}` etc. | `\{GT:`→`{IF:`, adicionar ` > 100` no expr |
| **`{IFNOTNULL:Name}`** | Existia | **Removido** → `{IF:Name != null}` | Replace |
| **`{IFNOTEMPTY:Name}`** | Existia | **Removido** → `{IF:Name != ''}` ou `{IF:Name}` (shorthand já cobre "" e null) | Replace |
| **`{BETWEEN:Param}`** | `ParamMin`/`ParamMax` | **Mantido** (único sugar especializado) | Nenhuma |
| **`{WHEN:Param}`** | `{WHEN:Status}` existência | **Mantido + expr** → `{WHEN:Status == 'ACTIVE'}` | Opcional |
| **Versão** | `1.1.0-alpha` (bug) | **`2.0.0-alpha` breaking clean** — descartar alpha bugada | `dotnet pack -c Release` gera `2.0.0-alpha` |

**Gramática v3 (5 tags + 1 sugar):**

```
{WHERE} ... {ENDWHERE}
{IF: expr} ... {END}  // expr := Param | Param Operator Operand
{IN: Param} ... {END}
{CHOOSE} {WHEN: expr} ... {ENDWHEN} {OTHERWISE} ... {ENDOTHERWISE} {ENDCHOOSE}
{BETWEEN: Param} ... {END} // flat Min/Max
Operator := ==|!=|=|<>|>|<|>=|<=|gt|lt|gte|lte|ge|le (case-insensitive)
Operand := null|'str'|"str"|número|bool|@Param|bare Param
```

**Migração automática (1 comando):**

```bash
grep -rn "{EQ:\|{NE:\|{LT:\|{GT:\|{GTE:\|{GE:\|{LTE:\|{LE:\|{IFNOTNULL:\|{IFNOTEMPTY:" --include="*.sql" .
# substituir:
sed -i 's/{EQ:/{IF:/g; s/{NE:/{IF:/g; s/{LT:/{IF:/g; s/{GT:/{IF:/g; s/{GTE:/{IF:/g; s/{GE:/{IF:/g; s/{LTE:/{IF:/g; s/{LE:/{IF:/g; s/{IFNOTNULL:/{IF:/g; s/{IFNOTEMPTY:/{IF:/g' Sql/**/*.sql
# depois ajustar manualmente expr: {IF:Status} → {IF:Status == '\''ACTIVE'\''} se precisava literal
```

**Sem deprecated phase:** como ainda em planejamento, não há `MIGRATION` de 2 fases — breaking é imediato. `CHANGELOG [2.0.0-alpha] Breaking: remove {EQ} etc., use {IF:expr}`.

**Checklist:**

- [ ] `grep -rn "{EQ:\|{NE:\|{LT:\|{GT:none` → `grep -rn "EqRegex" src/ ==0`
- [ ] `grep -c "GeneratedRegex" src/Footing.Framework/Data/SqlTemplate.cs ==7`
- [ ] `dotnet test -c Release` 68 passed
- [ ] `dotnet pack -c Release` gera `2.0.0-alpha.nupkg`

---

## 8.1 Nota `2.0.0-alpha` → `2.0.0-alpha.1` hotfix comparador não-numérico (US-TEMPLATE-v3-comparador, 0.5 SP)

> **Status:** `2.0.0-alpha` (e059902, 68 testes, 7 GeneratedRegex, EvaluateCondition 80L) → `2.0.0-alpha.1` **hotfix C (false+Warning)** — corrige `> < >= <=` com não-numérico, sem D size automático. Stakeholder Sueli aprovou C, rejeitou D. Ver `print-08.md` C e `print-09.md` D rejeitado + `issues/US-TEMPLATE-v3-comparador-*.md`.

**O que muda:**

| Tipo | Antes (`2.0.0-alpha` e059902) | Depois (`2.0.0-alpha.1` hotfix C) | Ação dev |
|------|-------------------------------|----------------------------------|----------|
| **`> < >= <=` com `string vs número`** (`"abc" >100`) | `true` via fallback `string.Compare Ordinal` (`a97>149`) — bug silencioso | **`false` + `Debug.WriteLine Warning`** — `IsOrderingComparable` exige ambos `TryToDecimal` ou ambos `TryToDateTime` ou ambos `string` ou mesmo `IComparable` | Nenhuma se não usava `string>número` (era bug). Se usava e esperava `true` lexical, migrar para `== !=` ou converter param para numérico |
| **`> <` com `string vs string`** (`"eder" > "adam"`) | `true` Ordinal (via `IComparable` cultura ou fallback Ordinal) | **`true` Ordinal mantido** — ambos `string` → `string.Compare Ordinal`, log Debug | Nenhuma |
| **`> <` com `data string`** (`"2024-12-31" > "2024-01-01"`) | `true` lexical frágil (`"2024-2-1" > "2024-12-31"` lexical true mas data false) | **`true` cronológico** via `TryToDateTime` (Invariant+CurrentCulture) | Nenhuma — melhora; `DateTime via @Param` já funcionava, agora `string data` também |
| **`DateTime vs string`** (`DateTime(2024,12,31) > '2024-01-01'`) | Imprevisível (`ToString()` ordinal cultura-dependente) | **`true/false` cronológico** via `TryToDateTime` ambos | Nenhuma — melhora |
| **`null >= null`** | `true` (`== \|\| >= \|\| <=`) | **`false`** (só `==` true, alinhado SQL `NULL >= NULL` UNKNOWN) | Se usava `null >= null` esperando `true`, raro — trocar para `==` |
| **`== !=` com não-numérico** | `TryToDecimal → bool → Ordinal` | **Mantido intacto** | Nenhuma |
| **`BETWEEN` flat Min/Max** | `hasMin && hasMax` | **Intacto** | Nenhuma |
| **`SqlBatch` 166L** | Intacto | **Intacto** | Nenhuma |
| **D size automático** (`"abc"(3) >2 → true`, `10 > "abc"(3) → true`) | Não existia (era Ordinal) | **Não implementado** — `grep Length.CompareTo ==0`; C retorna `false`. D-explícito `length(Name) > 18` será `v2.1.0` Could Have se dor medida | Não use `Name > 18` esperando size; use validação C# ou aguarde `length()` explícito |

**Detalhe técnico (print-08 §6 pseudo):**

```csharp
// EvaluateCondition: IsOrderingComparable antes do switch
bool IsOrderingComparable(a,b) => (TryToDecimal(a,&_)&&TryToDecimal(b,&_))
  || (TryToDateTime(a,&_)&&TryToDateTime(b,&_)) || (a is string && b is string)
  || (a!=null&&b!=null&&a.GetType()==b.GetType()&&a is IComparable);
if (op is ">" or "<" or ">=" or "<=" && !IsOrderingComparable(...)) return false+Warning;
// Compare prioriza: TryToDecimal → TryToDateTime → string vs string Ordinal → same type IComparable → fallback Ordinal
bool TryToDateTime(v) => v is DateTime || DateTimeOffset || string TryParse Invariant+CurrentCulture
```

**Breaking:** leve `true→false` só para bug (`"abc" >100` antes `true`, depois `false`). Sem breaking para numérico/data/string-string. Sem `throw` (fail-safe omitido).

**Checklist:**

- [ ] `grep -n "TryToDateTime\|IsOrderingComparable" src/Footing.Framework/Data/SqlTemplate.cs` existe
- [ ] `grep -n "Length.*CompareTo" src/Footing.Framework/Data/SqlTemplate.cs` ==0 (D não implementado)
- [ ] `grep -c "GeneratedRegex" src/Footing.Framework/Data/SqlTemplate.cs` ==7
- [ ] `dotnet test -c Release` 71 passed
- [ ] `Render("{IF:Nome > 100}", new{Nome="abc"})` → `""` (false) não `true`
- [ ] `Render("{IF:Name > 'adam'}", new{Name="eder"})` → contém (string-Ordinal mantido)
- [ ] `Render("{IF:Nome > 2}", new{Nome="abc"})` → `""` (false, prova D rejeitado)
- [ ] `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.2.0.0-alpha.1.nupkg` com `README`+`LICENSE`

### 8.2 Nota `2.0.0-alpha.1` → `2.0.0` stable promoção sem código (SPRINT-04, 2 SP Must Have)

> **Status:** `2.0.0` stable — promoção pura sem código vs `2.0.0-alpha.1` (74d5efa, 71 testes, 7 regex, hotfix C). Stakeholder *"pode seguir com must have"* autorizado. Should/Could (`PackageIcon`/`TreatWarningsAsErrors`/`Benchmark`/`length()`) ficam para `2.1.0`.

| Item | `2.0.0-alpha.1` | `2.0.0` stable | Ação dev |
|------|------------------|----------------|----------|
| `Version` | `2.0.0-alpha.1` | **`2.0.0` stable SemVer sem sufixo** | `grep "<Version>2.0.0</Version>" csproj` |
| Código `SqlTemplate.cs` | 7 regex, `TryToDateTime`, `IsOrderingComparable` | **Idêntico — NÃO TOCAR** | `grep -c GeneratedRegex ==7`, `grep Length.CompareTo ==0` |
| `SqlBatch.cs` | 166L | **Idêntico** | — |
| Tests | 71 passed | **71 passed** (sem novos) | `dotnet test -c Release` |
| Pack | `2.0.0-alpha.1.nupkg` 49K | **`2.0.0.nupkg` ~49K + `.snupkg` com `README`+`LICENSE`** | `dotnet pack -c Release -o /tmp/pack` + `unzip -l` |
| Docs | `CHANGELOG [2.0.0-alpha.1]`, `MIGRATION →1.1.0-alpha` | **`CHANGELOG [2.0.0] - 2026-09-06`, `MIGRATION →2.0.0`, `PUBLISH 2.0.0`, `README` 5 tags sem drift, `print-10.md`** | Ver §8.2 checklist |
| Tag | `v2.0.0-alpha.1` | **`v2.0.0` só após auditor APROVADO** | `git tag v2.0.0` |

**Sem breaking change** — `2.0.0-alpha.1 → 2.0.0` é só `Version` stable para `PackageReference` sem `-alpha`. Consumidores `Academia` migram `Version="2.0.0-alpha.1"` → `Version="2.0.0"` (ver `PUBLISH.md`). `PackageIcon` vazio (`<PackageIcon></PackageIcon>`) mantido sem `NU5048`; `TreatWarningsAsErrors false` mantido (`CS0618`/`xUnit1031` esperados); `length(Name)>18` explícito futuro `2.1.0` Could Have se dor >30% templates.

**Checklist `2.0.0` stable (adicional ao §8.1):**

- [ ] `grep -n "<Version>2.0.0</Version>" src/Footing.Framework/Footing.Framework.csproj` == `2.0.0`
- [ ] `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.2.0.0.nupkg` + `.snupkg` ~49K com `README`+`LICENSE`
- [ ] `CHANGELOG.md [2.0.0] - 2026-09-06` com `Changed Version 2.0.0 stable` + Notas sem breaking
- [ ] `dotnet test -c Release` 71 passed, `dotnet build -c Release` 0 erro

### 8.3 Nota `2.0.0` → `2.1.0` minor additive sem breaking (Sprint 05 Could Have 3.5 SP)

> **Status:** `2.1.0` minor — additive sem breaking, 3.5 SP fechado (Sprint 05). `Version 2.0.0 → 2.1.0` opt-in. Tag `v2.1.0` só após auditoria @aud_carlos ≥9.0.

| Item | `2.0.0` | `2.1.0` minor | Ação dev | Breaking? |
|------|---------|----------------|----------|-----------|
| `Version` | `2.0.0` | **`2.1.0` SemVer minor sem sufixo** | `grep "<Version>2.1.0</Version>" csproj` | Não — additive |
| `SqlTemplate` `length()` | `grep EvaluateLengthParam ==0`, `grep length\( ==0`, `Name>2 false` (C) | **`length(Name) > 18` explícito opt-in** — `EvaluateCondition` branch `length(Param) Operator Operand` → `decimal Length` via `EvaluateLengthParam` (0 null/missing, s.Length string, ToString().Length número), `rightRaw` parse idêntico, `IsOrderingComparable(decimal,rightVal)` antes `> < >= <=`, `AreEqual/Compare` decimal. Mantém 7 `GeneratedRegex`, `C` intacto. Ex: `{IF:length(Name) > 18} AND Name=@Name {END}` (30 chars contém, 4 não), `{IF:length(Name) == 0}` empty, `{IF:length(Name) > @Min}` com param | **Não** — só novidade `{IF:length(...)}`; `{IF:Name > 18}` mantém `false+Warning` |
| `SqlBatch` guard | `ThrowIfNullOrWhiteSpace` só, `Users; DROP` passava | **`ValidateTableName` regex `^[A-Za-z_][A-Za-z0-9_\.]*$` + throw `ArgumentException`** em `BuildBatchInsert`/`InsertBatchAsync` (3 overloads). `Users`/`schema.Users` pass, `Users; DROP`/`a; b` throw | **Não** — fail-fast só para injection inválido (uso legítimo pass) |
| `PackageIcon` | `<PackageIcon></PackageIcon>` vazio, sem `icon.png` | **`icon.png` 128x128 + `<PackageIcon>icon.png</PackageIcon>` + `<None Include="..\..\icon.png" Pack="true" PackagePath="\" />`** — `dotnet pack` 2.1.0.nupkg ~49K com `README`+`LICENSE`+`icon.png` (`unzip -l | grep icon.png`), sem `NU5048` | Não |
| `TreatWarningsAsErrors` | `false`, `CS0618` Obsolete esperado + `SourceLink` 2 warnings | **`true` + `<WarningsNotAsErrors>CS0618</WarningsNotAsErrors>`** — `TypeHandlerRegistry`/`AddDependencyWalkAuto` mantidos `[Obsolete]` até `3.0.0` sem quebrar build; `NoWarn CS1591` mantido; `tests` `NoWarn CS0618;CS8765;xUnit1031;xUnit2009`. `dotnet build -c Release` 2 warnings só SourceLink (externo) | Não |
| Tests | 71 passed | **81 passed** (71 + 3 BATCH-SEC + 7 LEN) | `dotnet test -c Release` |
| Pack | `2.0.0.nupkg` 49K `README+LICENSE` | **`2.1.0.nupkg` ~49K `README+LICENSE+icon.png` + `.snupkg`** | `dotnet pack -c Release -o /tmp/pack` + `unzip -l` |
| Docs | `CHANGELOG [2.0.0]`, `MIGRATION §8.2`, `PUBLISH 2.0.0` | **`CHANGELOG [2.1.0]`, `MIGRATION §8.3`, `PUBLISH 2.1.0`, `README` tabela `length()`**, `print-11.md` | Ver checklist |
| Tag | `v2.0.0` | **`v2.1.0` só após auditor APROVADO** | `git tag v2.1.0 && git push origin v2.1.0` |

**Sem breaking:** `Academia` consome `Version="2.0.0"` → `Version="2.1.0"` sem código; templates `2.0.0` continuam válidos; `length()` é opt-in.

**Checklist `2.1.0` (DoD Sprint 05):**

- [ ] `grep "<Version>2.1.0</Version>" csproj` == `2.1.0`
- [ ] `grep -c "GeneratedRegex" SqlTemplate.cs` ==7, `grep EvaluateLengthParam` existe, `grep ValidateTableName` existe
- [ ] `grep TreatWarningsAsErrors true` e `WarningsNotAsErrors CS0618`
- [ ] `dotnet test -c Release` ≥75 passed (81 atual)
- [ ] `Render("{IF:length(Name) >2}", new{Name="abc"})` contém vs `Render("{IF:Name >2}", same)` não contém
- [ ] `dotnet pack -c Release -o /tmp/pack` gera `2.1.0.nupkg` + `.snupkg` ~49K com `README,LICENSE,icon.png`
- [ ] `CHANGELOG [2.1.0]` + `print-11.md` espelho

### 8.4 Nota `2.1.0` tri-state `null / Y / N` traz todos + `IS NULL` — docs 0.5 SP sem código (US-TEMPLATE-tri-state-null) + backlog `and/or` Won't 3.0

> **Status:** `v2.1.0 (14f5d7d, 81 testes, 7 regex)` já tagueado — **sem código novo**, só docs 0.5 SP. Baseline `v2.1.0` cobre tri-state via `{IF:Ativo}` shorthand (`TryGetValue && !=null && !IsNullOrEmpty`). `print-14.md` versionado como espelho git de `issues/US-TEMPLATE-tri-state-null.md` (que está em `.gitignore`). Ver `README.md § Tri-state`.

**Traz todos vs IS NULL (v2.1.0 hoje):**

| # | Template | `missing` | `null` | `""` | `"Y"` | `"N"` | Uso |
|---|----------|-----------|--------|------|-------|-------|-----|
| **T1 traz-todos** | `{WHERE} {IF:Ativo} AND c.Ativo=@Ativo {END} {ENDWHERE}` | ❌ traz todos | ❌ traz todos | ❌ traz todos (`IsNullOrEmpty`) | ✅ filtra Y | ✅ filtra N | ✅ `<select null/Y/N traz todos>` — stakeholder tri-state |
| **T3 IS NULL** | `{IF:Ativo == null} AND c.Ativo IS NULL {END}` | ✅ IS NULL (BC, `new {} == null true`) | ✅ IS NULL | ❌ | ❌ | ❌ | Buscar nulidade |

> **Shorthand and implícito:** `{IF:Ativo}` já é `and` de `Ativo != null and Ativo != ''` (`TryGetValue && !=null && !IsNullOrEmpty`). `{IF:Ativo != null}` sozinho **vaza** `""` (`"" != null` true → `AND Ativo=''` indevido); `{IF:Ativo != ''}` vaza `null`. Docs `README` alerta que `{IF:Ativo != null}` não é shorthand.

**4 estados Y/N/IS NULL/Todos — 2 params (Opção 0 docs, sem código):**

```sql
SELECT c.Id FROM Clientes c
{WHERE}
  {IF:Ativo} AND c.Ativo = @Ativo {END}
  {IF:BuscarNulos == true} AND c.Ativo IS NULL {END}
{ENDWHERE}
```
`BuscarNulos=false + Ativo null → todos`; `BuscarNulos=true → IS NULL`; `Ativo Y/N → filtra`. Sem `IFDEFINED`.

**Gap documentado (não breaking):** `missing` vs `null` indistinguíveis para `{IF:Ativo == null}` — ambos `true` (BC test `V3_IF_null_notnull` `new {} → IS NULL`). Para “traz todos” com `missing`, usar T1 shorthand, não T3.

| Decisão | SP | Status | Gatilho |
|---------|----|--------|---------|
| **Docs 0.5 SP agora** — Tabela T1/T3 + 4 estados 2 params + nota `and` implícito e `!= null` vaza `""` | 0.5 | ✅ **Feito v2.1.0 docs** | — |
| **`IFDEFINED:Ativo` / `defined(Ativo)`** — `p.ContainsKey` distingue `missing` (false) vs `null` (true), 7→8 regex, opt-in sem breaking | 1 | 🔜 **Could Have 2.2.0** | >30% queries com `IS NULL` + “traz todos” no mesmo param em 2 sprints |
| **`and/or/!/()` em `{IF: expr}`** (`Name != null and Name != ''` em 1 bloco, `or`, `()`, `!`) | 0.5 simples / 1 completo | ⏭️ **Won't 3.0** (backlog, não implementar) | BUY >30% templates pedirem `and/or` (ver `print-13.md` análise completa, `US-GAP-mybatis-viavel.md:45`) |
| `Option<T>` / sentinel `__ALL__` | 0 | Won't framework — pattern app | — |

**Checklist 8.4 (DoD docs tri-state):**

- [ ] `README.md` e `src/Footing.Framework/README.md` § Tri-state com tabela T1/T3 + 4 estados 2 params + nota `and` implícito + `!= null` vaza `""`
- [ ] `CHANGELOG.md [2.1.0] Notas` menciona docs tri-state 0.5 SP sem código + `print-14.md` espelho
- [ ] `print-14.md` commitado (espelho git, `git ls-files | grep print-14.md`)
- [ ] `src/Footing.Framework/Data/SqlTemplate.cs` **não tocado** — `grep -c GeneratedRegex ==7`, `grep EvaluateLengthParam` existe, `grep ValidateTableName` existe
- [ ] `dotnet test -c Release` 81 passed, `dotnet build -c Release` 0 erro (só SourceLink warnings)
- [ ] `grep "<Version>2.1.0</Version>" csproj` == `2.1.0` (sem bump)
- [ ] `and/or` e `IFDEFINED` **não implementados** neste commit — só docs/backlog

### 8.5 Nota `2.2.0-alpha` → `2.2.0` stable promoção docs sem código (SPRINT-06, 0.75 SP Must Have)

> **Status:** `2.2.0` stable — promoção pura docs sem código vs `2.2.0-alpha` (486602f, 303 testes, 7 regex, chaos 3.5 SP fail-safe C1 literal, icon 128x128, TWE true, length() opt-in, ValidateTableName guard). Stakeholder *“pode deixar para 2.3”* autorizou deferir fail-fast 1 SP para 2.3 e focar em terminar 2.2.

| Item | `2.2.0-alpha` | `2.2.0` stable | Ação dev |
|------|----------------|----------------|----------|
| `Version` | `2.2.0-alpha` | **`2.2.0` stable SemVer sem sufixo** | `grep "<Version>2.2.0</Version>" csproj` |
| Código `SqlTemplate.cs` | 7 regex, `EvaluateLengthParam`, `IsOrderingComparable` | **Idêntico — NÃO TOCAR** | `grep -c GeneratedRegex ==7` |
| `SqlBatch.cs` | 176L `ValidateTableName` | **Idêntico** | — |
| Tests | 303 passed | **303 passed** (sem novos) | `dotnet test -c Release` |
| Chaos C1-C9 | fail-safe literal/omit/false+Warning | **Mantido fail-safe** (fail-fast deferido para 2.3 S1-S6) | `print-17.md` §4 tabela S1-S13 |
| Pack | `2.2.0-alpha.nupkg` ~50K | **`2.2.0.nupkg` ~50K + `.snupkg` com `README`+`LICENSE`+`icon.png`** | `dotnet pack -c Release -o /tmp/pack` + `unzip -l` |
| Docs | `CHANGELOG [2.1.0]`, `MIGRATION §8.4` | **`CHANGELOG [2.2.0]`, `MIGRATION §8.5`, `PUBLISH 2.2.0`, `README` 5 tags + length + tri-state sem drift, `print-17.md`** | Ver checklist |
| Tag | `v2.2.0-alpha` | **`v2.2.0` só após auditor APROVADO** | `git tag v2.2.0` |

**Sem breaking change** — `2.2.0-alpha → 2.2.0` é só `Version` stable para `PackageReference` sem `-alpha`. Fail-fast `S1-S6 throw SqlTemplateParseException` replanejado como **Must 2.3.0 1 SP** (ver `print-17.md` §3 backlog 2.3).

**Checklist `2.2.0` stable (adicional ao §8.3/8.4):**
- [ ] `grep -n "<Version>2.2.0</Version>" src/.../csproj` == `2.2.0`
- [ ] `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.2.2.0.nupkg` + `.snupkg` ~50K com `README`+`LICENSE`+`icon.png`
- [ ] `CHANGELOG.md [2.2.0] - 2026-09-06` com `Changed Version 2.2.0 stable` + Notas sem breaking
- [ ] `dotnet test -c Release` 303 passed, `dotnet build -c Release` 0 erro (só SourceLink 2 warnings)

### 8.6 Nota `2.3.0` stable fechamento — and + fail-fast + gap MyBatis viável (SPRINT-07, 4.0 SP)

> **Status:** `2.3.0` stable fechamento 3.5 SP restante + 0.5 and done = 4.0 SP total. `v2.3.0-alpha` (d8fdd4c, 314 testes, 7 regex, and simples 0.5 SP Should, `FindAndIndicesOutsideQuotes`, `ContainsOr/Parens/Exclamation`, `length()` compat, icon 128x128, TWE true) → `v2.3.0` stable (`FAILFAST S1-S6 throw 1 Must` early + `INCLUDE 1 Must` + `TRIM/SET 1 Should` + `BIND 0.5 Should` + docs/pack/tag). Stakeholder “pode seguir” autorizado, DAG `FAILFAST→INCLUDE→TRIM/SET→BIND→DOC→tag`. **Sem breaking fora fail-fast C1** (alpha permitido).

| Item | `2.2.0` | `2.3.0` stable | Ação dev | Breaking? |
|------|---------|----------------|----------|-----------|
| `Version` | `2.2.0` | **`2.3.0` SemVer stable sem sufixo** | `grep "<Version>2.3.0</Version>" csproj` | Não — stable |
| `SqlTemplate` `and` | `grep FindAndIndicesOutsideQuotes ==0` | **`and` simples 0.5 SP** — `{IF:Busca != null and Busca != ''}` 1× `and` 2 fatores case-insensitive fora de `''/""`, short-circuit, `length()` compat, rejeita `or/()/!/a and b and c` com `false+Warning` | Nenhuma se não usava `and` | Não — additive opt-in |
| `SqlTemplate` fail-fast | fail-safe C1 literal `Contains("{IF:")` 6 rows | **`SqlTemplateParseException` S1-S6 throw** — `{IF}→{END}` etc. Stack LIFO, `Unclosed {`, line/col `\n` count, snippet 60, `ValidateTemplate` em `Parse` + `RenderTemplate` guard, bubble `SqlTemplateLoader.Load` | **Breaking alpha** — corrigir templates com `{END}` faltando; 6 rows ChaosData viram `Throws` | **Sim alpha** — permitido pre-release |
| `SqlTemplate` `INCLUDE` | sem | **`{INCLUDE:Fragment}`** `FragmentCache ConcurrentDictionary` + `RegisterFragment` + `[GeneratedRegex(@"\{(?:INCLUDE\|SQL):(\w+)\}")]` depth 5 guard miss literal | Usar `RegisterFragment` para DRY `SELECT cols` | Não |
| `SqlTemplate` `TRIM/SET` | sem, `WHERE` usava `NormalizeWhereContent` direto | **`{TRIM prefix="WHERE" prefixOverrides="AND|OR "}…{ENDTRIM}`** + `{SET}…{ENDSET}` açúcar `TrimContent(prefix:"SET", suffixOverrides:",")` + `WHERE` delega `TrimContent` | `UPDATE T {SET} {IF:Name != null} Name=@Name, {END} UpdatedAt=NOW(), {ENDSET}` sem vírgula trailing | Não |
| `SqlTemplate` `BIND` | sem | **`{BIND:Var, value='%' + Param + '%'}`** `BindRegex` + `EvaluateBindValue` concatena literals e params, injeta `paramDict["Var"]` antes de `IF/WHERE`, valida `Var ^[A-Za-z_]\w*$`, rejeita `;--/*` fail-safe | `LIKE` pattern via `{BIND:LikePattern, value='%' + Name + '%'}` | Não |
| `GeneratedRegex` | 7 | **`7→10`** (INCLUDE+TRIM+BIND; SET delega sem regex extra) | `grep -c GeneratedRegex ==10` | — |
| Tests | 303 passed | **~327 passed** (314→327, mínimo 324) — 11 and + 2 FailFast + 2 Include + 4 TrimSet + 2 Bind + 6 reescritos C1 | `dotnet test -c Release` | — |
| Pack | `2.2.0.nupkg` ~50K | **`2.3.0.nupkg` ~51K (49–55K) + `.snupkg` com `README`+`LICENSE`+`icon.png`** sem `NU5048` (real 59K, corrigido em §8.7) | `dotnet pack -c Release -o /tmp/pack` + `unzip -l` | — |
| Docs | `CHANGELOG [2.2.0]`, `MIGRATION §8.5`, `PUBLISH 2.2.0`, `README` 5 tags | **`CHANGELOG [2.3.0]`, `MIGRATION §8.6`, `PUBLISH 2.3.0`, `README` 9 tags + and/length + and/length + tri-state + fail-fast note, `print-19.md`** | Ver checklist | — |
| Tag | `v2.2.0` | **`v2.3.0` só após auditor APROVADO ≥9.0** | `git tag v2.3.0 && git push origin v2.3.0` dispara `publish.yml` | — |

**Breaking note C1 literal→throw (alpha permitido):**
- Antes `v2.2.0` fail-safe: `SqlTemplate.Parse("SELECT * {IF:Name} WHERE ...")` sem `{END}` → `Render` continha literal `"{IF:Name}"` sem throw → SQL syntax error confuso `incorrect syntax near '{'`.
- Depois `v2.3.0` fail-fast: `Parse` throw `SqlTemplateParseException: Missing {END} for {IF:Name} opened at line 1 col X. Expected '{END}' before EOF. Snippet:"..." Suggestion:add '{END}'.` com `Tag/Expected/Line/Column/Snippet/Suggestion` (`: InvalidOperationException`) — DX MyBatis `BuilderException` compat, `RenderTemplate` guard, `Loader` bubble não guarda Cache cagado. Typo `Nmae`, `"abc">100`, `IN "admin"`, `"' ; DROP"` permanecem fail-safe omitido/false+Warning/placeholder (dados).

**Migração 2.2.0→2.3.0 checklist dev:**
- [ ] `grep "<Version>2.3.0</Version>" csproj` == `2.3.0`
- [ ] Corrigir templates com `Missing {END}/{ENDWHERE}/{ENDCHOOSE}/{ENDWHEN}` faltando — adicionar fechamento; `grep -rn "\{IF:" --include="*.sql"` conferir balanceamento
- [ ] Atualizar `PackageReference Version="2.2.0"` → `Version="2.3.0"` (Academia.Core/Web/Worker)
- [ ] Opcional: usar `INCLUDE` para DRY, `TRIM/SET` para UPDATE sem vírgula, `BIND` para LIKE
- [ ] Opcional: usar `and` simples `{IF:Busca != null and Busca != ''}` — só 1 `and` 2 fatores, case-insensitive, `or/()/!` ainda rejeita com Warning
- [ ] `dotnet test -c Release` ~327 passed, `dotnet pack -c Release -o /tmp/pack` gera `2.3.0.nupkg` 51K

**Checklist `2.3.0` stable (DoD Sprint 07):**
- [ ] `grep -c "GeneratedRegex" SqlTemplate.cs` ==10 (7+INCLUDE+TRIM+BIND; SET delega)
- [ ] `grep -n "ValidateTemplate\|SqlTemplateParseException\|FragmentCache\|IncludeRegex\|TrimRegex\|TrimContent\|BindRegex" SqlTemplate.cs` todos existem
- [ ] `grep -n "EvaluateLengthParam" SqlTemplate.cs` existe + `grep -n "ValidateTableName" SqlBatch.cs` existe
- [ ] `dotnet test -c Release` → ~327 passed (≥324, ≤335)
- [ ] `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.2.3.0.nupkg` + `.snupkg` ~51K com `README+LICENSE+icon.png` sem `NU5048`

### 8.7 Nota 2.3.1 patch docs sem código (SPRINT-08, 0.5 SP Must Have)

> **Status:** `2.3.1` patch docs — sem código vs `2.3.0` (a468c41, 327 testes, 10 regex, pack real 59K). Stakeholder “sim” para 2.3.1+2.4.0, prioriza 2.3.1 0.5 SP rápido.

| Item | `2.3.0` | `2.3.1` patch | Ação dev | Breaking? |
|------|---------|----------------|----------|-----------|
| `Version` | `2.3.0` | **`2.3.1` SemVer patch sem sufixo** | `grep "<Version>2.3.1</Version>" csproj` | Não — docs only |
| Código `SqlTemplate.cs` | 10 regex, `ValidateTemplate` | **Idêntico — NÃO TOCAR** | `grep -c GeneratedRegex ==10` | — |
| Pack | `2.3.0.nupkg 59K` mas docs diziam `51K` | **`2.3.1.nupkg ~59K` + `.snupkg` com `README+LICENSE+icon.png`** sem `NU5048` | `dotnet pack -c Release -o /tmp/pack` + `unzip -l` | — |
| Docs | `CHANGELOG [2.3.0] 51K` | **`CHANGELOG [2.3.1] Fixed 51K→59K` + `MIGRATION §8.7`** | Ver checklist | — |
| Git remote | `git remote -v` vazio | **`git remote add origin https://github.com/etelhado/Footing.Framework` + `git remote -v` + `PUBLISH § git remote`** | `git remote -v` | — |

**Checklist 2.3.1:**
- [ ] `grep "<Version>2.3.1</Version>" csproj` == `2.3.1`
- [ ] `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.2.3.1.nupkg + .snupkg ~59K` com `README+LICENSE+icon.png`
- [ ] `CHANGELOG [2.3.1]` + `MIGRATION §8.7` + `PUBLISH 2.3.1` + `git remote -v` com `origin`
- [ ] `dotnet test -c Release` 327 passed, `grep -c GeneratedRegex ==10` inalterado

### 8.8 Nota 2.4.0 minor — IFDEFINED + PropsCache + FsCheck + ValidateTableName trailing dot (SPRINT-09, 2.75 SP)

> **Status:** `2.4.0` minor 2.75 SP — `2.3.1` 327 testes, 10 regex, pack 59K → `2.4.0` 345 testes, 11 regex, pack ~60K, sem breaking fora trailing dot fail-fast. Stakeholder “sim” repriorizou IFDEFINED Could `>30%` → Must 2.4.0.

| Item | `2.3.1` | `2.4.0` minor | Ação dev | Breaking? |
|------|---------|---------------|----------|-----------|
| `Version` | `2.3.1` | **`2.4.0` SemVer minor sem sufixo** | `grep "<Version>2.4.0</Version>" csproj` | Não — minor additive |
| `SqlTemplate` `IFDEFINED` | sem `IFDEFINED`, 10 regex, `defined()` inexistente | **`{IFDEFINED:Param} … {END}` + `{IFNOTDEFINED:Param}` + `{IF:defined(Param)}` + `defined(Param) and Param=='Y'`** — `ContainsKey` distingue `missing` (false) vs `null`/`""`/`Y` (true), `10→11 GeneratedRegex` (`IF(?:NOT)?DEFINED`), `RenderIfDefinedBlocks` após `WHERE` antes de `IF`, `ValidateTemplate` para `IFDEFINED`/`IFNOTDEFINED`, `defined()` branch em `EvaluateSingle` com `IsDefined` helper, `CHOOSE WHEN defined` compat, `length()` compat, `and` 1× intacto | **Não** — opt-in |
| `SqlTemplate` `PropsCache` | `SqlTemplate.cs:12` + `SqlBatch.cs:17` dois `ConcurrentDictionary` duplicados | **single `ReflectionCache.PropsCache` + `PropsCacheHelper` alias** `internal static ReflectionCache { PropsCache = new(); Get(Type) => PropsCache.GetOrAdd(...) }` em `ReflectionCache.cs`, usado por `SqlTemplate.ToDictionary` e `SqlBatch.GetProperties`, `BindingFlags.Public|Instance CanRead` intacto, thread-safe `Parallel.For` | Não |
| `SqlBatch` `ValidateTableName` | `^[A-Za-z_][A-Za-z0-9_\.]*$` permite `Users.` trailing dot | **`^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$` rejeita `Users.`/`schema..Users`/`.Users`/`123Users`, pass `Users`/`schema.Users`** | **Leve** `Users.` pass→throw (uso legítimo nunca tem trailing dot) |
| `FsCheck` | sem | **`FsCheck.Xunit 2.16.6` 5 properties ×40 =200 random** `Prop_And_ShouldNeverThrow` + `Prop_Where_Trim_ShouldNeverContain_WhereAnd` + `Prop_Batch_ValidateTableName_ShouldFailFast` + `Prop_Include_ShouldNotStackOverflow` + `Prop_Bind_ShouldNotInject` + `Render` nunca throw com `{{`, emoji, `\0`, `<2s` determinístico | Não |
| `GeneratedRegex` | 10 | **`10→11`** (`IFDEFINED` + `IFNOTDEFINED` via mesma regex) | — |
| Tests | 327 passed | **~345 passed** (327+10 IFDEFINED +3 trailing dot +5 FsCheck) — mínimo 335, máximo 350 | `dotnet test -c Release` |
| Pack | `2.3.1.nupkg 59K` | **`2.4.0.nupkg + .snupkg ~60K` com `README+LICENSE+icon.png` sem `NU5048`, `Deterministic true`** | `dotnet pack -c Release -o /tmp/pack` + `unzip -l` |
| Docs | `CHANGELOG [2.3.1]`, `MIGRATION §8.7`, `PUBLISH 2.3.1`, `README 9 tags` | **`CHANGELOG [2.4.0]`, `MIGRATION §8.8`, `PUBLISH 2.4.0`, `README 10 tags` (+IFDEFINED), `print-21.md`** | Ver checklist |

**Semântica IFDEFINED (missing vs null):**

| Template | `new {}` missing | `new { Ativo=(string)null }` | `new { Ativo="" }` | `new { Ativo="Y" }` |
|----------|------------------|------------------------------|--------------------|---------------------|
| `{IF:Ativo} AND x {END}` | ❌ "" (missing→false) | ❌ "" (null→false) | ❌ "" (empty→false) | ✅ `"AND x"` |
| `{IF:Ativo == null} …` | ✅ `IS NULL` (BC missing==null) | ✅ `IS NULL` | ❌ | ❌ |
| **`{IFDEFINED:Ativo} AND IS NULL {END}`** | **❌ "" (missing false)** | **✅ `"AND IS NULL"` (null true)** | **✅ true (empty true)** | **✅ true** |
| `{IF:defined(Ativo)} …` | ❌ false | ✅ true | ✅ true | ✅ true |
| `{IF:defined(Ativo) and Ativo=='Y'}` | ❌ false | ❌ false | ❌ false | ✅ true |
| `{IFNOTDEFINED:Ativo} AND y {END}` | ✅ true | ❌ false | ❌ false | ❌ false |

**Exemplo final docs (traz todos vs IS NULL sem 2 params):**

```sql
SELECT c.Id FROM Clientes c
{WHERE}
  {IF:Ativo} AND c.Ativo = @Ativo {END}          -- Y/N filtra, null/missing/"" traz todos
  {IFDEFINED:Ativo} AND c.Ativo IS NULL {END}    -- só quando Ativo explicit null (missing não dispara)
{ENDWHERE}
-- ou com defined + and:
-- {IF:defined(Ativo) and Ativo == null} AND c.Ativo IS NULL {END}
```

**Breaking note trailing dot (v2.3.1→2.4.0):**

- Antes `v2.3.1` `ValidateTableName` regex `^[A-Za-z_][A-Za-z0-9_\.]*$` permitia `BuildBatchInsert("Users.", items)` → `INSERT INTO Users.` (SQL inválido, falha só no banco).
- Depois `v2.4.0` regex `^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$` → `throw ArgumentException("tableName contains invalid characters")` fail-fast (igual `Users; DROP`).
- Migração: corrigir `tableName` com trailing dot (uso legítimo nunca tem dot final) — se gerava `schema.` dinâmico, remover dot.

**Checklist `2.4.0` minor (DoD Sprint 09):**

- [ ] `grep "<Version>2.4.0</Version>" csproj` == `2.4.0`
- [ ] `grep -c "GeneratedRegex" SqlTemplate.cs` ==11 (10→11)
- [ ] `grep -n "IfDefinedRegex\|IFDEFINED" SqlTemplate.cs` existe + `grep -n "defined" SqlTemplate.cs` branch `defined\s*\(\s*\w+\s*\)`
- [ ] `grep -n "RenderIfDefinedBlocks" SqlTemplate.cs` existe + pipeline após `WHERE` antes de `IF`
- [ ] `grep -n "ValidateTableName" SqlBatch.cs` regex `(\.[A-Za-z_` sem `\.` trailing — `Users.` throw
- [ ] `grep -n "ReflectionCache\|PropsCacheHelper" src/` 1 definição shared (não 2 duplicadas)
- [ ] `dotnet test -c Release` → ~345 passed (≥335), `dotnet build -c Release` 0 erro
- [ ] `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.2.4.0.nupkg + .snupkg ~60K` com `README+LICENSE+icon.png` sem `NU5048`
- [ ] `CHANGELOG [2.4.0]` com `Added IFDEFINED/FsCheck` + `Changed PropsCache/ValidateTableName` + link `compare/v2.3.1...v2.4.0`
- [ ] `MIGRATION §8.8` + header `→2.4.0` + breaking trailing dot note
- [ ] `PUBLISH 2.4.0` + `README 10 tags` (+IFDEFINED)
- [ ] `git tag v2.4.0` só após auditor ≥9.0 + `git push origin v2.4.0`

### 8.9 Nota 3.0.0 major breaking — Remoção Obsolete (TypeHandlerRegistry + AddDependencyWalkAuto) (SPRINT-10, 1.5 SP)

> **Status:** `3.0.0` major breaking 1.5 SP — `2.4.0` 345 testes, 11 regex, pack 60K → `3.0.0` ~343 testes, 11→11 regex, pack ~60K, `TWE true` strict sem `WarningsNotAsErrors CS0618`. Sem feature OGNL/or. Breaking só delete `Obsolete` desde `0.3.0-alpha` (SemVer §8). Tag `v3.0.0` só após auditoria @aud_carlos ≥9.0.

| Item | `2.4.0` | `3.0.0` major | Ação dev | Breaking? |
|------|---------|---------------|----------|-----------|
| `Version` | `2.4.0` | **`3.0.0` SemVer major sem sufixo** | `grep "<Version>3.0.0</Version>" src/Footing.Framework/Footing.Framework.csproj` | **Sim — major delete Obsolete** |
| `Obsolete` | 2 (`TypeHandlerRegistry` + `AddDependencyWalkAuto` em `0.3.0-alpha`) | **`0` — deletados** `grep -rn "\[Obsolete" --include="*.cs" src/Footing.Framework/ ==0` | `grep -rn Obsolete src/` ==0 antes de remover `CS0618` | **Sim — compile error se ainda usa** |
| `TypeHandlerRegistry` | `TypeHandler.cs:29-50` `public static class TypeHandlerRegistry` com `ConcurrentDictionary` + `[Obsolete]` | **deletado** `grep -rn TypeHandlerRegistry --include="*.cs" src/ ==0` | `SqlMapper.AddTypeHandler(new MeuHandler())` Dapper nativo | **Sim** `CS0246 Type not found` |
| `AddDependencyWalkAuto` | `DependencyWalkExtension.cs:10` `AddDependencyWalkAuto(root,config)` com `GetReferencedAssemblies` + `Assembly.Load` + `[Obsolete]` | **deletado** `grep -rn AddDependencyWalkAuto --include="*.cs" src/ ==0` | `services.AddDependencyWalk("Ns", cfg, typeof(Program).Assembly)` com assemblies explícitos | **Sim** `CS0117` |
| `WarningsNotAsErrors` | `<WarningsNotAsErrors>CS0618</WarningsNotAsErrors>` + `TWE true` | **removido** `grep -n WarningsNotAsErrors csproj ==0` + `TWE true` strict | `dotnet build -c Release` 0 erro, só SourceLink 2 warnings externos | Não — qualidade |
| `GeneratedRegex` | 11 | **`11→11` inalterado** (IFDEFINED intacto, sem novo regex) | `grep -c GeneratedRegex SqlTemplate.cs ==11` | — |
| Tests | 345 passed | **~343 passed** (345 -2 Obsolete tests: `TypeHandlerRegistry_ConcurrentDictionary_thread_safe` + `Obsolete_attributes_present` → `Obsolete_attributes_absent` 0) — DoD aceita 343 ou 345 com teste invertido | `dotnet test -c Release` ~343 passed, `grep Obsolete tests ==1 Obsolete_attributes_absent` | Não — docs |
| Pack | `2.4.0.nupkg ~60K` | **`3.0.0.nupkg + .snupkg ~60K` (58–62K) com `README+LICENSE+icon.png` sem `NU5048`, `Deterministic true`** | `dotnet pack -c Release -o /tmp/pack` + `unzip -l *.nupkg | grep icon.png` | — |
| Docs | `CHANGELOG [2.4.0]`, `MIGRATION §8.8`, `PUBLISH 2.4.0`, `README 10 tags` com TypeHandlerRegistry legacy | **`CHANGELOG [3.0.0] Removed BREAKING` + `MIGRATION §8.9` + `PUBLISH 3.0.0` + `README` Type Handlers clean (só `SqlMapper.AddTypeHandler`)** | Ver checklist | — |
| Tag | `v2.4.0` | **`v3.0.0` só após auditor APROVADO ≥9.0** | `git tag v3.0.0 && git push origin v3.0.0` dispara `publish.yml` | — |

**Breaking note 2.4.0 → 3.0.0 — migrar consumers legados (< 5 min):**

- Antes `v2.4.0`:
  ```csharp
  #pragma warning disable CS0618
  TypeHandlerRegistry.Register(new BoolCharTypeHandler());
  TypeHandlerRegistry.Register(new StringEnumTypeHandler());
  services.AddDependencyWalkAuto("MeuApp", configuration);
  #pragma warning restore CS0618
  ```
- Depois `v3.0.0`:
  ```csharp
  // Data — prefer Dapper nativo
  SqlMapper.AddTypeHandler(new BoolCharTypeHandler());
  SqlMapper.AddTypeHandler(typeof(MyEnum), new StringEnumTypeHandler());
  // DI — assemblies explícitos determinísticos
  services.AddDependencyWalk("MeuApp", configuration, typeof(Program).Assembly);
  ```

**Grep migration checklist dev (copy-paste):**
```bash
grep -rn "TypeHandlerRegistry" --include="*.cs" src/  # deve ser 0 (exit 1)
grep -rn "AddDependencyWalkAuto" --include="*.cs" src/  # deve ser 0
grep -rn "\[Obsolete" --include="*.cs" src/Footing.Framework/  # deve ser 0
grep -n "WarningsNotAsErrors" src/Footing.Framework/Footing.Framework.csproj  # deve ser 0 ou sem CS0618
grep -n "TreatWarningsAsErrors.*true" src/Footing.Framework/Footing.Framework.csproj  # true
grep -c "GeneratedRegex" src/Footing.Framework/Data/SqlTemplate.cs  # 11
dotnet build -c Release 2>&1 | tail -5  # 0 erro
dotnet test -c Release 2>&1 | tail -3  # Passed: ~343
dotnet pack -c Release -o /tmp/pack && ls -lh /tmp/pack/Footing.Framework.3.0.0.nupkg  # ~60K
```

**Checklist `3.0.0` major (DoD Sprint 10):**
- [ ] `grep "<Version>3.0.0</Version>" csproj` == `3.0.0`
- [ ] `grep -c GeneratedRegex SqlTemplate.cs` ==11 inalterado
- [ ] `dotnet test -c Release` → ~343 passed (≥340, ≤345), `Obsolete_attributes_absent` verde
- [ ] `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.3.0.0.nupkg + .snupkg ~60K` com `README+LICENSE+icon.png`
- [ ] `CHANGELOG [3.0.0]` + `MIGRATION §8.9` + `PUBLISH 3.0.0` + `README` clean sem drift

### 8.10 Nota 3.1.0 minor — Must 3.1.0 4.5 SP additive sem breaking

> **Status:** `3.1.0` minor Must 4.5 SP — `3.0.0` 343 testes, 11 regex, pack 60K → `3.1.0` 370 testes, 11 regex, pack 70K, 2478 LOC <3.5K tese. Sem breaking additive opt-in. Stakeholder aprovou Must 4.5 SP. Ver `issues/US-GENERAL-framework-vision.md` + `print-23.md`.

| Feature | SP | Arquivo | LOC | Critério |
|---------|----|---------|-----|----------|
| `Result<T>` | 0.5 | `Common/Result.cs` | 7 | `Result.Ok/Fail/Map/Bind` |
| `PagedResult` | 1 | `Data/Pagination.cs` | 5 | `QueryPagedAsync COUNT+LIMIT` |
| `IAuditable` | 0.5 | `Data/Audit.cs` | 5 | `ApplyAudit/SoftDelete` |
| `OTel` | 0.5 | `Diagnostics/FootingActivitySource.cs` | 6 | `ActivitySource` tags |
| `Health` | 0.25 | `Health/FootingHealthCheck.cs` | 5 | `IHealthCheck` |
| `Idempotency` | 0.5 | `Idempotency/Idempotency.cs` | 9 | `IMemoryCache` dedup |
| `Outbox` | 1.25 | `Outbox/Outbox.cs` | 13 | `IHostedService` poll |

**Migração 3.0.0 → 3.1.0:** nenhuma breaking — `Version 3.0.0 → 3.1.0` minor additive. `Academia` consome `Version="3.1.0"` sem código. Novos módulos opt-in via `AddFooting*` extensions.

**Grep checklist:**
```bash
grep -n "<Version>3.1.0</Version>" src/Footing.Framework/Footing.Framework.csproj  # 3.1.0
find src -name "*.cs" ! -path "*/obj/*" | xargs wc -l  # ~2478 <2650
grep -c "GeneratedRegex" src/Footing.Framework/Data/SqlTemplate.cs  # 11
grep -rn "Result<\|PagedResult\|IAuditable\|ActivitySource" --include="*.cs" src/  # existe
dotnet build -c Release 2>&1 | tail -3  # 0 erro TWE true
dotnet test -c Release 2>&1 | tail -3  # Passed: 370
dotnet pack -c Release -o /tmp/pack && ls -lh /tmp/pack/Footing.Framework.3.1.0.nupkg  # ~70K
```

**Checklist 3.1.0 (DoD Must 4.5 SP):**
- [ ] `grep "<Version>3.1.0</Version>"` == `3.1.0`
- [ ] `wc -l src <2650` (`2478` atual) `<3.5K` tese
- [ ] `dotnet test` → `370 Passed` (344→370, +26 Must)
- [ ] `dotnet pack` → `3.1.0.nupkg ~70K` com `README+LICENSE+icon.png` `unzip -l | grep icon.png`
- [ ] `grep -c GeneratedRegex ==11` sem novo regex
- [ ] `CHANGELOG [3.1.0]` + `MIGRATION §8.10` + `PUBLISH 3.1.0` sem breaking

### 8.10.1 Veto Final 1.6 SP — 3.1.0 sem bump (Result/Pagination/Audit Won't + SPI Spring Security)

> **Status:** `Veto Final 1.6 SP` — PO Sueli veto `Result Won't` (recurso linguagem) + `PagedResult Won't` (dialect) + `Idempotency/Outbox SPI agnóstico` (Spring Security pattern, sem `Redis/NPgSQL` grudado). Mantém `OTel 0.5 + Health 0.25 Must`. **Sem bump Version** — mantém `3.1.0`. Ver `issues/US-GENERAL-3.1.0-veto-final.md` + `print-28.md` (espelho versionado). **Base b30a688 → veto-final:** `2478→2449 LOC (−29) <3.5K`, `370→356 testes (−14 Won't)`, `70K→63K pack` (`68K` previsto), `11 regex` mantido, `0 deps DB`.

| # | Código | Antes Must | Veto Final | SP | Justificativa |
|---|--------|------------|------------|----|---------------|
| 1 | US-GEN-RESULT-001 | 0.5 | **Won't definitivo 0** | 0 | Recurso linguagem C# exception idiomático |
| 2 | US-GEN-PAGINATION-001 | 1.0 | **Won't total 0** | 0 | Se precisa dialect não implementar |
| 3 | US-GEN-AUDIT-001 | 0.5 | **Won't/Could 0.1 snippet** | 0 | Helper impõe contrato, snippet docs |
| 4 | US-GEN-OTEL-001 | 0.5 | **Must MANTER** | 0.5 | 6 LOC custo 0, BCL ActivitySource |
| 5 | US-GEN-HEALTH-001 | 0.25 | **Must MANTER** | 0.25 | 5 LOC k8s agnóstico |
| 6 | US-GEN-IDEMPOTENCY-001 | 0.5 | **SPI 0.1 Must** | 0.1 | Só `IIdempotencyStore` 4 LOC, sem `Memory/Redis` grudado |
| 7 | US-GEN-OUTBOX-001 | 1.25 | **SPI 0.5 Must** | 0.5 | `IOutboxStore + OutboxMessage + OutboxProcessor` poll 2s Batch 100, sem `NPgSQL` grudado |
| 8 | US-GEN-DOC-3.1-001 | 0.25 | **Must 0.25** | 0.25 | veto-final + DDL Firebird/dbf |
| | **Total** | **4.5** | **1.6 SP** | **1.6** | **−64% vs 4.5** |

**Arquivos afetados veto-final (sem bump):**
```
src/Footing.Framework/Common/Result.cs                  [DELETE] Won't 7 LOC
src/Footing.Framework/Data/Pagination.cs                [DELETE] Won't 5 LOC
src/Footing.Framework/Data/Audit.cs                     [DELETE] Won't 5 LOC (snippet docs)
src/Footing.Framework/Diagnostics/FootingActivitySource.cs [MANTER] 6 LOC
src/Footing.Framework/Health/FootingHealthCheck.cs      [MANTER] 5 LOC
src/Footing.Framework/Idempotency/Idempotency.cs        [REWORK SPI 0.1] só interface 2 LOC, sem Memory/Redis
src/Footing.Framework/Outbox/Outbox.cs                  [REWORK SPI 0.5] interface+processor 8 LOC, sem InMemory/NPgSQL
src/Footing.Framework/Footing.Framework.csproj          [EDIT] remove Caching.Memory, Version 3.1.0 sem bump
tests/Footing.Framework.Tests/Footing31Tests.cs         [EDIT] 370→356 (−14 Won't)
```

**SPI Spring Security — framework interface, app escolhe DB (exemplos docs, não pack):**
```csharp
// Framework só SPI (0 deps)
public interface IIdempotencyStore{bool TryGet<T>(string k,out T? v);void Set<T>(string k,T v,TimeSpan? ttl=null);Task<T> GetOrCreateAsync<T>(string k,Func<Task<T>> f,TimeSpan? ttl=null,CancellationToken ct=default);}
public sealed record OutboxMessage(Guid Id,string Type,string Payload,DateTime CreatedAt,DateTime? ProcessedAt=null,int Attempts=0,string? LastError=null);
public interface IOutboxStore{Task SaveAsync(OutboxMessage m,IDbTransaction? tx=null,CancellationToken ct=default);Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int b=100,CancellationToken ct=default);Task MarkProcessedAsync(Guid id,CancellationToken ct=default);Task MarkFailedAsync(Guid id,string e,CancellationToken ct=default);}

// App NPgSQL — exemplo docs (não pack)
public class NpgsqlOutboxStore(IDbConnectionFactory f):IOutboxStore{
  public async Task SaveAsync(OutboxMessage m,IDbTransaction? tx=null,CancellationToken ct=default){var c=tx?.Connection??f.CreateConnection();await c.ExecuteAsync("INSERT INTO Outbox VALUES (@Id,@Type,@Payload::jsonb,@CreatedAt)",m,tx);}
  public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int b=100,CancellationToken ct=default){using var c=f.CreateConnection();return (await c.QueryAsync<OutboxMessage>("SELECT * FROM Outbox WHERE ProcessedAt IS NULL ORDER BY CreatedAt LIMIT @b FOR UPDATE SKIP LOCKED",new{b})).AsList();}
  public Task MarkProcessedAsync(Guid id,CancellationToken ct=default){using var c=f.CreateConnection();return c.ExecuteAsync("UPDATE Outbox SET ProcessedAt=NOW() WHERE Id=@id",new{id});}
  public Task MarkFailedAsync(Guid id,string e,CancellationToken ct=default){using var c=f.CreateConnection();return c.ExecuteAsync("UPDATE Outbox SET Attempts=Attempts+1,LastError=@e WHERE Id=@id",new{id,e});}
}
// Firebird: SELECT FIRST @b * FROM OUTBOX WHERE PROCESSED_AT IS NULL
// dbf: OleDb PROVIDER=VFPOLEDB + SELECT TOP 100 * FROM outbox.dbf
// Idempotency Firebird TABLE ou Memory snippet docs — app registra: services.AddSingleton<IOutboxStore,NpgsqlOutboxStore>(); AddHostedService<OutboxProcessor>();
```

**Audit snippet docs (Won't 3.1.1 / Could 3.2 0.1 — não em src):**
```csharp
// docs/snippets/Audit.cs — copy-paste se app quiser (não shipa no pack)
public interface IAuditable{DateTime CreatedAt{get;set;}DateTime UpdatedAt{get;set;}string? CreatedBy{get;set;}string? UpdatedBy{get;set;}}
public interface ISoftDelete{bool IsDeleted{get;set;}DateTime? DeletedAt{get;set;}}
public static class AuditExtensions{public static void ApplyAudit(this IAuditable e,string? u=null,bool n=false){var now=DateTime.UtcNow;e.UpdatedAt=now;e.UpdatedBy=u;if(n){e.CreatedAt=now;e.CreatedBy=u;}}public static void SoftDelete(this ISoftDelete e){e.IsDeleted=true;e.DeletedAt=DateTime.UtcNow;}}
```

**Idempotency Memory snippet docs (desacoplado, não em src):**
```csharp
// docs/snippets/MemoryIdempotencyStore.cs — copy-paste se app quiser single-pod dev (não K8s)
using Microsoft.Extensions.Caching.Memory;
public sealed class MemoryIdempotencyStore(IMemoryCache c):IIdempotencyStore{public MemoryIdempotencyStore():this(new MemoryCache(new MemoryCacheOptions())){}
 public bool TryGet<T>(string k,out T? v){if(c.TryGetValue(k,out var o)&&o is T t){v=t;return true;}v=default;return false;}
 public void Set<T>(string k,T v,TimeSpan? ttl=null){var o=new MemoryCacheEntryOptions();if(ttl.HasValue)o.SetAbsoluteExpiration(ttl.Value);else o.SetSlidingExpiration(TimeSpan.FromHours(1));c.Set(k,v,o);}
 public async Task<T> GetOrCreateAsync<T>(string k,Func<Task<T>> f,TimeSpan? ttl=null,CancellationToken ct=default){if(TryGet<T>(k,out var cur)&&cur!=null)return cur;var r=await f();Set(k,r,ttl);return r;}}
```

**Checklist Veto Final 1.6 SP (DoD):**
- [ ] `grep -rn "class Result\|PagedResult\|IAuditable" src/Footing.Framework --include="*.cs"` ==0
- [ ] `grep -n "interface IIdempotencyStore" Idempotency.cs` existe, `MemoryIdempotencyStore` não em `src`
- [ ] `grep -n "interface IOutboxStore" Outbox.cs` existe, `InMemoryOutboxStore` não em `src`, `OutboxProcessor` existe
- [ ] `grep -rn "Npgsql\|Firebird\|OleDb\|Redis" src/Footing.Framework --include="*.cs" --include="*.csproj"` ==0 (0 deps DB)
- [ ] `grep -n "Version.*3.1.0" csproj` == `3.1.0` sem bump
- [ ] `dotnet test 356 Passed` + `TWE true` + `pack 63K` (70→63K) + `11 regex` + `print-28.md` versionado

---

### 8.11 Nota 3.2.0 minor — 1.5 SP sem migrations (DiSmoke 0.25 + Spec 0.5 + Benchmark 0.5 + Docs 0.25)

> **Status:** `3.2.0` minor sem migrations — additive sem breaking 1.5 SP Must puxado 4.0 (DiSmoke+Spec+Benchmark Must) + Docs, `Version 3.1.0 → 3.2.0` minor, `2449→2480 LOC +31 <3.5K`, `356→364t +8 (DiSmoke 2 + Spec 3 + Benchmark 3)`, `11→11 regex`, `0 deps DB`, `pack 63K→66K`, `Migrations/` não existe (fica Sprint 3.3 solo 0.5-1 SP detalhada — VersionParser V__ + Checksum + Journal/Runner/Provider + DDL PG/Firebird/dbf + drift + Baseline + R__).

| Item | `3.1.0` veto-final | `3.2.0` sem-migrations | Ação dev | Breaking? |
|------|--------------------|------------------------|----------|-----------|
| `Version` | `3.1.0` | **`3.2.0` minor SemVer sem sufixo** | `grep "<Version>3.2.0</Version>" csproj` | Não — additive |
| `DiSmoke` | não existia | **`DiSmokeTests` 2 testes** — `AddDependencyWalk + EventBus + Health` smoke `GetServices.Count==1 + ValidateScopes` agnóstico sem DB, pega assembly esquecido sem SG | Usar `AddDependencyWalk` com assembly explícito | Não |
| `Spec` | não existia | **`ISpecification<T>` 0.5 SP** — `ISpecification<T> + Predicate/And/Or/Not + Where` composable `IsSatisfiedBy`, 0 deps, 3 testes Spec And/Or/Where | `Spec.Where<User>(u=>u.Ativo).And(other)` | Não |
| `Benchmark` | não existia | **`BenchmarkSmokeTests` 3 testes** — `BenchmarkDotNet opcional PrivateAssets + Stopwatch fallback 10k renders <2000ms` agnóstico só `tests/` | `dotnet test --filter BenchmarkSmoke` | Não |
| `Migrations` | não existia | **Won't 3.2 — fica 3.3 solo 0.5-1 SP** — VersionParser V__ + Checksum SHA256 + Journal/Runner/Provider + DDL + drift + Baseline pendente detalhamento | Não criar `Migrations/` nesta sprint | Não |
| `GeneratedRegex` | 11 | **`11→11`** (sem novo regex, Version `Split`, Checksum `SHA256` BCL em 3.3) | `grep -c GeneratedRegex SqlTemplate.cs ==11` | — |
| Tests | 356 passed | **364 passed** (+8) | `dotnet test -c Release` | — |
| Pack | 63K | **66K** + `README+LICENSE+icon.png` | `dotnet pack -c Release -o /tmp/pack` + `unzip -l` | — |
| Docs | `CHANGELOG [3.1.0]`, `MIGRATION §8.10`, `PUBLISH 3.1.0` | **`CHANGELOG [3.2.0]`, `MIGRATION §8.11` sem migrations + roadmap 3.3 solo, `PUBLISH 3.2.0`, `README` 11 tags, `print-30.md` espelho** | Ver checklist | — |
| Tag | `v3.1.0` | **`v3.2.0` só após auditor APROVADO ≥9.0** | `git tag v3.2.0 && git push origin v3.2.0` dispara `publish.yml` | — |

**Migração 3.1.0 → 3.2.0:** nenhuma — additive sem breaking. Para usar Spec, `using Footing.Framework.Specification; var spec = SpecificationExtensions.Where<User>(u=>u.Ativo).And(ageSpec); users.Where(spec.IsSatisfiedBy)`. DiSmoke e Benchmark são só testes (`tests/`). `Migrations/` não existe — não criar `Migrations/` nesta sprint (fica 3.3 solo detalhada ver `print-30.md` + `issues/SPRINT-3.3-migrations-solo.md`).

**Checklist 3.2.0 DoD:**

- [ ] `grep "<Version>3.2.0</Version>" csproj` == `3.2.0`
- [ ] `grep -c "GeneratedRegex" SqlTemplate.cs` ==11
- [ ] `find src -name "*.cs" ! -path "*/obj/*" | xargs wc -l | tail -1` == `~2480 <3500`
- [ ] `dotnet test -c Release` 364 passed, `dotnet build -c Release` 0 erro, `TWE true`
- [ ] `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.3.2.0.nupkg + .snupkg ~66K` com `README+LICENSE+icon.png`
- [ ] `grep -r Npgsql\|Redis\|Firebird\|OleDb\|DbUp\|Flyway src/Footing.Framework --include="*.cs" --include="*.csproj"` ==0
- [ ] `ls src/Footing.Framework/Migrations` == não existe (fica 3.3)
- [ ] `CHANGELOG [3.2.0]` + `MIGRATION §8.11` + `PUBLISH 3.2.0` + `README` 11 tags + `print-30.md` espelho

---

### 8.12 Nota 3.3.0 minor — Migrations SPI carteiro Y/N (0.7 SP)

> **Status:** `3.3.0` minor additive agnóstico Y/N — `3.2.0` 364 testes, 11 regex, pack 66K → `3.3.0` 371 testes, 11→11 regex, pack ~68K, 2877 LOC <3.5K. **Sem breaking fora DDL Y/N agnóstico documentado** (success `CHAR(1) Y/N` desde print-32). Tag `v3.3.0` só após auditoria @aud_carlos ≥9.0.

| Item | `3.2.0` sem-migrations | `3.3.0` Migrations Y/N | Ação dev | Breaking? |
|------|------------------------|------------------------|----------|-----------|
| `Version` | `3.2.0` | **`3.3.0` minor SemVer** | `grep "<Version>3.3.0</Version>" csproj` | Não — additive |
| `DDL __migrations` | não existia | **`docs/examples/Migrations/DDL/__migrations.sql` `CHAR(1) Y/N CHECK (success IN ('Y','N')) + VARCHAR(4000)` 100% agnóstico (PG CHAR(1), MSSQL CHAR(1), FB CHAR(1), dbf C(1))** | `cat docs/examples/Migrations/DDL/__migrations.sql` | **Doc Y/N** `success Y/N` vs `BOOLEAN` (print-32) |
| `TypeHandler` | `BoolCharTypeHandler S/N` | **+ `BoolCharYNTypeHandler Y/N` `bool→'Y'/'N'` `DbType.AnsiStringFixedLength Size=1` `Parse == 'Y'`** | `SqlMapper.AddTypeHandler(new BoolCharYNTypeHandler())` | Não — additive |
| `Migrations SPI` | não existia | **`IMigrationJournal + IMigrationScriptProvider + IMigrationRunner` tripartite async `IAsyncEnumerable<MigrationInfo>` + `IDbConnectionFactory` + `CancellationToken`** | `grep -n "interface IMigration" src/Footing.Framework/Migrations/*` | Não |
| `VersionParser` | não | **`MigrationVersionParser.TryParse` `V1__`, `V1_0_1__` (_→.), `R__`, `R001__`, `001__` baseline — `IndexOf "__"` sem regex (11→11)** | `MigrationVersionParser.TryParse("V1_0_1__fix.sql", out var m)` | Não |
| `Checksum` | não | **SHA256 `Convert.ToHexString(SHA256.HashData(UTF8))` 64 hex drift fail-fast** | `MigrationChecksum.Compute(sql)` | Não |
| `Runner` | não | **`MigrationRunner` carteiro 60 LOC `EnsureHistoryTable → GetFailed bloqueio → OrderBy Version → ExecuteRawSql IDbCommand + Insert Y/N → catch Insert N + throw MigrationException → Repeatables checksum Upsert → Repair DELETE WHERE success='N' + recalc → Validate drift → Baseline → GenerateScript dry-run + placeholders `Dictionary Replace` (`{{key}}`)** | `new MigrationRunner(journal, provider, factory, opts)` | Não |
| `Provider` | não | **`FileSystemMigrationScriptProvider` + `EmbeddedResourceMigrationScriptProvider` `IAsyncEnumerable<MigrationInfo>`** | `new FileSystemMigrationScriptProvider("/Migrations")` | Não |
| `GeneratedRegex` | 11 | **`11→11`** sem novo regex | `grep -c GeneratedRegex SqlTemplate.cs` | — |
| Tests | 364 passed | **371 passed** (+7: VersionParser 4 + Checksum 1 + YN 1 + Runner 1) | `dotnet test -c Release` | — |
| Pack | 66K | **68K** + `README+LICENSE+icon.png` | `dotnet pack -c Release -o /tmp/pack` | — |
| Docs | `CHANGELOG [3.2.0]`, `MIGRATION §8.11`, `PUBLISH 3.2.0` | **`CHANGELOG [3.3.0]` + `MIGRATION §8.12 Y/N` + `PUBLISH 3.3.0` + `README Migrations Y/N` + `print-32.md` Y/N + `__migrations.sql` Y/N** | Ver checklist | — |
| Tag | `v3.2.0` | **`v3.3.0` só após auditor APROVADO ≥9.0** | `git tag v3.3.0 && git push origin v3.3.0` | — |

**DDL Y/N agnóstico (migrations.md §3 revisado print-32):**
```sql
CREATE TABLE __migrations (
    id INTEGER PRIMARY KEY,
    version VARCHAR(50) NOT NULL,
    description VARCHAR(200),
    type VARCHAR(20),
    checksum VARCHAR(64),
    installed_on TIMESTAMP,
    execution_time_ms INTEGER,
    success CHAR(1) NOT NULL CHECK (success IN ('Y','N')),
    error_message VARCHAR(4000)
);
-- success Y/N alinha 1:1 com BoolCharYNTypeHandler (bool ? "Y" : "N", success == "Y")
-- SELECT * WHERE success='Y' (sucesso) / WHERE success='N' (falha pendente bloqueia Migrate) / DELETE WHERE success='N' (Repair)
```

**SPI uso (app escolhe DB, framework é carteiro):**
```csharp
services.AddSingleton<IMigrationJournal, MyJournal>(); // app fornece PG/Firebird/dbf impl via IDbConnectionFactory
services.AddSingleton<IMigrationScriptProvider>(new FileSystemMigrationScriptProvider("Migrations"));
services.AddSingleton<IMigrationRunner, MigrationRunner>();
services.AddSingleton(new MigrationOptions{ Placeholders = new Dictionary<string,string>{ ["schema"]="public"} });
// runner
await runner.MigrateAsync(); // fail-fast se success='N' pendente
await runner.ValidateAsync(); // drift checksum
await runner.RepairAsync();   // DELETE WHERE success='N'
await runner.BaselineAsync("0");
var dry = await runner.GenerateScriptAsync(); // sem executar
```

**Checklist 3.3.0 DoD:**
- [ ] `grep "<Version>3.3.0</Version>" csproj` == `3.3.0`
- [ ] `grep -c "GeneratedRegex" SqlTemplate.cs` ==11
- [ ] `find src -name "*.cs" ! -path "*/obj/*" | xargs wc -l` ~2877 <3500
- [ ] `dotnet test -c Release` 371 passed, `dotnet build -c Release` 0 erro, `TWE true`
- [ ] `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.3.3.0.nupkg ~68K` com `README+LICENSE+icon.png`
- [ ] `grep -r Npgsql\|Firebird\|OleDb\|Redis\|DbUp src/Footing.Framework --include="*.cs" --include="*.csproj"` ==0
- [ ] `grep -rn "CHAR(1).*Y.*N\|success.*'Y'" src/Footing.Framework/Migrations --include="*.cs"` existe Y/N
- [ ] `ls docs/examples/Migrations/DDL/__migrations.sql` existe Y/N agnóstico
- [ ] `ls src/Footing.Framework/Migrations/` 8 arquivos SPI + Runner + Parser + Checksum
- [ ] `CHANGELOG [3.3.0]` + `MIGRATION §8.12` + `PUBLISH 3.3.0` + `README Migrations`

### 8.13 Nota 3.4.2 stable — promoção docs/tag sem código sobre d61ef60 (0.2 SP)

> **Status:** `3.4.2` stable promoção 0.2 SP docs/tag sem código — `3.3.0` 371t 11 regex 68K 2877 LOC → `3.4.2` 473t 11 regex 78K 3188 LOC <3.5K, `Version 3.4.2` stable sem sufixo sobre d61ef60, `TWE true` strict, `GLOBAL_RULES 59L` agnóstico, `0 Npgsql/Firebird/OleDb/Redis` em `src/`, `pack ~78K` com `README+LICENSE+icon.png`. **Sem breaking fora `and` simples já stable em `2.3.0`**. Tag `v3.4.2` só após auditoria @aud_carlos ≥9.0 (não taguear nesta sprint — deixar para auditoria). Ver `print-38.md` roadmap `3.4.2→3.5.0→4.0.0` + `issues/ROADMAP-proximas-versoes.md`.

| Item | `3.3.0` | `3.4.2` stable | Ação dev | Breaking? |
|------|---------|----------------|----------|-----------|
| `Version` | `3.3.0` | **`3.4.2` SemVer stable patch sem sufixo** | `grep "<Version>3.4.2</Version>" src/Footing.Framework/Footing.Framework.csproj` | Não — stable |
| `Migrations resiliência` | 371t Y/N SPI | **`3.4.0` 389t +18 falha>feliz** — `MigrationChecksum` CRLF `Replace("\r\n","\n")` + `.gitattributes *.sql text eol=lf` + `SqliteInMemoryFactory` `Data Source=file:memdb?mode=memory&cache=shared` + 18 testes `M1-M6` (13 Must falha + 5 Should feliz) Half/CRLF/DROP/Repair/Validate/Baseline/GenerateScript | `SqliteInMemoryFactory` + `MigrationChecksum.Compute normalized` | Não |
| `4 DBs fixtures` | — | **`3.4.1` 393t +4 fixtures** — `TestProviderRegistry` 60L 4 DBs `sqlite/pg/mssql/mysql` + `Fixtures/Migration{Sqlite,Pg,Mssql,Mysql}/V001__..R001__.sql` 20 .sql + `Theory` agnóstica | `FOOTING_TEST_PROVIDER=pg dotnet test` | Não |
| `GilsonChaos 80` | — | **`3.4.2` 473t +80 promovidos** — `C1-C9+and/BIND/INCLUDE/TRIM/SET/IFDEFINED/unicode` exploratórios → xUnit permanente 393→473 | `dotnet test --filter Gilson` 80 Passed | Não |
| `GeneratedRegex` | 11 | **`11→11`** sem novo regex | `grep -c GeneratedRegex SqlTemplate.cs ==11` | — |
| `LOC src` | 2877 | **`3188` <3500** (+311 tese) | `find src -name "*.cs" ! -path "*/obj/*" | xargs wc -l` | — |
| `Pack` | 68K | **`78K` + `.snupkg` com `README+LICENSE+icon.png` sem `NU5048`, `Deterministic true`** | `dotnet pack -c Release -o /tmp/pack && unzip -l *.nupkg | grep icon.png` | — |
| `TWE` | true | **`true strict`** sem `WarningsNotAsErrors` | `dotnet build -c Release` 0 erro | — |
| `GLOBAL_RULES` | 59L | **`59L` agnóstico intacto** | `cat GLOBAL_RULES.md | wc -l` | — |
| `Npgsql agnóstico` | 0 | **`0` em `src/`** `grep -r Npgsql src ==0` + `unzip -p *.nupkg *.nuspec | grep -i Npgsql ==0` | Ver gate | — |
| `Docs` | `CHANGELOG [3.3.0]`, `MIGRATION §8.12`, `PUBLISH 3.3.0`, `README 11 tags`, `print-32.md` | **`CHANGELOG [3.4.2]` + `MIGRATION §8.13` + `PUBLISH 3.4.2` stable + `README 11 tags verificado` + `print-38.md` roadmap versionado** | Ver checklist | — |
| `Tag` | `v3.3.0` pendente | **`v3.4.2` só após auditor APROVADO ≥9.0** | `git tag v3.4.2 && git push origin v3.4.2` dispara `publish.yml` | — |

**Sem breaking 3.3.0 → 3.4.2:** `3.4.2` já `Version 3.4.2` stable sem sufixo sobre d61ef60 — só docs promoção. `Academia` consome `PackageReference Version="3.4.2"` sem código. `Migrations` Y/N `CHAR(1) Y/N` intacto, `SqlTemplate` `and` simples 1× 2 fatores intacto, `IFDEFINED` 11 regex intacto, `Migrations` `TestProviderRegistry` 4 DBs opt-in. Ver `print-38.md` §3 roadmap `3.5.0 Should 1.0 SP` (UseTransaction + sample t-odonto + docs OTel) só se dor >30%, hoje **Won't**.

**Checklist 3.4.2 DoD promoção stable:**
- [ ] `grep "<Version>3.4.2</Version>" src/Footing.Framework/Footing.Framework.csproj` == `3.4.2`
- [ ] `grep -c "GeneratedRegex" src/Footing.Framework/Data/SqlTemplate.cs` ==11
- [ ] `find src -name "*.cs" ! -path "*/obj/*" | xargs wc -l | tail -1` == `3188 <3500`
- [ ] `dotnet build -c Release` 0 erro, `TWE true`, `WarningsNotAsErrors` vazio
- [ ] `dotnet test -c Release` 473 Passed 0 Skipped, `dotnet pack -c Release -o /tmp/pack` gera `Footing.Framework.3.4.2.nupkg + .snupkg ~78K` com `README+LICENSE+icon.png` (`unzip -l | grep icon.png`)
- [ ] `grep -r Npgsql\|Redis\|Firebird\|OleDb src/Footing.Framework --include="*.cs" --include="*.csproj"` ==0 + `unzip -p *.nupkg *.nuspec | grep -i Npgsql ==0`
- [ ] `cat GLOBAL_RULES.md | wc -l` ==59, `ls src/Footing.Framework/Migrations/` 8 arquivos SPI + Runner + Parser + Checksum
- [ ] `CHANGELOG [3.4.2]` + `MIGRATION §8.13` + `PUBLISH 3.4.2` + `README 11 tags verificado` + `print-38.md` roadmap versionado sem drift
- [ ] `git status` limpo exceto `.opencode/print` backlog (não taguear — auditor tagueia)

### 8.14 Nota 3.5.0 revisado 0.7 SP — UseTransaction Won't definitivo + docs Antora Must (0 LOC prod docs + throw agnóstico)

> **Status:** `3.5.0` revisado `0.7 SP` — `UseTransaction` **Won't definitivo, sem BUY** + docs Antora Must 0.3 + perf Could 0.2, sobre `a159d03` (`2878 LOC 473t 11 regex 78K`). Stakeholder definiu: "isso documente, não vai haver suporte exclusivo a isso nem em buy, o que quero é um exception descritivo informando que não deve se fazer isso" — `UseTransaction` 20L PG-only vira **Won't com throw `NotSupportedException` descritivo**, sem `if driver==pg`, apenas `throw` direto agnóstico.

| Item | Antes `3.5.0 a159d03` | Depois `3.5.0 revisado Won't` | Ação dev | Breaking? |
|------|------------------------|-------------------------------|----------|-----------|
| `Version` | `3.5.0` | **`3.5.0` mantido** (ou `3.5.1` patch docs) — sem bump | `grep "<Version>3.5.0</Version>" csproj` | Não — docs + throw |
| `MigrationOptions.UseTransaction` | não existia | **`bool UseTransaction {get;set;} default false` com `throw NotSupportedException` se `true`** — `src/Migrations/MigrationOptions.cs` 7→~22L, `grep UseTransaction src/` só para `throw` | `new MigrationOptions{UseTransaction=true}` → `throw` | Não — `false` default compat, `true` sempre throw descritivo |
| `MigrationRunner` | sem guard | **`if (_options.UseTransaction) throw NotSupportedException("DDL transacional agnóstico não suportado — MSSQL tolera mas não recomenda, Firebird/dbf/SQLite não permitem DDL em transação. Mantenha AutoTransaction=false e use BEGIN/COMMIT no .sql se for PG. Veja GLOBAL_RULES §1 e migrations.md §6.3")`** — sem `if driver==pg`, apenas `throw` direto | `new MigrationRunner(..., new MigrationOptions{UseTransaction=true})` → `throw` | Não |
| `GLOBAL_RULES.md` | 59L sem UseTransaction | **+1 bullet §1 UseTransaction Won't definitivo** — `DDL transacional agnóstico não suportado, mesmo com BUY — MSSQL tolera mas não recomenda, Firebird/dbf/SQLite não permitem DDL em transação. AutoTransaction=false agnóstico, BEGIN/COMMIT no .sql se PG. Veja migrations.md §6.3` | `cat GLOBAL_RULES.md | grep UseTransaction` | Não |
| `migrations.md §6.3` | `AutoTransaction=false` agnóstico | **`§6.3 UseTransaction Won't definitivo` — documenta `Won't mesmo com BUY`, `AutoTransaction=false` agnóstico, `BEGIN/COMMIT` no `.sql` se PG, `throw` descritivo, sem `if driver==pg`** | ver `migrations.md` | Não |
| `docs/modules/ROOT/pages/migrations.adoc` | `UseTransaction opt-in Should 0.2` docs-only | **`UseTransaction Won't definitivo` — reescrito Won't, sem BUY, `0 Npgsql/src`, `throw` snippet** | `grep UseTransaction docs/modules/ROOT/pages/migrations.adoc` | Não |
| `docs/modules/ROOT/pages/otel-health.adoc` | `UseTransaction opt-in S1 Should` | **`UseTransaction Won't definitivo` — cross-ref `migrations.adoc Won't`** | `grep UseTransaction docs/modules/ROOT/pages/otel-health.adoc` | Não |
| `GeneratedRegex` | 11 | **`11→11`** sem novo regex | `grep -c GeneratedRegex src/.../SqlTemplate.cs ==11` | — |
| `LOC src` | 2878 | **`~2880-2895 <3500`** (+~15-20L throw docs) | `find src -name "*.cs" ! -path "*/obj/*" \| xargs wc -l` | — |
| `Tests` | 473 Passed | **`474 Passed` (+1 `MigrationOptions_UseTransaction_ThrowsDescriptive`)** — `Assert.Throws<NotSupportedException>(() => new MigrationOptions{UseTransaction=true})` + `Assert.ThrowsAsync<NotSupportedException>(() => new MigrationRunner(..., UseTransaction:true).MigrateAsync())`, mensagem contém `DDL transacional agnóstico não suportado` | `dotnet test -c Release` | — |
| `Pack` | 78K | **`78K`** `README+LICENSE+icon.png`, `0 Npgsql/src`, `TWE true` | `dotnet pack -c Release -o /tmp/pack && unzip -p *.nupkg *.nuspec \| grep -i Npgsql ==0` | — |
| `Docs` | `CHANGELOG [3.5.0]`, `MIGRATION §8.13` | **`MIGRATION §8.14` + `GLOBAL_RULES + migrations.md §6.3 + migrations.adoc Won't` sem drift** | `npx antora antora-playbook.yml` → 10 pages 0 erro | — |

**Por que Won't mesmo com BUY (agnóstico):**

> DDL transacional não é portável. `MSSQL` `CREATE TABLE` em transação tolera mas não recomenda (lock longo, log, deadlock, `ALTER` pode bloquear), `Firebird`/`dbf` `DDL auto-commit` ignora transação e quebra semântica atômica, `SQLite` `DDL` não transacional confiável. Suporte agnóstico implicaria `if (driver==pg)` + `IDbTransaction` — viola `GLOBAL_RULES §1` agnosticismo. Framework opta por `throw` descritivo e delega `BEGIN/COMMIT` ao `.sql` quando PG realmente precisa (`migrations.md §6.3`).

**Snippet Won't com throw (único em src):**

```csharp
// MigrationOptions.cs — setter lança se true (default false)
public bool UseTransaction
{
    get => _useTransaction;
    set
    {
        if (value) throw new NotSupportedException("DDL transacional agnóstico não suportado — MSSQL tolera mas não recomenda, Firebird/dbf/SQLite não permitem DDL em transação. Mantenha AutoTransaction=false e use BEGIN/COMMIT no .sql se for PG. Veja GLOBAL_RULES §1 e migrations.md §6.3");
        _useTransaction = value;
    }
}

// MigrationRunner.cs — guard sem if driver==pg
if (_options.UseTransaction) throw new NotSupportedException("DDL transacional agnóstico não suportado — MSSQL tolera mas não recomenda, Firebird/dbf/SQLite não permitem DDL em transação. Mantenha AutoTransaction=false e use BEGIN/COMMIT no .sql se for PG. Veja GLOBAL_RULES §1 e migrations.md §6.3");
```

**Checklist 3.5.0 Won't (DoD):**
- [ ] `grep -rn "UseTransaction" src/Footing.Framework --include="*.cs"` → só `MigrationOptions.cs` setter `throw` + `MigrationRunner.cs` guard `throw`, sem `IDbTransaction` impl, sem `if driver==pg`
- [ ] `grep -rn "UseTransaction" docs/modules/ROOT/pages/migrations.adoc` → `Won't definitivo, sem BUY`
- [ ] `cat GLOBAL_RULES.md | grep UseTransaction` → `Won't definitivo`
- [ ] `cat migrations.md | grep -A2 "6.3.*Won't"` → `UseTransaction Won't`
- [ ] `dotnet test -c Release` → `474 Passed` (473 + 1 UseTransaction throw), `dotnet build -c Release` 0 erro `TWE true`
- [ ] `grep -c GeneratedRegex src/.../SqlTemplate.cs` ==11, `find src -name "*.cs" ! -path "*/obj/*" | xargs wc -l` <3500, `grep -r Npgsql src/Footing.Framework --include="*.cs" ==0`
- [ ] `dotnet pack -c Release -o /tmp/pack` → `Footing.Framework.3.5.0.nupkg ~78K` `0 Npgsql/src`, `Version 3.5.0` mantido (ou `3.5.1` patch docs se auditor preferir patch)

---

### 8.15 Nota 3.5.1 patch — Fixed i18n varredura PT→EN + BenchmarkDotNet formal + and revalidação (0.35 SP sem or)

> **Status:** `3.5.1` patch 0.35 SP sem or — `3.5.0` aa37af8 477t 11 regex 78K 2897 LOC → `3.5.1` 477t 11 regex 78K 2897 LOC <3.5K (0 LOC prod docs only + `tests/Footing.Framework.Benchmarks` PrivateAssets `BenchmarkDotNet` formal, Smoke `<2000ms` já DONE). `or` flat **Won't definitivo 0 SP sem gatilho BUY** (validado t-odonto 44 xml 208 `<if>` **0% 0/208** sem demanda vs `and` **34.1% 71/208** Must DONE v2.3.0-alpha 11 testes) + `and 3+` 0.25 Won't + `full OGNL` 3-5 Won't + SG 2.5 Won't + Sample 0 Won't. Tag `v3.5.1` só após auditor @aud_carlos ≥9.0.

| Item | `3.5.0` aa37af8 | `3.5.1` patch | Ação dev | Breaking? |
|------|------------------|---------------|----------|-----------|
| `Version` | `3.5.0` | **`3.5.1` patch SemVer** | `grep "<Version>3.5.1</Version>" csproj` | Não — patch docs/perf |
| `Fixed i18n` | — | **`Fixed i18n varredura PT→EN 0.15 SP`** — `src` 27 Must + `tests` 8 Should (`c6e7e28 0.10 + aa37af8 0.05`) — `MigrationRunner` 4 throws, `SqlTemplate` 6 warnings, 14 XML docs, `grep -P "[áàâãéêíóôõúç]" src ==0`, `grep -rn "\.md" src ==0` | Ver `CHANGELOG [3.5.1] Fixed` | Não |
| `Benchmark` | `BenchmarkSmokeTests` Stopwatch 3 testes `<2000ms 10k` | **+ `BenchmarkDotNet` formal PrivateAssets** — `tests/Footing.Framework.Benchmarks/Benchmarks.cs` suite `PrivateAssets="all"` + `BenchmarkDotNet` `MemoryDiagnoser` + Smoke `<2000ms` mantido, 0.20 SP, 11→11 regex | `dotnet run -c Release --project tests/Footing.Framework.Benchmarks` | Não |
| `and` | 11 testes Gherkin Must DONE | **Revalidação 0 SP** — `dotnet test --filter And 43 Passed` + `dotnet test 477 Passed` + `grep -c GeneratedRegex ==11` + docs `sql-template.adoc` 11 tags | `dotnet test --filter And` | Não |
| `or` Won't | Won't definitivo | **Permanece Won't definitivo sem gatilho BUY** — t-odonto 44 xml 208 `<if>` `or 0% 0/208` validado (stakeholder: "or sempre será wont") — não reavaliar mesmo com BUY >30% | `grep ContainsOrOutsideQuotes src/Data/SqlTemplate.cs` retorna `false+Warning` | — |
| `GeneratedRegex` | 11 | **`11→11`** sem novo regex (Benchmark não toca `src`) | `grep -c GeneratedRegex ==11` | — |
| `LOC src` | 2897 | **`2897 <3500`** (0 LOC prod docs only + benchmark só `tests/`) | `find src -name "*.cs" ! -path "*/obj/*" | xargs wc -l` | — |
| `Tests` | 477 Passed | **477 Passed** (ou 477→477 benchmark terceirizado) | `dotnet test -c Release` | — |
| `Pack` | 78K | **78K** `README+LICENSE+icon.png` `0 Npgsql/src` `TWE true` | `dotnet pack -c Release -o /tmp/pack && unzip -p *.nupkg *.nuspec | grep -i Npgsql ==0` | — |
| `Docs` | `CHANGELOG [3.5.0]`, `MIGRATION §8.14`, `PUBLISH 3.5.0` | **`CHANGELOG [3.5.1] Fixed+Changed` + `MIGRATION §8.15` + `PUBLISH 3.5.1` + `README 11 tags 3.5.1` + `docs/antora.yml 3.5.1` + `_attributes.adoc 3.5.1`** | `npx antora antora-playbook.yml` → 13 html 0 erro | — |
| `Tag` | `v3.5.0` | **`v3.5.1` só após auditor APROVADO ≥9.0** | `git tag v3.5.1 && git push origin v3.5.1` dispara `publish.yml` | — |

**Gates 3.5.1 DoD (copy-paste auditor):**

```bash
dotnet test -c Release --filter "And"  # 43 Passed
dotnet test -c Release                 # 477 Passed 0 Skipped
grep -c "GeneratedRegex" src/Footing.Framework/Data/SqlTemplate.cs # 11
find src -name "*.cs" ! -path "*/obj/*" | xargs wc -l | tail -1 # 2897 <3500
grep "<Version>" src/Footing.Framework/Footing.Framework.csproj # 3.5.1
dotnet build -c Release -p:ContinuousIntegrationBuild=false 2>&1 | tail -3 # 0 erro TWE true
grep -rn "DDL transactions are not supported" src --include="*.cs" | wc -l # 3
grep -P "[áàâãéêíóôõúç]" src --include="*.cs" | wc -l # 0
grep -rn "\.md" src --include="*.cs" | grep -v obj | wc -l # 0
grep -r "Npgsql\|Redis\|FluentValidation\|Firebird\|OleDb" src --include="*.cs" --include="*.csproj" | grep -v "DDL transactions are not supported" | wc -l # 0
dotnet pack -c Release -o /tmp/pack && unzip -p /tmp/pack/Footing.Framework.*.nupkg *.nuspec | grep -i Npgsql | wc -l # 0
dotnet run -c Release --project tests/Footing.Framework.Benchmarks # <2000ms 10k + BenchmarkDotNet report
wc -l GLOBAL_RULES.md # 60
```

**Checklist 3.5.1 DoD:**
- [ ] `CHANGELOG [3.5.1] Fixed i18n + Changed Version 3.5.0→3.5.1` + `MIGRATION §8.15` + `PUBLISH 3.5.1` + `README 3.5.1 11 tags`
- [ ] `dotnet test 477 Passed` + `And 43 Passed` + `11 regex` + `2897 LOC` + `Version 3.5.1` + `pack 78K` + `tag v3.5.1`
- [ ] `or Won't definitivo 0% 0/208 t-odonto vs and 34.1% 71/208 Must DONE` sem `or/and 3+/!/()/user.name` novo

---

> **Dúvidas?** Abra issue com label `migration` ou consulte `CHANGELOG.md` seções `[3.5.1]`, `[3.5.0]`, `[3.4.2]`, `[3.3.0]`, `[3.2.0]`, `[3.1.0]`
, `[3.0.0]`, `[2.4.0]`, `[2.3.1]`, `[2.3.0]`, `[2.2.0]`, `[2.1.0]`, `[2.0.0]`, `[2.0.0-alpha.1]`, `[2.0.0-alpha]`, `[1.1.0-alpha]` e `[1.0.0]`.