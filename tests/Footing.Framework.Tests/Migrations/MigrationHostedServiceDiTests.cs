using Footing.Framework.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Footing.Framework.Tests.Migrations;

public class MigrationHostedServiceDiTests
{
    private sealed class FakeJournal : IMigrationJournal
    {
        public int EnsureCalls;
        public Task EnsureHistoryTableAsync(CancellationToken ct = default) { Interlocked.Increment(ref EnsureCalls); return Task.CompletedTask; }
        public Task<IReadOnlyList<string>> GetAppliedVersionsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        public Task<bool> HasAppliedAsync(string v, CancellationToken ct = default) => Task.FromResult(false);
        public Task<string?> GetChecksumAsync(string v, CancellationToken ct = default) => Task.FromResult<string?>(null);
        public Task MarkAppliedAsync(MigrationInfo info, string checksum, long ms, string successYN, string? error, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateChecksumAsync(string v, string cs, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<string>> GetFailedVersionsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        public Task<IReadOnlyList<MigrationStatus>> GetInfoAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<MigrationStatus>>(Array.Empty<MigrationStatus>());
        public Task RepairAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task BaselineAsync(string v, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeRunner : IMigrationRunner
    {
        public int MigrateCalls;
        public int RepairCalls;
        public int BaselineCalls;
        public Task<MigrationResult> MigrateAsync(CancellationToken ct = default) { Interlocked.Increment(ref MigrateCalls); return Task.FromResult(new MigrationResult(Array.Empty<MigrationInfo>(), Array.Empty<MigrationInfo>(), TimeSpan.Zero)); }
        public Task ValidateAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<MigrationStatus>> InfoAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<MigrationStatus>>(Array.Empty<MigrationStatus>());
        public Task RepairAsync(CancellationToken ct = default) { Interlocked.Increment(ref RepairCalls); return Task.CompletedTask; }
        public Task BaselineAsync(string v = "0", CancellationToken ct = default) { Interlocked.Increment(ref BaselineCalls); return Task.CompletedTask; }
        public Task<string> GenerateScriptAsync(CancellationToken ct = default) => Task.FromResult("");
    }

    [Fact]
    public async Task HostedService_resolve_sem_journal_nao_throw_DI()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMigrationRunner>(new FakeRunner());
        services.AddSingleton<IOptions<MigrationOptions>>(Options.Create(new MigrationOptions { AutoMigrate = true }));
        services.AddSingleton<IHostedService, MigrationHostedService>();
        var sp = services.BuildServiceProvider();
        var hosted = sp.GetServices<IHostedService>().OfType<MigrationHostedService>().Single();
        await hosted.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task HostedService_EnsureHistoryTable_antes_de_Repair_e_Baseline()
    {
        var journal = new FakeJournal();
        var runner = new FakeRunner();
        var opts = Options.Create(new MigrationOptions { AutoMigrate = true, RepairOnMigrate = true, BaselineOnMigrate = true, BaselineVersion = "0" });
        var services = new ServiceCollection();
        services.AddSingleton<IMigrationJournal>(journal);
        services.AddSingleton<IMigrationRunner>(runner);
        services.AddSingleton<IOptions<MigrationOptions>>(opts);
        services.AddLogging();
        services.AddSingleton<IHostedService, MigrationHostedService>();
        var sp = services.BuildServiceProvider();
        var hosted = sp.GetServices<IHostedService>().OfType<MigrationHostedService>().Single();
        await hosted.StartAsync(CancellationToken.None);
        Assert.Equal(1, journal.EnsureCalls);
        Assert.Equal(1, runner.RepairCalls);
        Assert.Equal(1, runner.BaselineCalls);
        Assert.Equal(1, runner.MigrateCalls);
    }
}
