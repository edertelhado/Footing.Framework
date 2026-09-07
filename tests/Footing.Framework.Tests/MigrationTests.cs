using System.Data;
using System.Data.Common;
using Footing.Framework.Data;
using Footing.Framework.Data.TypeHandlers;
using Footing.Framework.Migrations;

namespace Footing.Framework.Tests;

public class MigrationTests
{
    [Fact]
    public void VersionParser_V1__create_users()
    {
        Assert.True(MigrationVersionParser.TryParse("V1__create_users.sql", out var info));
        Assert.Equal("1", info.Version);
        Assert.Equal("create_users", info.Description);
        Assert.Equal("VERSIONED", info.Type);
    }

    [Fact]
    public void VersionParser_V1_0_1__fix()
    {
        Assert.True(MigrationVersionParser.TryParse("V1_0_1__fix.sql", out var info));
        Assert.Equal("1.0.1", info.Version);
        Assert.Equal("fix", info.Description);
        Assert.Equal("VERSIONED", info.Type);
    }

    [Fact]
    public void VersionParser_R__views_and_R001()
    {
        Assert.True(MigrationVersionParser.TryParse("R__views.sql", out var r1));
        Assert.Equal("REPEATABLE", r1.Type);
        Assert.Equal("views", r1.Description);
        Assert.True(MigrationVersionParser.TryParse("R001__refresh_views.sql", out var r2));
        Assert.Equal("REPEATABLE", r2.Type);
        Assert.Equal("R001", r2.Version);
    }

    [Fact]
    public void VersionParser_NNN_baseline()
    {
        Assert.True(MigrationVersionParser.TryParse("001__create_schema.sql", out var b));
        Assert.Equal("BASELINE", b.Type);
        Assert.Equal("001", b.Version);
        Assert.Equal("create_schema", b.Description);
    }

    [Fact]
    public void Checksum_SHA256_stable()
    {
        var a = MigrationChecksum.Compute("SELECT 1");
        var b = MigrationChecksum.Compute("SELECT 1");
        var c = MigrationChecksum.Compute("SELECT 2");
        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.Equal(64, a.Length);
    }

    [Fact]
    public void BoolCharYN_Handler_YN()
    {
        var h = new BoolCharYNTypeHandler();
        var p = new FakeParam();
        h.SetValue(p, true);
        Assert.Equal("Y", p.Value);
        Assert.Equal(DbType.AnsiStringFixedLength, p.DbType);
        Assert.True(h.Parse("Y"));
        Assert.True(h.Parse("y"));
        Assert.False(h.Parse("N"));
        Assert.False(h.Parse("n"));
    }

