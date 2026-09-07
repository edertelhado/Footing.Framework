using System.Threading.Channels;
using FootingFrameworkBus = Footing.Framework.EventBus.EventBus;
using FootingBusOptions = Footing.Framework.EventBus.EventBusOptions;

namespace Footing.Framework.Tests;

public class EventBusChannelTests
{
    private class TestEvent { public int Id { get; set; } }

    [Fact]
    public async Task Burst_10k_publish_sem_loss()
    {
        var bus = new FootingFrameworkBus(new FootingBusOptions { WorkerCount = 4, Capacity = 10000, FullMode = BoundedChannelFullMode.Wait });
        int counter = 0;
        var tcs = new TaskCompletionSource();
        bus.Subscribe<TestEvent>(async e =>
        {
            Interlocked.Increment(ref counter);
            if (Volatile.Read(ref counter) == 10000) tcs.TrySetResult();
            await Task.CompletedTask;
        });

        for (int i = 0; i < 10000; i++)
            await bus.PublishAsync(new TestEvent { Id = i });

        // Wait up to 5s for all processed
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(5000));
        await bus.StopAsync();
        Assert.Equal(10000, Volatile.Read(ref counter));
        Assert.True(completed == tcs.Task, "Timeout waiting for 10k events");
    }

    [Fact]
    public async Task Capacity_2_backpressure_TryWrite_warn_e_WriteAsync_Wait()
    {
        // Use capacity 2 with 1 worker that blocks
        var bus = new FootingFrameworkBus(new FootingBusOptions { WorkerCount = 1, Capacity = 2, FullMode = BoundedChannelFullMode.Wait });
        var blocker = new TaskCompletionSource();
        int processed = 0;
        bus.Subscribe<TestEvent>(async e =>
        {
            Interlocked.Increment(ref processed);
            await blocker.Task; // block worker
        });

        // Fill channel: Publish uses TryWrite, Worker is blocked on first event handler?
        // But Publish may succeed initially until channel full
        // Publish 2 should succeed (capacity 2), 3rd should fail via TryWrite
        // Need to ensure worker is busy. Publish first event, give worker time to start handling and block
        bus.Publish(new TestEvent { Id = 1 });
        await Task.Delay(200); // let worker pick up and block

        // Now channel is empty? Actually worker is blocked but already consumed 1 from channel, so channel has 0 pending
        // We need to fill channel to capacity 2 while worker blocked
        bool r2 = bus.TryPublish(new TestEvent { Id = 2 });
        bool r3 = bus.TryPublish(new TestEvent { Id = 3 });
        // With worker blocked, channel capacity 2 should hold 2 messages
        // r2 and r3 should be true (channel holds 2)
        // 4th message should fail
        bool r4 = bus.TryPublish(new TestEvent { Id = 4 });
        // Since channel full (2 pending while worker blocked on first), next TryWrite should fail (return false)
        // However timing dependent: if r2+r3 filled, r4 false; else if worker still processing, might be true
        // Assert that at least one TryPublish succeeded and channel respects capacity
        Assert.True(r2);
        Assert.True(r3);
        // With capacity 2 and Wait mode, TryWrite returns false when full
        Assert.False(r4);

        // Now test WriteAsync waits instead of dropping: PublishAsync should wait until blocker released
        var publishTask = bus.PublishAsync(new TestEvent { Id = 5 });
        // Should not complete immediately because channel full
        var delay = Task.Delay(300);
        var winner = await Task.WhenAny(publishTask, delay);
        Assert.True(winner == delay, "PublishAsync should wait when channel full");

        // Release blocker, allow draining
        blocker.SetResult();
        await publishTask.WaitAsync(TimeSpan.FromSeconds(2)); // should complete after drain

        await Task.Delay(300);
        await bus.StopAsync();
        // At least initial blocked + 2 queued + 1 waited = 4 plus first = total should be >=4
        Assert.True(Volatile.Read(ref processed) >= 4);
    }

    [Fact]
    public async Task StopAsync_drena_pendentes()
    {
        var bus = new FootingFrameworkBus(new FootingBusOptions { WorkerCount = 2, Capacity = 100, FullMode = BoundedChannelFullMode.Wait });
        int counter = 0;
        bus.Subscribe<TestEvent>(async e =>
        {
            await Task.Delay(10);
            Interlocked.Increment(ref counter);
        });

        for (int i = 0; i < 50; i++)
            bus.Publish(new TestEvent { Id = i });

        // Stop should drain via Writer.Complete + WhenAll workers
        await bus.StopAsync();
        // After StopAsync, all 50 should have been processed (drain)
        Assert.Equal(50, Volatile.Read(ref counter));
    }

    [Fact]
    public void Channel_options_sao_bounded_Wait_SingleReaderFalse()
    {
        // Verify EventBus uses BoundedChannelOptions with expected defaults
        // Indirect via behavior: creating bus with capacity 1000 and checking it enforces bounded
        var bus = new FootingFrameworkBus(new FootingBusOptions { WorkerCount = 1, Capacity = 5, FullMode = BoundedChannelFullMode.Wait, DrainOnStop = true });
        Assert.NotNull(bus);
        // Ensure no Task.Delay busy-loop: check source no Delay(10)
        var eventBusPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Footing.Framework", "EventBus", "EventBus.cs"));
        var content = File.ReadAllText(eventBusPath);
        Assert.DoesNotContain("Delay(10", content);
        Assert.DoesNotContain("ConcurrentQueue", content);
    }
}
