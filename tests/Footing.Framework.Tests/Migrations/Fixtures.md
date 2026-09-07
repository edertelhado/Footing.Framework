# Fixtures — 4 DBs Must + Como adicionar Could Have sem tocar src/

## Must 4 DBs (20 .sql)

```
Fixtures/
├── _Common/SCENARIOS.md
├── MigrationSqlite/   INTEGER PRIMARY KEY AUTOINCREMENT, TEXT
├── MigrationPg/       SERIAL, JSONB, UUID
├── MigrationMssql/    INT IDENTITY(1,1), NVARCHAR, DATETIME2
└── MigrationMysql/    INT AUTO_INCREMENT, ENGINE=InnoDB
```

Cada pasta 5 .sql: `V001 CRLF + V002 half + V003 DROP + V004 FK + R001 REPEATABLE` — mesma falha, só dialeto muda (ver `_Common/SCENARIOS.md`).

## Seleção runtime

```
FOOTING_TEST_PROVIDER=pg|mssql|mysql|sqlite
FOOTING_TEST_CONN_STRING="..."
# precedência: env var > appsettings.test.json > default sqlite:memory
```

Exemplos:

```bash
dotnet test                                  # sqlite:memory 389 Passed 0 Skipped
FOOTING_TEST_PROVIDER=pg FOOTING_TEST_CONN_STRING="Host=localhost;Port=5432;Database=footing_test;Username=postgres;Password=postgres" dotnet test --filter Provider
FOOTING_TEST_PROVIDER=mssql FOOTING_TEST_CONN_STRING="Server=localhost;Database=footing_test;User Id=sa;Password=***;TrustServerCertificate=true" dotnet test
FOOTING_TEST_PROVIDER=mysql FOOTING_TEST_CONN_STRING="Server=localhost;Database=footing_test;Uid=root;Pwd=***" dotnet test
```

Infra agnóstica: `docker/podman/lxc/bare metal/AWS/self-hosted` irrelevante — teste só consome `IDbConnection` via `TestProviderRegistry.Resolve`.

## Registry

`TestProviderRegistry.cs` com `Dictionary<string, Func<string, IDbConnectionFactory>> _registry` 4 entradas Must:

```csharp
["sqlite"] = cs => new SqliteInMemoryFactory()
["pg"]     = cs => CreateViaReflection(cs, "Npgsql.NpgsqlConnection, Npgsql")
["mssql"]  = cs => CreateViaReflection(cs, "Microsoft.Data.SqlClient.SqlConnection, Microsoft.Data.SqlClient")
["mysql"]  = cs => CreateViaReflection(cs, "MySql.Data.MySqlClient.MySqlConnection, MySqlConnector")
```

`CreateViaReflection` usa `Type.GetType + Activator.CreateInstance` — 0 PackageReference em `tests.csproj` (reflection).

## Como adicionar Could Have (Hana/Oracle/Fb/dbf) sem tocar src/

1. Criar pasta:

```bash
mkdir -p tests/Footing.Framework.Tests/Migrations/Fixtures/MigrationHana
cat > Fixtures/MigrationHana/V002__create_orders_and_seed.sql <<'SQL'
CREATE COLUMN TABLE orders (id INTEGER PRIMARY KEY, total DECIMAL(10,2));
INSERT INTO orders VALUES (1, 100.00);
INSERT INTO orders VALUES (1, 200.00);
SQL
# + V001 CRLF, V003 DROP, R001 REPEATABLE (SELECT 1 FROM DUMMY)
```

2. Registrar factory app-side:

```csharp
public sealed class HanaOdbcFactory : IDbConnectionFactory {
    private readonly string _cs;
    public HanaOdbcFactory(string cs) => _cs = cs;
    public IDbConnection CreateConnection() => new OdbcConnection(_cs);
}
TestProviderRegistry.Register("hana", cs => new HanaOdbcFactory(cs));
```

3. Rodar:

```bash
FOOTING_TEST_PROVIDER=hana FOOTING_TEST_CONN_STRING="Driver={HDBODBC};ServerNode=hana:39013;..." dotnet test --filter Hana
```

0 linhas em `src/Footing.Framework` — `MigrationRunner` continua carteiro SQL.

## Gates

```bash
grep -r Npgsql\|Firebird\|OleDb\|MySql src/Footing.Framework --include="*.cs" --include="*.csproj" && echo FAIL || echo PASS
dotnet test -c Release # 389 Passed 0 Skipped sem env var
ls Fixtures/Migration*/V*.sql | wc -l # 16 + 4 R001 =20
grep -c GeneratedRegex src/Footing.Framework/Data/SqlTemplate.cs # 11
find src -name "*.cs" ! -path "*/obj/*" | xargs wc -l | tail -1 # <3500
dotnet pack -c Release && ls -lh artifacts/*.nupkg # ~80K
```
