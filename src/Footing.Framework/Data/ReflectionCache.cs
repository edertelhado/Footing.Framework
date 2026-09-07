using System.Collections.Concurrent;
using System.Reflection;

namespace Footing.Framework.Data;

/// <summary>
/// Shared PropsCache for SqlTemplate and SqlBatch — single ConcurrentDictionary per Type.
/// </summary>
internal static class ReflectionCache
{
    internal static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropsCache = new();

    internal static PropertyInfo[] Get(Type type)
        => PropsCache.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).ToArray());
}

/// <summary>
/// Alias for PropsCacheHelper — forwards to ReflectionCache to keep 1 dict shared.
/// </summary>
internal static class PropsCacheHelper
{
    internal static ConcurrentDictionary<Type, PropertyInfo[]> Cache => ReflectionCache.PropsCache;

    internal static PropertyInfo[] Get(Type type) => ReflectionCache.Get(type);

    internal static PropertyInfo[] GetOrAdd(Type type) => ReflectionCache.Get(type);
}
