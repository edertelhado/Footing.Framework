using Footing.Framework.Data;

namespace Footing.Framework.Tests;

public class SqlBatchSecTests
{
    private record User(string Name, int Age);

    [Fact]
    public void BuildBatchInsert_valid_table_pass()
    {
        var items = new[] { new User("Ana", 30) };
        var (sql, _) = SqlBatch.BuildBatchInsert("Users", items);
        Assert.Contains("INSERT INTO Users", sql);

        var (sql2, _) = SqlBatch.BuildBatchInsert("schema.Users", items);
        Assert.Contains("INSERT INTO schema.Users", sql2);

        var (sql3, _) = SqlBatch.BuildBatchInsert("Users_123", items);
        Assert.Contains("INSERT INTO Users_123", sql3);
    }

    [Fact]
    public void BuildBatchInsert_bad_table_throws()
    {
        var items = new[] { new User("Ana", 30) };
        Assert.Throws<ArgumentException>(() => SqlBatch.BuildBatchInsert("Users; DROP TABLE Users; --", items));
        Assert.Throws<ArgumentException>(() => SqlBatch.BuildBatchInsert("Users; DROP", items));
        Assert.Throws<ArgumentException>(() => SqlBatch.BuildBatchInsert("a; b", items));
        Assert.Throws<ArgumentException>(() => SqlBatch.BuildBatchInsert("Users--", items));
        Assert.Throws<ArgumentException>(() => SqlBatch.BuildBatchInsert("Users\"", items));
        Assert.Throws<ArgumentException>(() => SqlBatch.BuildBatchInsert("Users[", items));
    }

    [Fact]
    public void ValidateTableName_TrailingDot_Throws()
    {
        var items = new[] { new User("Ana", 30) };
        Assert.Throws<ArgumentException>(() => SqlBatch.BuildBatchInsert("Users.", items));
        Assert.Throws<ArgumentException>(() => SqlBatch.BuildBatchInsert("schema.Users.", items));
        Assert.Throws<ArgumentException>(() => SqlBatch.BuildBatchInsert("Users. ", items));
    }

    [Fact]
    public void ValidateTableName_DoubleDot_Throws()
    {
        var items = new[] { new User("Ana", 30) };
        Assert.Throws<ArgumentException>(() => SqlBatch.BuildBatchInsert("schema..Users", items));
        Assert.Throws<ArgumentException>(() => SqlBatch.BuildBatchInsert("Users..", items));
        Assert.Throws<ArgumentException>(() => SqlBatch.BuildBatchInsert(".Users", items));
        Assert.Throws<ArgumentException>(() => SqlBatch.BuildBatchInsert("a..b", items));
    }

    [Fact]
    public void ValidateTableName_ValidSchemaDot_Pass()
    {
        var items = new[] { new User("Ana", 30) };
        var (sql, _) = SqlBatch.BuildBatchInsert("schema.Users", items);
        Assert.Contains("INSERT INTO schema.Users", sql);
        var (sql2, _) = SqlBatch.BuildBatchInsert("my_schema.Users_123", items);
        Assert.Contains("INSERT INTO my_schema.Users_123", sql2);
        var (sql3, _) = SqlBatch.BuildBatchInsert("Users", items);
        Assert.Contains("INSERT INTO Users", sql3);
    }

    [Fact]
    public async Task InsertBatchAsync_bad_table_throws_all_overloads()
    {
        var items = new[] { new User("Ana", 30) };
        var conn = new FakeConn();
        await Assert.ThrowsAsync<ArgumentException>(() => SqlBatch.InsertBatchAsync(conn, "bad; DROP", items));
        await Assert.ThrowsAsync<ArgumentException>(() => SqlBatch.InsertBatchAsync(conn, "bad; DROP", items, "Name"));
    }

    // minimal fake to satisfy InsertBatchAsync validation before execution
    private class FakeConn : System.Data.Common.DbConnection
    {
        public override string ConnectionString { get; set; } = "";
        public override string Database => "Fake";
        public override string DataSource => "Fake";
        public override string ServerVersion => "1.0";
        public override System.Data.ConnectionState State => System.Data.ConnectionState.Open;
        public override void ChangeDatabase(string db) { }
        public override void Close() { }
        public override void Open() { }
        protected override System.Data.Common.DbTransaction BeginDbTransaction(System.Data.IsolationLevel il) => throw new NotImplementedException();
        protected override System.Data.Common.DbCommand CreateDbCommand() => throw new NotImplementedException();
    }
}
