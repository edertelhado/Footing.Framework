using System.Reflection;
namespace Footing.Framework.Lifecycle;

internal class LifecycleProxy : IDisposable, IAsyncDisposable
{
    private readonly object _instance;
    private readonly MethodInfo? _preDestroy;
    private readonly MethodInfo? _preDestroyAsync;

    public object Instance => _instance;

    public LifecycleProxy(object instance, MethodInfo? preDestroy, MethodInfo? preDestroyAsync)
    {
        _instance = instance;
        _preDestroy = preDestroy;
        _preDestroyAsync = preDestroyAsync;
    }

    public void Dispose()
    {
        DisposeCore();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    private void DisposeCore()
    {
        if (_preDestroyAsync != null)
        {
            try
            {
                var task = (Task)_preDestroyAsync.Invoke(_instance, null)!;
                Task.Run(async () => await task.ConfigureAwait(false)).GetAwaiter().GetResult();
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw new InvalidOperationException(
                    $"Error executing [PreDestroy] on {_instance.GetType().Name}", ex.InnerException);
            }
        }
        else if (_preDestroy != null)
        {
            try
            {
                _preDestroy.Invoke(_instance, null);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw new InvalidOperationException(
                    $"Error executing [PreDestroy] on {_instance.GetType().Name}", ex.InnerException);
            }
        }

        (_instance as IDisposable)?.Dispose();
    }

    private async ValueTask DisposeAsyncCore()
    {
        if (_preDestroyAsync != null)
        {
            try
            {
                var task = (Task)_preDestroyAsync.Invoke(_instance, null)!;
                await task.ConfigureAwait(false);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw new InvalidOperationException(
                    $"Error executing [PreDestroy] on {_instance.GetType().Name}", ex.InnerException);
            }
        }
        else if (_preDestroy != null)
        {
            try
            {
                _preDestroy.Invoke(_instance, null);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw new InvalidOperationException(
                    $"Error executing [PreDestroy] on {_instance.GetType().Name}", ex.InnerException);
            }
        }

        if (_instance is IAsyncDisposable ad)
            await ad.DisposeAsync().ConfigureAwait(false);
        else if (_instance is IDisposable d)
            d.Dispose();
    }
}
