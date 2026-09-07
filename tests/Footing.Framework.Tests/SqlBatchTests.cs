using System.Data;
using System.Data.Common;
using Dapper;
using Footing.Framework.Data;

namespace Footing.Framework.Tests;

public class SqlBatchTests
{
    private record User(string Name, int Age, string? Email = null);
    private record Product(string FirstName, string LastName, string IgnoredProp);

    // Helper FakeConnection that counts rows via SQL parsing and captures transaction usage
    private class FakeConnection : DbConnection
    {
        public List<string> ExecutedSqls { get; } = new();
        public List<IDbTransaction?> ExecutedTransactions { get; } = new();
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
            private readonly FakeConnection _conn;
            public FakeTransaction(FakeConnection conn) => _conn = conn;
            public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
            protected override DbConnection? DbConnection => _conn;
            public override void Commit() { }
            public override void Rollback() { }
        }

        private class FakeCommand : DbCommand
        {
            private readonly FakeConnection _conn;
            public FakeCommand(FakeConnection conn) => _conn = conn;
            public override string CommandText { get; set; } = "";
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            protected override DbConnection? DbConnection { get => _conn; set { } }
            protected override DbParameterCollection DbParameterCollection { get; } = new FakeParameterCollection();
            protected override DbTransaction? DbTransaction { get; set; }
            public override void Cancel() { }
            public override int ExecuteNonQuery()
            {
                _conn.ExecutedSqls.Add(CommandText);
                _conn.ExecutedTransactions.Add(DbTransaction);
                int rows = 0;
                int idx = 0;
                while ((idx = CommandText.IndexOf("(@p", idx, StringComparison.Ordinal)) != -1)
                {
                    rows++;
                    idx += 3;
                }
                if (rows == 0 && CommandText.Contains("INSERT", StringComparison.OrdinalIgnoreCase)) rows = 1;
                return rows;
            }
            public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(ExecuteNonQuery());
            }
            public override object? ExecuteScalar() => ExecuteNonQuery();
            public override void Prepare() { }
            protected override DbParameter CreateDbParameter() => new FakeParameter();
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotImplementedException();
            protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) => throw new NotImplementedException();
        }

        private class FakeParameter : DbParameter
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
        private class FakeParameterCollection : DbParameterCollection
        {
            private readonly List<object> _list = new();
            public override int Count => _list.Count;
            public override object SyncRoot => _list;
            public override int Add(object value) { _list.Add(value); return _list.Count - 1; }
            public override void AddRange(Array values) => _list.AddRange(values.Cast<object>());
            public override void Clear() => _list.Clear();
            public override bool Contains(object value) => _list.Contains(value);
            public override bool Contains(string value) => _list.Any(x => (x as DbParameter)?.ParameterName == value);
            public override void CopyTo(Array array, int index) => _list.ToArray().CopyTo(array, index);
            public override System.Collections.IEnumerator GetEnumerator() => _list.GetEnumerator();
            protected override DbParameter GetParameter(int index) => (DbParameter)_list[index];
            protected override DbParameter GetParameter(string parameterName) => (DbParameter)_list.First(x => ((DbParameter)x).ParameterName == parameterName);
            public override int IndexOf(object value) => _list.IndexOf(value);
            public override int IndexOf(string parameterName) => _list.FindIndex(x => ((DbParameter)x).ParameterName == parameterName);
            public override void Insert(int index, object value) => _list.Insert(index, value);
            public override void Remove(object value) => _list.Remove(value);
            public override void RemoveAt(int index) => _list.RemoveAt(index);
            public override void RemoveAt(string parameterName) => RemoveAt(IndexOf(parameterName));
            protected override void SetParameter(int index, DbParameter value) => _list[index] = value;
            protected override void SetParameter(string parameterName, DbParameter value) { var idx = IndexOf(parameterName); if (idx >=0) _list[idx]=value; else _list.Add(value); }
        }
    }

    private class FakeFactory : IDbConnectionFactory
    {
        public FakeConnection LastConnection { get; private set; } = new();
        public IDbConnection CreateConnection()
        {
            LastConnection = new FakeConnection();
            return LastConnection;
        }
    }

    // Cenário 1: BuildBatchInsert gera SQL parametrizado corretamente
    [Fact]
    public void BuildBatchInsert_2_rows_gera_sql_parametrizado()
    {
        var items = new[] { new User("Ana", 30, "a@b.com"), new User("Bob", 25, "b@b.com") };
        var (sql, parms) = SqlBatch.BuildBatchInsert("Users", items);
        Assert.Contains("INSERT INTO Users", sql);
        Assert.Contains("Name", sql);
        Assert.Contains("Age", sql);
        Assert.Contains("@p0_Name", sql);
        Assert.Contains("@p0_Age", sql);
        Assert.Contains("@p1_Name", sql);
        Assert.Contains("@p1_Age", sql);
        Assert.DoesNotContain("'Ana'", sql);
        Assert.Equal("Ana", parms.Get<string>("p0_Name"));
        Assert.Equal(30, parms.Get<int>("p0_Age"));
        Assert.Equal("Bob", parms.Get<string>("p1_Name"));
        Assert.Equal(25, parms.Get<int>("p1_Age"));
        Assert.Equal("a@b.com", parms.Get<string>("p0_Email"));
    }

    [Fact]
    public void BuildBatchInsert_sem_valores_literais()
    {
        var items = new[] { new User("X", 99) };
        var (sql, _) = SqlBatch.BuildBatchInsert("T", items);
        Assert.DoesNotContain("'X'", sql);
        Assert.DoesNotContain("VALUES ('X'", sql);
        Assert.Contains("VALUES (@p0_", sql);
    }

    // Cenário 2: Lista vazia
    [Fact]
    public async Task Build_e_Insert_vazio_noop()
    {
        var empty = Array.Empty<User>();
        var (sql, parms) = SqlBatch.BuildBatchInsert("Users", empty);
        Assert.Equal("", sql);
        Assert.Empty(parms.ParameterNames);
        var conn = new FakeConnection();
        var inserted = await SqlBatch.InsertBatchAsync(conn, "Users", empty);
        Assert.Equal(0, inserted);
        Assert.Empty(conn.ExecutedSqls);
    }

    [Fact]
    public async Task InsertBatchAsync_lista_vazia_com_batchSize_custom_noop()
    {
        var conn = new FakeConnection();
        var result = await SqlBatch.InsertBatchAsync(conn, "T", Enumerable.Empty<User>(), batchSize: 100);
        Assert.Equal(0, result);
        Assert.Empty(conn.ExecutedSqls);
    }

    // Cenário 3: Chunking
    [Fact]
    public async Task InsertBatchAsync_chunking_1200_com_3cols_3_statements()
    {
        var items = Enumerable.Range(0, 1200).Select(i => new User($"N{i}", i, $"e{i}@t.com")).ToList();
        var conn = new FakeConnection();
        var inserted = await SqlBatch.InsertBatchAsync(conn, "T", items, batchSize: 500);
        Assert.Equal(1200, inserted);
        Assert.Equal(3, conn.ExecutedSqls.Count);
        foreach (var sql in conn.ExecutedSqls)
        {
            int paramCount = sql.Split("@p").Length - 1;
            Assert.True(paramCount <= 1500, $"chunk paramCount {paramCount} >1500");
        }
        int[] expectedRows = { 500, 500, 200 };
        for (int i = 0; i < expectedRows.Length; i++)
        {
            int rows = conn.ExecutedSqls[i].Split("(@p").Length - 1;
            Assert.Equal(expectedRows[i], rows);
        }
    }

    [Fact]
    public async Task InsertBatchAsync_auto_calc_2100_com_10cols()
    {
        var items = Enumerable.Range(0, 500).Select(i => new TenCols(i)).ToList();
        var conn = new FakeConnection();
        var inserted = await SqlBatch.InsertBatchAsync(conn, "BigTable", items, batchSize: 500);
        Assert.Equal(500, inserted);
        Assert.Equal(3, conn.ExecutedSqls.Count);
        foreach (var sql in conn.ExecutedSqls)
        {
            int paramCount = sql.Split("@p").Length - 1;
            Assert.True(paramCount <= 2100);
        }
    }

    private class TenCols
    {
        public TenCols(int id) { Id = id; C1 = id; C2 = id; C3 = id; C4 = id; C5 = id; C6 = id; C7 = id; C8 = id; C9 = id; }
        public int Id { get; }
        public int C1 { get; }
        public int C2 { get; }
        public int C3 { get; }
        public int C4 { get; }
        public int C5 { get; }
        public int C6 { get; }
        public int C7 { get; }
        public int C8 { get; }
        public int C9 { get; }
    }

    // Cenário 4: Null handling
    [Fact]
    public void BuildBatchInsert_null_handling()
    {
        var items = new[] { new User(null!, 30, "a@b.com") };
        var (sql, parms) = SqlBatch.BuildBatchInsert("Users", items);
        Assert.Contains("Name", sql);
        Assert.Contains("@p0_Name", sql);
        var val = parms.Get<string?>("p0_Name");
        Assert.Null(val);
        Assert.Equal(30, parms.Get<int>("p0_Age"));
        Assert.Equal("a@b.com", parms.Get<string>("p0_Email"));
    }

    // Cenário 5: Column filtering
    [Fact]
    public void BuildBatchInsert_column_filtering()
    {
        var items = new[] { new Product("John", "Doe", "IGN") };
        var (sqlFiltered, parmsFiltered) = SqlBatch.BuildBatchInsert("Users", items, columnsOverride: "FirstName,LastName");
        Assert.Contains("FirstName", sqlFiltered);
        Assert.Contains("LastName", sqlFiltered);
        Assert.DoesNotContain("IgnoredProp", sqlFiltered);
        Assert.Equal("John", parmsFiltered.Get<string>("p0_FirstName"));
        Assert.Equal("Doe", parmsFiltered.Get<string>("p0_LastName"));
        var (sqlAll, _) = SqlBatch.BuildBatchInsert("Users", items);
        Assert.Contains("IgnoredProp", sqlAll);
        var (sqlCase, _) = SqlBatch.BuildBatchInsert("Users", items, "firstname,lastname");
        Assert.Contains("FirstName", sqlCase);
        Assert.DoesNotContain("IgnoredProp", sqlCase);
    }

    // Cenário 6: UnitOfWork
    [Fact]
    public async Task InsertBatchAsync_com_UoW_transacao()
    {
        var factory = new FakeFactory();
        await using var uow = new UnitOfWork(factory);
        await uow.BeginAsync();
        var items = Enumerable.Range(0, 10).Select(i => new User($"N{i}", i)).ToList();
        var inserted = await SqlBatch.InsertBatchAsync(uow, "Users", items, batchSize: 5);
        Assert.Equal(10, inserted);
        var conn = (FakeConnection)uow.Connection;
        Assert.Equal(2, conn.ExecutedSqls.Count);
        Assert.All(conn.ExecutedTransactions, tx => Assert.Equal(uow.Transaction, tx));
        await uow.DisposeAsync();
    }

    [Fact]
    public async Task InsertBatchAsync_ct_cancellation()
    {
        var conn = new FakeConnection();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            SqlBatch.InsertBatchAsync(conn, "T", Enumerable.Range(0, 10).Select(i => new User($"N{i}", i)), cancellationToken: cts.Token));
    }

    // Cenário 7: Compatibilidade Dapper
    [Fact]
    public void BuildBatchInsert_compatible_Dapper_params()
    {
        var items = Enumerable.Range(0, 100).Select(i => new User($"N{i}", i)).ToArray();
        var (sql, parms) = SqlBatch.BuildBatchInsert("T", items);
        Assert.StartsWith("INSERT INTO T", sql);
        Assert.Contains("@p0_Name", sql);
        Assert.Contains("@p99_Name", sql);
        for (int i = 0; i < 100; i++)
            Assert.Equal($"N{i}", parms.Get<string>($"p{i}_Name"));
        int tuples = sql.Split("(@p").Length - 1;
        Assert.Equal(100, tuples);
    }

    [Fact]
    public async Task InsertBatchAsync_chunking_verifica_transacao_null_autocommit()
    {
        var conn = new FakeConnection();
        var items = new[] { new User("A", 1), new User("B", 2) };
        var inserted = await SqlBatch.InsertBatchAsync(conn, "T", items, transaction: null, batchSize: 500);
        Assert.Equal(2, inserted);
        Assert.Single(conn.ExecutedSqls);
        Assert.Null(conn.ExecutedTransactions[0]);
    }
}