    [Fact]
    public async Task Runner_Migrate_Validate_Repair_Baseline_Placeholders_YN()
    {
        var journal = new InMemoryJournal();
        var scripts = new InMemoryProvider(new[]
        {
            new MigrationInfo("1", "create_users", "V1__create_users.sql", "VERSIONED", MigrationChecksum.Compute("CREATE TABLE users (id INT)"), "CREATE TABLE users (id INT)"),
            new MigrationInfo("1.0.1", "fix", "V1_0_1__fix.sql", "VERSIONED", MigrationChecksum.Compute("ALTER TABLE users ADD COLUMN email VARCHAR(200)"), "ALTER TABLE users ADD COLUMN email VARCHAR(200)"),
        });
        var factory = new FakeFactory();
        var opts = new MigrationOptions { Placeholders = new Dictionary<string,string>{ ["schema"]="public"} };
        var runner = new MigrationRunner(journal, scripts, factory, opts);

        var result = await runner.MigrateAsync();
        Assert.Equal(2, result.Applied.Count);
        Assert.Empty(result.Skipped);
        // segunda chamada deve pular tudo
        var result2 = await runner.MigrateAsync();
        Assert.Empty(result2.Applied);
        Assert.Equal(2, result2.Skipped.Count);

        // Validate ok
        await runner.ValidateAsync();
        // drift
        var driftScripts = new InMemoryProvider(new[]
        {
            new MigrationInfo("1", "create_users", "V1__create_users.sql", "VERSIONED", MigrationChecksum.Compute("CREATE TABLE users (id INT) -- drift"), "CREATE TABLE users (id INT) -- drift"),
        });
        var runnerDrift = new MigrationRunner(journal, driftScripts, factory);
        await Assert.ThrowsAsync<MigrationException>(() => runnerDrift.ValidateAsync());

        // placeholders
        var phProvider = new InMemoryProvider(new[]
        {
            new MigrationInfo("2", "ph", "V2__ph.sql", "VERSIONED", MigrationChecksum.Compute("CREATE TABLE {{schema}}.t (id INT)"), "CREATE TABLE {{schema}}.t (id INT)"),
        });
        var phJournal = new InMemoryJournal();
        var phRunner = new MigrationRunner(phJournal, phProvider, factory, opts);
        await phRunner.MigrateAsync();
        Assert.Single(phJournal.Store, s => s.Value.successYN == "Y");

        // blocking if failed pending
        var failJournal = new InMemoryJournal();
        await failJournal.MarkAppliedAsync(new MigrationInfo("1","a","V1__a.sql","VERSIONED","",""), MigrationChecksum.Compute("a"), 10, "N", "boom");
        var failRunner = new MigrationRunner(failJournal, scripts, factory);
        await Assert.ThrowsAsync<MigrationException>(() => failRunner.MigrateAsync());
        await failRunner.RepairAsync();
        Assert.Empty(await failJournal.GetFailedVersionsAsync());

        // GenerateScript dry-run
        var dryJournal = new InMemoryJournal();
        var dryRunner = new MigrationRunner(dryJournal, scripts, factory);
        var script = await dryRunner.GenerateScriptAsync();
        Assert.Contains("V1__create_users.sql", script);

        // Baseline
        await dryRunner.BaselineAsync("0");
        Assert.Contains("0", await dryJournal.GetAppliedVersionsAsync());
    }

    // fakes
    private sealed class InMemoryJournal : IMigrationJournal
    {
        public Dictionary<string,(string checksum, string successYN, string? error)> Store = new();
        public Task EnsureHistoryTableAsync(CancellationToken ct=default)=>Task.CompletedTask;
        public Task<IReadOnlyList<string>> GetAppliedVersionsAsync(CancellationToken ct=default)=>Task.FromResult<IReadOnlyList<string>>(Store.Where(kv=>kv.Value.successYN=="Y").Select(kv=>kv.Key).ToList());
        public Task<bool> HasAppliedAsync(string v, CancellationToken ct=default)=>Task.FromResult(Store.ContainsKey(v) && Store[v].successYN=="Y");
        public Task<string?> GetChecksumAsync(string v, CancellationToken ct=default)=>Task.FromResult(Store.TryGetValue(v,out var val)?val.checksum:null);
        public Task MarkAppliedAsync(MigrationInfo info, string checksum, long ms, string successYN, string? error, CancellationToken ct=default){Store[info.Version]=(checksum,successYN,error); return Task.CompletedTask;}
        public Task UpdateChecksumAsync(string v, string cs, CancellationToken ct=default){ if(Store.ContainsKey(v)) Store[v]=(cs,Store[v].successYN,Store[v].error); return Task.CompletedTask;}
        public Task<IReadOnlyList<string>> GetFailedVersionsAsync(CancellationToken ct=default)=>Task.FromResult<IReadOnlyList<string>>(Store.Where(kv=>kv.Value.successYN=="N").Select(kv=>kv.Key).ToList());
        public Task<IReadOnlyList<MigrationStatus>> GetInfoAsync(CancellationToken ct=default)=>Task.FromResult<IReadOnlyList<MigrationStatus>>(Store.Select(kv=>new MigrationStatus(kv.Key,"",kv.Value.successYN=="Y"?"VERSIONED":"",kv.Value.checksum,DateTime.UtcNow,kv.Value.successYN=="Y"?"Success":"Failed")).ToList());
        public Task RepairAsync(CancellationToken ct=default){ foreach(var k in Store.Where(kv=>kv.Value.successYN=="N").Select(kv=>kv.Key).ToList()) Store.Remove(k); return Task.CompletedTask;}
        public Task BaselineAsync(string v, CancellationToken ct=default){ Store[v]=(MigrationChecksum.Compute("baseline"),"Y",null); return Task.CompletedTask;}
    }

