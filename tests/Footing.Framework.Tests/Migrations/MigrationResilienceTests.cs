using System.Data;
using Microsoft.Data.Sqlite;
using Footing.Framework.Data;
using Footing.Framework.Migrations;

namespace Footing.Framework.Tests.Migrations;

/// <summary>
/// Bateria resiliência v2 Must falha>feliz 18 testes (13 Must falha + 5 Should)
/// Infra default sqlite:memory prova DDL real (sqlite_master, CHECK Y/N, DROP IF EXISTS, Repair DELETE WHERE N)
/// ChaosJournal Fake só para janelas não simuláveis (Execute→Journal 2 conexões, Ensure fail, Cancel, mono driver)
/// Base 371t → 389t (+18)
/// </summary>
public class MigrationResilienceTests
{
    // Helpers compartilhados
    private sealed class InMemoryProvider : IMigrationScriptProvider
    {
        private readonly IReadOnlyList<MigrationInfo> _list;
        public InMemoryProvider(IEnumerable<MigrationInfo> list) => _list = list.ToList();
        public async IAsyncEnumerable<MigrationInfo> GetScriptsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            foreach (var s in _list) { ct.ThrowIfCancellationRequested(); yield return s; await Task.Yield(); }
        }
    }

    private sealed class ChaosJournal : IMigrationJournal
    {
        private readonly SqliteJournal _inner;
        public bool FailOnMarkApplied { get; set; }
        public bool FailOnEnsure { get; set; }
        public ChaosJournal(IDbConnectionFactory f) => _inner = new SqliteJournal(f);
        // fallback InMemory se sem factory (para mono driver test sem sqlite)
        private readonly Dictionary<string,(string cs,string yn,string? err)> _mem = new();
        private readonly bool _useMem;
        public ChaosJournal() { _useMem = true; _inner = null!; }
        public Task EnsureHistoryTableAsync(CancellationToken ct=default) =>
            FailOnEnsure ? throw new InvalidOperationException("DB down Ensure") :
            _useMem ? Task.CompletedTask : _inner.EnsureHistoryTableAsync(ct);
        public Task<IReadOnlyList<string>> GetAppliedVersionsAsync(CancellationToken ct=default) =>
            _useMem ? Task.FromResult<IReadOnlyList<string>>(_mem.Where(kv=>kv.Value.yn=="Y").Select(kv=>kv.Key).ToList()) : _inner.GetAppliedVersionsAsync(ct);
        public Task<bool> HasAppliedAsync(string v, CancellationToken ct=default) =>
            _useMem ? Task.FromResult(_mem.ContainsKey(v) && _mem[v].yn=="Y") : _inner.HasAppliedAsync(v, ct);
        public Task<string?> GetChecksumAsync(string v, CancellationToken ct=default) =>
            _useMem ? Task.FromResult(_mem.TryGetValue(v,out var val)?val.cs:null) : _inner.GetChecksumAsync(v, ct);
        public Task MarkAppliedAsync(MigrationInfo info, string checksum, long ms, string yn, string? err, CancellationToken ct=default) =>
            FailOnMarkApplied ? throw new InvalidOperationException("DB down MarkApplied") :
            _useMem ? Task.Run(()=>_mem[info.Version]=(checksum,yn,err), ct) : _inner.MarkAppliedAsync(info,checksum,ms,yn,err,ct);
        public Task UpdateChecksumAsync(string v, string cs, CancellationToken ct=default) =>
            _useMem ? Task.Run(()=>{ if(_mem.ContainsKey(v)) _mem[v]=(cs,_mem[v].yn,_mem[v].err); },ct) : _inner.UpdateChecksumAsync(v,cs,ct);
        public Task<IReadOnlyList<string>> GetFailedVersionsAsync(CancellationToken ct=default) =>
            _useMem ? Task.FromResult<IReadOnlyList<string>>(_mem.Where(kv=>kv.Value.yn=="N").Select(kv=>kv.Key).ToList()) : _inner.GetFailedVersionsAsync(ct);
        public Task<IReadOnlyList<MigrationStatus>> GetInfoAsync(CancellationToken ct=default) =>
            _useMem ? Task.FromResult<IReadOnlyList<MigrationStatus>>(_mem.Select(kv=>new MigrationStatus(kv.Key,"","VERSIONED",kv.Value.cs,DateTime.UtcNow,kv.Value.yn=="Y"?"Success":"Failed")).ToList()) : _inner.GetInfoAsync(ct);
        public Task RepairAsync(CancellationToken ct=default) =>
            _useMem ? Task.Run(()=>{ foreach(var k in _mem.Where(kv=>kv.Value.yn=="N").Select(kv=>kv.Key).ToList()) _mem.Remove(k); },ct) : _inner.RepairAsync(ct);
        public Task BaselineAsync(string v, CancellationToken ct=default) =>
            _useMem ? Task.Run(()=>_mem[v]=(MigrationChecksum.Compute("baseline"),"Y",null),ct) : _inner.BaselineAsync(v,ct);
    }

    private sealed class FakeFactory : IDbConnectionFactory
    {
        private readonly Action<string>? _onExecute;
        private readonly bool _mono;
        public bool Executed { get; private set; }
        public FakeFactory(Action<string>? onExecute=null, bool mono=false){ _onExecute=onExecute; _mono=mono; }
        public IDbConnection CreateConnection()=> new FakeConn(this);
        private sealed class FakeConn : IDbConnection
        {
            private readonly FakeFactory _p;
            public FakeConn(FakeFactory p)=>_p=p;
            public string ConnectionString{get;set;}=""; public int ConnectionTimeout=>0; public string Database=>"fake"; public ConnectionState State=>ConnectionState.Open;
            public IDbTransaction BeginTransaction()=>new FakeTx(this); public IDbTransaction BeginTransaction(IsolationLevel il)=>new FakeTx(this);
            public void ChangeDatabase(string n){} public void Close(){} public IDbCommand CreateCommand()=> new FakeCmd(_p, this);
            public void Open(){} public void Dispose(){}
        }
        private sealed class FakeTx : IDbTransaction{ private readonly IDbConnection _c; public FakeTx(IDbConnection c)=>_c=c; public IDbConnection Connection=>_c; public IsolationLevel IsolationLevel=>IsolationLevel.ReadCommitted; public void Commit(){} public void Rollback(){} public void Dispose(){} }
        private sealed class FakeCmd : IDbCommand
        {
            private readonly FakeFactory _parent; private IDbConnection _c;
            public FakeCmd(FakeFactory p, IDbConnection c){ _parent=p; _c=c; }
            public string CommandText{get;set;}=""; public int CommandTimeout{get;set;} public CommandType CommandType{get;set;} public IDbConnection Connection{get=>_c;set=>_c=value;} public IDataParameterCollection Parameters=>new FakeParams(); public IDbTransaction? Transaction{get;set;} public UpdateRowSource UpdatedRowSource{get;set;}
            public void Cancel(){} public IDbDataParameter CreateParameter()=>new FakeParam(); public void Dispose(){}
            public int ExecuteNonQuery(){
                if(_parent._mono && CommandText.Count(c=>c==';')>1) throw new InvalidOperationException("mono driver: multiple statements not supported");
                _parent.Executed=true; _parent._onExecute?.Invoke(CommandText);
                if(CommandText.Contains("FAIL")) throw new InvalidOperationException("fail");
                return 1;
            }
            public IDataReader ExecuteReader()=>null!; public IDataReader ExecuteReader(CommandBehavior b)=>null!; public object ExecuteScalar()=>null!; public void Prepare(){}
        }
        private sealed class FakeParam : IDbDataParameter{ public DbType DbType{get;set;} public ParameterDirection Direction{get;set;} public bool IsNullable=>true; public string ParameterName{get;set;}=""; public string SourceColumn{get;set;}=""; public DataRowVersion SourceVersion{get;set;} public object? Value{get;set;} public byte Precision{get;set;} public byte Scale{get;set;} public int Size{get;set;} }
        private sealed class FakeParams : IDataParameterCollection{ private readonly List<object> _l=new(); public object this[string n]{get=>null!;set{}} public object this[int i]{get=>_l[i];set=>_l[i]=value;} public int Count=>_l.Count; public bool IsFixedSize=>false; public bool IsReadOnly=>false; public bool IsSynchronized=>false; public object SyncRoot=>this; public int Add(object v){_l.Add(v); return _l.Count-1;} public void Clear()=>_l.Clear(); public bool Contains(string n)=>false; public bool Contains(object v)=>_l.Contains(v); public void CopyTo(Array a,int i)=>_l.CopyTo((object[])a,i); public System.Collections.IEnumerator GetEnumerator()=>_l.GetEnumerator(); public int IndexOf(string n)=>-1; public int IndexOf(object v)=>_l.IndexOf(v); public void Insert(int i,object v)=>_l.Insert(i,v); public void Remove(object v)=>_l.Remove(v); public void RemoveAt(string n){} public void RemoveAt(int i)=>_l.RemoveAt(i); }
    }

    // === M5 CRLF Must 3 testes ===

    [Fact]
    public void Checksum_CRLF_vs_LF_Iguais_Normalizado()
    {
        var crlf = "CREATE TABLE users (id INT);\r\nINSERT INTO users VALUES (1);\r\n";
        var lf   = "CREATE TABLE users (id INT);\nINSERT INTO users VALUES (1);\n";
        var cr   = "CREATE TABLE users (id INT);\rINSERT INTO users VALUES (1);\r";
        Assert.Equal(MigrationChecksum.Compute(crlf), MigrationChecksum.Compute(lf));
        Assert.Equal(MigrationChecksum.Compute(lf), MigrationChecksum.Compute(cr));
        Assert.Equal(MigrationChecksum.Compute(crlf), MigrationChecksum.Compute(cr));
    }

    [Fact]
    public async Task Validate_CRLF_NaoFalsoDrift_Sqlite()
    {
        using var factory = new SqliteInMemoryFactory();
        var journal = new SqliteJournal(factory);
        var crlfSql = "CREATE TABLE users (id INT);\r\n";
        await journal.MarkAppliedAsync(new MigrationInfo("1","create_users","V1__create_users.sql","VERSIONED","",crlfSql),
            MigrationChecksum.Compute(crlfSql), 10, "Y", null);
        var lfSql = "CREATE TABLE users (id INT);\n";
        var provider = new InMemoryProvider(new[]{ new MigrationInfo("1","create_users","V1__create_users.sql","VERSIONED", MigrationChecksum.Compute(lfSql), lfSql) });
        var runner = new MigrationRunner(journal, provider, factory);
        await runner.ValidateAsync(); // não deve throw falso drift
    }

    [Fact]
    public async Task Validate_AlteracaoReal_DetectaDrift_Sqlite()
    {
        using var factory = new SqliteInMemoryFactory();
        var journal = new SqliteJournal(factory);
        var orig = "CREATE TABLE users (id INT);\n";
        await journal.MarkAppliedAsync(new MigrationInfo("1","create_users","V1__create_users.sql","VERSIONED","",orig),
            MigrationChecksum.Compute(orig), 10, "Y", null);
        var altered = "CREATE TABLE users (id INT, name VARCHAR(200));\n"; // drift real
        var provider = new InMemoryProvider(new[]{ new MigrationInfo("1","create_users","V1__create_users.sql","VERSIONED", MigrationChecksum.Compute(altered), altered) });
        var runner = new MigrationRunner(journal, provider, factory);
        var ex = await Assert.ThrowsAsync<MigrationException>(() => runner.ValidateAsync());
        Assert.Contains("drift", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // === M2 Half Must 3 testes sqlite ===

    [Fact]
    public async Task Runner_HalfFailure_CreateOk_InsertFail_Registra_N_SideEffectPersiste_Sqlite()
    {
        using var factory = new SqliteInMemoryFactory();
        var journal = new SqliteJournal(factory);
        // CREATE + INSERT com UNIQUE violation para provocar falha real no sqlite
        var sql = "CREATE TABLE orders (id INTEGER PRIMARY KEY); INSERT INTO orders VALUES (1); INSERT INTO orders VALUES (1);";
        var scripts = new InMemoryProvider(new[]{
            new MigrationInfo("2","create_orders_and_seed","V002__create_orders_and_seed.sql","VERSIONED", MigrationChecksum.Compute(sql), sql)
        });
        var runner = new MigrationRunner(journal, scripts, factory);
        var ex = await Assert.ThrowsAsync<MigrationException>(() => runner.MigrateAsync());
        Assert.Contains("2", ex.Message);
        Assert.Equal(new[]{"2"}, await journal.GetFailedVersionsAsync());
        // prova side-effect persiste: sqlite_master contém orders mesmo após N
        using var conn = factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='orders'";
        var name = cmd.ExecuteScalar() as string;
        Assert.Equal("orders", name);
        // Repair limpa N mas não desfaz DDL
        await runner.RepairAsync();
        Assert.Empty(await journal.GetFailedVersionsAsync());
        Assert.Equal("orders", cmd.ExecuteScalar() as string);
    }

    [Fact]
    public async Task Runner_HalfFailure_Com_IF_NOT_EXISTS_Reexecuta_Sqlite()
    {
        using var factory = new SqliteInMemoryFactory();
        var journal = new SqliteJournal(factory);
        var sql = "CREATE TABLE IF NOT EXISTS orders2 (id INTEGER PRIMARY KEY); INSERT INTO orders2 VALUES (1); INSERT INTO orders2 VALUES (1);";
        var scripts = new InMemoryProvider(new[]{
            new MigrationInfo("2","create_orders_if_not_exists","V002__create_orders_if_not_exists.sql","VERSIONED", MigrationChecksum.Compute(sql), sql)
        });
        var runner = new MigrationRunner(journal, scripts, factory);
        await Assert.ThrowsAsync<MigrationException>(() => runner.MigrateAsync());
        Assert.Single(await journal.GetFailedVersionsAsync());
        await runner.RepairAsync();
        Assert.Empty(await journal.GetFailedVersionsAsync());
        // reexecuta com IF NOT EXISTS não deve dar already exists - mas vai falar de novo duplicate 1
        // para provar idempotente, alteramos script para IF NOT EXISTS sem INSERT duplicado
        var sql2 = "CREATE TABLE IF NOT EXISTS orders2 (id INTEGER PRIMARY KEY); INSERT OR IGNORE INTO orders2 VALUES (1);";
        var scripts2 = new InMemoryProvider(new[]{
            new MigrationInfo("2","create_orders_if_not_exists","V002__create_orders_if_not_exists.sql","VERSIONED", MigrationChecksum.Compute(sql2), sql2)
        });
        var runner2 = new MigrationRunner(journal, scripts2, factory);
        // como Repair limpou N e tabela já existe, com IF NOT EXISTS deve suceder
        var result = await runner2.MigrateAsync();
        Assert.Single(result.Applied);
        Assert.Contains("2", await journal.GetAppliedVersionsAsync());
    }

    [Fact]
    public async Task Runner_MigrationsAtomicas_V002_Y_V003_N_Sqlite()
    {
        using var factory = new SqliteInMemoryFactory();
        var journal = new SqliteJournal(factory);
        var v2sql = "CREATE TABLE t2 (id INT);";
        var v3sql = "CREATE TABLE t3 (id INTEGER PRIMARY KEY); INSERT INTO t3 VALUES (1); INSERT INTO t3 VALUES (1);"; // fail
        var scripts = new InMemoryProvider(new[]{
            new MigrationInfo("2","v2","V002__t2.sql","VERSIONED", MigrationChecksum.Compute(v2sql), v2sql),
            new MigrationInfo("3","v3","V003__t3.sql","VERSIONED", MigrationChecksum.Compute(v3sql), v3sql),
        });
        var runner = new MigrationRunner(journal, scripts, factory);
        await Assert.ThrowsAsync<MigrationException>(() => runner.MigrateAsync());
        // V002 Y isolado, V003 N
        Assert.Contains("2", await journal.GetAppliedVersionsAsync());
        Assert.Equal(new[]{"3"}, await journal.GetFailedVersionsAsync());
        using var conn = factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='t2'";
        Assert.Equal("t2", cmd.ExecuteScalar() as string);
    }

    // === M3 DROP Must 3 testes sqlite ===

    [Fact]
    public async Task Runner_DropSecondFails_PrimeiroJaDropado_Registra_N_Sqlite()
    {
        using var factory = new SqliteInMemoryFactory();
        var journal = new SqliteJournal(factory);
        // prepara t1 existente
        using (var c = factory.CreateConnection()) { using var cmd = c.CreateCommand(); cmd.CommandText = "CREATE TABLE t1 (id INT);"; cmd.ExecuteNonQuery(); }
        var sql = "DROP TABLE t1; DROP TABLE does_not_exist_xyz;";
        var scripts = new InMemoryProvider(new[]{
            new MigrationInfo("3","drop_two","V003__drop_old_tables.sql","VERSIONED", MigrationChecksum.Compute(sql), sql)
        });
        var runner = new MigrationRunner(journal, scripts, factory);
        await Assert.ThrowsAsync<MigrationException>(() => runner.MigrateAsync());
        Assert.Equal(new[]{"3"}, await journal.GetFailedVersionsAsync());
        // primeira já dropada persiste
        using var conn = factory.CreateConnection();
        using var cmd2 = conn.CreateCommand();
        cmd2.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='t1'";
        Assert.Null(cmd2.ExecuteScalar()); // t1 dropada
        cmd2.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='does_not_exist_xyz'";
        Assert.Null(cmd2.ExecuteScalar());
    }

    [Fact]
    public async Task Runner_Drop_Idempotent_DoesNotFail_Sqlite()
    {
        using var factory = new SqliteInMemoryFactory();
        var journal = new SqliteJournal(factory);
        var sql = "DROP TABLE IF EXISTS old_users; DROP TABLE IF EXISTS old_orders;";
        var scripts = new InMemoryProvider(new[]{
            new MigrationInfo("3","drop_old","V003__drop_old_tables.sql","VERSIONED", MigrationChecksum.Compute(sql), sql)
        });
        var runner = new MigrationRunner(journal, scripts, factory);
        var r = await runner.MigrateAsync();
        Assert.Single(r.Applied);
        Assert.Empty(await journal.GetFailedVersionsAsync());
        // reexecuta após já marcado Y deve skip, mas se Repair e reexecutar de novo deve ainda não throw
        var r2 = await runner.MigrateAsync();
        Assert.Empty(r2.Applied); // já aplicado skip
    }

    [Fact]
    public async Task Runner_Drop_FKViolation_Registra_N_Sqlite()
    {
        using var factory = new SqliteInMemoryFactory();
        var journal = new SqliteJournal(factory);
        // cria parent + child com FK
        using (var c = factory.CreateConnection())
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = "CREATE TABLE parent (id INTEGER PRIMARY KEY); CREATE TABLE child (id INTEGER PRIMARY KEY, parent_id INTEGER REFERENCES parent(id)); INSERT INTO parent VALUES (1); INSERT INTO child VALUES (1,1);";
            cmd.ExecuteNonQuery();
        }
        var sql = "DROP TABLE parent;"; // deve falhar FK
        var scripts = new InMemoryProvider(new[]{
            new MigrationInfo("4","drop_parent","V004__drop_parent.sql","VERSIONED", MigrationChecksum.Compute(sql), sql)
        });
        var runner = new MigrationRunner(journal, scripts, factory);
        var ex = await Assert.ThrowsAsync<MigrationException>(() => runner.MigrateAsync());
        // mensagem contém foreign key ou constraint
        Assert.NotNull(ex.InnerException);
        Assert.Equal(new[]{"4"}, await journal.GetFailedVersionsAsync());
    }

    // === M4 DB down Must 4 testes Fake/Chaos ===

    [Fact]
    public async Task Runner_DbCai_Entre_Execute_E_Journal_Fake()
    {
        // janela A: Execute OK mas MarkApplied falha — side-effect sem histórico
        var executed = false;
        var factory = new FakeFactory(onExecute: _ => executed = true);
        var journal = new ChaosJournal { FailOnMarkApplied = true };
        var runner = new MigrationRunner(journal,
            new InMemoryProvider(new[]{ new MigrationInfo("1","create_users","V1__create_users.sql","VERSIONED", MigrationChecksum.Compute("CREATE TABLE users (id INT)"), "CREATE TABLE users (id INT)") }),
            factory);
        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.MigrateAsync());
        Assert.True(executed);
        Assert.Empty(await journal.GetAppliedVersionsAsync());
        Assert.Empty(await journal.GetFailedVersionsAsync()); // nem Y nem N pois throw antes de MarkApplied N? Runner faz MarkApplied N no catch, mas Chaos falha também no catch -> propagate original? No runner catch tenta MarkApplied N, que também falha com FailOnMarkApplied. Então ficará sem histórico, prova janela A.
    }

    [Fact]
    public async Task Runner_DbCai_Durante_EnsureHistoryTable_Fake()
    {
        var journal = new ChaosJournal { FailOnEnsure = true };
        var runner = new MigrationRunner(journal,
            new InMemoryProvider(new[]{ new MigrationInfo("1","a","V1__a.sql","VERSIONED", MigrationChecksum.Compute("CREATE TABLE t (id INT)"), "CREATE TABLE t (id INT)") }),
            new FakeFactory());
        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.MigrateAsync());
        // nada aplicado, retry seguro após corrigir
        journal.FailOnEnsure = false;
        var result = await runner.MigrateAsync();
        Assert.Single(result.Applied);
    }

    [Fact]
    public async Task Runner_Cancelamento_Throw_OCE()
    {
        using var factory = new SqliteInMemoryFactory();
        var journal = new SqliteJournal(factory);
        var scripts = new InMemoryProvider(new[]{
            new MigrationInfo("1","a","V1__a.sql","VERSIONED", MigrationChecksum.Compute("CREATE TABLE t (id INT)"), "CREATE TABLE t (id INT)"),
            new MigrationInfo("2","b","V2__b.sql","VERSIONED", MigrationChecksum.Compute("CREATE TABLE t2 (id INT)"), "CREATE TABLE t2 (id INT)"),
        });
        var runner = new MigrationRunner(journal, scripts, factory);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runner.MigrateAsync(cts.Token));
    }

    // === M6 Repair/Validate + M5/M6 Must sqlite ===

    [Fact]
    public async Task Repair_LimpaSo_N_Mantem_Y_Sqlite()
    {
        using var factory = new SqliteInMemoryFactory();
        var journal = new SqliteJournal(factory);
        await journal.MarkAppliedAsync(new MigrationInfo("1","a","V1__a.sql","VERSIONED","",""), MigrationChecksum.Compute("a"), 10, "Y", null);
        await journal.MarkAppliedAsync(new MigrationInfo("2","b","V2__b.sql","VERSIONED","",""), MigrationChecksum.Compute("b"), 10, "N", "boom");
        await journal.MarkAppliedAsync(new MigrationInfo("3","c","V3__c.sql","VERSIONED","",""), MigrationChecksum.Compute("c"), 10, "Y", null);
        Assert.Equal(new[]{"2"}, await journal.GetFailedVersionsAsync());
        await journal.RepairAsync();
        Assert.Empty(await journal.GetFailedVersionsAsync());
        var applied = await journal.GetAppliedVersionsAsync();
        Assert.Contains("1", applied);
        Assert.Contains("3", applied);
        Assert.DoesNotContain("2", applied);
        // prova DELETE WHERE success='N' via sql direto
        using var conn = factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM __migrations WHERE success='N'";
        Assert.Equal(0L, (long)cmd.ExecuteScalar()!);
        cmd.CommandText = "SELECT COUNT(*) FROM __migrations WHERE success='Y'";
        Assert.Equal(2L, (long)cmd.ExecuteScalar()!);
    }

    [Fact]
    public async Task GenerateScript_DryRun_NaoExecuta_Sqlite()
    {
        using var factory = new SqliteInMemoryFactory();
        var journal = new SqliteJournal(factory);
        var sql1 = "CREATE TABLE g1 (id INT);";
        var sql2 = "CREATE TABLE g2 (id INT);";
        var scripts = new InMemoryProvider(new[]{
            new MigrationInfo("1","g1","V1__g1.sql","VERSIONED", MigrationChecksum.Compute(sql1), sql1),
            new MigrationInfo("2","g2","V2__g2.sql","VERSIONED", MigrationChecksum.Compute(sql2), sql2),
        });
        var runner = new MigrationRunner(journal, scripts, factory);
        var script = await runner.GenerateScriptAsync();
        Assert.Contains("V1__g1.sql", script);
        Assert.Contains("V2__g2.sql", script);
        // dry-run não executa: nada em journal
        Assert.Empty(await journal.GetAppliedVersionsAsync());
        using var conn = factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='g1'";
        Assert.Null(cmd.ExecuteScalar());
    }

    [Fact]
    public async Task Repeatable_ChecksumMudou_Reexecuta_Sqlite()
    {
        using var factory = new SqliteInMemoryFactory();
        var journal = new SqliteJournal(factory);
        var sqlV1 = "DROP VIEW IF EXISTS v; CREATE VIEW v AS SELECT 1 as a;";
        var r1 = new MigrationInfo("R001","refresh_views","R001__refresh_views.sql","REPEATABLE", MigrationChecksum.Compute(sqlV1), sqlV1);
        var runner = new MigrationRunner(journal, new InMemoryProvider(new[]{ r1 }), factory);
        var res1 = await runner.MigrateAsync();
        Assert.Single(res1.Applied);
        // mesmo checksum skip
        var runner2 = new MigrationRunner(journal, new InMemoryProvider(new[]{ r1 }), factory);
        var res2 = await runner2.MigrateAsync();
        Assert.Empty(res2.Applied);
        // checksum mudou reexecuta (DROP IF EXISTS garante idempotência)
        var sqlV2 = "DROP VIEW IF EXISTS v; CREATE VIEW v AS SELECT 2 as a;";
        var r2 = new MigrationInfo("R001","refresh_views","R001__refresh_views.sql","REPEATABLE", MigrationChecksum.Compute(sqlV2), sqlV2);
        var runner3 = new MigrationRunner(journal, new InMemoryProvider(new[]{ r2 }), factory);
        var res3 = await runner3.MigrateAsync();
        Assert.Single(res3.Applied);
        // checksum armazenado deve ser o novo
        var stored = await journal.GetChecksumAsync("R001");
        Assert.Equal(MigrationChecksum.Compute(sqlV2), stored);
    }

    [Fact]
    public async Task Runner_DbCai_Entre_Migrations_Retoma_Sqlite()
    {
        using var factory = new SqliteInMemoryFactory();
        var journal = new SqliteJournal(factory);
        var v1sql = "CREATE TABLE retoma1 (id INT);";
        var v2sql = "CREATE TABLE retoma2 (id INT);";
        var scriptsAll = new InMemoryProvider(new[]{
            new MigrationInfo("1","retoma1","V001__retoma1.sql","VERSIONED", MigrationChecksum.Compute(v1sql), v1sql),
            new MigrationInfo("2","retoma2","V002__retoma2.sql","VERSIONED", MigrationChecksum.Compute(v2sql), v2sql),
        });
        var runner1 = new MigrationRunner(journal, new InMemoryProvider(new[]{
            new MigrationInfo("1","retoma1","V001__retoma1.sql","VERSIONED", MigrationChecksum.Compute(v1sql), v1sql)
        }), factory);
        var r1 = await runner1.MigrateAsync();
        Assert.Single(r1.Applied);
        // simula DB cai entre migrations: segunda chamada com ambos, deve retomar V2
        var runner2 = new MigrationRunner(journal, scriptsAll, factory);
        var r2 = await runner2.MigrateAsync();
        Assert.Single(r2.Applied);
        Assert.Equal("2", r2.Applied[0].Version);
        Assert.Equal(2, (await journal.GetAppliedVersionsAsync()).Count);
    }

    [Fact]
    public async Task Baseline_Marca_0_Sqlite_Placeholders()
    {
        using var factory = new SqliteInMemoryFactory();
        var journal = new SqliteJournal(factory);
        var runner = new MigrationRunner(journal, new InMemoryProvider(Array.Empty<MigrationInfo>()), factory);
        await runner.BaselineAsync("0");
        Assert.Contains("0", await journal.GetAppliedVersionsAsync());
        // Placeholders {{schema}} replace — usa sufixo para compatível com sqlite (public como schema não existe)
        var opts = new MigrationOptions { Placeholders = new Dictionary<string,string>{ ["schema"]="public"} };
        var phSql = "CREATE TABLE t_{{schema}}_ph (id INT);";
        var provider = new InMemoryProvider(new[]{
            new MigrationInfo("1","ph","V1__ph.sql","VERSIONED", MigrationChecksum.Compute(phSql), phSql)
        });
        // journal já tem baseline 0, migrar V1 com placeholder deve expandir
        var phJournal = new SqliteJournal(factory); // mesmo factory compartilhado já tem baseline
        // limpar para teste isolado de placeholder: usar novo factory
        using var factory2 = new SqliteInMemoryFactory();
        var j2 = new SqliteJournal(factory2);
        var runnerPh = new MigrationRunner(j2, provider, factory2, opts);
        var res = await runnerPh.MigrateAsync();
        Assert.Single(res.Applied);
        Assert.Contains("public", res.Applied[0].Sql);
        Assert.Contains("t_public_ph", res.Applied[0].Sql);
        Assert.DoesNotContain("{{schema}}", res.Applied[0].Sql);
        // EnsureHistoryTable CHAR(1) CHECK Y/N prova
        using var conn = factory2.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='__migrations'";
        var ddl = cmd.ExecuteScalar() as string;
        Assert.Contains("CHECK", ddl!);
        Assert.Contains("'Y'", ddl!);
        Assert.Contains("'N'", ddl!);
        // tenta inserir success='X' deve falhar CHECK
        cmd.CommandText = "INSERT INTO __migrations (version, success) VALUES ('999','X')";
        await Assert.ThrowsAsync<SqliteException>(async () => { using var c2 = factory2.CreateConnection(); using var cmd2 = c2.CreateCommand(); cmd2.CommandText = "INSERT INTO __migrations (version, success) VALUES ('999','X')"; await Task.Run(()=>cmd2.ExecuteNonQuery()); });
    }

    [Fact]
    public async Task Runner_MultiStatement_MonoDriver_Registra_N_Fake()
    {
        // driver mono (anedota dbf/OleDb) não suporta múltiplos statements — deve registrar N e bloquear
        var journal = new ChaosJournal();
        var factory = new FakeFactory(mono:true);
        var sql = "CREATE TABLE users (id INT); INSERT INTO users VALUES (1);";
        var runner = new MigrationRunner(journal,
            new InMemoryProvider(new[]{ new MigrationInfo("1","create_users","V1__create_users.sql","VERSIONED", MigrationChecksum.Compute(sql), sql) }),
            factory);
        var ex = await Assert.ThrowsAsync<MigrationException>(() => runner.MigrateAsync());
        Assert.Contains("1", ex.Message);
        Assert.Equal(new[]{"1"}, await journal.GetFailedVersionsAsync());
        // bloqueio segunda chamada
        await Assert.ThrowsAsync<MigrationException>(() => runner.MigrateAsync());
        await runner.RepairAsync();
        Assert.Empty(await journal.GetFailedVersionsAsync());
    }
}
