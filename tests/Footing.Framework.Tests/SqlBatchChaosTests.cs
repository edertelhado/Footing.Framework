using System.Data;
using System.Data.Common;
using Dapper;
using Footing.Framework.Data;
using Footing.Framework.Tests.Helpers;

namespace Footing.Framework.Tests;

/// <summary>
/// US-TEST-RESISTENCIA-CHAOS C8 — SqlBatch chaos (fail-fast)
/// BuildBatchInsert/InsertBatchAsync SEMPRE throw onde valida (ValidateTableName, batchSize<=0)
/// e noop onde vazio/typo total. Contrasta com SqlTemplate fail-safe.
/// </summary>
public class SqlBatchChaosTests
{
    private record User(string Name, int Age, string? Email = null);
    private record Product(string FirstName, string LastName, string IgnoredProp);
    private record WithGuid(Guid Id, string Name);
    private record WithEnum(string Name, ChaosStatus Status);

    // Minimal FakeConnection para InsertBatchAsync sem DB real
    private class FakeConnection : DbConnection
    {
        public List<string> ExecutedSqls { get; } = new();
        private string _cs = "";
        public override string ConnectionString { get => _cs; set => _cs = value; }
        public override string Database => "Fake";
        public override string DataSource => "Fake";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => ConnectionState.Open;
        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new FakeTransaction(this);
        protected override DbCommand CreateDbCommand() => new FakeCommand(this);
        private class FakeTransaction : DbTransaction
        {
            private readonly FakeConnection _c; public FakeTransaction(FakeConnection c) => _c = c;
            public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
            protected override DbConnection? DbConnection => _c;
            public override void Commit() { } public override void Rollback() { }
        }
        private class FakeCommand : DbCommand
        {
            private readonly FakeConnection _c; public FakeCommand(FakeConnection c) => _c = c;
            public override string CommandText { get; set; } = "";
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            protected override DbConnection? DbConnection { get => _c; set { } }
            protected override DbParameterCollection DbParameterCollection { get; } = new FakeParamCollection();
            protected override DbTransaction? DbTransaction { get; set; }
            public override void Cancel() { }
            public override int ExecuteNonQuery()
            {
                _c.ExecutedSqls.Add(CommandText);
                int rows = 0; int idx = 0;
                while ((idx = CommandText.IndexOf("(@p", idx, StringComparison.Ordinal)) != -1) { rows++; idx += 3; }
                if (rows == 0 && CommandText.Contains("INSERT", StringComparison.OrdinalIgnoreCase)) rows = 1;
                return rows;
            }
            public override Task<int> ExecuteNonQueryAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(ExecuteNonQuery()); }
            public override object? ExecuteScalar() => ExecuteNonQuery();
            public override void Prepare() { }
            protected override DbParameter CreateDbParameter() => new FakeParam();
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotImplementedException();
            protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken ct) => throw new NotImplementedException();
        }
        private class FakeParam : DbParameter
        {
            public override DbType DbType { get; set; }
            public override ParameterDirection Direction { get; set; }
            public override bool IsNullable { get; set; }
            public override string ParameterName { get; set; } = "";
            public override string SourceColumn { get; set; } = "";
            public override object? Value { get; set; }
            public override bool SourceColumnNullMapping { get; set; }
            public override int Size { get; set; }
            public override void ResetDbType() { }
        }
        private class FakeParamCollection : DbParameterCollection
        {
            private readonly List<object> _l = new();
            public override int Count => _l.Count;
            public override object SyncRoot => _l;
            public override int Add(object v) { _l.Add(v); return _l.Count - 1; }
            public override void AddRange(Array v) => _l.AddRange(v.Cast<object>());
            public override void Clear() => _l.Clear();
            public override bool Contains(object v) => _l.Contains(v);
            public override bool Contains(string v) => _l.Any(x => (x as DbParameter)?.ParameterName == v);
            public override void CopyTo(Array a, int i) => _l.ToArray().CopyTo(a, i);
            public override System.Collections.IEnumerator GetEnumerator() => _l.GetEnumerator();
            protected override DbParameter GetParameter(int i) => (DbParameter)_l[i];
            protected override DbParameter GetParameter(string n) => (DbParameter)_l.First(x => ((DbParameter)x).ParameterName == n);
            public override int IndexOf(object v) => _l.IndexOf(v);
            public override int IndexOf(string n) => _l.FindIndex(x => ((DbParameter)x).ParameterName == n);
            public override void Insert(int i, object v) => _l.Insert(i, v);
            public override void Remove(object v) => _l.Remove(v);
            public override void RemoveAt(int i) => _l.RemoveAt(i);
            public override void RemoveAt(string n) => RemoveAt(IndexOf(n));
            protected override void SetParameter(int i, DbParameter v) => _l[i] = v;
            protected override void SetParameter(string n, DbParameter v) { var idx = IndexOf(n); if (idx >= 0) _l[idx] = v; else _l.Add(v); }
        }
    }

    // C8.1 tableName injection → ArgumentException (fail-fast)
    [Theory]
    [MemberData(nameof(ChaosData.C8_InvalidTableNames), MemberType = typeof(ChaosData))]
    public void C8_BuildBatchInsert_TableName_Injection_ThrowsArgumentException(string badTable)
    {
        var items = new[] { new User("Ana", 30) };
        var ex = Assert.Throws<ArgumentException>(() => SqlBatch.BuildBatchInsert(badTable, items));
        Assert.Contains("invalid characters", ex.Message);
        Assert.Equal("tableName", ex.ParamName);
    }

    [Theory]
    [MemberData(nameof(ChaosData.C8_InvalidTableNames), MemberType = typeof(ChaosData))]
    public async Task C8_InsertBatchAsync_TableName_Injection_ThrowsArgumentException(string badTable)
    {
        var conn = new FakeConnection();
        var items = new[] { new User("Ana", 30) };
        await Assert.ThrowsAsync<ArgumentException>(() => SqlBatch.InsertBatchAsync(conn, badTable, items));
        await Assert.ThrowsAsync<ArgumentException>(() => SqlBatch.InsertBatchAsync(conn, badTable, items, "Name"));
    }

    [Theory]
    [MemberData(nameof(ChaosData.C8_ValidTableNames), MemberType = typeof(ChaosData))]
    public void C8_BuildBatchInsert_TableName_Valid_NaoThrow(string goodTable)
    {
        var items = new[] { new User("Ana", 30) };
        var ex = Record.Exception(() => SqlBatch.BuildBatchInsert(goodTable, items));
        Assert.Null(ex);
        var (sql, _) = SqlBatch.BuildBatchInsert(goodTable, items);
        Assert.Contains($"INSERT INTO {goodTable}", sql);
    }

    // C8.2 batchSize inválido → ArgumentOutOfRangeException
    [Theory]
    [MemberData(nameof(ChaosData.C8_BatchSizeInvalid), MemberType = typeof(ChaosData))]
    public async Task C8_InsertBatchAsync_BatchSize_Invalido_ThrowsArgumentOutOfRange(int badSize)
    {
        var conn = new FakeConnection();
        var items = new[] { new User("Ana", 30) };
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => SqlBatch.InsertBatchAsync(conn, "Users", items, batchSize: badSize));
        Assert.Contains("batchSize", ex.Message);
        // overload com columnsOverride também
        var ex2 = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => SqlBatch.InsertBatchAsync(conn, "Users", items, "Name", batchSize: badSize));
        Assert.Contains("batchSize", ex2.Message);
    }

    [Fact]
    public async Task C8_InsertBatchAsync_Chunking_500_Com1200_3Statements()
    {
        var items = Enumerable.Range(0, 1200).Select(i => new User($"N{i}", i)).ToList();
        var conn = new FakeConnection();
        var inserted = await SqlBatch.InsertBatchAsync(conn, "T", items, batchSize: 500);
        Assert.Equal(1200, inserted);
        Assert.Equal(3, conn.ExecutedSqls.Count);
        // 2100/3=700 → 500 limita, então 500/500/200
        Assert.Contains("@p0_Name", conn.ExecutedSqls[0]);
        Assert.Contains("@p499_Name", conn.ExecutedSqls[0]);
    }

    // C8.3 columnsOverride typo → filtra pra 0 cols → noop "", ou 1 col
    [Theory]
    [MemberData(nameof(ChaosData.C8_ColumnsOverride), MemberType = typeof(ChaosData))]
    public void C8_BuildBatchInsert_ColumnsOverride_Typo_NoOpOuFiltro(string cols, int expectedColCount, bool expectNonEmpty)
    {
        var items = new[] { new Product("John", "Doe", "IGN") };
        var (sql, parms) = SqlBatch.BuildBatchInsert("Users", items, cols);
        if (!expectNonEmpty)
        {
            Assert.Equal("", sql);
            Assert.Empty(parms.ParameterNames);
        }
        else
        {
            Assert.NotEqual("", sql);
            // verifica que só colunas válidas aparecem
            if (expectedColCount == 1)
            {
                Assert.Contains("FirstName", sql);
                Assert.DoesNotContain("IgnoredProp", sql);
            }
            if (expectedColCount == 2)
            {
                Assert.Contains("FirstName", sql);
                Assert.Contains("LastName", sql);
            }
        }
    }

    [Fact]
    public void C8_ColumnsOverride_TypoTotal_NoOp_Gherkin()
    {
        var items = new[] { new Product("John", "Doe", "IGN") };
        var (sql, p) = SqlBatch.BuildBatchInsert("Users", items, "TYPO_INEXISTENTE");
        Assert.Equal("", sql);
        Assert.Empty(p.ParameterNames);
        var (sql2, _) = SqlBatch.BuildBatchInsert("Users", items, "FirstName,TYPO");
        Assert.Contains("FirstName", sql2);
        Assert.DoesNotContain("IgnoredProp", sql2);
        Assert.DoesNotContain("TYPO", sql2);
    }

    [Fact]
    public void C8_ColumnsOverride_CaseInsensitive()
    {
        var items = new[] { new Product("John", "Doe", "IGN") };
        var (sql, _) = SqlBatch.BuildBatchInsert("Users", items, "firstname,lastname");
        Assert.Contains("FirstName", sql);
        Assert.Contains("LastName", sql);
        Assert.DoesNotContain("IgnoredProp", sql);
    }

    // C8.4 Props com null, Guid, enum, 2100 overflow, lista vazia, CancellationToken
    [Fact]
    public void C8_Props_Null_Guid_Enum_Parametrizado()
    {
        // null
        var itemsNull = new[] { new User(null!, 30) };
        var (sqlNull, parmsNull) = SqlBatch.BuildBatchInsert("Users", itemsNull);
        Assert.Contains("@p0_Name", sqlNull);
        Assert.Null(parmsNull.Get<string?>("p0_Name"));
        // Guid
        var itemsGuid = new[] { new WithGuid(Guid.Parse("550e8400-e29b-41d4-a716-446655440000"), "Ana") };
        var (sqlGuid, parmsGuid) = SqlBatch.BuildBatchInsert("T", itemsGuid);
        Assert.Contains("Id", sqlGuid);
        Assert.Contains("Name", sqlGuid);
        Assert.Equal(Guid.Parse("550e8400-e29b-41d4-a716-446655440000"), parmsGuid.Get<Guid>("p0_Id"));
        // enum
        var itemsEnum = new[] { new WithEnum("Ana", ChaosStatus.Active) };
        var (sqlEnum, parmsEnum) = SqlBatch.BuildBatchInsert("T", itemsEnum);
        Assert.Contains("Status", sqlEnum);
        Assert.Equal(ChaosStatus.Active, parmsEnum.Get<ChaosStatus>("p0_Status"));
        // lista vazia → noop
        var (sqlEmpty, parmsEmpty) = SqlBatch.BuildBatchInsert("Users", Array.Empty<User>());
        Assert.Equal("", sqlEmpty);
        Assert.Empty(parmsEmpty.ParameterNames);
    }

    [Fact]
    public async Task C8_InsertBatchAsync_ListaVazia_NoOp_ZeroSemExecutar()
    {
        var conn = new FakeConnection();
        var inserted = await SqlBatch.InsertBatchAsync(conn, "Users", Array.Empty<User>());
        Assert.Equal(0, inserted);
        Assert.Empty(conn.ExecutedSqls);
    }

    [Fact]
    public async Task C8_EffectiveBatch_2100_Com10Cols_Chunk210()
    {
        // 500 rows ×10 cols =5000 params → effective 2100/10=210 → 3 chunks
        var items = Enumerable.Range(0, 500).Select(i => new TenCols(i)).ToList();
        var conn = new FakeConnection();
        var inserted = await SqlBatch.InsertBatchAsync(conn, "BigTable", items, batchSize: 500);
        Assert.Equal(500, inserted);
        Assert.Equal(3, conn.ExecutedSqls.Count);
        foreach (var sql in conn.ExecutedSqls)
        {
            int paramCount = sql.Split("@p").Length - 1;
            Assert.True(paramCount <= 2100, $"paramCount {paramCount} >2100");
        }
        // cada chunk ≤210 rows
        int[] expectedRows = { 210, 210, 80 };
        for (int i = 0; i < expectedRows.Length; i++)
        {
            int rows = conn.ExecutedSqls[i].Split("(@p").Length - 1;
            Assert.Equal(expectedRows[i], rows);
        }
    }

    private class TenCols
    {
        public TenCols(int id) { Id = id; C1 = id; C2 = id; C3 = id; C4 = id; C5 = id; C6 = id; C7 = id; C8 = id; C9 = id; }
        public int Id { get; } public int C1 { get; } public int C2 { get; } public int C3 { get; } public int C4 { get; } public int C5 { get; } public int C6 { get; } public int C7 { get; } public int C8 { get; } public int C9 { get; }
    }

    [Fact]
    public async Task C8_CancellationToken_Cancelado_ThrowsOperationCanceled()
    {
        var conn = new FakeConnection();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            SqlBatch.InsertBatchAsync(conn, "T", Enumerable.Range(0, 10).Select(i => new User($"N{i}", i)), cancellationToken: cts.Token));
        // overload com columnsOverride
        var cts2 = new CancellationTokenSource();
        cts2.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            SqlBatch.InsertBatchAsync(conn, "T", Enumerable.Range(0, 10).Select(i => new User($"N{i}", i)), "Name", cancellationToken: cts2.Token));
    }

    [Fact]
    public void C8_BuildBatchInsert_SemValoresLiterais_SomentePlaceholder()
    {
        var items = new[] { new User("'; DROP TABLE Users; --", 30) };
        var (sql, parms) = SqlBatch.BuildBatchInsert("Users", items);
        Assert.DoesNotContain("DROP", sql);
        Assert.DoesNotContain("';", sql);
        Assert.Contains("@p0_Name", sql);
        Assert.Equal("'; DROP TABLE Users; --", parms.Get<string>("p0_Name"));
    }
}
