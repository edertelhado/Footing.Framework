// Snippet docs — MemoryIdempotencyStore desacoplado (exemplo, não pack)
// Uso dev single-pod apenas; K8s 2 pods quebra — prefira Redis/Firebird TABLE
using Microsoft.Extensions.Caching.Memory;
using Footing.Framework.Idempotency;
public sealed class MemoryIdempotencyStore(IMemoryCache cache):IIdempotencyStore{
 public MemoryIdempotencyStore():this(new MemoryCache(new MemoryCacheOptions())){}
 public bool TryGet<T>(string k,out T? v){if(cache.TryGetValue(k,out var o)&&o is T t){v=t;return true;}v=default;return false;}
 public void Set<T>(string k,T v,TimeSpan? ttl=null){var o=new MemoryCacheEntryOptions();if(ttl.HasValue)o.SetAbsoluteExpiration(ttl.Value);else o.SetSlidingExpiration(TimeSpan.FromHours(1));cache.Set(k,v,o);}
 public async Task<T> GetOrCreateAsync<T>(string k,Func<Task<T>> f,TimeSpan? ttl=null,CancellationToken ct=default){if(TryGet<T>(k,out var cur)&&cur!=null)return cur;var r=await f();Set(k,r,ttl);return r;}}
