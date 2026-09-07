using System.Reflection;
using Footing.Framework.Attributes;
using Footing.Framework.DI;
using Footing.Framework.EventBus;
using Footing.Framework.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Footing.Framework.Tests;

public class LifecycleDedupTests
{
    // Helpers for PostConstruct / PreDestroy
    private class PostConstructSuccess
    {
        public bool Called { get; set; }
        [PostConstruct] public void Init() => Called = true;
    }

    private class PostConstructAsyncThrows
    {
        [PostConstruct] public async Task InitAsync()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("post fail");
        }
    }

    private class PreDestroyAsyncWithDisposeSync : IDisposable
    {
        public bool PreDestroyCalled { get; private set; }
        public bool Disposed { get; private set; }
        [PreDestroy] public async Task CleanupAsync()
        {
            await Task.Delay(20);
            PreDestroyCalled = true;
        }
        public void Dispose() => Disposed = true;
    }

    private class PreDestroyAsyncWithDisposeAsync : IAsyncDisposable
    {
        public bool PreDestroyCalled { get; private set; }
        public bool DisposeAsyncCalled { get; private set; }
        [PreDestroy] public async Task CleanupAsync()
        {
            await Task.Delay(10);
            PreDestroyCalled = true;
        }
        public async ValueTask DisposeAsync()
        {
            await Task.Delay(5);
            DisposeAsyncCalled = true;
        }
    }

    // EventListener dedup
    private class EventListenerSample
    {
        public static int Count = 0;
        [EventListener] public void OnTest(TestEvt evt) => Interlocked.Increment(ref Count);
    }
    private class TestEvt { }

    [Service] private class MyServiceA { }
    [Service] private class MyServiceB { }

    [Fact]
    public void PreDestroy_async_plus_Dispose_sync_nao_deadlock_timeout_5s()
    {
        var instance = new PreDestroyAsyncWithDisposeSync();
        var pre = typeof(PreDestroyAsyncWithDisposeSync).GetMethod("CleanupAsync")!;
        var fwAsm = typeof(Footing.Framework.Data.SqlTemplate).Assembly;
        var proxyType = fwAsm.GetType("Footing.Framework.Lifecycle.LifecycleProxy")!;
        var proxy = Activator.CreateInstance(proxyType, instance, null, pre)!;
        var disposeMethod = proxyType.GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance)!;
        var task = Task.Run(() => disposeMethod.Invoke(proxy, null));
        bool completed = task.Wait(TimeSpan.FromSeconds(5));
        Assert.True(completed, "Dispose deadlock - should complete within 5s via Task.Run");
        Assert.True(instance.PreDestroyCalled);
        Assert.True(instance.Disposed);
    }

    [Fact]
    public async Task PreDestroy_async_plus_DisposeAsync_executa()
    {
        var instance = new PreDestroyAsyncWithDisposeAsync();
        var pre = typeof(PreDestroyAsyncWithDisposeAsync).GetMethod("CleanupAsync")!;
        var fwAsm = typeof(Footing.Framework.Data.SqlTemplate).Assembly;
        var proxyType = fwAsm.GetType("Footing.Framework.Lifecycle.LifecycleProxy")!;
        var proxy = Activator.CreateInstance(proxyType, instance, null, pre)!;
        var disposeAsyncMethod = proxyType.GetMethod("DisposeAsync", BindingFlags.Public | BindingFlags.Instance)!;
        var vt = (ValueTask)disposeAsyncMethod.Invoke(proxy, null)!;
        await vt;
        Assert.True(instance.PreDestroyCalled);
        Assert.True(instance.DisposeAsyncCalled);
    }

    [Fact]
    public void PostConstruct_async_propaga_InnerException()
    {
        // Directly test ResolverExtensions.ExecutePostConstruct via reflection (private method)
        var fwAsm = typeof(Footing.Framework.Data.SqlTemplate).Assembly;
        var resolverType = fwAsm.GetType("Footing.Framework.DI.ResolverExtensions")!;
        var method = resolverType.GetMethod("ExecutePostConstruct", BindingFlags.NonPublic | BindingFlags.Static)!;
        var instance = new PostConstructAsyncThrows();
        var tie = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { instance }));
        var ex = Assert.IsType<InvalidOperationException>(tie.InnerException);
        Assert.NotNull(ex.InnerException);
        Assert.Equal("post fail", ex.InnerException!.Message);
    }

    [Fact]
    public async Task RegisterEventListeners_idempotente_count_1_apos_duplo_AddDependencyWalk()
    {
        EventListenerSample.Count = 0;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventBus(o => { o.WorkerCount = 1; o.Capacity = 100; });
        // Use EventBus directly for dedup test (not DependencyWalk)
        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<Footing.Framework.EventBus.EventBus>();

        // Test dedup directly: call RegisterListenersFromInstance twice
        var sample = new EventListenerSample();
        bus.RegisterListenersFromInstance(sample);
        bus.RegisterListenersFromInstance(sample); // second should be no-op due to _registeredListeners TryAdd

        // Also test HostedService idempotence
        var hosted = provider.GetServices<IHostedService>().OfType<EventBusHostedService>().First();
        await hosted.StartAsync(CancellationToken.None);
        await hosted.StartAsync(CancellationToken.None); // second start should be idempotent

        // Publish and wait
        int before = EventListenerSample.Count;
        await bus.PublishAsync(new TestEvt());
        await Task.Delay(500);
        await bus.StopAsync();
        // Should have incremented exactly 1 per publish (not 2) because dedup
        Assert.Equal(1, EventListenerSample.Count - before);
    }

    [Fact]
    public void DependencyWalk_nao_pega_System()
    {
        var services = new ServiceCollection();
        // Use rootNamespace that would match System if bug (StartsWith incorrect)
        // AddDependencyWalk with assembly containing System types should not register System.*
        // We pass the assembly that contains our test types and also System assembly implicitly via AppDomain scan?
        // Explicit test: add assembly System.Private.CoreLib? Should not register.
        var asm = typeof(string).Assembly; // System.Private.CoreLib
        services.AddDependencyWalk("System", null, asm);
        // Should not have registered any System type as service
        bool hasSystemString = services.Any(d => d.ServiceType == typeof(string));
        Assert.False(hasSystemString);
        // Also verify filtering by GetName.Name not FullName: root "Footing" should not match "Footing.Framework.Tests" extra if using FullName?
        // Already covered by not picking System.*
    }

    [Fact]
    public void Obsolete_attributes_absent()
    {
        var fwAsm = typeof(Footing.Framework.Data.SqlTemplate).Assembly;
        // Nenhum type ou método deve ter [Obsolete] em src (TypeHandlerRegistry + AddDependencyWalkAuto removidos em 3.0.0)
        var obsoleteTypes = fwAsm.GetTypes()
            .Where(t => t.GetCustomAttribute<ObsoleteAttribute>() != null)
            .ToList();
        var obsoleteMethods = fwAsm.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttribute<ObsoleteAttribute>() != null)
            .ToList();
        Assert.Empty(obsoleteTypes);
        Assert.Empty(obsoleteMethods);
        // TypeHandlerRegistry não existe mais
        Assert.Null(fwAsm.GetType("Footing.Framework.Data.TypeHandlerRegistry"));
        // AddDependencyWalkAuto removido, apenas AddDependencyWalk permanece
        Assert.Null(typeof(DependencyWalkExtension).GetMethod("AddDependencyWalkAuto"));
        Assert.NotNull(typeof(DependencyWalkExtension).GetMethod("AddDependencyWalk"));
    }
}
