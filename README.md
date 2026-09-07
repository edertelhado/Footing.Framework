# Footing.Framework

[![Docs Antora](https://img.shields.io/badge/docs-antora-blueviolet)](docs/build/site/footing-framework/3.5/index.html) [![Version](https://img.shields.io/badge/version-3.5.1-blue)](src/Footing.Framework/Footing.Framework.csproj) [![Tests](https://img.shields.io/badge/tests-477%20passed-brightgreen)](tests/Footing.Framework.Tests) [![Regex](https://img.shields.io/badge/regex-11-orange)](src/Footing.Framework/Data/SqlTemplate.cs) [![Pack](https://img.shields.io/badge/pack-78K-yellow)](https://www.nuget.org/packages/Footing.Framework) [![Agnostic](https://img.shields.io/badge/agnostic-PG%20%7C%20Firebird%20%7C%20dbf-lightgrey)](docs/modules/ROOT/pages/outbox-idempotency.adoc)

> **Autor:** **Eder Rafael Telhado** — Brasil · [@edertelhado](https://github.com/edertelhado) · [CapybaraInfo](https://github.com/CapybaraInfo) · [LinkedIn](https://www.linkedin.com/in/etelhado/) · `edertelhado@outlook.com.br` · Explorando novas tecnologias, Spring Boot & VueJS

Microframework base para construção de serviços com .NET (a "sapata" do projeto). Oferece EventBus in-process com
handlers tipados e priorizados, Data Layer dinâmico (SqlTemplate + Dapper), DI com auto-scan, Lifecycle e
Injeção de Configuração.

> **Nota:** este é um microframework **genérico e reutilizável**, extraível para outros projetos. Não deve conter
> código de domínio, migrations ou SQL de negócio de nenhum projeto específico.

---

## Índice

- [EventBus](#eventbus)
    - [Conceitos](#conceitos)
    - [Registro via DI](#registro-via-di)
    - [Handlers por Atributo \[EventListener\]](#handlers-por-atributo-eventlistener)
    - [Handlers Tipados IEventHandler\<T\>](#handlers-tipados-ieventhandler)
    - [Subscrição Manual](#subscrição-manual)
    - [Publicação de Eventos](#publicação-de-eventos)
    - [Prioridade de Execução](#prioridade-de-execução)
- [Data Layer](#data-layer)
    - [SqlTemplate (Estilo MyBatis)](#sqltemplate-estilo-mybatis)
    - [IDbConnectionFactory](#idbconnectionfactory)
    - [UnitOfWork](#unitofwork)
    - [Type Handlers](#type-handlers)
- [DI e Atributos](#di-e-atributos)
    - [Auto-Scan com AddDependencyWalk](#auto-scan-com-adddependencywalk)
    - [Atributos](#atributos)
    - [ResolverExtensions](#resolverextensions)
- [Lifecycle](#lifecycle)
    - [PostConstruct](#postconstruct)
    - [PreDestroy](#predestroy)
- [Config](#config)
    - [InjectConfig](#injectconfig)

---

## EventBus

Barramento de eventos in-process com suporte a handlers assíncronos, priorização e resolução por hierarquia de tipos.

### Conceitos

- **Publish** — enfileira o evento em `Channel<object>` bounded (capacity 1000 by default, `FullMode.Wait` para backpressure) para processamento em background por workers paralelos (sem `Task.Delay` busy-loop)
- **PublishAsync** — enfileira com `WriteAsync` respeitando backpressure e `CancellationToken`; workers drenam a fila no `StopAsync` (`Writer.Complete` + `WhenAll`)
- **Handlers resolvem por herança** — handlers registrados para uma interface ou base type recebem eventos de tipos derivados
- **Prioridade** — handlers com prioridade maior executam primeiro

### Registro via DI

```csharp
// Registro simples com 4 workers e Channel bounded capacity 1000 (padrão)
services.AddEventBus();

// Configuração customizada com backpressure
services.AddEventBus(options =>
{
    options.WorkerCount = 4;
    options.Capacity = 1000;
    options.FullMode = BoundedChannelFullMode.Wait; // Wait, DropNewest, DropOldest, DropWrite
    options.DrainOnStop = true;
    options.AutoRegisterHandlers = true;
});

// Publish fire-and-forget (TryWrite, warn se cheio)
eventBus.Publish(new UserCreatedEvent { UserId = 123 });
// Publish com backpressure
await eventBus.PublishAsync(new UserCreatedEvent { UserId = 123 }, cancellationToken);
```

### Handlers por Atributo `[EventListener]`

Decore qualquer método público de uma classe registrada no DI:

```csharp
public class UserCreatedHandler
{
    [EventListener(Priority = 10)]
    public async Task OnUserCreated(UserCreatedEvent evt)
    {
        // lógica do handler
    }

    [EventListener] // Priority = 0 (padrão)
    public void OnUserCreatedSync(UserCreatedEvent evt)
    {
        // handlers síncronos também funcionam
    }
}
```

O método deve receber um único parâmetro — o tipo do evento. Pode retornar `void` ou `Task`.

### Handlers Tipados `IEventHandler<T>`

Implemente a interface para handlers fortemente tipados:

```csharp
public class UserCreatedEventHandler : IEventHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent evt, CancellationToken ct = default)
    {
        // processa o evento
    }
}
```

Implementações de `IEventHandler<T>` são automaticamente descobertas e registradas como `Transient` no DI.

### Subscrição Manual

```csharp
var eventBus = serviceProvider.GetRequiredService<EventBus>();

// Com delegate
eventBus.Subscribe<UserCreatedEvent>(async evt =>
{
    await DoSomething(evt.UserId);
}, priority: 5);

// Com IEventHandler
eventBus.Subscribe(new UserCreatedEventHandler(), priority: 10);
```

### Publicação de Eventos

```csharp
// Fire-and-forget (via fila)
eventBus.Publish(new UserCreatedEvent { UserId = 123 });

// Tipado
eventBus.Publish(new UserCreatedEvent { UserId = 123 });

// Aguarda processamento
await eventBus.PublishAsync(new UserCreatedEvent { UserId = 123 });
await eventBus.PublishAsync<UserCreatedEvent>(new UserCreatedEvent { UserId = 123 });
```

### Prioridade de Execução

Handlers com **maior valor** de `Priority` executam **primeiro**.

```csharp
// Executa primeiro (prioridade mais alta)
[EventListener(Priority = 100)]
public void CriticalHandler(MyEvent evt) { ... }

// Executa depois
[EventListener(Priority = 0)]
public void NormalHandler(MyEvent evt) { ... }
```

Handlers do mesmo nível de prioridade executam em paralelo via `Task.WhenAll`.

---

## Data Layer

Camada de acesso a dados que combina **SQL dinâmico** com **Dapper**, totalmente agnóstica a banco de dados.

### SqlTemplateLoader

Carrega arquivos `.sql` com template de dentro do assembly (embedded resources), mantendo o SQL em arquivos separados ao
invés de strings no C#.

#### Setup

1. Crie um diretório `Sql/` ou `Queries/` no seu projeto e adicione os arquivos `.sql`:

```sql
-- src/MyApp.Data/Sql/Users/GetAll.sql
SELECT u.Id, u.Name, u.Email, u.CreatedAt
FROM Users u
{WHERE}
    u.Active = 1
    {IF:Name} AND u.Name LIKE @Name {END}
    {IF:Email != null} AND u.Email = @Email {END}
{ENDWHERE}
ORDER BY u.Name
```

2. Marque os arquivos como **EmbeddedResource** no `.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="Sql\**\*.sql" />
</ItemGroup>
```

#### Uso

```csharp
// Por convenção: usa assembly + namespace do tipo de referência
var template = SqlTemplateLoader.For<UserRepository>("Sql.Users.GetAll");

// Equivalente explícito:
var template = SqlTemplateLoader.ForAssembly(typeof(UserRepository).Assembly, "Sql.Users.GetAll");

// Por nome completo do resource:
var template = SqlTemplateLoader.Load("MyApp.Data.Sql.Users.GetAll.sql", typeof(UserRepository).Assembly);

// Renderiza com parâmetros
var result = template.Render(new { Name = "%john%", Email = "john@test.com" });
await conn.QueryAsync<User>(result.Sql, result.Parameters);
```

#### Cache

Os templates são cacheados em memória (`ConcurrentDictionary`). Para invalidar:

```csharp
SqlTemplateLoader.Invalidate("MyApp.Data.Sql.Users.GetAll.sql");
SqlTemplateLoader.ClearCache();
```

#### Carregar todos os `.sql` de um assembly

```csharp
var all = SqlTemplateLoader.LoadAll(typeof(UserRepository).Assembly);
// all["MyApp.Data.Sql.Users.GetAll.sql"].Render(...)
```

### SqlTemplate (Estilo MyBatis)

Templates de SQL com tags condicionais, similar ao MyBatis/iBATIS:

```csharp
var template = SqlTemplate.Parse(@"
    SELECT u.*, r.Name AS RoleName
    FROM Users u
    JOIN Roles r ON r.Id = u.RoleId
    {WHERE}
        u.Active = 1
        {IF:Name} AND u.Name LIKE @Name {END}
        {IF:Email != null} AND u.Email = @Email {END}
        {IF:Role != ''} AND r.Name = @Role {END}
        {IF:Status == 'ACTIVE'} AND u.Status = @Status {END}
        {IF:Age >= 18} AND u.Age >= @Age {END}
        {IN:Roles} AND u.RoleId IN @Roles {END}
        {BETWEEN:CreatedAt} AND u.CreatedAt BETWEEN @CreatedAtMin AND @CreatedAtMax {END}
    {ENDWHERE}
    ORDER BY u.Name
");

var result = template.Render(new
{
    Name = "%john%",
    Email = "john@test.com",
    Status = "ACTIVE",
    Age = 20,
    Roles = new[] { 1, 2, 3 },
    CreatedAtMin = new DateTime(2024,01,01),
    CreatedAtMax = new DateTime(2024,12,31)
});

// Gera:
// SELECT u.*, r.Name AS RoleName FROM Users u
// JOIN Roles r ON r.Id = u.RoleId
// WHERE u.Name LIKE @Name AND u.Status = @Status AND u.Age >= @Age AND u.RoleId IN @Roles
// ORDER BY u.Name
```

#### Tags Suportadas (v3.4.2 — 11 tags, 11 regex — and/length/IFDEFINED intactos, Spec agnóstico, 473t verificado)

| Tag | Descrição |
|-----|-----------|
| `{IF: expr} ... {END}` | `expr := Param \| Param Operator Operand \| length(Param) Operator Operand \| defined(Param) \| A and B` — renderiza se `EvaluateCondition(expr)==true`. `Operator := ==\|!=\|=\|<>\|>\|<\|>=\|<=\|gt\|lt\|gte\|lte\|ge\|le` (case-insensitive). `Operand := null\|'str'\|"str"\|número\|bool\|@Param\|bare Param`. `length(Param)` → `decimal` Length (`0` null/missing, `s.Length` string, `ToString().Length` número/outro). `defined(Param)` → `ContainsKey` distingue `missing` (false) vs `null`/`""`/`Y` (true) — ex: `{IF:defined(Ativo)}`, `{IF:defined(Ativo) and Ativo=='Y'}`. **`and` simples 2.3.0:** `{IF:Busca != null and Busca != ''} AND x {END}` 1× `and` 2 fatores case-insensitive fora de `''/""`, short-circuit, rejeita `or/()/!/a and b and c` com `false+Warning`. Ex: `{IF:Status}`, `{IF:Status == 'ACTIVE'}`, `{IF:Age >= 18}`, `{IF:Status != null}`, `{IF:Preco > @Limite}`, **`{IF:length(Name) > 18} AND Name=@Name {END}`** (2.1.0 opt-in) |
| `{IFDEFINED:Param} ... {END}` / `{IFNOTDEFINED:Param} ... {END}` | `IFDEFINED` — `ContainsKey` distingue `missing` (false) vs `null`/`""`/`Y` (true), opt-in sem breaking. Ex: `{IFDEFINED:Ativo} AND c.Ativo IS NULL {END}` → `new {}` missing→omitido, `new {Ativo=null}`→IS NULL. `IFNOTDEFINED` oposto. `defined(Param)` dentro de `{IF:}` é alias. Ver tri-state §. |
| `{WHERE} ... {ENDWHERE}` | Bloco WHERE inteligente — só adiciona o WHERE se algo dentro renderizar, e remove AND/OR do primeiro item (via `TrimContent` `prefix="WHERE" prefixOverrides="AND|OR "`) |
| `{IN:Param} ... {END}` | Renderiza se `Param is IEnumerable` não-string com ≥1 item não-nulo |
| `{CHOOSE} {WHEN: expr} ... {ENDWHEN} {OTHERWISE} ... {ENDOTHERWISE} {ENDCHOOSE}` | Switch-case: primeiro `WHEN` com `EvaluateCondition(expr)==true` → seu conteúdo; senão `OTHERWISE` |
| `{BETWEEN:Param} ... {END}` | Sugar flat `hasMin && hasMax` (`ParamMin`/`ParamMax`) — ex: `{BETWEEN:CreatedAt} AND CreatedAt BETWEEN @CreatedAtMin AND @CreatedAtMax {END}` |
| `{TRIM prefix="..." prefixOverrides="..." suffix="..." suffixOverrides="..."} ... {ENDTRIM}` | TRIM genérico MyBatis — remove overrides do início/fim e adiciona prefix/suffix se conteúdo não vazio. Ex: `{TRIM prefix="WHERE" prefixOverrides="AND|OR "} AND x=1 OR y=2 {ENDTRIM}` → `WHERE x=1 OR y=2` (via `TrimContent`) |
| `{SET} ... {ENDSET}` | Açúcar sobre TRIM — `{SET} {IF:Name != null} Name=@Name, {END} UpdatedAt=NOW(), {ENDSET}` → `SET Name=@Name, UpdatedAt=NOW()` sem vírgula trailing (via `TrimContent` `prefix="SET" suffixOverrides=","`) — `WHERE` também delega `TrimContent` |
| `{INCLUDE:Fragment}` / `{SQL:Fragment}` | Fragmento reutilizável DRY — `SqlTemplate.RegisterFragment("BaseColumns","u.Id, u.Name")` + `SELECT {INCLUDE:BaseColumns} FROM Users` → `SELECT u.Id, u.Name FROM Users` (`FragmentCache ConcurrentDictionary`, depth guard 5, miss → literal fail-safe) |
| `{BIND:Var, value='%' + Param + '%'}` | Variável concatenação — `value` split `+` fora de `''/""`, concatena `'literal'` + `\w+` param, valida `Var ^[A-Za-z_]\w*$`, rejeita `;--/*` fail-safe, injeta `paramDict["Var"]=computed` antes de `IF/WHERE` — ex: `{BIND:LikePattern, value='%' + Name + '%'} ... {IF:LikePattern != null} AND Name LIKE @LikePattern {END}` com `Name="eder"` → `"%eder%"` |

> **Removidos v2.0.0-alpha (breaking):** `{EQ}/{NE}/{LT}/{GT}/{GTE}/{GE}/{LTE}/{LE}` (9 tags bugadas só existência) + `{IFNOTNULL}/{IFNOTEMPTY}` — migrar para `{IF:Param}`, `{IF:Param == 'x'}`, `{IF:Param > 100}`, `{IF:Param != null}`, `{IF:Param != ''}`. Tags antigas permanecem literais (fail-safe visível). Ver `MIGRATION.md` §8.

> **Fail-fast vs fail-safe (v2.4.0):** Template malformado `S1-S6` (`{IF:Name} ...` sem `{END}`, `{WHERE} ...` sem `{ENDWHERE}`, `{CHOOSE} ...` sem `{ENDCHOOSE}`, `{WHEN} ...` sem `{ENDWHEN}`, `{BETWEEN} ...` sem `{END}`, `{IF:Name` sem `}` → `Unclosed tag`, `{IFDEFINED:Ativo} ...` sem `{END}`) → **`SqlTemplateParseException` com `Tag/Expected/Line/Column/Snippet/Suggestion` (`: InvalidOperationException`)** em `SqlTemplate.Parse`/`RenderTemplate` guard + bubble `SqlTemplateLoader.Load` (fail-fast, igual MyBatis `BuilderException` no startup). Param erro (`Nmae` typo, `"abc" >100`, `IN "admin"`, `"' ; DROP"` valor) permanece **fail-safe omitido/false+Warning/placeholder** (dados resilientes). Ver `MIGRATION.md §8.8` e `print-21.md`.

#### Tri-state `null / Y / N` + `IS NULL` — `traz todos` vs `IS NULL` (v2.4.0 IFDEFINED 1 SP)

> **TL;DR:** `<select null/Y/N traz todos>` já é `{IF:Ativo} AND c.Ativo=@Ativo {END}` — `null / missing / "" → omitido → traz todos`, `Y/N → filtra`. Para buscar nulidade use `{IF:Ativo == null} AND c.Ativo IS NULL {END}` (BC `missing==null`). **Novo 2.4.0:** `{IFDEFINED:Ativo} AND c.Ativo IS NULL {END}` distingue `missing` (false) vs `null` (true) via `ContainsKey` — traz todos vs IS NULL sem 2 params.

| # | Template | `missing` (`new { }`) | `null` | `""` | `"Y"` | `"N"` | Veredito |
|---|----------|------------------------|--------|------|-------|-------|----------|
| **T1 traz-todos** | `{WHERE} {IF:Ativo} AND c.Ativo=@Ativo {END} {ENDWHERE}` | ❌ traz todos (WHERE some) | ❌ traz todos | ❌ traz todos (`IsNullOrEmpty`) | ✅ filtra Y | ✅ filtra N | ✅ **`<select null/Y/N traz todos>` — use este** |
| **T3 IS NULL** | `{IF:Ativo == null} AND c.Ativo IS NULL {END}` | ✅ IS NULL (BC — `new {} == null true`) | ✅ IS NULL | ❌ (`"" != null` false) | ❌ | ❌ | Buscar `IS NULL` — `missing` e `null` hoje indistinguíveis para `== null` |

> **And implícito:** `{IF:Ativo}` shorthand = `TryGetValue && v != null && !IsNullOrEmpty` — já é `Ativo != null and Ativo != ''` em um bloco. `{IF:Ativo != null}` sozinho **vaza** `""` (`"" != null` → true → renderiza `AND c.Ativo=''` indevido); `{IF:Ativo != ''}` vaza `null`. Para `and/or` explícito com operador (`{IF:Ativo != null and Ativo != ''}`) use 2 blocos sequenciais/aninhados ou aguardar backlog `and/or` Won't 3.0 (gatilho BUY >30% — ver `MIGRATION.md §8.4`).

**4 estados (Y/N/IS NULL/Todos) — 2 params (docs, sem código, Opção 0):**

```sql
-- Sql/Clientes/Search.sql
SELECT c.Id, c.Nome, c.Ativo FROM Clientes c
{WHERE}
  {IF:Ativo} AND c.Ativo = @Ativo {END}
  {IF:BuscarNulos == true} AND c.Ativo IS NULL {END}
{ENDWHERE}
```
```csharp
var tpl = SqlTemplate.Parse(File.ReadAllText("Sql/Clientes/Search.sql"));
tpl.Render(new { Ativo=(string?)null, BuscarNulos=false }); // todos (WHERE some → sem filtro)
tpl.Render(new { Ativo=(string?)null, BuscarNulos=true });  // IS NULL
tpl.Render(new { Ativo="Y", BuscarNulos=false });            // filtra Y
tpl.Render(new { Ativo="" }); // HTML "" → traz todos (shorthand falsy)
```

> **IFDEFINED 2.4.0:** `IFDEFINED`/`IFNOTDEFINED`/`defined()` já implementado 2.4.0 1 SP — `missing` vs `null` via `ContainsKey`, `10→11 regex`, `and` compat (`defined(Ativo) and Ativo=='Y'`), `CHOOSE WHEN defined` compat. Ver `MIGRATION.md §8.8` e `print-21.md`.
> **Backlog:** `and/or/!/()` permanece **Won't 3.0** com gatilho BUY >30% templates (ver `print-13.md`); `or` flat/`and` 3+ Won't 3.0.

### IDbConnectionFactory

Fábrica de conexões. Os repositórios dependem dela e usam **Dapper diretamente** — sem wrapper que esconda a API
completa (multi-mapping, transações, `commandTimeout`, etc.). O framework é genérico: a implementação concreta fica no
projeto de aplicação/domínio.

```csharp
// Footing.Framework/Data/IDbConnectionFactory.cs
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

// Academia.Core/Data/NpgsqlConnectionFactory.cs (implementação concreta)
public class NpgsqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection() => new NpgsqlConnection(connectionString);
}
```

### UnitOfWork

Transação explícita sobre Dapper: abre a conexão, inicia a transação e expõe `Connection` + `Transaction` para as
chamadas Dapper (não esconde a API). Faz rollback automático no `DisposeAsync` se `CommitAsync` não foi chamado.

```csharp
// Footing.Framework/Data/UnitOfWork.cs
public sealed class UnitOfWork : IAsyncDisposable
{
    public IDbConnection Connection { get; }
    public IDbTransaction? Transaction { get; }
    public Task BeginAsync(CancellationToken cancellationToken = default);
    public Task CommitAsync();
}

// Uso: múltiplos statements na MESMA transação (INSERT + UPDATE atômico)
await using var uow = new UnitOfWork(connectionFactory);
await uow.BeginAsync();

var invite = await connection.QueryFirstOrDefaultAsync<CompanyInvite>(sql, p, uow.Transaction);
await connection.ExecuteAsync(insertUserSql, user, uow.Transaction);
await connection.ExecuteAsync(incrementSql, inviteId, uow.Transaction);

await uow.CommitAsync(); // qualquer exceção antes → rollback no dispose
```

Registro no startup e uso no repositório:

```csharp
// Program.cs
DefaultTypeMap.MatchNamesWithUnderscores = true; // schema snake_case → records PascalCase
builder.Services.AddSingleton<IDbConnectionFactory>(new NpgsqlConnectionFactory(connectionString));

// Repository
public class UserRepository(IDbConnectionFactory connectionFactory)
{
    public async Task<IEnumerable<User>> SearchUsers(string? name, string? email)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = SqlTemplateLoader.For<UserRepository>("Queries.SearchUsers")
            .Render(new { name, email });
        return await conn.QueryAsync<User>(query.Sql, query.Parameters);
    }
}
```

### Batch Insert — SqlBatch

Insert em batch Dapper-friendly com chunking automático (default 500, auto-ajuste para limite 2100 params SQL Server).

```csharp
// Footing.Framework/Data/SqlBatch.cs
public static class SqlBatch
{
    public static (string Sql, DynamicParameters Parameters) BuildBatchInsert<T>(string tableName, IEnumerable<T> items, string? columnsOverride = null);
    public static Task<int> InsertBatchAsync<T>(IDbConnection conn, string tableName, IEnumerable<T> items, IDbTransaction? tx = null, int batchSize = 500, CancellationToken ct = default);
    public static Task<int> InsertBatchAsync<T>(UnitOfWork uow, string tableName, IEnumerable<T> items, int batchSize = 500, CancellationToken ct = default);
}

// Uso com UnitOfWork (recomendado — atômico)
await using var uow = new UnitOfWork(factory);
await uow.BeginAsync();
var inserted = await SqlBatch.InsertBatchAsync(uow, "Users", users, batchSize: 500);
await uow.CommitAsync(); // rollback automático se não commitar

// Sem UnitOfWork (autocommit por chunk)
using var conn = factory.CreateConnection();
var inserted2 = await SqlBatch.InsertBatchAsync(conn, "Users", users);

// Build apenas SQL (para log/teste, sem DB)
var (sql, parms) = SqlBatch.BuildBatchInsert("Users", users);
// sql: INSERT INTO Users (Name, Age, Email) VALUES (@p0_Name, @p0_Age, @p0_Email), (@p1_Name, @p1_Age, @p1_Email), ...
await conn.ExecuteAsync(sql, parms);

// Filtrar colunas (ex: omitir Id auto-increment)
var (sql2, parms2) = SqlBatch.BuildBatchInsert("Users", users, columnsOverride: "Name,Age,Email");

// Chunking: 5000 rows × 3 cols → 10 statements (500 cada), ~30-50ms, <2100 params/chunk
```

> **DB-agnostic:** gera `INSERT INTO t (cols) VALUES (...)` parametrizado (`@p0_Col`) — funciona em PG/MySQL/SQLite/SQLServer sem `DataTable`/`SqlBulkCopy`.

### Type Handlers

Sistema para conversão personalizada de tipos entre banco e C#.

> **Dapper nativo:** use `SqlMapper.AddTypeHandler` diretamente. `TypeHandlerRegistry` foi **removido em 3.0.0** (era `[Obsolete]` desde `0.3.0-alpha`) — migrar para `SqlMapper.AddTypeHandler`.

```csharp
// Registro — Dapper nativo (global, uma vez na inicialização)
SqlMapper.AddTypeHandler(new BoolCharTypeHandler());
SqlMapper.AddTypeHandler(typeof(MyEnum), new StringEnumTypeHandler());

// Implemente ITypeHandler<T>
public class StringArrayHandler : ITypeHandler<string[]>
{
    public string[]? Parse(IDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return null;
        var raw = reader.GetString(ordinal);
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }

    public object? Serialize(string[]? value)
        => value is { Length: > 0 } ? string.Join(",", value) : null;
}

// Atalho com atributo na propriedade (mapeamento lógico)
public class User
{
    public int Id { get; set; }
    [TypeHandler(typeof(StringArrayHandler))]
    public string[]? Roles { get; set; }
}
```

Handlers built-in:

- `JsonTypeHandler` — retorna o valor JSON bruto do banco (funciona com `json`/`jsonb` nativos e strings)
- `EnumTypeHandler` — converte strings e inteiros para enum
- `StringEnumTypeHandler` — converte enum C# para/from string no banco (Dapper + framework). Registrar: `SqlMapper.AddTypeHandler(typeof(Enum), new StringEnumTypeHandler())`
- `BoolCharTypeHandler` — salva `bool` como `'S'`/`'N'` e lê `'S'`/`'N'` como `bool` (Dapper + framework). Registrar: `SqlMapper.AddTypeHandler(new BoolCharTypeHandler())`

> Removido em 3.0.0: `TypeHandlerRegistry` foi deletado — use `SqlMapper.AddTypeHandler` Dapper nativo. `ITypeHandler`/`ITypeHandler<T>` e handlers concretos permanecem.

### ColumnAttribute

> Removido em 0.3.0-alpha — use Dapper `CustomPropertyTypeMap`, `DefaultTypeMap.MatchNamesWithUnderscores = true` ou alias `AS` no SQL.

```csharp
// Antes (0.1.x — removido)
// [Column("user_id")] public int Id { get; set; }

// Depois — opção 1: snake_case global
DefaultTypeMap.MatchNamesWithUnderscores = true;
// Depois — opção 2: alias no SQL
// SELECT user_id AS Id, full_name AS Name FROM users
```

---

## DI e Atributos

### Auto-Scan com `AddDependencyWalk`

Escaneia assemblies por classes anotadas com `[Injectable]`, `[Service]` ou `[Repository]` e as registra automaticamente
no DI (assemblies explícitos, determinístico):

```csharp
services.AddDependencyWalk("MeuProjeto", configuration, typeof(Program).Assembly);
services.AddDependencyWalk("MeuProjeto", configuration, typeof(Program).Assembly, typeof(UserRepository).Assembly);
```

> Removido em 3.0.0: `AddDependencyWalkAuto` foi deletado (era `[Obsolete]` desde `0.3.0-alpha`) — use `AddDependencyWalk` com assemblies explícitos.

### Atributos

| Atributo       | Lifetime padrão      | Uso                                    |
|----------------|----------------------|----------------------------------------|
| `[Injectable]` | Obrigatório informar | Para qualquer classe que precise de DI |
| `[Service]`    | Transient            | Serviços sem estado                    |
| `[Repository]` | Scoped               | Acesso a dados                         |

```csharp
[Service]
public class UserService { ... }

[Repository]
public class UserRepository { ... }

[Injectable(InjectableLifetime.Singleton)]
public class CacheManager { ... }
```

### ResolverExtensions

O resolvedor do framework:

- Cria a instância via `ActivatorUtilities.CreateInstance` (resolve dependências do DI automaticamente)
- Injeta configurações via `[InjectConfig]`
- Executa métodos `[PostConstruct]`
- Registra listeners `[EventListener]` no EventBus

---

## Lifecycle

### PostConstruct

Método executado automaticamente após a construção da instância:

```csharp
public class EmailService
{
    private SmtpClient? _client;

    [PostConstruct]
    public void Initialize()
    {
        _client = new SmtpClient("smtp.example.com");
    }

    [PostConstruct]
    public async Task InitializeAsync()
    {
        _client = await CreateClientAsync();
    }
}
```

Suporta métodos síncronos e assíncronos (retornando `Task`).

### PreDestroy

Método executado antes da destruição (quando o escopo é descartado):

```csharp
public class EmailService : IDisposable
{
    private SmtpClient? _client;

    [PostConstruct]
    public void Initialize()
    {
        _client = new SmtpClient("smtp.example.com");
    }

    [PreDestroy]
    public void Cleanup()
    {
        _client?.Dispose();
    }
}
```

---

## Config

### InjectConfig

Injeta valores de `IConfiguration` diretamente em propriedades:

```csharp
public class EmailSettings
{
    [InjectConfig("Smtp:Host")]
    public string? Host { get; set; }

    [InjectConfig("Smtp:Port")]
    public int Port { get; set; }
}
```

O valor é lido de `IConfiguration` usando a chave informada no atributo.

---

## Observabilidade — Diagnostics (OTel) + Health

### Diagnostics — FootingActivitySource (OTel nativo 6 LOC, 0 deps)

```csharp
using Footing.Framework.Diagnostics;
using var a1 = FootingActivitySource.StartSqlTemplateRender("SELECT * FROM Users {WHERE} ...");
using var a2 = FootingActivitySource.StartEventBusPublish("UserCreatedEvent");
using var a3 = FootingActivitySource.StartSqlBatch("Users", 100);
// ActivitySource("Footing.Framework","3.1.0") — custo 0 sem listener (HasListeners guard)
// Configure OTel: services.AddOpenTelemetry().WithTracing(b=>b.AddSource("Footing.Framework"))
```

### Health — FootingHealthCheck (5 LOC, HealthChecks.Abstractions)

```csharp
services.AddFootingHealthChecks(); // registra DbHealthCheck + EventBusHealthCheck
// GET /health → DbHealthCheck (IDbConnectionFactory.CreateConnection().OpenAsync) + EventBusHealthCheck
app.MapHealthChecks("/health");
```

## Resiliência — Idempotency SPI + Outbox SPI (Spring Security pattern, agnóstico DB)

> **Framework só SPI (interface), app escolhe DB** — como `Spring Security UserDetailsService`. `0 PackageReference` `Npgsql/Firebird/OleDb/Redis` em `src`.

### Idempotency SPI (0.1 SP, 2 LOC, 0 deps)

```csharp
// Framework SPI — src/Footing.Framework/Idempotency/Idempotency.cs
public interface IIdempotencyStore{
  bool TryGet<T>(string key, out T? value);
  void Set<T>(string key, T value, TimeSpan? ttl=null);
  Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl=null, CancellationToken ct=default);
}

// App registra impl (Memory/Redis/Firebird/dbf) — exemplo Memory desacoplado (docs/snippets, não pack)
services.AddSingleton<IIdempotencyStore, MeuIdempotencyStore>(); // ex: RedisIdempotencyStore ou FirebirdIdempotencyStore ou Memory snippet

// Controller — Idempotency-Key header
[HttpPost("/pedidos")]
public async Task<IActionResult> Create([FromHeader(Name="Idempotency-Key")] string key, CreatePedidoDto dto, [FromServices] IIdempotencyStore store){
  if(string.IsNullOrEmpty(key)) return BadRequest("Idempotency-Key required");
  var pedido = await store.GetOrCreateAsync(key, () => handler.Create(dto), TimeSpan.FromHours(24));
  return Created($"/pedidos/{pedido.Id}", pedido);
}
```

### Outbox SPI + Processor (0.5 SP, 8 LOC, 0 deps)

```csharp
// Framework SPI + Processor — src/Footing.Framework/Outbox/Outbox.cs
public sealed record OutboxMessage(Guid Id,string Type,string Payload,DateTime CreatedAt,DateTime? ProcessedAt=null,int Attempts=0,string? LastError=null);
public interface IOutboxStore{
  Task SaveAsync(OutboxMessage m, IDbTransaction? tx=null, CancellationToken ct=default);
  Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batch=100, CancellationToken ct=default);
  Task MarkProcessedAsync(Guid id, CancellationToken ct=default);
  Task MarkFailedAsync(Guid id, string error, CancellationToken ct=default);
}
public sealed class OutboxProcessor(IOutboxStore store, EventBus.EventBus bus, ILogger<OutboxProcessor>? log=null):BackgroundService{
  public TimeSpan Interval{get;set;}=TimeSpan.FromSeconds(2);public int BatchSize{get;set;}=100;
  // poll GetUnprocessed → bus.Publish → MarkProcessed / MarkFailed
}

// App fornece impl DB — NPgSQL exemplo (docs, não pack)
public class NpgsqlOutboxStore(IDbConnectionFactory f):IOutboxStore{
  public Task SaveAsync(OutboxMessage m, IDbTransaction? tx=null, CancellationToken ct=default){
    var c=tx?.Connection??f.CreateConnection(); return c.ExecuteAsync("INSERT INTO Outbox VALUES (@Id,@Type,@Payload::jsonb,@CreatedAt)",m,tx);
  }
  public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int b=100,CancellationToken ct=default){
    using var c=f.CreateConnection(); return (await c.QueryAsync<OutboxMessage>("SELECT * FROM Outbox WHERE ProcessedAt IS NULL ORDER BY CreatedAt LIMIT @b FOR UPDATE SKIP LOCKED",new{b})).AsList();
  }
  public Task MarkProcessedAsync(Guid id,CancellationToken ct=default){using var c=f.CreateConnection();return c.ExecuteAsync("UPDATE Outbox SET ProcessedAt=NOW() WHERE Id=@id",new{id});}
  public Task MarkFailedAsync(Guid id,string e,CancellationToken ct=default){using var c=f.CreateConnection();return c.ExecuteAsync("UPDATE Outbox SET Attempts=Attempts+1,LastError=@e WHERE Id=@id",new{id,e});}
}
// Firebird: SELECT FIRST @b * FROM OUTBOX WHERE PROCESSED_AT IS NULL
// dbf: PROVIDER=VFPOLEDB + SELECT TOP @b * FROM outbox.dbf
// Registro Spring Security pattern:
services.AddSingleton<IOutboxStore, NpgsqlOutboxStore>(); // ou FirebirdOutboxStore ou DbfOutboxStore
services.AddHostedService<OutboxProcessor>(); // framework processor agnóstico

// Uso transacional — atomicidade sem loss (Commit ok Publish fail → retry poll)
await using var uow=new UnitOfWork(factory); await uow.BeginAsync();
await conn.ExecuteAsync("INSERT INTO Pedidos ...",pedido,uow.Transaction);
await outbox.SaveEventAsync(new PedidoCriadoEvent{PedidoId=pedido.Id},uow.Transaction); // mesma Tx
await uow.CommitAsync(); // Pedido+Outbox atômico → poll 2s → bus.Publish → MarkProcessed
```

> **Won't 3.1.1 (veto):** `Result<T> railway` (7 LOC, recurso linguagem), `PagedResult<T> + QueryPagedAsync` (5 LOC, precisa dialect), `IAuditable/ISoftDelete` (5 LOC, snippet docs). App faz `record Paginacao<T>` 5 LOC ou `if/throw DomainException` idiomático. Ver `MIGRATION.md §8.10.1` + `print-28.md`.

## Specification — ISpecification\<T\> (v3.4.2 agnóstico verificado)

```csharp
using Footing.Framework.Specification;
var ativo = SpecificationExtensions.Where<User>(u => u.Ativo);
var adulto = SpecificationExtensions.Where<User>(u => u.Age >= 18);
var spec = ativo.And(adulto).Where(u => u.Name != "");
// ou: ativo.Or(adulto).Not()
var filtrados = users.Where(spec.IsSatisfiedBy).ToList(); // composable, sem DB, sem ORM
```

## Migrations — SPI carteiro Y/N (v3.4.2 agnóstico 100% verificado)

> **Y/N 100% agnóstico:** `success CHAR(1) Y/N CHECK (success IN ('Y','N'))` em `docs/examples/Migrations/DDL/__migrations.sql` (PG CHAR(1), MSSQL CHAR(1), FB CHAR(1), dbf C(1)) — vence `BOOLEAN/BIT/INTEGER`. Alinha com `BoolCharYNTypeHandler` `bool→'Y'/'N'` `DbType.AnsiStringFixedLength Size=1`.

```csharp
// SPI tripartite — app fornece Journal (via IDbConnectionFactory)
public interface IMigrationJournal { Task EnsureHistoryTableAsync(CancellationToken ct=default); Task<IReadOnlyList<string>> GetAppliedVersionsAsync(CancellationToken ct=default); Task MarkAppliedAsync(MigrationInfo m, string checksum, long ms, string successYN, string? error, CancellationToken ct=default); /* + HasApplied/GetChecksum/Baseline/Repair/Info */ }
public interface IMigrationScriptProvider { IAsyncEnumerable<MigrationInfo> GetScriptsAsync(CancellationToken ct=default); } // FileSystem ou EmbeddedResource
public interface IMigrationRunner { Task<MigrationResult> MigrateAsync(CancellationToken ct=default); Task ValidateAsync(CancellationToken ct=default); Task<IReadOnlyList<MigrationStatus>> InfoAsync(CancellationToken ct=default); Task RepairAsync(CancellationToken ct=default); Task BaselineAsync(string v="0", CancellationToken ct=default); Task<string> GenerateScriptAsync(CancellationToken ct=default); }

// VersionParser sem regex (11→11) — V1__ , V1_0_1__ (_→.), R__ , R001__ , 001__ baseline
MigrationVersionParser.TryParse("V1_0_1__fix.sql", out var m); // Version=1.0.1, Type=VERSIONED
MigrationVersionParser.TryParse("R__views.sql", out var r);    // Type=REPEATABLE, checksum diff → reexecuta
MigrationVersionParser.TryParse("001__baseline.sql", out var b);// Type=BASELINE

// Checksum SHA256 drift fail-fast
var cs = MigrationChecksum.Compute(sql); // 64 hex

// Runner carteiro — placeholders Dictionary Replace {{schema}}
var runner = new MigrationRunner(journal, new FileSystemMigrationScriptProvider("Migrations"), factory, new MigrationOptions{ Placeholders = new Dictionary<string,string>{ ["schema"]="public"} });
await runner.MigrateAsync();             // bloqueia se success='N' pendente, catch → Insert N + throw MigrationException
await runner.ValidateAsync();            // drift
await runner.RepairAsync();              // DELETE WHERE success='N'
await runner.BaselineAsync("0");
var dry = await runner.GenerateScriptAsync(); // sem executar, DBA revisa

// TypeHandler Y/N
SqlMapper.AddTypeHandler(new BoolCharYNTypeHandler()); // bool ↔ 'Y'/'N'
```

DDL agnóstico: `CREATE TABLE __migrations (..., success CHAR(1) NOT NULL CHECK (success IN ('Y','N')), error_message VARCHAR(4000))` — `SELECT * WHERE success='Y'` / `success='N'` bloqueia / `DELETE WHERE success='N'` repair.

## Dependências

- .NET 10.0+
- Dapper 2.1.72
- Microsoft.Extensions.* 10.0.8 (sem `Caching.Memory` pós-veto)
