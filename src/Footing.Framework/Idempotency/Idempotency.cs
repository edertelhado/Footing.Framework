namespace Footing.Framework.Idempotency;
public interface IIdempotencyStore{bool TryGet<T>(string key,out T? value);void Set<T>(string key,T value,TimeSpan? ttl=null);Task<T> GetOrCreateAsync<T>(string key,Func<Task<T>> factory,TimeSpan? ttl=null,CancellationToken ct=default);}
