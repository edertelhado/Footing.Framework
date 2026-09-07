#nullable enable
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Footing.Framework.DI;

public static class DependencyWalkExtension
{
    public static void AddDependencyWalk(this IServiceCollection services, string rootNamespace,
        IConfiguration? configuration = null, params Assembly[] assemblies)
    {
        var typesToRegister = new List<Type>();
        foreach (var assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray()!;
            }
            catch
            {
                continue;
            }

            foreach (var type in types)
            {
                if (type.IsClass && !type.IsAbstract)
                    typesToRegister.Add(type);
            }
        }

        var distinctTypes = typesToRegister.Distinct().ToList();

        foreach (var type in distinctTypes)
        {
            var injectableAttr = type.GetCustomAttribute<InjectableAttribute>();
            if (injectableAttr != null)
            {
                ResolverExtensions.RegisterType(services, type, injectableAttr.Lifetime, rootNamespace, configuration);
                continue;
            }

            var repoAttr = type.GetCustomAttribute<RepositoryAttribute>();
            if (repoAttr != null)
            {
                ResolverExtensions.RegisterType(services, type, repoAttr.Lifetime, rootNamespace, configuration);
                continue;
            }

            var serviceAttr = type.GetCustomAttribute<ServiceAttribute>();
            if (serviceAttr != null)
            {
                ResolverExtensions.RegisterType(services, type, serviceAttr.Lifetime, rootNamespace, configuration);
                continue;
            }
        }
    }
}
