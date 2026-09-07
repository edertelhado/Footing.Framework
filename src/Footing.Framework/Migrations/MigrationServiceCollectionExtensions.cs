using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Footing.Framework.Migrations;

/// <summary>
/// Spring Security like: default SQL-97 journal + EmbeddedResource provider, porta aberta para substituir via TryAdd.
/// </summary>
public static class MigrationServiceCollectionExtensions
{
    /// <summary>
    /// Registra migrations agnósticas. Default: EmbeddedResource (assembly do caller) + SQL-97 journal.
    /// Override: registre IMigrationJournal ou IMigrationScriptProvider antes ou depois — TryAdd respeita o existente.
    /// </summary>
    public static IServiceCollection AddMigrations(
        this IServiceCollection services,
        Action<MigrationOptions>? configure = null)
    {
        if (configure != null) services.Configure(configure);
        else services.Configure<MigrationOptions>(_ => { });

        // TryAdd deixa porta aberta para substituir (ex: NpgsqlJournal custom, FileSystem provider)
        services.TryAddSingleton<IMigrationScriptProvider>(sp =>
        {
            var opts = sp.GetService<Microsoft.Extensions.Options.IOptions<MigrationOptions>>()?.Value ?? new MigrationOptions();
            if (!string.IsNullOrEmpty(opts.FileSystemFolder))
                return new FileSystemMigrationScriptProvider(opts.FileSystemFolder!);
            var asm = opts.EmbeddedAssembly ?? System.Reflection.Assembly.GetEntryAssembly() ?? typeof(MigrationServiceCollectionExtensions).Assembly;
            return new EmbeddedResourceMigrationScriptProvider(asm, opts.EmbeddedPrefix);
        });

        services.TryAddSingleton<IMigrationJournal, DefaultMigrationJournal>();
        services.TryAddSingleton<IMigrationRunner, MigrationRunner>();

        // HostedService opcional — roda Migrate no StartAsync se AutoMigrate true
        services.AddHostedService<MigrationHostedService>();

        return services;
    }

    /// <summary>
    /// Atalho para EmbeddedResource com assembly + prefix explícitos.
    /// </summary>
    public static IServiceCollection AddMigrations(
        this IServiceCollection services,
        System.Reflection.Assembly assembly,
        string? prefix = null,
        Action<MigrationOptions>? configure = null)
    {
        return services.AddMigrations(o =>
        {
            o.EmbeddedAssembly = assembly;
            o.EmbeddedPrefix = prefix;
            configure?.Invoke(o);
        });
    }
}

/// <summary>
/// Auto-migrate no startup quando MigrationOptions.AutoMigrate = true (default).
/// Valida/baseline/repair conforme opções antes do Migrate.
/// </summary>
public sealed class MigrationHostedService : IHostedService
{
    private readonly IMigrationRunner _runner;
    private readonly Microsoft.Extensions.Options.IOptions<MigrationOptions> _options;
    private readonly Microsoft.Extensions.Logging.ILogger<MigrationHostedService>? _log;

    public MigrationHostedService(IMigrationRunner runner, Microsoft.Extensions.Options.IOptions<MigrationOptions> options, Microsoft.Extensions.Logging.ILogger<MigrationHostedService>? log = null)
    {
        _runner = runner;
        _options = options;
        _log = log;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var o = _options.Value;
        if (!o.AutoMigrate) return;

        _log?.LogInformation("Migrations: starting AutoMigrate (Validate={Validate}, Repair={Repair}, Baseline={Baseline})", o.ValidateOnMigrate, o.RepairOnMigrate, o.BaselineOnMigrate);

        if (o.RepairOnMigrate)
        {
            _log?.LogInformation("Migrations: Repair (DELETE WHERE success='N')");
            await _runner.RepairAsync(cancellationToken);
        }
        if (o.BaselineOnMigrate)
        {
            _log?.LogInformation("Migrations: Baseline {Version}", o.BaselineVersion);
            await _runner.BaselineAsync(o.BaselineVersion, cancellationToken);
        }
        var result = await _runner.MigrateAsync(cancellationToken);
        _log?.LogInformation("Migrations: Migrate done — applied {Applied}, skipped {Skipped}", result.Applied.Count, result.Skipped.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
