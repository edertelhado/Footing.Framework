using Footing.Framework.Data;
using Footing.Framework.Diagnostics;
using Footing.Framework.Health;
using Footing.Framework.Idempotency;
using Footing.Framework.Outbox;
using Microsoft.Extensions.Diagnostics.HealthChecks;
public class IdempotencyTests
{
    private sealed class FakeIdempotencyStore : IIdempotencyStore
    {
        private readonly Dictionary<string, object?> _d = new();
        public bool TryGet<T>(string k, out T? v) { if (_d.TryGetValue(k, out var o) && o is T t) { v = t; return true; } v = default; return false; }
        public void Set<T>(string k, T v, TimeSpan? ttl = null) => _d[k] = v;
        public async Task<T> GetOrCreateAsync<T>(string k, Func<Task<T>> f, TimeSpan? ttl = null, CancellationToken ct = default) { if (TryGet<T>(k, out var cur) && cur != null) return cur; var r = await f(); Set(k, r, ttl); return r; }
    }
    [Fact] public async Task GetOrCreate_Caches() { var s = new FakeIdempotencyStore(); var c = 0; var v1 = await s.GetOrCreateAsync("k", () => Task.FromResult(c++)); var v2 = await s.GetOrCreateAsync("k", () => Task.FromResult(c++)); Assert.Equal(0, v1); Assert.Equal(0, v2); Assert.Equal(1, c); }
    [Fact] public void TryGet_Miss() { var s = new FakeIdempotencyStore(); Assert.False(s.TryGet<string>("miss", out _)); }
    [Fact] public void Set_And_TryGet() { var s = new FakeIdempotencyStore(); s.Set("k", "v"); Assert.True(s.TryGet<string>("k", out var v)); Assert.Equal("v", v); }
}
public class OutboxTests
{
    private sealed class FakeOutboxStore : IOutboxStore
    {
        private readonly List<OutboxMessage> _l = new(); private readonly object _o = new();
        public Task SaveAsync(OutboxMessage m, System.Data.IDbTransaction? tx = null, CancellationToken ct = default) { lock (_o) _l.Add(m); return Task.CompletedTask; }
        public Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int b = 100, CancellationToken ct = default) { lock (_o) return Task.FromResult<IReadOnlyList<OutboxMessage>>(_l.Where(x => x.ProcessedAt == null).Take(b).ToList()); }
        public Task MarkProcessedAsync(Guid id, CancellationToken ct = default) { lock (_o) { var i = _l.FindIndex(x => x.Id == id); if (i >= 0) _l[i] = _l[i] with { ProcessedAt = DateTime.UtcNow }; } return Task.CompletedTask; }
        public Task MarkFailedAsync(Guid id, string e, CancellationToken ct = default) { lock (_o) { var i = _l.FindIndex(x => x.Id == id); if (i >= 0) _l[i] = _l[i] with { Attempts = _l[i].Attempts + 1, LastError = e }; } return Task.CompletedTask; }
    }
    [Fact] public async Task Save_And_GetUnprocessed() { var s = new FakeOutboxStore(); await s.SaveAsync(new OutboxMessage(Guid.NewGuid(), "t", "p", DateTime.UtcNow)); var l = await s.GetUnprocessedAsync(); Assert.Single(l); }
    [Fact] public async Task MarkProcessed_Removes() { var s = new FakeOutboxStore(); var id = Guid.NewGuid(); await s.SaveAsync(new OutboxMessage(id, "t", "p", DateTime.UtcNow)); await s.MarkProcessedAsync(id); var l = await s.GetUnprocessedAsync(); Assert.Empty(l); }
    [Fact] public async Task MarkFailed_Increments() { var s = new FakeOutboxStore(); var id = Guid.NewGuid(); await s.SaveAsync(new OutboxMessage(id, "t", "p", DateTime.UtcNow)); await s.MarkFailedAsync(id, "err"); var list = await s.GetUnprocessedAsync(); Assert.Equal(1, list.First().Attempts); }
    [Fact] public async Task SaveEvent_Serializes() { var s = new FakeOutboxStore(); await s.SaveEventAsync(new { Name = "eder" }); var l = await s.GetUnprocessedAsync(); Assert.Single(l); Assert.Contains("eder", l.First().Payload); }
    [Fact] public async Task Processor_Processes() { var s = new FakeOutboxStore(); var bus = new Footing.Framework.EventBus.EventBus(1); var tcs = new TaskCompletionSource(); bus.Subscribe<string>(e => { tcs.SetResult(); return Task.CompletedTask; }); await s.SaveAsync(new OutboxMessage(Guid.NewGuid(), typeof(string).AssemblyQualifiedName!, "\"hello\"", DateTime.UtcNow)); var proc = new OutboxProcessor(s, bus, null) { Interval = TimeSpan.FromMilliseconds(50), BatchSize = 10 }; await proc.StartAsync(CancellationToken.None); await Task.WhenAny(tcs.Task, Task.Delay(500)); await proc.StopAsync(CancellationToken.None); Assert.True(tcs.Task.IsCompleted); }
}
public class DiagnosticsTests
{
    [Fact] public void ActivitySource_NotNull() { Assert.NotNull(FootingActivitySource.Source); Assert.Equal("Footing.Framework", FootingActivitySource.Source.Name); }
    [Fact] public void StartActivities_NoThrow() { using var a1 = FootingActivitySource.StartSqlTemplateRender("SELECT *"); using var a2 = FootingActivitySource.StartEventBusPublish("MyEvent"); using var a3 = FootingActivitySource.StartSqlBatch("Users", 10); Assert.True(true); }
}
public class HealthTests
{
    private class FakeFactory : IDbConnectionFactory { public System.Data.IDbConnection CreateConnection() => new FakeConn(); }
    private class FakeConn : System.Data.IDbConnection { public string ConnectionString { get; set; } = ""; public int ConnectionTimeout => 0; public string Database => ""; public System.Data.ConnectionState State => System.Data.ConnectionState.Closed; public System.Data.IDbTransaction BeginTransaction() => null!; public System.Data.IDbTransaction BeginTransaction(System.Data.IsolationLevel il) => null!; public void ChangeDatabase(string d) { } public void Close() { } public System.Data.IDbCommand CreateCommand() => null!; public void Open() { } public void Dispose() { } }
    [Fact] public async Task DbHealth_Healthy() { var h = new DbHealthCheck(new FakeFactory()); var r = await h.CheckHealthAsync(new HealthCheckContext()); Assert.Equal(HealthStatus.Healthy, r.Status); }
    [Fact] public async Task EventBusHealth_Healthy() { var bus = new Footing.Framework.EventBus.EventBus(1); var h = new EventBusHealthCheck(bus); var r = await h.CheckHealthAsync(new HealthCheckContext()); Assert.Equal(HealthStatus.Healthy, r.Status); await bus.DisposeAsync(); }
}
