# SCENARIOS — Paridade 4 DBs Must (sqlite|pg|mssql|mysql) + Could Have (hana/oracle/fb)

> Cada pasta `Migration{Provider}/` replica mesmas 5 características — só dialeto muda.
> Runner é carteiro SQL (`IDbConnectionFactory` + `IDbCommand.ExecuteNonQuery()`), não sabe dialeto.

## 5 Características (paridade)

| # | Característica | Objetivo | Como provocar falha | Esperado |
|---|----------------|----------|---------------------|----------|
| 1 | **CRLF drift** | Provar `MigrationChecksum.Compute` normaliza `\r\n` vs `\n` | `V001` com `\r\n` no disco vs memória `\n` | `ValidateAsync` não deve dar falso Drift |
| 2 | **HALF: CREATE OK + INSERT FAIL** | Provar side-effect persiste (autocommit), `success='N'` bloqueia | `V002` `CREATE TABLE orders; INSERT 1; INSERT 1` (dup PK) | `GetFailedVersions=N`, `sqlite_master` contém orders, `Repair DELETE WHERE N` limpa |
| 3 | **DROP idempotente vs não** | Provar `DROP TABLE t1; DROP TABLE xyz` falha 2º e persiste 1º | `V003` `DROP TABLE t1; DROP TABLE does_not_exist_xyz;` | `t1` dropada persiste, `N` registra, bloqueia até Repair; `DROP IF EXISTS` idempotente não falha |
| 4 | **DROP FK violation** | Provar FK bloqueia DROP | `V004` `DROP TABLE parent` com child FK existente | `N` registrado, `InnerException` contém FK/constraint |
| 5 | **200+ types + REPEATABLE** | Provar tipos por dialeto + `R001` reexecuta se checksum mudou | `V001` com tipos específicos + `R001` `DROP VIEW IF EXISTS v; CREATE VIEW v AS SELECT 1` | `V001` cria com tipos corretos; `R001` segunda execução skip se mesmo checksum, reexecuta se novo |

## Dialetos

| Provider | V001 PK | Tipos | Exemplo half V002 |
|----------|---------|-------|-------------------|
| **Sqlite** | `INTEGER PRIMARY KEY AUTOINCREMENT` | `TEXT, NUMERIC, DATETIME` | `CREATE TABLE orders (id INTEGER PRIMARY KEY, total NUMERIC(10,2)); INSERT 1; INSERT 1` |
| **Pg** | `SERIAL PRIMARY KEY` | `VARCHAR, JSONB, UUID, TIMESTAMP` | `CREATE TABLE orders (id INTEGER PRIMARY KEY, total NUMERIC(10,2)); ...` |
| **Mssql** | `INT IDENTITY(1,1) PRIMARY KEY` | `NVARCHAR, DECIMAL, DATETIME2, NVARCHAR(MAX)` | `CREATE TABLE orders (id INT PRIMARY KEY, total DECIMAL(10,2)); ...` |
| **Mysql** | `INT AUTO_INCREMENT PRIMARY KEY` | `VARCHAR, DECIMAL, TIMESTAMP, JSON` | `CREATE TABLE orders (id INT PRIMARY KEY, total DECIMAL(10,2)) ENGINE=InnoDB; ...` |

## Arquivos por pasta

```
MigrationSqlite/
├── V001__create_users.sql              # CRLF + 200+ types Sqlite
├── V002__create_orders_and_seed.sql    # HALF
├── V003__drop_old_tables.sql           # DROP half
├── V004__drop_parent_fk.sql            # FK
└── R001__refresh_views.sql             # REPEATABLE

MigrationPg/       idem SERIAL/JSONB
MigrationMssql/    idem IDENTITY/NVARCHAR
MigrationMysql/    idem AUTO_INCREMENT/ENGINE=InnoDB
```

## Seleção runtime

```
FOOTING_TEST_PROVIDER=pg|mssql|mysql|sqlite   # Must 4
FOOTING_TEST_CONN_STRING="..."
# precedência: env var > appsettings.test.json > default sqlite:memory
dotnet test                                    # 389 Passed sqlite:memory
FOOTING_TEST_PROVIDER=pg FOOTING_TEST_CONN_STRING="Host=..." dotnet test --filter Provider
```

Infra agnóstica: `docker/podman/lxc/bare metal/AWS/self-hosted` irrelevante — só `IDbConnection`.

## Could Have (adicionar sem tocar src/)

```bash
mkdir -p Fixtures/MigrationHana
cat > Fixtures/MigrationHana/V002__create_orders_and_seed.sql <<'SQL'
CREATE COLUMN TABLE orders (id INTEGER PRIMARY KEY, total DECIMAL(10,2));
INSERT INTO orders VALUES (1, 100.00);
INSERT INTO orders VALUES (1, 200.00);
SQL
TestProviderRegistry.Register("hana", cs => new HanaOdbcFactory(cs)); # OdbcConnection
FOOTING_TEST_PROVIDER=hana FOOTING_TEST_CONN_STRING="Driver={HDBODBC};..." dotnet test --filter Hana
# 0 linhas em src/Footing.Framework
```
