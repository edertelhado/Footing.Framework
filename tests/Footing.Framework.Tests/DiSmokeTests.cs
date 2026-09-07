using Footing.Framework.Attributes;
using Footing.Framework.DI;
using Footing.Framework.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Footing.Framework.Tests;

// Dummy services for smoke — agnostic, no DB, detects missing assembly without SG
[Service]
public sealed class DiSmokeDummyService { public string Ping() => "pong"; }

[Repository]
public sealed class DiSmokeDummyRepo { public string Ping() => "pong"; }

[Injectable(InjectableLifetime.Singleton)]
public sealed class DiSmokeDummySingleton { }

public class DiSmokeTests
{
    [Fact]
    public async Task DiSmoke_GetServices_Count_Equals_Expected()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventBus();
        services.AddDependencyWalk("Footing.Framework.Tests", null, typeof(DiSmokeTests).Assembly);

        // Build e valida contagem — se assembly esquecido, count ==0
        await using var sp = services.BuildServiceProvider();

        // Deve registrar DummyService e DummyRepo e DummySingleton via scan
        var svcCount = services.Count(d => d.ServiceType == typeof(DiSmokeDummyService));
        var repoCount = services.Count(d => d.ServiceType == typeof(DiSmokeDummyRepo));
        var singletonCount = services.Count(d => d.ServiceType == typeof(DiSmokeDummySingleton));

        Assert.Equal(1, svcCount);
        Assert.Equal(1, repoCount);
        Assert.Equal(1, singletonCount);

        // GetServices deve retornar 1 cada (agência: sem SG ainda)
        var svcInstances = sp.GetServices<DiSmokeDummyService>().ToList();
        var repoInstances = sp.GetServices<DiSmokeDummyRepo>().ToList();
        Assert.Single(svcInstances);
        Assert.Single(repoInstances);
        await sp.DisposeAsync();
    }

    [Fact]
    public async Task DiSmoke_ValidateScopes_NoException()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventBus(o => { o.WorkerCount = 1; o.Capacity = 10; });
        services.AddDependencyWalk("Footing.Framework.Tests", null, typeof(DiSmokeTests).Assembly);

        // ValidateScopes + ValidateOnBuild deve passar sem throw — pega escopo errado
        await using var sp = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        Assert.NotNull(sp);
        // Resolve scope factory sem throw
        var scopeFactory = sp.GetService<IServiceScopeFactory>();
        Assert.NotNull(scopeFactory);
        using var scope = scopeFactory!.CreateScope();
        var scopedSvc = scope.ServiceProvider.GetService<DiSmokeDummyRepo>();
        Assert.NotNull(scopedSvc);
        await sp.DisposeAsync();
    }
}