    private sealed class InMemoryProvider : IMigrationScriptProvider
    {
        private readonly IReadOnlyList<MigrationInfo> _list;
        public InMemoryProvider(IEnumerable<MigrationInfo> list)=>_list=list.ToList();
        public async IAsyncEnumerable<MigrationInfo> GetScriptsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct=default){ foreach(var s in _list){ ct.ThrowIfCancellationRequested(); yield return s; await Task.Yield(); } }
    }

    private sealed class FakeFactory : IDbConnectionFactory{ public IDbConnection CreateConnection()=>new FakeConn(); }
    private sealed class FakeConn : IDbConnection
    {
        public string ConnectionString{get;set;}=""; public int ConnectionTimeout=>0; public string Database=>"fake"; public ConnectionState State=>ConnectionState.Open;
        public IDbTransaction BeginTransaction()=>new FakeTx(this); public IDbTransaction BeginTransaction(IsolationLevel il)=>new FakeTx(this);
        public void ChangeDatabase(string n){} public void Close(){} public IDbCommand CreateCommand()=>new FakeCmd(this){CommandText=""};
        public void Open(){} public void Dispose(){} 
    }
    private sealed class FakeTx : IDbTransaction{ private readonly IDbConnection _c; public FakeTx(IDbConnection c)=>_c=c; public IDbConnection Connection=>_c; public IsolationLevel IsolationLevel=>IsolationLevel.ReadCommitted; public void Commit(){} public void Rollback(){} public void Dispose(){} }
    private sealed class FakeCmd : IDbCommand
    {
        private IDbConnection _c; public FakeCmd(IDbConnection c)=>_c=c;
        public string CommandText{get;set;}=""; public int CommandTimeout{get;set;} public CommandType CommandType{get;set;} public IDbConnection Connection{get=>_c;set=>_c=value;} public IDataParameterCollection Parameters=>new FakeParams(); public IDbTransaction? Transaction{get;set;} public UpdateRowSource UpdatedRowSource{get;set;}
        public void Cancel(){} public IDbDataParameter CreateParameter()=>new FakeParam(); public void Dispose(){} public int ExecuteNonQuery(){ if(CommandText.Contains("FAIL")) throw new InvalidOperationException("fail"); return 1; } public IDataReader ExecuteReader()=>null!; public IDataReader ExecuteReader(CommandBehavior b)=>null!; public object ExecuteScalar()=>null!; public void Prepare(){} 
    }
    private sealed class FakeParam : IDbDataParameter{ public DbType DbType{get;set;} public ParameterDirection Direction{get;set;} public bool IsNullable=>true; public string ParameterName{get;set;}=""; public string SourceColumn{get;set;}=""; public DataRowVersion SourceVersion{get;set;} public object? Value{get;set;} public byte Precision{get;set;} public byte Scale{get;set;} public int Size{get;set;} }
    private sealed class FakeParams : IDataParameterCollection{ private readonly List<object> _l=new(); public object this[string n]{get=>null!;set{}} public object this[int i]{get=>_l[i];set=>_l[i]=value;} public int Count=>_l.Count; public bool IsFixedSize=>false; public bool IsReadOnly=>false; public bool IsSynchronized=>false; public object SyncRoot=>this; public int Add(object v){_l.Add(v); return _l.Count-1;} public void Clear()=>_l.Clear(); public bool Contains(string n)=>false; public bool Contains(object v)=>_l.Contains(v); public void CopyTo(Array a,int i)=>_l.CopyTo((object[])a,i); public System.Collections.IEnumerator GetEnumerator()=>_l.GetEnumerator(); public int IndexOf(string n)=>-1; public int IndexOf(object v)=>_l.IndexOf(v); public void Insert(int i,object v)=>_l.Insert(i,v); public void Remove(object v)=>_l.Remove(v); public void RemoveAt(string n){} public void RemoveAt(int i)=>_l.RemoveAt(i); }
}
